/**
 *
 *    Copyright 2020-2021, Optimizely and contributors
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */

using Castle.Core.Internal;
using Moq;
using NUnit.Framework;
using OptimizelySDK.Bucketing;
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
using System;
using System.Collections.Generic;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class OptimizelyUserContextTest
    {
        string UserID = "testUserID";
        private Optimizely Optimizely;
        private Mock<ILogger> LoggerMock;
        private Mock<IErrorHandler> ErrorHandlerMock;
        private Mock<IEventDispatcher> EventDispatcherMock;
        private Mock<TestNotificationCallbacks> NotificationCallbackMock;

        [SetUp]
        public void SetUp()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            NotificationCallbackMock = new Mock<TestNotificationCallbacks>();

            ErrorHandlerMock = new Mock<IErrorHandler>();
            ErrorHandlerMock.Setup(e => e.HandleError(It.IsAny<Exception>()));
            EventDispatcherMock = new Mock<IEventDispatcher>();

            Optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object);
        }

        [Test]
        public void OptimizelyUserContextWithAttributes()
        {
            var attributes = new UserAttributes() { { "house", "GRYFFINDOR" } };
            OptimizelyUserContext user = new OptimizelyUserContext(Optimizely, UserID, attributes, ErrorHandlerMock.Object, LoggerMock.Object);

            Assert.AreEqual(user.GetOptimizely(), Optimizely);
            Assert.AreEqual(user.GetUserId(), UserID);
            Assert.AreEqual(user.GetAttributes(), attributes);
        }

        [Test]
        public void OptimizelyUserContextNoAttributes()
        {
            OptimizelyUserContext user = new OptimizelyUserContext(Optimizely, UserID, null, ErrorHandlerMock.Object, LoggerMock.Object);

            Assert.AreEqual(user.GetOptimizely(), Optimizely);
            Assert.AreEqual(user.GetUserId(), UserID);
            Assert.True(user.GetAttributes().Count == 0);
        }

        [Test]
        public void SetAttribute()
        {
            var attributes = new UserAttributes() { { "house", "GRYFFINDOR" } };
            OptimizelyUserContext user = new OptimizelyUserContext(Optimizely, UserID, attributes, ErrorHandlerMock.Object, LoggerMock.Object);

            user.SetAttribute("k1", "v1");
            user.SetAttribute("k2", true);
            user.SetAttribute("k3", 100);
            user.SetAttribute("k4", 3.5);

            Assert.AreEqual(user.GetOptimizely(), Optimizely);
            Assert.AreEqual(user.GetUserId(), UserID);
            var newAttributes = user.GetAttributes();
            Assert.AreEqual(newAttributes["house"], "GRYFFINDOR");
            Assert.AreEqual(newAttributes["k1"], "v1");
            Assert.AreEqual(newAttributes["k2"], true);
            Assert.AreEqual(newAttributes["k3"], 100);
            Assert.AreEqual(newAttributes["k4"], 3.5);
        }

        [Test]
        public void SetAttributeNoAttribute()
        {
            OptimizelyUserContext user = new OptimizelyUserContext(Optimizely, UserID, null, ErrorHandlerMock.Object, LoggerMock.Object);

            user.SetAttribute("k1", "v1");
            user.SetAttribute("k2", true);

            Assert.AreEqual(user.GetOptimizely(), Optimizely);
            Assert.AreEqual(user.GetUserId(), UserID);
            var newAttributes = user.GetAttributes();
            Assert.AreEqual(newAttributes["k1"], "v1");
            Assert.AreEqual(newAttributes["k2"], true);
        }

        [Test]
        public void SetAttributeOverride()
        {
            var attributes = new UserAttributes() { { "house", "GRYFFINDOR" } };
            OptimizelyUserContext user = new OptimizelyUserContext(Optimizely, UserID, attributes, ErrorHandlerMock.Object, LoggerMock.Object);

            user.SetAttribute("k1", "v1");
            user.SetAttribute("house", "v2");

            var newAttributes = user.GetAttributes();
            Assert.AreEqual(newAttributes["k1"], "v1");
            Assert.AreEqual(newAttributes["house"], "v2");
        }

        [Test]
        public void SetAttributeNullValue()
        {
            var attributes = new UserAttributes() { { "k1", null } };
            OptimizelyUserContext user = new OptimizelyUserContext(Optimizely, UserID, attributes, ErrorHandlerMock.Object, LoggerMock.Object);

            var newAttributes = user.GetAttributes();
            Assert.AreEqual(newAttributes["k1"], null);

            user.SetAttribute("k1", true);
            newAttributes = user.GetAttributes();
            Assert.AreEqual(newAttributes["k1"], true);

            user.SetAttribute("k1", null);
            newAttributes = user.GetAttributes();
            Assert.AreEqual(newAttributes["k1"], null);
        }

        [Test]
        public void SetAttributeToOverrideAttribute()
        {
            OptimizelyUserContext user = new OptimizelyUserContext(Optimizely, UserID, null, ErrorHandlerMock.Object, LoggerMock.Object);


            Assert.AreEqual(user.GetOptimizely(), Optimizely);
            Assert.AreEqual(user.GetUserId(), UserID);

            user.SetAttribute("k1", "v1");
            Assert.AreEqual(user.GetAttributes()["k1"], "v1");

            user.SetAttribute("k1", true);
            Assert.AreEqual(user.GetAttributes()["k1"], true);
        }

        #region decide

        [Test]
        public void TestDecide()
        {
            var flagKey = "multi_variate_feature";
            var variablesExpected = Optimizely.GetAllFeatureVariables(flagKey, UserID);

            var user = Optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");
            var decision = user.Decide(flagKey);

            Assert.AreEqual(decision.VariationKey, "Gred");
            Assert.False(decision.Enabled);
            Assert.AreEqual(decision.Variables.ToDictionary(), variablesExpected.ToDictionary());
            Assert.AreEqual(decision.RuleKey, "test_experiment_multivariate");
            Assert.AreEqual(decision.FlagKey, flagKey);
            Assert.AreNotEqual(decision.UserContext, user);
            Assert.IsTrue(TestData.CompareObjects(decision.UserContext, user));
            Assert.AreEqual(decision.Reasons.Length, 0);
        }

        [Test]
        public void DecideInvalidFlagKey()
        {
            var flagKey = "invalid_feature";

            var user = Optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");

            var decisionExpected = OptimizelyDecision.NewErrorDecision(
                flagKey,
                user,
                DecisionMessage.Reason(DecisionMessage.FLAG_KEY_INVALID, flagKey),
                ErrorHandlerMock.Object,
                LoggerMock.Object);
            var decision = user.Decide(flagKey);

            Assert.IsTrue(TestData.CompareObjects(decision, decisionExpected));
        }

        [Test]
        public void DecideWhenConfigIsNull()
        {
            Optimizely optimizely = new Optimizely(TestData.UnsupportedVersionDatafile, EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object);

            var flagKey = "multi_variate_feature";
            var decisionExpected = OptimizelyDecision.NewErrorDecision(
                flagKey,
                new OptimizelyUserContext(optimizely, UserID, new UserAttributes(), ErrorHandlerMock.Object, LoggerMock.Object),
                DecisionMessage.SDK_NOT_READY,
                ErrorHandlerMock.Object,
                LoggerMock.Object);
            var user = optimizely.CreateUserContext(UserID);
            var decision = user.Decide(flagKey);

            Assert.IsTrue(TestData.CompareObjects(decision, decisionExpected));
        }
        #endregion

        #region decideAll

        [Test]
        public void DecideForKeysWithOneFlag()
        {
            var flagKey = "multi_variate_feature";
            var flagKeys = new string[] { flagKey };

            var variablesExpected = Optimizely.GetAllFeatureVariables(flagKey, UserID);

            var user = Optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");

            var decisions = user.DecideForKeys(flagKeys);

            Assert.True(decisions.Count == 1);
            var decision = decisions[flagKey];

            OptimizelyDecision expDecision = new OptimizelyDecision(
            "Gred",
            false,
            variablesExpected,
            "test_experiment_multivariate",
            flagKey,
            user,
            new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decision, expDecision));
        }

        [Test]
        public void DecideAllTwoFlag()
        {
            var flagKey1 = "multi_variate_feature";
            var flagKey2 = "string_single_variable_feature";
            var flagKeys = new string[] { flagKey1, flagKey2 };

            var variablesExpected1 = Optimizely.GetAllFeatureVariables(flagKey1, UserID);
            var variablesExpected2 = Optimizely.GetAllFeatureVariables(flagKey2, UserID);

            var user = Optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");
            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            Optimizely.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);

            var decisions = user.DecideForKeys(flagKeys);

            var userAttributes = new UserAttributes
            {
               { "browser_type", "chrome" }
            };

            Assert.True(decisions.Count == 2);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FLAG, UserID, userAttributes, It.IsAny<Dictionary<string, object>>()),
               Times.Exactly(2));
            OptimizelyDecision expDecision1 = new OptimizelyDecision(
            "Gred",
            false,
            variablesExpected1,
            "test_experiment_multivariate",
            flagKey1,
            user,
            new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey1], expDecision1));

            OptimizelyDecision expDecision2 = new OptimizelyDecision(
            "control",
            true,
            variablesExpected2,
            "test_experiment_with_feature_rollout",
            flagKey2,
            user,
            new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey2], expDecision2));
        }

        [Test]
        public void DecideAllAllFlags()
        {
            var flagKey1 = "boolean_feature";
            var flagKey2 = "double_single_variable_feature";
            var flagKey3 = "integer_single_variable_feature";
            var flagKey4 = "boolean_single_variable_feature";
            var flagKey5 = "string_single_variable_feature";
            var flagKey6 = "multi_variate_feature";
            var flagKey7 = "mutex_group_feature";
            var flagKey8 = "empty_feature";
            var flagKey9 = "no_rollout_experiment_feature";
            var flagKey10 = "unsupported_variabletype";

            var variablesExpected1 = Optimizely.GetAllFeatureVariables(flagKey1, UserID);
            var variablesExpected2 = Optimizely.GetAllFeatureVariables(flagKey2, UserID);
            var variablesExpected3 = Optimizely.GetAllFeatureVariables(flagKey3, UserID);
            var variablesExpected4 = Optimizely.GetAllFeatureVariables(flagKey4, UserID);
            var variablesExpected5 = Optimizely.GetAllFeatureVariables(flagKey5, UserID);
            var variablesExpected6 = Optimizely.GetAllFeatureVariables(flagKey6, UserID);
            var variablesExpected7 = Optimizely.GetAllFeatureVariables(flagKey7, UserID);
            var variablesExpected8 = Optimizely.GetAllFeatureVariables(flagKey8, UserID);
            var variablesExpected9 = Optimizely.GetAllFeatureVariables(flagKey9, UserID);
            var variablesExpected10 = Optimizely.GetAllFeatureVariables(flagKey10, UserID);

            var user = Optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");
            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            Optimizely.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);

            var decisions = user.DecideAll();

            var userAttributes = new UserAttributes
            {
               { "browser_type", "chrome" }
            };

            Assert.True(decisions.Count == 10);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FLAG, UserID, userAttributes, It.IsAny<Dictionary<string, object>>()),
               Times.Exactly(10));

            OptimizelyDecision expDecision1 = new OptimizelyDecision(
            null,
            false,
            variablesExpected1,
            null,
            flagKey1,
            user,
            new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey1], expDecision1));

            OptimizelyDecision expDecision2 = new OptimizelyDecision(
            "variation",
            false,
            variablesExpected2,
            "test_experiment_double_feature",
            flagKey2,
            user,
            new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey2], expDecision2));

            OptimizelyDecision expDecision3 = new OptimizelyDecision(
            "control",
            false,
            variablesExpected3,
            "test_experiment_integer_feature",
            flagKey3,
            user,
            new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey3], expDecision3));

            OptimizelyDecision expDecision4 = new OptimizelyDecision(
            "188881",
            false,
            variablesExpected4,
            "188880",
            flagKey4,
            user,
            new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey4], expDecision4));

            OptimizelyDecision expDecision5 = new OptimizelyDecision(
            "control",
            true,
            variablesExpected5,
            "test_experiment_with_feature_rollout",
            flagKey5,
            user,
            new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey5], expDecision5));

            OptimizelyDecision expDecision6 = new OptimizelyDecision(
            "Gred",
            false,
            variablesExpected6,
            "test_experiment_multivariate",
            flagKey6,
            user,
            new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey6], expDecision6));

            OptimizelyDecision expDecision7 = new OptimizelyDecision(
            null,
            false,
            variablesExpected7,
            null,
            flagKey7,
            user,
            new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey7], expDecision7));

            OptimizelyDecision expDecision8 = new OptimizelyDecision(
            null,
            false,
            variablesExpected8,
            null,
            flagKey8,
            user,
            new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey8], expDecision8));

            OptimizelyDecision expDecision9 = new OptimizelyDecision(
            null,
            false,
            variablesExpected9,
            null,
            flagKey9,
            user,
            new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey9], expDecision9));
            
            OptimizelyDecision expDecision10 = new OptimizelyDecision(
            null,
            false,
            variablesExpected10,
            null,
            flagKey10,
            user,
            new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey10], expDecision10));
        }

        [Test]
        public void DecideAllEnabledFlagsOnlyDecideOptions()
        {
            var flagKey1 = "string_single_variable_feature";

            var variablesExpected1 = Optimizely.GetAllFeatureVariables(flagKey1, UserID);

            var decideOptions = new OptimizelyDecideOption[] { OptimizelyDecideOption.ENABLED_FLAGS_ONLY };
            var user = Optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");

            var decisions = user.DecideAll(decideOptions);

            Assert.True(decisions.Count == 1);

            OptimizelyDecision expDecision1 = new OptimizelyDecision(
            "control",
            true,
            variablesExpected1,
            "test_experiment_with_feature_rollout",
            flagKey1,
            user,
            new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey1], expDecision1));
        }

        [Test]
        public void DecideAllEnabledFlagsDefaultDecideOptions()
        {
            var flagKey1 = "string_single_variable_feature";
            var decideOptions = new OptimizelyDecideOption[] { OptimizelyDecideOption.ENABLED_FLAGS_ONLY };

            var optimizely = new Optimizely(TestData.Datafile,
                EventDispatcherMock.Object,
                LoggerMock.Object,
                ErrorHandlerMock.Object,
                defaultDecideOptions: decideOptions);

            var variablesExpected1 = Optimizely.GetAllFeatureVariables(flagKey1, UserID);

            var user = optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");

            var decisions = user.DecideAll();

            Assert.True(decisions.Count == 1);

            OptimizelyDecision expDecision1 = new OptimizelyDecision(
            "control",
            true,
            variablesExpected1,
            "test_experiment_with_feature_rollout",
            flagKey1,
            user,
            new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey1], expDecision1));
        }

        [Test]
        public void DecideAllEnabledFlagsDefaultDecideOptionsPlusApiOptions()
        {
            var flagKey1 = "string_single_variable_feature";
            var decideOptions = new OptimizelyDecideOption[] { OptimizelyDecideOption.ENABLED_FLAGS_ONLY };

            var optimizely = new Optimizely(TestData.Datafile,
                EventDispatcherMock.Object,
                LoggerMock.Object,
                ErrorHandlerMock.Object,
                defaultDecideOptions: decideOptions);

            var user = optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");
            decideOptions = new OptimizelyDecideOption[] { OptimizelyDecideOption.EXCLUDE_VARIABLES };

            var decisions = user.DecideAll(decideOptions);

            Assert.True(decisions.Count == 1);
            var expectedOptlyJson = new Dictionary<string, object>();
            OptimizelyDecision expDecision1 = new OptimizelyDecision(
            "control",
            true,
            new OptimizelyJSON(expectedOptlyJson, ErrorHandlerMock.Object, LoggerMock.Object),
            "test_experiment_with_feature_rollout",
            flagKey1,
            user,
            new string[] { });
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey1], expDecision1));
        }

        [Test]
        public void DecideExcludeVariablesDecideOptions()
        {
            var flagKey = "multi_variate_feature";
            var variablesExpected = new Dictionary<string, object>();
            var user = Optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");
            var decideOptions = new OptimizelyDecideOption[] { OptimizelyDecideOption.EXCLUDE_VARIABLES };

            var decision = user.Decide(flagKey, decideOptions);

            Assert.AreEqual(decision.VariationKey, "Gred");
            Assert.False(decision.Enabled);
            Assert.AreEqual(decision.Variables.ToDictionary(), variablesExpected);
            Assert.AreEqual(decision.RuleKey, "test_experiment_multivariate");
            Assert.AreEqual(decision.FlagKey, flagKey);
            Assert.AreNotEqual(decision.UserContext, user);
            Assert.IsTrue(TestData.CompareObjects(decision.UserContext, user));
            Assert.True(decision.Reasons.IsNullOrEmpty());
        }

        [Test]
        public void DecideIncludeReasonsDecideOptions()
        {
            var flagKey = "invalid_key";
            var user = Optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");

            var decision = user.Decide(flagKey);
            Assert.True(decision.Reasons.Length == 1);
            Assert.AreEqual(decision.Reasons[0], DecisionMessage.Reason(DecisionMessage.FLAG_KEY_INVALID, flagKey));

            var decideOptions = new OptimizelyDecideOption[] { OptimizelyDecideOption.INCLUDE_REASONS };
            
            decision = user.Decide(flagKey, decideOptions);
            Assert.True(decision.Reasons.Length == 1);
            Assert.AreEqual(decision.Reasons[0], DecisionMessage.Reason(DecisionMessage.FLAG_KEY_INVALID, flagKey));

            flagKey = "multi_variate_feature";
            decision = user.Decide(flagKey);
            Assert.True(decision.Reasons.Length == 0);

            Assert.AreEqual(decision.VariationKey, "Gred");
            Assert.False(decision.Enabled);
            Assert.AreEqual(decision.RuleKey, "test_experiment_multivariate");
            Assert.AreEqual(decision.FlagKey, flagKey);
            Assert.AreNotEqual(decision.UserContext, user);
            Assert.IsTrue(TestData.CompareObjects(decision.UserContext, user));
            Assert.True(decision.Reasons.IsNullOrEmpty());

            decision = user.Decide(flagKey, decideOptions);
            Assert.True(decision.Reasons.Length > 0);
            Assert.AreEqual("User [testUserID] is in variation [Gred] of experiment [test_experiment_multivariate].", decision.Reasons[1]);
            Assert.AreEqual("The user \"testUserID\" is bucketed into experiment \"test_experiment_multivariate\" of feature \"multi_variate_feature\".", decision.Reasons[2]);
        }

        [Test]
        public void TestDoNotSendEventDecide()
        {
            var flagKey = "multi_variate_feature";
            var variablesExpected = Optimizely.GetAllFeatureVariables(flagKey, UserID);

            var optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object);
            var user = optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");

            var decideOptions = new OptimizelyDecideOption[] { OptimizelyDecideOption.DISABLE_DECISION_EVENT };
            var decision = user.Decide(flagKey, decideOptions);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Never);

            decision = user.Decide(flagKey);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Once);

            Assert.AreEqual(decision.VariationKey, "Gred");
            Assert.False(decision.Enabled);
            Assert.AreEqual(decision.Variables.ToDictionary(), variablesExpected.ToDictionary());
            Assert.AreEqual(decision.RuleKey, "test_experiment_multivariate");
            Assert.AreEqual(decision.FlagKey, flagKey);
            Assert.AreNotEqual(decision.UserContext, user);
            Assert.IsTrue(TestData.CompareObjects(decision.UserContext, user));
        }

        [Test]
        public void TestDefaultDecideOptions()
        {
            var flagKey = "multi_variate_feature";
            var variablesExpected = Optimizely.GetAllFeatureVariables(flagKey, UserID);
            var decideOptions = new OptimizelyDecideOption[] { OptimizelyDecideOption.DISABLE_DECISION_EVENT };

            var optimizely = new Optimizely(TestData.Datafile,
                EventDispatcherMock.Object,
                LoggerMock.Object,
                ErrorHandlerMock.Object,
                defaultDecideOptions: decideOptions);

            var user = optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");

            var decision = user.Decide(flagKey);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Never);

            Assert.AreEqual(decision.VariationKey, "Gred");
            Assert.False(decision.Enabled);
            Assert.AreEqual(decision.Variables.ToDictionary(), variablesExpected.ToDictionary());
            Assert.AreEqual(decision.RuleKey, "test_experiment_multivariate");
            Assert.AreEqual(decision.FlagKey, flagKey);
            Assert.AreNotEqual(decision.UserContext, user);
            Assert.IsTrue(TestData.CompareObjects(decision.UserContext, user));
        }

        [Test]
        public void TestDecisionNotification()
        {
            var flagKey = "string_single_variable_feature";
            var variationKey = "control";
            var enabled = true;
            var variables = Optimizely.GetAllFeatureVariables(flagKey, UserID);
            var ruleKey = "test_experiment_with_feature_rollout";
            var reasons = new string[] { };
            var user = Optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");

            var decisionInfo = new Dictionary<string, object>
            {
                { "flagKey", flagKey },
                { "enabled", enabled },
                { "variables", variables.ToDictionary() },
                { "variationKey", variationKey },
                { "ruleKey", ruleKey },
                { "reasons", reasons },
                { "decisionEventDispatched", true },
            };
         
            var userAttributes = new UserAttributes
            {
               { "browser_type", "chrome" }
            };

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            Optimizely.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);

            user.Decide(flagKey);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FLAG, UserID, userAttributes, It.Is<Dictionary<string, object>>(info =>
               TestData.CompareObjects(info, decisionInfo))), 
               Times.Once); 
        }

        [Test]
        public void TestDecideOptionsByPassUPS()
        {
            var userProfileServiceMock = new Mock<UserProfileService>();
            var flagKey = "string_single_variable_feature";

            var experimentId = "122235";
            var userId = "testUser3";
            var variationKey = "control";
            var fbVariationId = "122237";
            var fbVariationKey = "variation";


            var userProfile = new UserProfile(userId, new Dictionary<string, Decision>
            {
                { experimentId, new Decision(fbVariationId)}
            });

            userProfileServiceMock.Setup(_ => _.Lookup(userId)).Returns(userProfile.ToMap());

            var optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object, userProfileServiceMock.Object);

            var user = optimizely.CreateUserContext(userId);
            
            var projectConfig = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, ErrorHandlerMock.Object);
            
            var variationUserProfile = user.Decide(flagKey);
            Assert.AreEqual(fbVariationKey, variationUserProfile.VariationKey);

            var decideOptions = new OptimizelyDecideOption[] { OptimizelyDecideOption.IGNORE_USER_PROFILE_SERVICE };
            variationUserProfile = user.Decide(flagKey, decideOptions);
            Assert.AreEqual(variationKey, variationUserProfile.VariationKey);
        }

        [Test]
        public void TestDecideOptionsByPassUPSNeverCallsSaveVariation()
        {
            var userProfileServiceMock = new Mock<UserProfileService>();
            var flagKey = "string_single_variable_feature";

            var userId = "testUser3";
            var variationKey = "control";

            var optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object, userProfileServiceMock.Object);
            var user = optimizely.CreateUserContext(userId);

            var decideOptions = new OptimizelyDecideOption[] { OptimizelyDecideOption.IGNORE_USER_PROFILE_SERVICE };
            var variationUserProfile = user.Decide(flagKey, decideOptions);
            userProfileServiceMock.Verify(l => l.Save(It.IsAny<Dictionary<string, object>>()), Times.Never);

            Assert.AreEqual(variationKey, variationUserProfile.VariationKey);
        }
        #endregion

        #region TrackEvent
        [Test]
        public void TestTrackEventWithAudienceConditions()
        {
            var OptimizelyWithTypedAudiences = new Optimizely(TestData.TypedAudienceDatafile, EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object);
            var userAttributes = new UserAttributes
            {
                { "house", "Gryffindor" },
                { "should_do_it", false }
            };

            var user = OptimizelyWithTypedAudiences.CreateUserContext(UserID, userAttributes);

            // Should be excluded as exact match boolean audience with id '3468206643' does not match so the overall conditions fail.
            user.TrackEvent("user_signed_up");

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Once);
        }

        [Test]
        public void TrackEventEmptyAttributesWithEventTags()
        {
            var OptimizelyWithTypedAudiences = new Optimizely(TestData.TypedAudienceDatafile, EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object);
            
            var user = OptimizelyWithTypedAudiences.CreateUserContext(UserID);

            // Should be excluded as exact match boolean audience with id '3468206643' does not match so the overall conditions fail.
            user.TrackEvent("user_signed_up", new EventTags
            {
                { "revenue", 42 },
                { "wont_send_null", null}
            });

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Once);
        }
        #endregion
    }
}
