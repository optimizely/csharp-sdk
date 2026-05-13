/*
 * Copyright 2026, Optimizely
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

using System.Linq;
using NUnit.Framework;
using OptimizelySDK.Entity;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests.UtilsTests
{
    [TestFixture]
    public class LocalHoldoutConfigTests
    {
        [Test]
        public void TestGetGlobalHoldouts_ReturnsOnlyGlobalHoldouts()
        {
            var holdouts = new[]
            {
                CreateHoldout("h1", "global1", null),
                CreateHoldout("h2", "local1", new[] { "rule1" }),
                CreateHoldout("h3", "global2", null),
                CreateHoldout("h4", "local2", new[] { "rule2", "rule3" })
            };

            var config = new HoldoutConfig(holdouts);
            var globalHoldouts = config.GetGlobalHoldouts();

            Assert.AreEqual(2, globalHoldouts.Count);
            Assert.IsTrue(globalHoldouts.Any(h => h.Id == "h1"));
            Assert.IsTrue(globalHoldouts.Any(h => h.Id == "h3"));
            Assert.IsFalse(globalHoldouts.Any(h => h.Id == "h2"));
            Assert.IsFalse(globalHoldouts.Any(h => h.Id == "h4"));
        }

        [Test]
        public void TestGetHoldoutsForRule_ReturnsCorrectLocalHoldouts()
        {
            var holdouts = new[]
            {
                CreateHoldout("h1", "global1", null),
                CreateHoldout("h2", "local_rule1", new[] { "rule1" }),
                CreateHoldout("h3", "local_rule1_rule2", new[] { "rule1", "rule2" }),
                CreateHoldout("h4", "local_rule3", new[] { "rule3" })
            };

            var config = new HoldoutConfig(holdouts);

            // Test rule1 - should get h2 and h3
            var rule1Holdouts = config.GetHoldoutsForRule("rule1");
            Assert.AreEqual(2, rule1Holdouts.Count);
            Assert.IsTrue(rule1Holdouts.Any(h => h.Id == "h2"));
            Assert.IsTrue(rule1Holdouts.Any(h => h.Id == "h3"));

            // Test rule2 - should get h3 only
            var rule2Holdouts = config.GetHoldoutsForRule("rule2");
            Assert.AreEqual(1, rule2Holdouts.Count);
            Assert.IsTrue(rule2Holdouts.Any(h => h.Id == "h3"));

            // Test rule3 - should get h4 only
            var rule3Holdouts = config.GetHoldoutsForRule("rule3");
            Assert.AreEqual(1, rule3Holdouts.Count);
            Assert.IsTrue(rule3Holdouts.Any(h => h.Id == "h4"));
        }

        [Test]
        public void TestGetHoldoutsForRule_NonExistentRule_ReturnsEmpty()
        {
            var holdouts = new[]
            {
                CreateHoldout("h1", "global1", null),
                CreateHoldout("h2", "local_rule1", new[] { "rule1" })
            };

            var config = new HoldoutConfig(holdouts);
            var result = config.GetHoldoutsForRule("rule999");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void TestGetHoldoutsForRule_NullRuleId_ReturnsEmpty()
        {
            var holdouts = new[]
            {
                CreateHoldout("h1", "local_rule1", new[] { "rule1" })
            };

            var config = new HoldoutConfig(holdouts);
            var result = config.GetHoldoutsForRule(null);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void TestGetHoldoutsForRule_EmptyRuleId_ReturnsEmpty()
        {
            var holdouts = new[]
            {
                CreateHoldout("h1", "local_rule1", new[] { "rule1" })
            };

            var config = new HoldoutConfig(holdouts);
            var result = config.GetHoldoutsForRule("");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void TestHoldoutConfig_EmptyIncludedRules_NotInAnyMap()
        {
            // Holdout with empty IncludedRules array should not appear in global or rule maps
            var holdouts = new[]
            {
                CreateHoldout("h1", "global1", null),
                CreateHoldout("h2", "local_empty", new string[0]),
                CreateHoldout("h3", "local_rule1", new[] { "rule1" })
            };

            var config = new HoldoutConfig(holdouts);

            // Empty array holdout should not be in global holdouts
            var globalHoldouts = config.GetGlobalHoldouts();
            Assert.AreEqual(1, globalHoldouts.Count);
            Assert.IsFalse(globalHoldouts.Any(h => h.Id == "h2"));

            // Empty array holdout should not be in any rule map
            var rule1Holdouts = config.GetHoldoutsForRule("rule1");
            Assert.AreEqual(1, rule1Holdouts.Count);
            Assert.IsFalse(rule1Holdouts.Any(h => h.Id == "h2"));

            // Verify it still exists in the ID map
            var h2 = config.GetHoldout("h2");
            Assert.IsNotNull(h2);
            Assert.AreEqual("h2", h2.Id);
        }

        [Test]
        public void TestHoldoutConfig_CrossFlagTargeting()
        {
            // Test holdout targeting rules from multiple flags
            var holdouts = new[]
            {
                CreateHoldout("h1", "cross_flag", new[] { "flag1_rule1", "flag2_rule1", "flag3_rule1" })
            };

            var config = new HoldoutConfig(holdouts);

            // Verify each rule gets the holdout
            var flag1Rule1Holdouts = config.GetHoldoutsForRule("flag1_rule1");
            Assert.AreEqual(1, flag1Rule1Holdouts.Count);
            Assert.AreEqual("h1", flag1Rule1Holdouts[0].Id);

            var flag2Rule1Holdouts = config.GetHoldoutsForRule("flag2_rule1");
            Assert.AreEqual(1, flag2Rule1Holdouts.Count);
            Assert.AreEqual("h1", flag2Rule1Holdouts[0].Id);

            var flag3Rule1Holdouts = config.GetHoldoutsForRule("flag3_rule1");
            Assert.AreEqual(1, flag3Rule1Holdouts.Count);
            Assert.AreEqual("h1", flag3Rule1Holdouts[0].Id);
        }

        [Test]
        public void TestHoldoutConfig_MultipleHoldoutsPerRule()
        {
            // Test multiple holdouts targeting the same rule
            var holdouts = new[]
            {
                CreateHoldout("h1", "local1", new[] { "rule1" }),
                CreateHoldout("h2", "local2", new[] { "rule1" }),
                CreateHoldout("h3", "local3", new[] { "rule1", "rule2" })
            };

            var config = new HoldoutConfig(holdouts);

            var rule1Holdouts = config.GetHoldoutsForRule("rule1");
            Assert.AreEqual(3, rule1Holdouts.Count);
            Assert.IsTrue(rule1Holdouts.Any(h => h.Id == "h1"));
            Assert.IsTrue(rule1Holdouts.Any(h => h.Id == "h2"));
            Assert.IsTrue(rule1Holdouts.Any(h => h.Id == "h3"));
        }

        [Test]
        public void TestHoldoutConfig_PrecedenceGlobalBeforeLocal()
        {
            // Verify that global holdouts and local holdouts are separate
            var holdouts = new[]
            {
                CreateHoldout("h_global", "global", null),
                CreateHoldout("h_local", "local", new[] { "rule1" })
            };

            var config = new HoldoutConfig(holdouts);

            var globalHoldouts = config.GetGlobalHoldouts();
            Assert.AreEqual(1, globalHoldouts.Count);
            Assert.AreEqual("h_global", globalHoldouts[0].Id);

            var rule1Holdouts = config.GetHoldoutsForRule("rule1");
            Assert.AreEqual(1, rule1Holdouts.Count);
            Assert.AreEqual("h_local", rule1Holdouts[0].Id);

            // Global and local are distinct - global doesn't appear in rule map
            Assert.IsFalse(rule1Holdouts.Any(h => h.Id == "h_global"));
        }

        [Test]
        public void TestGetHoldout_ReturnsCorrectHoldout()
        {
            var holdouts = new[]
            {
                CreateHoldout("h1", "global1", null),
                CreateHoldout("h2", "local1", new[] { "rule1" })
            };

            var config = new HoldoutConfig(holdouts);

            var h1 = config.GetHoldout("h1");
            Assert.IsNotNull(h1);
            Assert.AreEqual("h1", h1.Id);
            Assert.AreEqual("global1", h1.Key);

            var h2 = config.GetHoldout("h2");
            Assert.IsNotNull(h2);
            Assert.AreEqual("h2", h2.Id);
            Assert.AreEqual("local1", h2.Key);
        }

        [Test]
        public void TestHoldoutCount_ReturnsCorrectCount()
        {
            var holdouts = new[]
            {
                CreateHoldout("h1", "global1", null),
                CreateHoldout("h2", "local1", new[] { "rule1" }),
                CreateHoldout("h3", "global2", null)
            };

            var config = new HoldoutConfig(holdouts);

            Assert.AreEqual(3, config.HoldoutCount);
        }

        [Test]
        public void TestGlobalHoldoutCount_ReturnsCorrectCount()
        {
            var holdouts = new[]
            {
                CreateHoldout("h1", "global1", null),
                CreateHoldout("h2", "local1", new[] { "rule1" }),
                CreateHoldout("h3", "global2", null)
            };

            var config = new HoldoutConfig(holdouts);

            Assert.AreEqual(2, config.GlobalHoldoutCount);
        }

        // Helper method to create a test holdout
        private Holdout CreateHoldout(string id, string key, string[] includedRules)
        {
            return new Holdout
            {
                Id = id,
                Key = key,
                Status = "Running",
                Variations = new Variation[0],
                TrafficAllocation = new TrafficAllocation[0],
                AudienceIds = new string[0],
                IncludedRules = includedRules
            };
        }
    }
}
