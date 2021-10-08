using NUnit.Framework;
using OptimizelySDK.Entity;
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
        #endregion

        #region Experiment
        public static void AreEqual(Experiment expected, Experiment actual)
        {
            Assert.AreEqual(expected.AudienceConditions, actual.AudienceConditions);
            Assert.AreEqual(expected.AudienceConditionsList, actual.AudienceConditionsList);
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
        #endregion

        #region FeatureDecision
        public static void AreEqual(FeatureDecision expected, FeatureDecision actual)
        {
            AreEqual(expected.Experiment, actual.Experiment);
        }
        #endregion

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
        #endregion

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
        #endregion

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

        public static void AreEquivalent(Dictionary<string, List<Variation>> expected, Dictionary<string, List<Variation>> actual)
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

        #endregion

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
        #endregion

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
        #endregion
    }
}
