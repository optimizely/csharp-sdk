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
using System;
using System.Collections.Specialized;

namespace OptimizelySDK.Odp
{
    public class LruCache<T> : ILruCache<T>
    {
        public const int DEFAULT_MAX_SIZE = 10000;
        public const int DEFAULT_TIMEOUT_SECONDS = 600;

        private readonly ILogger _logger;
        private readonly object _mutex = new object();
        private readonly int _maxSize;
        private readonly long _timeoutMilliseconds;
        private readonly OrderedDictionary _orderedDictionary = new OrderedDictionary();

        public LruCache() : this(DEFAULT_MAX_SIZE, DEFAULT_TIMEOUT_SECONDS, null) { }

        public LruCache(int maxSize, int timeoutSeconds, ILogger logger = null)
        {
            _maxSize = maxSize < 0 ? default : maxSize;
            _timeoutMilliseconds = (timeoutSeconds < 0) ? 0 : (timeoutSeconds * 1000L);
            _logger = logger ?? new DefaultLogger();
        }

        public void Save(string key, T value)
        {
            if (_maxSize == 0)
            {
                // Cache is disabled when maxSize = 0
                return;
            }

            lock (_mutex)
            {
                if (_orderedDictionary.Contains(key))
                {
                    _orderedDictionary.Remove(key);
                }

                if (_orderedDictionary.Count >= _maxSize)
                {
                    _orderedDictionary.RemoveAt(0);
                }

                _orderedDictionary.Add(key, new ItemWrapper(value));
            }
        }

        public T Lookup(string key)
        {
            if (_maxSize == 0)
            {
                // Cache is disabled when maxSize = 0
                return default;
            }

            lock (_mutex)
            {
                if (!_orderedDictionary.Contains(key))
                {
                    return default;
                }

                var item = (ItemWrapper)_orderedDictionary[key];
                var nowMs = DateTime.Now.ToUnixTimeMilliseconds();

                // ttl = 0 means items never expire.
                if (_timeoutMilliseconds == 0 || (nowMs - item.Timestamp < _timeoutMilliseconds))
                {
                    _orderedDictionary.Remove(key);
                    _orderedDictionary.Add(key, item);

                    return item.Value;
                }

                _orderedDictionary.Remove(key);

                return default;
            }
        }

        public void Reset()
        {
            lock (_mutex)
            {
                _orderedDictionary.Clear();
            }
        }

        private class ItemWrapper
        {
            public readonly T Value;
            public readonly long Timestamp;

            public ItemWrapper(T value)
            {
                Value = value;
                Timestamp = DateTime.Now.ToUnixTimeMilliseconds();
            }
        }

        public OrderedDictionary ReadCurrentCache()
        {
            lock (_mutex)
            {
                return _orderedDictionary;
            }
        }
    }
}
