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
        #region Variations
        public static void AreEqual(Variation expected, Variation actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.FeatureEnabled, actual.FeatureEnabled);

            AreEquivalent(expected.FeatureVariableUsageInstances, actual.FeatureVariableUsageInstances);
            AreEquivalent(expected.VariableIdToVariableUsageInstanceMap, actual.VariableIdToVariableUsageInstanceMap);
        }

        public static void AreEquivalent(IEnumerable<KeyValuePair<string, ICollection<Variation>>> expected, IEnumerable<KeyValuePair<string, ICollection<Variation>>> actual)
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

            foreach(var z in zipped)
            {
                AreEquivalent(z.Expected, z.Actual);
            }
        }

        public static void AreEquivalent(KeyValuePair<string, ICollection<Variation>> expected, KeyValuePair<string, ICollection<Variation>> actual)
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

        public static void AreEquivalent(Dictionary<string, FeatureVariableUsage> expected, Dictionary<string, FeatureVariableUsage> actual)
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


    }
}
