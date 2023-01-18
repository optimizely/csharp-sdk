/* 
 * Copyright 2022-2023 Optimizely
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
using System.Collections.Generic;
using System.Linq;

namespace OptimizelySDK.Tests.OdpTests
{
    [TestFixture]
    public class OdpManagerTest
    {
        private const string API_KEY = "JUs7AFak3aP1K3y";
        private const string API_HOST = "https://odp-api.example.com";
        private const string UPDATED_API_KEY = "D1fF3rEn7kEy";
        private const string UPDATED_ODP_ENDPOINT = "https://an-updated-odp-endpoint.example.com";
        private const string TEST_EVENT_TYPE = "event-type";
        private const string TEST_EVENT_ACTION = "event-action";
        private const string VALID_FS_USER_ID = "valid-test-fs-user-id";

        private readonly List<string> _updatedSegmentsToCheck = new List<string>
        {
            "updated-segment-1",
            "updated-segment-2",
        };

        private readonly Dictionary<string, string> _testEventIdentifiers =
            new Dictionary<string, string>
            {
                {
                    "fs_user_id", "id-key-1"
                },
            };

        private readonly Dictionary<string, object> _testEventData = new Dictionary<string, object>
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
        };

        private readonly List<string> _emptySegmentsToCheck = new List<string>(0);

        private Mock<ILogger> _mockLogger;
        private Mock<IOdpEventManager> _mockOdpEventManager;
        private Mock<IOdpSegmentManager> _mockSegmentManager;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger>();
            _mockOdpEventManager = new Mock<IOdpEventManager>();
            _mockSegmentManager = new Mock<IOdpSegmentManager>();
        }

        [Test]
        public void ShouldStartEventManagerWhenOdpManagerIsInitialized()
        {
            _mockOdpEventManager.Setup(e => e.Start());

            _ = new OdpManager.Builder().
                WithSegmentManager(_mockSegmentManager.Object).
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build();

            _mockOdpEventManager.Verify(e => e.Start(), Times.Once);
        }

        [Test]
        public void ShouldStopEventManagerWhenCloseIsCalled()
        {
            _mockOdpEventManager.Setup(e => e.Stop());
            var manager = new OdpManager.Builder().
                WithSegmentManager(_mockSegmentManager.Object).
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build();
            manager.UpdateSettings(API_KEY, API_HOST, _emptySegmentsToCheck);

            manager.Dispose();

            _mockOdpEventManager.Verify(e => e.Stop(), Times.Once);
        }

        [Test]
        public void ShouldUseNewSettingsInEventManagerWhenOdpConfigIsUpdated()
        {
            var eventManagerParameterCollector = new List<OdpConfig>();
            _mockOdpEventManager.Setup(e =>
                e.UpdateSettings(Capture.In(eventManagerParameterCollector)));
            var manager = new OdpManager.Builder().
                WithSegmentManager(_mockSegmentManager.Object).
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build();

            var wasUpdated = manager.UpdateSettings(UPDATED_API_KEY, UPDATED_ODP_ENDPOINT,
                _updatedSegmentsToCheck);

            Assert.IsTrue(wasUpdated);
            var configPassedToOdpEventManager = eventManagerParameterCollector.FirstOrDefault();
            Assert.AreEqual(UPDATED_API_KEY, configPassedToOdpEventManager?.ApiKey);
            Assert.AreEqual(UPDATED_ODP_ENDPOINT, configPassedToOdpEventManager.ApiHost);
            Assert.AreEqual(_updatedSegmentsToCheck, configPassedToOdpEventManager.SegmentsToCheck);
        }

        [Test]
        public void ShouldUseNewSettingsInSegmentManagerWhenOdpConfigIsUpdated()
        {
            var segmentManagerParameterCollector = new List<OdpConfig>();
            _mockSegmentManager.Setup(s =>
                s.UpdateSettings(Capture.In(segmentManagerParameterCollector)));
            var manager = new OdpManager.Builder().
                WithSegmentManager(_mockSegmentManager.Object).
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build();

            var wasUpdated = manager.UpdateSettings(UPDATED_API_KEY, UPDATED_ODP_ENDPOINT,
                _updatedSegmentsToCheck);

            Assert.IsTrue(wasUpdated);
            var configPassedToSegmentManager = segmentManagerParameterCollector.FirstOrDefault();
            Assert.AreEqual(UPDATED_API_KEY, configPassedToSegmentManager?.ApiKey);
            Assert.AreEqual(UPDATED_ODP_ENDPOINT, configPassedToSegmentManager.ApiHost);
            Assert.AreEqual(_updatedSegmentsToCheck, configPassedToSegmentManager.SegmentsToCheck);
        }

        [Test]
        public void ShouldHandleOdpConfigSettingsNoChange()
        {
            _mockSegmentManager.Setup(s => s.UpdateSettings(It.IsAny<OdpConfig>()));
            _mockOdpEventManager.Setup(e => e.UpdateSettings(It.IsAny<OdpConfig>()));
            var manager = new OdpManager.Builder().
                WithSegmentManager(_mockSegmentManager.Object).
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build();
            manager.UpdateSettings(API_KEY, API_HOST, _emptySegmentsToCheck); // initial set
            _mockOdpEventManager.ResetCalls();
            _mockSegmentManager.ResetCalls();

            // attempt to set with the same config
            var wasUpdated = manager.UpdateSettings(API_KEY, API_HOST, _emptySegmentsToCheck);

            Assert.IsFalse(wasUpdated);
            _mockSegmentManager.Verify(s => s.UpdateSettings(It.IsAny<OdpConfig>()), Times.Never);
            _mockOdpEventManager.Verify(e => e.UpdateSettings(It.IsAny<OdpConfig>()), Times.Never);
        }

        [Test]
        public void ShouldUpdateSettingsWithReset()
        {
            _mockOdpEventManager.Setup(e => e.UpdateSettings(It.IsAny<OdpConfig>()));
            _mockSegmentManager.Setup(s => s.ResetCache());
            _mockSegmentManager.Setup(s => s.UpdateSettings(It.IsAny<OdpConfig>()));
            var manager = new OdpManager.Builder().
                WithSegmentManager(_mockSegmentManager.Object).
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build();

            var wasUpdated = manager.UpdateSettings(UPDATED_API_KEY, UPDATED_ODP_ENDPOINT,
                _updatedSegmentsToCheck);

            Assert.IsTrue(wasUpdated);
            _mockOdpEventManager.Verify(e => e.UpdateSettings(It.IsAny<OdpConfig>()), Times.Once);
            _mockSegmentManager.Verify(s => s.ResetCache(), Times.Once);
            _mockSegmentManager.Verify(s => s.UpdateSettings(It.IsAny<OdpConfig>()), Times.Once);
        }

        [Test]
        public void ShouldDisableOdpThroughConfiguration()
        {
            _mockOdpEventManager.Setup(e => e.Start());
            _mockOdpEventManager.Setup(e => e.IsStarted).Returns(true);
            _mockOdpEventManager.Setup(e => e.SendEvent(It.IsAny<OdpEvent>()));
            _mockOdpEventManager.Setup(e => e.UpdateSettings(It.IsAny<OdpConfig>()));
            var manager = new OdpManager.Builder().
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build(); // auto-start event manager attempted, but no config
            manager.UpdateSettings(API_KEY, API_HOST,
                _emptySegmentsToCheck); // event manager config added + auto-start

            // should send event
            manager.SendEvent(TEST_EVENT_TYPE, TEST_EVENT_ACTION, _testEventIdentifiers,
                _testEventData);

            _mockOdpEventManager.Verify(e => e.Start(), Times.Once);
            _mockOdpEventManager.Verify(e => e.UpdateSettings(It.IsAny<OdpConfig>()), Times.Once);
            _mockOdpEventManager.Verify(e => e.SendEvent(It.IsAny<OdpEvent>()), Times.Once);
            _mockLogger.Verify(l =>
                l.Log(LogLevel.ERROR, "ODP event not dispatched (ODP disabled)."), Times.Never);

            _mockOdpEventManager.ResetCalls();
            _mockLogger.ResetCalls();

            // remove config and try sending again
            manager.UpdateSettings(string.Empty, string.Empty, _emptySegmentsToCheck);
            manager.SendEvent(TEST_EVENT_TYPE, TEST_EVENT_ACTION, _testEventIdentifiers,
                _testEventData);
            manager.Dispose();

            // should not try to send and provide a log message
            _mockOdpEventManager.Verify(e => e.SendEvent(It.IsAny<OdpEvent>()), Times.Never);
            _mockLogger.Verify(l =>
                l.Log(LogLevel.ERROR, "ODP event not dispatched (ODP disabled)."), Times.Once);
        }

        [Test]
        public void ShouldGetEventManager()
        {
            var manager = new OdpManager.Builder().
                WithSegmentManager(_mockSegmentManager.Object).
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build();
            manager.UpdateSettings(API_KEY, API_HOST, _emptySegmentsToCheck);

            Assert.IsNotNull(manager.EventManager);
        }

        [Test]
        public void ShouldGetSegmentManager()
        {
            var manager = new OdpManager.Builder().
                WithSegmentManager(_mockSegmentManager.Object).
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build();
            manager.UpdateSettings(API_KEY, API_HOST, _emptySegmentsToCheck);

            Assert.IsNotNull(manager.SegmentManager);
        }

        [Test]
        public void ShouldIdentifyUserWhenOdpIsIntegrated()
        {
            _mockOdpEventManager.Setup(e => e.IdentifyUser(It.IsAny<string>()));
            _mockOdpEventManager.Setup(e => e.IsStarted).Returns(true);
            var manager = new OdpManager.Builder().
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build();
            manager.UpdateSettings(API_KEY, API_HOST, _emptySegmentsToCheck);

            manager.IdentifyUser(VALID_FS_USER_ID);
            manager.Dispose();

            _mockLogger.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Never);
            _mockOdpEventManager.Verify(e => e.IdentifyUser(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void ShouldNotIdentifyUserWhenOdpDisabled()
        {
            _mockOdpEventManager.Setup(e => e.IdentifyUser(It.IsAny<string>()));
            _mockOdpEventManager.Setup(e => e.IsStarted).Returns(true);
            var manager = new OdpManager.Builder().
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build(false);
            manager.UpdateSettings(API_KEY, API_HOST, _emptySegmentsToCheck);

            manager.IdentifyUser(VALID_FS_USER_ID);
            manager.Dispose();

            _mockLogger.Verify(l =>
                l.Log(LogLevel.DEBUG, "ODP identify event not dispatched (ODP disabled)."));
            _mockOdpEventManager.Verify(e => e.IdentifyUser(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ShouldSendEventWhenOdpIsIntegrated()
        {
            _mockOdpEventManager.Setup(e => e.SendEvent(It.IsAny<OdpEvent>()));
            _mockOdpEventManager.Setup(e => e.IsStarted).Returns(true);
            var manager = new OdpManager.Builder().
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build();
            manager.UpdateSettings(API_KEY, API_HOST, _emptySegmentsToCheck);

            manager.SendEvent(TEST_EVENT_TYPE, TEST_EVENT_ACTION, _testEventIdentifiers,
                _testEventData);
            manager.Dispose();

            _mockOdpEventManager.Verify(e => e.SendEvent(It.IsAny<OdpEvent>()), Times.Once);
        }

        [Test]
        public void ShouldNotSendEventOdpNotIntegrated()
        {
            _mockOdpEventManager.Setup(e => e.SendEvent(It.IsAny<OdpEvent>()));
            var manager = new OdpManager.Builder().
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build(false); // do not enable
            manager.UpdateSettings(string.Empty, string.Empty, _emptySegmentsToCheck);

            manager.SendEvent(TEST_EVENT_TYPE, TEST_EVENT_ACTION, _testEventIdentifiers,
                _testEventData);
            manager.Dispose();

            _mockLogger.Verify(l =>
                l.Log(LogLevel.ERROR, "ODP event not dispatched (ODP disabled)."), Times.Once);
            _mockOdpEventManager.Verify(e => e.SendEvent(It.IsAny<OdpEvent>()), Times.Never);
        }
    }
}
