using System;
using Moq;
using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event.internals;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Tests.EventTests.InternalsTest
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

            Config = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, ErrorHandlerMock.Object);
        }

        [Test]
        public void ImpressionEventTest()
        {
            var projectConfig = Config;
            var experiment = Config.GetExperimentFromKey("test_experiment");
            var variation = Config.GetVariationFromId(experiment.Key, "77210100090");
            var userId = TestUserId;

            var impressionEvent = UserEventFactory.CreateImpressionEvent(projectConfig, experiment, variation, userId, null);

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

            var userAttributes = new UserAttributes {
                { "device_type", "iPhone" },
                { "company", "Optimizely" }
            };

            var impressionEvent = UserEventFactory.CreateImpressionEvent(projectConfig, experiment, variation, userId, userAttributes);

            Assert.AreEqual(Config.ProjectId, impressionEvent.Context.ProjectId);
            Assert.AreEqual(Config.Revision, impressionEvent.Context.Revision);
            Assert.AreEqual(Config.AccountId, impressionEvent.Context.AccountId);
            Assert.AreEqual(Config.AnonymizeIP, impressionEvent.Context.AnonymizeIP);
            Assert.AreEqual(Config.BotFiltering, impressionEvent.BotFiltering);
            Assert.AreEqual(experiment, impressionEvent.Experiment);
            Assert.AreEqual(variation, impressionEvent.Variation);
            Assert.AreEqual(userId, impressionEvent.UserId);
            Assert.AreEqual(Config.BotFiltering, impressionEvent.BotFiltering);

            var expectedVisitorAttributes = EventFactory.BuildAttributeList(userAttributes, projectConfig);
            TestData.CompareObjects(expectedVisitorAttributes, impressionEvent.VisitorAttributes);
        }

        [Test]
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

            Assert.AreEqual(Config.ProjectId, conversionEvent.Context.ProjectId);
            Assert.AreEqual(Config.Revision, conversionEvent.Context.Revision);
            Assert.AreEqual(Config.AccountId, conversionEvent.Context.AccountId);
            Assert.AreEqual(Config.AnonymizeIP, conversionEvent.Context.AnonymizeIP);
            Assert.AreEqual(Config.BotFiltering, conversionEvent.BotFiltering);
            Assert.AreEqual(Config.GetEvent(eventKey), conversionEvent.Event);
            Assert.AreEqual(userId, conversionEvent.UserId);
            Assert.AreEqual(Config.BotFiltering, conversionEvent.BotFiltering);

            var expectedVisitorAttributes = EventFactory.BuildAttributeList(userAttributes, projectConfig);
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

            Assert.AreEqual(Config.ProjectId, conversionEvent.Context.ProjectId);
            Assert.AreEqual(Config.Revision, conversionEvent.Context.Revision);
            Assert.AreEqual(Config.AccountId, conversionEvent.Context.AccountId);
            Assert.AreEqual(Config.AnonymizeIP, conversionEvent.Context.AnonymizeIP);
            Assert.AreEqual(Config.BotFiltering, conversionEvent.BotFiltering);
            Assert.AreEqual(Config.GetEvent(eventKey), conversionEvent.Event);
            Assert.AreEqual(userId, conversionEvent.UserId);
            Assert.AreEqual(Config.BotFiltering, conversionEvent.BotFiltering);
            Assert.AreEqual(eventTags, conversionEvent.EventTags);

            var expectedVisitorAttributes = EventFactory.BuildAttributeList(userAttributes, projectConfig);
            TestData.CompareObjects(expectedVisitorAttributes, conversionEvent.VisitorAttributes);
        }
    }
}
