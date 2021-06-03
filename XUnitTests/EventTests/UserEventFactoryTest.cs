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
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event;
using OptimizelySDK.Logger;
using Xunit;

namespace OptimizelySDK.XUnitTests.EventTests
{
    public class UserEventFactoryTest
    {
        private string TestUserId = "testUserId";
        private Mock<ILogger> LoggerMock;
        private Mock<IErrorHandler> ErrorHandlerMock;
        private ProjectConfig Config;

        public UserEventFactoryTest()
        {
            LoggerMock = new Mock<ILogger>();
            ErrorHandlerMock = new Mock<IErrorHandler>();

            Config = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, ErrorHandlerMock.Object);
        }

        [Fact]
        public void ImpressionEventTest()
        {
            var projectConfig = Config;
            var experiment = Config.GetExperimentFromKey("test_experiment");
            var variation = Config.GetVariationFromId(experiment.Key, "77210100090");
            var userId = TestUserId;

            var impressionEvent = UserEventFactory.CreateImpressionEvent(projectConfig, experiment, variation, userId, null);

            Assert.Equal(Config.ProjectId, impressionEvent.Context.ProjectId);
            Assert.Equal(Config.Revision, impressionEvent.Context.Revision);
            Assert.Equal(Config.AccountId, impressionEvent.Context.AccountId);
            Assert.Equal(Config.AnonymizeIP, impressionEvent.Context.AnonymizeIP);
            Assert.Equal(Config.BotFiltering, impressionEvent.BotFiltering);
            Assert.Equal(experiment, impressionEvent.Experiment);
            Assert.Equal(variation, impressionEvent.Variation);
            Assert.Equal(userId, impressionEvent.UserId);
            Assert.Equal(Config.BotFiltering, impressionEvent.BotFiltering);
        }

        [Fact]
        public void ImpressionEventTestWithAttributes()
        {
            var projectConfig = Config;
            var experiment = Config.GetExperimentFromKey("test_experiment");
            var variation = Config.GetVariationFromId(experiment.Key, "77210100090");
            var userId = TestUserId;

            var userAttributes = new UserAttributes {
                { "device_type", "iPhone" },
                { "company", "Optimizely" }
            };

            var impressionEvent = UserEventFactory.CreateImpressionEvent(projectConfig, experiment, variation, userId, userAttributes);

            Assert.Equal(Config.ProjectId, impressionEvent.Context.ProjectId);
            Assert.Equal(Config.Revision, impressionEvent.Context.Revision);
            Assert.Equal(Config.AccountId, impressionEvent.Context.AccountId);
            Assert.Equal(Config.AnonymizeIP, impressionEvent.Context.AnonymizeIP);
            Assert.Equal(Config.BotFiltering, impressionEvent.BotFiltering);
            Assert.Equal(experiment, impressionEvent.Experiment);
            Assert.Equal(variation, impressionEvent.Variation);
            Assert.Equal(userId, impressionEvent.UserId);
            Assert.Equal(Config.BotFiltering, impressionEvent.BotFiltering);

            var expectedVisitorAttributes = EventFactory.BuildAttributeList(userAttributes, projectConfig);
            TestData.CompareObjects(expectedVisitorAttributes, impressionEvent.VisitorAttributes);
        }

        [Fact]
        public void ConversionEventTest()
        {
            var projectConfig = Config;
            var experiment = Config.GetExperimentFromKey("test_experiment");
            var variation = Config.GetVariationFromId(experiment.Key, "77210100090");
            var userId = TestUserId;
            var eventKey = "purchase";
            var userAttributes = new UserAttributes {
                { "device_type", "iPhone" },
                { "company", "Optimizely" }
            };

            var conversionEvent = UserEventFactory.CreateConversionEvent(projectConfig, eventKey, userId, userAttributes, null);

            Assert.Equal(Config.ProjectId, conversionEvent.Context.ProjectId);
            Assert.Equal(Config.Revision, conversionEvent.Context.Revision);
            Assert.Equal(Config.AccountId, conversionEvent.Context.AccountId);
            Assert.Equal(Config.AnonymizeIP, conversionEvent.Context.AnonymizeIP);
            Assert.Equal(Config.BotFiltering, conversionEvent.BotFiltering);
            Assert.Equal(Config.GetEvent(eventKey), conversionEvent.Event);
            Assert.Equal(userId, conversionEvent.UserId);
            Assert.Equal(Config.BotFiltering, conversionEvent.BotFiltering);

            var expectedVisitorAttributes = EventFactory.BuildAttributeList(userAttributes, projectConfig);
            TestData.CompareObjects(expectedVisitorAttributes, conversionEvent.VisitorAttributes);
        }

        [Fact]
        public void ConversionEventWithEventTagsTest()
        {
            var projectConfig = Config;
            var experiment = Config.GetExperimentFromKey("test_experiment");
            var variation = Config.GetVariationFromId(experiment.Key, "77210100090");
            var userId = TestUserId;
            var eventKey = "purchase";

            var eventTags = new EventTags {
                { "revenue", 4200 },
                { "value", 1.234 },
                { "non-revenue", "abc" }
            };
            var userAttributes = new UserAttributes {
                { "device_type", "iPhone" },
                { "company", "Optimizely" }
            };

            var conversionEvent = UserEventFactory.CreateConversionEvent(projectConfig, eventKey, userId, userAttributes, eventTags);

            Assert.Equal(Config.ProjectId, conversionEvent.Context.ProjectId);
            Assert.Equal(Config.Revision, conversionEvent.Context.Revision);
            Assert.Equal(Config.AccountId, conversionEvent.Context.AccountId);
            Assert.Equal(Config.AnonymizeIP, conversionEvent.Context.AnonymizeIP);
            Assert.Equal(Config.BotFiltering, conversionEvent.BotFiltering);
            Assert.Equal(Config.GetEvent(eventKey), conversionEvent.Event);
            Assert.Equal(userId, conversionEvent.UserId);
            Assert.Equal(Config.BotFiltering, conversionEvent.BotFiltering);
            Assert.Equal(eventTags, conversionEvent.EventTags);

            var expectedVisitorAttributes = EventFactory.BuildAttributeList(userAttributes, projectConfig);
            TestData.CompareObjects(expectedVisitorAttributes, conversionEvent.VisitorAttributes);
        }
    }
}
