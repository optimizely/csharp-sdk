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

        private OdpConfig _odpConfig;

        private Mock<ILogger> _mockLogger;
        private Mock<IOdpEventApiManager> _mockApiManager;

        [SetUp]
        public void Setup()
        {
            _odpConfig = new OdpConfig(API_KEY, API_HOST, new List<string>());

            _mockApiManager = new Mock<IOdpEventApiManager>();

            _mockLogger = new Mock<ILogger>();
            _mockLogger.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
        }

        [Test]
        public void ShouldLogAndDiscardEventsWhenEventManagerNotRunning()
        {
            var eventManager =
                new OdpEventManager(_odpConfig, _mockApiManager.Object, _mockLogger.Object);

            // since we've not called start() then...
            eventManager.SendEvent(_testEvents[0]);

            // ...we should get a notice after trying to send an event
            _mockLogger.Verify(
                l => l.Log(LogLevel.WARN,
                    "ODP is not enabled."), Times.Once);
        }

        [Test]
        public void ShouldLogAndDiscardEventsWhenEventManagerConfigNotReady()
        {
            var mockOdpConfig = new Mock<IOdpConfig>();
            mockOdpConfig.Setup(o => o.IsReady()).Returns(false);
            var eventManager =
                new OdpEventManager(mockOdpConfig.Object, _mockApiManager.Object,
                    _mockLogger.Object);

            eventManager.Start();
            eventManager.SendEvent(_testEvents[0]); // Warning on enqueue

            _mockLogger.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "ODP is not integrated."),
                Times.Once);
        }

        [Test]
        public void ShouldDiscardEventsWithInvalidData()
        {
            var eventManager =
                new OdpEventManager(_odpConfig, _mockApiManager.Object, _mockLogger.Object);
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

            eventManager.Start();
            eventManager.SendEvent(eventWithAnArray);
            eventManager.SendEvent(eventWithADate);

            _mockLogger.Verify(l => l.Log(LogLevel.ERROR, "ODP data is not valid."),
                Times.Exactly(2));
        }

        [Test]
        public void ShouldLogMaxQueueHitAndDiscard()
        {
            var eventManager =
                new OdpEventManager(_odpConfig, _mockApiManager.Object, _mockLogger.Object, 1);

            eventManager.Start();
            _testEvents.ForEach(e => eventManager.SendEvent(e));

            _mockLogger.Verify(
                l => l.Log(LogLevel.WARN,
                    "ODP event send failed (queueSize = 1)."),
                Times.Once);
        }

        [Test]
        public void ShouldAddAdditionalInformationToEachEvent()
        {
            var cde = new CountdownEvent(1);
            var eventsCollector = new List<List<OdpEvent>>();
            _mockApiManager.Setup(api => api.SendEvents(It.IsAny<string>(), It.IsAny<string>(),
                    Capture.In(eventsCollector)))
                .Callback(() => cde.Signal());
            var eventManager =
                new OdpEventManager(_odpConfig, _mockApiManager.Object, _mockLogger.Object, 10, 10,
                    100);
            var expectedEvent = _processedEvents[0];

            eventManager.Start();
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
            var eventManager =
                new OdpEventManager(_odpConfig, _mockApiManager.Object, _mockLogger.Object, 10, 10,
                    100);

            eventManager.Start();
            // do not add events to the queue, but allow for...
            Task.Delay(400).Wait(); // at least 3 flush intervals executions (giving a little longer)

            _mockLogger.Verify(l => l.Log(LogLevel.DEBUG, "Processing Queue (flush)"),
                Times.AtLeast(3));
        }

        [Test]
        public void ShouldDispatchEventsInCorrectNumberOfBatches()
        {
            _mockApiManager.Setup(a =>
                    a.SendEvents(It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<List<OdpEvent>>()))
                .Returns(false);
            var eventManager =
                new OdpEventManager(_odpConfig, _mockApiManager.Object, _mockLogger.Object, 10, 10,
                    500);

            eventManager.Start();
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
                        Capture.In(eventCollector)))
                .Callback(() => cde.Signal())
                .Returns(false);
            var eventManager =
                new OdpEventManager(_odpConfig, _mockApiManager.Object, _mockLogger.Object);

            eventManager.Start();
            _testEvents.ForEach(e => eventManager.SendEvent(e));
            cde.Wait();

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
                        It.IsAny<List<OdpEvent>>()))
                .Callback(() => cde.Signal())
                .Returns(true);
            var eventManager =
                new OdpEventManager(_odpConfig, _mockApiManager.Object, _mockLogger.Object, 10, 2,
                    100);

            eventManager.Start();
            for (int i = 0; i < 4; i++)
            {
                eventManager.SendEvent(MakeEvent(i));
            }

            cde.Wait();

            // retry 3x (default) for 2 batches or 6 calls to attempt to process
            _mockApiManager.Verify(
                a => a.SendEvents(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<List<OdpEvent>>()), Times.Exactly(6));
        }

        [Test]
        public void ShouldFlushAllScheduledEventsBeforeStopping()
        {
            var eventManager =
                new OdpEventManager(_odpConfig, _mockApiManager.Object, _mockLogger.Object, 100,
                    2, // small batch size
                    2000); // long flush interval

            eventManager.Start();
            for (int i = 0; i < 25; i++)
            {
                eventManager.SendEvent(MakeEvent(i));
            }

            // short wait here
            Task.Delay(100).Wait();
            // then stop to get the queue flushed in batches
            eventManager.Stop();

            _mockLogger.Verify(l => l.Log(LogLevel.DEBUG, "Stop requested."), Times.Once);
            _mockLogger.Verify(l => l.Log(LogLevel.DEBUG, "Stopped. Queue Count: 0."), Times.Once);
        }

        [Test]
        public void ShouldPrepareCorrectPayloadForIdentifyUser()
        {
            var cde = new CountdownEvent(1);
            var eventsCollector = new List<List<OdpEvent>>();
            _mockApiManager.Setup(api => api.SendEvents(It.IsAny<string>(), It.IsAny<string>(),
                    Capture.In(eventsCollector)))
                .Callback(() => cde.Signal());
            var eventManager =
                new OdpEventManager(_odpConfig, _mockApiManager.Object, _mockLogger.Object, 1, 1);
            const string USER_ID = "test_fs_user_id";

            eventManager.Start();
            eventManager.IdentifyUser(USER_ID);
            cde.Wait();

            var eventsSentToApi = eventsCollector.FirstOrDefault();
            var actualEvent = eventsSentToApi?.FirstOrDefault();
            Assert.IsNotNull(actualEvent);
            Assert.AreEqual(OdpEventManager.TYPE, actualEvent.Type);
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
            var apiKey = "testing-api-key";
            var apiKeyCollector = new List<string>();
            var apiHost = "https://some.other.example.com";
            var apiHostCollector = new List<string>();
            var segmentsToCheck = new List<string>
            {
                "empty-cart",
                "1-item-cart",
            };
            var segmentsToCheckCollector = new List<List<string>>();
            var mockOdpConfig = new Mock<IOdpConfig>();
            mockOdpConfig.Setup(m => m.Update(Capture.In(apiKeyCollector),
                Capture.In(apiHostCollector), Capture.In(segmentsToCheckCollector)));
            var differentOdpConfig = new OdpConfig(apiKey, apiHost, segmentsToCheck);
            var eventManager =
                new OdpEventManager(mockOdpConfig.Object, _mockApiManager.Object,
                    _mockLogger.Object);

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
