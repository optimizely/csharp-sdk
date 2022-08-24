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

using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Entity;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OptimizelySDK.Odp.Client
{
    /// <summary>
    /// Http implementation for sending requests and handling responses to Optimizely Data Platform
    /// </summary>
    public class OdpClient : IOdpClient
    {
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
        private readonly HttpClient _client;

        /// <summary>
        /// An implementation for sending requests and handling responses to Optimizely Data Platform (ODP)
        /// </summary>
        /// <param name="errorHandler">Handler to record exceptions</param>
        /// <param name="logger">Collect and record events/errors for this ODP client</param>
        /// <param name="client">Client implementation to send/receive requests over HTTP</param>
        public OdpClient(IErrorHandler errorHandler = null, ILogger logger = null,
            HttpClient client = null
        )
        {
            _errorHandler = errorHandler ?? new NoOpErrorHandler();
            _logger = logger ?? new NoOpLogger();
            _client = client ?? new HttpClient();
        }

        /// <summary>
        /// Synchronous handler for querying the ODP GraphQL endpoint 
        /// </summary>
        /// <param name="parameters">Parameters inputs to send to ODP</param>
        /// <returns>JSON response from ODP</returns>
        public string QuerySegments(QuerySegmentsParameters parameters)
        {
            HttpResponseMessage response;
            try
            {
                response = QuerySegmentsAsync(parameters).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex);
                _logger.Log(LogLevel.ERROR, "Audience segments fetch failed (network error)");
                return default;
            }

            var responseStatusCode = (int)response.StatusCode;
            if (responseStatusCode >= 400 && responseStatusCode < 600)
            {
                _logger.Log(LogLevel.ERROR,
                    $"Audience segments fetch failed ({responseStatusCode})");
                return default;
            }

            return response.Content.ReadAsStringAsync().Result;
        }

        /// <summary>
        /// Asynchronous handler for querying the ODP GraphQL endpoint
        /// </summary>
        /// <param name="parameters">Parameters inputs to send to ODP</param>
        /// <returns>JSON response from ODP</returns>
        private async Task<HttpResponseMessage> QuerySegmentsAsync(
            QuerySegmentsParameters parameters
        )
        {
            var request = BuildRequestMessage(parameters.ToGraphQLJson(), parameters);

            var response = await _client.SendAsync(request);

            return response;
        }

        /// <summary>
        /// Produces the request GraphQL query payload 
        /// </summary>
        /// <param name="jsonQuery">JSON GraphQL query</param>
        /// <param name="parameters">Configuration used to connect to ODP</param>
        /// <returns>Formed HTTP request message ready to be transmitted</returns>
        private static HttpRequestMessage BuildRequestMessage(string jsonQuery,
            QuerySegmentsParameters parameters
        )
        {
            const string API_HEADER_KEY = "x-api-key";
            const string CONTENT_TYPE = "application/json";
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(parameters.ApiHost),
                Method = HttpMethod.Post,
                Headers =
                {
                    {
                        API_HEADER_KEY, parameters.ApiKey
                    },
                },
                Content = new StringContent(jsonQuery, Encoding.UTF8, CONTENT_TYPE),
            };

            return request;
        }
    }
}
