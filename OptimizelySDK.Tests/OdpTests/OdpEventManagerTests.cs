/* 
 * Copyright 2022, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Moq;
using NUnit.Framework;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp;
using OptimizelySDK.Odp.Entity;
using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizelySDK.Tests.OdpTests
{
    [TestFixture]
    public class OdpEventManagerTests
    {
        private const string API_KEY = "N0tReAlAp1K3y";
        private const string API_HOST = "https://odp-events.example.com";
        private const string MOCK_IDEMPOTENCE_ID = "7d2fc936-8e3b-4e46-aff1-6ccc6fcd1394";
        private const string FS_USER_ID = "fs_user_id";
        private const int MAX_COUNT_DOWN_EVENT_WAIT_MS = 2000;

        private readonly List<OdpEvent> _testEvents = new List<OdpEvent>
        {
            new OdpEvent("t1", "a1", new Dictionary<string, string>
            {
                {
                    FS_USER_ID, "id-key-1"
                },
            }, new Dictionary<string, object>
            {
                {
                    "key-1", "value-1"
                },
                {
                    "key-2", null
                },
                {
                    "key-3", 3.3
                },
                {
                    "key-4", true
                },
            }),
            new OdpEvent("t2", "a2", new Dictionary<string, string>
            {
                {
                    FS_USER_ID, "id-key-2"
                },
            }, new Dictionary<string, object>
            {
                {
                    "key-2", "value-2"
                },
                {
                    "data_source", "my-source"
                },
            }),
        };

        private readonly List<OdpEvent> _processedEvents = new List<OdpEvent>
        {
            new OdpEvent("t1", "a1", new Dictionary<string, string>
            {
                {
                    FS_USER_ID, "id-key-1"
                },
            }, new Dictionary<string, object>
            {
                {
                    "idempotence_id", MOCK_IDEMPOTENCE_ID
                },
                {
                    "data_source_type", "sdk"
                },
                {
                    "data_source", Optimizely.SDK_TYPE
                },
                {
                    "data_source_version", Optimizely.SDK_VERSION
                },
                {
                    "key-1", "value-1"
                },
                {
                    "key-2", null
                },
                {
                    "key-3", 3.3
                },
                {
                    "key-4", true
                },
            }),
            new OdpEvent("t2", "a2", new Dictionary<string, string>
            {
                {
                    FS_USER_ID, "id-key-2"
                },
            }, new Dictionary<string, object>
            {
                {
                    "idempotence_id", MOCK_IDEMPOTENCE_ID
                },
                {
                    "data_source_type", "sdk"
                },
                {
                    "data_source", Optimizely.SDK_TYPE
                },
                {
                    "data_source_version", Optimizely.SDK_VERSION
                },
                {
                    "key-2", "value-2"
                },
            }),
        };

        private readonly List<string> _emptySegmentsToCheck = new List<string>(0);

        private OdpConfig _odpConfig;

        private Mock<ILogger> _mockLogger;
        private Mock<IOdpEventApiManager> _mockApiManager;

        [SetUp]
        public void Setup()
        {
            _odpConfig = new OdpConfig(API_KEY, API_HOST, _emptySegmentsToCheck);

            _mockApiManager = new Mock<IOdpEventApiManager>();

            _mockLogger = new Mock<ILogger>();
            _mockLogger.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
        }

        [Test]
        public void ShouldLogAndDiscardEventsWhenEventManagerNotRunning()
        {
            var eventManager = new OdpEventManager.Builder().WithOdpConfig(_odpConfig).
                WithOdpEventApiManager(_mockApiManager.Object).
                WithLogger(_mockLogger.Object).
                Build(startImmediately: false);

            // since we've not called start() then...
            eventManager.SendEvent(_testEvents[0]);

            // ...we should get a notice after trying to send an event
            _mockLogger.Verify(
                l => l.Log(LogLevel.WARN, Constants.ODP_NOT_ENABLED_MESSAGE),
                Times.Once);
        }

        [Test]
        public void ShouldLogAndDiscardEventsWhenEventManagerConfigNotReady()
        {
            var mockOdpConfig = new Mock<OdpConfig>(API_KEY, API_HOST, _emptySegmentsToCheck);
            mockOdpConfig.Setup(o => o.IsReady()).Returns(false);
            var eventManager = new OdpEventManager.Builder().WithOdpConfig(mockOdpConfig.Object).
                WithOdpEventApiManager(_mockApiManager.Object).
                WithLogger(_mockLogger.Object).
                Build(startImmediately: false); // doing it manually in Act next

            eventManager.Start(); // Log when Start() called
            eventManager.SendEvent(_testEvents[0]); // Log when enqueue attempted

            _mockLogger.Verify(
                l => l.Log(LogLevel.WARN, Constants.ODP_NOT_INTEGRATED_MESSAGE),
                Times.Exactly(2));
        }

        [Test]
        public void ShouldLogWhenOdpNotIntegratedAndIdentifyUserCalled()
        {
            var mockOdpConfig = new Mock<OdpConfig>(API_KEY, API_HOST, _emptySegmentsToCheck);
            mockOdpConfig.Setup(o => o.IsReady()).Returns(false);
            var eventManager = new OdpEventManager.Builder().WithOdpConfig(mockOdpConfig.Object).
                WithOdpEventApiManager(_mockApiManager.Object).
                WithLogger(_mockLogger.Object).
                Build();

            eventManager.IdentifyUser(FS_USER_ID);

            _mockLogger.Verify(
                l => l.Log(LogLevel.WARN, Constants.ODP_NOT_INTEGRATED_MESSAGE),
                Times.Exactly(2)); // during Start() and SendEvent()
        }

        [Test]
        public void ShouldLogWhenOdpNotIntegratedAndStartCalled()
        {
            var mockOdpConfig = new Mock<OdpConfig>(API_KEY, API_HOST, _emptySegmentsToCheck);
            mockOdpConfig.Setup(o => o.IsReady()).Returns(false);
            var eventManager = new OdpEventManager.Builder().WithOdpConfig(mockOdpConfig.Object).
                WithOdpEventApiManager(_mockApiManager.Object).
                WithLogger(_mockLogger.Object).
                Build(startImmediately: false); // doing it manually in Act next

            eventManager.Start();

            _mockLogger.Verify(l => l.Log(LogLevel.WARN, Constants.ODP_NOT_INTEGRATED_MESSAGE),
                Times.Once);
        }

        [Test]
        public void ShouldDiscardEventsWithInvalidData()
        {
            var eventWithAnArray = new OdpEvent("t3", "a3",
                new Dictionary<string, string>
                {
                    {
                        FS_USER_ID, "id-key-3"
                    },
                }, new Dictionary<string, object>
                {
                    {
                        "key-3", new[]
                        {
                            "array", "which", "isn't", "supported", "for", "event", "data",
                        }
                    },
                });
            var eventWithADate = new OdpEvent("t3", "a3",
                new Dictionary<string, string>
                {
                    {
                        FS_USER_ID, "id-key-3"
                    },
                }, new Dictionary<string, object>
                {
                    {
                        "key-3", new DateTime()
                    },
                });
            var eventManager = new OdpEventManager.Builder().WithOdpConfig(_odpConfig).
                WithOdpEventApiManager(_mockApiManager.Object).
                WithLogger(_mockLogger.Object).
                Build();

            eventManager.SendEvent(eventWithAnArray);
            eventManager.SendEvent(eventWithADate);

            _mockLogger.Verify(l => l.Log(LogLevel.ERROR, Constants.ODP_INVALID_DATA_MESSAGE),
                Times.Exactly(2));
        }

        [Test]
        public void ShouldAddAdditionalInformationToEachEvent()
        {
            var expectedEvent = _processedEvents[0];
            var cde = new CountdownEvent(1);
            var eventsCollector = new List<List<OdpEvent>>();
            _mockApiManager.Setup(api => api.SendEvents(It.IsAny<string>(), It.IsAny<string>(),
                    Capture.In(eventsCollector))).
                Callback(() => cde.Signal());
            var eventManager = new OdpEventManager.Builder().WithOdpConfig(_odpConfig).
                WithOdpEventApiManager(_mockApiManager.Object).
                WithLogger(_mockLogger.Object).
                WithEventQueue(new BlockingCollection<object>(10)). // max capacity of 10
                WithBatchSize(10).
                WithFlushInterval(TimeSpan.FromMilliseconds(100)).
                Build();

            eventManager.SendEvent(_testEvents[0]);
            cde.Wait();

            var eventsSentToApi = eventsCollector.FirstOrDefault();
            var actualEvent = eventsSentToApi?.FirstOrDefault();
            Assert.IsNotNull(actualEvent);
            Assert.AreEqual(expectedEvent.Type, actualEvent.Type);
            Assert.AreEqual(expectedEvent.Action, actualEvent.Action);
            Assert.AreEqual(expectedEvent.Identifiers, actualEvent.Identifiers);
            Assert.AreEqual(expectedEvent.Data["key-1"], actualEvent.Data["key-1"]);
            Assert.AreEqual(expectedEvent.Data["key-2"], actualEvent.Data["key-2"]);
            Assert.AreEqual(expectedEvent.Data["key-3"], actualEvent.Data["key-3"]);
            Assert.AreEqual(expectedEvent.Data["key-4"], actualEvent.Data["key-4"]);
            Assert.AreEqual(expectedEvent.Data["idempotence_id"].ToString().Length,
                actualEvent.Data["idempotence_id"].ToString().Length);
            Assert.AreEqual(expectedEvent.Data["data_source"], actualEvent.Data["data_source"]);
            Assert.AreEqual(expectedEvent.Data["data_source_type"],
                actualEvent.Data["data_source_type"]);
            Assert.AreEqual(expectedEvent.Data["data_source_version"],
                actualEvent.Data["data_source_version"]);
        }

        [Test]
        public void ShouldAttemptToFlushAnEmptyQueueAtFlushInterval()
        {
            var eventManager = new OdpEventManager.Builder().WithOdpConfig(_odpConfig).
                WithOdpEventApiManager(_mockApiManager.Object).
                WithLogger(_mockLogger.Object).
                WithEventQueue(new BlockingCollection<object>(10)).
                WithBatchSize(10).
                WithFlushInterval(TimeSpan.FromMilliseconds(100)).
                Build();

            // do not add events to the queue, but allow for
            // at least 3 flush intervals executions
            Task.Delay(500).Wait();
            eventManager.Stop();

            _mockLogger.Verify(l => l.Log(LogLevel.DEBUG, "Flushing queue."),
                Times.AtLeast(3));
        }

        [Test]
        public void ShouldDispatchEventsInCorrectNumberOfBatches()
        {
            _mockApiManager.Setup(a =>
                    a.SendEvents(It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<List<OdpEvent>>())).
                Returns(false);
            var eventManager = new OdpEventManager.Builder().WithOdpConfig(_odpConfig).
                WithOdpEventApiManager(_mockApiManager.Object).
                WithLogger(_mockLogger.Object).
                WithEventQueue(new BlockingCollection<object>(10)).
                WithBatchSize(10).
                WithFlushInterval(TimeSpan.FromMilliseconds(500)).
                Build();

            for (int i = 0; i < 25; i++)
            {
                eventManager.SendEvent(MakeEvent(i));
            }

            Task.Delay(1000).Wait();

            // Batch #1 & #2 with 10 in each should send immediately then...
            // Batch #3 of 5 should send after flush interval
            _mockApiManager.Verify(a =>
                a.SendEvents(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<List<OdpEvent>>()), Times.Exactly(3));
        }

        [Test]
        public void ShouldDispatchEventsWithCorrectPayload()
        {
            var cde = new CountdownEvent(1);
            var apiKeyCollector = new List<string>();
            var apiHostCollector = new List<string>();
            var eventCollector = new List<List<OdpEvent>>();
            _mockApiManager.Setup(a =>
                    a.SendEvents(Capture.In(apiKeyCollector), Capture.In(apiHostCollector),
                        Capture.In(eventCollector))).
                Callback(() => cde.Signal()).
                Returns(false);
            var eventManager = new OdpEventManager.Builder().WithOdpConfig(_odpConfig).
                WithOdpEventApiManager(_mockApiManager.Object).
                WithLogger(_mockLogger.Object).
                WithBatchSize(10).
                WithFlushInterval(TimeSpan.FromSeconds(1)).
                Build();

            _testEvents.ForEach(e => eventManager.SendEvent(e));
            cde.Wait(MAX_COUNT_DOWN_EVENT_WAIT_MS);

            // sending 1 batch of 2 events after 1s flush interval since batchSize is 10
            _mockApiManager.Verify(a =>
                a.SendEvents(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<List<OdpEvent>>()), Times.Once);
            Assert.AreEqual(API_KEY, apiKeyCollector[0]);
            Assert.AreEqual(API_HOST, apiHostCollector[0]);
            var collectedEventBatch = eventCollector[0];
            Assert.AreEqual(_testEvents.Count, collectedEventBatch.Count);
            Assert.AreEqual(collectedEventBatch[0].Identifiers, _processedEvents[0].Identifiers);
            Assert.AreEqual(collectedEventBatch[0].Data.Count, _processedEvents[0].Data.Count);
            Assert.AreEqual(collectedEventBatch[1].Identifiers, _processedEvents[1].Identifiers);
            Assert.AreEqual(collectedEventBatch[1].Data.Count, _processedEvents[1].Data.Count);
        }

        [Test]
        public void ShouldRetryFailedEvents()
        {
            var cde = new CountdownEvent(6);
            _mockApiManager.Setup(a =>
                    a.SendEvents(It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<List<OdpEvent>>())).
                Callback(() => cde.Signal()).
                Returns(true);
            var eventManager = new OdpEventManager.Builder().WithOdpConfig(_odpConfig).
                WithOdpEventApiManager(_mockApiManager.Object).
                WithLogger(_mockLogger.Object).
                WithEventQueue(new BlockingCollection<object>(10)).
                WithBatchSize(2).
                WithFlushInterval(TimeSpan.FromMilliseconds(100)).
                Build();

            for (int i = 0; i < 4; i++)
            {
                eventManager.SendEvent(MakeEvent(i));
            }

            cde.Wait(MAX_COUNT_DOWN_EVENT_WAIT_MS);

            // retry 3x (default) for 2 batches or 6 calls to attempt to process
            _mockApiManager.Verify(
                a => a.SendEvents(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<List<OdpEvent>>()), Times.Exactly(6));
        }

        [Test]
        public void ShouldFlushAllScheduledEventsBeforeStopping()
        {
            var eventManager = new OdpEventManager.Builder().WithOdpConfig(_odpConfig).
                WithOdpEventApiManager(_mockApiManager.Object).
                WithLogger(_mockLogger.Object).
                WithEventQueue(new BlockingCollection<object>(100)).
                WithBatchSize(2). // small batch size
                WithFlushInterval(TimeSpan.FromSeconds(2)). // long flush interval
                Build();

            for (int i = 0; i < 25; i++)
            {
                eventManager.SendEvent(MakeEvent(i));
            }

            // short wait here
            Task.Delay(100).Wait();
            // then stop to get the queue flushed in batches
            eventManager.Stop();

            _mockLogger.Verify(l => l.Log(LogLevel.INFO, "Received shutdown signal."), Times.Once);
            _mockLogger.Verify(
                l => l.Log(LogLevel.INFO,
                    "Exiting processing loop. Attempting to flush pending events."), Times.Once);
            _mockLogger.Verify(l => l.Log(LogLevel.WARN, "Stopping scheduler."), Times.Once);
        }

        [Test]
        public void ShouldPrepareCorrectPayloadForIdentifyUser()
        {
            const string USER_ID = "test_fs_user_id";
            var cde = new CountdownEvent(1);
            var eventsCollector = new List<List<OdpEvent>>();
            _mockApiManager.Setup(api => api.SendEvents(It.IsAny<string>(), It.IsAny<string>(),
                    Capture.In(eventsCollector))).
                Callback(() => cde.Signal());
            var eventManager = new OdpEventManager.Builder().WithOdpConfig(_odpConfig).
                WithOdpEventApiManager(_mockApiManager.Object).
                WithLogger(_mockLogger.Object).
                WithEventQueue(new BlockingCollection<object>(1)).
                WithBatchSize(1).
                Build();

            eventManager.IdentifyUser(USER_ID);
            cde.Wait(MAX_COUNT_DOWN_EVENT_WAIT_MS);

            var eventsSentToApi = eventsCollector.FirstOrDefault();
            var actualEvent = eventsSentToApi?.FirstOrDefault();
            Assert.IsNotNull(actualEvent);
            Assert.AreEqual(Constants.ODP_EVENT_TYPE, actualEvent.Type);
            Assert.AreEqual("identified", actualEvent.Action);
            Assert.AreEqual(USER_ID, actualEvent.Identifiers[OdpUserKeyType.FS_USER_ID.ToString()]);
            var eventData = actualEvent.Data;
            Assert.AreEqual(Guid.NewGuid().ToString().Length,
                eventData["idempotence_id"].ToString().Length);
            Assert.AreEqual("sdk", eventData["data_source_type"]);
            Assert.AreEqual("csharp-sdk", eventData["data_source"]);
            Assert.IsNotNull(eventData["data_source_version"]);
        }

        [Test]
        public void ShouldApplyUpdatedOdpConfigurationWhenAvailable()
        {
            var apiKeyCollector = new List<string>();
            var apiHostCollector = new List<string>();
            var segmentsToCheckCollector = new List<List<string>>();
            var apiKey = "testing-api-key";
            var apiHost = "https://some.other.example.com";
            var segmentsToCheck = new List<string>
            {
                "empty-cart",
                "1-item-cart",
            };
            var mockOdpConfig = new Mock<OdpConfig>(API_KEY, API_HOST, segmentsToCheck);
            mockOdpConfig.Setup(m => m.Update(Capture.In(apiKeyCollector),
                Capture.In(apiHostCollector), Capture.In(segmentsToCheckCollector)));
            var differentOdpConfig = new OdpConfig(apiKey, apiHost, segmentsToCheck);
            var eventManager = new OdpEventManager.Builder().WithOdpConfig(mockOdpConfig.Object).
                WithOdpEventApiManager(_mockApiManager.Object).
                WithLogger(_mockLogger.Object).
                Build();

            eventManager.UpdateSettings(differentOdpConfig);

            Assert.AreEqual(apiKey, apiKeyCollector[0]);
            Assert.AreEqual(apiHost, apiHostCollector[0]);
            Assert.AreEqual(segmentsToCheck, segmentsToCheckCollector[0]);
        }

        private static OdpEvent MakeEvent(int id) =>
            new OdpEvent($"test-type-{id}", $"test-action-{id}", new Dictionary<string, string>
            {
                {
                    "an-identifier", $"value1-{id}"
                },
                {
                    "another-identity", $"value2-{id}"
                },
            }, new Dictionary<string, object>
            {
                {
                    "data1", $"data1-value1-{id}"
                },
                {
                    "data2", id
                },
            });
    }
}
