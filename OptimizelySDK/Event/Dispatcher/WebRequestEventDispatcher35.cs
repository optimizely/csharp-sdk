/* 
 * Copyright 2017, 2026, Optimizely
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

#if NET35 || NET40
using System;
using System.IO;
using System.Net;
using System.Threading;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Event.Dispatcher
{
    public class WebRequestClientEventDispatcher35 : IEventDispatcher
    {
        public Logger.ILogger Logger { get; set; }

        /// <summary>
        /// Dispatch the Event with retry and exponential backoff.
        /// The call will not wait for the result, it returns after sending (fire and forget)
        /// But it does get called back asynchronously when the response comes and handles
        /// </summary>
        /// <param name="logEvent"></param>
        public void DispatchEvent(LogEvent logEvent)
        {
            ThreadPool.QueueUserWorkItem(_ => DispatchWithRetry(logEvent));
        }

        /// <summary>
        /// Dispatch event with retry logic and exponential backoff.
        /// </summary>
        private void DispatchWithRetry(LogEvent logEvent)
        {
            var attemptNumber = 0;
            var backoffMs = EventRetryConfig.INITIAL_BACKOFF_MS;
            var maxAttempts = 1 + EventRetryConfig.MAX_RETRIES;

            while (attemptNumber < maxAttempts)
            {
                HttpWebRequest request = null;
                HttpWebResponse response = null;
                try
                {
                    request = (HttpWebRequest)WebRequest.Create(logEvent.Url);
                    request.UserAgent = "Optimizely-csharp-SDKv01";
                    request.Method = logEvent.HttpVerb;

                    foreach (var h in logEvent.Headers)
                    {
                        if (!WebHeaderCollection.IsRestricted(h.Key))
                        {
                            request.Headers[h.Key] = h.Value;
                        }
                    }

                    request.ContentType = "application/json";

                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        streamWriter.Write(logEvent.GetParamsAsJson());
                        streamWriter.Flush();
                        streamWriter.Close();
                    }

                    response = (HttpWebResponse)request.GetResponse();

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (var responseStream = response.GetResponseStream())
                        using (var responseReader = new StreamReader(responseStream, System.Text.Encoding.UTF8))
                        {
                            responseReader.ReadToEnd();
                        }
                        // Success - exit the retry loop
                        return;
                    }
                }
                catch (WebException ex)
                {
                    var httpResponse = ex.Response as HttpWebResponse;
                    var shouldRetry = ShouldRetry(httpResponse?.StatusCode);

                    if (shouldRetry && attemptNumber < maxAttempts - 1)
                    {
                        Thread.Sleep(backoffMs);
                        backoffMs = Math.Min(EventRetryConfig.MAX_BACKOFF_MS,
                            (int)(backoffMs * EventRetryConfig.BACKOFF_MULTIPLIER));
                        attemptNumber++;
                    }
                    else
                    {
                        LogMessage(LogLevel.ERROR,
                            string.Format("Error Dispatching Event after {0} attempt(s): {1}",
                                attemptNumber + 1, ex.GetAllMessages()));
                        return;
                    }
                }
                catch (Exception ex)
                {
                    // For non-web exceptions, log and don't retry
                    LogMessage(LogLevel.ERROR, "Error Dispatching Event: " + ex.GetAllMessages());
                    return;
                }
                finally
                {
                    response?.Close();
                }
            }
        }

        /// <summary>
        /// Determines whether a request should be retried based on HTTP status code.
        /// Retries on 5xx server errors and network failures (null status code).
        /// </summary>
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
        /// Helper method to log messages safely when Logger might be null.
        /// </summary>
        private void LogMessage(LogLevel level, string message)
        {
            Logger?.Log(level, message);
        }
    }
}
#endif
