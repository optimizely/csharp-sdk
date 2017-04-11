/* 
 * Copyright 2017, Optimizely
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
using OptimizelySDK.Logger;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace OptimizelySDK.Event.Dispatcher
{
    public class HttpClientEventDispatcher45 : IEventDispatcher
    {
        public ILogger Logger { get; set; } = new DefaultLogger();

        /// <summary>
        /// Timeout for the HTTP request (10 seconds)
        /// </summary>
        const int TIMEOUT_MS = 10000;

        /// <summary>
        /// Dispatch an Event asynchronously
        /// </summary>
        private async void DispatchEventAsync(LogEvent logEvent)
        {
            try
            {
                string json = logEvent.GetParamsAsJson();

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(TIMEOUT_MS);

                    var request = new HttpRequestMessage
                    {
                        RequestUri = new Uri(logEvent.Url),
                        Method = HttpMethod.Post,
                        // The Content-Type header applies to the Content, not the Request itself
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
                    };

                    foreach (var h in logEvent.Headers)
                        if (h.Key.ToLower() != "content-type")
                            request.Content.Headers.Add(h.Key, h.Value);

                    var result = await client.SendAsync(request);
                    result.EnsureSuccessStatusCode();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.ERROR, string.Format("Error Dispatching Event: {0}", ex.Message));
            }
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