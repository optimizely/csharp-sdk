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
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp;
using OptimizelySDK.Odp.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

        private IErrorHandler _errorHandler;
        private OdpConfig _odpConfig;

        private Mock<ILogger> _mockLogger;
        private Mock<IRestApiManager> _mockApiManager;

        [SetUp]
        public void Setup()
        {
            _odpConfig = new OdpConfig(API_KEY, API_HOST, new List<string>());
            _errorHandler = new Mock<IErrorHandler>().Object;

            _mockLogger = new Mock<ILogger>();
            _mockLogger.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            _mockApiManager = new Mock<IRestApiManager>();
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
                    "Failed to Process ODP Event. ODPEventManager is not running."), Times.Once);
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
            eventManager.SendEvent(_testEvents[0]);

            _mockLogger.Verify(
                l => l.Log(LogLevel.DEBUG, "Unable to Process ODP Event. ODPConfig is not ready."),
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

            eventManager.SendEvent(eventWithAnArray);
            eventManager.SendEvent(eventWithADate);

            _mockLogger.Verify(l => l.Log(LogLevel.ERROR, "Event data found to be invalid."),
                Times.Exactly(2));
        }

        [Test]
        public void ShouldLogMaxQueueHitAndDiscard()
        {
            var eventManager =
                new OdpEventManager(_odpConfig, _mockApiManager.Object, _mockLogger.Object, 1);

            eventManager.Start();
            eventManager.SendEvent(_testEvents[0]);
            eventManager.SendEvent(_testEvents[1]);

            _mockLogger.Verify(
                l => l.Log(LogLevel.WARN,
                    "Failed to Process ODP Event. Event Queue full. queueSize = 1."),
                Times.Once);
        }

        [Test]
        public void ShouldAddAdditionalInformationToEachEvent()
        {
            var eventsCollector = new List<List<OdpEvent>>();
            _mockApiManager.Setup(api => api.SendEvents(It.IsAny<string>(), It.IsAny<string>(),
                Capture.In(eventsCollector)));
            var eventManager =
                new OdpEventManager(_odpConfig, _mockApiManager.Object, _mockLogger.Object);
            var expectedEvent = _processedEvents[0];
            
            eventManager.Start();
            eventManager.SendEvent(_testEvents[0]);
            Thread.Sleep(2000);
            var eventsSentToApi = eventsCollector[0];
            var actualEvent = eventsSentToApi[0];

            Assert.AreEqual(expectedEvent.Type, actualEvent.Type);
            Assert.AreEqual(expectedEvent.Action, actualEvent.Action);
            Assert.AreEqual(expectedEvent.Identifiers, actualEvent.Identifiers);
            Assert.AreEqual(expectedEvent.Data["key-1"], actualEvent.Data["key-1"]);
            Assert.AreEqual(expectedEvent.Data["key-2"], actualEvent.Data["key-2"]);
            Assert.AreEqual(expectedEvent.Data["key-3"], actualEvent.Data["key-3"]);
            Assert.AreEqual(expectedEvent.Data["key-4"], actualEvent.Data["key-4"]);
            Assert.AreEqual(expectedEvent.Data["idempotence_id"].ToString().Length, actualEvent.Data["idempotence_id"].ToString().Length);
            Assert.AreEqual(expectedEvent.Data["data_source"], actualEvent.Data["data_source"]);
            Assert.AreEqual(expectedEvent.Data["data_source_type"], actualEvent.Data["data_source_type"]);
            Assert.AreEqual(expectedEvent.Data["data_source_version"], actualEvent.Data["data_source_version"]);
        }

        [Test]
        public void ShouldAttemptToFlushAnEmptyQueueAtFlushInterval() { }

        [Test]
        public void ShouldDispatchEventsInCorrectNumberOfBatches() { }

        [Test]
        public void ShouldDispatchEventsWithCorrectPayload() { }

        [Test]
        public void ShouldRetryFailedEvents() { }

        [Test]
        public void ShouldFlushAllScheduledEventsBeforeStopping() { }

        [Test]
        public void ShouldPrepareCorrectPayloadForRegisterVuid() { }

        [Test]
        public void ShouldPrepareCorrectPayloadForIdentifyUser() { }

        [Test]
        public void ShouldApplyUpdatedOdpConfigurationWhenAvailable() { }
    }
}
