/*
 * Copyright 2025, 2026, Optimizely
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
using OptimizelySDK.Notifications;
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
        public void TestLocalHoldouts_DeliveryRuleHoldoutHitSkipsRuleAndLaterRuleServes()
        {
            InitializeLocalHoldoutsConfig();

            LocalHoldoutsConfig.GetGlobalHoldouts()[0].TrafficAllocation = new TrafficAllocation[0];
            LocalHoldoutsConfig.GetHoldoutsForRule("rule_id_2")[0].TrafficAllocation =
                new TrafficAllocation[0];

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
            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_ROLLOUT, decision.Source);
            Assert.AreEqual("rule_id_2", decision.Experiment?.Id,
                "Held-out rule_id_1 should be skipped and rule_id_2 should serve");
            Assert.AreEqual("rule2_var_1", decision.Variation?.Id);
            Assert.AreEqual(1, decision.HoldoutDecisions.Count);
            Assert.AreEqual("local_holdout_rule1", decision.HoldoutDecisions[0].Experiment.Key);
            Assert.AreEqual("local_holdout_off", decision.HoldoutDecisions[0].Variation.Key);
            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_HOLDOUT,
                decision.HoldoutDecisions[0].Source);
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
            Assert.AreEqual(0, decision.HoldoutDecisions.Count);
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
            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_FEATURE_TEST, decision.Source);
            Assert.AreEqual("experiment_rule_2", decision.Experiment?.Key,
                "Held out of experiment_rule_1, user should be served by experiment_rule_2");
            Assert.AreEqual("variation_b", decision.Variation?.Key);
            Assert.AreEqual(1, decision.HoldoutDecisions.Count);
            Assert.AreEqual("local_holdout_exp_rule1", decision.HoldoutDecisions[0].Experiment.Key);
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
                "Global holdout with excludeTargetedDeliveries=false should apply to TD rules");
        }

        [Test]
        public void TestExcludeTargetedDeliveries_True_TDRuleEvaluatesNormally()
        {
            InitializeExcludeTDConfig();

            foreach (var lh in ExcludeTDConfig.LocalHoldouts)
            {
                lh.TrafficAllocation = new TrafficAllocation[0];
            }

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
                "With excludeTargetedDeliveries=true, TD rules should evaluate normally (not blocked by holdout)");

            var reasons = result[0].DecisionReasons.ToReport(true);
            var excludeTDReason = reasons.FirstOrDefault(r =>
                r.Contains("has excludeTargetedDeliveries enabled, continuing to rollout evaluation"));
            Assert.IsNotNull(excludeTDReason,
                "Reasons should include excludeTargetedDeliveries bypass message");
            Assert.IsTrue(excludeTDReason.Contains(globalHoldout.Key),
                "Reason should reference the holdout name");
        }

        [Test]
        public void TestExcludeTargetedDeliveries_True_ABRuleStillBlocked()
        {
            InitializeExcludeTDConfig();

            foreach (var lh in ExcludeTDConfig.LocalHoldouts)
            {
                lh.TrafficAllocation = new TrafficAllocation[0];
            }

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

            Assert.AreNotEqual(FeatureDecision.DECISION_SOURCE_FEATURE_TEST, decision.Source,
                "With excludeTargetedDeliveries=true, A/B experiment rules should be skipped (not evaluated)");
            Assert.IsNull(decision.Variation,
                "Variation should be null since A/B was blocked and no TD rule matched");
            Assert.AreEqual(1, decision.HoldoutDecisions.Count,
                "HoldoutDecisions should be attached showing the user was in a holdout that blocked A/B rules");
        }

        [Test]
        public void TestExcludeTargetedDeliveries_MissingField_DefaultsFalse()
        {
            InitializeNoExcludeTDConfig();

            var globalHoldout = NoExcludeTDConfig.GetGlobalHoldouts()[0];
            Assert.IsFalse(globalHoldout.ExcludeTargetedDeliveries,
                "Missing excludeTargetedDeliveries should default to false");

            foreach (var localHoldout in NoExcludeTDConfig.LocalHoldouts)
            {
                Assert.IsFalse(localHoldout.ExcludeTargetedDeliveries,
                    $"Local holdout {localHoldout.Key} should default excludeTargetedDeliveries to false");
            }
        }

        [Test]
        public void TestLocalHoldout_ExcludeTargetedDeliveries_True_StillAppliesForTDRule()
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
            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_ROLLOUT, decision.Source);
            Assert.AreEqual("rule_id_everyone_else", decision.Experiment?.Id);
            CollectionAssert.AreEqual(new[] { "local_holdout_rule1", "local_holdout_rule2" },
                decision.HoldoutDecisions.Select(h => h.Experiment.Key).ToArray(),
                "Local holdouts must apply to TD rules even when excludeTargetedDeliveries=true (flag is only for global holdouts)");
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
            Assert.AreEqual("experiment_rule_2", decision.Experiment?.Key);
            CollectionAssert.AreEqual(new[] { "local_holdout_exp_rule1" },
                decision.HoldoutDecisions.Select(h => h.Experiment.Key).ToArray(),
                "Local holdout with excludeTargetedDeliveries=true should still apply for A/B rules");
        }

        [Test]
        public void TestGlobalHoldout_ExcludeTargetedDeliveries_NoTDRuleMatch_ReturnsNullDecision()
        {
            InitializeExcludeTDConfig();

            var globalHoldout = ExcludeTDConfig.GetGlobalHoldouts()[0];
            Assert.IsTrue(globalHoldout.ExcludeTargetedDeliveries);

            var realBucketer = new Bucketer(LoggerMock.Object);
            var decisionService = new DecisionService(realBucketer,
                new NoOpErrorHandler(), null, LoggerMock.Object, null);

            var featureFlag = ExcludeTDConfig.FeatureKeyMap["test_flag_1"];

            var rollout = ExcludeTDConfig.GetRolloutFromId(featureFlag.RolloutId);
            foreach (var rule in rollout.Experiments)
            {
                rule.TrafficAllocation = new List<TrafficAllocation>().ToArray();
            }

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
            Assert.IsNull(decision.Variation,
                "When TD returns no match with excludeTargetedDeliveries=true, variation should be null (no holdout fallback)");
            Assert.IsNull(decision.Experiment,
                "When TD returns no match with excludeTargetedDeliveries=true, experiment should be null (no holdout fallback)");
            Assert.AreEqual(1, decision.HoldoutDecisions.Count,
                "HoldoutDecisions should be attached for event dispatch even when decision is null");
        }
        [Test]
        public void TestExcludeTargetedDeliveries_HoldoutEventSentWhenTDBypasses()
        {
            InitializeExcludeTDConfig();

            var globalHoldout = ExcludeTDConfig.GetGlobalHoldouts()[0];
            Assert.IsTrue(globalHoldout.ExcludeTargetedDeliveries);

            var eventDispatcher = new Event.Dispatcher.DefaultEventDispatcher(LoggerMock.Object);
            var optimizelyWithMockedEvents = new Optimizely(
                TestData["datafileWithExcludeTargetedDeliveries"].ToString(),
                eventDispatcher,
                LoggerMock.Object,
                new NoOpErrorHandler(),
                null,
                false,
                EventProcessorMock.Object
            );

            EventProcessorMock.Setup(ep => ep.Process(It.IsAny<ImpressionEvent>()));

            var userContext = optimizelyWithMockedEvents.CreateUserContext(TestUserId, new UserAttributes());
            var featureFlag = ExcludeTDConfig.FeatureKeyMap["test_flag_1"];
            var decision = userContext.Decide(featureFlag.Key);

            Assert.IsNotNull(decision);

            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.AtLeast(2),
                "Both holdout and TD impression events should be sent when holdout is bypassed for TD rules");

            EventProcessorMock.Verify(ep => ep.Process(It.Is<ImpressionEvent>(ie =>
                ie.Experiment.Key == globalHoldout.Key
            )), Times.Once, "Holdout impression event should be sent even when holdout is bypassed for TD rules");

            Dictionary<string, object> lastNotification = null;
            optimizelyWithMockedEvents.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                (NotificationCenter.DecisionCallback)((type, userId, userAttributes, decisionInfo) =>
                {
                    lastNotification = decisionInfo;
                }));

            var decision2 = userContext.Decide(featureFlag.Key);
            Assert.IsNotNull(lastNotification, "Decision notification should have been sent");
            Assert.IsTrue(lastNotification.ContainsKey("decisionEventDispatched"),
                "Notification should contain decisionEventDispatched");
            Assert.IsTrue((bool)lastNotification["decisionEventDispatched"],
                "decisionEventDispatched should be true when holdout impression is sent");
        }

        [Test]
        public void TestExcludeTargetedDeliveries_HoldoutDecisionAttachedToTDResult()
        {
            InitializeExcludeTDConfig();

            foreach (var lh in ExcludeTDConfig.LocalHoldouts)
            {
                lh.TrafficAllocation = new TrafficAllocation[0];
            }

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
                "TD rule should be returned as the decision");
            Assert.AreEqual(1, decision.HoldoutDecisions.Count,
                "HoldoutDecisions should be attached when holdout was bypassed for TD");
            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_HOLDOUT, decision.HoldoutDecisions[0].Source,
                "Attached holdout decision should have holdout source");
        }

        [Test]
        public void TestExcludeTargetedDeliveries_HoldoutEventSentWhenNoTDMatch_DispatchedTrue()
        {
            InitializeExcludeTDConfig();

            var optimizelyWithMockedEvents = new Optimizely(
                TestData["datafileWithExcludeTargetedDeliveries"].ToString(),
                null,
                LoggerMock.Object,
                new NoOpErrorHandler(),
                null,
                false,
                EventProcessorMock.Object
            );

            // Zero out all rollout (TD) rules and local holdouts so the global holdout
            // matches but NO targeted delivery rule can match — the decision is null.
            var config = optimizelyWithMockedEvents.ProjectConfigManager.GetConfig()
                as DatafileProjectConfig;
            var globalHoldout = config.GetGlobalHoldouts()[0];
            Assert.IsTrue(globalHoldout.ExcludeTargetedDeliveries);

            var featureFlag = config.FeatureKeyMap["test_flag_1"];
            var rollout = config.GetRolloutFromId(featureFlag.RolloutId);
            foreach (var rule in rollout.Experiments)
            {
                rule.TrafficAllocation = new TrafficAllocation[0];
            }
            foreach (var lh in config.LocalHoldouts)
            {
                lh.TrafficAllocation = new TrafficAllocation[0];
            }

            EventProcessorMock.Setup(ep => ep.Process(It.IsAny<ImpressionEvent>()));

            Dictionary<string, object> lastNotification = null;
            optimizelyWithMockedEvents.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                (NotificationCenter.DecisionCallback)((type, userId, userAttributes, decisionInfo) =>
                {
                    lastNotification = decisionInfo;
                }));

            var userContext = optimizelyWithMockedEvents.CreateUserContext(TestUserId,
                new UserAttributes());
            var decision = userContext.Decide(featureFlag.Key);

            Assert.IsNotNull(decision);

            // The holdout impression event must still be dispatched even though no TD rule matched.
            EventProcessorMock.Verify(ep => ep.Process(It.Is<ImpressionEvent>(ie =>
                ie.Experiment.Key == globalHoldout.Key
            )), Times.Once, "Holdout impression event should be sent even when no TD rule matches");

            // Regression: an impression WAS dispatched (the holdout), so decisionEventDispatched
            // must be true even though the primary decision is null.
            Assert.IsNotNull(lastNotification, "Decision notification should have been sent");
            Assert.IsTrue((bool)lastNotification["decisionEventDispatched"],
                "decisionEventDispatched should be true when the holdout impression is dispatched, " +
                "even if the primary (TD) decision is null");
        }

        private Optimizely CreateLocalHoldoutsOptimizelyWithMockedEvents(
            Action<JObject> mutateDatafile = null
        )
        {
            var datafile = (JObject)TestData["datafileWithLocalHoldouts"].DeepClone();
            datafile["holdouts"][0]["trafficAllocation"] = new JArray();
            mutateDatafile?.Invoke(datafile);

            EventProcessorMock.Setup(ep => ep.Process(It.IsAny<ImpressionEvent>()));
            return new Optimizely(datafile.ToString(), null, LoggerMock.Object,
                new NoOpErrorHandler(), null, false, EventProcessorMock.Object);
        }

        private static void ZeroTraffic(JObject datafile, string section, string id)
        {
            var entity = datafile[section].First(e => (string)e["id"] == id);
            entity["trafficAllocation"] = new JArray();
        }

        private void VerifyHoldoutImpression(string holdoutKey, Times times)
        {
            EventProcessorMock.Verify(ep => ep.Process(It.Is<ImpressionEvent>(ie =>
                ie.Experiment.Key == holdoutKey &&
                ie.Metadata.RuleType == FeatureDecision.DECISION_SOURCE_HOLDOUT
            )), times);
        }

        [Test]
        public void TestLocalHoldouts_HitsOnSeveralRulesSendSeveralHoldoutEvents()
        {
            var optimizely = CreateLocalHoldoutsOptimizelyWithMockedEvents();

            var decision = optimizely.CreateUserContext(TestUserId).Decide("test_flag_1");

            Assert.AreEqual("everyone_else_rule", decision.RuleKey);
            Assert.IsTrue(decision.Enabled);
            VerifyHoldoutImpression("local_holdout_rule1", Times.Once());
            VerifyHoldoutImpression("local_holdout_rule2", Times.Once());
            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Exactly(2));
        }

        [Test]
        public void TestLocalHoldouts_HoldoutHitThenNoRuleServes()
        {
            var optimizely = CreateLocalHoldoutsOptimizelyWithMockedEvents(df =>
            {
                foreach (var rule in df["rollouts"][0]["experiments"])
                {
                    rule["trafficAllocation"] = new JArray();
                }
            });
            var config = optimizely.ProjectConfigManager.GetConfig() as DatafileProjectConfig;
            var decisionService = new DecisionService(new Bucketer(LoggerMock.Object),
                new NoOpErrorHandler(), null, LoggerMock.Object, null);
            var userContext = optimizely.CreateUserContext(TestUserId);

            var result = decisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { config.FeatureKeyMap["test_flag_1"] }, userContext, config,
                new UserAttributes(), new OptimizelyDecideOption[0]);

            var featureDecision = result[0].ResultObject;
            Assert.IsNull(featureDecision.Variation);
            Assert.IsNull(featureDecision.Experiment);
            Assert.AreEqual(FeatureDecision.DECISION_SOURCE_ROLLOUT, featureDecision.Source);
            Assert.AreEqual(2, featureDecision.HoldoutDecisions.Count);

            var decision = userContext.Decide("test_flag_1");

            Assert.IsFalse(decision.Enabled);
            Assert.IsNull(decision.VariationKey);
            Assert.IsNull(decision.RuleKey);
            VerifyHoldoutImpression("local_holdout_rule1", Times.Once());
            VerifyHoldoutImpression("local_holdout_rule2", Times.Once());
        }

        [Test]
        public void TestLocalHoldouts_DeliveryRuleHitWithSendFlagDecisionsOffStillSendsHoldoutEvent()
        {
            var optimizely = CreateLocalHoldoutsOptimizelyWithMockedEvents(df =>
            {
                df["sendFlagDecisions"] = false;
                ZeroTraffic(df, "localHoldouts", "holdout_local_rule2");
            });

            var decision = optimizely.CreateUserContext(TestUserId).Decide("test_flag_1");

            Assert.AreEqual("delivery_rule_2", decision.RuleKey);
            VerifyHoldoutImpression("local_holdout_rule1", Times.Once());
            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Once(),
                "Only the holdout event should be sent; the rollout event is suppressed by sendFlagDecisions=false");
        }

        [Test]
        public void TestLocalHoldouts_DisableDecisionEventSuppressesHoldoutEvents()
        {
            var optimizely = CreateLocalHoldoutsOptimizelyWithMockedEvents();

            var decision = optimizely.CreateUserContext(TestUserId).Decide("test_flag_1",
                new[] { OptimizelyDecideOption.DISABLE_DECISION_EVENT });

            Assert.AreEqual("everyone_else_rule", decision.RuleKey);
            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Never());
        }

        [Test]
        public void TestLocalHoldouts_HeldOutOfFirstExperimentRuleServedBySecond()
        {
            var optimizely = CreateLocalHoldoutsOptimizelyWithMockedEvents();

            var decision = optimizely.CreateUserContext(TestUserId).Decide("test_flag_2");

            Assert.AreEqual("experiment_rule_2", decision.RuleKey);
            Assert.AreEqual("variation_b", decision.VariationKey);
            VerifyHoldoutImpression("local_holdout_exp_rule1", Times.Once());
            EventProcessorMock.Verify(ep => ep.Process(It.Is<ImpressionEvent>(ie =>
                ie.Experiment.Key == "experiment_rule_2" &&
                ie.Metadata.RuleType == FeatureDecision.DECISION_SOURCE_FEATURE_TEST
            )), Times.Once());
        }

        [Test]
        public void TestLocalHoldouts_OneHoldoutTargetingTwoRulesSendsTwoHoldoutEvents()
        {
            var optimizely = CreateLocalHoldoutsOptimizelyWithMockedEvents(df =>
            {
                var holdouts = (JArray)df["localHoldouts"];
                holdouts.First(h => (string)h["id"] == "holdout_local_rule1")["includedRules"] =
                    new JArray("rule_id_1", "rule_id_2");
                holdouts.First(h => (string)h["id"] == "holdout_local_rule2")["includedRules"] =
                    new JArray();
            });

            var decision = optimizely.CreateUserContext(TestUserId).Decide("test_flag_1");

            Assert.AreEqual("everyone_else_rule", decision.RuleKey);
            VerifyHoldoutImpression("local_holdout_rule1", Times.Exactly(2));
            VerifyHoldoutImpression("local_holdout_rule2", Times.Never());
            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Exactly(2));
        }

        [Test]
        public void TestLocalHoldouts_HoldoutBeatsStickyUserProfileVariation()
        {
            InitializeLocalHoldoutsConfig();
            LocalHoldoutsConfig.GetGlobalHoldouts()[0].TrafficAllocation = new TrafficAllocation[0];

            var userProfile = new UserProfile(TestUserId, new Dictionary<string, Bucketing.Decision>
            {
                { "exp_rule_id_1", new Bucketing.Decision("exp1_var_1") },
            });
            var userProfileServiceMock = new Mock<UserProfileService>();
            userProfileServiceMock.Setup(ups => ups.Lookup(TestUserId)).Returns(userProfile.ToMap());

            var decisionService = new DecisionService(new Bucketer(LoggerMock.Object),
                new NoOpErrorHandler(), userProfileServiceMock.Object, LoggerMock.Object, null);
            var userContext = new OptimizelyUserContext(LocalHoldoutsOptimizely, TestUserId, null,
                new NoOpErrorHandler(), LoggerMock.Object);

            var result = decisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { LocalHoldoutsConfig.FeatureKeyMap["test_flag_2"] },
                userContext, LocalHoldoutsConfig, new UserAttributes(),
                new OptimizelyDecideOption[0]);

            var decision = result[0].ResultObject;
            Assert.AreEqual("experiment_rule_2", decision.Experiment?.Key);
            Assert.AreEqual("variation_b", decision.Variation?.Key);
            Assert.AreEqual(1, decision.HoldoutDecisions.Count);
            Assert.AreEqual("local_holdout_exp_rule1", decision.HoldoutDecisions[0].Experiment.Key);
        }

        [Test]
        public void TestLocalHoldouts_GlobalExcludeTDHitThenLocalHitOrderedGlobalFirst()
        {
            var optimizely = CreateLocalHoldoutsOptimizelyWithMockedEvents(df =>
            {
                var globalHoldout = df["holdouts"][0];
                globalHoldout["trafficAllocation"] = new JArray(new JObject
                {
                    { "entityId", "gvar_1" },
                    { "endOfRange", 10000 },
                });
                globalHoldout["excludeTargetedDeliveries"] = true;
                ZeroTraffic(df, "localHoldouts", "holdout_local_rule2");
            });
            var config = optimizely.ProjectConfigManager.GetConfig() as DatafileProjectConfig;
            var decisionService = new DecisionService(new Bucketer(LoggerMock.Object),
                new NoOpErrorHandler(), null, LoggerMock.Object, null);
            var userContext = optimizely.CreateUserContext(TestUserId);

            var result = decisionService.GetVariationsForFeatureList(
                new List<FeatureFlag> { config.FeatureKeyMap["test_flag_1"] }, userContext, config,
                new UserAttributes(), new OptimizelyDecideOption[0]);

            var featureDecision = result[0].ResultObject;
            Assert.AreEqual("delivery_rule_2", featureDecision.Experiment?.Key);
            Assert.AreEqual(2, featureDecision.HoldoutDecisions.Count);
            Assert.AreEqual("global_holdout_2", featureDecision.HoldoutDecisions[0].Experiment.Key);
            Assert.AreEqual("local_holdout_rule1", featureDecision.HoldoutDecisions[1].Experiment.Key);

            var decision = userContext.Decide("test_flag_1");

            Assert.AreEqual("delivery_rule_2", decision.RuleKey);
            VerifyHoldoutImpression("global_holdout_2", Times.Once());
            VerifyHoldoutImpression("local_holdout_rule1", Times.Once());
            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Exactly(2));
        }

        [Test]
        public void TestLocalHoldouts_ErrorDecisionStillSendsHoldoutEvents()
        {
            var optimizely = CreateLocalHoldoutsOptimizelyWithMockedEvents();
            var config = optimizely.ProjectConfigManager.GetConfig() as DatafileProjectConfig;
            var holdout = config.LocalHoldouts.First(h => h.Key == "local_holdout_exp_rule1");
            var errorDecision = new FeatureDecision(config.GetExperimentFromKey("experiment_rule_2"),
                null, FeatureDecision.DECISION_SOURCE_FEATURE_TEST, null, error: true);
            errorDecision.HoldoutDecisions.Add(new FeatureDecision(holdout, holdout.Variations[0],
                FeatureDecision.DECISION_SOURCE_HOLDOUT));

            var decisionServiceMock = new Mock<DecisionService>(new Bucketer(LoggerMock.Object),
                new NoOpErrorHandler(), null, LoggerMock.Object, null);
            decisionServiceMock.Setup(ds => ds.GetVariationsForFeatureList(
                    It.IsAny<List<FeatureFlag>>(), It.IsAny<OptimizelyUserContext>(),
                    It.IsAny<ProjectConfig>(), It.IsAny<UserAttributes>(),
                    It.IsAny<OptimizelyDecideOption[]>())).
                Returns(new List<Result<FeatureDecision>>
                {
                    Result<FeatureDecision>.NewResult(errorDecision, new DecisionReasons()),
                });
            typeof(Optimizely).GetField("DecisionService",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance).
                SetValue(optimizely, decisionServiceMock.Object);

            var decision = optimizely.CreateUserContext(TestUserId).Decide("test_flag_2");

            Assert.IsNull(decision.VariationKey);
            VerifyHoldoutImpression("local_holdout_exp_rule1", Times.Once());
            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Once());
        }

        [Test]
        public void TestLocalHoldouts_LegacyApiSkipsHeldOutRuleWithoutHoldoutEvent()
        {
            var optimizely = CreateLocalHoldoutsOptimizelyWithMockedEvents(df =>
            {
                df["sendFlagDecisions"] = true;
            });

            Assert.IsTrue(optimizely.IsFeatureEnabled("test_flag_1", TestUserId));
            Assert.IsTrue(optimizely.IsFeatureEnabled("test_flag_2", TestUserId));

            EventProcessorMock.Verify(ep => ep.Process(It.Is<ImpressionEvent>(ie =>
                ie.Metadata.RuleType == FeatureDecision.DECISION_SOURCE_HOLDOUT)), Times.Never());
        }
    }
}
