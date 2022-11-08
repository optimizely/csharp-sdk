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
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace OptimizelySDK.Tests.OdpTests
{
    [TestFixture]
    public class OdpSegmentManagerTest
    {
        private const string API_KEY = "S0m3Ap1KEy4U";
        private const string API_HOST = "https://odp-host.example.com";
        private const string FS_USER_ID = "some_valid_user_id";

        private readonly List<string> _segmentsToCheck = new List<string>
        {
            "segment1",
            "segment2",
        };

        private OdpConfig _odpConfig;
        private Mock<IOdpSegmentApiManager> _mockApiManager;
        private Mock<ILogger> _mockLogger;
        private Mock<ICache<List<string>>> _mockCache;

        [SetUp]
        public void Setup()
        {
            _odpConfig = new OdpConfig(API_KEY, API_HOST, _segmentsToCheck);

            _mockApiManager = new Mock<IOdpSegmentApiManager>();

            _mockLogger = new Mock<ILogger>();
            _mockLogger.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            _mockCache = new Mock<ICache<List<string>>>();
        }

        [Test]
        public void ShouldFetchSegmentsOnCacheMiss()
        {
            var keyCollector = new List<string>();
            _mockCache.Setup(c => c.Lookup(Capture.In(keyCollector)))
                .Returns(default(List<string>));
            _mockApiManager.Setup(a => a.FetchSegments(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<OdpUserKeyType>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                .Returns(_segmentsToCheck.ToArray());
            var manager = new OdpSegmentManager(_odpConfig, _mockApiManager.Object,
                Constants.DEFAULT_MAX_CACHE_SIZE, null, _mockLogger.Object, _mockCache.Object);

            var segments = manager.FetchQualifiedSegments(FS_USER_ID);

            var cacheKey = keyCollector.FirstOrDefault();
            Assert.AreEqual($"fs_user_id-$-{FS_USER_ID}", cacheKey);
            _mockApiManager.Verify(a => a.FetchSegments(API_KEY, API_HOST,
                OdpUserKeyType.FS_USER_ID, FS_USER_ID, _odpConfig.SegmentsToCheck), Times.Once);
            _mockLogger.Verify(l =>
                l.Log(LogLevel.DEBUG, "ODP Cache Miss. Making a call to ODP Server."));
            Assert.AreEqual(_segmentsToCheck, segments);
            // verify(mockApiManager, times(1))
            //     .fetchQualifiedSegments(odpConfig.getApiKey(),
            //         odpConfig.getApiHost() + "/v3/graphql", "vuid", "testId",
            //         Arrays.asList("segment1", "segment2"));
            // verify(mockCache, times(1))
            //     .save("vuid-$-testId", Arrays.asList("segment1", "segment2"));
            // verify(mockCache, times(0)).reset();
            //
            //
            // assertEquals(Arrays.asList("segment1", "segment2"), segments);
        }

        [Test]
        public void ShouldFetchSegmentsSuccessOnCacheHit() { }

        [Test]
        public void ShouldHandleFetchSegmentsWithError() { }

        [Test]
        public void ShouldIgnoreCache() { }

        [Test]
        public void ShouldResetCache() { }

        [Test]
        public void ShouldMakeValidCacheKey() { }

        private void setCache() { }

        private void peekCache() { }
    }
}
