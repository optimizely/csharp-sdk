using OptimizelySDK.Logger;
using System;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OptimizelySDK.Tests")]

namespace OptimizelySDK.Odp
{
    public class LruCache<T> : ILruCache<T>
    {
        public const int DEFAULT_MAX_SIZE = 10000;
        public const int DEFAULT_TIMEOUT_SECONDS = 600;

        private readonly ILogger _logger;
        private readonly object _mutex = new object();
        private int _maxSize;
        private long _timeoutMilliseconds;
        internal OrderedDictionary _orderedDictionary = new OrderedDictionary();

        public LruCache() : this(DEFAULT_MAX_SIZE, DEFAULT_TIMEOUT_SECONDS, null) { }

        public LruCache(ILogger logger) :
            this(DEFAULT_MAX_SIZE, DEFAULT_TIMEOUT_SECONDS, logger) { }


        public LruCache(int maxSize, int timeoutSeconds, ILogger logger = null)
        {
            _maxSize = maxSize < 0 ? default : maxSize;
            _timeoutMilliseconds = (timeoutSeconds < 0) ? 0 : (timeoutSeconds * 1000L);
            _logger = logger ?? new DefaultLogger();
        }

        public void SetMaxSize(int size)
        {
            lock (_mutex)
            {
                if (_orderedDictionary.Count > 0)
                {
                    if (size >= _orderedDictionary.Count)
                    {
                        _maxSize = size;
                    }
                    else
                    {
                        _logger.Log(LogLevel.WARN,
                            "Cannot set max cache size less than current size.");
                    }
                }
                else
                {
                    var sizeToSet = size;
                    if (size < 0)
                    {
                        sizeToSet = 0;
                    }

                    _maxSize = sizeToSet;
                }
            }
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
    }
}