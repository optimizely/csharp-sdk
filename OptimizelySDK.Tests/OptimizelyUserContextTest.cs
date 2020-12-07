/**
 *
 *    Copyright 2020, Optimizely and contributors
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

using Moq;
using NUnit.Framework;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Logger;
using OptimizelySDK.OptimizelyDecisions;
using System;

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

        [SetUp]
        public void SetUp()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

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

            Assert.AreEqual(user.Optimizely, Optimizely);
            Assert.AreEqual(user.UserId, UserID);
            Assert.AreEqual(user.UserAttributes, attributes);
        }

        [Test]
        public void OptimizelyUserContextNoAttributes()
        {
            OptimizelyUserContext user = new OptimizelyUserContext(Optimizely, UserID, null, ErrorHandlerMock.Object, LoggerMock.Object);

            Assert.AreEqual(user.Optimizely, Optimizely);
            Assert.AreEqual(user.UserId, UserID);
            Assert.True(user.UserAttributes.Count == 0);
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

            Assert.AreEqual(user.Optimizely, Optimizely);
            Assert.AreEqual(user.UserId, UserID);
            var newAttributes = user.UserAttributes;
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

            Assert.AreEqual(user.Optimizely, Optimizely);
            Assert.AreEqual(user.UserId, UserID);
            var newAttributes = user.UserAttributes;
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

            var newAttributes = user.UserAttributes;
            Assert.AreEqual(newAttributes["k1"], "v1");
            Assert.AreEqual(newAttributes["house"], "v2");
        }

        [Test]
        public void SetAttributeNullValue()
        {
            var attributes = new UserAttributes() { { "k1", null } };
            OptimizelyUserContext user = new OptimizelyUserContext(Optimizely, UserID, attributes, ErrorHandlerMock.Object, LoggerMock.Object);

            var newAttributes = user.UserAttributes;
            Assert.AreEqual(newAttributes["k1"], null);

            user.SetAttribute("k1", true);
            newAttributes = user.UserAttributes;
            Assert.AreEqual(newAttributes["k1"], true);

            user.SetAttribute("k1", null);
            newAttributes = user.UserAttributes;
            Assert.AreEqual(newAttributes["k1"], null);
        }
        public void SetAttributeToOverrideAttribute()
        {
            OptimizelyUserContext user = new OptimizelyUserContext(Optimizely, UserID, null, ErrorHandlerMock.Object, LoggerMock.Object);


            Assert.AreEqual(user.Optimizely, Optimizely);
            Assert.AreEqual(user.UserId, UserID);

            user.SetAttribute("k1", "v1");
            Assert.AreEqual(user.UserAttributes["k1"], "v1");

            user.SetAttribute("k1", true);
            Assert.AreEqual(user.UserAttributes["k1"], true);
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
            Assert.AreEqual(decision.UserContext, user);
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
    }
}
