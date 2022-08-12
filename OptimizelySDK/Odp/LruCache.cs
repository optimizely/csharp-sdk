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
        private readonly int _maxSize;
        private readonly object _mutex;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _timeoutDisabled = TimeSpan.Zero;
        private readonly ILogger _logger;
        private readonly Dictionary<string, (LinkedListNode<string> node, ItemWrapper value)> _cache;
        private readonly LinkedList<string> _list;

        public LruCache(int? maxSize = null,
            TimeSpan? itemTimeout = null, ILogger logger = null
        )
        {
            const int DEFAULT_MAX_SIZE = 10000;
            const int CACHE_DISABLED = 0;
            var defaultTimeout = TimeSpan.FromMinutes(10);

            _mutex = new object();

            if (maxSize is null)
            {
                _maxSize = DEFAULT_MAX_SIZE;
            }
            else if (maxSize < 0)
            {
                _maxSize = CACHE_DISABLED;
            }
            else
            {
                _maxSize = maxSize.Value;
            }

            if (itemTimeout is null)
            {
                _timeout = defaultTimeout;
            }
            else if (itemTimeout?.TotalMilliseconds < 0)
            {
                _timeout = _timeoutDisabled;
            }
            else
            {
                _timeout = itemTimeout.Value;
            }

            _logger = logger ?? new DefaultLogger();

            _cache =
                new Dictionary<string, (LinkedListNode<string> node, ItemWrapper value)>(_maxSize);
            _list = new LinkedList<string>();
        }

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

        public void Reset()
        {
            lock (_mutex)
            {
                _cache.Clear();
                _list.Clear();
            }
        }

        private class ItemWrapper
        {
            public readonly T Value;
            public readonly long CreationTimestamp;

            public ItemWrapper(T value)
            {
                Value = value;
                CreationTimestamp = DateTime.Now.MillisecondsSince1970();
            }
        }

        public LinkedList<string> _readCurrentCacheKeys()
        {
            _logger.Log(LogLevel.WARN, "_readCurrentCacheKeys used for non-testing purpose");
            return _list;
        }
    }
}
