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

namespace OptimizelySDK.Odp.Entity
{
    public class OptimizelySdkSettings
    {
        /// <summary>
        /// The maximum size of audience segments cache - cache is disabled if this is set to zero
        /// </summary>
        public int SegmentsCacheSize { get; set; }

        /// <summary>
        /// The timeout in seconds of audience segments cache - timeout is disabled if this is set to zero
        /// </summary>
        public int SegmentsCacheTimeout { get; set; }

        /// <summary>
        /// ODP features are disabled if this is set to true.
        /// </summary>
        public bool DisableOdp { get; set; }

        /// <summary>
        /// Optimizely SDK Settings
        /// </summary>
        /// <param name="segmentsCacheSize">The maximum size of audience segments cache (optional. default = 100). Set to zero to disable caching</param>
        /// <param name="segmentsCacheTimeout">The timeout in seconds of audience segments cache (optional. default = 600). Set to zero to disable timeout</param>
        /// <param name="disableOdp">Set this flag to true (default = false) to disable ODP features</param>
        public OptimizelySdkSettings(int segmentsCacheSize = 100, int segmentsCacheTimeout = 600,
            bool disableOdp = false
        )
        {
            SegmentsCacheSize = segmentsCacheSize;
            SegmentsCacheTimeout = segmentsCacheTimeout;
            DisableOdp = disableOdp;
        }
    }
}
