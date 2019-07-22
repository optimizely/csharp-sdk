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
using System.Threading;

namespace OptimizelySDK.Tests.EventTests
{
    public class CountdownEventDispatcher : IEventDispatcher
    {
        public ILogger Logger { get; set; }

        private CountdownEvent CountdownEvent;

        public CountdownEventDispatcher(CountdownEvent countdownEvent)
        {
            CountdownEvent = countdownEvent;
        }

        public void DispatchEvent(LogEvent logEvent)
        {
            CountdownEvent.Signal();
        }
    }

    [TestFixture]
    class BatchEventProcessorTest
    {
        private static string TestUserId = "testUserId";
        private const string EventId = "eventId";
        private const string EventName = "eventName";

        private const int MAX_BATCH_SIZE = 10;
        private const int MAX_DURATION_MS = 1000;
        private const int TIMEOUT_INTERVAL_MS = 5000;

        private ProjectConfig Config;
        private Mock<ProjectConfig> ConfigMock;

        private Mock<ILogger> LoggerMock;
        private BlockingCollection<object> eventQueue;
        private BatchEventProcessor EventProcessor;
        private Mock<IEventDispatcher> EventDispatcherMock;

        private NotificationCenter NotificationCenter = new NotificationCenter();
        private Mock<TestNotificationCallbacks> NotificationCallbackMock;

        [SetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            Config = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, new ErrorHandler.NoOpErrorHandler());
            ConfigMock = new Mock<ProjectConfig>() { CallBase = true };

            ConfigMock.SetupGet(config => config.Revision);
            ConfigMock.SetupGet(config => config.ProjectId);

            eventQueue = new BlockingCollection<object>(100);
            EventDispatcherMock = new Mock<IEventDispatcher>();

            NotificationCallbackMock = new Mock<TestNotificationCallbacks>();
            NotificationCallbackMock.Setup(nc => nc.TestLogEventCallback(It.IsAny<LogEvent>()));

            NotificationCenter.AddNotification(NotificationCenter.NotificationType.LogEvent,
                NotificationCallbackMock.Object.TestLogEventCallback);
        }

        [TearDown]
        public void TearDown()
        {
            EventProcessor.Stop();
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
            var countdownEvent = new CountdownEvent(1);
            SetEventProcessor(new CountdownEventDispatcher(countdownEvent));

            UserEvent userEvent = BuildConversionEvent(EventName);
            EventProcessor.Process(userEvent);

            Assert.True(countdownEvent.Wait(TimeSpan.FromMilliseconds(MAX_DURATION_MS * 3)), "Exceeded timeout waiting for notification.");
            Assert.AreEqual(0, EventProcessor.EventQueue.Count);
        }

        [Test]
        public void TestFlushMaxBatchSize()
        {
            var countdownEvent = new CountdownEvent(1);
            SetEventProcessor(new CountdownEventDispatcher(countdownEvent));

            for (int i = 0; i < MAX_BATCH_SIZE; i++)
            {
                string eventName = EventName + i;
                UserEvent userEvent = BuildConversionEvent(eventName);
                EventProcessor.Process(userEvent);
            }

            EventProcessor.Stop();

            countdownEvent.Wait();
            Assert.AreEqual(0, EventProcessor.EventQueue.Count);
        }

        [Test]
        public void TestFlush()
        {
            var countdownEvent = new CountdownEvent(2);
            SetEventProcessor(new CountdownEventDispatcher(countdownEvent));

            UserEvent userEvent = BuildConversionEvent(EventName);
            EventProcessor.Process(userEvent);
            EventProcessor.Flush();

            EventProcessor.Process(userEvent);
            EventProcessor.Flush();

            Assert.True(countdownEvent.Wait(TimeSpan.FromMilliseconds(MAX_DURATION_MS / 2)), "Exceeded timeout waiting for notification.");
            Assert.AreEqual(0, EventProcessor.EventQueue.Count);
        }

