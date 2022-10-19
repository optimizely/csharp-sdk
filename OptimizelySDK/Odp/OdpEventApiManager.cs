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

using Newtonsoft.Json;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Entity;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OptimizelySDK.Odp
{
    /// <summary>
    /// Manager for communicating with ODP's REST API endpoint
    /// </summary>
    public class OdpEventApiManager : IOdpEventApiManager
    {
        /// <summary>
        /// Standard message for ODP event sending errors
        /// </summary>
        private const string EVENT_SENDING_FAILURE_MESSAGE = "ODP event send failed";

        /// <summary>
        /// Error handler used to record errors
        /// </summary>
        private readonly IErrorHandler _errorHandler;

        /// <summary>
        /// Logger used to record messages that occur within the ODP client 
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Http client used for handling requests and responses over HTTP
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Manager for communicating with ODP's REST API endpoint
        /// </summary>
        /// <param name="logger">Collect and record events to log</param>
        /// <param name="errorHandler">Handler to record exceptions</param>
        /// <param name="httpClient">HttpClient to use to send ODP events</param>
        public OdpEventApiManager(ILogger logger = null, IErrorHandler errorHandler = null,
            HttpClient httpClient = null
        )
        {
            _logger = logger ?? new NoOpLogger();
            _errorHandler = errorHandler ?? new NoOpErrorHandler();
            _httpClient = httpClient ?? new HttpClient();
        }

        /// <summary>
        /// Send events to ODP's RESTful API
        /// </summary>
        /// <param name="apiKey">ODP public key</param>
        /// <param name="apiHost">Host of ODP endpoint</param>
        /// <param name="events">ODP events to send</param>
        /// <returns>Retry is true - if network or server error (5xx), otherwise false</returns>
        public bool SendEvents(string apiKey, string apiHost, List<OdpEvent> events)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiHost))
            {
                _logger.Log(LogLevel.ERROR,
                    $"{EVENT_SENDING_FAILURE_MESSAGE} (Parameters apiKey or apiHost invalid)");
                return false;
            }

            if (events.Count == 0)
            {
                _logger.Log(LogLevel.ERROR, $"{EVENT_SENDING_FAILURE_MESSAGE} (no events)");
                return false;
            }

            var endpoint = $"{apiHost}/v3/events";
            var data = JsonConvert.SerializeObject(events);
            var shouldRetry = false;

            HttpResponseMessage response = default;
            try
            {
                response = SendEventsAsync(apiKey, endpoint, data).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex);
                _logger.Log(LogLevel.ERROR, $"{EVENT_SENDING_FAILURE_MESSAGE} (network error)");
                shouldRetry = true;
            }

            var responseStatusCode = response == null ? 0 : (int)response.StatusCode;
            if (responseStatusCode >= 400)
            {
                _logger.Log(LogLevel.ERROR,
                    $"{EVENT_SENDING_FAILURE_MESSAGE} (${responseStatusCode})");
            }

            if (responseStatusCode >= 500)
            {
                shouldRetry = true;
            }

            return shouldRetry;
        }

        /// <summary>
        /// Async call to send the event data to ODP
        /// </summary>
        /// <param name="apiKey">ODP public key</param>
        /// <param name="endpoint">Fully-qualified ODP REST API endpoint</param>
        /// <param name="data">JSON string version of ODP event data</param>
        /// <returns>HTTP response endpoint</returns>
        private async Task<HttpResponseMessage> SendEventsAsync(string apiKey, string endpoint,
            string data
        )
        {
            var request = BuildOdpEventMessage(apiKey, endpoint, data);

            return await _httpClient.SendAsync(request);
        }

        /// <summary>
        /// Build an HTTP request message to send the ODP events
        /// </summary>
        /// <param name="apiKey">ODP public key</param>
        /// <param name="endpoint">Fully-qualified ODP REST API endpoint</param>
        /// <param name="data">JSON string version of ODP event data</param>
        /// <returns>Ready request message to send</returns>
        private static HttpRequestMessage BuildOdpEventMessage(string apiKey, string endpoint,
            string data
        )
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(endpoint),
                Method = HttpMethod.Post,
                Headers =
                {
                    {
                        Constants.HEADER_API_KEY, apiKey
                    },
                },
                Content = new StringContent(data, Encoding.UTF8, "application/json"),
            };

            return request;
        }
    }
}
