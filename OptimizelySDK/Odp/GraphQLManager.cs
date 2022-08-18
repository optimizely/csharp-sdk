﻿/* 
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
using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Client;
using OptimizelySDK.Odp.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimizelySDK.Odp
{
    /// <summary>
    /// Manager for communicating with the Optimizely Data Platform GraphQL endpoint
    /// </summary>
    public class GraphQLManager : IGraphQLManager
    {
        private readonly ILogger _logger;
        private readonly IOdpClient _odpClient;

        public GraphQLManager(ILogger logger = null, IOdpClient client = null)
        {
            _logger = logger ?? new DefaultLogger();
            _odpClient = client ?? new OdpClient(_logger);
        }

        /// <summary>
        /// Retrieves the audience segments from ODP
        /// </summary>
        /// <param name="apiKey">Public key use</param>
        /// <param name="apiHost">Fully-qualified URL of ODP</param>
        /// <param name="userKey">vuid or fs_user_id key</param>
        /// <param name="userValue">Associated value to query for the user key</param>
        /// <param name="segmentToCheck">Audience segments to check for experiment inclusion</param>
        /// <returns>Array of audience segments</returns>
        public string[] FetchSegments(string apiKey, string apiHost, string userKey,
            string userValue, List<string> segmentToCheck
        )
        {
            var parameters = new QuerySegmentsParameters
            {
                ApiKey = apiKey,
                ApiHost = apiHost,
                UserKey = userKey,
                UserValue = userValue,
                SegmentToCheck = segmentToCheck
            };

            var segmentsResponseJson = _odpClient.QuerySegments(parameters);
            if (string.IsNullOrWhiteSpace(segmentsResponseJson))
            {
                return new string[0];
            }

            var parsedSegments = ParseSegmentsResponseJson(segmentsResponseJson);

            if (parsedSegments.HasErrors)
            {
                var errors = string.Join(";", parsedSegments.Errors.Select(e => e.ToString()));

                _logger.Log(LogLevel.WARN, $"Audience segments fetch failed ({errors})");

                return new string[0];
            }

            if (parsedSegments?.Data?.Customer?.Audiences?.Edges is null)
            {
                _logger.Log(LogLevel.ERROR, "Audience segments fetch failed (decode error)");

                return new string[0];
            }

            return parsedSegments.Data?.Customer?.Audiences?.Edges?.
                Where(e => e.Node.State == BaseCondition.QUALIFIED).
                Select(e => e.Node.Name).ToArray();
        }

        /// <summary>
        /// Parses JSON response
        /// </summary>
        /// <param name="jsonResponse">JSON response from ODP</param>
        /// <returns>Strongly-typed ODP Response object</returns>
        public static Response ParseSegmentsResponseJson(string jsonResponse)
        {
            return string.IsNullOrWhiteSpace(jsonResponse) ?
                default :
                JsonConvert.DeserializeObject<Response>(jsonResponse);
        }
    }
}