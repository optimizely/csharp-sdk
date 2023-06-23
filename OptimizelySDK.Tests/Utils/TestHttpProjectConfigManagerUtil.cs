/*
 * Copyright 2019-2020, 2023 Optimizely
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
using System.Threading.Tasks;
using Moq;
using OptimizelySDK.Config;

namespace OptimizelySDK.Tests.Utils
{
    /// <summary>
    /// This class provides some util methods relevant to HttpProjectConfigManager which helps to write unit tests.
    /// </summary>
    public static class TestHttpProjectConfigManagerUtil
    {
        public static Task MockSendAsync(Mock<HttpProjectConfigManager.HttpClient> httpClientMock,
            string datafile = null, TimeSpan? delay = null,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            Dictionary<string, string> responseContentHeaders = null
        )
        {
            var t = new TaskCompletionSource<bool>();

            httpClientMock.Setup(_ => _.SendAsync(It.IsAny<HttpRequestMessage>()))
                .Returns(() =>
                {
                    if (delay != null)
                    {
                        // This delay mocks the networking delay. And help to see the behavior when get a datafile with some delay.
                        Task.Delay(delay.Value).Wait();
                    }

                    var responseMessage = new HttpResponseMessage
                    {
                        StatusCode = statusCode,
                        Content = new StringContent(datafile ?? string.Empty),
                    };

                    if (responseContentHeaders != null)
                    {
                        foreach (var header in responseContentHeaders)
                        {
                            responseMessage.Content.Headers.Add(header.Key, header.Value);
                        }
                    }

                    return Task.FromResult(responseMessage);
                })
                .Callback(()
                    =>
                {
                    t.SetResult(true);
                });

            return t.Task;
        }

        /// <summary>
        /// This method is only to set Client static field of HttpProjectConfigManager class.
        /// </summary>
        /// <param name="value">value of type HttpProjectConfigManager.HttpClient</param>
        public static void SetClientFieldValue(object value)
        {
            var type = typeof(HttpProjectConfigManager);
            var field = type.GetField("Client",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(new object(), value);
            }
        }
    }
}
