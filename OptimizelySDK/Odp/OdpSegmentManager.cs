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
        private readonly IOdpConfig _odpConfig;

        /// <summary>
        /// Cached segments 
        /// </summary>
        private readonly ICache<List<string>> _segmentsCache;

        public OdpSegmentManager(IOdpConfig odpConfig, IOdpSegmentApiManager apiManager,
            int cacheSize = Constants.DEFAULT_MAX_CACHE_SIZE, TimeSpan? itemTimeout = null,
            ILogger logger = null, ICache<List<string>> cache = null
        )
        {
            _apiManager = apiManager;
            _odpConfig = odpConfig;
            _logger = logger ?? new DefaultLogger();

            var timeout = itemTimeout ?? TimeSpan.FromMinutes(Constants.DEFAULT_CACHE_MINUTES);
            if (timeout < TimeSpan.Zero)
            {
                _logger.Log(LogLevel.WARN,
                    "Negative item timeout provided. Items will not expire in cache.");
                timeout = TimeSpan.Zero;
            }

            _segmentsCache = cache ?? new LruCache<List<string>>(cacheSize, timeout, logger);
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
            if (!_odpConfig.IsReady())
            {
                _logger.Log(LogLevel.ERROR, "Audience segments fetch failed (ODP is not enabled)");
                return new List<string>();
            }

            if (!_odpConfig.HasSegments())
            {
                _logger.Log(LogLevel.DEBUG,
                    "No Segments are used in the project, Not Fetching segments. Returning empty list");
                return new List<string>();
            }

            options = options ?? new List<OdpSegmentOption>();

            List<string> qualifiedSegments;
            var cacheKey = GetCacheKey(OdpUserKeyType.FS_USER_ID.ToString().ToLower(), fsUserId);

            if (options.Contains(OdpSegmentOption.ResetCache))
            {
                _segmentsCache.Reset();
            }
            else if (!options.Contains(OdpSegmentOption.IgnoreCache))
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
                    _odpConfig.SegmentsToCheck)?.ToList();

            if (!options.Contains(OdpSegmentOption.IgnoreCache))
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
    }
}
