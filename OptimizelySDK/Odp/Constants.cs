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

namespace OptimizelySDK.Odp
{
    public static class Constants
    {
        /// <summary>
        /// Specific key for designating the ODP API public key 
        /// </summary>
        public const string HEADER_API_KEY = "x-api-key";

        /// <summary>
        /// Media type for json requests
        /// </summary>
        public const string APPLICATION_JSON_MEDIA_TYPE = "application/json";

        /// <summary>
        /// Path to ODP REST events API
        /// </summary>
        public const string ODP_EVENTS_API_ENDPOINT_PATH = "/v3/events";

        /// <summary>
        /// Path to ODP GraphQL API
        /// </summary>
        public const string ODP_GRAPHQL_API_ENDPOINT_PATH = "/v3/graphql";

        /// <summary>
        /// Default message when numeric HTTP status code is not available
        /// </summary>
        public const string NETWORK_ERROR_REASON = "network error";
    }
}
