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
using OptimizelySDK.Utils;

namespace OptimizelySDK.Cmab
{
    /// <summary>
    ///     Configuration options for CMAB (Contextual Multi-Armed Bandit) functionality.
    /// </summary>
    public class CmabConfig
    {
        /// <summary>
        ///     Initializes a new instance of the CmabConfig class with default cache settings.
        /// </summary>
        /// <param name="cacheSize">Maximum number of entries in the cache. Default is 1000.</param>
        /// <param name="cacheTtl">Time-to-live for cache entries. Default is 30 minutes.</param>
        public CmabConfig(int? cacheSize = null, TimeSpan? cacheTtl = null)
        {
            CacheSize = cacheSize;
            CacheTtl = cacheTtl;
            CustomCache = null;
        }

        /// <summary>
        ///     Initializes a new instance of the CmabConfig class with a custom cache implementation.
        /// </summary>
        /// <param name="customCache">Custom cache implementation for CMAB decisions.</param>
        public CmabConfig(ICacheWithRemove<CmabCacheEntry> customCache)
        {
            CustomCache = customCache ?? throw new ArgumentNullException(nameof(customCache));
            CacheSize = null;
            CacheTtl = null;
        }

        /// <summary>
        ///     Gets the maximum number of entries in the CMAB cache.
        ///     If null, the default value (1000) will be used.
        /// </summary>
        public int? CacheSize { get; }

        /// <summary>
        ///     Gets the time-to-live for CMAB cache entries.
        ///     If null, the default value (30 minutes) will be used.
        /// </summary>
        public TimeSpan? CacheTtl { get; }

        /// <summary>
        ///     Gets the custom cache implementation for CMAB decisions.
        ///     If provided, CacheSize and CacheTtl will be ignored.
        /// </summary>
        public ICacheWithRemove<CmabCacheEntry> CustomCache { get; }
    }
}
