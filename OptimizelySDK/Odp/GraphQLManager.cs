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
    public class GraphQLManager : IGraphQLManager
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
        /// Retrieves the audience segments from the Optimizely Data Platform (ODP)
        /// </summary>
        /// <param name="logger">Collect and record events/errors for this GraphQL implementation</param>
        /// <param name="errorHandler">Handler to record exceptions</param>
        /// <param name="httpClient">HttpClient to use to send queries to ODP</param>
        public GraphQLManager(ILogger logger = null, IErrorHandler errorHandler = null,
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
        /// <param name="userKey">'vuid' or 'fs_user_id key'</param>
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

            var endpoint = $"{apiHost}/v3/graphql";
            var query = ToGraphQLJson(userKey.ToString(), userValue, segmentsToCheck);

            var segmentsResponseJson = QuerySegments(apiKey, endpoint, query);
            if (CanBeJsonParsed(segmentsResponseJson))
            {
                _logger.Log(LogLevel.ERROR,
                    $"{AUDIENCE_FETCH_FAILURE_MESSAGE} (network error)");
                return null;
            }

            var parsedSegments = ParseSegmentsResponseJson(segmentsResponseJson);
            if (parsedSegments is null)
            {
                _logger.Log(LogLevel.ERROR,
                    $"{AUDIENCE_FETCH_FAILURE_MESSAGE} (decode error)");
                return null;
            }

            if (parsedSegments.HasErrors)
            {
                var errors = string.Join(";", parsedSegments.Errors.Select(e => e.ToString()));

                _logger.Log(LogLevel.ERROR, $"{AUDIENCE_FETCH_FAILURE_MESSAGE} ({errors})");

                return null;
            }

            if (parsedSegments.Data?.Customer?.Audiences?.Edges is null)
            {
                _logger.Log(LogLevel.ERROR, $"{AUDIENCE_FETCH_FAILURE_MESSAGE} (decode error)");

                return null;
            }

            return parsedSegments.Data.Customer.Audiences.Edges.
                Where(e => e.Node.State == BaseCondition.QUALIFIED).
                Select(e => e.Node.Name).ToArray();
        }

        /// <summary>
        /// Converts the current QuerySegmentsParameters into a GraphQL query string
        /// </summary>
        /// <returns>GraphQL payload</returns>
        private static string ToGraphQLJson(string userKey, string userValue,
            IEnumerable segmentsToCheck
        )
        {
            var userValueWithEscapedQuotes = $"\\\"{userValue}\\\"";
            var segmentsArrayJson =
                JsonConvert.SerializeObject(segmentsToCheck).Replace("\"", "\\\"");

            var json = new StringBuilder();
            json.Append("{\"query\" : \"query {customer");
            json.Append($"({userKey} : {userValueWithEscapedQuotes}) ");
            json.Append("{audiences");
            json.Append($"(subset: {segmentsArrayJson})");
            json.Append("{edges {node {name state}}}}}\"}");

            return json.ToString();
        }

        /// <summary>
        /// Synchronous handler for querying the ODP GraphQL endpoint 
        /// </summary>
        /// <returns>JSON response from ODP</returns>
        private string QuerySegments(string apiKey, string endpoint,
            string query
        )
        {
            HttpResponseMessage response;
            try
            {
                response = QuerySegmentsAsync(apiKey, endpoint, query).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex);
                _logger.Log(LogLevel.ERROR, $"{AUDIENCE_FETCH_FAILURE_MESSAGE} (network error)");
                return default;
            }

            var responseStatusCode = int.Parse(response.StatusCode.ToString());
            if (responseStatusCode >= 400 && responseStatusCode < 600)
            {
                _logger.Log(LogLevel.ERROR,
                    $"{AUDIENCE_FETCH_FAILURE_MESSAGE} ({responseStatusCode})");
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

            var response = await _httpClient.SendAsync(request);

            return response;
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
                        "x-api-key", apiKey
                    },
                },
                Content = new StringContent(query, Encoding.UTF8, "application/json"),
            };

            return request;
        }

        /// <summary>
        /// Ensure a string has content that can be parsed from JSON to an object
        /// </summary>
        /// <param name="jsonToValidate">Value containing possible JSON</param>
        /// <returns>True if content could be interpreted as JSON else False</returns>
        private static bool CanBeJsonParsed(string jsonToValidate)
        {
            return string.IsNullOrWhiteSpace(jsonToValidate);
        }

        /// <summary>
        /// Parses JSON response
        /// </summary>
        /// <param name="jsonResponse">JSON response from ODP</param>
        /// <returns>Strongly-typed ODP Response object</returns>
        public static Response ParseSegmentsResponseJson(string jsonResponse)
        {
            return JsonConvert.DeserializeObject<Response>(jsonResponse);
        }
    }
}
