using OptimizelySDK.Entity;
using OptimizelySDK.Utils;
using System;
using System.Collections.Generic;

namespace OptimizelySDK.Notifications
{
    public sealed class DecisionNotification
    {
        protected readonly string Type;
        protected readonly string UserId;
        protected readonly UserAttributes Attributes;
        protected readonly IDictionary<string, object> DecisionInfo;

        protected DecisionNotification()
        {
        }
        protected DecisionNotification(string type,
                                       string userId,
                                       UserAttributes attributes,
                                       IDictionary<string, object> decisionInfo)
        {
            Type = type;
            UserId = userId;
            if (attributes == null)
            {
                attributes = new UserAttributes();
            }
            Attributes = attributes;
            DecisionInfo = decisionInfo;
        }

        public string GetType() {
            return Type;
        }

        public string GetUserId()
        {
            return UserId;
        }

        public UserAttributes GetAttributes()
        {
            return Attributes;
        }

        public IDictionary<string, object> GetDecisionInfo()
        {
            return DecisionInfo;
        }

        public class ExperimentDecisionNotificationBuilder
        {
            public static readonly string EXPERIMENT_KEY = "experimentKey";
            public static readonly string VARIATION_KEY = "variationKey";
            private string Type;
            private string ExperimentKey;
            private Variation Variation;
            private string UserId;
            private UserAttributes Attributes;
            private Dictionary<string, object> DecisionInfo;

            public ExperimentDecisionNotificationBuilder WithUserId(string userId)
            {
                UserId = userId;

                return this;
            }

            public ExperimentDecisionNotificationBuilder WithType(string type)
            {
                Type = type;

                return this;
            }

            public ExperimentDecisionNotificationBuilder WithExperimentKey(string experimentKey)
            {
                ExperimentKey = experimentKey;

                return this;
            }

            public ExperimentDecisionNotificationBuilder WithVariation(Variation variation)
            {
                Variation = variation;

                return this;
            }

            public ExperimentDecisionNotificationBuilder WithAttributes(UserAttributes attributes)
            {
                Attributes = attributes;

                return this;
            }

            public DecisionNotification Build()
            {
                DecisionInfo = new Dictionary<string, object>();
                DecisionInfo.Add(EXPERIMENT_KEY, ExperimentKey);
                DecisionInfo.Add(VARIATION_KEY, Variation != null ? Variation.Key : null);
                return new DecisionNotification(
                    Type,
                    UserId,
                    Attributes,
                    DecisionInfo);
            }
        }


        public class FeatureDecisionNotificationBuilder
        {
            public static readonly string FEATURE_KEY = "featureKey";
            public static readonly string FEATURE_ENABLED = "featureEnabled";
            public static readonly string SOURCE = "source";
            public static readonly string SOURCE_INFO = "sourceInfo";

            private string FeatureKey;
            private bool FeatureEnabled;
            private SourceInfo SourceInfo;
            private string Source;
            private string UserId;
            private UserAttributes Attributes;
            private IDictionary<string, object> DecisionInfo;
            public FeatureDecisionNotificationBuilder withUserId(string userId)
            {
                UserId = userId;
                return this;
            }

            public FeatureDecisionNotificationBuilder withAttributes(UserAttributes attributes)
            {
                Attributes = attributes;
                return this;
            }

            public FeatureDecisionNotificationBuilder withSourceInfo(SourceInfo sourceInfo)
            {
                SourceInfo = sourceInfo;
                return this;
            }

            public FeatureDecisionNotificationBuilder withSource(string source)
            {
                Source = source;
                return this;
            }

            public FeatureDecisionNotificationBuilder withFeatureKey(string featureKey)
            {
                FeatureKey = featureKey;
                return this;
            }

            public FeatureDecisionNotificationBuilder withFeatureEnabled(bool featureEnabled)
            {
                FeatureEnabled = featureEnabled;
                return this;
            }

            public DecisionNotification Build()
            {
                if (Source == null)
                {
                    throw new Exception("source not set");
                }

                if (FeatureKey == null)
                {
                    throw new Exception("featureKey not set");
                }

                DecisionInfo = new Dictionary<string, object>();
                DecisionInfo.Add(FEATURE_KEY, FeatureKey);
                DecisionInfo.Add(FEATURE_ENABLED, FeatureEnabled);
                DecisionInfo.Add(SOURCE, Source);
                DecisionInfo.Add(SOURCE_INFO, SourceInfo.Get());

                return new DecisionNotification(
                    DecisionNotificationTypes.FEATURE,
                    UserId,
                    Attributes,
                    DecisionInfo);
            }
        }

        public class FeatureVariableDecisionNotificationBuilder
        {
            public static readonly string FEATURE_KEY = "featureKey";
            public static readonly string FEATURE_ENABLED = "featureEnabled";
            public static readonly string SOURCE = "source";
            public static readonly string SOURCE_INFO = "sourceInfo";
            public static readonly string VARIABLE_KEY = "variableKey";
            public static readonly string VARIABLE_TYPE = "variableType";
            public static readonly string VARIABLE_VALUE = "variableValue";
            public static readonly string VARIABLE_VALUES = "variableValues";

            private string NotificationType;
            private string featureKey;
            private bool featureEnabled;
            private FeatureDecision featureDecision;
            private string variableKey;
            private string variableType;
            private object variableValue;
            private object variableValues;
            private string userId;
            private UserAttributes attributes;
            private IDictionary<string, object> decisionInfo;

            protected FeatureVariableDecisionNotificationBuilder()
            {
            }

            public FeatureVariableDecisionNotificationBuilder withUserId(string userId)
            {
                this.userId = userId;
                return this;
            }

