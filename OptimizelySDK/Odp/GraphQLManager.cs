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
using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Client;
using OptimizelySDK.Odp.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OptimizelySDK.Odp
{
    public class GraphQLManager : IGraphQLManager
    {
        private readonly ILogger Logger;
        private readonly IOdpClient OdpClient;

        public GraphQLManager(ILogger logger = null, IOdpClient client = null)
        {
            Logger = logger ?? new DefaultLogger();
            OdpClient = client ?? new OdpClient(Logger);
        }

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

            string segmentsResponseJson;
            try
            {
                segmentsResponseJson = OdpClient.QuerySegments(parameters);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.WARN, ex.Message);

                return new string[0];
            }

            var response = ParseSegmentsResponseJson(segmentsResponseJson);

            if (response is null)
            {
                Logger.Log(LogLevel.WARN, "Error while parsing response.");

                return new string[0];
            }

            if (response.HasErrors)
            {
                var message = string.Join(";", response.Errors.Select(e => e.ToString()));
                Logger.Log(LogLevel.WARN, message);

                return new string[0];
            }

            return response.Data?.Customer?.Audiences?.Edges?.
                Where(e => e.Node.State == BaseCondition.QUALIFIED).
                Select(e => e.Node.Name).ToArray();
        }

        public Response ParseSegmentsResponseJson(string jsonResponse)
        {
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                return default;
            }
            
            var json = Regex.Replace(jsonResponse, @"\s+", string.Empty);

            return JsonConvert.DeserializeObject<Response>(json);
        }
    }
}
