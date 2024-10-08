/*
 * Copyright 2017-2024, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#if !(NET35 || NET40 || NETSTANDARD1_6)
#define USE_ODP
#endif

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
using OptimizelySDK.OptimizelyDecisions;
using System.Linq;

#if USE_ODP
using OptimizelySDK.Odp;
#endif

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

        [Obsolete]
        private EventBuilder EventBuilder;

        private IEventDispatcher EventDispatcher;

        private ILogger Logger;

        private IErrorHandler ErrorHandler;

        private UserProfileService UserProfileService;

        private DecisionService DecisionService;

        public NotificationCenter NotificationCenter;

        public ProjectConfigManager ProjectConfigManager;

        private EventProcessor EventProcessor;

        private OptimizelyDecideOption[] DefaultDecideOptions;

#if USE_ODP
        private IOdpManager OdpManager;
#endif

        /// <summary>
        /// It returns true if the ProjectConfig is valid otherwise false.
        /// Also, it may block execution if GetConfig() blocks execution to get ProjectConfig.
        /// </summary>
        public bool IsValid => ProjectConfigManager?.GetConfig() != null;

        public static String SDK_VERSION
        {
            get
            {
                // Example output: "2.1.0" .  Should be kept in synch with NuGet package version.
#if NET35 || NET40
                var assembly = Assembly.GetExecutingAssembly();
#else
                var assembly = typeof(Optimizely).GetTypeInfo().Assembly;
#endif
                // Microsoft    Major.Minor.Build.Revision
                // Semantic     Major.Minor.Patch
                var version = assembly.GetName().Version;
                var answer = String.Format("{0}.{1}.{2}", version.Major, version.Minor,
                    version.Build);
                return answer;
            }
        }

        public static String SDK_TYPE => "csharp-sdk";

        public const string USER_ID = "User Id";
        public const string EXPERIMENT_KEY = "Experiment Key";
        public const string EVENT_KEY = "Event Key";
        public const string FEATURE_KEY = "Feature Key";
        public const string VARIABLE_KEY = "Variable Key";
        private const string SOURCE_TYPE_EXPERIMENT = "experiment";

        public bool Disposed { get; private set; }

        /// <summary>
        /// Optimizely constructor for managing Optimizely Feature Experimentation .NET projects.
        /// </summary>
        /// <param name="datafile">string JSON string representing the project</param>
        /// <param name="eventDispatcher">EventDispatcherInterface</param>
        /// <param name="logger">LoggerInterface</param>
        /// <param name="errorHandler">ErrorHandlerInterface</param>
        /// <param name="userProfileService">User profile service</param>
        /// <param name="skipJsonValidation">boolean representing whether JSON schema validation needs to be performed</param>
        /// <param name="eventProcessor">EventProcessor</param>
        /// <param name="defaultDecideOptions">Default Decide options</param>
        /// <param name="odpManager">Optional ODP Manager</param>
        public Optimizely(string datafile,
            IEventDispatcher eventDispatcher = null,
            ILogger logger = null,
            IErrorHandler errorHandler = null,
            UserProfileService userProfileService = null,
            bool skipJsonValidation = false,
            EventProcessor eventProcessor = null,
            OptimizelyDecideOption[] defaultDecideOptions = null
#if USE_ODP
            , IOdpManager odpManager = null
#endif
        )
        {
            try
            {
#if USE_ODP
                InitializeComponents(eventDispatcher, logger, errorHandler, userProfileService,
                    null, eventProcessor, defaultDecideOptions, odpManager);
#else
                InitializeComponents(eventDispatcher, logger, errorHandler, userProfileService,
                    null, eventProcessor, defaultDecideOptions);
#endif

                if (ValidateInputs(datafile, skipJsonValidation))
                {
                    var config = DatafileProjectConfig.Create(datafile, Logger, ErrorHandler);
                    ProjectConfigManager = new FallbackProjectConfigManager(config);
#if USE_ODP
                    // No need to setup notification for datafile updates. This constructor
                    // is for hardcoded datafile which should not be changed using this method.
                    OdpManager?.UpdateSettings(config.PublicKeyForOdp, config.HostForOdp,
                        config.Segments.ToList());
#endif
                }
                else
                {
                    Logger.Log(LogLevel.ERROR, "Provided 'datafile' has invalid schema.");
                }
            }
            catch (Exception ex)
            {
                var error = String.Empty;
                if (ex.GetType() == typeof(ConfigParseException))
                {
                    error = ex.Message;
                }
                else
                {
                    error = "Provided 'datafile' is in an invalid format. " + ex.Message;
                }

                Logger.Log(LogLevel.ERROR, error);
                ErrorHandler.HandleError(ex);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:OptimizelySDK.Optimizely"/> class.
        /// </summary>
        /// <param name="configManager">Config manager.</param>
        /// <param name="notificationCenter">Notification center</param>
        /// <param name="eventDispatcher">Event dispatcher.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="errorHandler">Error handler.</param>
        /// <param name="userProfileService">User profile service.</param>
        /// <param name="eventProcessor">EventProcessor</param>
        /// <param name="defaultDecideOptions">Default Decide options</param>
        /// <param name="odpManager">Optional ODP Manager</param>
        public Optimizely(ProjectConfigManager configManager,
            NotificationCenter notificationCenter = null,
            IEventDispatcher eventDispatcher = null,
            ILogger logger = null,
            IErrorHandler errorHandler = null,
            UserProfileService userProfileService = null,
            EventProcessor eventProcessor = null,
            OptimizelyDecideOption[] defaultDecideOptions = null
#if USE_ODP
            , IOdpManager odpManager = null
#endif
        )
        {
            ProjectConfigManager = configManager;

#if USE_ODP
            InitializeComponents(eventDispatcher, logger, errorHandler, userProfileService,
                notificationCenter, eventProcessor, defaultDecideOptions, odpManager);

            var projectConfig = ProjectConfigManager.CachedProjectConfig;

            if (ProjectConfigManager.CachedProjectConfig != null)
            {
                // in case Project config is instantly available
                OdpManager?.UpdateSettings(projectConfig.PublicKeyForOdp, projectConfig.HostForOdp,
                    projectConfig.Segments.ToList());
            }

            if (ProjectConfigManager.SdkKey != null)
            {
                NotificationCenterRegistry.GetNotificationCenter(configManager.SdkKey, logger)
                    ?.AddNotification(NotificationCenter.NotificationType.OptimizelyConfigUpdate,
                        () =>
                        {
                            projectConfig = ProjectConfigManager.CachedProjectConfig;

                            OdpManager?.UpdateSettings(projectConfig.PublicKeyForOdp,
                                projectConfig.HostForOdp,
                                projectConfig.Segments.ToList());
                        });
            }

#else
            InitializeComponents(eventDispatcher, logger, errorHandler, userProfileService,
                notificationCenter, eventProcessor, defaultDecideOptions);
#endif
        }

        [Obsolete]
        private void InitializeComponents(IEventDispatcher eventDispatcher = null,
            ILogger logger = null,
            IErrorHandler errorHandler = null,
            UserProfileService userProfileService = null,
            NotificationCenter notificationCenter = null,
            EventProcessor eventProcessor = null,
            OptimizelyDecideOption[] defaultDecideOptions = null
#if USE_ODP
            , IOdpManager odpManager = null
#endif
        )
        {
            Logger = logger ?? new NoOpLogger();
            EventDispatcher = eventDispatcher ?? new DefaultEventDispatcher(Logger);
            ErrorHandler = errorHandler ?? new NoOpErrorHandler();
            Bucketer = new Bucketer(Logger);
            EventBuilder = new EventBuilder(Bucketer, Logger);
            UserProfileService = userProfileService;
            NotificationCenter = notificationCenter ?? new NotificationCenter(Logger);
            DecisionService =
                new DecisionService(Bucketer, ErrorHandler, userProfileService, Logger);
            EventProcessor = eventProcessor ?? new ForwardingEventProcessor(EventDispatcher,
                NotificationCenter,
                Logger);
            DefaultDecideOptions = defaultDecideOptions ?? new OptimizelyDecideOption[] { };
#if USE_ODP
            OdpManager = odpManager ?? new OdpManager.Builder()
                .WithErrorHandler(errorHandler)
                .WithLogger(logger)
                .Build();
#endif
        }

        /// <summary>
        /// Buckets visitor and sends impression event to Optimizely.
        /// </summary>
        /// <param name="experimentKey">experimentKey string Key identifying the experiment</param>
        /// <param name="userId">string ID for user</param>
        /// <param name="userAttributes">associative array of Attributes for the user</param>
        /// <returns>null|Variation Representing variation</returns>
        public Variation Activate(string experimentKey, string userId,
            UserAttributes userAttributes = null
        )
        {
            var config = ProjectConfigManager?.GetConfig();

            if (config == null)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'Activate'.");
                return null;
            }

            var inputValues = new Dictionary<string, string>
                { { USER_ID, userId }, { EXPERIMENT_KEY, experimentKey } };

            if (!ValidateStringInputs(inputValues))
            {
                return null;
            }

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

            SendImpressionEvent(experiment, variation, userId, userAttributes, config,
                SOURCE_TYPE_EXPERIMENT, true);

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
        public void Track(string eventKey, string userId, UserAttributes userAttributes = null,
            EventTags eventTags = null
        )
        {
            var config = ProjectConfigManager?.GetConfig();

            if (config == null)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'Track'.");
                return;
            }

            var inputValues = new Dictionary<string, string>
                { { USER_ID, userId }, { EVENT_KEY, eventKey } };

            if (!ValidateStringInputs(inputValues))
            {
                return;
            }

            var eevent = config.GetEvent(eventKey);

            if (eevent.Key == null)
            {
                Logger.Log(LogLevel.INFO,
                    string.Format("Not tracking user {0} for event {1}.", userId, eventKey));
                return;
            }

            if (eventTags != null)
            {
                eventTags = eventTags.FilterNullValues(Logger);
            }

            var userEvent = UserEventFactory.CreateConversionEvent(config, eventKey, userId,
                userAttributes, eventTags);
            EventProcessor.Process(userEvent);
            Logger.Log(LogLevel.INFO,
                string.Format("Tracking event {0} for user {1}.", eventKey, userId));

            if (NotificationCenter.GetNotificationCount(NotificationCenter.NotificationType.Track) >
                0)
            {
                var conversionEvent = EventFactory.CreateLogEvent(userEvent, Logger);
                NotificationCenter.SendNotifications(NotificationCenter.NotificationType.Track,
                    eventKey, userId,
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
        public Variation GetVariation(string experimentKey, string userId,
            UserAttributes userAttributes = null
        )
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
        private Variation GetVariation(string experimentKey, string userId, ProjectConfig config,
            UserAttributes userAttributes = null
        )
        {
            if (config == null)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'GetVariation'.");
                return null;
            }

            var inputValues = new Dictionary<string, string>
                { { USER_ID, userId }, { EXPERIMENT_KEY, experimentKey } };

            if (!ValidateStringInputs(inputValues))
            {
                return null;
            }

            var experiment = config.GetExperimentFromKey(experimentKey);
            if (experiment.Key == null)
            {
                return null;
            }

            userAttributes = userAttributes ?? new UserAttributes();

            var userContext = CreateUserContextCopy(userId, userAttributes);
            var variation = DecisionService.GetVariation(experiment, userContext, config)
                ?.ResultObject;
            var decisionInfo = new Dictionary<string, object>
            {
                { "experimentKey", experimentKey }, { "variationKey", variation?.Key },
            };

            var decisionNotificationType = config.IsFeatureExperiment(experiment.Id) ?
                DecisionNotificationTypes.FEATURE_TEST :
                DecisionNotificationTypes.AB_TEST;
            NotificationCenter.SendNotifications(NotificationCenter.NotificationType.Decision,
                decisionNotificationType, userId,
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
                { { USER_ID, userId }, { EXPERIMENT_KEY, experimentKey } };
            return ValidateStringInputs(inputValues) &&
                   DecisionService.SetForcedVariation(experimentKey, userId, variationKey, config);
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
                { { USER_ID, userId }, { EXPERIMENT_KEY, experimentKey } };

            if (!ValidateStringInputs(inputValues))
            {
                return null;
            }

            return DecisionService.GetForcedVariation(experimentKey, userId, config).ResultObject;
        }

        #region FeatureFlag APIs

        /// <summary>
        /// Determine whether a feature is enabled.
        /// Send an impression event if the user is bucketed into an experiment using the feature.
        /// </summary>
        /// <param name="featureKey">The feature key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes.</param>
        /// <returns>True if feature is enabled, otherwise false</returns>
        public virtual bool IsFeatureEnabled(string featureKey, string userId,
            UserAttributes userAttributes = null
        )
        {
            var config = ProjectConfigManager?.GetConfig();

            if (config == null)
            {
                Logger.Log(LogLevel.ERROR,
                    "Datafile has invalid format. Failing 'IsFeatureEnabled'.");

                return false;
            }

            var inputValues = new Dictionary<string, string>
                { { USER_ID, userId }, { FEATURE_KEY, featureKey } };

            if (!ValidateStringInputs(inputValues))
            {
                return false;
            }

            var featureFlag = config.GetFeatureFlagFromKey(featureKey);
            if (string.IsNullOrEmpty(featureFlag.Key))
            {
                return false;
            }

            if (!Validator.IsFeatureFlagValid(config, featureFlag))
            {
                return false;
            }

            var featureEnabled = false;
            var sourceInfo = new Dictionary<string, string>();
            var decision = DecisionService.GetVariationForFeature(featureFlag,
                    CreateUserContextCopy(userId, userAttributes),
                    config)
                .ResultObject;
            var variation = decision?.Variation;
            var decisionSource = decision?.Source ?? FeatureDecision.DECISION_SOURCE_ROLLOUT;

            if (variation != null)
            {
                featureEnabled = variation.FeatureEnabled.GetValueOrDefault();

                // This information is only necessary for feature tests.
                // For rollouts experiments and variations are an implementation detail only.
                if (decision?.Source == FeatureDecision.DECISION_SOURCE_FEATURE_TEST)
                {
                    decisionSource = decision.Source;
                    sourceInfo["experimentKey"] = decision.Experiment.Key;
                    sourceInfo["variationKey"] = variation.Key;
                }
                else
                {
                    Logger.Log(LogLevel.INFO,
                        $@"The user ""{userId}"" is not being experimented on feature ""{featureKey
                        }"".");
                }
            }

            if (featureEnabled == true)
            {
                Logger.Log(LogLevel.INFO,
                    $@"Feature flag ""{featureKey}"" is enabled for user ""{userId}"".");
            }
            else
            {
                Logger.Log(LogLevel.INFO,
                    $@"Feature flag ""{featureKey}"" is not enabled for user ""{userId}"".");
            }

            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", featureEnabled },
                { "source", decisionSource },
                { "sourceInfo", sourceInfo },
            };

            SendImpressionEvent(decision?.Experiment, variation, userId, userAttributes, config,
                featureKey, decisionSource, featureEnabled);

            NotificationCenter.SendNotifications(NotificationCenter.NotificationType.Decision,
                DecisionNotificationTypes.FEATURE, userId,
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
        public virtual T GetFeatureVariableValueForType<T>(string featureKey, string variableKey,
            string userId,
            UserAttributes userAttributes, string variableType
        )
        {
            var config = ProjectConfigManager?.GetConfig();
            if (config == null)
            {
                Logger.Log(LogLevel.ERROR,
                    $@"Datafile has invalid format. Failing '{
                        FeatureVariable.GetFeatureVariableTypeName(variableType)}'.");
                return default;
            }

            var inputValues = new Dictionary<string, string>
            {
                { USER_ID, userId }, { FEATURE_KEY, featureKey }, { VARIABLE_KEY, variableKey },
            };

            if (!ValidateStringInputs(inputValues))
            {
                return default;
            }

            var featureFlag = config.GetFeatureFlagFromKey(featureKey);
            if (string.IsNullOrEmpty(featureFlag.Key))
            {
                return default;
            }

            var featureVariable = featureFlag.GetFeatureVariableFromKey(variableKey);
            if (featureVariable == null)
            {
                Logger.Log(LogLevel.ERROR,
                    $@"No feature variable was found for key ""{variableKey}"" in feature flag ""{
                        featureKey}"".");
                return default;
            }
            else if (featureVariable.Type != variableType)
            {
                Logger.Log(LogLevel.ERROR,
                    $@"Variable is of type ""{featureVariable.Type
                    }"", but you requested it as type ""{variableType}"".");
                return default;
            }

            var featureEnabled = false;
            var variableValue = featureVariable.DefaultValue;
            var decision = DecisionService.GetVariationForFeature(featureFlag,
                    CreateUserContextCopy(userId, userAttributes),
                    config)
                .ResultObject;

            if (decision?.Variation != null)
            {
                var variation = decision.Variation;
                featureEnabled = variation.FeatureEnabled.GetValueOrDefault();
                var featureVariableUsageInstance =
                    variation.GetFeatureVariableUsageFromId(featureVariable.Id);

                if (featureVariableUsageInstance != null)
                {
                    if (variation.FeatureEnabled == true)
                    {
                        variableValue = featureVariableUsageInstance.Value;
                        Logger.Log(LogLevel.INFO,
                            $@"Got variable value ""{variableValue}"" for variable ""{variableKey
                            }"" of feature flag ""{featureKey}"".");
                    }
                    else
                    {
                        Logger.Log(LogLevel.INFO,
                            $@"Feature ""{featureKey}"" is not enabled for user {userId
                            }. Returning the default variable value ""{variableValue}"".");
                    }
                }
                else
                {
                    Logger.Log(LogLevel.INFO,
                        $@"Variable ""{variableKey}"" is not used in variation ""{variation.Key
                        }"", returning default value ""{variableValue}"".");
                }
            }
            else
            {
                Logger.Log(LogLevel.INFO,
                    $@"User ""{userId}"" is not in any variation for feature flag ""{featureKey
                    }"", returning default value ""{variableValue}"".");
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
                {
                    "variableValue",
                    typeCastedValue is OptimizelyJSON ?
                        ((OptimizelyJSON)typeCastedValue).ToDictionary() :
                        typeCastedValue
                },
                { "variableType", variableType.ToString().ToLower() },
                { "source", decision?.Source },
                { "sourceInfo", sourceInfo },
            };

            NotificationCenter.SendNotifications(NotificationCenter.NotificationType.Decision,
                DecisionNotificationTypes.FEATURE_VARIABLE, userId,
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
        public bool? GetFeatureVariableBoolean(string featureKey, string variableKey, string userId,
            UserAttributes userAttributes = null
        )
        {
            return GetFeatureVariableValueForType<bool?>(featureKey, variableKey, userId,
                userAttributes, FeatureVariable.BOOLEAN_TYPE);
        }

        /// <summary>
        /// Gets double feature variable value.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="variableKey">The variable key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>double | Feature variable value or null</returns>
        public double? GetFeatureVariableDouble(string featureKey, string variableKey,
            string userId, UserAttributes userAttributes = null
        )
        {
            return GetFeatureVariableValueForType<double?>(featureKey, variableKey, userId,
                userAttributes, FeatureVariable.DOUBLE_TYPE);
        }

        /// <summary>
        /// Gets integer feature variable value.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="variableKey">The variable key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>int | Feature variable value or null</returns>
        public int? GetFeatureVariableInteger(string featureKey, string variableKey, string userId,
            UserAttributes userAttributes = null
        )
        {
            return GetFeatureVariableValueForType<int?>(featureKey, variableKey, userId,
                userAttributes, FeatureVariable.INTEGER_TYPE);
        }

        /// <summary>
        /// Gets string feature variable value.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="variableKey">The variable key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>string | Feature variable value or null</returns>
        public string GetFeatureVariableString(string featureKey, string variableKey, string userId,
            UserAttributes userAttributes = null
        )
        {
            return GetFeatureVariableValueForType<string>(featureKey, variableKey, userId,
                userAttributes, FeatureVariable.STRING_TYPE);
        }

        /// <summary>
        /// Gets json sub type feature variable value.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="variableKey">The variable key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>OptimizelyJson | Feature variable value or null</returns>
        public OptimizelyJSON GetFeatureVariableJSON(string featureKey, string variableKey,
            string userId, UserAttributes userAttributes = null
        )
        {
            return GetFeatureVariableValueForType<OptimizelyJSON>(featureKey, variableKey, userId,
                userAttributes, FeatureVariable.JSON_TYPE);
        }

        /// <summary>
        /// Create a context of the user for which decision APIs will be called.
        /// A user context will be created successfully even when the SDK is not fully configured yet.
        /// </summary>
        /// <param name="userId">The user ID to be used for bucketing.</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>OptimizelyUserContext | An OptimizelyUserContext associated with this OptimizelyClient.</returns>
        public OptimizelyUserContext CreateUserContext(string userId,
            UserAttributes userAttributes = null
        )
        {
            var inputValues = new Dictionary<string, string> { { USER_ID, userId } };

            if (!ValidateStringInputs(inputValues))
            {
                return null;
            }

            return new OptimizelyUserContext(this, userId, userAttributes, ErrorHandler, Logger);
        }

        private OptimizelyUserContext CreateUserContextCopy(string userId,
            UserAttributes userAttributes = null
        )
        {
            var inputValues = new Dictionary<string, string> { { USER_ID, userId } };

            if (!ValidateStringInputs(inputValues))
            {
                return null;
            }

            return new OptimizelyUserContext(this, userId, userAttributes, null, null, ErrorHandler,
                Logger
#if USE_ODP
                , false
#endif
            );
        }

        /// <summary>
        /// Returns a decision result ({@link OptimizelyDecision}) for a given flag key and a user context, which contains all data required to deliver the flag.
        /// <ul>
        /// <li>If the SDK finds an error, it’ll return a decision with <b>null</b> for <b>variationKey</b>. The decision will include an error message in <b>reasons</b>.
        /// </ul>
        /// </summary>
        /// <param name="key">A flag key for which a decision will be made.</param>
        /// <param name="options">A list of options for decision-making.</param>
        /// <returns>A decision result.</returns>
        internal OptimizelyDecision Decide(OptimizelyUserContext user,
            string key,
            OptimizelyDecideOption[] options
        )
        {
            var config = ProjectConfigManager?.GetConfig();

            if (config == null)
            {
                return OptimizelyDecision.NewErrorDecision(key, user, DecisionMessage.SDK_NOT_READY,
                    ErrorHandler, Logger);
            }

            if (key == null)
            {
                return OptimizelyDecision.NewErrorDecision(key,
                    user,
                    DecisionMessage.Reason(DecisionMessage.FLAG_KEY_INVALID, key),
                    ErrorHandler, Logger);
            }

            var flag = config.GetFeatureFlagFromKey(key);
            if (flag.Key == null)
            {
                return OptimizelyDecision.NewErrorDecision(key,
                    user,
                    DecisionMessage.Reason(DecisionMessage.FLAG_KEY_INVALID, key),
                    ErrorHandler, Logger);
            }

            var userId = user?.GetUserId();
            var userAttributes = user?.GetAttributes();
            var decisionEventDispatched = false;
            var allOptions = GetAllOptions(options);
            var decisionReasons = new DecisionReasons();
            FeatureDecision decision = null;

            var decisionContext = new OptimizelyDecisionContext(flag.Key);
            var forcedDecisionVariation =
                DecisionService.ValidatedForcedDecision(decisionContext, config, user);
            decisionReasons += forcedDecisionVariation.DecisionReasons;

            if (forcedDecisionVariation.ResultObject != null)
            {
                decision = new FeatureDecision(null, forcedDecisionVariation.ResultObject,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST);
            }
            else
            {
                var flagDecisionResult = DecisionService.GetVariationForFeature(
                    flag,
                    user,
                    config,
                    userAttributes,
                    allOptions
                );
                decisionReasons += flagDecisionResult.DecisionReasons;
                decision = flagDecisionResult.ResultObject;
            }

            var featureEnabled = false;

            if (decision?.Variation != null)
            {
                featureEnabled = decision.Variation.FeatureEnabled.GetValueOrDefault();
            }

            if (featureEnabled)
            {
                Logger.Log(LogLevel.INFO,
                    "Feature \"" + key + "\" is enabled for user \"" + userId + "\"");
            }
            else
            {
                Logger.Log(LogLevel.INFO,
                    "Feature \"" + key + "\" is not enabled for user \"" + userId + "\"");
            }

            var variableMap = new Dictionary<string, object>();
            if (flag?.Variables != null &&
                !allOptions.Contains(OptimizelyDecideOption.EXCLUDE_VARIABLES))
            {
                foreach (var featureVariable in flag?.Variables)
                {
                    var variableValue = featureVariable.DefaultValue;
                    if (featureEnabled)
                    {
                        var featureVariableUsageInstance =
                            decision?.Variation.GetFeatureVariableUsageFromId(featureVariable.Id);
                        if (featureVariableUsageInstance != null)
                        {
                            variableValue = featureVariableUsageInstance.Value;
                        }
                    }

                    var typeCastedValue =
                        GetTypeCastedVariableValue(variableValue, featureVariable.Type);

                    if (typeCastedValue is OptimizelyJSON)
                    {
                        typeCastedValue = ((OptimizelyJSON)typeCastedValue).ToDictionary();
                    }

                    variableMap.Add(featureVariable.Key, typeCastedValue);
                }
            }

            var optimizelyJSON = new OptimizelyJSON(variableMap, ErrorHandler, Logger);

            var decisionSource = decision?.Source ?? FeatureDecision.DECISION_SOURCE_ROLLOUT;
            if (!allOptions.Contains(OptimizelyDecideOption.DISABLE_DECISION_EVENT))
            {
                decisionEventDispatched = SendImpressionEvent(decision?.Experiment,
                    decision?.Variation, userId, userAttributes, config, key, decisionSource,
                    featureEnabled);
            }

            var reasonsToReport = decisionReasons
                .ToReport(allOptions.Contains(OptimizelyDecideOption.INCLUDE_REASONS))
                .ToArray();
            var variationKey = decision?.Variation?.Key;

            // TODO: add ruleKey values when available later. use a copy of experimentKey until then.
            var ruleKey = decision?.Experiment?.Key;

            var decisionInfo = new Dictionary<string, object>
            {
                { "flagKey", key },
                { "enabled", featureEnabled },
                { "variables", variableMap },
                { "variationKey", variationKey },
                { "ruleKey", ruleKey },
                { "reasons", reasonsToReport },
                { "decisionEventDispatched", decisionEventDispatched },
            };

            NotificationCenter.SendNotifications(NotificationCenter.NotificationType.Decision,
                DecisionNotificationTypes.FLAG, userId,
                userAttributes ?? new UserAttributes(), decisionInfo);

            return new OptimizelyDecision(
                variationKey,
                featureEnabled,
                optimizelyJSON,
                ruleKey,
                key,
                user,
                reasonsToReport);
        }

        internal Dictionary<string, OptimizelyDecision> DecideAll(OptimizelyUserContext user,
            OptimizelyDecideOption[] options
        )
        {
            var decisionMap = new Dictionary<string, OptimizelyDecision>();

            var projectConfig = ProjectConfigManager?.GetConfig();
            if (projectConfig == null)
            {
                Logger.Log(LogLevel.ERROR,
                    "Optimizely instance is not valid, failing isFeatureEnabled call.");
                return decisionMap;
            }

            var allFlags = projectConfig.FeatureFlags;
            var allFlagKeys = allFlags.Select(v => v.Key).ToArray<string>();

            return DecideForKeys(user, allFlagKeys, options);
        }

        internal Dictionary<string, OptimizelyDecision> DecideForKeys(OptimizelyUserContext user,
            string[] keys,
            OptimizelyDecideOption[] options
        )
        {
            var decisionDictionary = new Dictionary<string, OptimizelyDecision>();

            var projectConfig = ProjectConfigManager?.GetConfig();
            if (projectConfig == null)
            {
                Logger.Log(LogLevel.ERROR,
                    "Optimizely instance is not valid, failing isFeatureEnabled call.");
                return decisionDictionary;
            }

            if (keys.Length == 0)
            {
                return decisionDictionary;
            }

            var allOptions = GetAllOptions(options);

            DecisionService.DecisionBatchInProgress = true;
            foreach (var key in keys)
            {
                var decision = Decide(user, key, options);
                if (!allOptions.Contains(OptimizelyDecideOption.ENABLED_FLAGS_ONLY) ||
                    decision.Enabled)
                {
                    decisionDictionary.Add(key, decision);
                }
            }

            DecisionService.DecisionBatchInProgress = false;

            return decisionDictionary;
        }

        private OptimizelyDecideOption[] GetAllOptions(OptimizelyDecideOption[] options)
        {
            var copiedOptions = DefaultDecideOptions;
            if (options != null)
            {
                copiedOptions = options.Union(DefaultDecideOptions).ToArray();
            }

            return copiedOptions;
        }

        /// <summary>
        /// Sends impression event.
        /// </summary>
        /// <param name="experiment">The experiment</param>
        /// <param name="variation">The variation entity</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <param name="ruleType">It can either be experiment in case impression event is sent from activate or it's feature-test or rollout</param>
        private void SendImpressionEvent(Experiment experiment, Variation variation, string userId,
            UserAttributes userAttributes, ProjectConfig config,
            string ruleType, bool enabled
        )
        {
            SendImpressionEvent(experiment, variation, userId, userAttributes, config, "", ruleType,
                enabled);
        }

        /// <summary>
        /// Sends impression event.
        /// </summary>
        /// <param name="experiment">The experiment</param>
        /// <param name="variation">The variation entity</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <param name="flagKey">It can either be experiment key in case if ruleType is experiment or it's feature key in case ruleType is feature-test or rollout</param>
        /// <param name="ruleType">It can either be experiment in case impression event is sent from activate or it's feature-test or rollout</param>
        private bool SendImpressionEvent(Experiment experiment, Variation variation, string userId,
            UserAttributes userAttributes, ProjectConfig config,
            string flagKey, string ruleType, bool enabled
        )
        {
            if (experiment != null && !experiment.IsExperimentRunning)
            {
                Logger.Log(LogLevel.ERROR,
                    @"Experiment has ""Launched"" status so not dispatching event during activation.");
            }

            var userEvent = UserEventFactory.CreateImpressionEvent(config, experiment, variation,
                userId, userAttributes, flagKey, ruleType, enabled);
            if (userEvent == null)
            {
                return false;
            }

            EventProcessor.Process(userEvent);

            if (experiment != null)
            {
                Logger.Log(LogLevel.INFO,
                    $"Activating user {userId} in experiment {experiment.Key}.");
            }

            // Kept For backwards compatibility.
            // This notification is deprecated and the new DecisionNotifications
            // are sent via their respective method calls.
            if (NotificationCenter.GetNotificationCount(
                    NotificationCenter.NotificationType.Activate) > 0)
            {
                var impressionEvent = EventFactory.CreateLogEvent(userEvent, Logger);
                NotificationCenter.SendNotifications(NotificationCenter.NotificationType.Activate,
                    experiment, userId,
                    userAttributes, variation, impressionEvent);
            }

            return true;
        }

        /// <summary>
        /// Get the list of features that are enabled for the user.
        /// </summary>
        /// <param name="userId">The user Id</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>List of the feature keys that are enabled for the user.</returns>
        public List<string> GetEnabledFeatures(string userId, UserAttributes userAttributes = null)
        {
            var enabledFeaturesList = new List<string>();

            var config = ProjectConfigManager?.GetConfig();

            if (config == null)
            {
                Logger.Log(LogLevel.ERROR,
                    "Datafile has invalid format. Failing 'GetEnabledFeatures'.");
                return enabledFeaturesList;
            }

            if (!ValidateStringInputs(new Dictionary<string, string> { { USER_ID, userId } }))
            {
                return enabledFeaturesList;
            }

            foreach (var feature in config.FeatureKeyMap.Values)
            {
                var featureKey = feature.Key;
                if (IsFeatureEnabled(featureKey, userId, userAttributes))
                {
                    enabledFeaturesList.Add(featureKey);
                }
            }

            return enabledFeaturesList;
        }

        /// <summary>
        /// Get the values of all variables in the feature.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>string | null An OptimizelyJSON instance for all variable values.</returns>
        public OptimizelyJSON GetAllFeatureVariables(string featureKey, string userId,
            UserAttributes userAttributes = null
        )
        {
            var config = ProjectConfigManager?.GetConfig();
            if (config == null)
            {
                Logger.Log(LogLevel.ERROR,
                    "Optimizely instance is not valid, failing getAllFeatureVariableValues call. type");
                return null;
            }

            if (featureKey == null)
            {
                Logger.Log(LogLevel.WARN, "The featureKey parameter must be nonnull.");
                return null;
            }
            else if (userId == null)
            {
                Logger.Log(LogLevel.WARN, "The userId parameter must be nonnull.");
                return null;
            }

            var featureFlag = config.GetFeatureFlagFromKey(featureKey);
            if (string.IsNullOrEmpty(featureFlag.Key))
            {
                Logger.Log(LogLevel.INFO,
                    "No feature flag was found for key \"" + featureKey + "\".");
                return null;
            }

            if (!Validator.IsFeatureFlagValid(config, featureFlag))
            {
                return null;
            }

            var featureEnabled = false;
            var decisionResult = DecisionService.GetVariationForFeature(featureFlag,
                CreateUserContextCopy(userId, userAttributes), config);
            var variation = decisionResult.ResultObject?.Variation;

            if (variation != null)
            {
                featureEnabled = variation.FeatureEnabled.GetValueOrDefault();
            }
            else
            {
                Logger.Log(LogLevel.INFO, "User \"" + userId +
                                          "\" was not bucketed into any variation for feature flag \"" +
                                          featureKey + "\". " +
                                          "The default values are being returned.");
            }

            if (featureEnabled)
            {
                Logger.Log(LogLevel.INFO,
                    "Feature \"" + featureKey + "\" is enabled for user \"" + userId + "\"");
            }
            else
            {
                Logger.Log(LogLevel.INFO,
                    "Feature \"" + featureKey + "\" is not enabled for user \"" + userId + "\"");
            }

            var valuesMap = new Dictionary<string, object>();
            foreach (var featureVariable in featureFlag.Variables)
            {
                var variableValue = featureVariable.DefaultValue;
                if (featureEnabled)
                {
                    var featureVariableUsageInstance =
                        variation.GetFeatureVariableUsageFromId(featureVariable.Id);
                    if (featureVariableUsageInstance != null)
                    {
                        variableValue = featureVariableUsageInstance.Value;
                    }
                }

                var typeCastedValue =
                    GetTypeCastedVariableValue(variableValue, featureVariable.Type);

                if (typeCastedValue is OptimizelyJSON)
                {
                    typeCastedValue = ((OptimizelyJSON)typeCastedValue).ToDictionary();
                }

                valuesMap.Add(featureVariable.Key, typeCastedValue);
            }

            var sourceInfo = new Dictionary<string, string>();
            if (decisionResult.ResultObject?.Source == FeatureDecision.DECISION_SOURCE_FEATURE_TEST)
            {
                sourceInfo["experimentKey"] = decisionResult.ResultObject.Experiment.Key;
                sourceInfo["variationKey"] = decisionResult.ResultObject.Variation.Key;
            }

            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", featureEnabled },
                { "variableValues", valuesMap },
                { "source", decisionResult.ResultObject?.Source },
                { "sourceInfo", sourceInfo },
            };

            NotificationCenter.SendNotifications(NotificationCenter.NotificationType.Decision,
                DecisionNotificationTypes.ALL_FEATURE_VARIABLE, userId,
                userAttributes ?? new UserAttributes(), decisionInfo);

            return new OptimizelyJSON(valuesMap, ErrorHandler, Logger);
        }

        /// <summary>
        /// Get OptimizelyConfig containing experiments and features map
        /// </summary>
        /// <returns>OptimizelyConfig Object</returns>
        public OptimizelyConfig GetOptimizelyConfig()
        {
            var config = ProjectConfigManager?.GetConfig();

            if (config == null)
            {
                Logger.Log(LogLevel.ERROR,
                    "Datafile has invalid format. Failing 'GetOptimizelyConfig'.");
                return null;
            }

            // PollingProjectConfigManager now also implements IOptimizelyConfigManager interface to support OptimizelyConfigService API.
            // This check is needed in case a consumer provides their own ProjectConfigManager which does not implement IOptimizelyConfigManager interface
            if (ProjectConfigManager is IOptimizelyConfigManager)
            {
                return ((IOptimizelyConfigManager)ProjectConfigManager).GetOptimizelyConfig();
            }

            Logger.Log(LogLevel.DEBUG,
                "ProjectConfigManager is not instance of IOptimizelyConfigManager, generating new OptimizelyConfigObject as a fallback");

            return new OptimizelyConfigService(config).GetOptimizelyConfig();
        }

        #endregion FeatureFlag APIs

#if USE_ODP
        /// <summary>
        /// Attempts to fetch and return a list of a user's qualified segments.
        /// </summary>
        /// <param name="userId">FS User ID</param>
        /// <param name="segmentOptions">Options used during segment cache handling</param>
        /// <returns>Qualified segments for the user from the cache or the ODP server</returns>
        internal string[] FetchQualifiedSegments(string userId,
            List<OdpSegmentOption> segmentOptions
        )
        {
            var config = ProjectConfigManager?.GetConfig();

            if (config == null)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'FetchQualifiedSegments'.");
                return null;
            }

            return OdpManager?.FetchQualifiedSegments(userId, segmentOptions);
        }

        /// <summary>
        /// Send identification event to Optimizely Data Platform
        /// </summary>
        /// <param name="userId">FS User ID to send</param>
        internal void IdentifyUser(string userId)
        {
            var config = ProjectConfigManager?.GetConfig();

            if (config == null)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'IdentifyUser'.");
                return;
            }

            OdpManager?.IdentifyUser(userId);
        }

        /// <summary>
        /// Add event to queue for sending to ODP
        /// </summary>
        /// <param name="action">Subcategory of the event type</param>
        /// <param name="identifiers">Dictionary for identifiers. The caller must provide at least one key-value pair.</param>
        /// <param name="type">Type of event (defaults to `fullstack`)</param>
        /// <param name="data">Optional event data in a key-value pair format</param>
        public void SendOdpEvent(string action, Dictionary<string, string> identifiers, string type = Constants.ODP_EVENT_TYPE,
            Dictionary<string, object> data = null
        )
        {
            if (String.IsNullOrEmpty(action))
            {
                Logger.Log(LogLevel.ERROR, Constants.ODP_INVALID_ACTION_MESSAGE);
                return;
            }

            if (String.IsNullOrEmpty(type))
            {
                type = Constants.ODP_EVENT_TYPE;
            }

            var config = ProjectConfigManager?.GetConfig();

            if (config == null)
            {
                Logger.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'SendOdpEvent'.");
                return;
            }

            OdpManager?.SendEvent(type, action, identifiers, data);
        }
#endif

        /// <summary>
        /// Validate all string inputs are not null or empty.
        /// </summary>
        /// <param name="inputs">Array Hash input types and values</param>
        /// <returns>True if all values are valid, false otherwise</returns>
        private bool ValidateStringInputs(Dictionary<string, string> inputs)
        {
            var isValid = true;

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

        private object GetTypeCastedVariableValue(string value, string type)
        {
            object result = null;
            switch (type)
            {
                case FeatureVariable.BOOLEAN_TYPE:
                    bool.TryParse(value, out var booleanValue);
                    result = booleanValue;
                    break;

                case FeatureVariable.DOUBLE_TYPE:
                    double.TryParse(value, System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out var doubleValue);
                    result = doubleValue;
                    break;

                case FeatureVariable.INTEGER_TYPE:
                    int.TryParse(value, out var intValue);
                    result = intValue;
                    break;

                case FeatureVariable.STRING_TYPE:
                    result = value;
                    break;

                case FeatureVariable.JSON_TYPE:
                    result = new OptimizelyJSON(value, ErrorHandler, Logger);
                    break;
            }

            if (result == null)
            {
                Logger.Log(LogLevel.ERROR,
                    $@"Unable to cast variable value ""{value}"" to type ""{type}"".");
            }

            return result;
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Disposed = true;

            if (ProjectConfigManager is IDisposable)
            {
#if USE_ODP
                NotificationCenterRegistry.RemoveNotificationCenter(ProjectConfigManager.SdkKey);
#endif

                (ProjectConfigManager as IDisposable)?.Dispose();
            }

            ProjectConfigManager = null;

            (EventProcessor as IDisposable)?.Dispose();

#if USE_ODP
            OdpManager?.Dispose();
#endif
        }
    }
}
