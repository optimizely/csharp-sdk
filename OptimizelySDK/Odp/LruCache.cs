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

namespace OptimizelySDK.Odp
{
    public class LruCache<T> : ICache<T>
        where T : class
    {
        private readonly ILogger _logger;
        private readonly object _mutex;
        private readonly int _maxSize;
        private readonly TimeSpan _timeout;
        private readonly OrderedDictionary _orderedDictionary;

        public LruCache(int? maxSize = null,
            TimeSpan? timeout = null, ILogger logger = null
        )
        {
            var defaultMaxSize = 10000;
            var defaultTimeout = TimeSpan.FromMinutes(10);

            _mutex = new object();

            if (maxSize is null)
            {
                _maxSize = defaultMaxSize;
            }
            else if (maxSize < 0)
            {
                // Cache is disabled when maxSize = 0
                _maxSize = 0;
            }
            else
            {
                _maxSize = maxSize.Value;
            }

            if (timeout is null)
            {
                _timeout = defaultTimeout;
            }
            else if (timeout?.TotalMilliseconds < 0)
            {
                // ttl = 0 means items never expire.
                _timeout = TimeSpan.Zero;
            }
            else
            {
                _timeout = timeout.Value;
            }

            _logger = logger ?? new DefaultLogger();

            _orderedDictionary = new OrderedDictionary();
        }

        public void Save(string key, T value)
        {
            if (_maxSize == 0)
            {
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
                return default;
            }

            lock (_mutex)
            {
                if (!_orderedDictionary.Contains(key))
                {
                    return default;
                }

                var currentTimestamp = DateTime.Now.MillisecondsSince1970();
                if (_orderedDictionary[key] is ItemWrapper item)
                {
                    if (_timeout == TimeSpan.Zero ||
                        (currentTimestamp - item.Timestamp < _timeout.TotalMilliseconds))
                    {
                        _orderedDictionary.Remove(key);
                        _orderedDictionary.Add(key, item);

                        return item.Value;
                    }
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
                Timestamp = DateTime.Now.MillisecondsSince1970();
            }
        }

        public OrderedDictionary _readCurrentCache()
        {
            _logger.Log(LogLevel.WARN, "_readCurrentCache used for non-testing purpose");
            return _orderedDictionary;
        }
    }
}
