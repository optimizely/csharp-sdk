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
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizelySDK.Tests.OdpTests
{
    /// <summary>
    /// Shared utility methods used for stubbing HttpClient instances
    /// </summary>
    public static class HttpClientTestUtil
    {
        /// <summary>
        /// Create an HttpClient instance that returns an expected response
        /// </summary>
        /// <param name="statusCode">Http Status Code to return</param>
        /// <param name="content">Message body content to return</param>
        /// <returns>HttpClient instance that will return desired HttpResponseMessage</returns>
        public static HttpClient MakeHttpClient(HttpStatusCode statusCode,
            string content = default
        )
        {
            var response = new HttpResponseMessage(statusCode);

            if (!string.IsNullOrWhiteSpace(content))
            {
                response.Content = new StringContent(content);
            }

            var mockedHandler = new Mock<HttpMessageHandler>();
            mockedHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            return new HttpClient(mockedHandler.Object);
        }

        /// <summary>
        /// Create an HttpClient instance that will timeout for SendAsync calls
        /// </summary>
        /// <returns>HttpClient instance that throw TimeoutException</returns>
        public static HttpClient MakeHttpClientWithTimeout()
        {
            var mockedHandler = new Mock<HttpMessageHandler>();
            mockedHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Throws<TimeoutException>();
            return new HttpClient(mockedHandler.Object);
        }
    }
}
