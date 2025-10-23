/*
 * Copyright 2025, Optimizely
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

using System;

namespace OptimizelySDK.Cmab
{
    internal static class CmabConstants
    {
        public const string PREDICTION_URL = "https://prediction.cmab.optimizely.com/predict";
        public const int DEFAULT_CACHE_SIZE = 10_000;
        public const string CONTENT_TYPE = "application/json";

        public const string ERROR_FETCH_FAILED_FMT = "CMAB decision fetch failed with status: {0}";
        public const string ERROR_INVALID_RESPONSE = "Invalid CMAB fetch response";
        public const string EXHAUST_RETRY_MESSAGE = "Exhausted all retries for CMAB request";

        public const string USER_NOT_IN_CMAB_EXPERIMENT =
            "User [{0}] not in CMAB experiment [{1}] due to traffic allocation.";

        public const string CMAB_FETCH_FAILED =
            "Failed to fetch CMAB decision for experiment [{0}].";

        public const string CMAB_DECISION_FETCHED =
            "CMAB decision fetched for user [{0}] in experiment [{1}].";

        public const string CMAB_EXPERIMENT_NOT_PROPERLY_CONFIGURED =
            "CMAB experiment [{0}] is not properly configured.";

        public static readonly TimeSpan MAX_TIMEOUT = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan DEFAULT_CACHE_TTL = TimeSpan.FromMinutes(10);
    }
}
