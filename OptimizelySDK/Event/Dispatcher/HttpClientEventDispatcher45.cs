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
        /// Dispatch an Event asynchronously
        /// </summary>
        private async void DispatchEventAsync(LogEvent logEvent)
        {
            try
            {
                string json = logEvent.GetParamsAsJson();
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

                var result = await Client.SendAsync(request);
                result.EnsureSuccessStatusCode();
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
        public void DispatchEventSilently(LogEvent logEvent)
        {
            Task.Run(() => DispatchEventAsync(logEvent));
        }

        /// <summary>
        /// Dispatch an event Asynchronously by creating a new task and calls the
        /// Async version of DispatchEvent
        /// This is a "sequential" option
        /// </summary>
        public void DispatchEvent(LogEvent logEvent)
        {
            DispatchEventAsync(logEvent);
        }
    }
}
#endif