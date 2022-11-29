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

using System.Collections.Generic;

namespace OptimizelySDK.Odp
{
    /// <summary>
    /// Interface to schedule connections to ODP for audience segmentation and caches the results.
    /// </summary>
    public interface IOdpSegmentManager
    {
        /// <summary>
        /// Attempts to fetch and return a list of a user's qualified segments from the local segments cache.
        /// If no cached data exists for the target user, this fetches and caches data from the ODP server instead.
        /// </summary>
        /// <param name="fsUserId">The FS User ID identifying the user</param>
        /// <param name="options">An array of OptimizelySegmentOption used to ignore and/or reset the cache.</param>
        /// <returns>Qualified segments for the user from the cache or the ODP server if the cache is empty.</returns>
        List<string> FetchQualifiedSegments(string fsUserId, List<OdpSegmentOption> options = null);

        /// <summary>
        /// Update the ODP configuration settings being used by the Segment Manager
        /// </summary>
        /// <param name="odpConfig">New ODP Configuration to apply</param>
        void UpdateSettings(OdpConfig odpConfig);

        /// <summary>
        /// Reset/clear the segments cache
        /// </summary>
        void ResetCache();
    }
}
