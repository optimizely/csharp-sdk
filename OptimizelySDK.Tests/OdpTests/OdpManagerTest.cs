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

using Castle.Core.Logging;
using Moq;
using NUnit.Framework;
using OptimizelySDK.Odp;

namespace OptimizelySDK.Tests.OdpTests
{
    [TestFixture]
    public class OdpManagerTest
    {
        private OdpConfig _odpConfig;
        private Mock<ILogger> _mockLogger;
        private Mock<IOdpEventManager> _mockOdpEventManager;
        private Mock<IOdpSegmentApiManager> _mockSegmentApiManager;

        [SetUp]
        public void Setup() { }

        [Test]
        public void ShouldStartEventManagerWhenOdpManagerIsInitialized() { }

        [Test]
        public void ShouldStopEventManagerWhenCloseIsCalled() { }

        [Test]
        public void ShouldUseNewSettingsInEventManagerWhenOdpConfigIsUpdated() { }

        [Test]
        public void ShouldUseNewSettingsInSegmentManagerWhenOdpConfigIsUpdated() { }

        [Test]
        public void ShouldHandleSettingsNoChange() { }

        [Test]
        public void ShouldUpdateSettingsWithReset() { }

        [Test]
        public void ShouldGetEventManager() { }

        [Test]
        public void ShouldGetSegmentManager() { }

        [Test]
        public void ShouldFetchQualifiedSegments() { }

        [Test]
        public void ShouldDisableOdpThroughConfiguration() { }

        [Test]
        public void ShouldIdentifyUserWhenDatafileNotReady() { }

        [Test]
        public void ShouldIdentifyUserWhenOdpIsIntegrated() { }

        [Test]
        public void ShouldNotIdentifyUserWhenOdpNotIntegrated() { }

        [Test]
        public void ShouldNotIdentifyUserWhenOdpDisabled() { }

        [Test]
        public void ShouldSendEventWhenOdpIsIntegrated() { }

        [Test]
        public void ShouldNotSendEventOdpNotIntegrated() { }
    }
}
