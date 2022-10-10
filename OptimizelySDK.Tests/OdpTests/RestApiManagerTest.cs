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
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp;
using OptimizelySDK.Odp.Entity;
using System.Collections.Generic;
using System.Net;

namespace OptimizelySDK.Tests.OdpTests
{
    [TestFixture]
    public class RestApiManagerTest
    {
        private const string VALID_ODP_PUBLIC_KEY = "a-valid-odp-public-key";
        private const string ODP_REST_API_HOST = "https://api.example.com";

        private readonly List<OdpEvent> _odpEvents = new List<OdpEvent>();

        private Mock<IErrorHandler> _mockErrorHandler;
        private Mock<ILogger> _mockLogger;

        [TestFixtureSetUp]
        public void Setup()
        {
            _mockErrorHandler = new Mock<IErrorHandler>();
            _mockLogger = new Mock<ILogger>();
            _mockLogger.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            _odpEvents.Add(new OdpEvent("t1", "a1",
                new Dictionary<string, string>
                {
                    {
                        "id-key-1", "id-value-1"
                    },
                },
                new Dictionary<string, object>
                {
                    {
                        "key11", "value-1"
                    },
                    {
                        "key12", true
                    },
                    {
                        "key13", 3.5
                    },
                    {
                        "key14", null
                    },
                }
            ));
            _odpEvents.Add(new OdpEvent("t2", "a2",
                new Dictionary<string, string>
                {
                    {
                        "id-key-2", "id-value-2"
                    },
                }, new Dictionary<string, object>
                {
                    {
                        "key2", "value-2"
                    },
                }
            ));
        }

        [Test]
        public void ShouldSendEventsSuccessfullyAndNotSuggestRetry()
        {
            var httpClient = HttpClientTestUtil.MakeHttpClient(HttpStatusCode.OK);
            var manger =
                new RestApiManager(_mockLogger.Object, _mockErrorHandler.Object, httpClient);

            var shouldRetry = manger.SendEvents(VALID_ODP_PUBLIC_KEY, ODP_REST_API_HOST,
                _odpEvents);

            Assert.IsFalse(shouldRetry);
        }

        [Test]
        public void ShouldNotSuggestRetryFor400HttpResponse()
        {
            var httpClient = HttpClientTestUtil.MakeHttpClient(HttpStatusCode.BadRequest);
            var manger =
                new RestApiManager(_mockLogger.Object, _mockErrorHandler.Object, httpClient);

            var shouldRetry = manger.SendEvents(VALID_ODP_PUBLIC_KEY, ODP_REST_API_HOST,
                _odpEvents);

            Assert.IsFalse(shouldRetry);
        }

        [Test]
        public void ShouldSuggestRetryFor500HttpResponse()
        {
            var httpClient = HttpClientTestUtil.MakeHttpClient(HttpStatusCode.InternalServerError);
            var manger =
                new RestApiManager(_mockLogger.Object, _mockErrorHandler.Object, httpClient);

            var shouldRetry = manger.SendEvents(VALID_ODP_PUBLIC_KEY, ODP_REST_API_HOST,
                _odpEvents);

            Assert.IsTrue(shouldRetry);
        }

        [Test]
        public void ShouldSuggestRetryForNetworkTimeout() { 
            var httpClient = HttpClientTestUtil.MakeHttpClientWithTimeout();
            var manger =
                new RestApiManager(_mockLogger.Object, _mockErrorHandler.Object, httpClient);

            var shouldRetry = manger.SendEvents(VALID_ODP_PUBLIC_KEY, ODP_REST_API_HOST,
                _odpEvents);

            Assert.IsTrue(shouldRetry);}
    }
}
