/* 
 * Copyright 2022 Optimizely
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

        private readonly List<string> _updatedSegmentsToCheck = new List<string>
        {
            "updated-segment-1",
            "updated-segment-2",
        };

        private readonly List<string> _emptySegmentsToCheck = new List<string>(0);

        private OdpConfig _odpConfig;
        private Mock<ILogger> _mockLogger;
        private Mock<IOdpEventManager> _mockOdpEventManager;
        private Mock<IOdpSegmentManager> _mockSegmentManager;

        [SetUp]
        public void Setup()
        {
            _odpConfig = new OdpConfig(API_KEY, API_HOST, _emptySegmentsToCheck);
            _mockLogger = new Mock<ILogger>();
            _mockOdpEventManager = new Mock<IOdpEventManager>();
            _mockSegmentManager = new Mock<IOdpSegmentManager>();
        }

        [Test]
        public void ShouldStartEventManagerWhenOdpManagerIsInitialized()
        {
            _mockOdpEventManager.Setup(e => e.Start());

            _ = new OdpManager.Builder().WithOdpConfig(_odpConfig).
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
            var manager = new OdpManager.Builder().WithOdpConfig(_odpConfig).
                WithSegmentManager(_mockSegmentManager.Object).
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build();

            manager.Close();

            _mockOdpEventManager.Verify(e => e.Stop(), Times.Once);
        }

        [Test]
        public void ShouldUseNewSettingsInEventManagerWhenOdpConfigIsUpdated()
        {
            var eventManagerParameterCollector = new List<OdpConfig>();
            _mockOdpEventManager.Setup(e =>
                e.UpdateSettings(Capture.In(eventManagerParameterCollector)));
            var manager = new OdpManager.Builder().WithOdpConfig(_odpConfig).
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
            var manager = new OdpManager.Builder().WithOdpConfig(_odpConfig).
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
            var manager = new OdpManager.Builder().WithOdpConfig(_odpConfig).
                WithSegmentManager(_mockSegmentManager.Object).
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build();

            var wasUpdated = manager.UpdateSettings(_odpConfig.ApiKey, _odpConfig.ApiHost,
                _odpConfig.SegmentsToCheck);

            Assert.IsFalse(wasUpdated);
            _mockSegmentManager.Verify(s => s.UpdateSettings(It.IsAny<OdpConfig>()), Times.Never);
            _mockOdpEventManager.Verify(e => e.UpdateSettings(It.IsAny<OdpConfig>()), Times.Never);
        }

        [Test]
        public void ShouldUpdateSettingsWithReset()
        {
            _mockSegmentManager.Setup(s =>
                s.UpdateSettings(It.IsAny<OdpConfig>()));
            var manager = new OdpManager.Builder().WithOdpConfig(_odpConfig).
                WithSegmentManager(_mockSegmentManager.Object).
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build();

            var wasUpdated = manager.UpdateSettings(UPDATED_API_KEY, UPDATED_ODP_ENDPOINT,
                _updatedSegmentsToCheck);

            Assert.IsTrue(wasUpdated);
            _mockSegmentManager.Verify(s => s.ResetCache(), Times.Once);
        }

        [Test]
        public void ShouldGetEventManager()
        {
            var manager = new OdpManager.Builder().WithOdpConfig(_odpConfig).
                WithSegmentManager(_mockSegmentManager.Object).
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build();

            Assert.IsNotNull(manager.EventManager);
        }

        [Test]
        public void ShouldGetSegmentManager()
        {
            var manager = new OdpManager.Builder().WithOdpConfig(_odpConfig).
                WithSegmentManager(_mockSegmentManager.Object).
                WithEventManager(_mockOdpEventManager.Object).
                WithLogger(_mockLogger.Object).
                Build();

            Assert.IsNotNull(manager.SegmentManager);
        }

        [Ignore, Test]
        public void ShouldDisableOdpThroughConfiguration() { }

        [Ignore, Test]
        public void ShouldIdentifyUserWhenDatafileNotReady() { }

        [Ignore, Test]
        public void ShouldIdentifyUserWhenOdpIsIntegrated() { }

        [Ignore, Test]
        public void ShouldNotIdentifyUserWhenOdpNotIntegrated() { }

        [Ignore, Test]
        public void ShouldNotIdentifyUserWhenOdpDisabled() { }

        [Ignore, Test]
        public void ShouldSendEventWhenOdpIsIntegrated() { }

        [Ignore, Test]
        public void ShouldNotSendEventOdpNotIntegrated() { }
    }
}
