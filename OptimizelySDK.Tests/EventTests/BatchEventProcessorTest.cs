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
    [TestFixture]
    class BatchEventProcessorTest
    {
        private static string TestUserId = "testUserId";
        private const string EventName = "purchase";

        public const int MAX_BATCH_SIZE = 10;
        public const int MAX_DURATION_MS = 1000;
        public const int TIMEOUT_INTERVAL_MS = 5000;

        private ProjectConfig Config;
        private Mock<ILogger> LoggerMock;
        private BlockingCollection<object> eventQueue;
        private BatchEventProcessor EventProcessor;
        private Mock<IEventDispatcher> EventDispatcherMock;
        private TestEventDispatcher TestEventDispatcher;
        private NotificationCenter NotificationCenter = new NotificationCenter();
        private Mock<TestNotificationCallbacks> NotificationCallbackMock;

        [SetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            Config = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, new ErrorHandler.NoOpErrorHandler());
            
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
            var eventDispatcher = new TestEventDispatcher();
            SetEventProcessor(eventDispatcher);

            UserEvent userEvent = BuildConversionEvent(EventName);
            EventProcessor.Process(userEvent);
            eventDispatcher.ExpectConversion(EventName, TestUserId);

            Thread.Sleep(1500);
            
            Assert.True(eventDispatcher.CompareEvents());
            Assert.AreEqual(0, EventProcessor.EventQueue.Count);
        }

        [Test]
        public void TestFlushOnMaxTimeout()
        {
            var countdownEvent = new CountdownEvent(1);
            var eventDispatcher = new TestEventDispatcher(countdownEvent);
            SetEventProcessor(eventDispatcher);

            UserEvent userEvent = BuildConversionEvent(EventName);
            EventProcessor.Process(userEvent);
            eventDispatcher.ExpectConversion(EventName, TestUserId);

            Thread.Sleep(1500);

            Assert.True(eventDispatcher.CompareEvents());
            Assert.True(countdownEvent.Wait(TimeSpan.FromMilliseconds(MAX_DURATION_MS * 3)), "Exceeded timeout waiting for notification.");
            Assert.AreEqual(0, EventProcessor.EventQueue.Count);
        }

        [Test]
        public void TestFlushMaxBatchSize()
        {
            var countdownEvent = new CountdownEvent(1);
            var eventDispatcher = new TestEventDispatcher(countdownEvent) { Logger = LoggerMock.Object };
            SetEventProcessor(eventDispatcher);

            for (int i = 0; i < MAX_BATCH_SIZE; i++)
            {
                UserEvent userEvent = BuildConversionEvent(EventName);
                EventProcessor.Process(userEvent);
                eventDispatcher.ExpectConversion(EventName, TestUserId);
            }

            Thread.Sleep(1000);

            Assert.True(eventDispatcher.CompareEvents());
            countdownEvent.Wait();
            Assert.AreEqual(0, EventProcessor.EventQueue.Count);

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void TestFlush()
        {
            var countdownEvent = new CountdownEvent(2);
            var eventDispatcher = new TestEventDispatcher(countdownEvent);
            SetEventProcessor(eventDispatcher);

            UserEvent userEvent = BuildConversionEvent(EventName);
            EventProcessor.Process(userEvent);
            EventProcessor.Flush();
            eventDispatcher.ExpectConversion(EventName, TestUserId);

            EventProcessor.Process(userEvent);
            EventProcessor.Flush();
            eventDispatcher.ExpectConversion(EventName, TestUserId);

            Thread.Sleep(1500);

            Assert.True(eventDispatcher.CompareEvents());
            Assert.True(countdownEvent.Wait(TimeSpan.FromMilliseconds(MAX_DURATION_MS / 2)), "Exceeded timeout waiting for notification.");
            Assert.AreEqual(0, EventProcessor.EventQueue.Count);
        }

        [Test]
        public void TestFlushOnMismatchRevision()
        {
            var countdownEvent = new CountdownEvent(2);
            var eventDispatcher = new TestEventDispatcher(countdownEvent);
            SetEventProcessor(eventDispatcher);

            Config.Revision = "1";
            Config.ProjectId = "X";
            var userEvent1 = BuildConversionEvent(EventName, Config);
            EventProcessor.Process(userEvent1);
            eventDispatcher.ExpectConversion(EventName, TestUserId);

            Config.Revision = "2";
            Config.ProjectId = "X";
            var userEvent2 = BuildConversionEvent(EventName, Config);
            EventProcessor.Process(userEvent2);
            eventDispatcher.ExpectConversion(EventName, TestUserId);

            Thread.Sleep(1500);

            Assert.True(eventDispatcher.CompareEvents());
            Assert.True(countdownEvent.Wait(TimeSpan.FromMilliseconds(MAX_DURATION_MS * 3)), "Exceeded timeout waiting for notification.");
        }

        [Test]
        public void TestFlushOnMismatchProjectId()
        {
            var countdownEvent = new CountdownEvent(2);
            var eventDispatcher = new TestEventDispatcher(countdownEvent);
            SetEventProcessor(eventDispatcher);

            Config.Revision = "1";
            Config.ProjectId = "X";
            var userEvent1 = BuildConversionEvent(EventName, Config);
            EventProcessor.Process(userEvent1);
            eventDispatcher.ExpectConversion(EventName, TestUserId);

            Config.Revision = "1";
            Config.ProjectId = "Y";
            var userEvent2 = BuildConversionEvent(EventName, Config);
            EventProcessor.Process(userEvent2);
            eventDispatcher.ExpectConversion(EventName, TestUserId);

            Thread.Sleep(1500);

            Assert.True(eventDispatcher.CompareEvents());
            Assert.True(countdownEvent.Wait(TimeSpan.FromMilliseconds(MAX_DURATION_MS * 3)), "Exceeded timeout waiting for notification.");
            Assert.AreEqual(0, EventProcessor.EventQueue.Count);
        }

        [Test]
        public void TestStopAndStart()
        {
            var countdownEvent = new CountdownEvent(2);
            var eventDispatcher = new TestEventDispatcher(countdownEvent);
            SetEventProcessor(eventDispatcher);

            UserEvent userEvent = BuildConversionEvent(EventName);
            EventProcessor.Process(userEvent);
            eventDispatcher.ExpectConversion(EventName, TestUserId);
            Thread.Sleep(1500);
            Assert.True(eventDispatcher.CompareEvents());

            EventProcessor.Stop();

            EventProcessor.Process(userEvent);
            eventDispatcher.ExpectConversion(EventName, TestUserId);
            EventProcessor.Start();
            EventProcessor.Stop();

            Assert.True(countdownEvent.Wait(TimeSpan.FromMilliseconds(MAX_DURATION_MS * 3)), "Exceeded timeout waiting for notification.");
        }

        [Test]
        public void TestDispose()
        {
            var countdownEvent = new CountdownEvent(2);
            var eventDispatcher = new TestEventDispatcher(countdownEvent);
            SetEventProcessor(eventDispatcher);

            Assert.IsTrue(EventProcessor.IsStarted);

            UserEvent userEvent = BuildConversionEvent(EventName);
            EventProcessor.Process(userEvent);
            eventDispatcher.ExpectConversion(EventName, TestUserId);

            EventProcessor.Dispose();

            Assert.True(eventDispatcher.CompareEvents());

            // make sure, isStarted is false while dispose.
            Assert.False(EventProcessor.IsStarted);
        }

        [Test]
        public void TestDisposeDontRaiseException()
        {
            var countdownEvent = new CountdownEvent(2);
            var eventDispatcher = new TestEventDispatcher(countdownEvent);
            SetEventProcessor(eventDispatcher);

            Assert.IsTrue(EventProcessor.IsStarted);

            UserEvent userEvent = BuildConversionEvent(EventName);
            EventProcessor.Process(userEvent);
            eventDispatcher.ExpectConversion(EventName, TestUserId);

            EventProcessor.Dispose();

            Assert.True(eventDispatcher.CompareEvents());

            // make sure, isStarted is false while dispose.
            Assert.False(EventProcessor.IsStarted);

            // Need to make sure, after dispose, process shouldn't raise exception
            EventProcessor.Process(userEvent);

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

        [Test]
        public void TestCloseTimeout()
        {
            var countdownEvent = new CountdownEvent(1);
            var eventDispatcher = new CountdownEventDispatcher { CountdownEvent = countdownEvent };
            SetEventProcessor(EventDispatcherMock.Object);

            UserEvent userEvent = BuildConversionEvent(EventName);
            EventProcessor.Process(userEvent);
            EventProcessor.Stop();

            countdownEvent.Signal();
        }

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
            return UserEventFactory.CreateConversionEvent(projectConfig, eventName, TestUserId,
                new UserAttributes(), new EventTags());
        }
    }

    class CountdownEventDispatcher : IEventDispatcher
    {
        public ILogger Logger { get; set; }
        public CountdownEvent CountdownEvent { get; set; }
        public void DispatchEvent(LogEvent logEvent) => Assert.False(!CountdownEvent.Wait(TimeSpan.FromMilliseconds(BatchEventProcessorTest.TIMEOUT_INTERVAL_MS * 2)));
    }
}
