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

using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Entity;
using System;
using System.Collections.Generic;

namespace OptimizelySDK.Odp
{
    public class OdpManager : IOdpManager
    {
        private bool _enabled;

        private volatile OdpConfig _odpConfig;

        public IOdpSegmentManager SegmentManager { get; private set; }

        public IOdpEventManager EventManager { get; private set; }

        private ILogger _logger;

        public bool UpdateSettings(string apiKey, string apiHost, List<string> segmentsToCheck)
        {
            var newConfig = new OdpConfig(apiKey, apiHost, segmentsToCheck);
            if (_odpConfig.Equals(newConfig))
            {
                return false;
            }

            _odpConfig = newConfig;

            EventManager.Flush();
            EventManager.UpdateSettings(_odpConfig);

            SegmentManager.ResetCache();
            SegmentManager.UpdateSettings(_odpConfig);

            return true;
        }

        public List<string> FetchQualifiedSegments(string userId, List<OdpSegmentOption> options)
        {
            if (!_enabled || SegmentManager == null)
            {
                _logger.Log(LogLevel.ERROR, Constants.ODP_NOT_ENABLED_MESSAGE);
                return null;
            }

            return SegmentManager.FetchQualifiedSegments(userId, options);
        }

        public void IdentifyUser(string userId)
        {
            if (!_enabled || EventManager == null)
            {
                _logger.Log(LogLevel.DEBUG, "ODP identify event not dispatched (ODP disabled).");
                return;
            }

            if (!EventManager.IsStarted)
            {
                _logger.Log(LogLevel.DEBUG,
                    "ODP identify event not dispatched (ODP not integrated).");
                return;
            }

            EventManager.IdentifyUser(userId);
        }

        public void SendEvent(string type, string action, Dictionary<string, string> identifiers,
            Dictionary<string, object> data
        )
        {
            if (!_enabled || EventManager == null)
            {
                _logger.Log(LogLevel.DEBUG, "ODP event not dispatched (ODP disabled).");
                return;
            }

            if (!EventManager.IsStarted)
            {
                _logger.Log(LogLevel.DEBUG,
                    "ODP event not dispatched (ODP not integrated).");
                return;
            }

            EventManager.SendEvent(new OdpEvent(type, action, identifiers, data));
        }

        public void Close()
        {
            if (!_enabled || EventManager == null)
            {
                return;
            }

            EventManager.Stop();
        }

        /// <summary>
        /// Builder pattern to create an instances of OdpManager
        /// </summary>
        public class Builder
        {
            private OdpConfig _odpConfig;
            private IOdpEventManager _eventManager;
            private IOdpSegmentManager _segmentManager;
            private int _cacheSize;
            private int _cacheTimeoutSeconds;
            private ILogger _logger;
            private IErrorHandler _errorHandler;
            private ICache<List<string>> _cache;

            public Builder WithSegmentManager(IOdpSegmentManager segmentManager)
            {
                _segmentManager = segmentManager;
                return this;
            }

            public Builder WithEventManager(IOdpEventManager eventManager)
            {
                _eventManager = eventManager;
                return this;
            }

            public Builder WithOdpConfig(OdpConfig odpConfig)
            {
                _odpConfig = odpConfig;
                return this;
            }

            public Builder WithCacheSize(int cacheSize)
            {
                _cacheSize = cacheSize;
                return this;
            }

            public Builder WithCacheTimeout(int seconds)
            {
                _cacheTimeoutSeconds = seconds;
                return this;
            }

            public Builder WithLogger(ILogger logger = null)
            {
                _logger = logger;
                return this;
            }

            public Builder WithErrorHandler(IErrorHandler errorHandler = null)
            {
                _errorHandler = errorHandler;
                return this;
            }

            public Builder WithCacheImplementation(ICache<List<string>> cache)
            {
                _cache = cache;
                return this;
            }

            /// <summary>
            /// Build OdpManager instance using collected parameters
            /// </summary>
            /// <param name="asEnabled">Should mark as enabled upon initialization</param>
            /// <returns>OdpManager instance</returns>
            public OdpManager Build(bool asEnabled = true)
            {
                var manager = new OdpManager
                {
                    _odpConfig = _odpConfig,
                    _logger = _logger ?? new NoOpLogger(),
                    _enabled = asEnabled,
                };

                _errorHandler = _errorHandler ?? new NoOpErrorHandler();

                if (_eventManager == null)
                {
                    var eventApiManager = new OdpEventApiManager(_logger, _errorHandler);

                    manager.EventManager = new OdpEventManager.Builder().WithOdpConfig(_odpConfig).
                        WithOdpEventApiManager(eventApiManager).
                        WithLogger(_logger).
                        WithErrorHandler(_errorHandler).
                        Build();
                }
                else
                {
                    manager.EventManager = _eventManager;
                }

                if (_segmentManager == null)
                {
                    var cacheTimeout = TimeSpan.FromSeconds(_cacheTimeoutSeconds <= 0 ?
                        Constants.DEFAULT_CACHE_SECONDS :
                        _cacheTimeoutSeconds);
                    var apiManager = new OdpSegmentApiManager(_logger, _errorHandler);

                    manager.SegmentManager = new OdpSegmentManager(_odpConfig, apiManager,
                        _cacheSize, cacheTimeout, _logger, _cache);
                }
                else
                {
                    manager.SegmentManager = _segmentManager;
                }
                
                manager.EventManager.Start();

                return manager;
            }
        }
    }
}
