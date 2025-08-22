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

using System.IO;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OptimizelySDK.Bucketing;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.OptimizelyDecisions;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class BucketerHoldoutTest
    {
        private Mock<ILogger> LoggerMock;
        private Bucketer Bucketer;
        private TestBucketer TestBucketer;
        private ProjectConfig Config;
        private JObject TestData;
        private const string TestUserId = "test_user_id";
        private const string TestBucketingId = "test_bucketing_id";

        [SetUp]
        public void Initialize()
        {
            LoggerMock = new Mock<ILogger>();
            
            // Load holdout test data
            var testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestData", "HoldoutTestData.json");
            var jsonContent = File.ReadAllText(testDataPath);
            TestData = JObject.Parse(jsonContent);
            
            // Use datafile with holdouts for proper config setup
            var datafileWithHoldouts = TestData["datafileWithHoldouts"].ToString();
            Config = DatafileProjectConfig.Create(datafileWithHoldouts, LoggerMock.Object,
                new ErrorHandler.NoOpErrorHandler());
            TestBucketer = new TestBucketer(LoggerMock.Object);
            
            // Verify that the config contains holdouts
            Assert.IsNotNull(Config.Holdouts, "Config should have holdouts");
            Assert.IsTrue(Config.Holdouts.Length > 0, "Config should contain holdouts");
        }

        [Test]
        public void TestBucketHoldout_ValidTrafficAllocation()
        {
            // Test user bucketed within traffic allocation range
            // Use the global holdout from config which has multiple variations
            var holdout = Config.GetHoldout("holdout_global_1");
            Assert.IsNotNull(holdout, "Holdout should exist in config");

            // Set bucket value to be within first variation's traffic allocation (0-5000 range)
            TestBucketer.SetBucketValues(new[] { 2500 });

            var result = TestBucketer.Bucket(Config, holdout, TestBucketingId, TestUserId);

            Assert.IsNotNull(result.ResultObject);
            Assert.AreEqual("var_1", result.ResultObject.Id);
            Assert.AreEqual("control", result.ResultObject.Key);
            
            // Verify logging
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, 
                It.Is<string>(s => s.Contains($"Assigned bucket [2500] to user [{TestUserId}]"))), 
                Times.Once);
        }

        [Test]
        public void TestBucketHoldout_UserOutsideAllocation()
        {
            // Test user not bucketed when outside traffic allocation range
            var holdoutJson = TestData["globalHoldout"].ToString();
            var holdout = JsonConvert.DeserializeObject<Holdout>(holdoutJson);
            
            // Modify traffic allocation to be smaller (0-1000 range = 10%)
            holdout.TrafficAllocation[0].EndOfRange = 1000;

            // Set bucket value outside traffic allocation range
            TestBucketer.SetBucketValues(new[] { 1500 });

            var result = TestBucketer.Bucket(Config, holdout, TestBucketingId, TestUserId);

            Assert.IsNull(result.ResultObject.Id);
            Assert.IsNull(result.ResultObject.Key);
            
            // Verify user was assigned bucket value but no variation was found
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, 
                It.Is<string>(s => s.Contains($"Assigned bucket [1500] to user [{TestUserId}]"))), 
                Times.Once);
        }

        [Test]
        public void TestBucketHoldout_NoTrafficAllocation()
        {
            // Test holdout with empty traffic allocation
            var holdoutJson = TestData["globalHoldout"].ToString();
            var holdout = JsonConvert.DeserializeObject<Holdout>(holdoutJson);
            
            // Clear traffic allocation
            holdout.TrafficAllocation = new TrafficAllocation[0];

            TestBucketer.SetBucketValues(new[] { 5000 });

            var result = TestBucketer.Bucket(Config, holdout, TestBucketingId, TestUserId);

            Assert.IsNull(result.ResultObject.Id);
            Assert.IsNull(result.ResultObject.Key);
            
            // Verify bucket was assigned but no variation found
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, 
                It.Is<string>(s => s.Contains($"Assigned bucket [5000] to user [{TestUserId}]"))), 
                Times.Once);
        }

        [Test]
        public void TestBucketHoldout_InvalidVariationId()
        {
            // Test holdout with invalid variation ID in traffic allocation
            var holdoutJson = TestData["globalHoldout"].ToString();
            var holdout = JsonConvert.DeserializeObject<Holdout>(holdoutJson);
            
            // Set traffic allocation to point to non-existent variation
            holdout.TrafficAllocation[0].EntityId = "invalid_variation_id";

            TestBucketer.SetBucketValues(new[] { 5000 });

            var result = TestBucketer.Bucket(Config, holdout, TestBucketingId, TestUserId);

            Assert.IsNull(result.ResultObject.Id);
            Assert.IsNull(result.ResultObject.Key);
            
            // Verify bucket was assigned
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, 
                It.Is<string>(s => s.Contains($"Assigned bucket [5000] to user [{TestUserId}]"))), 
                Times.Once);
        }

        [Test]
        public void TestBucketHoldout_EmptyVariations()
        {
            // Test holdout with no variations - use holdout from datafile that has no variations
            var holdout = Config.GetHoldout("holdout_empty_1");
            Assert.IsNotNull(holdout, "Empty holdout should exist in config");
            Assert.AreEqual(0, holdout.Variations?.Length ?? 0, "Holdout should have no variations");

            TestBucketer.SetBucketValues(new[] { 5000 });

            var result = TestBucketer.Bucket(Config, holdout, TestBucketingId, TestUserId);

            Assert.IsNull(result.ResultObject.Id);
            Assert.IsNull(result.ResultObject.Key);
            
            // Verify bucket was assigned
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, 
                It.Is<string>(s => s.Contains($"Assigned bucket [5000] to user [{TestUserId}]"))), 
                Times.Once);
        }

        [Test]
        public void TestBucketHoldout_EmptyExperimentKey()
        {
            // Test holdout with empty key
            var holdoutJson = TestData["globalHoldout"].ToString();
            var holdout = JsonConvert.DeserializeObject<Holdout>(holdoutJson);
            
            // Clear holdout key
            holdout.Key = "";

            TestBucketer.SetBucketValues(new[] { 5000 });

            var result = TestBucketer.Bucket(Config, holdout, TestBucketingId, TestUserId);

            // Should return empty variation for invalid experiment key
            Assert.IsNotNull(result.ResultObject);
            Assert.IsNull(result.ResultObject.Id);
            Assert.IsNull(result.ResultObject.Key);
        }

        [Test]
        public void TestBucketHoldout_NullExperimentKey()
        {
            // Test holdout with null key
            var holdoutJson = TestData["globalHoldout"].ToString();
            var holdout = JsonConvert.DeserializeObject<Holdout>(holdoutJson);
            
            // Set holdout key to null
            holdout.Key = null;

            TestBucketer.SetBucketValues(new[] { 5000 });

            var result = TestBucketer.Bucket(Config, holdout, TestBucketingId, TestUserId);

            // Should return empty variation for null experiment key
            Assert.IsNotNull(result.ResultObject);
            Assert.IsNull(result.ResultObject.Id);
            Assert.IsNull(result.ResultObject.Key);
        }

        [Test]
        public void TestBucketHoldout_MultipleVariationsInRange()
        {
            // Test holdout with multiple variations and user buckets into first one
            var holdoutJson = TestData["globalHoldout"].ToString();
            var holdout = JsonConvert.DeserializeObject<Holdout>(holdoutJson);
            
            // Add a second variation
            var variation2 = new Variation
            {
                Id = "var_2",
                Key = "treatment",
                FeatureEnabled = true
            };
            holdout.Variations = new[] { holdout.Variations[0], variation2 };
            
            // Set traffic allocation for first variation (0-5000) and second (5000-10000)
            holdout.TrafficAllocation = new[]
            {
                new TrafficAllocation { EntityId = "var_1", EndOfRange = 5000 },
                new TrafficAllocation { EntityId = "var_2", EndOfRange = 10000 }
            };

            // Test user buckets into first variation
            TestBucketer.SetBucketValues(new[] { 2500 });
            var result = TestBucketer.Bucket(Config, holdout, TestBucketingId, TestUserId);

            Assert.IsNotNull(result.ResultObject);
            Assert.AreEqual("var_1", result.ResultObject.Id);
            Assert.AreEqual("control", result.ResultObject.Key);
        }

        [Test]
        public void TestBucketHoldout_MultipleVariationsInSecondRange()
        {
            // Test holdout with multiple variations and user buckets into second one
            // Use the global holdout from config which now has multiple variations
            var holdout = Config.GetHoldout("holdout_global_1");
            Assert.IsNotNull(holdout, "Holdout should exist in config");
            
            // Verify holdout has multiple variations 
            Assert.IsTrue(holdout.Variations.Length >= 2, "Holdout should have multiple variations");
            Assert.AreEqual("var_1", holdout.Variations[0].Id);
            Assert.AreEqual("var_2", holdout.Variations[1].Id);

            // Test user buckets into second variation (bucket value 7500 should be in 5000-10000 range)
            TestBucketer.SetBucketValues(new[] { 7500 });
            var result = TestBucketer.Bucket(Config, holdout, TestBucketingId, TestUserId);

            Assert.IsNotNull(result.ResultObject);
            Assert.AreEqual("var_2", result.ResultObject.Id);
            Assert.AreEqual("treatment", result.ResultObject.Key);
        }

        [Test]
        public void TestBucketHoldout_EdgeCaseBoundaryValues()
        {
            // Test edge cases at traffic allocation boundaries
            var holdoutJson = TestData["globalHoldout"].ToString();
            var holdout = JsonConvert.DeserializeObject<Holdout>(holdoutJson);
            
            // Set traffic allocation to 5000 (50%)
            holdout.TrafficAllocation[0].EndOfRange = 5000;

            // Test exact boundary value (should be included)
            TestBucketer.SetBucketValues(new[] { 4999 });
            var result = TestBucketer.Bucket(Config, holdout, TestBucketingId, TestUserId);

            Assert.IsNotNull(result.ResultObject);
            Assert.AreEqual("var_1", result.ResultObject.Id);

            // Test value just outside boundary (should not be included)
            TestBucketer.SetBucketValues(new[] { 5000 });
            result = TestBucketer.Bucket(Config, holdout, TestBucketingId, TestUserId);

            Assert.IsNull(result.ResultObject.Id);
        }

        [Test]
        public void TestBucketHoldout_ConsistentBucketingWithSameInputs()
        {
            // Test that same inputs produce consistent results
            // Use holdout from config instead of creating at runtime
            var holdout = Config.GetHoldout("holdout_global_1");
            Assert.IsNotNull(holdout, "Holdout should exist in config");

            // Create a real bucketer (not test bucketer) for consistent hashing
            var realBucketer = new Bucketing.Bucketer(LoggerMock.Object);
            var result1 = realBucketer.Bucket(Config, holdout, TestBucketingId, TestUserId);
            var result2 = realBucketer.Bucket(Config, holdout, TestBucketingId, TestUserId);

            // Results should be identical
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
            
            if (result1.ResultObject?.Id != null)
            {
                Assert.AreEqual(result1.ResultObject.Id, result2.ResultObject.Id);
                Assert.AreEqual(result1.ResultObject.Key, result2.ResultObject.Key);
            }
            else
            {
                Assert.IsNull(result2.ResultObject?.Id);
            }
        }

        [Test]
        public void TestBucketHoldout_DifferentBucketingIdsProduceDifferentResults()
        {
            // Test that different bucketing IDs can produce different results
            // Use holdout from config instead of creating at runtime
            var holdout = Config.GetHoldout("holdout_global_1");
            Assert.IsNotNull(holdout, "Holdout should exist in config");

            // Create a real bucketer (not test bucketer) for real hashing behavior
            var realBucketer = new Bucketing.Bucketer(LoggerMock.Object);
            var result1 = realBucketer.Bucket(Config, holdout, "bucketingId1", TestUserId);
            var result2 = realBucketer.Bucket(Config, holdout, "bucketingId2", TestUserId);

            // Results may be different (though not guaranteed due to hashing)
            // This test mainly ensures no exceptions are thrown with different inputs
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
            Assert.IsNotNull(result1.ResultObject);
            Assert.IsNotNull(result2.ResultObject);
        }

        [Test]
        public void TestBucketHoldout_VerifyDecisionReasons()
        {
            // Test that decision reasons are properly populated
            var holdoutJson = TestData["globalHoldout"].ToString();
            var holdout = JsonConvert.DeserializeObject<Holdout>(holdoutJson);

            TestBucketer.SetBucketValues(new[] { 5000 });
            var result = TestBucketer.Bucket(Config, holdout, TestBucketingId, TestUserId);

            Assert.IsNotNull(result.DecisionReasons);
            // Decision reasons should be populated from the bucketing process
            // The exact content depends on whether the user was bucketed or not
        }

        [TearDown]
        public void TearDown()
        {
            LoggerMock = null;
            Bucketer = null;
            TestBucketer = null;
            Config = null;
            TestData = null;
        }
    }
}
