﻿/* 
 * Copyright 2017, Optimizely
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
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;
using OptimizelySDK.Notifications;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OptimizelySDK
{
    public class Optimizely
    {
        private Bucketer Bucketer;

        private EventBuilder EventBuilder;

        private IEventDispatcher EventDispatcher;
        
        private ProjectConfig Config;

        private ILogger Logger;

        private IErrorHandler ErrorHandler;

        private UserProfileService UserProfileService;

        private DecisionService DecisionService;

        private NotificationCenter NotificationCenter;

        public bool IsValid { get; private set; }

        public static String SDK_VERSION {
            get {
                // Example output: "1.2.1" .  Should be kept in synch with NuGet package version.
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
            EventBuilder = new EventBuilder(Bucketer);
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
                Logger.Log(LogLevel.ERROR, "Provided 'datafile' is in an invalid format. " + ex.Message);
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
        /// <param name="attributes">associative array of Attributes for the user</param>
        /// <returns>null|string Representing variation</returns>
        public string Activate(string experimentKey, string userId, UserAttributes userAttributes = null)
        {
            if (!IsValid)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'activate'.");
                return null;
            }

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

            if (userAttributes != null) {
                userAttributes = userAttributes.FilterNullValues(Logger);
            }

            SendImpressionEvent(experiment, variation, userId, userAttributes);

            return variation.Key;
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

                if (userAttributes != null)
                {
                    userAttributes = userAttributes.FilterNullValues(Logger);
                }

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
        /// <returns>null|string Representing variation</returns>
        public string GetVariation(string experimentKey, string userId, UserAttributes userAttributes = null)
        {
            if (!IsValid)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'GetVariation'.");
                return null;
            }

            Experiment experiment = Config.GetExperimentFromKey(experimentKey);
            if (experiment.Key == null)
                return null;

            Variation variation = DecisionService.GetVariation(experiment, userId, userAttributes);
            return variation == null ? null : variation.Key;
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
            return Config.SetForcedVariation(experimentKey, userId, variationKey);
        }

        /// <summary>
        /// Gets the forced variation key for the given user and experiment.  
        /// </summary>
        /// <param name="experimentKey">The experiment key</param>
        /// <param name="userId">The user ID</param>
        /// <returns>null|string The variation key.</returns>
        public Variation GetForcedVariation(string experimentKey, string userId)
        {
            var forcedVariation = Config.GetForcedVariation(experimentKey, userId);

            return forcedVariation;
        }

        #region  FeatureFlag APIs

        /// <summary>
        /// Determine whether a feature is enabled.
        /// Send an impression event if the user is bucketed into an experiment using the feature.
        /// </summary>
        /// <param name="experimentKey">The experiment key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes.</param>
        /// <returns>True if feature is enabled, false or null otherwise</returns>
        public bool? IsFeatureEnabled(string featureKey, string userId, UserAttributes userAttributes = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Logger.Log(LogLevel.ERROR, "User ID must not be empty.");
                return null;
            }

            if (string.IsNullOrEmpty(featureKey))
            {
                Logger.Log(LogLevel.ERROR, "Feature flag key must not be empty.");
                return null;
            }

            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            if (string.IsNullOrEmpty(featureFlag.Key))
                return null;

            if (!Validator.IsFeatureFlagValid(Config, featureFlag))
                return false;

            var variation = DecisionService.GetVariationForFeature(featureFlag, userId, userAttributes);
            if ( variation == null )
            {
                Logger.Log(LogLevel.INFO, $@"Feature flag ""{featureKey}"" is not enabled for user ""{userId}"".");
                return false;
            }

            var experiment = Config.GetExperimentForVariationId(variation.Id);

            if (!string.IsNullOrEmpty(experiment.Key))
            {
                SendImpressionEvent(experiment, variation, userId, userAttributes);
            }
            else
            {
                var audiences = new Audience[1];
                var rolloutRule = Config.GetRolloutRuleForVariationId(variation.Id);

                if (!string.IsNullOrEmpty(rolloutRule.Key)
                    && rolloutRule.AudienceIds != null
                    && rolloutRule.AudienceIds.Length > 0)
                {
                    audiences[0] = Config.GetAudience(rolloutRule.AudienceIds[0]);
                }

                Logger.Log(LogLevel.INFO, $@"The user ""{userId}"" is not being experimented on feature ""{featureKey}"".");
            }

            Logger.Log(LogLevel.INFO, $@"Feature flag ""{featureKey}"" is enabled for user ""{userId}"".");
            return true;
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
            if (string.IsNullOrEmpty(featureKey))
            {
                Logger.Log(LogLevel.ERROR, "Feature flag key must not be empty.");
                return null;
            }

            if (string.IsNullOrEmpty(variableKey))
            {
                Logger.Log(LogLevel.ERROR, "Variable key must not be empty.");
                return null;
            }

            if (string.IsNullOrEmpty(userId))
            {
                Logger.Log(LogLevel.ERROR, "User ID must not be empty.");
                return null;
            }

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
            var variation = DecisionService.GetVariationForFeature(featureFlag, userId, userAttributes);

            if (variation != null)
            {
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
        public bool? GetFeatureVariableBoolean(string featureKey, string variableKey, string userId, UserAttributes userAttributes)
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
        public double? GetFeatureVariableDouble(string featureKey, string variableKey, string userId, UserAttributes userAttributes)
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
        public int? GetFeatureVariableInteger(string featureKey, string variableKey, string userId, UserAttributes userAttributes)
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
        public string GetFeatureVariableString(string featureKey, string variableKey, string userId, UserAttributes userAttributes)
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

        #endregion // FeatureFlag APIs
    }
}
