/* 
 * Copyright 2017-2019, Optimizely
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
using OptimizelySDK.Config;
using OptimizelySDK.Event;
using OptimizelySDK.OptlyConfig;

namespace OptimizelySDK
{
#if NET35
    [Obsolete("Net3.5 SDK support is deprecated, use NET4.0 or above")]
#elif NETSTANDARD1_6
    [Obsolete("Net standard 1.6 SDK support is deprecated, use Net standard 2.0 or above")]
#endif
    public class Optimizely : IOptimizely, IDisposable
    {
        private Bucketer Bucketer;

        private EventBuilder EventBuilder;

        private IEventDispatcher EventDispatcher;

        private ILogger Logger;

        private IErrorHandler ErrorHandler;

        private UserProfileService UserProfileService;

        private DecisionService DecisionService;

        public NotificationCenter NotificationCenter;

        public ProjectConfigManager ProjectConfigManager;

        private EventProcessor EventProcessor;

        /// <summary>
        /// It returns true if the ProjectConfig is valid otherwise false.
        /// Also, it may block execution if GetConfig() blocks execution to get ProjectConfig.
        /// </summary>
        public bool IsValid { 
            get {
                return ProjectConfigManager?.GetConfig() != null;
            }
        }

        public static String SDK_VERSION
        {
            get
            {
                // Example output: "2.1.0" .  Should be kept in synch with NuGet package version.
#if NET35 || NET40
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

        public static String SDK_TYPE
        {
            get
            {
                return "csharp-sdk";
            }
        }

        public const string USER_ID = "User Id";
        public const string EXPERIMENT_KEY = "Experiment Key";
        public const string EVENT_KEY = "Event Key";
        public const string FEATURE_KEY = "Feature Key";
        public const string VARIABLE_KEY = "Variable Key";
        public bool Disposed { get; private set; }

        /// <summary>
        /// Optimizely constructor for managing Full Stack .NET projects.
        /// </summary>
        /// <param name="datafile">string JSON string representing the project</param>
        /// <param name="eventDispatcher">EventDispatcherInterface</param>
        /// <param name="logger">LoggerInterface</param>
        /// <param name="errorHandler">ErrorHandlerInterface</param>
        /// <param name="skipJsonValidation">boolean representing whether JSON schema validation needs to be performed</param>
        /// <param name="eventProcessor">EventProcessor</param>
        public Optimizely(string datafile,
                          IEventDispatcher eventDispatcher = null,
                          ILogger logger = null,
                          IErrorHandler errorHandler = null,
                          UserProfileService userProfileService = null,
                          bool skipJsonValidation = false,
                          EventProcessor eventProcessor = null)
        {
            try {
                InitializeComponents(eventDispatcher, logger, errorHandler, userProfileService, null, eventProcessor);

                if (ValidateInputs(datafile, skipJsonValidation)) {
                    var config = DatafileProjectConfig.Create(datafile, Logger, ErrorHandler);
                    ProjectConfigManager = new FallbackProjectConfigManager(config);
                } else {
                    Logger.Log(LogLevel.ERROR, "Provided 'datafile' has invalid schema.");
                }

            } catch (Exception ex) {
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
        /// Initializes a new instance of the <see cref="T:OptimizelySDK.Optimizely"/> class.
        /// </summary>
        /// <param name="configManager">Config manager.</param>
        /// <param name="eventDispatcher">Event dispatcher.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="errorHandler">Error handler.</param>
        /// <param name="userProfileService">User profile service.</param>
        /// <param name="eventProcessor">EventProcessor</param>
        public Optimizely(ProjectConfigManager configManager,
                         NotificationCenter notificationCenter = null,
                         IEventDispatcher eventDispatcher = null,
                         ILogger logger = null,
                         IErrorHandler errorHandler = null,
                         UserProfileService userProfileService = null,
                         EventProcessor eventProcessor = null)
        {
            ProjectConfigManager = configManager;

            InitializeComponents(eventDispatcher, logger, errorHandler, userProfileService, notificationCenter, eventProcessor);
        }

        private void InitializeComponents(IEventDispatcher eventDispatcher = null,
                         ILogger logger = null,
                         IErrorHandler errorHandler = null,
                         UserProfileService userProfileService = null,
                         NotificationCenter notificationCenter = null,
                         EventProcessor eventProcessor = null)
        {
            Logger = logger ?? new NoOpLogger();
            EventDispatcher = eventDispatcher ?? new DefaultEventDispatcher(Logger);
            ErrorHandler = errorHandler ?? new NoOpErrorHandler();
            Bucketer = new Bucketer(Logger);
            EventBuilder = new EventBuilder(Bucketer, Logger);
            UserProfileService = userProfileService;
            NotificationCenter = notificationCenter ?? new NotificationCenter(Logger);
            DecisionService = new DecisionService(Bucketer, ErrorHandler, userProfileService, Logger);
            EventProcessor = eventProcessor ?? new ForwardingEventProcessor(EventDispatcher, NotificationCenter, Logger);
        }

        /// <summary>
        /// Helper function to validate all required conditions before performing activate or track.
        /// </summary>
        /// <param name="experiment">Experiment Object representing experiment</param>
        /// <param name="userId">string ID for user</param>
        /// <param name="userAttributes">associative array of Attributes for the user</param>
        private bool ValidatePreconditions(Experiment experiment, string userId, ProjectConfig config, UserAttributes userAttributes = null)
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

            if (!ExperimentUtils.IsUserInExperiment(config, experiment, userAttributes, Logger))
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
            var config = ProjectConfigManager?.GetConfig();

            if (config == null)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'Activate'.");
                return null;
            }

            var inputValues = new Dictionary<string, string>
            {
                { USER_ID, userId },
                { EXPERIMENT_KEY, experimentKey }
            };

            if (!ValidateStringInputs(inputValues))
                return null;

            var experiment = config.GetExperimentFromKey(experimentKey);

            if (experiment.Key == null)
            {
                Logger.Log(LogLevel.INFO, string.Format("Not activating user {0}.", userId));
                return null;
            }

            var variation = GetVariation(experimentKey, userId, config, userAttributes);

            if (variation == null || variation.Key == null)
            {
                Logger.Log(LogLevel.INFO, string.Format("Not activating user {0}.", userId));
                return null;
            }

            SendImpressionEvent(experiment, variation, userId, userAttributes, config);

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
            var config = ProjectConfigManager?.GetConfig();

            if (config == null)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'Track'.");
                return;
            }

            var inputValues = new Dictionary<string, string>
            {
                { USER_ID, userId },
                { EVENT_KEY, eventKey }
            };

            if (!ValidateStringInputs(inputValues))
                return;

            var eevent = config.GetEvent(eventKey);

            if (eevent.Key == null)
            {
                Logger.Log(LogLevel.INFO, string.Format("Not tracking user {0} for event {1}.", userId, eventKey));
                return;
            }


            if (eventTags != null)
            {
                eventTags = eventTags.FilterNullValues(Logger);
            }

            var userEvent = UserEventFactory.CreateConversionEvent(config, eventKey, userId, userAttributes, eventTags);
            EventProcessor.Process(userEvent);
            Logger.Log(LogLevel.INFO, string.Format("Tracking event {0} for user {1}.", eventKey, userId));

            if (NotificationCenter.GetNotificationCount(NotificationCenter.NotificationType.Track) > 0)
            {
                var conversionEvent = EventFactory.CreateLogEvent(userEvent, Logger);
                NotificationCenter.SendNotifications(NotificationCenter.NotificationType.Track, eventKey, userId,
                userAttributes, eventTags, conversionEvent);
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
            var config = ProjectConfigManager?.GetConfig();
            return GetVariation(experimentKey, userId, config, userAttributes);
        }

        /// <summary>
        /// Get variation where user will be bucketed from the given ProjectConfig.
        /// </summary>
        /// <param name="experimentKey">experimentKey string Key identifying the experiment</param>
        /// <param name="userId">ID for the user</param>
        /// <param name="config">ProjectConfig to be used for variation</param>
        /// <param name="userAttributes">Attributes for the users</param>
        /// <returns>null|Variation Representing variation</returns>
        private Variation GetVariation(string experimentKey, string userId, ProjectConfig config, UserAttributes userAttributes = null)
        {
            if (config == null)
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

            Experiment experiment = config.GetExperimentFromKey(experimentKey);
            if (experiment.Key == null)
                return null;

            var variation = DecisionService.GetVariation(experiment, userId, config, userAttributes);
            var decisionInfo = new Dictionary<string, object>
            {
                { "experimentKey", experimentKey },
                { "variationKey", variation?.Key },
            };

            userAttributes = userAttributes ?? new UserAttributes();
            var decisionNotificationType = config.IsFeatureExperiment(experiment.Id) ? DecisionNotificationTypes.FEATURE_TEST : DecisionNotificationTypes.AB_TEST;
            NotificationCenter.SendNotifications(NotificationCenter.NotificationType.Decision, decisionNotificationType, userId,
                userAttributes, decisionInfo);
            return variation;
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
            var config = ProjectConfigManager?.GetConfig();

            if (config == null)
            {
                return false;
            }

            var inputValues = new Dictionary<string, string>
            {
                { USER_ID, userId },
                { EXPERIMENT_KEY, experimentKey }
            };
            return ValidateStringInputs(inputValues) && DecisionService.SetForcedVariation(experimentKey, userId, variationKey, config);
        }

        /// <summary>
        /// Gets the forced variation key for the given user and experiment.  
        /// </summary>
        /// <param name="experimentKey">The experiment key</param>
        /// <param name="userId">The user ID</param>
        /// <returns>null|string The variation key.</returns>
        public Variation GetForcedVariation(string experimentKey, string userId)
        {
            var config = ProjectConfigManager?.GetConfig();

            if (config == null)
            {
                return null;
            }

            var inputValues = new Dictionary<string, string>
            {
                { USER_ID, userId },
                { EXPERIMENT_KEY, experimentKey }
            };

            if (!ValidateStringInputs(inputValues))
                return null;

            return DecisionService.GetForcedVariation(experimentKey, userId, config);
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
            var config = ProjectConfigManager?.GetConfig();

            if (config == null) {

                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'IsFeatureEnabled'.");

                return false;
            }

            var inputValues = new Dictionary<string, string>
            {
                { USER_ID, userId },
                { FEATURE_KEY, featureKey }
            };

            if (!ValidateStringInputs(inputValues))
                return false;

            var featureFlag = config.GetFeatureFlagFromKey(featureKey);
            if (string.IsNullOrEmpty(featureFlag.Key))
                return false;

            if (!Validator.IsFeatureFlagValid(config, featureFlag))
                return false;

            bool featureEnabled = false;
            var sourceInfo = new Dictionary<string, string>();
            var decision = DecisionService.GetVariationForFeature(featureFlag, userId, config, userAttributes);

            if (decision.Variation != null)
            {
                var variation = decision.Variation;
                featureEnabled = variation.FeatureEnabled.GetValueOrDefault();

                if (decision.Source == FeatureDecision.DECISION_SOURCE_FEATURE_TEST)
                {
                    sourceInfo["experimentKey"] = decision.Experiment.Key;
                    sourceInfo["variationKey"] = variation.Key;
                    SendImpressionEvent(decision.Experiment, variation, userId, userAttributes, config);
                }
                else
                {
                    Logger.Log(LogLevel.INFO, $@"The user ""{userId}"" is not being experimented on feature ""{featureKey}"".");
                }
            }

            if (featureEnabled == true)
                Logger.Log(LogLevel.INFO, $@"Feature flag ""{featureKey}"" is enabled for user ""{userId}"".");
            else
                Logger.Log(LogLevel.INFO, $@"Feature flag ""{featureKey}"" is not enabled for user ""{userId}"".");

            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", featureEnabled },
                { "source", decision.Source },
                { "sourceInfo", sourceInfo },
            };

            NotificationCenter.SendNotifications(NotificationCenter.NotificationType.Decision, DecisionNotificationTypes.FEATURE, userId,
               userAttributes ?? new UserAttributes(), decisionInfo);
            return featureEnabled;
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
        public virtual T GetFeatureVariableValueForType<T>(string featureKey, string variableKey, string userId, 
                                                                     UserAttributes userAttributes, FeatureVariable.VariableType variableType)
        {

            var config = ProjectConfigManager?.GetConfig();
            if (config == null) {

                Logger.Log(LogLevel.ERROR, $@"Datafile has invalid format. Failing '{FeatureVariable.GetFeatureVariableTypeName(variableType)}'.");
                return default(T);
            }
            
            var inputValues = new Dictionary<string, string>
            {
                { USER_ID, userId },
                { FEATURE_KEY, featureKey },
                { VARIABLE_KEY, variableKey }
            };

            if (!ValidateStringInputs(inputValues))
                return default(T);

            var featureFlag = config.GetFeatureFlagFromKey(featureKey);
            if (string.IsNullOrEmpty(featureFlag.Key))
                return default(T);

            var featureVariable = featureFlag.GetFeatureVariableFromKey(variableKey);
            if (featureVariable == null)
            {
                Logger.Log(LogLevel.ERROR,
                    $@"No feature variable was found for key ""{variableKey}"" in feature flag ""{featureKey}"".");
                return default(T);
            }
            else if (featureVariable.Type != variableType)
            {
                Logger.Log(LogLevel.ERROR,
                    $@"Variable is of type ""{featureVariable.Type}"", but you requested it as type ""{variableType}"".");
                return default(T);
            }

            var featureEnabled = false;
            var variableValue = featureVariable.DefaultValue;
            var decision = DecisionService.GetVariationForFeature(featureFlag, userId, config, userAttributes);

            if (decision.Variation != null)
            {
                var variation = decision.Variation;
                featureEnabled = variation.FeatureEnabled.GetValueOrDefault();
                var featureVariableUsageInstance = variation.GetFeatureVariableUsageFromId(featureVariable.Id);

                if (featureVariableUsageInstance != null)
                {
                    if (variation.FeatureEnabled == true)
                    {
                        variableValue = featureVariableUsageInstance.Value;
                        Logger.Log(LogLevel.INFO, $@"Returning variable value ""{variableValue}"" for variation ""{variation.Key}"" of feature flag ""{featureKey}"".");
                    }
                    else
                    {
                        Logger.Log(LogLevel.INFO, $@"Feature ""{featureKey}"" is not enabled for user {userId}. Returning default value for variable ""{variableKey}"".");
                    }
                }
                else
                {
                    Logger.Log(LogLevel.INFO, $@"Variable ""{variableKey}"" is not used in variation ""{variation.Key}"", returning default value ""{variableValue}"".");
                }
            }
            else
            {
                Logger.Log(LogLevel.INFO,
                    $@"User ""{userId}"" is not in any variation for feature flag ""{featureKey}"", returning default value ""{variableValue}"".");
            }

            var sourceInfo = new Dictionary<string, string>();
            if (decision?.Source == FeatureDecision.DECISION_SOURCE_FEATURE_TEST)
            {
                sourceInfo["experimentKey"] = decision.Experiment.Key;
                sourceInfo["variationKey"] = decision.Variation.Key;
            }

            var typeCastedValue = GetTypeCastedVariableValue(variableValue, variableType);
            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", featureEnabled },
                { "variableKey", variableKey },
                { "variableValue", typeCastedValue },
                { "variableType", variableType.ToString().ToLower() },
                { "source", decision?.Source },
                { "sourceInfo", sourceInfo },
            };

            NotificationCenter.SendNotifications(NotificationCenter.NotificationType.Decision, DecisionNotificationTypes.FEATURE_VARIABLE, userId,
               userAttributes ?? new UserAttributes(), decisionInfo);
            return (T)typeCastedValue;
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
            return GetFeatureVariableValueForType<bool?>(featureKey, variableKey, userId, userAttributes, FeatureVariable.VariableType.BOOLEAN);
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
            return GetFeatureVariableValueForType<double?>(featureKey, variableKey, userId, userAttributes, FeatureVariable.VariableType.DOUBLE);
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
            return GetFeatureVariableValueForType<int?>(featureKey, variableKey, userId, userAttributes, FeatureVariable.VariableType.INTEGER);
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
            return GetFeatureVariableValueForType<string>(featureKey, variableKey, userId, userAttributes, FeatureVariable.VariableType.STRING);
        }

        /// <summary>
        /// Sends impression event.
        /// </summary>
        /// <param name="experiment">The experiment</param>
        /// <param name="variation">The variation entity</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        private void SendImpressionEvent(Experiment experiment, Variation variation, string userId,
                                         UserAttributes userAttributes, ProjectConfig config)
        {
            if (experiment.IsExperimentRunning)
            {
                var userEvent = UserEventFactory.CreateImpressionEvent(config, experiment, variation.Id, userId, userAttributes);
                EventProcessor.Process(userEvent);
                Logger.Log(LogLevel.INFO, string.Format("Activating user {0} in experiment {1}.", userId, experiment.Key));

                // Kept For backwards compatibility.
                // This notification is deprecated and the new DecisionNotifications
                // are sent via their respective method calls.
                if (NotificationCenter.GetNotificationCount(NotificationCenter.NotificationType.Activate) > 0)
                {
                    var impressionEvent = EventFactory.CreateLogEvent(userEvent, Logger);
                    NotificationCenter.SendNotifications(NotificationCenter.NotificationType.Activate, experiment, userId,
                    userAttributes, variation, impressionEvent);
                }
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

            var config = ProjectConfigManager?.GetConfig();

            if (config == null)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'GetEnabledFeatures'.");
                return enabledFeaturesList;
            }

            if (!ValidateStringInputs(new Dictionary<string, string> { { USER_ID, userId } }))
                return enabledFeaturesList;

            foreach (var feature in config.FeatureKeyMap.Values)
            {
                var featureKey = feature.Key;
                if (IsFeatureEnabled(featureKey, userId, userAttributes))
                    enabledFeaturesList.Add(featureKey);
            }

            return enabledFeaturesList;
        }

        public OptimizelyConfig GetOptimizelyConfig()
        {
            var config = ProjectConfigManager?.GetConfig();
            if (config == null)
            {
                return null;
            }
            return OptimizelyConfigService.getInstance().GetOptimizelyConfig(config);
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

            foreach (var input in inputs)
            {
                if (string.IsNullOrEmpty(input.Value))
                {
                    Logger.Log(LogLevel.ERROR, $"Provided {input.Key} is in invalid format.");
                    isValid = false;
                }
            }

            return isValid;
        }

        private object GetTypeCastedVariableValue(string value, FeatureVariable.VariableType type)
        {
            object result = null;
            switch (type)
            {
                case FeatureVariable.VariableType.BOOLEAN:
                    bool.TryParse(value, out bool booleanValue);
                    result = booleanValue;
                    break;
                case FeatureVariable.VariableType.DOUBLE:
                    double.TryParse(value, out double doubleValue);
                    result = doubleValue;
                    break;
                case FeatureVariable.VariableType.INTEGER:
                    int.TryParse(value, out int intValue);
                    result = intValue;
                    break;
                case FeatureVariable.VariableType.STRING:
                    result = value;
                    break;
            }

            if (result == null)
                Logger.Log(LogLevel.ERROR, $@"Unable to cast variable value ""{value}"" to type ""{type}"".");

            return result;
        }

        public void Dispose()
        {
            if (Disposed) return;

            Disposed = true;

            (ProjectConfigManager as IDisposable)?.Dispose();
            (EventProcessor as IDisposable)?.Dispose();

            ProjectConfigManager = null;
        }
    }    
}
