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

using OptimizelySDK.Logger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimizelySDK.Odp
{
    /// <summary>
    /// Concrete implementation that schedules connections to ODP for audience segmentation
    /// and caches the results.
    /// </summary>
    public class OdpSegmentManager : IOdpSegmentManager
    {
        /// <summary>
        /// Logger used to record messages that occur within the ODP client
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// ODP segment API manager to communicate with ODP 
        /// </summary>
        private readonly IOdpSegmentApiManager _apiManager;

        /// <summary>
        /// ODP configuration containing the connection parameters
        /// </summary>
        private volatile OdpConfig _odpConfig;

        /// <summary>
        /// Cached segments 
        /// </summary>
        private readonly ICache<List<string>> _segmentsCache;

        public OdpSegmentManager(IOdpSegmentApiManager apiManager,
            ICache<List<string>> cache = null,
            ILogger logger = null
        )
        {
            _apiManager = apiManager;
            _logger = logger ?? new DefaultLogger();

            _segmentsCache =
                cache ?? new LruCache<List<string>>(Constants.DEFAULT_MAX_CACHE_SIZE,
                    TimeSpan.FromSeconds(Constants.DEFAULT_CACHE_SECONDS), logger);
        }

        public OdpSegmentManager(IOdpSegmentApiManager apiManager,
            int? cacheSize = null,
            TimeSpan? itemTimeout = null,
            ILogger logger = null
        )
        {
            _apiManager = apiManager;
            _logger = logger ?? new DefaultLogger();

            _segmentsCache = new LruCache<List<string>>(cacheSize, itemTimeout, logger);
        }

        /// <summary>
        /// Attempts to fetch and return a list of a user's qualified segments from the local segments cache.
        /// If no cached data exists for the target user, this fetches and caches data from the ODP server instead.
        /// </summary>
        /// <param name="fsUserId">The FS User ID identifying the user</param>
        /// <param name="options">An array of OptimizelySegmentOption used to ignore and/or reset the cache.</param>
        /// <returns>Qualified segments for the user from the cache or the ODP server if the cache is empty.</returns>
        public List<string> FetchQualifiedSegments(string fsUserId,
            List<OdpSegmentOption> options = null
        )
        {
            if (_odpConfig == null || !_odpConfig.IsReady())
            {
                _logger.Log(LogLevel.WARN, Constants.ODP_NOT_INTEGRATED_MESSAGE);
                return null;
            }

            if (!_odpConfig.HasSegments())
            {
                _logger.Log(LogLevel.DEBUG,
                    "No Segments are used in the project, Not Fetching segments. Returning empty list.");
                return new List<string>();
            }

            options = options ?? new List<OdpSegmentOption>();

            List<string> qualifiedSegments;
            var cacheKey = GetCacheKey(OdpUserKeyType.FS_USER_ID.ToString().ToLower(), fsUserId);

            if (options.Contains(OdpSegmentOption.RESET_CACHE))
            {
                _segmentsCache.Reset();
            }

            if (!options.Contains(OdpSegmentOption.IGNORE_CACHE))
            {
                qualifiedSegments = _segmentsCache.Lookup(cacheKey);
                if (qualifiedSegments != null)
                {
                    _logger.Log(LogLevel.DEBUG, "ODP Cache Hit. Returning segments from Cache.");
                    return qualifiedSegments;
                }
            }

            _logger.Log(LogLevel.DEBUG, "ODP Cache Miss. Making a call to ODP Server.");

            qualifiedSegments = _apiManager.FetchSegments(
                    _odpConfig.ApiKey,
                    _odpConfig.ApiHost,
                    OdpUserKeyType.FS_USER_ID,
                    fsUserId,
                    _odpConfig.SegmentsToCheck)?.
                ToList();

            if (qualifiedSegments != null && !options.Contains(OdpSegmentOption.IGNORE_CACHE))
            {
                _segmentsCache.Save(cacheKey, qualifiedSegments);
            }

            return qualifiedSegments;
        }

        /// <summary>
        /// Creates a key used to identify which user fetchQualifiedSegments should lookup and save to in the segments cache
        /// </summary>
        /// <param name="userKey">Always 'fs_user_id' (parameter for consistency with other SDKs)</param>
        /// <param name="userValue">Arbitrary string representing the full stack user ID</param>
        /// <returns>Concatenates inputs and returns the string "{userKey}-$-{userValue}"</returns>
        private static string GetCacheKey(string userKey, string userValue)
        {
            return $"{userKey}-$-{userValue}";
        }

        /// <summary>
        /// Update the ODP configuration settings being used by the Segment Manager
        /// </summary>
        /// <param name="odpConfig">New ODP Configuration to apply</param>
        public void UpdateSettings(OdpConfig odpConfig)
        {
            _odpConfig = odpConfig;
        }

        /// <summary>
        /// Reset/clear the segments cache
        /// </summary>
        public void ResetCache()
        {
            _segmentsCache.Reset();
        }

        /// <summary>
        /// For Testing Only: Retrieve the current segment cache
        /// </summary>
        internal ICache<List<string>> SegmentsCacheForTesting => _segmentsCache;
    }
}
