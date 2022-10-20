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
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OptimizelySDK.Odp
{
    /// <summary>
    /// Manager for communicating with the Optimizely Data Platform GraphQL endpoint
    /// </summary>
    public class OdpSegmentApiManager : IOdpSegmentApiManager
    {
        /// <summary>
        /// Standard message for audience querying fetch errors
        /// </summary>
        private const string AUDIENCE_FETCH_FAILURE_MESSAGE = "Audience segments fetch failed";

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
        /// Manager for communicating with the Optimizely Data Platform (ODP) GraphQL endpoint
        /// </summary>
        /// <param name="logger">Collect and record events to log</param>
        /// <param name="errorHandler">Handler to record exceptions</param>
        /// <param name="httpClient">HttpClient to use to send queries to ODP</param>
        public OdpSegmentApiManager(ILogger logger = null, IErrorHandler errorHandler = null,
            HttpClient httpClient = null
        )
        {
            _logger = logger ?? new NoOpLogger();
            _errorHandler = errorHandler ?? new NoOpErrorHandler();
            _httpClient = httpClient ?? new HttpClient();
        }

        /// <summary>
        /// Retrieves the audience segments from ODP
        /// </summary>
        /// <param name="apiKey">ODP public key</param>
        /// <param name="apiHost">Host of ODP endpoint</param>
        /// <param name="userKey">Either `vuid` or `fs_user_id key`</param>
        /// <param name="userValue">Associated value to query for the user key</param>
        /// <param name="segmentsToCheck">Audience segments to check for experiment inclusion</param>
        /// <returns>Array of audience segments</returns>
        public string[] FetchSegments(string apiKey, string apiHost, OdpUserKeyType userKey,
            string userValue, List<string> segmentsToCheck
        )
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiHost))
            {
                _logger.Log(LogLevel.ERROR,
                    $"{AUDIENCE_FETCH_FAILURE_MESSAGE} (Parameters apiKey or apiHost invalid)");
                return null;
            }

            if (segmentsToCheck.Count == 0)
            {
                return new string[0];
            }

            var endpoint = $"{apiHost}{Constants.ODP_GRAPHQL_API_ENDPOINT_PATH}";
            var query =
                BuildGetSegmentsGraphQLQuery(userKey.ToString().ToLower(), userValue,
                    segmentsToCheck);

            var segmentsResponseJson = QuerySegments(apiKey, endpoint, query);
            if (string.IsNullOrWhiteSpace(segmentsResponseJson))
            {
                _logger.Log(LogLevel.ERROR,
                    $"{AUDIENCE_FETCH_FAILURE_MESSAGE} (network error)");
                return null;
            }

            var parsedSegments = DeserializeSegmentsFromJson(segmentsResponseJson);
            if (parsedSegments == null)
            {
                var message = $"{AUDIENCE_FETCH_FAILURE_MESSAGE} (decode error)";
                _logger.Log(LogLevel.ERROR, message);
                return null;
            }

            if (parsedSegments.HasErrors)
            {
                var errors = string.Join(";", parsedSegments.Errors.Select(e => e.ToString()));

                _logger.Log(LogLevel.ERROR, $"{AUDIENCE_FETCH_FAILURE_MESSAGE} ({errors})");

                return null;
            }

            if (parsedSegments.Data?.Customer?.Audiences?.Edges == null)
            {
                _logger.Log(LogLevel.ERROR, $"{AUDIENCE_FETCH_FAILURE_MESSAGE} (decode error)");

                return null;
            }

            return parsedSegments.Data.Customer.Audiences.Edges
                .Where(e => e.Node.State == BaseCondition.QUALIFIED)
                .Select(e => e.Node.Name)
                .ToArray();
        }

        /// <summary>
        /// Build GraphQL query for getting segments 
        /// </summary>
        /// <param name="userKey">Either `vuid` or `fs_user_id` key</param>
        /// <param name="userValue">Associated value to query for the user key</param>
        /// <param name="segmentsToCheck">Audience segments to check for experiment inclusion</param>
        /// <returns>GraphQL string payload</returns>
        private static string BuildGetSegmentsGraphQLQuery(string userKey, string userValue,
            IEnumerable segmentsToCheck
        )
        {
            return
                @"{
                    ""query"": ""{
                        query($userId: String, $audiences: [String]) {
                            {
                                customer({userKey}: $userId) {
                                    audiences(subset: $audiences) {
                                        edges {
                                            node {
                                                name
                                                state
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }"", 
                    ""variables"" : {
                        ""userId"": ""{userValue}"",
                        ""audiences"": {audiences}
                    }
                }"
                    .Replace("{userKey}", userKey)
                    .Replace("{userValue}", userValue)
                    .Replace("{audiences}", JsonConvert.SerializeObject(segmentsToCheck));
        }

        /// <summary>
        /// Synchronous handler for querying the ODP GraphQL endpoint
        /// </summary>
        /// <param name="apiKey">ODP public API key</param>
        /// <param name="endpoint">Fully-qualified ODP GraphQL Endpoint</param>
        /// <param name="query">GraphQL query string to send</param>
        /// <returns>JSON response from ODP</returns>
        private string QuerySegments(string apiKey, string endpoint, string query)
        {
            HttpResponseMessage response = null;
            try
            {
                response = QuerySegmentsAsync(apiKey, endpoint, query).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                _errorHandler.HandleError(ex);

                var statusCode = response == null ? 0 : (int)response.StatusCode;


                _logger.Log(LogLevel.ERROR,
                    $"{AUDIENCE_FETCH_FAILURE_MESSAGE} ({(statusCode == 0 ? Constants.NETWORK_ERROR_REASON : statusCode.ToString())})");

                return default;
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex);

                _logger.Log(LogLevel.ERROR,
                    $"{AUDIENCE_FETCH_FAILURE_MESSAGE} ({Constants.NETWORK_ERROR_REASON})");

                return default;
            }

            return response.Content.ReadAsStringAsync().Result;
        }

        /// <summary>
        /// Asynchronous handler for querying the ODP GraphQL endpoint
        /// </summary>
        /// <param name="apiKey">ODP API Key</param>
        /// <param name="endpoint">Fully-qualified ODP GraphQL endpoint URL</param>
        /// <param name="query">GraphQL query</param>
        /// <returns>JSON response from ODP</returns>
        private async Task<HttpResponseMessage> QuerySegmentsAsync(string apiKey, string endpoint,
            string query
        )
        {
            var request = BuildRequestMessage(apiKey, endpoint, query);

            return await _httpClient.SendAsync(request);
        }

        /// <summary>
        /// Produces the request GraphQL query payload 
        /// </summary>
        /// <param name="apiKey">ODP API Key</param>
        /// <param name="endpoint">Fully-qualified ODP GraphQL endpoint URL</param>
        /// <param name="query">GraphQL query</param>
        /// <returns>Formed HTTP request message ready to be transmitted</returns>
        private static HttpRequestMessage BuildRequestMessage(string apiKey, string endpoint,
            string query
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
                Content = new StringContent(query, Encoding.UTF8,
                    Constants.APPLICATION_JSON_MEDIA_TYPE),
            };

            return request;
        }

        /// <summary>
        /// Parses JSON response
        /// </summary>
        /// <param name="jsonResponse">JSON response from ODP</param>
        /// <returns>Strongly-typed ODP Response object</returns>
        public static Response DeserializeSegmentsFromJson(string jsonResponse)
        {
            return JsonConvert.DeserializeObject<Response>(jsonResponse);
        }
    }
}
