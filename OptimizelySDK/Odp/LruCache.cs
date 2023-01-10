/* 
 * Copyright 2022, Optimizely
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
using OptimizelySDK.Utils;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace OptimizelySDK.Odp
{
    public class LruCache<T> : ICache<T>
        where T : class
    {
        /// <summary>
        /// The maximum number of elements that should be stored
        /// </summary>
        private readonly int _maxSize;

        /// <summary>
        /// An object for obtaining a mutually exclusive lock for thread safety
        /// </summary>
        private readonly object _mutex;

        /// <summary>
        /// The maximum age of an object in the cache
        /// </summary>
        private readonly TimeSpan _timeout;

        /// <summary>
        /// Implementation used for recording LRU events or errors 
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Indexed data held in the cache 
        /// </summary>
        private readonly OrderedDictionary _cache;

        /// <summary>
        /// A Least Recently Used in-memory cache
        /// </summary>
        /// <param name="maxSize">Maximum number of elements to allow in the cache</param>
        /// <param name="itemTimeout">Timeout or time to live for each item</param>
        /// <param name="logger">Implementation used for recording LRU events or errors</param>
        public LruCache(int? maxSize = null,
            TimeSpan? itemTimeout = null,
            ILogger logger = null
        )
        {
            //TODO: Please add a condition to check minimum value of maxSize as well. 
            _mutex = new object();

            _maxSize = Math.Max(0, maxSize ?? Constants.DEFAULT_MAX_CACHE_SIZE);

            _logger = logger ?? new DefaultLogger();

            _timeout = itemTimeout ?? TimeSpan.FromSeconds(Constants.DEFAULT_CACHE_SECONDS);
            if (_timeout < TimeSpan.Zero)
            {
                _logger.Log(LogLevel.WARN,
                    "Negative item timeout provided. Items will not expire in cache.");
                _timeout = TimeSpan.Zero;
            }

            _cache = new OrderedDictionary(_maxSize);
        }

        /// <summary>
        /// Saves a new element into the cache
        /// </summary>
        /// <param name="key">Key of the element used for future retrieval</param>
        /// <param name="value">Strongly-typed value to store against the key</param>
        public void Save(string key, T value)
        {
            if (_maxSize == 0)
            {
                _logger.Log(LogLevel.WARN,
                    "Unable to Save(). LRU Cache is disabled. Set maxSize > 0 to enable.");
                return;
            }

            lock (_mutex)
            {
                if (_cache.Contains(key))
                {
                    var item = _cache[key];
                    _cache.Remove(key);
                    _cache.Insert(0, key, item);
                }
                else
                {
                    if (_cache.Count >= _maxSize)
                    {
                        _cache.RemoveAt(_cache.Count - 1);
                    }

                    _cache.Insert(0, key, new ItemWrapper(value));
                }
            }
        }

        /// <summary>
        /// Retrieves an element from the cache, reordering the elements
        /// </summary>
        /// <param name="key">Key of element to find and retrieve</param>
        /// <returns>Strongly-typed value from the cache</returns>
        public T Lookup(string key)
        {
            if (_maxSize == 0)
            {
                _logger.Log(LogLevel.WARN,
                    "Unable to Lookup(). LRU Cache is disabled. Set maxSize > 0 to enable.");
                return default;
            }

            lock (_mutex)
            {
                if (!_cache.Contains(key))
                {
                    return default;
                }

                var currentTimestamp = DateTime.Now.MillisecondsSince1970();

                var item = _cache[key] as ItemWrapper;
                var itemReturn = default(T);
                if (item != null && (_timeout == TimeSpan.Zero ||
                                     currentTimestamp - item.CreationTimestamp <
                                     _timeout.TotalMilliseconds))
                {
                    _cache.Remove(key);
                    _cache.Insert(0, key, item);

                    itemReturn = item.Value;
                }
                else
                {
                    _cache.Remove(key);
                }

                return itemReturn;
            }
        }

        /// <summary>
        /// Clears all the elements from the cache 
        /// </summary>
        public void Reset()
        {
            lock (_mutex)
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// Wrapping class around a generic value stored in the cache
        /// </summary>
        public class ItemWrapper
        {
            /// <summary>
            /// Value of the item
            /// </summary>
            public readonly T Value;

            /// <summary>
            /// Unix timestamp of when the item was added  
            /// </summary>
            public readonly long CreationTimestamp;

            /// <summary>
            /// Initialize the wrapper
            /// </summary>
            /// <param name="value">Item to be stored</param>
            public ItemWrapper(T value)
            {
                Value = value;
                CreationTimestamp = DateTime.Now.MillisecondsSince1970();
            }
        }

        /// <summary>
        /// Read the current cache index/linked list for unit testing
        /// </summary>
        /// <returns></returns>
        public string[] _readCurrentCacheKeys()
        {
            _logger.Log(LogLevel.WARN, "_readCurrentCacheKeys used for non-testing purpose");

            string[] cacheKeys;
            lock (_mutex)
            {
                cacheKeys = _cache.Keys.Cast<string>().ToArray();
            }

            return cacheKeys;
        }
    }
}
