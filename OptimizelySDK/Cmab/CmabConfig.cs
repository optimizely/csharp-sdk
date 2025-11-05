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
using OptimizelySDK.Cmab;
using OptimizelySDK.Utils;

namespace OptimizelySDK
{
    /// <summary>
    ///     Configuration options for CMAB (Contextual Multi-Armed Bandit) functionality.
    /// </summary>
    public class CmabConfig
    {
        /// <summary>
        ///     Gets or sets the maximum number of entries in the CMAB cache.
        ///     If null, the default value (1000) will be used.
        /// </summary>
        public int? CacheSize { get; private set; }

        /// <summary>
        ///     Gets or sets the time-to-live for CMAB cache entries.
        ///     If null, the default value (30 minutes) will be used.
        /// </summary>
        public TimeSpan? CacheTtl { get; private set; }

        /// <summary>
        ///     Gets or sets the custom cache implementation for CMAB decisions.
        ///     If provided, CacheSize and CacheTtl will be ignored.
        /// </summary>
        public ICacheWithRemove<CmabCacheEntry> Cache { get; private set; }

        /// <summary>
        ///     Gets or sets the prediction endpoint URL template for CMAB requests.
        /// </summary>
        public string PredictionEndpointTemplate { get; private set; } = CmabConstants.DEFAULT_PREDICTION_URL_TEMPLATE;

        /// <summary>
        ///     Sets the maximum number of entries in the CMAB cache.
        /// </summary>
        /// <param name="cacheSize">Maximum number of entries in the cache.</param>
        /// <returns>This CmabConfig instance for method chaining.</returns>
        public CmabConfig SetCacheSize(int cacheSize)
        {
            CacheSize = cacheSize;
            return this;
        }

        /// <summary>
        ///     Sets the time-to-live for CMAB cache entries.
        /// </summary>
        /// <param name="cacheTtl">Time-to-live for cache entries.</param>
        /// <returns>CmabConfig instance</returns>
        public CmabConfig SetCacheTtl(TimeSpan cacheTtl)
        {
            CacheTtl = cacheTtl;
            return this;
        }

        /// <summary>
        ///     Sets a custom cache implementation for CMAB decisions.
        ///     When set, CacheSize and CacheTtl will be ignored.
        /// </summary>
        /// <param name="cache">Custom cache implementation for CMAB decisions.</param>
        /// <returns>CmabConfig Instance</returns>
        public CmabConfig SetCache(ICacheWithRemove<CmabCacheEntry> cache)
        {
            Cache = cache ?? throw new ArgumentNullException(nameof(cache));
            return this;
        }

        /// <summary>
        ///     Sets the prediction endpoint URL template for CMAB requests.
        /// </summary>
        /// <param name="template">The URL template</param>
        /// <returns>CmabConfig Instance</returns>
        public CmabConfig SetPredictionEndpointTemplate(string template)
        {
            PredictionEndpointTemplate = template;
            return this;
        }
    }
}
