using OptimizelySDK.Logger;
using System;
using System.Collections.Specialized;

namespace OptimizelySDK.Odp
{
    public class LruCache<T> : ILruCache<T>
    {
        private const int DEFAULT_MAX_SIZE = 10000;
        private const int DEFAULT_TIMEOUT_SECONDS = 600;

        private readonly ILogger _logger;
        private readonly object _mutex = new object();
        private int _maxSize;
        private long _timeoutMilliseconds;
        private readonly OrderedDictionary _orderedDictionary = new OrderedDictionary();

        public LruCache() : this(DEFAULT_MAX_SIZE, DEFAULT_TIMEOUT_SECONDS, null) { }

        public LruCache(ILogger logger) :
            this(DEFAULT_MAX_SIZE, DEFAULT_TIMEOUT_SECONDS, logger) { }

        public LruCache(int maxSize, int timeoutSeconds, ILogger logger)
        {
            _maxSize = maxSize < 0 ? default(int) : maxSize;
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

        public void SetTimeout(long timeoutSeconds)
        {
            _timeoutMilliseconds = timeoutSeconds * 1000;
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

                ItemWrapper item = (ItemWrapper)_orderedDictionary[key];
                var nowMs = DateTime.Now.Millisecond;

                // ttl = 0 means items never expire.
                if (_timeoutMilliseconds == 0 ||
                    (nowMs - item.Timestamp < _timeoutMilliseconds))
                {
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
            public T Value;
            public long Timestamp;

            public ItemWrapper(T value)
            {
                Value = value;
                Timestamp = Convert.ToInt64(
                    DateTime.Now.Subtract(new DateTime(1970, 1, 1)).
                        TotalMilliseconds);
            }
        }
    }
}