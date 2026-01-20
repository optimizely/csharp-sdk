/*
 * Copyright 2026, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#if !NET35 && !NET40
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Tests.EventTests
{
    [TestFixture]
    public class HttpClientEventDispatcher45Test
    {
        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger>();
            _mockLogger.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            _requestTimestamps = new List<DateTime>();
        }

        private Mock<ILogger> _mockLogger;
        private List<DateTime> _requestTimestamps;
        
        [Test]
        public void DispatchEvent_Success_SingleAttempt()
        {
            var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(handler);
            var dispatcher = new HttpClientEventDispatcher45(httpClient)
            {
                Logger = _mockLogger.Object,
            };
            var logEvent = CreateLogEvent();

            dispatcher.DispatchEvent(logEvent);
            Thread.Sleep(500); // Wait for async dispatch

            Assert.AreEqual(1, handler.RequestCount);
            _mockLogger.Verify(l => l.Log(LogLevel.ERROR, It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void DispatchEvent_ServerError500_RetriesThreeTimes()
        {
            var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError);
            var httpClient = new HttpClient(handler);
            var dispatcher = new HttpClientEventDispatcher45(httpClient)
            {
                Logger = _mockLogger.Object,
            };
            var logEvent = CreateLogEvent();

            dispatcher.DispatchEvent(logEvent);
            Thread.Sleep(1500);

            Assert.AreEqual(3, handler.RequestCount);
            _mockLogger.Verify(
                l => l.Log(LogLevel.ERROR, It.Is<string>(s => s.Contains("3 attempt(s)"))),
                Times.Once);
        }

        [Test]
        public void DispatchEvent_ClientError400_NoRetry()
        {
            var handler = new MockHttpMessageHandler(HttpStatusCode.BadRequest);
            var httpClient = new HttpClient(handler);
            var dispatcher = new HttpClientEventDispatcher45(httpClient)
            {
                Logger = _mockLogger.Object,
            };
            var logEvent = CreateLogEvent();

            dispatcher.DispatchEvent(logEvent);
            Thread.Sleep(500);

            Assert.AreEqual(1, handler.RequestCount);
            _mockLogger.Verify(l => l.Log(LogLevel.ERROR, It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void DispatchEvent_SucceedsOnSecondAttempt_StopsRetrying()
        {
            var handler = new MockHttpMessageHandler(new[]
            {
                HttpStatusCode.InternalServerError,
                HttpStatusCode.OK,
            });
            var httpClient = new HttpClient(handler);
            var dispatcher = new HttpClientEventDispatcher45(httpClient)
            {
                Logger = _mockLogger.Object,
            };
            var logEvent = CreateLogEvent();

            dispatcher.DispatchEvent(logEvent);
            Thread.Sleep(1000);

            Assert.AreEqual(2, handler.RequestCount);
            _mockLogger.Verify(l => l.Log(LogLevel.ERROR, It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void DispatchEvent_ExponentialBackoff_VerifyTiming()
        {
            var handler =
                new MockHttpMessageHandler(HttpStatusCode.InternalServerError, _requestTimestamps);
            var httpClient = new HttpClient(handler);
            var dispatcher = new HttpClientEventDispatcher45(httpClient)
            {
                Logger = _mockLogger.Object,
            };
            var logEvent = CreateLogEvent();

            dispatcher.DispatchEvent(logEvent);
            Thread.Sleep(1500); // Wait for all retries

            Assert.AreEqual(3, _requestTimestamps.Count);

            // First retry after ~200ms
            var firstDelay = (_requestTimestamps[1] - _requestTimestamps[0]).TotalMilliseconds;
            Assert.That(firstDelay, Is.GreaterThanOrEqualTo(180).And.LessThan(350),
                $"First retry delay was {firstDelay}ms, expected ~200ms");

            // Second retry after ~400ms
            var secondDelay = (_requestTimestamps[2] - _requestTimestamps[1]).TotalMilliseconds;
            Assert.That(secondDelay, Is.GreaterThanOrEqualTo(380).And.LessThan(550),
                $"Second retry delay was {secondDelay}ms, expected ~400ms");
        }

        private static LogEvent CreateLogEvent()
        {
            return new LogEvent(
                "https://logx.optimizely.com/v1/events",
                new Dictionary<string, object>
                {
                    { "accountId", "12345" },
                    { "visitors", new object[] { } },
                },
                "POST",
                new Dictionary<string, string>());
        }

        /// <summary>
        ///     Mock HTTP message handler for testing.
        /// </summary>
        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode[] _statusCodes;
            private readonly List<DateTime> _timestamps;
            private int _currentIndex;

            public MockHttpMessageHandler(HttpStatusCode statusCode,
                List<DateTime> timestamps = null
            )
                : this(new[] { statusCode }, timestamps) { }

            public MockHttpMessageHandler(HttpStatusCode[] statusCodes,
                List<DateTime> timestamps = null
            )
            {
                _statusCodes = statusCodes;
                _timestamps = timestamps;
                _currentIndex = 0;
            }

            public int RequestCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                RequestCount++;
                _timestamps?.Add(DateTime.Now);

                var statusCode = _currentIndex < _statusCodes.Length ?
                    _statusCodes[_currentIndex] :
                    _statusCodes[_statusCodes.Length - 1];

                _currentIndex++;

                var response = new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent("{}"),
                };

                return Task.FromResult(response);
            }
        }
    }
}
#endif
