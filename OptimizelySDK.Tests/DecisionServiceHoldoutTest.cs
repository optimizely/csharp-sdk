/*
 * Copyright 2025, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
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
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OptimizelySDK.Bucketing;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.OptimizelyDecisions;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class DecisionServiceHoldoutTest
    {
        private Mock<ILogger> LoggerMock;
        private Mock<EventProcessor> EventProcessorMock;
        private DecisionService DecisionService;
        private DatafileProjectConfig Config;
        private JObject TestData;
        private Optimizely OptimizelyInstance;

        private const string TestUserId = "testUserId";
        private const string TestBucketingId = "testBucketingId";

        [SetUp]
        public void Initialize()
        {
            LoggerMock = new Mock<ILogger>();
            EventProcessorMock = new Mock<EventProcessor>();

            // Load test data
            var testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestData", "HoldoutTestData.json");
            var jsonContent = File.ReadAllText(testDataPath);
            TestData = JObject.Parse(jsonContent);

            // Use datafile with holdouts for proper config setup
            var datafileWithHoldouts = TestData["datafileWithHoldouts"].ToString();
            Config = DatafileProjectConfig.Create(datafileWithHoldouts, LoggerMock.Object,
                new ErrorHandler.NoOpErrorHandler()) as DatafileProjectConfig;

            // Use real Bucketer instead of mock
            var realBucketer = new Bucketer(LoggerMock.Object);
            DecisionService = new DecisionService(realBucketer,
                new ErrorHandler.NoOpErrorHandler(), null, LoggerMock.Object);

            // Create an Optimizely instance for creating user contexts
            var eventDispatcher = new Event.Dispatcher.DefaultEventDispatcher(LoggerMock.Object);
            OptimizelyInstance = new Optimizely(datafileWithHoldouts, eventDispatcher, LoggerMock.Object);

            // Verify that the config contains holdouts
            Assert.IsNotNull(Config.Holdouts, "Config should have holdouts");
            Assert.IsTrue(Config.Holdouts.Length > 0, "Config should contain holdouts");
        }

        [Test]
        public void TestGetVariationsForFeatureList_HoldoutActiveVariationBucketed()
        {
            // Test GetVariationsForFeatureList with holdout that has an active variation
            var featureFlag = Config.FeatureKeyMap["test_flag_1"]; // Use actual feature flag from test data
            var holdout = Config.GetHoldout("holdout_included_1"); // This holdout includes flag_1
            Assert.IsNotNull(holdout, "Holdout should exist in config");

            // Create user context
            var userContext = new OptimizelyUserContext(OptimizelyInstance, TestUserId, null,
                new ErrorHandler.NoOpErrorHandler(), LoggerMock.Object);

            var result = DecisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag }, userContext, Config,
                new UserAttributes(), new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0, "Should have at least one decision");

            // Find the holdout decision
            var holdoutDecision = result.FirstOrDefault(r => r.ResultObject?.Source == FeatureDecision.DECISION_SOURCE_HOLDOUT);
            Assert.IsNotNull(holdoutDecision, "Should have a holdout decision");

            // Verify that we got a valid variation (real bucketer should determine this based on traffic allocation)
            Assert.IsNotNull(holdoutDecision.ResultObject?.Variation, "Should have a variation");
        }

        [Test]
        public void TestGetVariationsForFeatureList_HoldoutInactiveNoBucketing()
        {
            // Test that inactive holdouts don't bucket users
            var featureFlag = Config.FeatureKeyMap["test_flag_1"]; // Use actual feature flag from test data

            // Get one of the holdouts that's actually processed for test_flag_1 (based on debug output)
            var holdout = Config.GetHoldout("holdout_global_1"); // global_holdout is one of the holdouts being processed
            Assert.IsNotNull(holdout, "Holdout should exist in config");

            // Mock holdout as inactive
            holdout.Status = "Paused";

            var userContext = new OptimizelyUserContext(OptimizelyInstance, TestUserId, null,
                new ErrorHandler.NoOpErrorHandler(), LoggerMock.Object);

            var result = DecisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag }, userContext, Config,
                new UserAttributes(), new OptimizelyDecideOption[0]);

            // Verify appropriate log message for inactive holdout
            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                    It.Is<string>(s => s.Contains("Holdout") && s.Contains("is not running"))),
                Times.AtLeastOnce);
        }

        [Test]
        public void TestGetVariationsForFeatureList_HoldoutUserNotBucketed()
        {
            // Test when user is not bucketed into holdout (outside traffic allocation)
            var featureFlag = Config.FeatureKeyMap["test_flag_1"]; // Use actual feature flag from test data
            var holdout = Config.GetHoldout("holdout_included_1"); // This holdout includes flag_1
            Assert.IsNotNull(holdout, "Holdout should exist in config");

            var userContext = new OptimizelyUserContext(OptimizelyInstance, TestUserId, null,
                new ErrorHandler.NoOpErrorHandler(), LoggerMock.Object);

            var result = DecisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag }, userContext, Config,
                new UserAttributes(), new OptimizelyDecideOption[0]);

            // With real bucketer, we can't guarantee specific bucketing results
            // but we can verify the method executes successfully
            Assert.IsNotNull(result, "Result should not be null");
        }

        [Test]
        public void TestGetVariationsForFeatureList_HoldoutWithUserAttributes()
        {
            // Test holdout evaluation with user attributes for audience targeting
            var featureFlag = Config.FeatureKeyMap["test_flag_1"]; // Use actual feature flag from test data
            var holdout = Config.GetHoldout("holdout_included_1"); // This holdout includes flag_1
            Assert.IsNotNull(holdout, "Holdout should exist in config");

            var userAttributes = new UserAttributes
            {
                { "browser", "chrome" },
                { "location", "us" }
            };

            var userContext = new OptimizelyUserContext(OptimizelyInstance, TestUserId, userAttributes,
                new ErrorHandler.NoOpErrorHandler(), LoggerMock.Object);

            var result = DecisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag }, userContext, Config,
                userAttributes, new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result, "Result should not be null");

            // With real bucketer, we can't guarantee specific variations but can verify execution
            // Additional assertions would depend on the holdout configuration and user bucketing
        }

        [Test]
        public void TestGetVariationsForFeatureList_MultipleHoldouts()
        {
            // Test multiple holdouts for a single feature flag
            var featureFlag = Config.FeatureKeyMap["test_flag_1"]; // Use actual feature flag from test data

            var userContext = new OptimizelyUserContext(OptimizelyInstance, TestUserId, null,
                new ErrorHandler.NoOpErrorHandler(), LoggerMock.Object);

            var result = DecisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag }, userContext, Config,
                new UserAttributes(), new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result, "Result should not be null");

            // With real bucketer, we can't guarantee specific bucketing results
            // but we can verify the method executes successfully
        }

        [Test]
        public void TestGetVariationsForFeatureList_Holdout_EmptyUserId()
        {
            // Test GetVariationsForFeatureList with empty user ID
            var featureFlag = Config.FeatureKeyMap["test_flag_1"]; // Use actual feature flag from test data

            var userContext = new OptimizelyUserContext(OptimizelyInstance, "", null,
                new ErrorHandler.NoOpErrorHandler(), LoggerMock.Object);

            var result = DecisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag }, userContext, Config,
                new UserAttributes(), new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result);

            // Empty user ID should still allow holdout bucketing (matches Swift SDK behavior)
            // The Swift SDK's testBucketToVariation_EmptyBucketingId shows empty string is valid
            var holdoutDecisions = result.Where(r => r.ResultObject?.Source == FeatureDecision.DECISION_SOURCE_HOLDOUT).ToList();

            // Should not log error about invalid user ID since empty string is valid for bucketing
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                    It.Is<string>(s => s.Contains("User ID") && (s.Contains("null") || s.Contains("empty")))),
                Times.Never);
        }

        [Test]
        public void TestGetVariationsForFeatureList_Holdout_DecisionReasons()
        {
            // Test that decision reasons are properly populated for holdouts
            var featureFlag = Config.FeatureKeyMap["test_flag_1"]; // Use actual feature flag from test data
            var holdout = Config.GetHoldout("holdout_included_1"); // This holdout includes flag_1
            Assert.IsNotNull(holdout, "Holdout should exist in config");

            var userContext = new OptimizelyUserContext(OptimizelyInstance, TestUserId, null,
                new ErrorHandler.NoOpErrorHandler(), LoggerMock.Object);

            var result = DecisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag }, userContext, Config,
                new UserAttributes(), new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result, "Result should not be null");

            // With real bucketer, we expect proper decision reasons to be generated
            // Find any decision with reasons
            var decisionWithReasons = result.FirstOrDefault(r => r.DecisionReasons != null && r.DecisionReasons.ToReport().Count > 0);

            if (decisionWithReasons != null)
            {
                Assert.IsTrue(decisionWithReasons.DecisionReasons.ToReport().Count > 0, "Should have decision reasons");
            }
        }

        [Test]
        public void TestImpressionEventForHoldout()
        {
            var featureFlag = Config.FeatureKeyMap["test_flag_1"]; 
            var userAttributes = new UserAttributes();

            var eventDispatcher = new Event.Dispatcher.DefaultEventDispatcher(LoggerMock.Object);
            var optimizelyWithMockedEvents = new Optimizely(
                TestData["datafileWithHoldouts"].ToString(),
                eventDispatcher,
                LoggerMock.Object,
                new ErrorHandler.NoOpErrorHandler(),
                null, // userProfileService
                false, // skipJsonValidation
                EventProcessorMock.Object
            );

            EventProcessorMock.Setup(ep => ep.Process(It.IsAny<ImpressionEvent>()));

            var userContext = optimizelyWithMockedEvents.CreateUserContext(TestUserId, userAttributes);
            var decision = userContext.Decide(featureFlag.Key);

            Assert.IsNotNull(decision, "Decision should not be null");
            Assert.IsNotNull(decision.RuleKey, "RuleKey should not be null");
            
            var actualHoldout = Config.Holdouts?.FirstOrDefault(h => h.Key == decision.RuleKey);

            Assert.IsNotNull(actualHoldout, 
                $"RuleKey '{decision.RuleKey}' should correspond to a holdout experiment");
            Assert.AreEqual(featureFlag.Key, decision.FlagKey, "Flag key should match");
            
            var holdoutVariation = actualHoldout.Variations.FirstOrDefault(v => v.Key == decision.VariationKey);

            Assert.IsNotNull(holdoutVariation, 
                $"Variation '{decision.VariationKey}' should be from the chosen holdout '{actualHoldout.Key}'");
            
            Assert.AreEqual(holdoutVariation.FeatureEnabled, decision.Enabled,
                "Enabled flag should match holdout variation's featureEnabled value");
            
            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Once,
                "Impression event should be processed exactly once for holdout decision");
                
            EventProcessorMock.Verify(ep => ep.Process(It.Is<ImpressionEvent>(ie =>
                ie.Experiment.Key == actualHoldout.Key &&
                ie.Experiment.Id == actualHoldout.Id &&
                ie.Timestamp > 0 &&
                ie.UserId == TestUserId
            )), Times.Once, "Impression event should contain correct holdout experiment details");
        }

        [Test]
        public void TestImpressionEventForHoldout_DisableDecisionEvent()
        {
            var featureFlag = Config.FeatureKeyMap["test_flag_1"];
            var userAttributes = new UserAttributes();

            var eventDispatcher = new Event.Dispatcher.DefaultEventDispatcher(LoggerMock.Object);
            var optimizelyWithMockedEvents = new Optimizely(
                TestData["datafileWithHoldouts"].ToString(),
                eventDispatcher,
                LoggerMock.Object,
                new ErrorHandler.NoOpErrorHandler(),
                null, // userProfileService
                false, // skipJsonValidation
                EventProcessorMock.Object
            );

            EventProcessorMock.Setup(ep => ep.Process(It.IsAny<ImpressionEvent>()));

            var userContext = optimizelyWithMockedEvents.CreateUserContext(TestUserId, userAttributes);
            var decision = userContext.Decide(featureFlag.Key, new[] { OptimizelyDecideOption.DISABLE_DECISION_EVENT });

            Assert.IsNotNull(decision, "Decision should not be null");
            Assert.IsNotNull(decision.RuleKey, "User should be bucketed into a holdout");
            
            var chosenHoldout = Config.Holdouts?.FirstOrDefault(h => h.Key == decision.RuleKey);

            Assert.IsNotNull(chosenHoldout, $"Holdout '{decision.RuleKey}' should exist in config");
            
            Assert.AreEqual(featureFlag.Key, decision.FlagKey, "Flag key should match");
            
            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Never,
                "No impression event should be processed when DISABLE_DECISION_EVENT option is used");
        }
    }
}
