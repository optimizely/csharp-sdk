/*
 * Copyright 2026, Optimizely
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

namespace OptimizelySDK.Event
{
    /// <summary>
    ///     Configuration constants for retrying event dispatch requests with exponential backoff.
    ///     Per ticket requirements:
    ///     - Max 3 total attempts (1 initial + 2 retries)
    ///     - Start at 200ms, exponentially grow to max 1 second
    /// </summary>
    public static class EventRetryConfig
    {
        /// <summary>
        ///     Maximum number of retry attempts after the initial attempt fails.
        ///     Total attempts = 1 (initial) + MAX_RETRIES = 3
        /// </summary>
        public const int MAX_RETRIES = 2;

        /// <summary>
        ///     Initial backoff delay in milliseconds before the first retry.
        /// </summary>
        public const int INITIAL_BACKOFF_MS = 200;

        /// <summary>
        ///     Maximum backoff delay in milliseconds between retries.
        /// </summary>
        public const int MAX_BACKOFF_MS = 1000;

        /// <summary>
        ///     Multiplier applied to the backoff delay after each retry.
        /// </summary>
        public const double BACKOFF_MULTIPLIER = 2.0;
    }
}
