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
using OptimizelySDK.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OptimizelySDK.Odp.Entity
{
    /// <summary>
    /// Handles parameters used in querying ODP segments
    /// </summary>
    public class QuerySegmentsParameters
    {
        public class Builder
        {
            /// <summary>
            /// Builder API key
            /// </summary>
            private string ApiKey { get; set; }

            /// <summary>
            /// Builder ODP endpoint 
            /// </summary>
            private string ApiHost { get; set; }

            /// <summary>
            /// Builder user type key
            /// </summary>
            private string UserKey { get; set; }

            /// <summary>
            /// Builder user key value
            /// </summary>
            private string UserValue { get; set; }

            /// <summary>
            /// Builder audience segments
            /// </summary>
            private List<string> SegmentsToCheck { get; set; }

            /// <summary>
            /// Builder logger to report problems during build
            /// </summary>
            private ILogger Logger { get; }

            public Builder(ILogger logger)
            {
                Logger = logger;
            }

            /// <summary>
            /// Sets the API key for accessing ODP
            /// </summary>
            /// <param name="apiKey">Optimizely Data Platform API key</param>
            /// <returns>Current state of builder</returns>
            public Builder WithApiKey(string apiKey)
            {
                ApiKey = apiKey;
                return this;
            }

            /// <summary>
            /// Set the API endpoint for ODP
            /// </summary>
            /// <param name="apiHost">Fully-qualified URL to ODP endpoint</param>
            /// <returns>Current state of builder</returns>
            public Builder WithApiHost(string apiHost)
            {
                ApiHost = apiHost;
                return this;
            }

            /// <summary>
            /// Sets the user key on which to query ODP
            /// </summary>
            /// <param name="userKey">'vuid' or 'fs_user_id'</param>
            /// <returns>Current state of builder</returns>
            public Builder WithUserKey(string userKey)
            {
                UserKey = userKey;
                return this;
            }

            /// <summary>
            /// Set the user key's value
            /// </summary>
            /// <param name="userValue">Value for user key</param>
            /// <returns>Current state of builder</returns>
            public Builder WithUserValue(string userValue)
            {
                UserValue = userValue;
                return this;
            }

            /// <summary>
            /// Sets the segments to check
            /// </summary>
            /// <param name="segmentsToCheck">List of audience segments to check</param>
            /// <returns>Current state of builder</returns>
            public Builder WithSegmentsToCheck(List<string> segmentsToCheck)
            {
                SegmentsToCheck = segmentsToCheck;
                return this;
            }

            /// <summary>
            /// Validates and constructs the QuerySegmentsParameters object based on provided spec 
            /// </summary>
            /// <returns>QuerySegmentsParameters object</returns>
            public QuerySegmentsParameters Build()
            {
                const string INVALID_MISSING_BUILDER_INPUT_MESSAGE =
                    "QuerySegmentsParameters Builder was provided an invalid";
                
                if (string.IsNullOrWhiteSpace(ApiKey))
                {
                    Logger.Log(LogLevel.ERROR,
                        $"{INVALID_MISSING_BUILDER_INPUT_MESSAGE} API Key");
                    return default;
                }

                if (string.IsNullOrWhiteSpace(ApiHost) ||
                    !Uri.TryCreate(ApiHost, UriKind.Absolute, out Uri _))
                {
                    Logger.Log(LogLevel.ERROR,
                        $"{INVALID_MISSING_BUILDER_INPUT_MESSAGE} API Host");
                    return default;
                }

                if (string.IsNullOrWhiteSpace(UserKey) || !Enum.TryParse(UserKey, out UserKeyType _))
                {
                    Logger.Log(LogLevel.ERROR,
                        $"{INVALID_MISSING_BUILDER_INPUT_MESSAGE} User Key");
                    return default;
                }

                if (string.IsNullOrWhiteSpace(UserValue))
                {
                    Logger.Log(LogLevel.ERROR,
                        $"{INVALID_MISSING_BUILDER_INPUT_MESSAGE} User Value");
                    return default;
                }

                if (SegmentsToCheck.Any(string.IsNullOrWhiteSpace))
                {
                    Logger.Log(LogLevel.ERROR,
                        $"Segments To Check contained a null or empty segment");
                    return default;
                }

                return new QuerySegmentsParameters
                {
                    ApiKey = ApiKey,
                    ApiHost = ApiHost,
                    UserKey = UserKey,
                    UserValue = UserValue,
                    SegmentsToCheck = SegmentsToCheck,
                };
            }

            /// <summary>
            /// Enumeration used during validation of User Key string
            /// </summary>
            private enum UserKeyType
            {
                vuid = 0, fs_user_id = 1
            }
        }

        /// <summary>
        /// Optimizely Data Platform API key
        /// </summary>
        public string ApiKey { get; private set; }

        /// <summary>
        /// Fully-qualified URL to ODP endpoint 
        /// </summary>
        public string ApiHost { get; private set; }

        /// <summary>
        /// 'vuid' or 'fs_user_id' (client device id or fullstack id)
        /// </summary>
        private string UserKey { get; set; }

        /// <summary>
        /// Value for the user key
        /// </summary>
        private string UserValue { get; set; }

        /// <summary>
        /// Audience segments to check for inclusion in the experiment
        /// </summary>
        private List<string> SegmentsToCheck { get; set; }

        private QuerySegmentsParameters() { }

        /// <summary>
        /// Converts the current QuerySegmentsParameters into a GraphQL JSON string
        /// </summary>
        /// <returns>GraphQL JSON payload</returns>
        public string ToGraphQLJson()
        {
            var userValueWithEscapedQuotes = $"\\\"{UserValue}\\\"";
            var segmentsArrayJson =
                JsonConvert.SerializeObject(SegmentsToCheck).Replace("\"", "\\\"");

            var json = new StringBuilder();
            json.Append("{\"query\" : \"query {customer");
            json.Append($"({UserKey} : {userValueWithEscapedQuotes}) ");
            json.Append("{audiences");
            json.Append($"(subset: {segmentsArrayJson})");
            json.Append("{edges {node {name state}}}}}\"}");

            return json.ToString();
        }
    }
}
