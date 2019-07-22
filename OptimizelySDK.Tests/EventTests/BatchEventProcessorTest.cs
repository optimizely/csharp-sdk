using Moq;
using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Event.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.Notifications;
using OptimizelySDK.Tests.NotificationTests;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace OptimizelySDK.Tests.EventTests
{
    [TestFixture]
    class BatchEventProcessorTest
    {
        private static string TestUserId = "testUserId";
        private const string EventId = "eventId";
        private const string EventName = "eventName";

        private const int MAX_BATCH_SIZE = 10;
        private TimeSpan MAX_DURATION_MS = TimeSpan.FromMilliseconds(1000);
        private TimeSpan TIMEOUT_INTERVAL_MS = TimeSpan.FromMilliseconds(5000);

        private ProjectConfig Config;
        private Mock<ProjectConfig> ConfigMock;

        private Mock<ILogger> LoggerMock;
        private BlockingCollection<object> eventQueue;
        private BatchEventProcessor EventProcessor;
        private Mock<IEventDispatcher> EventDispatcherMock;

        private NotificationCenter NotificationCenter = new NotificationCenter();
        private Mock<TestNotificationCallbacks> NotificationCallbackMock;

        [TestFixtureSetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            Config = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, new ErrorHandler.NoOpErrorHandler());
            ConfigMock = new Mock<ProjectConfig>();

            ConfigMock.SetupGet(config => config.Revision).Returns("1");
            ConfigMock.SetupGet(config => config.ProjectId).Returns("X");

            eventQueue = new BlockingCollection<object>(100);
            EventDispatcherMock = new Mock<IEventDispatcher>();

            NotificationCallbackMock = new Mock<TestNotificationCallbacks>();
            NotificationCallbackMock.Setup(nc => nc.TestLogEventCallback(It.IsAny<LogEvent>()));

            NotificationCenter.AddNotification(NotificationCenter.NotificationType.LogEvent,
                NotificationCallbackMock.Object.TestLogEventCallback);
        }
        
        [Test]
        public void TestDrainOnClose()
        {
            SetEventProcessor(EventDispatcherMock.Object);

            UserEvent userEvent = BuildConversionEvent(EventName);
            EventProcessor.Process(userEvent);
            EventProcessor.Stop();

            Assert.AreEqual(0, EventProcessor.EventQueue.Count);
        }

        [Test]
        public void TestFlushOnMaxTimeout()
        {
            SetEventProcessor(EventDispatcherMock.Object);

            UserEvent userEvent = BuildConversionEvent(EventName);
            EventProcessor.Process(userEvent);
            TimeSpan awaitTimeSpan = MAX_DURATION_MS;
            Task.Delay(awaitTimeSpan.Add(TimeSpan.FromMilliseconds(2000))).Wait();

            Assert.AreEqual(0, eventQueue.Count);
            EventProcessor.Stop();
        }

        [Test]
        public void TestFlushMaxBatchSize()
        {
            SetEventProcessor(EventDispatcherMock.Object);
            for ( int i = 0; i < MAX_BATCH_SIZE; i++ ) {
                string eventName = EventName + i;
                UserEvent userEvent = BuildConversionEvent(eventName);
                EventProcessor.Process(userEvent);
            }
            Assert.AreEqual(0, eventQueue.Count);
        }

        private void SetEventProcessor(IEventDispatcher eventDispatcher)
        {
            EventProcessor = new BatchEventProcessor.Builder()
                .WithEventQueue(eventQueue)
                .WithEventDispatcher(eventDispatcher)
                .WithMaxBatchSize(MAX_BATCH_SIZE)
                .WithFlushInterval(MAX_DURATION_MS)
                .WithTimeoutInterval(TIMEOUT_INTERVAL_MS)
                .WithLogger(LoggerMock.Object)
                .WithNotificationCenter(NotificationCenter)
                .Build();
        }

        private ConversionEvent BuildConversionEvent(string eventName)
        {
            return BuildConversionEvent(eventName, Config);
        }

        private static ConversionEvent BuildConversionEvent(string eventName, ProjectConfig projectConfig)
        {
            return UserEventFactory.CreateConversionEvent(projectConfig, EventId, TestUserId,
                new UserAttributes(), new EventTags());
        }
    }
}
