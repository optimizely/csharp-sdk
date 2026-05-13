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

using Newtonsoft.Json;
using NUnit.Framework;
using OptimizelySDK.Entity;

namespace OptimizelySDK.Tests.EntityTests
{
    [TestFixture]
    public class LocalHoldoutTests
    {
        [Test]
        public void TestGlobalHoldout_IncludedRulesNull()
        {
            // Test global holdout with IncludedRules = null
            var globalHoldoutJson = @"{
                ""id"": ""holdout_global_1"",
                ""key"": ""global_holdout"",
                ""status"": ""Running"",
                ""variations"": [],
                ""trafficAllocation"": [],
                ""audienceIds"": [],
                ""audienceConditions"": []
            }";

            var holdout = JsonConvert.DeserializeObject<Holdout>(globalHoldoutJson);

            Assert.IsNotNull(holdout);
            Assert.AreEqual("holdout_global_1", holdout.Id);
            Assert.IsNull(holdout.IncludedRules, "IncludedRules should be null for global holdout");
            Assert.IsTrue(holdout.IsGlobal(), "IsGlobal() should return true when IncludedRules is null");
        }

        [Test]
        public void TestLocalHoldout_SingleRule()
        {
            // Test local holdout with single rule
            var localHoldoutJson = @"{
                ""id"": ""holdout_local_1"",
                ""key"": ""local_holdout_single"",
                ""status"": ""Running"",
                ""variations"": [],
                ""trafficAllocation"": [],
                ""audienceIds"": [],
                ""audienceConditions"": [],
                ""includedRules"": [""rule_123""]
            }";

            var holdout = JsonConvert.DeserializeObject<Holdout>(localHoldoutJson);

            Assert.IsNotNull(holdout);
            Assert.AreEqual("holdout_local_1", holdout.Id);
            Assert.IsNotNull(holdout.IncludedRules);
            Assert.AreEqual(1, holdout.IncludedRules.Length);
            Assert.AreEqual("rule_123", holdout.IncludedRules[0]);
            Assert.IsFalse(holdout.IsGlobal(), "IsGlobal() should return false when IncludedRules is not null");
        }

        [Test]
        public void TestLocalHoldout_MultipleRules()
        {
            // Test local holdout with multiple rules
            var localHoldoutJson = @"{
                ""id"": ""holdout_local_2"",
                ""key"": ""local_holdout_multi"",
                ""status"": ""Running"",
                ""variations"": [],
                ""trafficAllocation"": [],
                ""audienceIds"": [],
                ""audienceConditions"": [],
                ""includedRules"": [""rule_123"", ""rule_456"", ""rule_789""]
            }";

            var holdout = JsonConvert.DeserializeObject<Holdout>(localHoldoutJson);

            Assert.IsNotNull(holdout);
            Assert.IsNotNull(holdout.IncludedRules);
            Assert.AreEqual(3, holdout.IncludedRules.Length);
            Assert.Contains("rule_123", holdout.IncludedRules);
            Assert.Contains("rule_456", holdout.IncludedRules);
            Assert.Contains("rule_789", holdout.IncludedRules);
            Assert.IsFalse(holdout.IsGlobal());
        }

        [Test]
        public void TestLocalHoldout_EmptyArray()
        {
            // Test local holdout with empty IncludedRules array (edge case)
            var localHoldoutJson = @"{
                ""id"": ""holdout_local_empty"",
                ""key"": ""local_holdout_empty"",
                ""status"": ""Running"",
                ""variations"": [],
                ""trafficAllocation"": [],
                ""audienceIds"": [],
                ""audienceConditions"": [],
                ""includedRules"": []
            }";

            var holdout = JsonConvert.DeserializeObject<Holdout>(localHoldoutJson);

            Assert.IsNotNull(holdout);
            Assert.IsNotNull(holdout.IncludedRules, "IncludedRules should not be null");
            Assert.AreEqual(0, holdout.IncludedRules.Length, "IncludedRules should be empty array");
            Assert.IsFalse(holdout.IsGlobal(), "IsGlobal() should return false for empty array (different from null)");
        }

        [Test]
        public void TestHoldout_MissingIncludedRulesField()
        {
            // Test that missing IncludedRules field defaults to null (global holdout)
            var holdoutJson = @"{
                ""id"": ""holdout_missing_field"",
                ""key"": ""holdout_no_field"",
                ""status"": ""Running"",
                ""variations"": [],
                ""trafficAllocation"": [],
                ""audienceIds"": [],
                ""audienceConditions"": []
            }";

            var holdout = JsonConvert.DeserializeObject<Holdout>(holdoutJson);

            Assert.IsNotNull(holdout);
            Assert.IsNull(holdout.IncludedRules, "Missing IncludedRules field should default to null");
            Assert.IsTrue(holdout.IsGlobal(), "Holdout without IncludedRules field should be global");
        }

        [Test]
        public void TestHoldout_CrossFlagTargeting()
        {
            // Test local holdout targeting rules from different flags (cross-flag)
            var crossFlagHoldoutJson = @"{
                ""id"": ""holdout_cross_flag"",
                ""key"": ""cross_flag_holdout"",
                ""status"": ""Running"",
                ""variations"": [],
                ""trafficAllocation"": [],
                ""audienceIds"": [],
                ""audienceConditions"": [],
                ""includedRules"": [""flag1_rule1"", ""flag2_rule1"", ""flag3_rule1""]
            }";

            var holdout = JsonConvert.DeserializeObject<Holdout>(crossFlagHoldoutJson);

            Assert.IsNotNull(holdout);
            Assert.IsNotNull(holdout.IncludedRules);
            Assert.AreEqual(3, holdout.IncludedRules.Length);
            Assert.IsFalse(holdout.IsGlobal());
            // Verify it contains rules from different flags (cross-flag targeting)
            Assert.Contains("flag1_rule1", holdout.IncludedRules);
            Assert.Contains("flag2_rule1", holdout.IncludedRules);
            Assert.Contains("flag3_rule1", holdout.IncludedRules);
        }

        [Test]
        public void TestHoldout_NullVsEmptyDifference()
        {
            // Verify that null and empty array are treated differently
            var globalJson = @"{""id"":""h1"", ""key"":""k1"", ""status"":""Running"", ""variations"":[], ""trafficAllocation"":[], ""audienceIds"":[], ""audienceConditions"":[]}";
            var localEmptyJson = @"{""id"":""h2"", ""key"":""k2"", ""status"":""Running"", ""variations"":[], ""trafficAllocation"":[], ""audienceIds"":[], ""audienceConditions"":[], ""includedRules"":[]}";

            var globalHoldout = JsonConvert.DeserializeObject<Holdout>(globalJson);
            var localEmptyHoldout = JsonConvert.DeserializeObject<Holdout>(localEmptyJson);

            Assert.IsNull(globalHoldout.IncludedRules);
            Assert.IsTrue(globalHoldout.IsGlobal());

            Assert.IsNotNull(localEmptyHoldout.IncludedRules);
            Assert.AreEqual(0, localEmptyHoldout.IncludedRules.Length);
            Assert.IsFalse(localEmptyHoldout.IsGlobal());

            // These should be treated differently: null = global, empty = local with no rules
            Assert.AreNotEqual(globalHoldout.IsGlobal(), localEmptyHoldout.IsGlobal());
        }
    }
}
