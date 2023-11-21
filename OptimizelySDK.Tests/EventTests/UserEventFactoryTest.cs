/*
 *
 *    Copyright 2019-2020, 2023 Optimizely and contributors
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        https://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */

using System;
using Moq;
using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Tests.EventTests
{
    public class UserEventFactoryTest
    {
        private string TestUserId = "testUserId";
        private Mock<ILogger> LoggerMock;
        private Mock<IErrorHandler> ErrorHandlerMock;
        private ProjectConfig Config;

        [SetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
            ErrorHandlerMock = new Mock<IErrorHandler>();

            Config = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object,
                ErrorHandlerMock.Object);
        }

        [Test]
        public void ImpressionEventTest()
        {
            var projectConfig = Config;
            var experiment = Config.GetExperimentFromKey("test_experiment");
            var variation = Config.GetVariationFromId(experiment.Key, "77210100090");
            var userId = TestUserId;

            var impressionEvent = UserEventFactory.CreateImpressionEvent(projectConfig, experiment,
                variation, userId, null, "test_experiment", "experiment");

            Assert.AreEqual(Config.ProjectId, impressionEvent.Context.ProjectId);
            Assert.AreEqual(Config.Revision, impressionEvent.Context.Revision);
            Assert.AreEqual(Config.AccountId, impressionEvent.Context.AccountId);
            Assert.AreEqual(Config.AnonymizeIP, impressionEvent.Context.AnonymizeIP);
            Assert.AreEqual(Config.BotFiltering, impressionEvent.BotFiltering);
            Assert.AreEqual(experiment, impressionEvent.Experiment);
            Assert.AreEqual(variation, impressionEvent.Variation);
            Assert.AreEqual(userId, impressionEvent.UserId);
            Assert.AreEqual(Config.BotFiltering, impressionEvent.BotFiltering);
        }

        [Test]
        public void ImpressionEventTestWithAttributes()
        {
            var projectConfig = Config;
            var experiment = Config.GetExperimentFromKey("test_experiment");
            var variation = Config.GetVariationFromId(experiment.Key, "77210100090");
            var userId = TestUserId;

            var userAttributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "company", "Optimizely" },
            };

            var impressionEvent = UserEventFactory.CreateImpressionEvent(projectConfig, experiment,
                variation, userId, userAttributes, "test_experiment", "experiment");

            Assert.AreEqual(Config.ProjectId, impressionEvent.Context.ProjectId);
            Assert.AreEqual(Config.Revision, impressionEvent.Context.Revision);
            Assert.AreEqual(Config.AccountId, impressionEvent.Context.AccountId);
            Assert.AreEqual(Config.AnonymizeIP, impressionEvent.Context.AnonymizeIP);
            Assert.AreEqual(Config.BotFiltering, impressionEvent.BotFiltering);
            Assert.AreEqual(experiment, impressionEvent.Experiment);
            Assert.AreEqual(variation, impressionEvent.Variation);
            Assert.AreEqual(userId, impressionEvent.UserId);
            Assert.AreEqual(Config.BotFiltering, impressionEvent.BotFiltering);

            var expectedVisitorAttributes =
                EventFactory.BuildAttributeList(userAttributes, projectConfig);
            TestData.CompareObjects(expectedVisitorAttributes, impressionEvent.VisitorAttributes);
        }

        [Test]
        public void EventFactoryBuildShouldLogInvalidUserAttributes()
        {
            var projectConfig = Config;

            var invalidUserAttributes = new UserAttributes
            {
                { "invalid_date_object", new DateTime() },
                { "invalid_array_use", new int[7] },
            };
            var expectedInvalidMessage =
                $"User attributes: invalid_date_object, invalid_array_use were invalid and omitted.";

            EventFactory.BuildAttributeList(invalidUserAttributes, projectConfig,
                LoggerMock.Object);

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, expectedInvalidMessage), Times.Once());
        }

        [Test]
        public void ConversionEventTest()
        {
            var projectConfig = Config;
            var experiment = Config.GetExperimentFromKey("test_experiment");
            var variation = Config.GetVariationFromId(experiment.Key, "77210100090");
            var userId = TestUserId;
            var eventKey = "purchase";
            var userAttributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "company", "Optimizely" },
            };

            var conversionEvent = UserEventFactory.CreateConversionEvent(projectConfig, eventKey,
                userId, userAttributes, null);

            Assert.AreEqual(Config.ProjectId, conversionEvent.Context.ProjectId);
            Assert.AreEqual(Config.Revision, conversionEvent.Context.Revision);
            Assert.AreEqual(Config.AccountId, conversionEvent.Context.AccountId);
            Assert.AreEqual(Config.AnonymizeIP, conversionEvent.Context.AnonymizeIP);
            Assert.AreEqual(Config.BotFiltering, conversionEvent.BotFiltering);
            Assert.AreEqual(Config.GetEvent(eventKey), conversionEvent.Event);
            Assert.AreEqual(userId, conversionEvent.UserId);
            Assert.AreEqual(Config.BotFiltering, conversionEvent.BotFiltering);

            var expectedVisitorAttributes =
                EventFactory.BuildAttributeList(userAttributes, projectConfig);
            TestData.CompareObjects(expectedVisitorAttributes, conversionEvent.VisitorAttributes);
        }

        [Test]
        public void ConversionEventWithEventTagsTest()
        {
            var projectConfig = Config;
            var experiment = Config.GetExperimentFromKey("test_experiment");
            var variation = Config.GetVariationFromId(experiment.Key, "77210100090");
            var userId = TestUserId;
            var eventKey = "purchase";

            var eventTags = new EventTags
            {
                { "revenue", 4200 },
                { "value", 1.234 },
                { "non-revenue", "abc" },
            };
            var userAttributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "company", "Optimizely" },
            };

            var conversionEvent = UserEventFactory.CreateConversionEvent(projectConfig, eventKey,
                userId, userAttributes, eventTags);

            Assert.AreEqual(Config.ProjectId, conversionEvent.Context.ProjectId);
            Assert.AreEqual(Config.Revision, conversionEvent.Context.Revision);
            Assert.AreEqual(Config.AccountId, conversionEvent.Context.AccountId);
            Assert.AreEqual(Config.AnonymizeIP, conversionEvent.Context.AnonymizeIP);
            Assert.AreEqual(Config.BotFiltering, conversionEvent.BotFiltering);
            Assert.AreEqual(Config.GetEvent(eventKey), conversionEvent.Event);
            Assert.AreEqual(userId, conversionEvent.UserId);
            Assert.AreEqual(Config.BotFiltering, conversionEvent.BotFiltering);
            Assert.AreEqual(eventTags, conversionEvent.EventTags);

            var expectedVisitorAttributes =
                EventFactory.BuildAttributeList(userAttributes, projectConfig);
            TestData.CompareObjects(expectedVisitorAttributes, conversionEvent.VisitorAttributes);
        }
    }
}
