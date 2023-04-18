/* 
 * Copyright 2022-2023 Optimizely
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

using System;

// ReSharper disable InconsistentNaming

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

        /// <summary>
        /// Default message to log when ODP is not ready or integrated
        /// </summary>
        public const string ODP_NOT_INTEGRATED_MESSAGE = "ODP is not integrated.";

        /// <summary>
        /// Default message to log when ODP is not enabled
        /// </summary>
        public const string ODP_NOT_ENABLED_MESSAGE = "ODP is not enabled.";

        /// <summary>
        /// Default message for when ODP is already running
        /// </summary>
        public const string ODP_ALREADY_STARTED = "ODP is already started.";

        /// <summary>
        /// Default message to log when an ODP Event contains invalid data
        /// </summary>
        public const string ODP_INVALID_DATA_MESSAGE = "ODP event send failed.";

        /// <summary>
        /// Default message to log when an ODP Event contains invalid action
        /// </summary>
        public const string ODP_INVALID_ACTION_MESSAGE = "ODP action is not valid (cannot be empty).";

        /// <summary>
        /// Default message to log when sending ODP event fails
        /// </summary>
        public const string ODP_SEND_FAILURE_MESSAGE = "ODP event send failed";

        /// <summary>
        /// Maximum attempts to retry ODP communication
        /// </summary>
        public const int MAX_RETRIES = 3;

        /// <summary>
        /// Default ODP batch size
        /// </summary>
        public const int DEFAULT_BATCH_SIZE = 10;

        /// <summary>
        /// Default maximum ODP event queue capacity
        /// </summary>
        public const int DEFAULT_QUEUE_CAPACITY = 10000;

        /// <summary>
        /// Server-side event type to record in ODP
        /// </summary>
        public const string ODP_EVENT_TYPE = "fullstack";

        /// <summary>
        /// Default interval to flush ODP event queue
        /// </summary>
        public static readonly TimeSpan DEFAULT_FLUSH_INTERVAL = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Default amount of time to wait for ODP response
        /// </summary>
        public static readonly TimeSpan DEFAULT_TIMEOUT_INTERVAL = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Default maximum number of elements to cache
        /// </summary>
        public const int DEFAULT_MAX_CACHE_SIZE = 10000;

        /// <summary>
        /// Default number of seconds to cache
        /// </summary>
        public const int DEFAULT_CACHE_SECONDS = 600;

        /// <summary>
        /// Type of ODP key used for fetching segments & sending events
        /// </summary>
        public const string FS_USER_ID = "fs_user_id";

        /// <summary>
        /// Alternate form of ODP key that is auto-converted to FS_USER_ID
        /// </summary>
        public const string FS_USER_ID_ALIAS = "fs-user-id";

        /// <summary>
        /// Unique identifier used for ODP events
        /// </summary>
        public const string IDEMPOTENCE_ID = "idempotence_id";
    }
}
