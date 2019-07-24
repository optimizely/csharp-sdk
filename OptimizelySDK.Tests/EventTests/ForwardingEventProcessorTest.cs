using Moq;
using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.Notifications;

namespace OptimizelySDK.Tests.EventTests
{
    [TestFixture]
    class ForwardingEventProcessorTest
    {
        private const string UserId = "userId";
        private const string EventName = "purchase";

        private ForwardingEventProcessor EventProcessor;
        private TestForwardingEventDispatcher EventDispatcher;
        private NotificationCenter NotificationCenter = new NotificationCenter();

        Mock<ILogger> LoggerMock;
        ProjectConfig ProjectConfig;

        [SetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            ProjectConfig = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, new NoOpErrorHandler());

            EventDispatcher = new TestForwardingEventDispatcher { IsUpdated = false };
            EventProcessor = new ForwardingEventProcessor(EventDispatcher, NotificationCenter, LoggerMock.Object);
        }

        [Test]
        public void TestEventHandler()
        {
            var userEvent = BuildConversionEvent(EventName);
            EventProcessor.Process(userEvent);
            Assert.True(EventDispatcher.IsUpdated);
        }

        [Test]
        public void TestNotifications()
        {
            bool notificationTriggered = false;
            NotificationCenter.AddNotification(NotificationCenter.NotificationType.LogEvent, logEvent => notificationTriggered = true);
            var userEvent = BuildConversionEvent(EventName);
            EventProcessor.Process(userEvent);

            Assert.True(notificationTriggered);
        }

        private ConversionEvent BuildConversionEvent(string eventName)
        {
            return UserEventFactory.CreateConversionEvent(ProjectConfig, eventName, UserId, new UserAttributes(), new EventTags());
        }
    }
}
