/* 
 * Copyright 2017-2018, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using OptimizelySDK.Bucketing;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event.Builder;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Exceptions;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;
using OptimizelySDK.Notifications;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OptimizelySDK
{
    public class Optimizely : IOptimizely
    {
        private Bucketer Bucketer;

        private EventBuilder EventBuilder;

        private IEventDispatcher EventDispatcher;
        
        private ProjectConfig Config;

        private ILogger Logger;

        private IErrorHandler ErrorHandler;

        private UserProfileService UserProfileService;

        private DecisionService DecisionService;

        public NotificationCenter NotificationCenter;

        public bool IsValid { get; private set; }

        public static String SDK_VERSION {
            get {
                // Example output: "2.1.0" .  Should be kept in synch with NuGet package version.
#if NET35
                Assembly assembly = Assembly.GetExecutingAssembly();
#else
                Assembly assembly = typeof(Optimizely).GetTypeInfo().Assembly;
#endif
                // Microsoft    Major.Minor.Build.Revision
                // Semantic     Major.Minor.Patch
                Version version = assembly.GetName().Version;
                String answer = String.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
                return answer;
            }
        }

        public static String SDK_TYPE {
            get {
                return "csharp-sdk";
            }
        }

        public const string USER_ID = "User Id";
        public const string EXPERIMENT_KEY = "Experiment Key";
        public const string EVENT_KEY = "Event Key";
        public const string FEATURE_KEY = "Feature Key";
        public const string VARIABLE_KEY = "Variable Key";

        /// <summary>
        /// Optimizely constructor for managing Full Stack .NET projects.
        /// </summary>
        /// <param name="datafile">string JSON string representing the project</param>
        /// <param name="eventDispatcher">EventDispatcherInterface</param>
        /// <param name="logger">LoggerInterface</param>
        /// <param name="errorHandler">ErrorHandlerInterface</param>
        /// <param name="skipJsonValidation">boolean representing whether JSON schema validation needs to be performed</param>
        public Optimizely(string datafile,
                          IEventDispatcher eventDispatcher = null,
                          ILogger logger = null,
                          IErrorHandler errorHandler = null,
                          UserProfileService userProfileService = null,
                          bool skipJsonValidation = false)
        {
            IsValid = false; // invalid until proven valid
            Logger = logger ?? new NoOpLogger();
            EventDispatcher = eventDispatcher ?? new DefaultEventDispatcher(Logger);
            ErrorHandler = errorHandler ?? new NoOpErrorHandler();
            Bucketer = new Bucketer(Logger);
            EventBuilder = new EventBuilder(Bucketer, Logger);
            UserProfileService = userProfileService;
            NotificationCenter = new NotificationCenter(Logger);

            try
            {
                if (!ValidateInputs(datafile, skipJsonValidation))
                {
                    Logger.Log(LogLevel.ERROR, "Provided 'datafile' has invalid schema.");
                    return;
                }

                Config = ProjectConfig.Create(datafile, Logger, ErrorHandler);
                IsValid = true;
                DecisionService = new DecisionService(Bucketer, ErrorHandler, Config, userProfileService, Logger);
            }
            catch (Exception ex)
            {
                string error = String.Empty;
                if (ex.GetType() == typeof(ConfigParseException))
                    error = ex.Message;
                else
                    error = "Provided 'datafile' is in an invalid format. " + ex.Message;

                Logger.Log(LogLevel.ERROR, error);
                ErrorHandler.HandleError(ex);
            }
        }


        /// <summary>
        /// Helper function to validate all required conditions before performing activate or track.
        /// </summary>
        /// <param name="experiment">Experiment Object representing experiment</param>
        /// <param name="userId">string ID for user</param>
        /// <param name="userAttributes">associative array of Attributes for the user</param>
        private bool ValidatePreconditions(Experiment experiment, string userId, UserAttributes userAttributes = null)
        {
            if (!experiment.IsExperimentRunning)
            {
                Logger.Log(LogLevel.INFO, string.Format("Experiment {0} is not running.", experiment.Key));
                return false;
            }

            if (experiment.IsUserInForcedVariation(userId))
            {
                return true;
            }

            if (!ExperimentUtils.IsUserInExperiment(Config, experiment, userAttributes))
            {
                Logger.Log(LogLevel.INFO, string.Format("User \"{0}\" does not meet conditions to be in experiment \"{1}\".", userId, experiment.Key));
                return false;
            }

            return true;
        }


        /// <summary>
        /// Buckets visitor and sends impression event to Optimizely.
        /// </summary>
        /// <param name="experimentKey">experimentKey string Key identifying the experiment</param>
        /// <param name="userId">string ID for user</param>
        /// <param name="userAttributes">associative array of Attributes for the user</param>
        /// <returns>null|Variation Representing variation</returns>
        public Variation Activate(string experimentKey, string userId, UserAttributes userAttributes = null)
        {
            if (!IsValid)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'activate'.");
                return null;
            }

            var inputValues = new Dictionary<string, string>
            {
                { USER_ID, userId },
                { EXPERIMENT_KEY, experimentKey }
            };

            if (!ValidateStringInputs(inputValues))
                return null;

            var experiment = Config.GetExperimentFromKey(experimentKey);

            if (experiment.Key == null)
            {
                Logger.Log(LogLevel.INFO, string.Format("Not activating user {0}.", userId));
                return null;
            }

            var variation = DecisionService.GetVariation(experiment, userId, userAttributes);

            if (variation == null || variation.Key == null)
            {
                Logger.Log(LogLevel.INFO, string.Format("Not activating user {0}.", userId));
                return null;
            }

            SendImpressionEvent(experiment, variation, userId, userAttributes);

            return variation;
        }

        /// <summary>
        /// Validate datafile
        /// </summary>
        /// <param name="datafile">string JSON string representing the project.</param>
        /// <param name="skipJsonValidation">whether JSON schema validation needs to be performed</param>
        /// <returns>true iff all provided inputs are valid</returns>
        private bool ValidateInputs(string datafile, bool skipJsonValidation)
        {
            return skipJsonValidation || Validator.ValidateJSONSchema(datafile);
        }


        /// <summary>
        /// Sends conversion event to Optimizely.
        /// </summary>
        /// <param name="eventKey">Event key representing the event which needs to be recorded</param>
        /// <param name="userId">ID for user</param>
        /// <param name="userAttributes">Attributes of the user</param>
        /// <param name="eventTags">eventTags array Hash representing metadata associated with the event.</param>
        public void Track(string eventKey, string userId, UserAttributes userAttributes = null, EventTags eventTags = null)
        {
            if (!IsValid)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'track'.");
                return;
            }

            var inputValues = new Dictionary<string, string>
            {
                { USER_ID, userId },
                { EVENT_KEY, eventKey }
            };

            if (!ValidateStringInputs(inputValues))
                return;

            var eevent = Config.GetEvent(eventKey);

            if (eevent.Key == null)
            {
                Logger.Log(LogLevel.ERROR, string.Format("Not tracking user {0} for event {1}.", userId, eventKey));
                return;
            }

            // Filter out experiments that are not running or when user(s) do not meet conditions.
            var validExperimentIdToVariationMap = new Dictionary<string, Variation>();
            var experimentIds = eevent.ExperimentIds;
            foreach (string id in eevent.ExperimentIds)
            {
                var experiment = Config.GetExperimentFromId(id);
                //Validate experiment
                var variation = DecisionService.GetVariation(experiment, userId, userAttributes);

                if (variation != null)
                {
                    validExperimentIdToVariationMap[experiment.Id] = variation;
                }
                else
                {
                    Logger.Log(LogLevel.INFO, string.Format("Not tracking user \"{0}\" for experiment \"{1}\"", userId, experiment.Key));
                }
            }

            if (validExperimentIdToVariationMap.Count > 0)
            {

                if (eventTags != null)
                {
                    eventTags = eventTags.FilterNullValues(Logger);
                }

                var conversionEvent = EventBuilder.CreateConversionEvent(Config, eventKey, validExperimentIdToVariationMap,
                    userId, userAttributes, eventTags);
                Logger.Log(LogLevel.INFO, string.Format("Tracking event {0} for user {1}.", eventKey, userId));
                Logger.Log(LogLevel.DEBUG, string.Format("Dispatching conversion event to URL {0} with params {1}.", 
                    conversionEvent.Url, conversionEvent.GetParamsAsJson()));

                try
                {
                    EventDispatcher.DispatchEvent(conversionEvent);
                }
                catch (Exception exception)
                {
                    Logger.Log(LogLevel.ERROR, string.Format("Unable to dispatch conversion event. Error {0}", exception.Message));
                }

                NotificationCenter.SendNotifications(NotificationCenter.NotificationType.Track, eventKey, userId, 
                    userAttributes, eventTags, conversionEvent);
            }
            else
            {
                Logger.Log(LogLevel.INFO, string.Format("There are no valid experiments for event {0} to track.", eventKey));
            }
        }


        /// <summary>
        /// Get variation where user will be bucketed
        /// </summary>
        /// <param name="experimentKey">experimentKey string Key identifying the experiment</param>
        /// <param name="userId">ID for the user</param>
        /// <param name="userAttributes">Attributes for the users</param>
        /// <returns>null|Variation Representing variation</returns>
        public Variation GetVariation(string experimentKey, string userId, UserAttributes userAttributes = null)
        {
            if (!IsValid)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'GetVariation'.");
                return null;
            }

            var inputValues = new Dictionary<string, string>
            {
                { USER_ID, userId },
                { EXPERIMENT_KEY, experimentKey }
            };

            if (!ValidateStringInputs(inputValues))
                return null;

            Experiment experiment = Config.GetExperimentFromKey(experimentKey);
            if (experiment.Key == null)
                return null;

            return DecisionService.GetVariation(experiment, userId, userAttributes);
        }

        /// <summary>
        /// Force a user into a variation for a given experiment.
        /// </summary>
        /// <param name="experimentKey">The experiment key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="variationKey">The variation key specifies the variation which the user will be forced into.
        /// If null, then clear the existing experiment-to-variation mapping.</param>
        /// <returns>A boolean value that indicates if the set completed successfully.</returns>
        public bool SetForcedVariation(string experimentKey, string userId, string variationKey)
        {
            var inputValues = new Dictionary<string, string>
            {
                { USER_ID, userId },
                { EXPERIMENT_KEY, experimentKey }
            };

            return ValidateStringInputs(inputValues) && Config.SetForcedVariation(experimentKey, userId, variationKey);
        }

        /// <summary>
        /// Gets the forced variation key for the given user and experiment.  
        /// </summary>
        /// <param name="experimentKey">The experiment key</param>
        /// <param name="userId">The user ID</param>
        /// <returns>null|string The variation key.</returns>
        public Variation GetForcedVariation(string experimentKey, string userId)
        {
            var inputValues = new Dictionary<string, string>
            {
                { USER_ID, userId },
                { EXPERIMENT_KEY, experimentKey }
            };

            if (!ValidateStringInputs(inputValues))
                return null;

            return Config.GetForcedVariation(experimentKey, userId);
        }

        #region  FeatureFlag APIs

        /// <summary>
        /// Determine whether a feature is enabled.
        /// Send an impression event if the user is bucketed into an experiment using the feature.
        /// </summary>
        /// <param name="featureKey">The feature key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes.</param>
        /// <returns>True if feature is enabled, false or null otherwise</returns>
        public virtual bool IsFeatureEnabled(string featureKey, string userId, UserAttributes userAttributes = null)
        {
            var inputValues = new Dictionary<string, string>
            {
                { USER_ID, userId },
                { FEATURE_KEY, featureKey }
            };

            if (!ValidateStringInputs(inputValues))
                return false;

            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            if (string.IsNullOrEmpty(featureFlag.Key))
                return false;

            if (!Validator.IsFeatureFlagValid(Config, featureFlag))
                return false;

            var decision = DecisionService.GetVariationForFeature(featureFlag, userId, userAttributes);
            if (decision != null) {
                if (decision.Source == FeatureDecision.DECISION_SOURCE_EXPERIMENT) {
                    SendImpressionEvent(decision.Experiment, decision.Variation, userId, userAttributes);
                } else {
                    Logger.Log(LogLevel.INFO, $@"The user ""{userId}"" is not being experimented on feature ""{featureKey}"".");
                }
                if (decision.Variation.IsFeatureEnabled) {
                    Logger.Log(LogLevel.INFO, $@"Feature flag ""{featureKey}"" is enabled for user ""{userId}"".");
                    return true;
                }
            }

            Logger.Log(LogLevel.INFO, $@"Feature flag ""{featureKey}"" is not enabled for user ""{userId}"".");
            return false;
        }

        /// <summary>
        /// Gets the feature variable value for given type.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="variableKey">The variable key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <param name="variableType">Variable type</param>
        /// <returns>string | null Feature variable value</returns>
        public virtual string GetFeatureVariableValueForType(string featureKey, string variableKey, string userId, 
            UserAttributes userAttributes, FeatureVariable.VariableType variableType)
        {
            var inputValues = new Dictionary<string, string>
            {
                { USER_ID, userId },
                { FEATURE_KEY, featureKey },
                { VARIABLE_KEY, variableKey }
            };

            if (!ValidateStringInputs(inputValues))
                return null;

            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            if (string.IsNullOrEmpty(featureFlag.Key))
                return null;

            var featureVariable = featureFlag.GetFeatureVariableFromKey(variableKey);
            if (featureVariable == null)
            {
                Logger.Log(LogLevel.ERROR,
                    $@"No feature variable was found for key ""{variableKey}"" in feature flag ""{featureKey}"".");
                return null;
            }
            else if (featureVariable.Type != variableType)
            {
                Logger.Log(LogLevel.ERROR,
                    $@"Variable is of type ""{featureVariable.Type}"", but you requested it as type ""{variableType}"".");
                return null;
            }

            var variableValue = featureVariable.DefaultValue;
            var decision = DecisionService.GetVariationForFeature(featureFlag, userId, userAttributes);

            if (decision != null)
            {
                var variation = decision.Variation;
                var featureVariableUsageInstance = variation.GetFeatureVariableUsageFromId(featureVariable.Id);

                if (featureVariableUsageInstance != null)
                {
                    variableValue = featureVariableUsageInstance.Value;
                    Logger.Log(LogLevel.INFO,
                        $@"Returning variable value ""{variableValue}"" for variation ""{variation.Key}"" of feature flag ""{featureFlag.Key}"".");
                }
                else
                {
                    Logger.Log(LogLevel.INFO,
                        $@"Variable ""{variableKey}"" is not used in variation ""{variation.Key}"", returning default value ""{variableValue}"".");
                }
            }
            else
            {
                Logger.Log(LogLevel.INFO,
                    $@"User ""{userId}"" is not in any variation for feature flag ""{featureFlag.Key}"", returning default value ""{variableValue}"".");
            }

            return variableValue;
        }

        /// <summary>
        /// Gets boolean feature variable value.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="variableKey">The variable key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>bool | Feature variable value or null</returns>
        public bool? GetFeatureVariableBoolean(string featureKey, string variableKey, string userId, UserAttributes userAttributes = null)
        {
            var variableType = FeatureVariable.VariableType.BOOLEAN;
            var variableValue = GetFeatureVariableValueForType(featureKey, variableKey, userId, userAttributes, variableType);
            
            if (variableValue != null)
            {
                if (Boolean.TryParse(variableValue, out bool booleanValue))
                    return booleanValue;
                else
                    Logger.Log(LogLevel.ERROR, $@"Unable to cast variable value ""{variableValue}"" to type ""{variableType}"".");
            }
            
            return null;
        }

        /// <summary>
        /// Gets double feature variable value.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="variableKey">The variable key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>double | Feature variable value or null</returns>
        public double? GetFeatureVariableDouble(string featureKey, string variableKey, string userId, UserAttributes userAttributes = null)
        {
            var variableType = FeatureVariable.VariableType.DOUBLE;
            var variableValue = GetFeatureVariableValueForType(featureKey, variableKey, userId, userAttributes, variableType);

            if (variableValue != null)
            {
                if (Double.TryParse(variableValue, out double doubleValue))
                    return doubleValue;
                else
                    Logger.Log(LogLevel.ERROR, $@"Unable to cast variable value ""{variableValue}"" to type ""{variableType}"".");
            }

            return null;
        }

        /// <summary>
        /// Gets integer feature variable value.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="variableKey">The variable key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>int | Feature variable value or null</returns>
        public int? GetFeatureVariableInteger(string featureKey, string variableKey, string userId, UserAttributes userAttributes = null)
        {
            var variableType = FeatureVariable.VariableType.INTEGER;
            var variableValue = GetFeatureVariableValueForType(featureKey, variableKey, userId, userAttributes, variableType);

            if (variableValue != null)
            {
                if (Int32.TryParse(variableValue, out int intValue))
                    return intValue;
                else
                    Logger.Log(LogLevel.ERROR, $@"Unable to cast variable value ""{variableValue}"" to type ""{variableType}"".");
            }

            return null;
        }

        /// <summary>
        /// Gets string feature variable value.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="variableKey">The variable key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>string | Feature variable value or null</returns>
        public string GetFeatureVariableString(string featureKey, string variableKey, string userId, UserAttributes userAttributes = null)
        {
            return GetFeatureVariableValueForType(featureKey, variableKey, userId, userAttributes, 
                FeatureVariable.VariableType.STRING);
        }

        /// <summary>
        /// Sends impression event.
        /// </summary>
        /// <param name="experiment">The experiment</param>
        /// <param name="variationId">The variation entity</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        private void SendImpressionEvent(Experiment experiment, Variation variation, string userId, 
        UserAttributes userAttributes)
        {
            if (experiment.IsExperimentRunning)
            {
                var impressionEvent = EventBuilder.CreateImpressionEvent(Config, experiment, variation.Id, userId, userAttributes);
                Logger.Log(LogLevel.INFO, string.Format("Activating user {0} in experiment {1}.", userId, experiment.Key));
                Logger.Log(LogLevel.DEBUG, string.Format("Dispatching impression event to URL {0} with params {1}.",
                    impressionEvent.Url, impressionEvent.GetParamsAsJson()));

                try
                {
                    EventDispatcher.DispatchEvent(impressionEvent);
                }
                catch (Exception exception)
                {
                    Logger.Log(LogLevel.ERROR, string.Format("Unable to dispatch impression event. Error {0}", exception.Message));
                }

                NotificationCenter.SendNotifications(NotificationCenter.NotificationType.Activate, experiment, userId,
                    userAttributes, variation, impressionEvent);
            }
            else
            {
                Logger.Log(LogLevel.ERROR, @"Experiment has ""Launched"" status so not dispatching event during activation.");
            }
        }

        /// <summary>
        /// Get the list of features that are enabled for the user.
        /// </summary>
        /// <param name="userId">The user Id</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>List of the feature keys that are enabled for the user.</returns>
        public List<string> GetEnabledFeatures(string userId, UserAttributes userAttributes = null)
        {
            List<string> enabledFeaturesList = new List<string>();

            if (!IsValid)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'GetEnabledFeatures'.");
                return enabledFeaturesList;
            }

            if (!ValidateStringInputs(new Dictionary<string, string> { { USER_ID, userId } }))
                return enabledFeaturesList;

            foreach (var feature in Config.FeatureKeyMap.Values)
            {
                var featureKey = feature.Key;
                if (IsFeatureEnabled(featureKey, userId, userAttributes))
                    enabledFeaturesList.Add(featureKey);
            }

            return enabledFeaturesList;
        }

        #endregion // FeatureFlag APIs

        /// <summary>
        /// Validate all string inputs are not null or empty.
        /// </summary>
        /// <param name="inputs">Array Hash input types and values</param>
        /// <returns>True if all values are valid, false otherwise</returns>
        private bool ValidateStringInputs(Dictionary<string, string> inputs)
        {
            bool isValid = true;

            // Empty user Id is valid value.
            if (inputs.ContainsKey(USER_ID))
            {
                if (inputs[USER_ID] == null)
                {
                    Logger.Log(LogLevel.ERROR, $"Provided {USER_ID} is in invalid format.");
                    isValid = false;
                }

                inputs.Remove(USER_ID);
            }

            foreach(var input in inputs)
            {
                if (string.IsNullOrEmpty(input.Value))
                {
                    Logger.Log(LogLevel.ERROR, $"Provided {input.Key} is in invalid format.");
                    isValid = false;
                }
            }

            return isValid;
        }
    }
}
