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
using System.Collections.Generic;

namespace OptimizelySDK.Odp
{
    public class LruCache<T> : ICache<T>
        where T : class
    {
        /// <summary>
        /// Default maximum number of elements to store
        /// </summary>
        private const int DEFAULT_MAX_SIZE = 10000;

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
        /// Indication that timeout is disabled and objects should remain in cache indefinitely
        /// </summary>
        private readonly TimeSpan _timeoutDisabled = TimeSpan.Zero;
        
        /// <summary>
        /// Implementation used for recording LRU events or errors 
        /// </summary>
        private readonly ILogger _logger;
        
        /// <summary>
        /// Indexed data held in the cache 
        /// </summary>
        private readonly Dictionary<string, (LinkedListNode<string> node, ItemWrapper value)> _cache;
        
        /// <summary>
        /// Ordered list of objects being held in the cache 
        /// </summary>
        private readonly LinkedList<string> _list;

        /// <summary>
        /// A Least Recently Used in-memory cache
        /// </summary>
        /// <param name="maxSize">Maximum number of elements to allow in the cache</param>
        /// <param name="itemTimeout">Timeout or time to live for each item</param>
        /// <param name="logger">Implementation used for recording LRU events or errors</param>
        public LruCache(int maxSize = DEFAULT_MAX_SIZE,
            TimeSpan? itemTimeout = default, ILogger logger = null
        )
        {
            const int CACHE_DISABLED = 0;

            _mutex = new object();

            _maxSize = Math.Max(CACHE_DISABLED, maxSize);

            _timeout = TimeSpan.FromTicks(Math.Max(_timeoutDisabled.Ticks,
                (itemTimeout ?? TimeSpan.FromMinutes(10)).Ticks));
            
            _logger = logger ?? new DefaultLogger();

            _cache =
                new Dictionary<string, (LinkedListNode<string> node, ItemWrapper value)>(_maxSize);
            _list = new LinkedList<string>();
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
                return;
            }

            lock (_mutex)
            {
                if (_cache.ContainsKey(key))
                {
                    (LinkedListNode<string> node, ItemWrapper item) = _cache[key];
                    _list.Remove(node);
                    _list.AddFirst(node);
                    _cache[key] = (node, item);
                }
                else
                {
                    if (_cache.Count >= _maxSize)
                    {
                        var removeKey = _list.Last.Value;
                        _cache.Remove(removeKey);
                        _list.RemoveLast();
                    }

                    _cache.Add(key, (_list.AddFirst(key), new ItemWrapper(value)));
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
                return default;
            }

            lock (_mutex)
            {
                if (!_cache.ContainsKey(key))
                {
                    return default;
                }

                (LinkedListNode<string> node, ItemWrapper item) = _cache[key];

                var currentTimestamp = DateTime.Now.MillisecondsSince1970();

                if (_timeout == _timeoutDisabled ||
                    (currentTimestamp - item.CreationTimestamp < _timeout.TotalMilliseconds))
                {
                    _list.Remove(node);
                    _list.AddFirst(node);

                    _cache[key] = (node, item);

                    return item.Value;
                }

                _cache.Remove(key);
                _list.Remove(node);

                return default;
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
                _list.Clear();
            }
        }

        /// <summary>
        /// Wrapping class around a generic value stored in the cache
        /// </summary>
        private class ItemWrapper
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
        public LinkedList<string> _readCurrentCacheKeys()
        {
            _logger.Log(LogLevel.WARN, "_readCurrentCacheKeys used for non-testing purpose");
            return _list;
        }
    }
}
