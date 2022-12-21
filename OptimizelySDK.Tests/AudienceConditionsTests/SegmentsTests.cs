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
using System.Collections.Generic;

namespace OptimizelySDK.Tests.AudienceConditionsTests
{
    [TestFixture]
    public class SegmentsTests
    {
        private Mock<ILogger> _mockLogger;

        [TestFixtureSetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger>();
            _mockLogger.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
        }

        [Test]
        public void ShouldGetSegmentsFromDatafileTypedAudiences()
        {
            var expectedSegments = new SortedSet<string>
            {
                "atsbugbashsegmentgender",
                "atsbugbashsegmenthaspurchased",
                "has_email_opted_out",
                "atsbugbashsegmentdob",
            };
            var optimizelyClient = new Optimizely(TestData.OdpSegmentsDatafile,
                new ValidEventDispatcher(), _mockLogger.Object);

            var allSegments = optimizelyClient.ProjectConfigManager.GetConfig().Segments;

            var actualSegments = new SortedSet<string>(allSegments);
            Assert.AreEqual(expectedSegments, actualSegments);
        }
    }
}
