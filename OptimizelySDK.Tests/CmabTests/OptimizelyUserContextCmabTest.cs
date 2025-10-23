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
using System.Linq;
using System.Reflection;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OptimizelySDK.Bucketing;
using OptimizelySDK.Cmab;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Logger;
using OptimizelySDK.Notifications;
using OptimizelySDK.OptimizelyDecisions;
using OptimizelySDK.Tests.NotificationTests;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests.CmabTests
{
    [TestFixture]
    public class OptimizelyUserContextCmabTest
    {
        private Mock<ILogger> _loggerMock;
        private Mock<IErrorHandler> _errorHandlerMock;
        private Mock<IEventDispatcher> _eventDispatcherMock;
        private TestCmabService _cmabService;
        private Mock<TestNotificationCallbacks> _notificationCallbackMock;
        private Optimizely _optimizely;
        private ProjectConfig _config;

        private const string TEST_USER_ID = "test_user_cmab";
        private const string TEST_FEATURE_KEY = "multi_variate_feature";
        private const string TEST_EXPERIMENT_KEY = "test_experiment_multivariate";
        private const string TEST_EXPERIMENT_ID = "122230";
        private const string VARIATION_A_ID = "122231";
        private const string VARIATION_A_KEY = "Fred";
        private const string TEST_CMAB_UUID = "uuid-cmab-123";
        private const string DEVICE_TYPE_ATTRIBUTE_ID = "7723280020";
        private const string DEVICE_TYPE_ATTRIBUTE_KEY = "device_type";
        private const string BROWSER_TYPE_ATTRIBUTE_KEY = "browser_type";

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger>();
            _errorHandlerMock = new Mock<IErrorHandler>();
            _eventDispatcherMock = new Mock<IEventDispatcher>();
            _cmabService = new TestCmabService
            {
                DefaultDecision = new CmabDecision(VARIATION_A_ID, TEST_CMAB_UUID)
            };
            _notificationCallbackMock = new Mock<TestNotificationCallbacks>();

            _config = DatafileProjectConfig.Create(TestData.Datafile, _loggerMock.Object,
                _errorHandlerMock.Object);

            ConfigureCmabExperiment(_config, TEST_EXPERIMENT_ID, TEST_EXPERIMENT_KEY);

            // Create Optimizely with mocked CMAB service using ConfigManager
            var configManager = new FallbackProjectConfigManager(_config);
            _optimizely = new Optimizely(configManager, null, _eventDispatcherMock.Object,
                _loggerMock.Object, _errorHandlerMock.Object);

            // Replace decision service with one that has our mock CMAB service
            var decisionService = new DecisionService(new Bucketer(_loggerMock.Object),
                _errorHandlerMock.Object, null, _loggerMock.Object, _cmabService);

            SetDecisionService(_optimizely, decisionService);
        }

        /// <summary>
        /// Verifies Decide returns decision with CMAB UUID populated
        /// </summary>
        [Test]
        public void TestDecideWithCmabExperimentReturnsDecision()
        {
            var userContext = CreateCmabUserContext();
            var decision = userContext.Decide(TEST_FEATURE_KEY);

            Assert.IsNotNull(decision);
            Assert.AreEqual(VARIATION_A_KEY, decision.VariationKey);
            Assert.IsTrue(decision.Enabled, "Feature flag should be enabled for CMAB variation.");
            Assert.AreEqual(TEST_FEATURE_KEY, decision.FlagKey);
            Assert.AreEqual(TEST_EXPERIMENT_KEY, decision.RuleKey);
            Assert.AreEqual(TEST_CMAB_UUID, decision.CmabUuid);
            Assert.IsTrue(decision.Reasons == null || decision.Reasons.Length == 0);

            Assert.AreEqual(1, _cmabService.CallCount);
            Assert.AreEqual(TEST_EXPERIMENT_ID, _cmabService.LastRuleId);
        }

        /// <summary>
        /// Verifies impression event is sent with CMAB UUID in metadata
        /// </summary>
        [Test]
        public void TestDecideWithCmabExperimentVerifyImpressionEvent()
        {
            var userContext = CreateCmabUserContext();
            LogEvent impressionEvent = null;

            _eventDispatcherMock.Setup(d => d.DispatchEvent(It.IsAny<LogEvent>()))
                .Callback<LogEvent>(e => impressionEvent = e);

            var decision = userContext.Decide(TEST_FEATURE_KEY);

            Assert.IsNotNull(decision);
            Assert.AreEqual(TEST_CMAB_UUID, decision.CmabUuid);
            _eventDispatcherMock.Verify(d => d.DispatchEvent(It.IsAny<LogEvent>()), Times.Once);
            Assert.IsNotNull(impressionEvent, "Impression event should be dispatched.");

            var payload = JObject.Parse(impressionEvent.GetParamsAsJson());
            var cmabUuidToken =
                payload.SelectToken("visitors[0].snapshots[0].decisions[0].metadata.cmab_uuid");

            Assert.IsNotNull(cmabUuidToken, "Metadata should include CMAB UUID.");
            Assert.AreEqual(TEST_CMAB_UUID, cmabUuidToken.Value<string>());
            Assert.AreEqual(1, _cmabService.CallCount);
        }

        /// <summary>
        /// Verifies IsFeatureEnabled sends impression event including CMAB UUID in metadata
        /// </summary>
        [Test]
        public void TestIsFeatureEnabledDispatchesCmabUuidInImpressionEvent()
        {
            LogEvent impressionEvent = null;

            _eventDispatcherMock.Setup(d => d.DispatchEvent(It.IsAny<LogEvent>()))
                .Callback<LogEvent>(e => impressionEvent = e);

            var attributes = new UserAttributes
            {
                { DEVICE_TYPE_ATTRIBUTE_KEY, "mobile" },
                { BROWSER_TYPE_ATTRIBUTE_KEY, "chrome" },
            };

            var featureEnabled = _optimizely.IsFeatureEnabled(TEST_FEATURE_KEY, TEST_USER_ID,
                attributes);

            Assert.IsTrue(featureEnabled, "Feature flag should be enabled for CMAB variation.");
            _eventDispatcherMock.Verify(d => d.DispatchEvent(It.IsAny<LogEvent>()), Times.Once,
                "Impression event should be dispatched for IsFeatureEnabled calls.");
            Assert.IsNotNull(impressionEvent, "Impression event should be captured.");

            var payload = JObject.Parse(impressionEvent.GetParamsAsJson());
            var cmabUuidToken =
                payload.SelectToken("visitors[0].snapshots[0].decisions[0].metadata.cmab_uuid");

            Assert.IsNotNull(cmabUuidToken, "Metadata should include CMAB UUID.");
            Assert.AreEqual(TEST_CMAB_UUID, cmabUuidToken.Value<string>());
            Assert.AreEqual(1, _cmabService.CallCount);
        }

        /// <summary>
        /// Verifies no impression event sent when DISABLE_DECISION_EVENT option is used
        /// </summary>
        [Test]
        public void TestDecideWithCmabExperimentDisableDecisionEvent()
        {
            var userContext = CreateCmabUserContext();

            var decision = userContext.Decide(TEST_FEATURE_KEY,
                new[] { OptimizelyDecideOption.DISABLE_DECISION_EVENT });

            Assert.IsNotNull(decision);
            Assert.AreEqual(TEST_CMAB_UUID, decision.CmabUuid);
            _eventDispatcherMock.Verify(d => d.DispatchEvent(It.IsAny<LogEvent>()), Times.Never,
                "No impression event should be sent with DISABLE_DECISION_EVENT");
            Assert.AreEqual(1, _cmabService.CallCount);
            Assert.IsTrue(_cmabService.OptionsPerCall[0]
                .Contains(OptimizelyDecideOption.DISABLE_DECISION_EVENT));
        }

        /// <summary>
        /// Verifies DecideForKeys works with mix of CMAB and non-CMAB flags
        /// </summary>
        [Test]
        public void TestDecideForKeysMixedCmabAndNonCmab()
        {
            var userContext = CreateCmabUserContext();
            var featureKeys = new[] { TEST_FEATURE_KEY, "boolean_single_variable_feature" };
            var decisions = userContext.DecideForKeys(featureKeys);

            Assert.IsNotNull(decisions);
            Assert.AreEqual(2, decisions.Count);
            Assert.IsTrue(decisions.ContainsKey(TEST_FEATURE_KEY));
            Assert.IsTrue(decisions.ContainsKey("boolean_single_variable_feature"));

            var cmabDecision = decisions[TEST_FEATURE_KEY];
            var nonCmabDecision = decisions["boolean_single_variable_feature"];

            Assert.IsNotNull(cmabDecision);
            Assert.AreEqual(VARIATION_A_KEY, cmabDecision.VariationKey);
            Assert.AreEqual(TEST_CMAB_UUID, cmabDecision.CmabUuid);

            Assert.IsNotNull(nonCmabDecision);
            Assert.IsNull(nonCmabDecision.CmabUuid);
            Assert.AreEqual(1, _cmabService.CallCount);
        }

        /// <summary>
        /// Verifies DecideAll includes CMAB experiment decisions
        /// </summary>
        [Test]
        public void TestDecideAllIncludesCmabExperiments()
        {
            var userContext = CreateCmabUserContext();
            var decisions = userContext.DecideAll();

            Assert.IsNotNull(decisions);
            Assert.IsTrue(decisions.Count > 0, "Should return decisions for all feature flags");
            Assert.IsTrue(decisions.TryGetValue(TEST_FEATURE_KEY, out var cmabDecision));
            Assert.IsNotNull(cmabDecision);
            Assert.AreEqual(VARIATION_A_KEY, cmabDecision.VariationKey);
            Assert.AreEqual(TEST_CMAB_UUID, cmabDecision.CmabUuid);
            Assert.GreaterOrEqual(_cmabService.CallCount, 1);
        }

        /// <summary>
        /// Verifies IGNORE_CMAB_CACHE option is passed correctly to decision flow
        /// </summary>
        [Test]
        public void TestDecideWithCmabExperimentIgnoreCmabCache()
        {
            var userContext = CreateCmabUserContext();

            var decision1 = userContext.Decide(TEST_FEATURE_KEY,
                new[] { OptimizelyDecideOption.IGNORE_CMAB_CACHE });
            var decision2 = userContext.Decide(TEST_FEATURE_KEY,
                new[] { OptimizelyDecideOption.IGNORE_CMAB_CACHE });

            Assert.IsNotNull(decision1);
            Assert.IsNotNull(decision2);
            Assert.AreEqual(VARIATION_A_KEY, decision1.VariationKey);
            Assert.AreEqual(VARIATION_A_KEY, decision2.VariationKey);
            Assert.AreEqual(2, _cmabService.CallCount);
            Assert.IsTrue(_cmabService.OptionsPerCall.All(options =>
                options.Contains(OptimizelyDecideOption.IGNORE_CMAB_CACHE)));
        }

        /// <summary>
        /// Verifies RESET_CMAB_CACHE option clears entire cache
        /// </summary>
        [Test]
        public void TestDecideWithCmabExperimentResetCmabCache()
        {
            var userContext = CreateCmabUserContext();

            var decision1 = userContext.Decide(TEST_FEATURE_KEY);

            var decision2 = userContext.Decide(TEST_FEATURE_KEY,
                new[] { OptimizelyDecideOption.RESET_CMAB_CACHE });

            Assert.IsNotNull(decision1);
            Assert.IsNotNull(decision2);
            Assert.AreEqual(VARIATION_A_KEY, decision1.VariationKey);
            Assert.AreEqual(VARIATION_A_KEY, decision2.VariationKey);
            Assert.AreEqual(2, _cmabService.CallCount);
            Assert.IsFalse(_cmabService.OptionsPerCall[0]
                .Contains(OptimizelyDecideOption.RESET_CMAB_CACHE));
            Assert.IsTrue(_cmabService.OptionsPerCall[1]
                .Contains(OptimizelyDecideOption.RESET_CMAB_CACHE));
        }

        /// <summary>
        /// Verifies INVALIDATE_USER_CMAB_CACHE option is passed correctly to decision flow
        /// </summary>
        [Test]
        public void TestDecideWithCmabExperimentInvalidateUserCmabCache()
        {
            // Arrange
            var userContext1 = CreateCmabUserContext();
            var userContext2 = CreateCmabUserContext("other_user");

            var decision1 = userContext1.Decide(TEST_FEATURE_KEY);

            var decision2 = userContext2.Decide(TEST_FEATURE_KEY);

            var decision3 = userContext1.Decide(TEST_FEATURE_KEY,
                new[] { OptimizelyDecideOption.INVALIDATE_USER_CMAB_CACHE });

            Assert.IsNotNull(decision1);
            Assert.IsNotNull(decision2);
            Assert.IsNotNull(decision3);
            Assert.AreEqual(3, _cmabService.CallCount);
            Assert.IsTrue(_cmabService.OptionsPerCall[2]
                .Contains(OptimizelyDecideOption.INVALIDATE_USER_CMAB_CACHE));
        }

        /// <summary>
        /// Verifies User Profile Service integration with CMAB experiments
        /// </summary>
        [Test]
        public void TestDecideWithCmabExperimentUserProfileService()
        {
            var userProfileServiceMock = new Mock<UserProfileService>();
            userProfileServiceMock.Setup(ups => ups.Save(It.IsAny<Dictionary<string, object>>()))
                .Callback<Dictionary<string, object>>(_ => { });

            var configManager = new FallbackProjectConfigManager(_config);
            var optimizelyWithUps = new Optimizely(configManager, null, _eventDispatcherMock.Object,
                _loggerMock.Object, _errorHandlerMock.Object, userProfileServiceMock.Object);

            var cmabService = new TestCmabService
            {
                DefaultDecision = new CmabDecision(VARIATION_A_ID, TEST_CMAB_UUID)
            };
            var decisionService = new DecisionService(new Bucketer(_loggerMock.Object),
                _errorHandlerMock.Object, userProfileServiceMock.Object, _loggerMock.Object,
                cmabService);
            SetDecisionService(optimizelyWithUps, decisionService);

            var userContext = CreateCmabUserContext(optimizely: optimizelyWithUps);

            var decision = userContext.Decide(TEST_FEATURE_KEY);

            Assert.IsNotNull(decision);
            Assert.AreEqual(VARIATION_A_KEY, decision.VariationKey);
            Assert.AreEqual(TEST_CMAB_UUID, decision.CmabUuid);
            userProfileServiceMock.Verify(ups => ups.Save(It.IsAny<Dictionary<string, object>>()),
                Times.Never);
            Assert.AreEqual(1, cmabService.CallCount);
        }

        /// <summary>
        /// Verifies IGNORE_USER_PROFILE_SERVICE option skips UPS lookup
        /// </summary>
        [Test]
        public void TestDecideWithCmabExperimentIgnoreUserProfileService()
        {
            var userProfileServiceMock = new Mock<UserProfileService>();

            var configManager = new FallbackProjectConfigManager(_config);
            var optimizelyWithUps = new Optimizely(configManager, null, _eventDispatcherMock.Object,
                _loggerMock.Object, _errorHandlerMock.Object, userProfileServiceMock.Object);

            var cmabService = new TestCmabService
            {
                DefaultDecision = new CmabDecision(VARIATION_A_ID, TEST_CMAB_UUID)
            };
            var decisionService = new DecisionService(new Bucketer(_loggerMock.Object),
                _errorHandlerMock.Object, userProfileServiceMock.Object, _loggerMock.Object,
                cmabService);
            SetDecisionService(optimizelyWithUps, decisionService);

            var userContext = CreateCmabUserContext(optimizely: optimizelyWithUps);

            var decision = userContext.Decide(TEST_FEATURE_KEY,
                new[] { OptimizelyDecideOption.IGNORE_USER_PROFILE_SERVICE });

            Assert.IsNotNull(decision);
            Assert.AreEqual(VARIATION_A_KEY, decision.VariationKey);
            Assert.AreEqual(TEST_CMAB_UUID, decision.CmabUuid);

            userProfileServiceMock.Verify(ups => ups.Lookup(It.IsAny<string>()), Times.Never);
            Assert.AreEqual(1, cmabService.CallCount);
            Assert.IsTrue(cmabService.OptionsPerCall[0]
                .Contains(OptimizelyDecideOption.IGNORE_USER_PROFILE_SERVICE));
        }

        /// <summary>
        /// Verifies INCLUDE_REASONS option includes CMAB decision info
        /// </summary>
        [Test]
        public void TestDecideWithCmabExperimentIncludeReasons()
        {
            var userContext = CreateCmabUserContext();

            var decision = userContext.Decide(TEST_FEATURE_KEY,
                new[] { OptimizelyDecideOption.INCLUDE_REASONS });

            Assert.IsNotNull(decision);
            Assert.IsNotNull(decision.Reasons);
            var expectedMessage = string.Format(CmabConstants.CMAB_DECISION_FETCHED, TEST_USER_ID,
                TEST_EXPERIMENT_KEY);
            Assert.IsTrue(decision.Reasons.Any(r => r.Contains(expectedMessage)),
                "Decision reasons should include CMAB fetch success message.");
            Assert.AreEqual(TEST_CMAB_UUID, decision.CmabUuid);
            Assert.AreEqual(1, _cmabService.CallCount);
        }

        /// <summary>
        /// Verifies error handling when CMAB service fails
        /// </summary>
        [Test]
        public void TestDecideWithCmabErrorReturnsErrorDecision()
        {
            var userContext = CreateCmabUserContext();

            _cmabService.ReturnNullNext = true;

            var decision = userContext.Decide(TEST_FEATURE_KEY,
                new[] { OptimizelyDecideOption.INCLUDE_REASONS });

            Assert.IsNotNull(decision);
            Assert.IsNull(decision.VariationKey);
            Assert.IsNull(decision.CmabUuid);
            Assert.IsTrue(decision.Reasons.Any(r => r.Contains(
                string.Format(CmabConstants.CMAB_FETCH_FAILED, TEST_EXPERIMENT_KEY))));
            Assert.AreEqual(1, _cmabService.CallCount);
        }

        /// <summary>
        /// Verifies decision notification is called for CMAB experiments
        /// </summary>
        [Test]
        public void TestDecideWithCmabExperimentDecisionNotification()
        {
            var notificationCenter = new NotificationCenter(_loggerMock.Object);
            Dictionary<string, object> capturedDecisionInfo = null;

            _notificationCallbackMock.Setup(nc => nc.TestDecisionCallback(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<UserAttributes>(),
                    It.IsAny<Dictionary<string, object>>()))
                .Callback((string type, string userId, UserAttributes attrs,
                    Dictionary<string, object> decisionInfo) => capturedDecisionInfo = decisionInfo);

            notificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                _notificationCallbackMock.Object.TestDecisionCallback);

            var configManager = new FallbackProjectConfigManager(_config);
            var optimizelyWithNotifications = new Optimizely(configManager, notificationCenter,
                _eventDispatcherMock.Object, _loggerMock.Object, _errorHandlerMock.Object);

            var cmabService = new TestCmabService
            {
                DefaultDecision = new CmabDecision(VARIATION_A_ID, TEST_CMAB_UUID)
            };
            var decisionService = new DecisionService(new Bucketer(_loggerMock.Object),
                _errorHandlerMock.Object, null, _loggerMock.Object, cmabService);
            SetDecisionService(optimizelyWithNotifications, decisionService);

            var userContext = CreateCmabUserContext(optimizely: optimizelyWithNotifications);

            var decision = userContext.Decide(TEST_FEATURE_KEY);

            Assert.IsNotNull(decision);
            Assert.AreEqual(TEST_FEATURE_KEY, decision.FlagKey);
            Assert.AreEqual(VARIATION_A_KEY, decision.VariationKey);
            Assert.AreEqual(TEST_CMAB_UUID, decision.CmabUuid);
            _notificationCallbackMock.Verify(nc => nc.TestDecisionCallback(
                    DecisionNotificationTypes.FLAG,
                    TEST_USER_ID,
                    It.IsAny<UserAttributes>(),
                    It.IsAny<Dictionary<string, object>>()),
                Times.Once);
            Assert.IsNotNull(capturedDecisionInfo);
            Assert.AreEqual(TEST_FEATURE_KEY, capturedDecisionInfo["flagKey"]);
            Assert.AreEqual(VARIATION_A_KEY, capturedDecisionInfo["variationKey"]);
            Assert.AreEqual(1, cmabService.CallCount);
        }

        private OptimizelyUserContext CreateCmabUserContext(string userId = TEST_USER_ID,
            Optimizely optimizely = null,
            IDictionary<string, object> additionalAttributes = null)
        {
            var client = optimizely ?? _optimizely;
            var userContext = client.CreateUserContext(userId);

            userContext.SetAttribute(BROWSER_TYPE_ATTRIBUTE_KEY, "chrome");
            userContext.SetAttribute(DEVICE_TYPE_ATTRIBUTE_KEY, "mobile");

            if (additionalAttributes != null)
            {
                foreach (var kvp in additionalAttributes)
                {
                    userContext.SetAttribute(kvp.Key, kvp.Value);
                }
            }

            return userContext;
        }

        private static void SetDecisionService(Optimizely optimizely, DecisionService decisionService)
        {
            var decisionServiceField = typeof(Optimizely).GetField("DecisionService",
                BindingFlags.NonPublic | BindingFlags.Instance);
            decisionServiceField?.SetValue(optimizely, decisionService);
        }

        private void ConfigureCmabExperiment(ProjectConfig config,
            string experimentId,
            string experimentKey,
            int trafficAllocation = 10000,
            IEnumerable<string> attributeIds = null)
        {
            Assert.IsNotNull(config, "Project config should be available for CMAB tests.");

            var attributeList = attributeIds?.ToList() ??
                new List<string> { DEVICE_TYPE_ATTRIBUTE_ID };

            var experiment = config.ExperimentIdMap.TryGetValue(experimentId, out var existing)
                ? existing
                : config.GetExperimentFromKey(experimentKey);

            Assert.IsNotNull(experiment, $"Experiment {experimentKey} should exist for CMAB tests.");

            experiment.Cmab = new Entity.Cmab(attributeList)
            {
                TrafficAllocation = trafficAllocation
            };

            config.ExperimentIdMap[experiment.Id] = experiment;
            if (config.ExperimentKeyMap.ContainsKey(experiment.Key))
            {
                config.ExperimentKeyMap[experiment.Key] = experiment;
            }
        }

        private class TestCmabService : ICmabService
        {
            public int CallCount { get; private set; }

            public string LastRuleId { get; private set; }

            public OptimizelyUserContext LastUserContext { get; private set; }

            public List<OptimizelyDecideOption[]> OptionsPerCall { get; } =
                new List<OptimizelyDecideOption[]>();

            public Queue<CmabDecision> DecisionsQueue { get; } = new Queue<CmabDecision>();

            public CmabDecision DefaultDecision { get; set; }

            public Exception ExceptionToThrow { get; set; }

            public bool ReturnNullNext { get; set; }

            public Func<ProjectConfig, OptimizelyUserContext, string, OptimizelyDecideOption[],
                    CmabDecision>
                Handler { get; set; }

            public void EnqueueDecision(CmabDecision decision)
            {
                DecisionsQueue.Enqueue(decision);
            }

            public CmabDecision GetDecision(ProjectConfig projectConfig,
                OptimizelyUserContext userContext,
                string ruleId,
                OptimizelyDecideOption[] options = null)
            {
                CallCount++;
                LastRuleId = ruleId;
                LastUserContext = userContext;
                var copiedOptions = options?.ToArray() ?? new OptimizelyDecideOption[0];
                OptionsPerCall.Add(copiedOptions);

                if (ExceptionToThrow != null)
                {
                    var ex = ExceptionToThrow;
                    ExceptionToThrow = null;
                    throw ex;
                }

                if (Handler != null)
                {
                    return Handler(projectConfig, userContext, ruleId, copiedOptions);
                }

                if (ReturnNullNext)
                {
                    ReturnNullNext = false;
                    return null;
                }

                if (DecisionsQueue.Count > 0)
                {
                    return DecisionsQueue.Dequeue();
                }

                return DefaultDecision;
            }
        }
    }
}
