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

using NUnit.Framework;

namespace OptimizelySDK.Tests.OdpTests
{
    [TestFixture]
    public class OdpEventManagerTests
    {
        [SetUp]
        public void Setup() { }

        [Test]
        public void ShouldLogAndDiscardEventsWhenEventManagerNotRunning() { }

        [Test]
        public void ShouldDiscardEventsWithInvalidData() { }

        [Test]
        public void ShouldLogMaxQueueHitAndDiscard() { }

        [Test]
        public void ShouldAddAdditionalInformationToEachEvent() { }

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
