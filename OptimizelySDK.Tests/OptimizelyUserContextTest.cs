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

    }
}