        [Test]
        public void TestFlushOnMismatchRevision()
        {
            var countdownEvent = new CountdownEvent(2);
            SetEventProcessor(new CountdownEventDispatcher(countdownEvent));

            ConfigMock.SetupGet(config => config.Revision).Returns("1");
            ConfigMock.SetupGet(config => config.ProjectId).Returns("X");
            var userEvent1 = BuildConversionEvent(EventName, ConfigMock.Object);
            EventProcessor.Process(userEvent1);

            ConfigMock.SetupGet(config => config.Revision).Returns("2");
            ConfigMock.SetupGet(config => config.ProjectId).Returns("X");
            var userEvent2 = BuildConversionEvent(EventName, ConfigMock.Object);
            EventProcessor.Process(userEvent2);

            EventProcessor.Stop();
            Assert.True(countdownEvent.Wait(TimeSpan.FromMilliseconds(MAX_DURATION_MS * 3)), "Exceeded timeout waiting for notification.");
        }

        [Test]
        public void TestFlushOnMismatchProjectId()
        {
            var countdownEvent = new CountdownEvent(2);
            SetEventProcessor(new CountdownEventDispatcher(countdownEvent));

            ConfigMock.SetupGet(config => config.Revision).Returns("1");
            ConfigMock.SetupGet(config => config.ProjectId).Returns("X");
            var userEvent1 = BuildConversionEvent(EventName, ConfigMock.Object);
            EventProcessor.Process(userEvent1);

            ConfigMock.SetupGet(config => config.Revision).Returns("1");
            ConfigMock.SetupGet(config => config.ProjectId).Returns("Y");
            var userEvent2 = BuildConversionEvent(EventName, ConfigMock.Object);
            EventProcessor.Process(userEvent2);

            EventProcessor.Stop();
            Assert.True(countdownEvent.Wait(TimeSpan.FromMilliseconds(MAX_DURATION_MS * 3)), "Exceeded timeout waiting for notification.");
            Assert.AreEqual(0, EventProcessor.EventQueue.Count);
        }

        [Test]
        public void TestStopAndStart()
        {
            var countdownEvent = new CountdownEvent(2);
            SetEventProcessor(new CountdownEventDispatcher(countdownEvent));

            UserEvent userEvent = BuildConversionEvent(EventName);
            EventProcessor.Process(userEvent);
            EventProcessor.Stop();

            EventProcessor.Process(userEvent);

            EventProcessor.Start();
            EventProcessor.Stop();

            Assert.True(countdownEvent.Wait(TimeSpan.FromMilliseconds(MAX_DURATION_MS * 3)), "Exceeded timeout waiting for notification.");
        }

        [Test]
        public void TestNotificationCenter()
        {
            var countdownEvent = new CountdownEvent(1);
            NotificationCenter.AddNotification(NotificationCenter.NotificationType.LogEvent, logEvent => countdownEvent.Signal());
            SetEventProcessor(EventDispatcherMock.Object);

            UserEvent userEvent = BuildConversionEvent(EventName);
            EventProcessor.Process(userEvent);
            EventProcessor.Stop();
            
            Assert.True(countdownEvent.Wait(TimeSpan.FromMilliseconds(MAX_DURATION_MS * 3)), "Exceeded timeout waiting for notification.");
        }

        //[Test]
        //public void TestCloseTimeout()
        //{
        //    var countdownEvent = new CountdownEvent(1);
        //    NotificationCenter.AddNotification(NotificationCenter.NotificationType.LogEvent, logEvent => countdownEvent.Signal());
        //    SetEventProcessor(EventDispatcherMock.Object);

        //    UserEvent userEvent = BuildConversionEvent(EventName);
        //    EventProcessor.Process(userEvent);
        //    EventProcessor.Stop();

        //    Assert.True(countdownEvent.Wait(TimeSpan.FromMilliseconds(MAX_DURATION_MS * 3)), "Exceeded timeout waiting for notification.");
        //}

        private void SetEventProcessor(IEventDispatcher eventDispatcher)
        {
            EventProcessor = new BatchEventProcessor.Builder()
                .WithEventQueue(eventQueue)
                .WithEventDispatcher(eventDispatcher)
                .WithMaxBatchSize(MAX_BATCH_SIZE)
                .WithFlushInterval(TimeSpan.FromMilliseconds(MAX_DURATION_MS))
                .WithTimeoutInterval(TimeSpan.FromMilliseconds(TIMEOUT_INTERVAL_MS))
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
