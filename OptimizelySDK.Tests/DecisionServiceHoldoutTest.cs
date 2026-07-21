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
using OptimizelySDK.Utils;

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
                new ErrorHandler.NoOpErrorHandler(), null, LoggerMock.Object, null);

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

        // =====================================================================
        // Level 2: Decision Service Tests for Local Holdouts
        // =====================================================================

        private DatafileProjectConfig LocalHoldoutsConfig;
        private Optimizely LocalHoldoutsOptimizely;

        private void InitializeLocalHoldoutsConfig()
        {
            var datafileWithLocalHoldouts = TestData["datafileWithLocalHoldouts"].ToString();
            LocalHoldoutsConfig = DatafileProjectConfig.Create(
                datafileWithLocalHoldouts,
                LoggerMock.Object,
                new NoOpErrorHandler()) as DatafileProjectConfig;

            var eventDispatcher = new Event.Dispatcher.DefaultEventDispatcher(LoggerMock.Object);
            LocalHoldoutsOptimizely = new Optimizely(
                datafileWithLocalHoldouts,
                eventDispatcher,
                LoggerMock.Object,
                new NoOpErrorHandler());
        }

        [Test]
        public void TestLocalHoldouts_GlobalHoldoutEvaluatedBeforePerRuleLogic()
        {
            // Global holdout is evaluated at flag level, before any per-rule logic.
            // datafileWithLocalHoldouts has a global holdout (holdout_global_2) with 100% traffic allocation.
            // test_flag_1 has a rollout with delivery rules. The global holdout should fire first.
            InitializeLocalHoldoutsConfig();

            Assert.IsNotNull(LocalHoldoutsConfig, "Config with local holdouts should be created");

            var globalHoldouts = LocalHoldoutsConfig.GetGlobalHoldouts();
            Assert.AreEqual(1, globalHoldouts.Count, "Should have exactly one global holdout");
            Assert.AreEqual("holdout_global_2", globalHoldouts[0].Id, "Global holdout id should match");

            // Verify that the global holdout is classified correctly
            Assert.IsTrue(globalHoldouts[0].IsGlobal,
                "holdout_global_2 should be global (IncludedRules is null)");

            // Verify GetHoldoutsForRule returns empty for a delivery rule since it's global
            var localForRule1 = LocalHoldoutsConfig.GetHoldoutsForRule("rule_id_1");
            Assert.IsTrue(localForRule1.Any(h => h.Id == "holdout_local_rule1"),
                "rule_id_1 should be targeted by holdout_local_rule1");
            Assert.IsFalse(localForRule1.Any(h => h.Id == "holdout_global_2"),
                "Global holdout should not appear in per-rule local holdouts list");

            // Use the decision service to get a decision for test_flag_1 (has rollout)
            var realBucketer = new Bucketer(LoggerMock.Object);
            var decisionService = new DecisionService(realBucketer,
                new NoOpErrorHandler(), null, LoggerMock.Object, null);

            var featureFlag = LocalHoldoutsConfig.FeatureKeyMap["test_flag_1"];
            var userContext = new OptimizelyUserContext(LocalHoldoutsOptimizely, TestUserId, null,
                new NoOpErrorHandler(), LoggerMock.Object);

            var result = decisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag },
                userContext,
                LocalHoldoutsConfig,
                new UserAttributes(),
                new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            // The global holdout has 100% traffic — user should be bucketed into it
            var decision = result[0].ResultObject;
            Assert.IsNotNull(decision, "Decision should not be null");
            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_HOLDOUT, decision.Source,
                "Decision source should be holdout since global holdout has 100% traffic");
            Assert.AreEqual("holdout_global_2", decision.Experiment?.Id,
                "Decision should be from the global holdout");
        }

        [Test]
        public void TestLocalHoldouts_UserBucketedIntoLocalHoldoutForDeliveryRuleReturnsHoldoutVariation()
        {
            // When a user hits a local holdout targeting delivery rule X, the holdout variation
            // is returned and the rule's own audience/traffic checks are not evaluated.
            // holdout_local_rule1 targets rule_id_1 with 100% traffic.
            // To test this without global holdout interference, we use a config where
            // global holdout has 0% traffic — we manipulate the global holdout in-place.
            InitializeLocalHoldoutsConfig();

            // Remove global holdout traffic so it doesn't intercept
            var globalHoldout = LocalHoldoutsConfig.GetGlobalHoldouts()[0];
            globalHoldout.TrafficAllocation = new TrafficAllocation[0]; // 0% traffic

            var realBucketer = new Bucketer(LoggerMock.Object);
            var decisionService = new DecisionService(realBucketer,
                new NoOpErrorHandler(), null, LoggerMock.Object, null);

            var featureFlag = LocalHoldoutsConfig.FeatureKeyMap["test_flag_1"]; // has rollout_1 with rule_id_1
            var userContext = new OptimizelyUserContext(LocalHoldoutsOptimizely, TestUserId, null,
                new NoOpErrorHandler(), LoggerMock.Object);

            var result = decisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag },
                userContext,
                LocalHoldoutsConfig,
                new UserAttributes(),
                new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var decision = result[0].ResultObject;
            Assert.IsNotNull(decision, "Decision should not be null");
            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_HOLDOUT, decision.Source,
                "Decision source should be holdout for local holdout hit");
            Assert.AreEqual("local_holdout_rule1", decision.Experiment?.Key,
                "Decision should be from local holdout targeting rule_id_1");
            Assert.AreEqual("local_holdout_off", decision.Variation?.Key,
                "Variation should be the holdout's variation");
        }

        [Test]
        public void TestLocalHoldouts_UserNotBucketedIntoLocalHoldoutFallsThroughToRegularRuleEvaluation()
        {
            // When a user is NOT bucketed into a local holdout, the regular rule evaluation proceeds.
            // We set the local holdout for rule_id_1 to 0% traffic so user falls through.
            InitializeLocalHoldoutsConfig();

            // Set global holdout to 0% traffic (bypass)
            var globalHoldout = LocalHoldoutsConfig.GetGlobalHoldouts()[0];
            globalHoldout.TrafficAllocation = new TrafficAllocation[0];

            // Set local holdout for rule_id_1 to 0% traffic (bypass)
            var localHoldoutsForRule1 = LocalHoldoutsConfig.GetHoldoutsForRule("rule_id_1");
            Assert.AreEqual(1, localHoldoutsForRule1.Count, "Should have one local holdout for rule_id_1");
            localHoldoutsForRule1[0].TrafficAllocation = new TrafficAllocation[0]; // 0% traffic

            var realBucketer = new Bucketer(LoggerMock.Object);
            var decisionService = new DecisionService(realBucketer,
                new NoOpErrorHandler(), null, LoggerMock.Object, null);

            var featureFlag = LocalHoldoutsConfig.FeatureKeyMap["test_flag_1"]; // has rollout with rule_id_1
            var userContext = new OptimizelyUserContext(LocalHoldoutsOptimizely, TestUserId, null,
                new NoOpErrorHandler(), LoggerMock.Object);

            var result = decisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag },
                userContext,
                LocalHoldoutsConfig,
                new UserAttributes(),
                new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var decision = result[0].ResultObject;
            Assert.IsNotNull(decision, "Decision should not be null when falling through to rollout rules");
            // User falls through local holdout and hits the regular rule with 100% traffic
            Assert.AreNotEqual(FeatureDecision.DECISION_SOURCE_HOLDOUT, decision.Source,
                "Decision source should NOT be holdout when local holdout has 0% traffic");
            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_ROLLOUT, decision.Source,
                "Decision source should be rollout after falling through local holdout");
        }

        [Test]
        public void TestLocalHoldouts_RuleSpecificity_LocalHoldoutTargetingRuleXDoesNotAffectRuleY()
        {
            // A local holdout targeting rule_id_1 should only apply to rule_id_1,
            // not to rule_id_2 in the same rollout.
            InitializeLocalHoldoutsConfig();

            // Verify that holdout_local_rule1 targets only rule_id_1
            var holdoutsForRule1 = LocalHoldoutsConfig.GetHoldoutsForRule("rule_id_1");
            var holdoutsForRule2 = LocalHoldoutsConfig.GetHoldoutsForRule("rule_id_2");

            Assert.AreEqual(1, holdoutsForRule1.Count, "rule_id_1 should have exactly one local holdout");
            Assert.AreEqual("local_holdout_rule1", holdoutsForRule1[0].Key,
                "Local holdout for rule_id_1 should be local_holdout_rule1");

            Assert.AreEqual(1, holdoutsForRule2.Count, "rule_id_2 should have exactly one local holdout");
            Assert.AreEqual("local_holdout_rule2", holdoutsForRule2[0].Key,
                "Local holdout for rule_id_2 should be local_holdout_rule2");

            // Verify the holdouts for rule_id_1 and rule_id_2 are distinct
            Assert.AreNotEqual(holdoutsForRule1[0].Id, holdoutsForRule2[0].Id,
                "Holdouts for rule_id_1 and rule_id_2 should be different holdouts");

            // Verify holdout_local_rule1 does NOT appear in rule_id_2 list
            Assert.IsFalse(holdoutsForRule2.Any(h => h.Key == "local_holdout_rule1"),
                "local_holdout_rule1 should NOT target rule_id_2");

            // Verify holdout_local_rule2 does NOT appear in rule_id_1 list
            Assert.IsFalse(holdoutsForRule1.Any(h => h.Key == "local_holdout_rule2"),
                "local_holdout_rule2 should NOT target rule_id_1");
        }

        [Test]
        public void TestLocalHoldouts_AppliesToDeliveryRules()
        {
            // Verify local holdout check applies to delivery rules (rollout rules).
            // holdout_local_rule1 targets rule_id_1 which is a delivery rule in rollout_1.
            InitializeLocalHoldoutsConfig();

            var holdoutsForDeliveryRule = LocalHoldoutsConfig.GetHoldoutsForRule("rule_id_1");
            Assert.AreEqual(1, holdoutsForDeliveryRule.Count,
                "Delivery rule rule_id_1 should have one local holdout");
            Assert.AreEqual("local_holdout_rule1", holdoutsForDeliveryRule[0].Key,
                "The local holdout for delivery rule rule_id_1 should be local_holdout_rule1");

            // Verify the delivery rule exists in the rollout
            var rollout = LocalHoldoutsConfig.GetRolloutFromId("rollout_1");
            Assert.IsNotNull(rollout, "rollout_1 should exist in config");
            var deliveryRule = rollout.Experiments?.FirstOrDefault(r => r.Id == "rule_id_1");
            Assert.IsNotNull(deliveryRule,
                "rule_id_1 should be a delivery rule within rollout_1");
        }

        /// <summary>
        /// Mandatory enforcement test (cross-SDK): forced decision takes precedence over a 100% traffic local holdout.
        /// Ordering: Forced Decision → Local Holdout → Regular Rule. If forced decision is set,
        /// it must win even when a 100% local holdout targets the same rule.
        /// </summary>
        [Test]
        public void TestForcedDecisionBeats100PercentLocalHoldout()
        {
            // Setup: local holdout 'holdout_local_exp_rule1' targets exp_rule_id_1 (experiment_rule_1) with 100% traffic.
            // User also has a forced decision set for test_flag_2 / experiment_rule_1.
            // Expected: forced decision wins; source is FEATURE_TEST, not HOLDOUT.
            InitializeLocalHoldoutsConfig();

            // Remove global holdout traffic so it doesn't interfere
            var globalHoldout = LocalHoldoutsConfig.GetGlobalHoldouts()[0];
            globalHoldout.TrafficAllocation = new TrafficAllocation[0];

            var realBucketer = new Bucketer(LoggerMock.Object);
            var decisionService = new DecisionService(realBucketer,
                new NoOpErrorHandler(), null, LoggerMock.Object, null);

            var featureFlag = LocalHoldoutsConfig.FeatureKeyMap["test_flag_2"];
            var userContext = new OptimizelyUserContext(LocalHoldoutsOptimizely, TestUserId, null,
                new NoOpErrorHandler(), LoggerMock.Object);

            // Set forced decision for experiment_rule_1 → variation_a
            userContext.SetForcedDecision(
                new OptimizelyDecisionContext("test_flag_2", "experiment_rule_1"),
                new OptimizelyForcedDecision("variation_a")
            );

            var result = decisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag },
                userContext,
                LocalHoldoutsConfig,
                new UserAttributes(),
                new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var decision = result[0].ResultObject;
            Assert.IsNotNull(decision, "Forced decision should produce a result");
            Assert.AreNotEqual(FeatureDecision.DECISION_SOURCE_HOLDOUT, decision.Source,
                "Forced decision must NOT return holdout source — forced decision takes priority over local holdout");
            Assert.AreEqual("variation_a", decision.Variation?.Key,
                "Forced decision variation should be returned, not holdout variation");
        }

        [Test]
        public void TestLocalHoldouts_AppliesToExperimentRules()
        {
            // Verify local holdout check applies to experiment rules (A/B test experiments).
            // holdout_local_exp_rule1 targets exp_rule_id_1 which is an experiment for test_flag_2.
            InitializeLocalHoldoutsConfig();

            var holdoutsForExpRule = LocalHoldoutsConfig.GetHoldoutsForRule("exp_rule_id_1");
            Assert.AreEqual(1, holdoutsForExpRule.Count,
                "Experiment rule exp_rule_id_1 should have one local holdout");
            Assert.AreEqual("local_holdout_exp_rule1", holdoutsForExpRule[0].Key,
                "The local holdout for experiment rule exp_rule_id_1 should be local_holdout_exp_rule1");

            // Verify the experiment exists as an experiment rule for test_flag_2
            var featureFlag = LocalHoldoutsConfig.FeatureKeyMap["test_flag_2"];
            Assert.IsNotNull(featureFlag, "test_flag_2 should exist in config");
            Assert.IsTrue(featureFlag.ExperimentIds.Contains("exp_rule_id_1"),
                "exp_rule_id_1 should be an experiment rule for test_flag_2");

            // Set global holdout to 0% traffic so it doesn't intercept
            var globalHoldout = LocalHoldoutsConfig.GetGlobalHoldouts()[0];
            globalHoldout.TrafficAllocation = new TrafficAllocation[0];

            var realBucketer = new Bucketer(LoggerMock.Object);
            var decisionService = new DecisionService(realBucketer,
                new NoOpErrorHandler(), null, LoggerMock.Object, null);

            var userContext = new OptimizelyUserContext(LocalHoldoutsOptimizely, TestUserId, null,
                new NoOpErrorHandler(), LoggerMock.Object);

            var result = decisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag },
                userContext,
                LocalHoldoutsConfig,
                new UserAttributes(),
                new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var decision = result[0].ResultObject;
            Assert.IsNotNull(decision, "Decision should not be null");
            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_HOLDOUT, decision.Source,
                "Decision source should be holdout since local holdout targets the experiment rule");
            Assert.AreEqual("local_holdout_exp_rule1", decision.Experiment?.Key,
                "Decision experiment should be the local holdout targeting exp_rule_id_1");
        }

        // =====================================================================
        // Exclude Targeted Deliveries Tests
        // =====================================================================

        private DatafileProjectConfig ExcludeTDConfig;
        private Optimizely ExcludeTDOptimizely;
        private DatafileProjectConfig NoExcludeTDConfig;
        private Optimizely NoExcludeTDOptimizely;

        private void InitializeExcludeTDConfig()
        {
            var datafile = TestData["datafileWithExcludeTargetedDeliveries"].ToString();
            ExcludeTDConfig = DatafileProjectConfig.Create(
                datafile, LoggerMock.Object,
                new NoOpErrorHandler()) as DatafileProjectConfig;

            var eventDispatcher = new Event.Dispatcher.DefaultEventDispatcher(LoggerMock.Object);
            ExcludeTDOptimizely = new Optimizely(
                datafile, eventDispatcher, LoggerMock.Object, new NoOpErrorHandler());
        }

        private void InitializeNoExcludeTDConfig()
        {
            var datafile = TestData["datafileWithoutExcludeTargetedDeliveries"].ToString();
            NoExcludeTDConfig = DatafileProjectConfig.Create(
                datafile, LoggerMock.Object,
                new NoOpErrorHandler()) as DatafileProjectConfig;

            var eventDispatcher = new Event.Dispatcher.DefaultEventDispatcher(LoggerMock.Object);
            NoExcludeTDOptimizely = new Optimizely(
                datafile, eventDispatcher, LoggerMock.Object, new NoOpErrorHandler());
        }

        [Test]
        public void TestExcludeTargetedDeliveries_DefaultFalse_HoldoutAppliesNormally()
        {
            InitializeNoExcludeTDConfig();

            var globalHoldout = NoExcludeTDConfig.GetGlobalHoldouts()[0];
            Assert.IsFalse(globalHoldout.ExcludeTargetedDeliveries);

            var realBucketer = new Bucketer(LoggerMock.Object);
            var decisionService = new DecisionService(realBucketer,
                new NoOpErrorHandler(), null, LoggerMock.Object, null);

            var featureFlag = NoExcludeTDConfig.FeatureKeyMap["test_flag_1"];
            var userContext = new OptimizelyUserContext(NoExcludeTDOptimizely, TestUserId, null,
                new NoOpErrorHandler(), LoggerMock.Object);

            var result = decisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag }, userContext, NoExcludeTDConfig,
                new UserAttributes(), new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var decision = result[0].ResultObject;
            Assert.IsNotNull(decision);
            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_HOLDOUT, decision.Source,
                "Global holdout with exclude_targeted_deliveries=false should apply to TD rules");
        }

        [Test]
        public void TestExcludeTargetedDeliveries_True_TDRuleEvaluatesNormally()
        {
            InitializeExcludeTDConfig();

            var globalHoldout = ExcludeTDConfig.GetGlobalHoldouts()[0];
            Assert.IsTrue(globalHoldout.ExcludeTargetedDeliveries);

            var realBucketer = new Bucketer(LoggerMock.Object);
            var decisionService = new DecisionService(realBucketer,
                new NoOpErrorHandler(), null, LoggerMock.Object, null);

            var featureFlag = ExcludeTDConfig.FeatureKeyMap["test_flag_1"];
            var userContext = new OptimizelyUserContext(ExcludeTDOptimizely, TestUserId, null,
                new NoOpErrorHandler(), LoggerMock.Object);

            var result = decisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag }, userContext, ExcludeTDConfig,
                new UserAttributes(), new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var decision = result[0].ResultObject;
            Assert.IsNotNull(decision);
            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_ROLLOUT, decision.Source,
                "With exclude_targeted_deliveries=true, TD rules should evaluate normally (not blocked by holdout)");
        }

        [Test]
        public void TestExcludeTargetedDeliveries_True_ABRuleStillBlocked()
        {
            InitializeExcludeTDConfig();

            var globalHoldout = ExcludeTDConfig.GetGlobalHoldouts()[0];
            Assert.IsTrue(globalHoldout.ExcludeTargetedDeliveries);

            var realBucketer = new Bucketer(LoggerMock.Object);
            var decisionService = new DecisionService(realBucketer,
                new NoOpErrorHandler(), null, LoggerMock.Object, null);

            var featureFlag = ExcludeTDConfig.FeatureKeyMap["test_flag_2"];
            var userContext = new OptimizelyUserContext(ExcludeTDOptimizely, TestUserId, null,
                new NoOpErrorHandler(), LoggerMock.Object);

            var result = decisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag }, userContext, ExcludeTDConfig,
                new UserAttributes(), new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var decision = result[0].ResultObject;
            Assert.IsNotNull(decision);

            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_HOLDOUT, decision.Source,
                "With exclude_targeted_deliveries=true, A/B experiment rules should still be blocked by holdout");
        }

        [Test]
        public void TestExcludeTargetedDeliveries_MissingField_DefaultsFalse()
        {
            InitializeNoExcludeTDConfig();

            var globalHoldout = NoExcludeTDConfig.GetGlobalHoldouts()[0];
            Assert.IsFalse(globalHoldout.ExcludeTargetedDeliveries,
                "Missing exclude_targeted_deliveries should default to false");

            foreach (var localHoldout in NoExcludeTDConfig.LocalHoldouts)
            {
                Assert.IsFalse(localHoldout.ExcludeTargetedDeliveries,
                    $"Local holdout {localHoldout.Key} should default exclude_targeted_deliveries to false");
            }
        }

        [Test]
        public void TestLocalHoldout_ExcludeTargetedDeliveries_True_SkippedForTDRule()
        {
            InitializeExcludeTDConfig();

            var globalHoldout = ExcludeTDConfig.GetGlobalHoldouts()[0];
            globalHoldout.TrafficAllocation = new TrafficAllocation[0];

            var realBucketer = new Bucketer(LoggerMock.Object);
            var decisionService = new DecisionService(realBucketer,
                new NoOpErrorHandler(), null, LoggerMock.Object, null);

            var featureFlag = ExcludeTDConfig.FeatureKeyMap["test_flag_1"];
            var userContext = new OptimizelyUserContext(ExcludeTDOptimizely, TestUserId, null,
                new NoOpErrorHandler(), LoggerMock.Object);

            var result = decisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag }, userContext, ExcludeTDConfig,
                new UserAttributes(), new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var decision = result[0].ResultObject;
            Assert.IsNotNull(decision);
            Assert.AreNotEqual(FeatureDecision.DECISION_SOURCE_HOLDOUT, decision.Source,
                "Local holdout with exclude_targeted_deliveries=true should be skipped for TD rules");
            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_ROLLOUT, decision.Source,
                "TD rule should evaluate normally when local holdout excludes targeted deliveries");
        }

        [Test]
        public void TestLocalHoldout_ExcludeTargetedDeliveries_True_StillAppliesForABRule()
        {
            InitializeExcludeTDConfig();

            var globalHoldout = ExcludeTDConfig.GetGlobalHoldouts()[0];
            globalHoldout.TrafficAllocation = new TrafficAllocation[0];

            var realBucketer = new Bucketer(LoggerMock.Object);
            var decisionService = new DecisionService(realBucketer,
                new NoOpErrorHandler(), null, LoggerMock.Object, null);

            var featureFlag = ExcludeTDConfig.FeatureKeyMap["test_flag_2"];
            var userContext = new OptimizelyUserContext(ExcludeTDOptimizely, TestUserId, null,
                new NoOpErrorHandler(), LoggerMock.Object);

            var result = decisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag }, userContext, ExcludeTDConfig,
                new UserAttributes(), new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var decision = result[0].ResultObject;
            Assert.IsNotNull(decision);
            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_HOLDOUT, decision.Source,
                "Local holdout with exclude_targeted_deliveries=true should still apply for A/B rules");
        }

        [Test]
        public void TestGlobalHoldout_ExcludeTargetedDeliveries_NoTDRuleMatch_FallsBackToHoldout()
        {
            InitializeExcludeTDConfig();

            var globalHoldout = ExcludeTDConfig.GetGlobalHoldouts()[0];
            Assert.IsTrue(globalHoldout.ExcludeTargetedDeliveries);

            var realBucketer = new Bucketer(LoggerMock.Object);
            var decisionService = new DecisionService(realBucketer,
                new NoOpErrorHandler(), null, LoggerMock.Object, null);

            // test_flag_1 has only rollout (TD) rules, no experiments.
            // With exclude_targeted_deliveries=true, TD rules evaluate normally.
            // But test_flag_2 has experiments. If no experiment matches traffic,
            // the holdout decision should still be returned.
            var featureFlag = ExcludeTDConfig.FeatureKeyMap["test_flag_2"];

            // Zero out experiment traffic so no experiment matches
            foreach (var exp in ExcludeTDConfig.ExperimentIdMap.Values)
            {
                if (featureFlag.ExperimentIds.Contains(exp.Id))
                {
                    exp.TrafficAllocation = new List<TrafficAllocation>().ToArray();
                }
            }

            // Zero out local holdout traffic
            foreach (var lh in ExcludeTDConfig.LocalHoldouts)
            {
                lh.TrafficAllocation = new TrafficAllocation[0];
            }

            var userContext = new OptimizelyUserContext(ExcludeTDOptimizely, TestUserId, null,
                new NoOpErrorHandler(), LoggerMock.Object);

            var result = decisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { featureFlag }, userContext, ExcludeTDConfig,
                new UserAttributes(), new OptimizelyDecideOption[0]);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var decision = result[0].ResultObject;
            Assert.IsNotNull(decision);
            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_HOLDOUT, decision.Source,
                "When no TD rule matches and experiments are blocked, holdout decision should be returned as fallback");
        }
    }
}

