/* 
 * Copyright 2017, 2019, 2026, Optimizely
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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Event.Dispatcher
{
    public class HttpClientEventDispatcher45 : IEventDispatcher
    {
        public ILogger Logger { get; set; } = new DefaultLogger();

        /// <summary>
        /// HTTP client object.
        /// </summary>
        private static readonly HttpClient Client;

        /// <summary>
        /// Constructor for initializing static members.
        /// </summary>
        static HttpClientEventDispatcher45()
        {
            Client = new HttpClient();
        }

        /// <summary>
        /// Dispatch an Event asynchronously with retry and exponential backoff.
        /// Retries on 5xx server errors and network failures.
        /// </summary>
        private async Task DispatchEventAsync(LogEvent logEvent)
        {
            var attemptNumber = 0;
            var backoffMs = EventRetryConfig.INITIAL_BACKOFF_MS;
            var maxAttempts = 1 + EventRetryConfig.MAX_RETRIES; // 1 initial + retries

            while (attemptNumber < maxAttempts)
            {
                HttpResponseMessage response = null;
                try
                {
                    var json = logEvent.GetParamsAsJson();
                    var request = new HttpRequestMessage
                    {
                        RequestUri = new Uri(logEvent.Url),
                        Method = HttpMethod.Post,
                        // The Content-Type header applies to the Content, not the Request itself
                        Content =
                            new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
                    };

                    foreach (var h in logEvent.Headers)
                    {
                        if (h.Key.ToLower() != "content-type")
                        {
                            request.Content.Headers.Add(h.Key, h.Value);
                        }
                    }

                    response = await Client.SendAsync(request).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    
                    // Success - exit the retry loop
                    return;
                }
                catch (HttpRequestException ex)
                {
                    var statusCode = response?.StatusCode;
                    var shouldRetry = ShouldRetry(statusCode);

                    if (shouldRetry && attemptNumber < maxAttempts - 1)
                    {
                        await Task.Delay(backoffMs).ConfigureAwait(false);
                        backoffMs = Math.Min(EventRetryConfig.MAX_BACKOFF_MS,
                            (int)(backoffMs * EventRetryConfig.BACKOFF_MULTIPLIER));
                        attemptNumber++;
                    }
                    else
                    {
                        Logger.Log(LogLevel.ERROR,
                            $"Error Dispatching Event after {attemptNumber + 1} attempt(s): {ex.GetAllMessages()}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    // For non-HTTP exceptions, log and don't retry
                    Logger.Log(LogLevel.ERROR, $"Error Dispatching Event: {ex.GetAllMessages()}");
                    return;
                }
            }
        }

        /// <summary>
        /// Determines whether a request should be retried based on HTTP status code.
        /// Retries on 5xx server errors and network failures (null status code).
        /// </summary>
        /// <param name="statusCode">The HTTP status code, or null for network failures</param>
        /// <returns>True if the request should be retried</returns>
        private static bool ShouldRetry(HttpStatusCode? statusCode)
        {
            // Retry on network failures (no response)
            if (statusCode == null)
            {
                return true;
            }

            // Retry on 5xx server errors
            var code = (int)statusCode.Value;
            return code >= 500 && code < 600;
        }

        /// <summary>
        /// Dispatch an event Asynchronously by creating a new task and calls the 
        /// Async version of DispatchEvent
        /// This is a "Fire and Forget" option
        /// </summary>
        public void DispatchEvent(LogEvent logEvent)
        {
            Task.Run(() => DispatchEventAsync(logEvent));
        }
    }
}
#endif
