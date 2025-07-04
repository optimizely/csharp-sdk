﻿/*
 *    Copyright 2020-2021, 2022-2024 Optimizely and contributors
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *    https://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
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
using OptimizelySDK.Odp;
using OptimizelySDK.OptimizelyDecisions;
using OptimizelySDK.Tests.NotificationTests;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class OptimizelyUserContextTest
    {
        private const string UserID = "testUserID";
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

            Optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object,
                LoggerMock.Object, ErrorHandlerMock.Object);
        }

        private Mock<UserProfileService> MakeUserProfileServiceMock()
        {
            var projectConfig = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object,
                ErrorHandlerMock.Object);
            var experiment = projectConfig.Experiments[8];
            var variation = experiment.Variations[0];
            var decision = new Decision(variation.Id);
            var userProfile = new UserProfile(UserID, new Dictionary<string, Decision>
            {
                { experiment.Id, decision },
            });
            var userProfileServiceMock = new Mock<UserProfileService>();
            userProfileServiceMock.Setup(up => up.Lookup(UserID)).Returns(userProfile.ToMap());
            return userProfileServiceMock;
        }

        [Test]
        public void OptimizelyUserContextWithAttributes()
        {
            var attributes = new UserAttributes()
            {
                {
                    "house", "GRYFFINDOR"
                },
            };
            var user = new OptimizelyUserContext(Optimizely, UserID, attributes,
                ErrorHandlerMock.Object, LoggerMock.Object);

            Assert.AreEqual(user.GetOptimizely(), Optimizely);
            Assert.AreEqual(user.GetUserId(), UserID);
            Assert.AreEqual(user.GetAttributes(), attributes);
        }

        [Test]
        public void OptimizelyUserContextNoAttributes()
        {
            var user = new OptimizelyUserContext(Optimizely, UserID, null,
                ErrorHandlerMock.Object, LoggerMock.Object);

            Assert.AreEqual(user.GetOptimizely(), Optimizely);
            Assert.AreEqual(user.GetUserId(), UserID);
            Assert.True(user.GetAttributes().Count == 0);
        }

        [Test]
        public void SetAttribute()
        {
            var attributes = new UserAttributes()
            {
                {
                    "house", "GRYFFINDOR"
                },
            };
            var user = new OptimizelyUserContext(Optimizely, UserID, attributes,
                ErrorHandlerMock.Object, LoggerMock.Object);

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
            var user = new OptimizelyUserContext(Optimizely, UserID, null,
                ErrorHandlerMock.Object, LoggerMock.Object);

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
            var attributes = new UserAttributes()
            {
                {
                    "house", "GRYFFINDOR"
                },
            };
            var user = new OptimizelyUserContext(Optimizely, UserID, attributes,
                ErrorHandlerMock.Object, LoggerMock.Object);

            user.SetAttribute("k1", "v1");
            user.SetAttribute("house", "v2");

            var newAttributes = user.GetAttributes();
            Assert.AreEqual(newAttributes["k1"], "v1");
            Assert.AreEqual(newAttributes["house"], "v2");
        }

        [Test]
        public void SetAttributeNullValue()
        {
            var attributes = new UserAttributes()
            {
                {
                    "k1", null
                },
            };
            var user = new OptimizelyUserContext(Optimizely, UserID, attributes,
                ErrorHandlerMock.Object, LoggerMock.Object);

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
            var user = new OptimizelyUserContext(Optimizely, UserID, null,
                ErrorHandlerMock.Object, LoggerMock.Object);

            Assert.AreEqual(user.GetOptimizely(), Optimizely);
            Assert.AreEqual(user.GetUserId(), UserID);

            user.SetAttribute("k1", "v1");
            Assert.AreEqual(user.GetAttributes()["k1"], "v1");

            user.SetAttribute("k1", true);
            Assert.AreEqual(user.GetAttributes()["k1"], true);
        }

        #region Decide

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
        public void TestForcedDecisionReturnsCorrectFlagAndRuleKeys()
        {
            var user = Optimizely.CreateUserContext(UserID);
            var context = new OptimizelyDecisionContext("flag", null);
            Assert.AreEqual("flag", context.FlagKey);
            Assert.Null(context.RuleKey);

            context = new OptimizelyDecisionContext("flag", "ruleKey");

            Assert.AreEqual("flag", context.FlagKey);
            Assert.AreEqual("ruleKey", context.RuleKey);
        }

        [Test]
        public void TestSetForcedDecisionSetsValue()
        {
            var user = Optimizely.CreateUserContext(UserID);
            var context = new OptimizelyDecisionContext("flag", null);
            var decision = new OptimizelyForcedDecision("variationKey");
            var result = user.SetForcedDecision(context, decision);

            Assert.IsTrue(result);
        }

        [Test]
        public void TestGetForcedDecisionReturnsNullIfInvalidConfig()
        {
            var optly = new Optimizely(new FallbackProjectConfigManager(null));

            var user = optly.CreateUserContext(UserID);

            var context = new OptimizelyDecisionContext("flag", null);
            var decision = new OptimizelyForcedDecision("variationKey");
            var result = user.GetForcedDecision(context);

            Assert.IsNull(result);
        }

        [Test]
        public void TestGetForcedDecisionReturnsNullWithNullFlagKey()
        {
            var user = Optimizely.CreateUserContext(UserID);

            var result = user.GetForcedDecision(null);

            Assert.IsNull(result);
        }

        [Test]
        public void TestGetForcedDecisionsReturnsValueWithRuleKey()
        {
            var user = Optimizely.CreateUserContext(UserID);
            var context = new OptimizelyDecisionContext("flagKey", "ruleKey");
            var decision = new OptimizelyForcedDecision("variation");
            user.SetForcedDecision(context, decision);

            var result = user.GetForcedDecision(context);

            Assertions.AreEqual(decision, result);
        }

        [Test]
        public void TestGetForcedDecisionReturnsValueWithoutRuleKey()
        {
            var user = Optimizely.CreateUserContext(UserID);
            var context = new OptimizelyDecisionContext("flagKey", null);
            var decision = new OptimizelyForcedDecision("variationKey");
            user.SetForcedDecision(context, decision);

            var result = user.GetForcedDecision(context);

            Assertions.AreEqual(decision, result);
        }

        [Test]
        public void TestGetForcedDecisionReturnsValueWithOnlyFlagKey()
        {
            var user = Optimizely.CreateUserContext(UserID);
            var context = new OptimizelyDecisionContext("flagKey", null);
            var decision = new OptimizelyForcedDecision("variationKey");
            user.SetForcedDecision(context, decision);

            var result = user.GetForcedDecision(context);

            Assertions.AreEqual(decision, result);
        }

        [Test]
        public void TestGetForcedDecisionReturnsValueWithRuleKey()
        {
            var user = Optimizely.CreateUserContext(UserID);
            var context = new OptimizelyDecisionContext("flagKey", "ruleKey");
            var decision = new OptimizelyForcedDecision("variationKey");
            user.SetForcedDecision(context, decision);

            var result = user.GetForcedDecision(context);

            Assertions.AreEqual(decision, result);
        }

        [Test]
        public void TestRemoveForcedDecisionReturnsFalseForNullFlagKey()
        {
            var user = Optimizely.CreateUserContext(UserID);

            Assert.IsFalse(user.RemoveForcedDecision(null));
        }

        [Test]
        public void TestRemoveForcedDecisionRemovesDecision()
        {
            var user = Optimizely.CreateUserContext(UserID);
            var context = new OptimizelyDecisionContext("flagKey", "ruleKey");
            var decision = new OptimizelyForcedDecision("variationKey");
            var setResult = user.SetForcedDecision(context, decision);

            Assert.IsTrue(setResult);

            user.RemoveForcedDecision(context);

            var result = user.GetForcedDecision(context);

            Assert.AreEqual(null, result);
        }

        [Test]
        public void TestRemoveAllForcedDecisionsRemovesDecisions()
        {
            var user = Optimizely.CreateUserContext(UserID);

            var context1 = new OptimizelyDecisionContext("flagKey", "ruleKey1");
            var context2 = new OptimizelyDecisionContext("flagKey2", "ruleKey2");
            var context3 = new OptimizelyDecisionContext("flagKey3", "ruleKey3");

            var decision1 = new OptimizelyForcedDecision("variation");
            var decision2 = new OptimizelyForcedDecision("variation2");
            var decision3 = new OptimizelyForcedDecision("variation3");

            user.SetForcedDecision(context1, decision1);
            user.SetForcedDecision(context2, decision2);
            user.SetForcedDecision(context3, decision3);

            user.RemoveAllForcedDecisions();

            var result1 = user.GetForcedDecision(context1);
            Assert.AreEqual(null, result1);

            var result2 = user.GetForcedDecision(context2);
            Assert.AreEqual(null, result2);

            var result3 = user.GetForcedDecision(context3);
            Assert.AreEqual(null, result3);
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
            var optimizely = new Optimizely(TestData.UnsupportedVersionDatafile,
                EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object);

            var flagKey = "multi_variate_feature";
            var decisionExpected = OptimizelyDecision.NewErrorDecision(
                flagKey,
                new OptimizelyUserContext(optimizely, UserID, new UserAttributes(),
                    ErrorHandlerMock.Object, LoggerMock.Object),
                DecisionMessage.SDK_NOT_READY,
                ErrorHandlerMock.Object,
                LoggerMock.Object);
            var user = optimizely.CreateUserContext(UserID);
            var decision = user.Decide(flagKey);

            Assert.IsTrue(TestData.CompareObjects(decision, decisionExpected));
        }

        [Test]
        public void SeparateDecideShouldHaveSameNumberOfUpsSaveAndLookup()
        {
            var flag1 = "double_single_variable_feature";
            var flag2 = "integer_single_variable_feature";
            var userProfileServiceMock = MakeUserProfileServiceMock();
            var saveArgsCollector = new List<Dictionary<string, object>>();
            userProfileServiceMock.Setup(up => up.Save(Capture.In(saveArgsCollector)));
            var optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object,
                LoggerMock.Object, ErrorHandlerMock.Object, userProfileServiceMock.Object);
            var user = optimizely.CreateUserContext(UserID);
            var flag1UserProfile = new UserProfile(UserID, new Dictionary<string, Decision>
            {
                { "224", new Decision("280") },
                { "122238", new Decision("122240") },
            });
            var flag2UserProfile = new UserProfile(UserID, new Dictionary<string, Decision>
            {
                { "224", new Decision("280") },
                { "122241", new Decision("122242") },
            });

            user.Decide(flag1);
            user.Decide(flag2);

            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "We were unable to get a user profile map from the UserProfileService."),
                Times.Never);
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "The UserProfileService returned an invalid map."),
                Times.Never);
            userProfileServiceMock.Verify(l => l.Lookup(UserID), Times.Exactly(2));
            userProfileServiceMock.Verify(l => l.Save(It.IsAny<Dictionary<string, object>>()),
                Times.Exactly(2));
            Assert.AreEqual(saveArgsCollector[0], flag1UserProfile.ToMap());
            Assert.AreEqual(saveArgsCollector[1], flag2UserProfile.ToMap());
        }

        [Test]
        public void DecideWithUpsShouldOnlyLookupSaveOnce()
        {
            var flagKeyFromTestDataJson = "double_single_variable_feature";
            var userProfileServiceMock = MakeUserProfileServiceMock();
            var saveArgsCollector = new List<Dictionary<string, object>>();
            userProfileServiceMock.Setup(up => up.Save(Capture.In(saveArgsCollector)));
            var optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object,
                LoggerMock.Object, ErrorHandlerMock.Object, userProfileServiceMock.Object);
            var user = optimizely.CreateUserContext(UserID);
            var expectedUserProfile = new UserProfile(UserID, new Dictionary<string, Decision>
            {
                { "224", new Decision("280") },
                { "122238", new Decision("122240") },
            });

            user.Decide(flagKeyFromTestDataJson);

            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "We were unable to get a user profile map from the UserProfileService."),
                Times.Never);
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "The UserProfileService returned an invalid map."),
                Times.Never);
            userProfileServiceMock.Verify(l => l.Lookup(UserID), Times.Once);
            userProfileServiceMock.Verify(l => l.Save(It.IsAny<Dictionary<string, object>>()),
                Times.Once);
            Assert.AreEqual(saveArgsCollector.First(), expectedUserProfile.ToMap());
        }

        #endregion Decide

        #region DecideForKeys

        [Test]
        public void DecideForKeysWithUpsShouldOnlyLookupSaveOnceWithMultipleFlags()
        {
            var flagKeys = new[] { "double_single_variable_feature", "boolean_feature" };
            var userProfileServiceMock = MakeUserProfileServiceMock();
            var saveArgsCollector = new List<Dictionary<string, object>>();
            userProfileServiceMock.Setup(up => up.Save(Capture.In(saveArgsCollector)));
            var optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object,
                LoggerMock.Object, ErrorHandlerMock.Object, userProfileServiceMock.Object);
            var userContext = optimizely.CreateUserContext(UserID);
            var expectedUserProfile = new UserProfile(UserID, new Dictionary<string, Decision>
            {
                { "224", new Decision("280") },
                { "122238", new Decision("122240") },
                { "7723330021", new Decision(null) },
                { "7718750065", new Decision(null) },
            });

            userContext.DecideForKeys(flagKeys);

            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "We were unable to get a user profile map from the UserProfileService."),
                Times.Never);
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "The UserProfileService returned an invalid map."),
                Times.Never);
            userProfileServiceMock.Verify(l => l.Lookup(UserID), Times.Once);
            userProfileServiceMock.Verify(l => l.Save(It.IsAny<Dictionary<string, object>>()),
                Times.Once);
            Assert.AreEqual(saveArgsCollector.First(), expectedUserProfile.ToMap());
        }

        [Test]
        public void DecideForKeysWithOneFlag()
        {
            var flagKey = "multi_variate_feature";
            var flagKeys = new string[]
            {
                flagKey,
            };

            var variablesExpected = Optimizely.GetAllFeatureVariables(flagKey, UserID);

            var user = Optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");

            var decisions = user.DecideForKeys(flagKeys);

            Assert.True(decisions.Count == 1);
            var decision = decisions[flagKey];

            var expDecision = new OptimizelyDecision(
                "Gred",
                false,
                variablesExpected,
                "test_experiment_multivariate",
                flagKey,
                user,
                new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decision, expDecision));
        }

        #endregion DecideForKeys

        #region DecideAll

        [Test]
        public void DecideAllWithUpsShouldOnlyLookupSaveOnce()
        {
            var userProfileServiceMock = MakeUserProfileServiceMock();
            var saveArgsCollector = new List<Dictionary<string, object>>();
            userProfileServiceMock.Setup(up => up.Save(Capture.In(saveArgsCollector)));
            var optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object,
                LoggerMock.Object, ErrorHandlerMock.Object, userProfileServiceMock.Object);
            var user = optimizely.CreateUserContext(UserID);
            var expectedUserProfile = new UserProfile(UserID, new Dictionary<string, Decision>
            {
                { "224", new Decision("280") },
                { "122238", new Decision("122240") },
                { "122241", new Decision("122242") },
                { "122235", new Decision("122236") },
                { "7723330021", new Decision(null) },
                { "7718750065", new Decision(null) },
            });

            user.DecideAll();

            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "We were unable to get a user profile map from the UserProfileService."),
                Times.Never);
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "The UserProfileService returned an invalid map."),
                Times.Never);
            userProfileServiceMock.Verify(l => l.Lookup(UserID), Times.Once);
            userProfileServiceMock.Verify(l => l.Save(It.IsAny<Dictionary<string, object>>()),
                Times.Once);
            Assert.AreEqual(saveArgsCollector.First(), expectedUserProfile.ToMap());
        }

        [Test]
        public void DecideAllTwoFlag()
        {
            var flagKey1 = "multi_variate_feature";
            var flagKey2 = "string_single_variable_feature";
            var flagKeys = new string[]
            {
                flagKey1, flagKey2,
            };

            var variablesExpected1 = Optimizely.GetAllFeatureVariables(flagKey1, UserID);
            var variablesExpected2 = Optimizely.GetAllFeatureVariables(flagKey2, UserID);

            var user = Optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");
            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            Optimizely.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            var decisions = user.DecideForKeys(flagKeys);

            var userAttributes = new UserAttributes
            {
                {
                    "browser_type", "chrome"
                },
            };

            Assert.True(decisions.Count == 2);
            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FLAG, UserID,
                    userAttributes, It.IsAny<Dictionary<string, object>>()),
                Times.Exactly(2));
            var expDecision1 = new OptimizelyDecision(
                "Gred",
                false,
                variablesExpected1,
                "test_experiment_multivariate",
                flagKey1,
                user,
                new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey1], expDecision1));

            var expDecision2 = new OptimizelyDecision(
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
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            Optimizely.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            var decisions = user.DecideAll();

            var userAttributes = new UserAttributes
            {
                {
                    "browser_type", "chrome"
                },
            };

            Assert.True(decisions.Count == 10);
            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FLAG, UserID,
                    userAttributes, It.IsAny<Dictionary<string, object>>()),
                Times.Exactly(10));

            var expDecision1 = new OptimizelyDecision(
                null,
                false,
                variablesExpected1,
                null,
                flagKey1,
                user,
                new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey1], expDecision1));

            var expDecision2 = new OptimizelyDecision(
                "variation",
                false,
                variablesExpected2,
                "test_experiment_double_feature",
                flagKey2,
                user,
                new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey2], expDecision2));

            var expDecision3 = new OptimizelyDecision(
                "control",
                false,
                variablesExpected3,
                "test_experiment_integer_feature",
                flagKey3,
                user,
                new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey3], expDecision3));

            var expDecision4 = new OptimizelyDecision(
                "188881",
                false,
                variablesExpected4,
                "188880",
                flagKey4,
                user,
                new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey4], expDecision4));

            var expDecision5 = new OptimizelyDecision(
                "control",
                true,
                variablesExpected5,
                "test_experiment_with_feature_rollout",
                flagKey5,
                user,
                new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey5], expDecision5));

            var expDecision6 = new OptimizelyDecision(
                "Gred",
                false,
                variablesExpected6,
                "test_experiment_multivariate",
                flagKey6,
                user,
                new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey6], expDecision6));

            var expDecision7 = new OptimizelyDecision(
                null,
                false,
                variablesExpected7,
                null,
                flagKey7,
                user,
                new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey7], expDecision7));

            var expDecision8 = new OptimizelyDecision(
                null,
                false,
                variablesExpected8,
                null,
                flagKey8,
                user,
                new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey8], expDecision8));

            var expDecision9 = new OptimizelyDecision(
                null,
                false,
                variablesExpected9,
                null,
                flagKey9,
                user,
                new string[0]);
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey9], expDecision9));

            var expDecision10 = new OptimizelyDecision(
                null,
                false,
                variablesExpected10,
                null,
                flagKey10,
                user,
                new[] { "Variable value for key \"any_key\" is invalid or wrong type." });
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey10], expDecision10));
        }

        [Test]
        public void DecideAllEnabledFlagsOnlyDecideOptions()
        {
            var flagKey1 = "string_single_variable_feature";

            var variablesExpected1 = Optimizely.GetAllFeatureVariables(flagKey1, UserID);

            var decideOptions = new OptimizelyDecideOption[]
            {
                OptimizelyDecideOption.ENABLED_FLAGS_ONLY,
            };
            var user = Optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");

            var decisions = user.DecideAll(decideOptions);

            Assert.True(decisions.Count == 1);

            var expDecision1 = new OptimizelyDecision(
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
            var decideOptions = new OptimizelyDecideOption[]
            {
                OptimizelyDecideOption.ENABLED_FLAGS_ONLY,
            };

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

            var expDecision1 = new OptimizelyDecision(
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
            var decideOptions = new OptimizelyDecideOption[]
            {
                OptimizelyDecideOption.ENABLED_FLAGS_ONLY,
            };

            var optimizely = new Optimizely(TestData.Datafile,
                EventDispatcherMock.Object,
                LoggerMock.Object,
                ErrorHandlerMock.Object,
                defaultDecideOptions: decideOptions);

            var user = optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");
            decideOptions = new OptimizelyDecideOption[]
            {
                OptimizelyDecideOption.EXCLUDE_VARIABLES,
            };

            var decisions = user.DecideAll(decideOptions);

            Assert.True(decisions.Count == 1);
            var expectedOptlyJson = new Dictionary<string, object>();
            var expDecision1 = new OptimizelyDecision(
                "control",
                true,
                new OptimizelyJSON(expectedOptlyJson, ErrorHandlerMock.Object, LoggerMock.Object),
                "test_experiment_with_feature_rollout",
                flagKey1,
                user,
                new string[]
                    { });
            Assert.IsTrue(TestData.CompareObjects(decisions[flagKey1], expDecision1));
        }

        [Test]
        public void DecideExcludeVariablesDecideOptions()
        {
            var flagKey = "multi_variate_feature";
            var variablesExpected = new Dictionary<string, object>();
            var user = Optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");
            var decideOptions = new OptimizelyDecideOption[]
            {
                OptimizelyDecideOption.EXCLUDE_VARIABLES,
            };

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
            Assert.AreEqual(decision.Reasons[0],
                DecisionMessage.Reason(DecisionMessage.FLAG_KEY_INVALID, flagKey));

            var decideOptions = new OptimizelyDecideOption[]
            {
                OptimizelyDecideOption.INCLUDE_REASONS,
            };

            decision = user.Decide(flagKey, decideOptions);
            Assert.True(decision.Reasons.Length == 1);
            Assert.AreEqual(decision.Reasons[0],
                DecisionMessage.Reason(DecisionMessage.FLAG_KEY_INVALID, flagKey));

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
            Assert.AreEqual(
                "User [testUserID] is in variation [Gred] of experiment [test_experiment_multivariate].",
                decision.Reasons[1]);
            Assert.AreEqual(
                "The user \"testUserID\" is bucketed into experiment \"test_experiment_multivariate\" of feature \"multi_variate_feature\".",
                decision.Reasons[2]);
        }

        [Test]
        public void TestDoNotSendEventDecide()
        {
            var flagKey = "multi_variate_feature";
            var variablesExpected = Optimizely.GetAllFeatureVariables(flagKey, UserID);

            var optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object,
                LoggerMock.Object, ErrorHandlerMock.Object);
            var user = optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");

            var decideOptions = new OptimizelyDecideOption[]
            {
                OptimizelyDecideOption.DISABLE_DECISION_EVENT,
            };
            var decision = user.Decide(flagKey, decideOptions);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Never);

            decision = user.Decide(flagKey);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);

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
            var decideOptions = new OptimizelyDecideOption[]
            {
                OptimizelyDecideOption.DISABLE_DECISION_EVENT,
            };

            var optimizely = new Optimizely(TestData.Datafile,
                EventDispatcherMock.Object,
                LoggerMock.Object,
                ErrorHandlerMock.Object,
                defaultDecideOptions: decideOptions);

            var user = optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");

            var decision = user.Decide(flagKey);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Never);

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
            var reasons = new string[]
                { };
            var user = Optimizely.CreateUserContext(UserID);
            user.SetAttribute("browser_type", "chrome");

            var decisionInfo = new Dictionary<string, object>
            {
                {
                    "flagKey", flagKey
                },
                {
                    "enabled", enabled
                },
                {
                    "variables", variables.ToDictionary()
                },
                {
                    "variationKey", variationKey
                },
                {
                    "ruleKey", ruleKey
                },
                {
                    "reasons", reasons
                },
                {
                    "decisionEventDispatched", true
                },
                {
                    "experimentId", "122235"
                },
                {
                    "variationId", "122236"
                },
            };

            var userAttributes = new UserAttributes
            {
                {
                    "browser_type", "chrome"
                },
            };

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            Optimizely.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            user.Decide(flagKey);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(
                    DecisionNotificationTypes.FLAG, UserID, userAttributes,
                    It.Is<Dictionary<string, object>>(info =>
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
                {
                    experimentId, new Decision(fbVariationId)
                },
            });

            userProfileServiceMock.Setup(_ => _.Lookup(userId)).Returns(userProfile.ToMap());

            var optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object,
                LoggerMock.Object, ErrorHandlerMock.Object, userProfileServiceMock.Object);

            var user = optimizely.CreateUserContext(userId);

            var projectConfig = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object,
                ErrorHandlerMock.Object);

            var variationUserProfile = user.Decide(flagKey);
            Assert.AreEqual(fbVariationKey, variationUserProfile.VariationKey);

            var decideOptions = new OptimizelyDecideOption[]
            {
                OptimizelyDecideOption.IGNORE_USER_PROFILE_SERVICE,
            };
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

            var optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object,
                LoggerMock.Object, ErrorHandlerMock.Object, userProfileServiceMock.Object);
            var user = optimizely.CreateUserContext(userId);

            var decideOptions = new OptimizelyDecideOption[]
            {
                OptimizelyDecideOption.IGNORE_USER_PROFILE_SERVICE,
            };
            var variationUserProfile = user.Decide(flagKey, decideOptions);
            userProfileServiceMock.Verify(l => l.Save(It.IsAny<Dictionary<string, object>>()),
                Times.Never);

            Assert.AreEqual(variationKey, variationUserProfile.VariationKey);
        }

        #endregion decideAll

        #region TrackEvent

        [Test]
        public void TestTrackEventWithAudienceConditions()
        {
            var OptimizelyWithTypedAudiences = new Optimizely(TestData.TypedAudienceDatafile,
                EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object);
            var userAttributes = new UserAttributes
            {
                {
                    "house", "Gryffindor"
                },
                {
                    "should_do_it", false
                },
            };

            var user = OptimizelyWithTypedAudiences.CreateUserContext(UserID, userAttributes);

            // Should be excluded as exact match boolean audience with id '3468206643' does not match so the overall conditions fail.
            user.TrackEvent("user_signed_up");

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);
        }

        [Test]
        public void TrackEventEmptyAttributesWithEventTags()
        {
            var OptimizelyWithTypedAudiences = new Optimizely(TestData.TypedAudienceDatafile,
                EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object);

            var user = OptimizelyWithTypedAudiences.CreateUserContext(UserID);

            // Should be excluded as exact match boolean audience with id '3468206643' does not match so the overall conditions fail.
            user.TrackEvent("user_signed_up", new EventTags
            {
                {
                    "revenue", 42
                },
                {
                    "wont_send_null", null
                },
            });

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);
        }

        #endregion TrackEvent

        [Test]
        public void ShouldFetchQualifiedSegmentsAsyncThenCallCallback()
        {
            var odpManager = new OdpManager.Builder().Build();
            var callbackResult = false;
            var optimizely = new Optimizely(TestData.OdpIntegrationDatafile,
                EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object,
                odpManager: odpManager);
            var context = new OptimizelyUserContext(optimizely, UserID, null,
                ErrorHandlerMock.Object, LoggerMock.Object);

            var task = context.FetchQualifiedSegments(success =>
            {
                callbackResult = success;
            });
            task.Wait();
            context.Dispose();

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, Constants.ODP_NOT_ENABLED_MESSAGE),
                Times.Never);
            Assert.IsTrue(callbackResult);
        }

        [Test]
        public void ShouldFetchQualifiedSegmentsSynchronously()
        {
            var odpManager = new OdpManager.Builder().Build();
            var mockLogger = new Mock<ILogger>();
            mockLogger.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            var optimizely = new Optimizely(TestData.OdpIntegrationDatafile,
                EventDispatcherMock.Object, mockLogger.Object, ErrorHandlerMock.Object,
                odpManager: odpManager);
            var context = new OptimizelyUserContext(optimizely, UserID, null,
                ErrorHandlerMock.Object, mockLogger.Object);

            var success = context.FetchQualifiedSegments();
            context.Dispose();

            mockLogger.Verify(l => l.Log(LogLevel.ERROR, Constants.ODP_NOT_ENABLED_MESSAGE),
                Times.Never);
            Assert.IsTrue(success);
        }
    }
}
