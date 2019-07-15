using Moq;
using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Event.Entity;
using OptimizelySDK.Logger;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace OptimizelySDK.Tests.EventTests
{
    [TestFixture]
    class BatchEventProcessorTest
    {
        private static string TestUserId = string.Empty;
        private const string EventId = "eventId";
        private const string EventName = "eventName";

        private const int MAX_BATCH_SIZE = 10;
        private TimeSpan MAX_DURATION_MS = TimeSpan.FromMilliseconds(1000);

        private ProjectConfig Config;
        private ILogger Logger;
        private BlockingCollection<object> eventQueue;
        private BatchEventProcessor eventProcessor;
        private Mock<IEventDispatcher> EventDispatcherMock;

        [TestFixtureSetUp]
        public void Setup()
        {
            TestUserId = "testUserId";
            var logger = new NoOpLogger();
            Config = DatafileProjectConfig.Create(TestData.Datafile, logger, new ErrorHandler.NoOpErrorHandler());
            eventQueue = new BlockingCollection<object>(100);
            EventDispatcherMock = new Mock<IEventDispatcher>();


        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            eventProcessor.Stop();
        }

        [Test]
        public void TestDrainOnClose()
        {
            UserEvent userEvent = BuildConversionEvent(EventName);
            SetEventProcessor(EventDispatcherMock.Object);
            eventProcessor.Process(userEvent);
            eventProcessor.Stop();

            Assert.AreEqual(0, eventQueue.Count);
        }

        [Test]
        public async void testFlushOnMaxTimeout()
        {
            SetEventProcessor(EventDispatcherMock.Object);

            UserEvent userEvent = BuildConversionEvent(EventName);
            eventProcessor.Process(userEvent);
            TimeSpan awaitTimeSpan = MAX_DURATION_MS;
            await Task.Delay(awaitTimeSpan.Add(TimeSpan.FromMilliseconds(2000)));

            Assert.AreEqual(0, eventQueue.Count);
            eventProcessor.Stop();
        }

        public void TestFlushMaxBatchSize()
        {
            SetEventProcessor(EventDispatcherMock.Object);
            for ( int i = 0; i < MAX_BATCH_SIZE; i++ ) {
                string eventName = EventName + i;
                UserEvent userEvent = BuildConversionEvent(eventName);
                eventProcessor.Process(userEvent);
            }
            Assert.AreEqual(0, eventQueue.Count);
        }

        private void SetEventProcessor(IEventDispatcher eventDispatcher)
        {
            eventProcessor = new BatchEventProcessor(eventQueue,
                EventDispatcherMock.Object,
                MAX_BATCH_SIZE,
                MAX_DURATION_MS,
                null,
                true,
                Logger
                );
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
