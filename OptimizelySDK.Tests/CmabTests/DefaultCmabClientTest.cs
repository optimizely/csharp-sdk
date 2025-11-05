/* 
* Copyright 2025, Optimizely
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

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using OptimizelySDK.Cmab;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Exceptions;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Tests.CmabTests
{
    [TestFixture]
    public class DefaultCmabClientTest
    {
        private class ResponseStep
        {
            public HttpStatusCode Status { get; private set; }
            public string Body { get; private set; }
            public ResponseStep(HttpStatusCode status, string body)
            {
                Status = status;
                Body = body;
            }
        }

        private static HttpClient MakeClient(params ResponseStep[] sequence)
        {
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var queue = new Queue<ResponseStep>(sequence);

            handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).Returns((HttpRequestMessage _, CancellationToken __) =>
            {
                if (queue.Count == 0)
                    throw new InvalidOperationException("No more mocked responses available.");

                var step = queue.Dequeue();
                var response = new HttpResponseMessage(step.Status);
                if (step.Body != null)
                {
                    response.Content = new StringContent(step.Body);
                }
                return Task.FromResult(response);
            });

            return new HttpClient(handler.Object);
        }

        private static HttpClient MakeClientExceptionSequence(params Exception[] sequence)
        {
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var queue = new Queue<Exception>(sequence);

            handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).Returns((HttpRequestMessage _, CancellationToken __) =>
            {
                if (queue.Count == 0)
                    throw new InvalidOperationException("No more mocked exceptions available.");

                var ex = queue.Dequeue();
                var tcs = new TaskCompletionSource<HttpResponseMessage>();
                tcs.SetException(ex);
                return tcs.Task;
            });

            return new HttpClient(handler.Object);
        }

        private static string ValidBody(string variationId = "v1")
            => $"{{\"predictions\":[{{\"variation_id\":\"{variationId}\"}}]}}";

        [Test]
        public void FetchDecisionReturnsSuccessNoRetry()
        {
            var http = MakeClient(new ResponseStep(HttpStatusCode.OK, ValidBody("v1")));
            var client = new DefaultCmabClient(CmabConstants.DEFAULT_PREDICTION_URL_TEMPLATE, http, retryConfig: null, logger: new NoOpLogger(), errorHandler: new NoOpErrorHandler());
            var result = client.FetchDecision("rule-1", "user-1", null, "uuid-1");

            Assert.AreEqual("v1", result);
        }

        [Test]
        public void FetchDecisionHttpExceptionNoRetry()
        {
            var http = MakeClientExceptionSequence(new HttpRequestException("boom"));
            var client = new DefaultCmabClient(CmabConstants.DEFAULT_PREDICTION_URL_TEMPLATE, http, retryConfig: null);

            Assert.Throws<CmabFetchException>(() =>
                client.FetchDecision("rule-1", "user-1", null, "uuid-1"));
        }

        [Test]
        public void FetchDecisionNon2xxNoRetry()
        {
            var http = MakeClient(new ResponseStep(HttpStatusCode.InternalServerError, null));
            var client = new DefaultCmabClient(CmabConstants.DEFAULT_PREDICTION_URL_TEMPLATE, http, retryConfig: null);

            Assert.Throws<CmabFetchException>(() =>
                client.FetchDecision("rule-1", "user-1", null, "uuid-1"));
        }

        [Test]
        public void FetchDecisionInvalidJsonNoRetry()
        {
            var http = MakeClient(new ResponseStep(HttpStatusCode.OK, "not json"));
            var client = new DefaultCmabClient(CmabConstants.DEFAULT_PREDICTION_URL_TEMPLATE, http, retryConfig: null);

            Assert.Throws<CmabInvalidResponseException>(() =>
                client.FetchDecision("rule-1", "user-1", null, "uuid-1"));
        }

        [Test]
        public void FetchDecisionInvalidStructureNoRetry()
        {
            var http = MakeClient(new ResponseStep(HttpStatusCode.OK, "{\"predictions\":[]}"));
            var client = new DefaultCmabClient(CmabConstants.DEFAULT_PREDICTION_URL_TEMPLATE, http, retryConfig: null);

            Assert.Throws<CmabInvalidResponseException>(() =>
                client.FetchDecision("rule-1", "user-1", null, "uuid-1"));
        }

        [Test]
        public void FetchDecisionSuccessWithRetryFirstTry()
        {
            var http = MakeClient(new ResponseStep(HttpStatusCode.OK, ValidBody("v2")));
            var retry = new CmabRetryConfig(maxRetries: 2, initialBackoff: TimeSpan.Zero, maxBackoff: TimeSpan.FromSeconds(1), backoffMultiplier: 2.0);
            var client = new DefaultCmabClient(CmabConstants.DEFAULT_PREDICTION_URL_TEMPLATE, http, retry);
            var result = client.FetchDecision("rule-1", "user-1", null, "uuid-1");

            Assert.AreEqual("v2", result);
        }

        [Test]
        public void FetchDecisionSuccessWithRetryThirdTry()
        {
            var http = MakeClient(
                new ResponseStep(HttpStatusCode.InternalServerError, null),
                new ResponseStep(HttpStatusCode.InternalServerError, null),
                new ResponseStep(HttpStatusCode.OK, ValidBody("v3"))
            );
            var retry = new CmabRetryConfig(maxRetries: 2, initialBackoff: TimeSpan.Zero, maxBackoff: TimeSpan.FromSeconds(1), backoffMultiplier: 2.0);
            var client = new DefaultCmabClient(CmabConstants.DEFAULT_PREDICTION_URL_TEMPLATE, http, retry);
            var result = client.FetchDecision("rule-1", "user-1", null, "uuid-1");

            Assert.AreEqual("v3", result);
        }

        [Test]
        public void FetchDecisionExhaustsAllRetries()
        {
            var http = MakeClient(
                new ResponseStep(HttpStatusCode.InternalServerError, null),
                new ResponseStep(HttpStatusCode.InternalServerError, null),
                new ResponseStep(HttpStatusCode.InternalServerError, null)
            );
            var retry = new CmabRetryConfig(maxRetries: 2, initialBackoff: TimeSpan.Zero, maxBackoff: TimeSpan.FromSeconds(1), backoffMultiplier: 2.0);
            var client = new DefaultCmabClient(CmabConstants.DEFAULT_PREDICTION_URL_TEMPLATE, http, retry);

            Assert.Throws<CmabFetchException>(() =>
                client.FetchDecision("rule-1", "user-1", null, "uuid-1"));
        }

        [Test]
        public void FetchDecision_CustomEndpoint_CallsCorrectUrl()
        {
            var customEndpoint = "https://custom.example.com/api/{0}";
            string capturedUrl = null;

            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage req, CancellationToken _) =>
                {
                    capturedUrl = req.RequestUri.ToString();
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(ValidBody("variation123"))
                    };
                    return Task.FromResult(response);
                });

            var http = new HttpClient(handler.Object);
            var client = new DefaultCmabClient(customEndpoint, http, retryConfig: null);
            var result = client.FetchDecision("rule-456", "user-1", null, "uuid-1");

            Assert.AreEqual("variation123", result);
            Assert.AreEqual("https://custom.example.com/api/rule-456", capturedUrl, 
                "Should call custom endpoint with rule ID formatted into template");
        }
    }
}
