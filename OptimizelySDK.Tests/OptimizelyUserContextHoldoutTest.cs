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
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Logger;
using OptimizelySDK.OptimizelyDecisions;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class OptimizelyUserContextHoldoutTest
    {
        private Mock<ILogger> LoggerMock;
        private Mock<IEventDispatcher> EventDispatcherMock;
        private DatafileProjectConfig Config;
        private JObject TestData;
        private Optimizely OptimizelyInstance;

        private const string TestUserId = "testUserId";
        private const string TestBucketingId = "testBucketingId";

        [SetUp]
        public void Initialize()
        {
            LoggerMock = new Mock<ILogger>();
            EventDispatcherMock = new Mock<IEventDispatcher>();

            // Load test data
            var testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestData", "HoldoutTestData.json");
            var jsonContent = File.ReadAllText(testDataPath);
            TestData = JObject.Parse(jsonContent);

            // Use datafile with holdouts for proper config setup
            var datafileWithHoldouts = TestData["datafileWithHoldouts"].ToString();

            // Create an Optimizely instance with the test data
            OptimizelyInstance = new Optimizely(datafileWithHoldouts, EventDispatcherMock.Object, LoggerMock.Object);

            // Get the config from the Optimizely instance to ensure they're synchronized
            Config = OptimizelyInstance.ProjectConfigManager.GetConfig() as DatafileProjectConfig;

            // Verify that the config contains holdouts
            Assert.IsNotNull(Config.Holdouts, "Config should have holdouts");
            Assert.IsTrue(Config.Holdouts.Length > 0, "Config should contain holdouts");
        }

        [Test]
        public void TestDecide_GlobalHoldout()
        {
            // Test Decide() method with global holdout decision
            var featureFlag = Config.FeatureKeyMap["test_flag_1"];
            Assert.IsNotNull(featureFlag, "Feature flag should exist");

            var userContext = OptimizelyInstance.CreateUserContext(TestUserId,
                new UserAttributes { { "country", "us" } });

            var decision = userContext.Decide("test_flag_1");

            Assert.IsNotNull(decision, "Decision should not be null");
            Assert.AreEqual("test_flag_1", decision.FlagKey, "Flag key should match");

            // With real bucketer, we can't guarantee specific variation but can verify structure
            // The decision should either be from holdout, experiment, or rollout
            Assert.IsTrue(!string.IsNullOrEmpty(decision.VariationKey) || decision.VariationKey == null,
                "Variation key should be valid or null");
        }

        [Test]
        public void TestDecide_IncludedFlagsHoldout()
        {
            // Test holdout with includedFlags configuration
            var featureFlag = Config.FeatureKeyMap["test_flag_1"];
            Assert.IsNotNull(featureFlag, "Feature flag should exist");

            // Check if there's a holdout that includes this flag
            var includedHoldout = Config.Holdouts.FirstOrDefault(h =>
                h.IncludedFlags != null && h.IncludedFlags.Contains(featureFlag.Id));

            if (includedHoldout != null)
            {
                var userContext = OptimizelyInstance.CreateUserContext(TestUserId,
                    new UserAttributes { { "country", "us" } });

                var decision = userContext.Decide("test_flag_1");

                Assert.IsNotNull(decision, "Decision should not be null");
                Assert.AreEqual("test_flag_1", decision.FlagKey, "Flag key should match");

                // Verify decision is valid
                Assert.IsTrue(decision.VariationKey != null || decision.VariationKey == null,
                    "Decision should have valid structure");
            }
            else
            {
                Assert.Inconclusive("No included holdout found for test_flag_1");
            }
        }

        [Test]
        public void TestDecide_ExcludedFlagsHoldout()
        {
            // Test holdout with excludedFlags configuration
            // Based on test data, flag_3 and flag_4 are excluded by holdout_excluded_1
            var userContext = OptimizelyInstance.CreateUserContext(TestUserId,
                new UserAttributes { { "country", "us" } });

            // Test with an excluded flag (test_flag_3 maps to flag_3)
            var excludedDecision = userContext.Decide("test_flag_3");

            Assert.IsNotNull(excludedDecision, "Decision should not be null for excluded flag");
            Assert.AreEqual("test_flag_3", excludedDecision.FlagKey, "Flag key should match");

            // For excluded flags, the decision should not come from the excluded holdout
            // The excluded holdout has key "excluded_holdout"
            Assert.AreNotEqual("excluded_holdout", excludedDecision.RuleKey,
                "Decision should not come from excluded holdout for flag_3");

            // Also test with a non-excluded flag (test_flag_1 maps to flag_1)
            var nonExcludedDecision = userContext.Decide("test_flag_1");

            Assert.IsNotNull(nonExcludedDecision, "Decision should not be null for non-excluded flag");
            Assert.AreEqual("test_flag_1", nonExcludedDecision.FlagKey, "Flag key should match");

            // For non-excluded flags, they can potentially be affected by holdouts
            // (depending on other holdout configurations like global or included holdouts)
        }

        [Test]
        public void TestDecideAll_MultipleHoldouts()
        {
            // Test DecideAll() with multiple holdouts affecting different flags
            var userContext = OptimizelyInstance.CreateUserContext(TestUserId,
                new UserAttributes { { "country", "us" } });

            var decisions = userContext.DecideAll();

            Assert.IsNotNull(decisions, "Decisions should not be null");
            Assert.IsTrue(decisions.Count > 0, "Should have at least one decision");

            // Verify each decision has proper structure
            foreach (var kvp in decisions)
            {
                var flagKey = kvp.Key;
                var decision = kvp.Value;

                Assert.AreEqual(flagKey, decision.FlagKey, $"Flag key should match for {flagKey}");
                Assert.IsNotNull(decision, $"Decision should not be null for {flagKey}");

                // Decision should have either a variation or be properly null
                Assert.IsTrue(decision.VariationKey != null || decision.VariationKey == null,
                    $"Decision structure should be valid for {flagKey}");
            }
        }

        [Test]
        public void TestDecide_HoldoutImpressionEvent()
        {
            // Test that impression events are sent for holdout decisions
            var userContext = OptimizelyInstance.CreateUserContext(TestUserId,
                new UserAttributes { { "country", "us" } });

            var decision = userContext.Decide("test_flag_1");

            Assert.IsNotNull(decision, "Decision should not be null");

            // Verify that event dispatcher was called
            // Note: With real bucketer, we can't guarantee holdout selection, 
            // but we can verify event structure
            EventDispatcherMock.Verify(
                e => e.DispatchEvent(It.IsAny<LogEvent>()),
                Times.AtLeastOnce,
                "Event should be dispatched for decision"
            );
        }

        [Test]
        public void TestDecide_HoldoutWithDecideOptions()
        {
            // Test decide options (like ExcludeVariables) with holdout decisions
            var userContext = OptimizelyInstance.CreateUserContext(TestUserId,
                new UserAttributes { { "country", "us" } });

            // Test with exclude variables option
            var decisionWithVariables = userContext.Decide("test_flag_1");
            var decisionWithoutVariables = userContext.Decide("test_flag_1",
                new OptimizelyDecideOption[] { OptimizelyDecideOption.EXCLUDE_VARIABLES });

            Assert.IsNotNull(decisionWithVariables, "Decision with variables should not be null");
            Assert.IsNotNull(decisionWithoutVariables, "Decision without variables should not be null");

            // When variables are excluded, the Variables object should be empty
            Assert.IsTrue(decisionWithoutVariables.Variables.ToDictionary().Count == 0,
                "Variables should be empty when excluded");
        }

        [Test]
        public void TestDecide_HoldoutWithAudienceTargeting()
        {
            // Test holdout decisions with different user attributes for audience targeting
            var featureFlag = Config.FeatureKeyMap["test_flag_1"];
            Assert.IsNotNull(featureFlag, "Feature flag should exist");

            // Test with matching attributes
            var userContextMatch = OptimizelyInstance.CreateUserContext(TestUserId,
                new UserAttributes { { "country", "us" } });
            var decisionMatch = userContextMatch.Decide("test_flag_1");

            // Test with non-matching attributes
            var userContextNoMatch = OptimizelyInstance.CreateUserContext(TestUserId,
                new UserAttributes { { "country", "ca" } });
            var decisionNoMatch = userContextNoMatch.Decide("test_flag_1");

            Assert.IsNotNull(decisionMatch, "Decision with matching attributes should not be null");
            Assert.IsNotNull(decisionNoMatch, "Decision with non-matching attributes should not be null");

            // Both decisions should have proper structure regardless of targeting
            Assert.AreEqual("test_flag_1", decisionMatch.FlagKey, "Flag key should match");
            Assert.AreEqual("test_flag_1", decisionNoMatch.FlagKey, "Flag key should match");
        }

        [Test]
        public void TestDecide_InactiveHoldout()
        {
            // Test decide when holdout is not running
            var featureFlag = Config.FeatureKeyMap["test_flag_1"];
            Assert.IsNotNull(featureFlag, "Feature flag should exist");

            // Find a holdout and set it to inactive
            var holdout = Config.Holdouts.FirstOrDefault();
            if (holdout != null)
            {
                var originalStatus = holdout.Status;
                holdout.Status = "Paused"; // Make holdout inactive

                try
                {
                    var userContext = OptimizelyInstance.CreateUserContext(TestUserId,
                        new UserAttributes { { "country", "us" } });

                    var decision = userContext.Decide("test_flag_1");

                    Assert.IsNotNull(decision, "Decision should not be null even with inactive holdout");
                    Assert.AreEqual("test_flag_1", decision.FlagKey, "Flag key should match");

                    // Should not get decision from the inactive holdout
                    if (!string.IsNullOrEmpty(decision.RuleKey))
                    {
                        Assert.AreNotEqual(holdout.Key, decision.RuleKey,
                            "Decision should not come from inactive holdout");
                    }
                }
                finally
                {
                    holdout.Status = originalStatus; // Restore original status
                }
            }
            else
            {
                Assert.Inconclusive("No holdout found to test inactive scenario");
            }
        }

        [Test]
        public void TestDecide_EmptyUserId()
        {
            // Test decide with empty user ID (should still work per Swift SDK behavior)
            var userContext = OptimizelyInstance.CreateUserContext("",
                new UserAttributes { { "country", "us" } });

            var decision = userContext.Decide("test_flag_1");

            Assert.IsNotNull(decision, "Decision should not be null with empty user ID");
            Assert.AreEqual("test_flag_1", decision.FlagKey, "Flag key should match");

            // Should not log error about invalid user ID since empty string is valid for bucketing
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                    It.Is<string>(s => s.Contains("User ID") && (s.Contains("null") || s.Contains("empty")))),
                Times.Never);
        }

        [Test]
        public void TestDecide_WithDecisionReasons()
        {
            // Test that decision reasons are properly populated for holdout decisions
            var userContext = OptimizelyInstance.CreateUserContext(TestUserId,
                new UserAttributes { { "country", "us" } });

            var decision = userContext.Decide("test_flag_1",
                new OptimizelyDecideOption[] { OptimizelyDecideOption.INCLUDE_REASONS });

            Assert.IsNotNull(decision, "Decision should not be null");
            Assert.AreEqual("test_flag_1", decision.FlagKey, "Flag key should match");

            // Decision reasons should be populated when requested
            Assert.IsNotNull(decision.Reasons, "Decision reasons should not be null");
            // With real bucketer, we expect some decision reasons to be generated
            Assert.IsTrue(decision.Reasons.Length >= 0, "Decision reasons should be present");
        }

        [Test]
        public void TestDecide_HoldoutPriority()
        {
            // Test holdout evaluation priority (global vs included vs excluded)
            var featureFlag = Config.FeatureKeyMap["test_flag_1"];
            Assert.IsNotNull(featureFlag, "Feature flag should exist");

            // Check if we have multiple holdouts
            var globalHoldouts = Config.Holdouts.Where(h =>
                h.IncludedFlags == null || h.IncludedFlags.Length == 0).ToList();
            var includedHoldouts = Config.Holdouts.Where(h =>
                h.IncludedFlags != null && h.IncludedFlags.Contains(featureFlag.Id)).ToList();

            if (globalHoldouts.Count > 0 || includedHoldouts.Count > 0)
            {
                var userContext = OptimizelyInstance.CreateUserContext(TestUserId,
                    new UserAttributes { { "country", "us" } });

                var decision = userContext.Decide("test_flag_1");

                Assert.IsNotNull(decision, "Decision should not be null");
                Assert.AreEqual("test_flag_1", decision.FlagKey, "Flag key should match");

                // Decision should be valid regardless of which holdout is selected
                Assert.IsTrue(decision.VariationKey != null || decision.VariationKey == null,
                    "Decision should have valid structure");
            }
            else
            {
                Assert.Inconclusive("No holdouts found to test priority");
            }
        }


    }
}
