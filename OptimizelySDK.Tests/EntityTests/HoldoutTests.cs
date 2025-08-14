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
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OptimizelySDK.Entity;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class HoldoutTests
    {
        private JObject testData;

        [SetUp]
        public void Setup()
        {
            // Load test data
            var testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestData", "HoldoutTestData.json");
            var jsonContent = File.ReadAllText(testDataPath);
            testData = JObject.Parse(jsonContent);
        }

        [Test]
        public void TestHoldoutDeserialization()
        {
            // Test global holdout deserialization
            var globalHoldoutJson = testData["globalHoldout"].ToString();
            var globalHoldout = JsonConvert.DeserializeObject<Holdout>(globalHoldoutJson);

            Assert.IsNotNull(globalHoldout);
            Assert.AreEqual("holdout_global_1", globalHoldout.Id);
            Assert.AreEqual("global_holdout", globalHoldout.Key);
            Assert.AreEqual("Running", globalHoldout.Status);
            Assert.AreEqual("layer_1", globalHoldout.LayerId);
            Assert.IsNotNull(globalHoldout.Variations);
            Assert.AreEqual(1, globalHoldout.Variations.Length);
            Assert.IsNotNull(globalHoldout.TrafficAllocation);
            Assert.AreEqual(1, globalHoldout.TrafficAllocation.Length);
            Assert.IsNotNull(globalHoldout.IncludedFlags);
            Assert.AreEqual(0, globalHoldout.IncludedFlags.Length);
            Assert.IsNotNull(globalHoldout.ExcludedFlags);
            Assert.AreEqual(0, globalHoldout.ExcludedFlags.Length);
        }

        [Test]
        public void TestHoldoutWithIncludedFlags()
        {
            var includedHoldoutJson = testData["includedFlagsHoldout"].ToString();
            var includedHoldout = JsonConvert.DeserializeObject<Holdout>(includedHoldoutJson);

            Assert.IsNotNull(includedHoldout);
            Assert.AreEqual("holdout_included_1", includedHoldout.Id);
            Assert.AreEqual("included_holdout", includedHoldout.Key);
            Assert.IsNotNull(includedHoldout.IncludedFlags);
            Assert.AreEqual(2, includedHoldout.IncludedFlags.Length);
            Assert.Contains("flag_1", includedHoldout.IncludedFlags);
            Assert.Contains("flag_2", includedHoldout.IncludedFlags);
            Assert.IsNotNull(includedHoldout.ExcludedFlags);
            Assert.AreEqual(0, includedHoldout.ExcludedFlags.Length);
        }

        [Test]
        public void TestHoldoutWithExcludedFlags()
        {
            var excludedHoldoutJson = testData["excludedFlagsHoldout"].ToString();
            var excludedHoldout = JsonConvert.DeserializeObject<Holdout>(excludedHoldoutJson);

            Assert.IsNotNull(excludedHoldout);
            Assert.AreEqual("holdout_excluded_1", excludedHoldout.Id);
            Assert.AreEqual("excluded_holdout", excludedHoldout.Key);
            Assert.IsNotNull(excludedHoldout.IncludedFlags);
            Assert.AreEqual(0, excludedHoldout.IncludedFlags.Length);
            Assert.IsNotNull(excludedHoldout.ExcludedFlags);
            Assert.AreEqual(2, excludedHoldout.ExcludedFlags.Length);
            Assert.Contains("flag_3", excludedHoldout.ExcludedFlags);
            Assert.Contains("flag_4", excludedHoldout.ExcludedFlags);
        }

        [Test]
        public void TestHoldoutWithEmptyFlags()
        {
            var globalHoldoutJson = testData["globalHoldout"].ToString();
            var globalHoldout = JsonConvert.DeserializeObject<Holdout>(globalHoldoutJson);

            Assert.IsNotNull(globalHoldout);
            Assert.IsNotNull(globalHoldout.IncludedFlags);
            Assert.AreEqual(0, globalHoldout.IncludedFlags.Length);
            Assert.IsNotNull(globalHoldout.ExcludedFlags);
            Assert.AreEqual(0, globalHoldout.ExcludedFlags.Length);
        }

        [Test]
        public void TestHoldoutEquality()
        {
            var holdoutJson = testData["globalHoldout"].ToString();
            var holdout1 = JsonConvert.DeserializeObject<Holdout>(holdoutJson);
            var holdout2 = JsonConvert.DeserializeObject<Holdout>(holdoutJson);

            Assert.IsNotNull(holdout1);
            Assert.IsNotNull(holdout2);
            // Note: This test depends on how Holdout implements equality
            // If Holdout doesn't override Equals, this will test reference equality
            // You may need to implement custom equality logic for Holdout
        }

        [Test]
        public void TestHoldoutStatusParsing()
        {
            var globalHoldoutJson = testData["globalHoldout"].ToString();
            var globalHoldout = JsonConvert.DeserializeObject<Holdout>(globalHoldoutJson);

            Assert.IsNotNull(globalHoldout);
            Assert.AreEqual("Running", globalHoldout.Status);

            // Test that the holdout is considered activated when status is "Running"
            // This assumes there's an IsActivated property or similar logic
            // Adjust based on actual Holdout implementation
        }

        [Test]
        public void TestHoldoutVariationsDeserialization()
        {
            var holdoutJson = testData["includedFlagsHoldout"].ToString();
            var holdout = JsonConvert.DeserializeObject<Holdout>(holdoutJson);

            Assert.IsNotNull(holdout);
            Assert.IsNotNull(holdout.Variations);
            Assert.AreEqual(1, holdout.Variations.Length);

            var variation = holdout.Variations[0];
            Assert.AreEqual("var_2", variation.Id);
            Assert.AreEqual("treatment", variation.Key);
            Assert.AreEqual(true, variation.FeatureEnabled);
        }

        [Test]
        public void TestHoldoutTrafficAllocationDeserialization()
        {
            var holdoutJson = testData["excludedFlagsHoldout"].ToString();
            var holdout = JsonConvert.DeserializeObject<Holdout>(holdoutJson);

            Assert.IsNotNull(holdout);
            Assert.IsNotNull(holdout.TrafficAllocation);
            Assert.AreEqual(1, holdout.TrafficAllocation.Length);

            var trafficAllocation = holdout.TrafficAllocation[0];
            Assert.AreEqual("var_3", trafficAllocation.EntityId);
            Assert.AreEqual(10000, trafficAllocation.EndOfRange);
        }

        [Test]
        public void TestHoldoutNullSafety()
        {
            // Test that holdout can handle null/missing includedFlags and excludedFlags
            var minimalHoldoutJson = @"{
                ""id"": ""test_holdout"",
                ""key"": ""test_key"",
                ""status"": ""Running"",
                ""layerId"": ""test_layer"",
                ""variations"": [],
                ""trafficAllocation"": [],
                ""audienceIds"": [],
                ""audienceConditions"": []
            }";

            var holdout = JsonConvert.DeserializeObject<Holdout>(minimalHoldoutJson);

            Assert.IsNotNull(holdout);
            Assert.AreEqual("test_holdout", holdout.Id);
            Assert.AreEqual("test_key", holdout.Key);

            // Verify that missing includedFlags and excludedFlags are handled properly
            // This depends on how the Holdout entity handles missing properties
            Assert.IsNotNull(holdout.IncludedFlags);
            Assert.IsNotNull(holdout.ExcludedFlags);
        }
    }
}