            public FeatureVariableDecisionNotificationBuilder withAttributes(UserAttributes attributes)
            {
                this.attributes = attributes;
                return this;
            }

            public FeatureVariableDecisionNotificationBuilder withFeatureKey(String featureKey)
            {
                this.featureKey = featureKey;
                return this;
            }

            public FeatureVariableDecisionNotificationBuilder withFeatureEnabled(bool featureEnabled)
            {
                this.featureEnabled = featureEnabled;
                return this;
            }

            public FeatureVariableDecisionNotificationBuilder withFeatureDecision(FeatureDecision featureDecision)
            {
                this.featureDecision = featureDecision;
                return this;
            }

            public FeatureVariableDecisionNotificationBuilder withVariableKey(String variableKey)
            {
                this.variableKey = variableKey;
                return this;
            }

            public FeatureVariableDecisionNotificationBuilder withVariableType(string variableType)
            {
                this.variableType = variableType;
                return this;
            }

            public FeatureVariableDecisionNotificationBuilder withVariableValue(object variableValue)
            {
                this.variableValue = variableValue;
                return this;
            }

            public FeatureVariableDecisionNotificationBuilder withVariableValues(object variableValues)
            {
                this.variableValues = variableValues;
                return this;
            }

            public DecisionNotification build()
            {
                if (featureKey == null)
                {
                    throw new Exception("featureKey not set");
                }


                decisionInfo = new Dictionary<string, object>();
                decisionInfo.Add(FEATURE_KEY, featureKey);
                decisionInfo.Add(FEATURE_ENABLED, featureEnabled);

                if (variableValues != null)
                {
                    NotificationType = DecisionNotificationTypes.ALL_FEATURE_VARIABLE;
                    decisionInfo.Add(VARIABLE_VALUES, variableValues);
                }
                else
                {
                    NotificationType = DecisionNotificationTypes.FEATURE_VARIABLE;

                    if (variableKey == null)
                    {
                        throw new Exception("variableKey not set");
                    }

                    if (variableType == null)
                    {
                        throw new Exception("variableType not set");
                    }

                    decisionInfo.Add(VARIABLE_KEY, variableKey);
                    decisionInfo.Add(VARIABLE_TYPE, variableType);
                    decisionInfo.Add(VARIABLE_VALUE, variableValue);
                }

                SourceInfo sourceInfo = new RolloutSourceInfo();

                if (featureDecision != null && FeatureDecision.DECISION_SOURCE_FEATURE_TEST.Equals(featureDecision.Source))
                {
                    sourceInfo = new FeatureTestSourceInfo(featureDecision.Experiment.Key, featureDecision.Variation.Key);
                    decisionInfo.Add(SOURCE, featureDecision.Source);
                }
                else
                {
                    decisionInfo.Add(SOURCE, FeatureDecision.DECISION_SOURCE_ROLLOUT);
                }
                decisionInfo.Add(SOURCE_INFO, sourceInfo.Get());

                return new DecisionNotification(
                    NotificationType,
                    userId,
                    attributes,
                    decisionInfo);
            }
        }

        public class FlagDecisionNotificationBuilder
        {
            public static readonly string FLAG_KEY = "flagKey";
            public static readonly string ENABLED = "enabled";
            public static readonly string VARIABLES = "variables";
            public static readonly string VARIATION_KEY = "variationKey";
            public static readonly string RULE_KEY = "ruleKey";
            public static readonly string REASONS = "reasons";
            public static readonly string DECISION_EVENT_DISPATCHED = "decisionEventDispatched";

            private string flagKey;
            private bool enabled;
            private object variables;
            private string userId;
            private UserAttributes attributes;
            private string variationKey;
            private string ruleKey;
            private List<string> reasons;
            private bool decisionEventDispatched;

            private Dictionary<string, object> decisionInfo;

            public FlagDecisionNotificationBuilder withUserId(string userId)
            {
                this.userId = userId;
                return this;
            }

            public FlagDecisionNotificationBuilder withAttributes(UserAttributes attributes)
            {
                this.attributes = attributes;
                return this;
            }

            public FlagDecisionNotificationBuilder withFlagKey(string flagKey)
            {
                this.flagKey = flagKey;
                return this;
            }

            public FlagDecisionNotificationBuilder withEnabled(bool enabled)
            {
                this.enabled = enabled;
                return this;
            }

            public FlagDecisionNotificationBuilder withVariables(object variables)
            {
                this.variables = variables;
                return this;
            }

            public FlagDecisionNotificationBuilder withVariationKey(string key)
            {
                this.variationKey = key;
                return this;
            }

            public FlagDecisionNotificationBuilder withRuleKey(string key)
            {
                this.ruleKey = key;
                return this;
            }

            public FlagDecisionNotificationBuilder withReasons(List<string> reasons)
            {
                this.reasons = reasons;
                return this;
            }

            public FlagDecisionNotificationBuilder withDecisionEventDispatched(bool dispatched)
            {
                this.decisionEventDispatched = dispatched;
                return this;
            }

            public DecisionNotification Build()
            {
                if (flagKey == null)
                {
                    throw new Exception("flagKey not set");
                }

                decisionInfo = new Dictionary<string, object>() {
                    { FLAG_KEY, flagKey },
                    { ENABLED, enabled },
                    { VARIABLES, variables },
                    { VARIATION_KEY, variationKey },
                    { RULE_KEY, ruleKey },
                    { REASONS, reasons },
                    { DECISION_EVENT_DISPATCHED, decisionEventDispatched },
                    { FLAG_KEY, flagKey }
                };

            return new DecisionNotification(
                DecisionNotificationTypes.FLAG,
                userId,
                attributes,
                decisionInfo);
            }
        }

    }
}
