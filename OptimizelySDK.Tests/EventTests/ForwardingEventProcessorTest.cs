using System;
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
        Mock<IErrorHandler> ErrorHandlerMock;

        ProjectConfig ProjectConfig;

        [SetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            ErrorHandlerMock = new Mock<IErrorHandler>();
            ErrorHandlerMock.Setup(e => e.HandleError(It.IsAny<Exception>()));

            ProjectConfig = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, new NoOpErrorHandler());

            EventDispatcher = new TestForwardingEventDispatcher { IsUpdated = false };
            EventProcessor = new ForwardingEventProcessor(EventDispatcher, NotificationCenter, LoggerMock.Object, ErrorHandlerMock.Object);
        }

        [Test]
        public void TestEventHandlerWithConversionEvent()
        {
            var userEvent = CreateConversionEvent(EventName);
            EventProcessor.Process(userEvent);

            LoggerMock.Verify(logger => logger.Log(LogLevel.DEBUG, "Dispatching conversion event."), Times.Once);

            Assert.True(EventDispatcher.IsUpdated);
        }


        [Test]
        public void TestExceptionWhileDispatching()
        {
            var eventProcessor = new ForwardingEventProcessor(new InvalidEventDispatcher(), NotificationCenter, LoggerMock.Object, ErrorHandlerMock.Object);
            var userEvent = CreateConversionEvent(EventName);

            eventProcessor.Process(userEvent);

            LoggerMock.Verify(logger => logger.Log(LogLevel.DEBUG, "Dispatching conversion event."), Times.Once);
            ErrorHandlerMock.Verify(errorHandler => errorHandler.HandleError(It.IsAny<Exception>()), Times.Once );            
        }

        [Test]
        public void TestNotifications()
        {
            bool notificationTriggered = false;
            NotificationCenter.AddNotification(NotificationCenter.NotificationType.LogEvent, logEvent => notificationTriggered = true);
            var userEvent = CreateConversionEvent(EventName);

            EventProcessor.Process(userEvent);

            Assert.True(notificationTriggered);
        }


        private ConversionEvent CreateConversionEvent(string eventName)
        {
            return UserEventFactory.CreateConversionEvent(ProjectConfig, eventName, UserId, new UserAttributes(), new EventTags());
        }
    }
}
