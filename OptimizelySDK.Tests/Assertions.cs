/*
 * Copyright 2017, 2019-2021, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
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

using NUnit.Framework;
using OptimizelySDK.Entity;
using OptimizelySDK.OptimizelyDecisions;
using OptimizelySDK.OptlyConfig;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimizelySDK.Tests
{
    /// <summary>
    /// Simplifies assertions and provides more insight into which particular piece is failing for a value.
    /// Especially helpful for Test Driven Development
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Assertions
    {
        #region Basic asserts

        public static bool HasItems(IEnumerable<object> expected, IEnumerable<object> actual, bool allowNull = true)
        {
            if (allowNull && expected == null && actual == null)
                return false;

            Assert.AreEqual(expected.Count(), actual.Count());

            return true;
        }

        public static void AreEquivalent(IEnumerable<string> expected, IEnumerable<string> actual)
        {
            if (HasItems(expected, actual, false))
            {
                var zipped = expected.Zip(actual, (e, a) =>
                {
                    return new
                    {
                        Expected = e,
                        Actual = a
                    };
                }).ToList();

                foreach (var z in zipped)
                {
                    Assert.AreEqual(z.Expected, z.Actual);
                };
            }
        }

        public static void AreEquivalent(Dictionary<string, string> expected, Dictionary<string, string> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                AreEqual(z.Expected, z.Actual);
            };
        }

        public static void AreEqual(KeyValuePair<string, string> expected, KeyValuePair<string, string> actual)
        {
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Value, actual.Value);
        }

        public static void AreEqual(OptimizelyJSON expected, OptimizelyJSON actual)
        {
            Assert.AreEqual(expected.ToString(), actual.ToString());
        }

        private static void AreEquivalent(KeyValuePair<string, object> expected, KeyValuePair<string, object> actual)
        {
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Value, actual.Value);
        }

        #endregion Basic asserts

        #region

        public static void AreEqual(OptimizelyForcedDecision expected, OptimizelyForcedDecision actual)
        {
            Assert.AreEqual(expected.VariationKey, actual.VariationKey);
        }

        #endregion

        #region OptimizelyAttribute

        public static void AreEquivalent(OptimizelyAttribute[] expected, OptimizelyAttribute[] actual)
        {
            Assert.AreEqual(expected.Count(), actual.Count());
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                AreEqual(z.Expected, z.Actual);
            };
        }

        public static void AreEqual(OptimizelyAttribute expected, OptimizelyAttribute actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Key, actual.Key);
        }

        #endregion OptimizelyAttribute

        #region OptimizelyAudience

        private static void AreEquivalent(OptimizelyAudience[] expected, OptimizelyAudience[] actual)
        {
            Assert.AreEqual(expected.Count(), actual.Count());
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                AreEqual(z.Expected, z.Actual);
            };
        }

        private static void AreEqual(OptimizelyAudience expected, OptimizelyAudience actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Name, actual.Name);
            //AreEqual(expected.Conditions, actual.Conditions);
        }

        #endregion OptimizelyAudience

        #region OptimizelyConfig

        public static void AreEqual(OptimizelyConfig expected, OptimizelyConfig actual)
        {
            AreEquivalent(expected.Attributes, actual.Attributes);
            AreEquivalent(expected.Audiences, actual.Audiences);
            Assert.AreEqual(expected.EnvironmentKey, actual.EnvironmentKey);
            AreEquivalent(expected.Events, actual.Events);
            AreEquivalent(expected.ExperimentsMap, actual.ExperimentsMap);
            AreEquivalent(expected.FeaturesMap, actual.FeaturesMap);
            Assert.AreEqual(expected.Revision, actual.Revision);
            Assert.AreEqual(expected.SDKKey, actual.SDKKey);
        }

        #endregion OptimizelyConfig

        #region OptimizelyUserContext

        public static void AreEqual(OptimizelyUserContext expected, OptimizelyUserContext actual)
        {
            Assert.AreEqual(expected.GetUserId(), actual.GetUserId());
            AreEquivalent(expected.GetAttributes(), actual.GetAttributes());
        }

        private static void AreEquivalent(UserAttributes expected, UserAttributes actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                AreEquivalent(z.Expected, z.Actual);
            };
        }

        #endregion

        #region OptimizelyEvent

        public static void AreEquivalent(OptimizelyEvent[] expected, OptimizelyEvent[] actual)
        {
            Assert.AreEqual(expected.Count(), actual.Count());
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                AreEqual(z.Expected, z.Actual);
            };
        }

        private static void AreEqual(OptimizelyEvent expected, OptimizelyEvent actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Key, actual.Key);
            AreEquivalent(expected.ExperimentIds, actual.ExperimentIds);
        }

        #endregion OptimizelyEvent

        #region OptimizelyDecision

        public static void AreEqual(OptimizelyDecision expected, OptimizelyDecision actual)
        {
            Assert.AreEqual(expected.Enabled, actual.Enabled);
            Assert.AreEqual(expected.FlagKey, actual.FlagKey);
            AreEquivalent(expected.Reasons, actual.Reasons);
            Assert.AreEqual(expected.RuleKey, actual.RuleKey);
            AreEqual(expected.UserContext, actual.UserContext);
            Assert.AreEqual(expected.VariationKey, actual.VariationKey);
        }

        public static void AreEquivalent(IDictionary<string, OptimizelyDecision> expected, IDictionary<string, OptimizelyDecision> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                AreEquivalent(z.Expected, z.Actual);
            };
        }

        public static void AreEquivalent(KeyValuePair<string, OptimizelyDecision> expected, KeyValuePair<string, OptimizelyDecision> actual)
        {
            Assert.AreEqual(expected.Key, actual.Key);
            AreEqual(expected.Value, actual.Value);
        }

        #endregion

        #region OptimizelyExperiement

        public static void AreEquivalent(List<OptimizelyExperiment> expected, List<OptimizelyExperiment> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                AreEqual(z.Expected, z.Actual);
            };
        }

        public static void AreEqual(OptimizelyExperiment expected, OptimizelyExperiment actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Audiences, actual.Audiences);
        }

        public static void AreEquivalent(IDictionary<string, OptimizelyExperiment> expected, IDictionary<string, OptimizelyExperiment> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                AreEquivalent(z.Expected, z.Actual);
            };
        }

        public static void AreEquivalent(KeyValuePair<string, OptimizelyExperiment> expected, KeyValuePair<string, OptimizelyExperiment> actual)
        {
            Assert.AreEqual(expected.Key, actual.Key);
            AreEqual(expected.Value, actual.Value);
        }

        #endregion OptimizelyExperiement

        #region OptimizelyFeature

        public static void AreEquivalent(IDictionary<string, OptimizelyFeature> expected, IDictionary<string, OptimizelyFeature> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                AreEquivalent(z.Expected, z.Actual);
            };
        }

        public static void AreEquivalent(KeyValuePair<string, OptimizelyFeature> expected, KeyValuePair<string, OptimizelyFeature> actual)
        {
            Assert.AreEqual(expected.Key, actual.Key);
            AreEqual(expected.Value, actual.Value);
        }

        public static void AreEqual(OptimizelyFeature expected, OptimizelyFeature actual)
        {
            AreEquivalent(expected.DeliveryRules, actual.DeliveryRules);
            AreEquivalent(expected.ExperimentRules, actual.ExperimentRules);
            AreEquivalent(expected.ExperimentsMap, actual.ExperimentsMap);
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Key, actual.Key);
            AreEquivalent(expected.VariablesMap, actual.VariablesMap);
        }

        #endregion OptimizelyFeature

        #region OptimizelyVariable

        public static void AreEquivalent(IDictionary<string, OptimizelyVariable> expected, IDictionary<string, OptimizelyVariable> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                AreEquivalent(z.Expected, z.Actual);
            };
        }

        public static void AreEquivalent(KeyValuePair<string, OptimizelyVariable> expected, KeyValuePair<string, OptimizelyVariable> actual)
        {
            Assert.AreEqual(expected.Key, actual.Key);
            AreEqual(expected.Value, actual.Value);
        }

        public static void AreEqual(OptimizelyVariable expected, OptimizelyVariable actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Type, actual.Type);
            Assert.AreEqual(expected.Value, actual.Value);
        }

        #endregion OptimizelyVariable

        #region Experiment

        public static void AreEqual(Experiment expected, Experiment actual)
        {
            Assert.AreEqual(expected.AudienceConditions, actual.AudienceConditions);
            //Assert.AreEqual(expected.AudienceConditionsList, actual.AudienceConditionsList);
            Assert.AreEqual(expected.AudienceConditionsString, actual.AudienceConditionsString);
            AreEquivalent(expected.AudienceIds, actual.AudienceIds);
            Assert.AreEqual(expected.AudienceIdsList, actual.AudienceIdsList);
            Assert.AreEqual(expected.AudienceIdsString, actual.AudienceIdsString);
            AreEquivalent(expected.ForcedVariations, actual.ForcedVariations);
            Assert.AreEqual(expected.GroupId, actual.GroupId);
            Assert.AreEqual(expected.GroupPolicy, actual.GroupPolicy);
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.IsExperimentRunning, actual.IsExperimentRunning);
            Assert.AreEqual(expected.IsInMutexGroup, actual.IsInMutexGroup);
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.LayerId, actual.LayerId);
            Assert.AreEqual(expected.Status, actual.Status);
            AreEquivalent(expected.TrafficAllocation, actual.TrafficAllocation);
            AreEquivalent(expected.UserIdToKeyVariations, actual.UserIdToKeyVariations);
            AreEquivalent(expected.VariationIdToVariationMap, actual.VariationIdToVariationMap);
            AreEquivalent(expected.VariationKeyToVariationMap, actual.VariationKeyToVariationMap);
            AreEquivalent(expected.Variations, actual.Variations);
        }

        #endregion Experiment

        #region FeatureDecision

        public static void AreEqual(FeatureDecision expected, FeatureDecision actual)
        {
            AreEqual(expected.Experiment, actual.Experiment);
        }

        #endregion FeatureDecision

        #region FeatureFlags

        public static void AreEquivalent(Dictionary<string, FeatureFlag> expected, Dictionary<string, FeatureFlag> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                AreEqual(z.Expected, z.Actual);
            }
        }

        public static void AreEqual(KeyValuePair<string, FeatureFlag> expected, KeyValuePair<string, FeatureFlag> actual)
        {
            Assert.AreEqual(expected.Key, actual.Key);
            AreEqual(expected.Value, actual.Value);
        }

        public static void AreEqual(FeatureFlag expected, FeatureFlag actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.RolloutId, actual.RolloutId);
            AreEquivalent(expected.VariableKeyToFeatureVariableMap, actual.VariableKeyToFeatureVariableMap);
        }

        #endregion FeatureFlags

        #region FeatureVariable

        public static void AreEqual(FeatureVariable expected, FeatureVariable actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.DefaultValue, actual.DefaultValue);
            Assert.AreEqual(expected.Status, actual.Status);
            Assert.AreEqual(expected.SubType, actual.SubType);
            Assert.AreEqual(expected.Type, actual.Type);
        }

        public static void AreEquivalent(Dictionary<string, FeatureVariable> expected, Dictionary<string, FeatureVariable> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                AreEquivalent(z.Expected, z.Actual);
            }
        }

        public static void AreEquivalent(KeyValuePair<string, FeatureVariable> expected, KeyValuePair<string, FeatureVariable> actual)
        {
            Assert.AreEqual(expected.Key, actual.Key);
            AreEqual(expected.Value, actual.Value);
        }

        #endregion FeatureVariable

        #region Variations

        public static void AreEqual(Variation expected, Variation actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.FeatureEnabled, actual.FeatureEnabled);

            AreEquivalent(expected.FeatureVariableUsageInstances, actual.FeatureVariableUsageInstances);
            AreEquivalent(expected.VariableIdToVariableUsageInstanceMap, actual.VariableIdToVariableUsageInstanceMap);
        }

        public static void AreEquivalent(Dictionary<string, Variation> expected, Dictionary<string, Variation> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                AreEqual(z.Expected, z.Actual);
            };
        }

        public static void AreEquivalent(List<Dictionary<string, List<Variation>>> expected, List<Dictionary<string, List<Variation>>> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                AreEquivalent(z.Expected, z.Actual);
            };
        }

        public static void AreEquivalent(IDictionary<string, List<Variation>> expected, IDictionary<string, List<Variation>> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                AreEqual(z.Expected, z.Actual);
            };
        }

        public static void AreEqual(KeyValuePair<string, Variation> expected, KeyValuePair<string, Variation> actual)
        {
            Assert.AreEqual(expected.Key, actual.Key);
            AreEqual(expected.Value, actual.Value);
        }

        public static void AreEqual(KeyValuePair<string, List<Variation>> expected, KeyValuePair<string, List<Variation>> actual)
        {
            Assert.AreEqual(expected.Key, actual.Key);
            AreEquivalent(expected.Value, actual.Value);
        }

        public static void AreEquivalent(KeyValuePair<string, List<Variation>> expected, KeyValuePair<string, List<Variation>> actual)
        {
            Assert.AreEqual(expected.Key, actual.Key);
            AreEquivalent(expected.Value, actual.Value);
        }

        public static void AreEquivalent(IEnumerable<Variation> expected, IEnumerable<Variation> actual)
        {
            Assert.AreEqual(expected.Count(), actual.Count());
            expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList().ForEach((item) =>
            {
                AreEqual(item.Expected, item.Actual);
            });
        }

        #endregion Variations

        #region FeatureVariableUsage

        public static void AreEquivalent(IEnumerable<FeatureVariableUsage> expected, IEnumerable<FeatureVariableUsage> actual)
        {
            if (HasItems(expected, actual))
            {
                expected.Zip(actual, (e, a) =>
                {
                    return new
                    {
                        Expected = e,
                        Actual = a
                    };
                }).ToList().ForEach((item) =>
                {
                    AreEqual(item.Expected, item.Actual);
                });
            }
        }

        public static void AreEquivalent(Dictionary<string, FeatureVariableUsage> expected, Dictionary<string, FeatureVariableUsage> actual)
        {
            if (expected == null && actual == null)
            {
                return;
            }

            Assert.AreEqual(expected.Count(), actual.Count());
            expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList().ForEach((item) =>
            {
                AreEquivalent(item.Expected, item.Actual);
            });
        }

        public static void AreEquivalent(KeyValuePair<string, FeatureVariableUsage> expected, KeyValuePair<string, FeatureVariableUsage> actual)
        {
            Assert.AreEqual(expected.Key, actual.Key);
            AreEqual(expected.Value, actual.Value);
        }

        public static void AreEqual(FeatureVariableUsage expected, FeatureVariableUsage actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Value, actual.Value);
        }

        #endregion FeatureVariableUsage

        #region TrafficAllocation

        public static void AreEquivalent(IEnumerable<TrafficAllocation> expected, IEnumerable<TrafficAllocation> actual)
        {
            Assert.AreEqual(expected.Count(), actual.Count());
            var zipped = expected.Zip(actual, (e, a) =>
            {
                return new
                {
                    Expected = e,
                    Actual = a
                };
            }).ToList();

            foreach (var z in zipped)
            {
                Assert.AreEqual(z.Expected, z.Actual);
            };
        }

        public static void AreEqual(TrafficAllocation expected, TrafficAllocation actual)
        {
            Assert.AreEqual(expected.EndOfRange, actual.EndOfRange);
            Assert.AreEqual(expected.EntityId, actual.EntityId);
        }

        #endregion TrafficAllocation

        #region DecisionReasons

        public static void AreEqual(DecisionReasons expected, DecisionReasons actual)
        {
            AreEquivalent(expected.ToReport(), actual.ToReport());
        }

        #endregion DecisionReasons

        #region Result T

        public static void AreEqual(Result<Variation> expected, Result<Variation> actual)
        {
            AreEqual(expected.DecisionReasons, actual.DecisionReasons);
            if (expected.ResultObject != null && actual.ResultObject != null)
            {
                AreEqual(expected.ResultObject, actual.ResultObject);
            }

            #endregion Result T
        }
    }
}
