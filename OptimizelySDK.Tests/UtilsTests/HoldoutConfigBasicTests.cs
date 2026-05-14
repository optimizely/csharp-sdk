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

using NUnit.Framework;
using OptimizelySDK.Entity;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class HoldoutConfigBasicTests
    {
        [Test]
        public void TestEmptyHoldouts_ShouldHaveEmptyMaps()
        {
            var config = new HoldoutConfig(new Holdout[0]);

            Assert.IsNotNull(config.HoldoutIdMap);
            Assert.AreEqual(0, config.HoldoutIdMap.Count);
            Assert.AreEqual(0, config.HoldoutCount);
        }

        [Test]
        public void TestHoldoutIdMapping()
        {
            var holdout1 = CreateTestHoldout("holdout_1", "h1");
            var holdout2 = CreateTestHoldout("holdout_2", "h2");
            var allHoldouts = new[] { holdout1, holdout2 };
            var config = new HoldoutConfig(allHoldouts);

            Assert.IsNotNull(config.HoldoutIdMap);
            Assert.AreEqual(2, config.HoldoutIdMap.Count);
            Assert.AreEqual(2, config.HoldoutCount);

            Assert.IsTrue(config.HoldoutIdMap.ContainsKey("holdout_1"));
            Assert.IsTrue(config.HoldoutIdMap.ContainsKey("holdout_2"));

            Assert.AreEqual(holdout1.Id, config.HoldoutIdMap["holdout_1"].Id);
            Assert.AreEqual(holdout2.Id, config.HoldoutIdMap["holdout_2"].Id);
        }

        [Test]
        public void TestGetHoldoutById()
        {
            var holdout = CreateTestHoldout("holdout_1", "h1");
            var config = new HoldoutConfig(new[] { holdout });

            var retrieved = config.GetHoldout("holdout_1");

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("holdout_1", retrieved.Id);
            Assert.AreEqual("h1", retrieved.Key);
        }

        [Test]
        public void TestGetHoldoutById_InvalidId()
        {
            var holdout = CreateTestHoldout("holdout_1", "h1");
            var config = new HoldoutConfig(new[] { holdout });

            var result = config.GetHoldout("invalid_id");
            Assert.IsNull(result);
        }

        [Test]
        public void TestGetHoldoutById_NullId()
        {
            var holdout = CreateTestHoldout("holdout_1", "h1");
            var config = new HoldoutConfig(new[] { holdout });

            var result = config.GetHoldout(null);
            Assert.IsNull(result);
        }

        [Test]
        public void TestGetHoldoutById_EmptyId()
        {
            var holdout = CreateTestHoldout("holdout_1", "h1");
            var config = new HoldoutConfig(new[] { holdout });

            var result = config.GetHoldout("");
            Assert.IsNull(result);
        }

        [Test]
        public void TestUpdateHoldoutMapping()
        {
            var holdout1 = CreateTestHoldout("holdout_1", "h1");
            var config = new HoldoutConfig(new[] { holdout1 });

            // Initial state
            Assert.AreEqual(1, config.HoldoutIdMap.Count);
            Assert.AreEqual(1, config.HoldoutCount);

            // Update with new holdouts
            var holdout2 = CreateTestHoldout("holdout_2", "h2");
            config.UpdateHoldoutMapping(new[] { holdout1, holdout2 });

            Assert.AreEqual(2, config.HoldoutIdMap.Count);
            Assert.AreEqual(2, config.HoldoutCount);
            Assert.IsTrue(config.HoldoutIdMap.ContainsKey("holdout_1"));
            Assert.IsTrue(config.HoldoutIdMap.ContainsKey("holdout_2"));
        }

        [Test]
        public void TestNullHoldouts()
        {
            var config = new HoldoutConfig(null);

            Assert.IsNotNull(config.HoldoutIdMap);
            Assert.AreEqual(0, config.HoldoutIdMap.Count);
            Assert.AreEqual(0, config.HoldoutCount);
        }

        // =====================================================================
        // Level 1: Local Holdout / IsGlobal Classification Tests (FSSDK-12369)
        // =====================================================================

        [Test]
        public void TestIsGlobal_NullIncludedRules_IsGlobal()
        {
            // A holdout with IncludedRules == null is a global holdout
            var holdout = CreateTestHoldout("h1", "global_holdout");
            holdout.IncludedRules = null;

            Assert.IsTrue(holdout.IsGlobal, "Holdout with null IncludedRules should be global");
        }

        [Test]
        public void TestIsGlobal_EmptyIncludedRules_IsNotGlobal()
        {
            // A holdout with IncludedRules == [] is LOCAL (empty array, not null)
            var holdout = CreateTestHoldout("h1", "local_holdout_empty");
            holdout.IncludedRules = new string[0];

            Assert.IsFalse(holdout.IsGlobal, "Holdout with empty array IncludedRules should NOT be global");
        }

        [Test]
        public void TestIsGlobal_NonEmptyIncludedRules_IsNotGlobal()
        {
            // A holdout with IncludedRules = ["rule_1"] is a local holdout
            var holdout = CreateTestHoldout("h1", "local_holdout");
            holdout.IncludedRules = new[] { "rule_1" };

            Assert.IsFalse(holdout.IsGlobal, "Holdout with non-empty IncludedRules should NOT be global");
        }

        [Test]
        public void TestGetGlobalHoldouts_ReturnsOnlyGlobalHoldouts()
        {
            var globalHoldout = CreateTestHoldout("global_id", "global_key");
            globalHoldout.IncludedRules = null; // global

            var localHoldout = CreateTestHoldout("local_id", "local_key");
            localHoldout.IncludedRules = new[] { "rule_1" }; // local

            var config = new HoldoutConfig(new[] { globalHoldout, localHoldout });

            var globals = config.GetGlobalHoldouts();
            Assert.AreEqual(1, globals.Count, "Should return exactly one global holdout");
            Assert.AreEqual("global_id", globals[0].Id);
        }

        [Test]
        public void TestGetGlobalHoldouts_NoGlobalHoldouts_ReturnsEmpty()
        {
            var localHoldout = CreateTestHoldout("local_id", "local_key");
            localHoldout.IncludedRules = new[] { "rule_1" }; // local

            var config = new HoldoutConfig(new[] { localHoldout });

            var globals = config.GetGlobalHoldouts();
            Assert.AreEqual(0, globals.Count, "Should return empty list when no global holdouts exist");
        }

        [Test]
        public void TestGetHoldoutsForRule_ReturnsMatchingLocalHoldout()
        {
            var localHoldout = CreateTestHoldout("local_id", "local_key");
            localHoldout.IncludedRules = new[] { "rule_1", "rule_2" };

            var config = new HoldoutConfig(new[] { localHoldout });

            var holdoutsForRule1 = config.GetHoldoutsForRule("rule_1");
            Assert.AreEqual(1, holdoutsForRule1.Count, "Should return one holdout for rule_1");
            Assert.AreEqual("local_id", holdoutsForRule1[0].Id);

            var holdoutsForRule2 = config.GetHoldoutsForRule("rule_2");
            Assert.AreEqual(1, holdoutsForRule2.Count, "Should return one holdout for rule_2");
        }

        [Test]
        public void TestGetHoldoutsForRule_UnknownRuleId_ReturnsEmpty()
        {
            var localHoldout = CreateTestHoldout("local_id", "local_key");
            localHoldout.IncludedRules = new[] { "rule_1" };

            var config = new HoldoutConfig(new[] { localHoldout });

            var holdoutsForUnknown = config.GetHoldoutsForRule("unknown_rule_id");
            Assert.AreEqual(0, holdoutsForUnknown.Count, "Should return empty list for unknown rule ID");
        }

        [Test]
        public void TestGetHoldoutsForRule_NullOrEmptyRuleId_ReturnsEmpty()
        {
            var localHoldout = CreateTestHoldout("local_id", "local_key");
            localHoldout.IncludedRules = new[] { "rule_1" };

            var config = new HoldoutConfig(new[] { localHoldout });

            Assert.AreEqual(0, config.GetHoldoutsForRule(null).Count, "Should return empty for null rule ID");
            Assert.AreEqual(0, config.GetHoldoutsForRule("").Count, "Should return empty for empty rule ID");
        }

        [Test]
        public void TestGetHoldoutsForRule_EmptyIncludedRules_NoRuleMatches()
        {
            // A local holdout with IncludedRules == [] matches NO rules
            var localHoldout = CreateTestHoldout("local_id", "local_key");
            localHoldout.IncludedRules = new string[0]; // empty array = local, but no rules

            var config = new HoldoutConfig(new[] { localHoldout });

            // Should not appear in global holdouts
            Assert.AreEqual(0, config.GetGlobalHoldouts().Count, "Empty-array holdout should not be global");

            // Should not match any rule
            Assert.AreEqual(0, config.GetHoldoutsForRule("any_rule").Count, "Empty-array holdout should match no rules");
        }

        [Test]
        public void TestBackwardCompatibility_NullIncludedRulesDefaultsToGlobal()
        {
            // Old datafile holdouts have no includedRules field → IncludedRules is null → global
            var legacyHoldout = CreateTestHoldout("legacy_id", "legacy_key");
            // IncludedRules is null by default (not set)

            var config = new HoldoutConfig(new[] { legacyHoldout });

            var globals = config.GetGlobalHoldouts();
            Assert.AreEqual(1, globals.Count, "Legacy holdout (null IncludedRules) should be treated as global");
            Assert.AreEqual("legacy_id", globals[0].Id);
        }

        [Test]
        public void TestMultipleLocalHoldoutsForSameRule()
        {
            var holdout1 = CreateTestHoldout("local_id_1", "local_key_1");
            holdout1.IncludedRules = new[] { "rule_shared" };

            var holdout2 = CreateTestHoldout("local_id_2", "local_key_2");
            holdout2.IncludedRules = new[] { "rule_shared" };

            var config = new HoldoutConfig(new[] { holdout1, holdout2 });

            var holdoutsForSharedRule = config.GetHoldoutsForRule("rule_shared");
            Assert.AreEqual(2, holdoutsForSharedRule.Count, "Both local holdouts should be returned for the shared rule");
        }

        [Test]
        public void TestCrossRuleTargeting_OneHoldoutTargetsMultipleRules()
        {
            // A single local holdout can target rules from multiple flags
            var crossFlagHoldout = CreateTestHoldout("cross_id", "cross_key");
            crossFlagHoldout.IncludedRules = new[] { "rule_flag_a", "rule_flag_b", "rule_flag_c" };

            var config = new HoldoutConfig(new[] { crossFlagHoldout });

            Assert.AreEqual(1, config.GetHoldoutsForRule("rule_flag_a").Count, "Should match rule_flag_a");
            Assert.AreEqual(1, config.GetHoldoutsForRule("rule_flag_b").Count, "Should match rule_flag_b");
            Assert.AreEqual(1, config.GetHoldoutsForRule("rule_flag_c").Count, "Should match rule_flag_c");
            Assert.AreEqual(0, config.GetHoldoutsForRule("rule_flag_d").Count, "Should not match unrelated rule");
        }

        // Helper method to create test holdouts
        private Holdout CreateTestHoldout(string id, string key)
        {
            return new Holdout
            {
                Id = id,
                Key = key,
                Status = "Running",
                Variations = new Variation[0],
                TrafficAllocation = new TrafficAllocation[0],
                AudienceIds = new string[0],
                AudienceConditions = null
            };
        }
    }
}
