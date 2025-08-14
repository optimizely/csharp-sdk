/*
 * Copyright 2025, Optimizely
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OptimizelySDK.Entity;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class HoldoutConfigTests
    {
        private JObject testData;
        private Holdout globalHoldout;
        private Holdout includedHoldout;
        private Holdout excludedHoldout;

        [SetUp]
        public void Setup()
        {
            // Load test data
            var testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestData", "HoldoutTestData.json");
            var jsonContent = File.ReadAllText(testDataPath);
            testData = JObject.Parse(jsonContent);

            // Deserialize test holdouts
            globalHoldout = JsonConvert.DeserializeObject<Holdout>(testData["globalHoldout"].ToString());
            includedHoldout = JsonConvert.DeserializeObject<Holdout>(testData["includedFlagsHoldout"].ToString());
            excludedHoldout = JsonConvert.DeserializeObject<Holdout>(testData["excludedFlagsHoldout"].ToString());
        }

        [Test]
        public void TestEmptyHoldouts_ShouldHaveEmptyMaps()
        {
            var config = new HoldoutConfig(new Holdout[0]);

            Assert.IsNotNull(config.HoldoutIdMap);
            Assert.AreEqual(0, config.HoldoutIdMap.Count);
            Assert.IsNotNull(config.GetHoldoutsForFlag("any_flag"));
            Assert.AreEqual(0, config.GetHoldoutsForFlag("any_flag").Count);
        }

        [Test]
        public void TestHoldoutIdMapping()
        {
            var allHoldouts = new[] { globalHoldout, includedHoldout, excludedHoldout };
            var config = new HoldoutConfig(allHoldouts);

            Assert.IsNotNull(config.HoldoutIdMap);
            Assert.AreEqual(3, config.HoldoutIdMap.Count);

            Assert.IsTrue(config.HoldoutIdMap.ContainsKey("holdout_global_1"));
            Assert.IsTrue(config.HoldoutIdMap.ContainsKey("holdout_included_1"));
            Assert.IsTrue(config.HoldoutIdMap.ContainsKey("holdout_excluded_1"));

            Assert.AreEqual(globalHoldout.Id, config.HoldoutIdMap["holdout_global_1"].Id);
            Assert.AreEqual(includedHoldout.Id, config.HoldoutIdMap["holdout_included_1"].Id);
            Assert.AreEqual(excludedHoldout.Id, config.HoldoutIdMap["holdout_excluded_1"].Id);
        }

        [Test]
        public void TestGetHoldoutById()
        {
            var allHoldouts = new[] { globalHoldout, includedHoldout, excludedHoldout };
            var config = new HoldoutConfig(allHoldouts);

            var retrievedGlobal = config.GetHoldout("holdout_global_1");
            var retrievedIncluded = config.GetHoldout("holdout_included_1");
            var retrievedExcluded = config.GetHoldout("holdout_excluded_1");

            Assert.IsNotNull(retrievedGlobal);
            Assert.AreEqual("holdout_global_1", retrievedGlobal.Id);
            Assert.AreEqual("global_holdout", retrievedGlobal.Key);

            Assert.IsNotNull(retrievedIncluded);
            Assert.AreEqual("holdout_included_1", retrievedIncluded.Id);
            Assert.AreEqual("included_holdout", retrievedIncluded.Key);

            Assert.IsNotNull(retrievedExcluded);
            Assert.AreEqual("holdout_excluded_1", retrievedExcluded.Id);
            Assert.AreEqual("excluded_holdout", retrievedExcluded.Key);
        }

        [Test]
        public void TestGetHoldoutById_InvalidId()
        {
            var allHoldouts = new[] { globalHoldout };
            var config = new HoldoutConfig(allHoldouts);

            var result = config.GetHoldout("invalid_id");
            Assert.IsNull(result);
        }

        [Test]
        public void TestGlobalHoldoutsForFlag()
        {
            var allHoldouts = new[] { globalHoldout };
            var config = new HoldoutConfig(allHoldouts);

            var holdoutsForFlag = config.GetHoldoutsForFlag("any_flag_id");

            Assert.IsNotNull(holdoutsForFlag);
            Assert.AreEqual(1, holdoutsForFlag.Count);
            Assert.AreEqual("holdout_global_1", holdoutsForFlag[0].Id);
        }

        [Test]
        public void TestIncludedHoldoutsForFlag()
        {
            var allHoldouts = new[] { includedHoldout };
            var config = new HoldoutConfig(allHoldouts);

            // Test for included flags
            var holdoutsForFlag1 = config.GetHoldoutsForFlag("flag_1");
            var holdoutsForFlag2 = config.GetHoldoutsForFlag("flag_2");
            var holdoutsForOtherFlag = config.GetHoldoutsForFlag("other_flag");

            Assert.IsNotNull(holdoutsForFlag1);
            Assert.AreEqual(1, holdoutsForFlag1.Count);
            Assert.AreEqual("holdout_included_1", holdoutsForFlag1[0].Id);

            Assert.IsNotNull(holdoutsForFlag2);
            Assert.AreEqual(1, holdoutsForFlag2.Count);
            Assert.AreEqual("holdout_included_1", holdoutsForFlag2[0].Id);

            Assert.IsNotNull(holdoutsForOtherFlag);
            Assert.AreEqual(0, holdoutsForOtherFlag.Count);
        }

        [Test]
        public void TestExcludedHoldoutsForFlag()
        {
            var allHoldouts = new[] { excludedHoldout };
            var config = new HoldoutConfig(allHoldouts);

            // Test for excluded flags - should NOT appear
            var holdoutsForFlag3 = config.GetHoldoutsForFlag("flag_3");
            var holdoutsForFlag4 = config.GetHoldoutsForFlag("flag_4");
            var holdoutsForOtherFlag = config.GetHoldoutsForFlag("other_flag");

            // Excluded flags should not get this holdout
            Assert.IsNotNull(holdoutsForFlag3);
            Assert.AreEqual(0, holdoutsForFlag3.Count);

            Assert.IsNotNull(holdoutsForFlag4);
            Assert.AreEqual(0, holdoutsForFlag4.Count);

            // Other flags should get this global holdout (with exclusions)
            Assert.IsNotNull(holdoutsForOtherFlag);
            Assert.AreEqual(1, holdoutsForOtherFlag.Count);
            Assert.AreEqual("holdout_excluded_1", holdoutsForOtherFlag[0].Id);
        }

        [Test]
        public void TestHoldoutOrdering_GlobalThenIncluded()
        {
            // Create additional test holdouts with specific IDs for ordering test
            var global1 = CreateTestHoldout("global_1", "g1", new string[0], new string[0]);
            var global2 = CreateTestHoldout("global_2", "g2", new string[0], new string[0]);
            var included = CreateTestHoldout("included_1", "i1", new[] { "test_flag" }, new string[0]);

            var allHoldouts = new[] { included, global1, global2 };
            var config = new HoldoutConfig(allHoldouts);

            var holdoutsForFlag = config.GetHoldoutsForFlag("test_flag");

            Assert.IsNotNull(holdoutsForFlag);
            Assert.AreEqual(3, holdoutsForFlag.Count);

            // Should be: global1, global2, included (global first, then included)
            var ids = holdoutsForFlag.Select(h => h.Id).ToArray();
            Assert.Contains("global_1", ids);
            Assert.Contains("global_2", ids);
            Assert.Contains("included_1", ids);

            // Included should be last (after globals)
            Assert.AreEqual("included_1", holdoutsForFlag.Last().Id);
        }

        [Test]
        public void TestComplexFlagScenarios_MultipleRules()
        {
            var global1 = CreateTestHoldout("global_1", "g1", new string[0], new string[0]);
            var global2 = CreateTestHoldout("global_2", "g2", new string[0], new string[0]);
            var included = CreateTestHoldout("included_1", "i1", new[] { "flag_1" }, new string[0]);
            var excluded = CreateTestHoldout("excluded_1", "e1", new string[0], new[] { "flag_2" });

            var allHoldouts = new[] { included, excluded, global1, global2 };
            var config = new HoldoutConfig(allHoldouts);

            // Test flag_1: should get globals + excluded global + included
            var holdoutsForFlag1 = config.GetHoldoutsForFlag("flag_1");
            Assert.AreEqual(4, holdoutsForFlag1.Count);
            var flag1Ids = holdoutsForFlag1.Select(h => h.Id).ToArray();
            Assert.Contains("global_1", flag1Ids);
            Assert.Contains("global_2", flag1Ids);
            Assert.Contains("excluded_1", flag1Ids); // excluded global should appear for other flags
            Assert.Contains("included_1", flag1Ids);

            // Test flag_2: should get only regular globals (excluded global should NOT appear)
            var holdoutsForFlag2 = config.GetHoldoutsForFlag("flag_2");
            Assert.AreEqual(2, holdoutsForFlag2.Count);
            var flag2Ids = holdoutsForFlag2.Select(h => h.Id).ToArray();
            Assert.Contains("global_1", flag2Ids);
            Assert.Contains("global_2", flag2Ids);
            Assert.IsFalse(flag2Ids.Contains("excluded_1")); // Should be excluded
            Assert.IsFalse(flag2Ids.Contains("included_1")); // Not included for this flag

            // Test flag_3: should get globals + excluded global
            var holdoutsForFlag3 = config.GetHoldoutsForFlag("flag_3");
            Assert.AreEqual(3, holdoutsForFlag3.Count);
            var flag3Ids = holdoutsForFlag3.Select(h => h.Id).ToArray();
            Assert.Contains("global_1", flag3Ids);
            Assert.Contains("global_2", flag3Ids);
            Assert.Contains("excluded_1", flag3Ids);
        }

        [Test]
        public void TestExcludedHoldout_ShouldNotAppearInGlobal()
        {
            var global = CreateTestHoldout("global_1", "global", new string[0], new string[0]);
            var excluded = CreateTestHoldout("excluded_1", "excluded", new string[0], new[] { "target_flag" });

            var allHoldouts = new[] { global, excluded };
            var config = new HoldoutConfig(allHoldouts);

            var holdoutsForTargetFlag = config.GetHoldoutsForFlag("target_flag");

            Assert.IsNotNull(holdoutsForTargetFlag);
            Assert.AreEqual(1, holdoutsForTargetFlag.Count);
            Assert.AreEqual("global_1", holdoutsForTargetFlag[0].Id);
            // excluded should NOT appear for target_flag
        }

        [Test]
        public void TestCaching_SecondCallUsesCachedResult()
        {
            var allHoldouts = new[] { globalHoldout, includedHoldout };
            var config = new HoldoutConfig(allHoldouts);

            // First call
            var firstResult = config.GetHoldoutsForFlag("flag_1");

            // Second call - should use cache
            var secondResult = config.GetHoldoutsForFlag("flag_1");

            Assert.IsNotNull(firstResult);
            Assert.IsNotNull(secondResult);
            Assert.AreEqual(firstResult.Count, secondResult.Count);

            // Results should be the same (caching working)
            for (int i = 0; i < firstResult.Count; i++)
            {
                Assert.AreEqual(firstResult[i].Id, secondResult[i].Id);
            }
        }

        [Test]
        public void TestNullFlagId_ReturnsEmptyList()
        {
            var config = new HoldoutConfig(new[] { globalHoldout });

            var result = config.GetHoldoutsForFlag(null);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void TestEmptyFlagId_ReturnsEmptyList()
        {
            var config = new HoldoutConfig(new[] { globalHoldout });

            var result = config.GetHoldoutsForFlag("");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void TestGetHoldoutsForFlag_WithNullHoldouts()
        {
            var config = new HoldoutConfig(null);

            var result = config.GetHoldoutsForFlag("any_flag");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void TestUpdateHoldoutMapping()
        {
            var config = new HoldoutConfig(new[] { globalHoldout });

            // Initial state
            Assert.AreEqual(1, config.HoldoutIdMap.Count);

            // Update with new holdouts
            config.UpdateHoldoutMapping(new[] { globalHoldout, includedHoldout });

            Assert.AreEqual(2, config.HoldoutIdMap.Count);
            Assert.IsTrue(config.HoldoutIdMap.ContainsKey("holdout_global_1"));
            Assert.IsTrue(config.HoldoutIdMap.ContainsKey("holdout_included_1"));
        }

        // Helper method to create test holdouts
        private Holdout CreateTestHoldout(string id, string key, string[] includedFlags, string[] excludedFlags)
        {
            return new Holdout
            {
                Id = id,
                Key = key,
                Status = "Running",
                LayerId = "test_layer",
                Variations = new Variation[0],
                TrafficAllocation = new TrafficAllocation[0],
                AudienceIds = new string[0],
                AudienceConditions = null,
                IncludedFlags = includedFlags,
                ExcludedFlags = excludedFlags
            };
        }
    }
}
