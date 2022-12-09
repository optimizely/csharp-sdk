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
    /// <summary>
    /// Concrete implementation to orchestrate segment manager, event manager, and ODP config
    /// </summary>
    public class OdpManager : IOdpManager, IDisposable
    {
        /// <summary>
        /// Denotes if ODP Manager is meant to be handling ODP communication
        /// </summary>
        private bool _enabled;

        /// <summary>
        /// Configuration used to communicate with ODP
        /// </summary>
        private volatile OdpConfig _odpConfig;

        /// <summary>
        /// Manager used to handle audience segment membership
        /// </summary>
        public IOdpSegmentManager SegmentManager { get; private set; }

        /// <summary>
        /// Manager used to send events to ODP 
        /// </summary>
        public IOdpEventManager EventManager { get; private set; }

        /// <summary>
        /// Logger used to record messages that occur within the ODP client
        /// </summary>
        private ILogger _logger;
        
        private OdpManager() { }

        /// <summary>
        /// Update the settings being used for ODP configuration and reset/restart dependent processes
        /// </summary>
        /// <param name="apiKey">Public API key from ODP</param>
        /// <param name="apiHost">Host portion of the URL of ODP</param>
        /// <param name="segmentsToCheck">Audience segments to consider</param>
        /// <returns>True if settings were update otherwise False</returns>
        public bool UpdateSettings(string apiKey, string apiHost, List<string> segmentsToCheck)
        {
            var newConfig = new OdpConfig(apiKey, apiHost, segmentsToCheck);
            if (_odpConfig.Equals(newConfig))
            {
                return false;
            }

            _odpConfig = newConfig;

            EventManager.UpdateSettings(_odpConfig);

            SegmentManager.ResetCache();
            SegmentManager.UpdateSettings(_odpConfig);

            return true;
        }

        /// <summary>
        /// Attempts to fetch and return a list of a user's qualified segments.
        /// </summary>
        /// <param name="userId">FS User ID</param>
        /// <param name="options">Options used during segment cache handling</param>
        /// <returns>Qualified segments for the user from the cache or the ODP server</returns>
        public string[] FetchQualifiedSegments(string userId, List<OdpSegmentOption> options)
        {
            if (SegmentManagerOrConfigNotReady())
            {
                _logger.Log(LogLevel.ERROR, Constants.ODP_NOT_ENABLED_MESSAGE);
                return null;
            }

            return SegmentManager.FetchQualifiedSegments(userId, options).ToArray();
        }

        /// <summary>
        /// Send identification event to ODP for a given full-stack User ID
        /// </summary>
        /// <param name="userId">User ID to send</param>
        public void IdentifyUser(string userId)
        {
            if (EventManagerOrConfigNotReady())
            {
                _logger.Log(LogLevel.DEBUG, "ODP identify event not dispatched (ODP disabled).");
                return;
            }

            EventManager.IdentifyUser(userId);
        }

        /// <summary>
        /// Add event to queue for sending to ODP
        /// </summary>
        /// <param name="type">Type of event (typically `fullstack` from server-side SDK events)</param>
        /// <param name="action">Subcategory of the event type</param>
        /// <param name="identifiers">Key-value map of user identifiers</param>
        /// <param name="data">Event data in a key-value pair format</param>
        public void SendEvent(string type, string action, Dictionary<string, string> identifiers,
            Dictionary<string, object> data
        )
        {
            if (EventManagerOrConfigNotReady())
            {
                _logger.Log(LogLevel.ERROR, "ODP event not dispatched (ODP disabled).");
                return;
            }

            EventManager.SendEvent(new OdpEvent(type, action, identifiers, data));
        }

        /// <summary>
        /// Sends signal to stop Event Manager and clean up ODP Manager use
        /// </summary>
        public void Dispose()
        {
            if (EventManager == null || !_enabled)
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
                _logger = _logger ?? new DefaultLogger();
                _errorHandler = _errorHandler ?? new NoOpErrorHandler();
                _odpConfig = _odpConfig ?? new OdpConfig();

                var manager = new OdpManager
                {
                    _odpConfig = _odpConfig,
                    _logger = _logger,
                    _enabled = asEnabled,
                };

                if (!manager._enabled)
                {
                    return manager;
                }

                if (_eventManager == null)
                {
                    var eventApiManager = new OdpEventApiManager(_logger, _errorHandler);

                    manager.EventManager = new OdpEventManager.Builder().
                        WithOdpConfig(_odpConfig).
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

        /// <summary>
        /// Determines if the EventManager is ready to be used
        /// </summary>
        /// <returns>True if EventManager can process events otherwise False</returns>
        private bool EventManagerOrConfigNotReady()
        {
            return EventManager == null || !_enabled || !_odpConfig.IsReady();
        }

        /// <summary>
        /// Determines if the SegmentManager is ready to be used
        /// </summary>
        /// <returns>True if SegmentManager can fetch audience segments otherwise False</returns>
        private bool SegmentManagerOrConfigNotReady()
        {
            return SegmentManager == null || !_enabled || !_odpConfig.IsReady();
        }
    }
}
