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

using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Entity;
using OptimizelySDK.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OptimizelySDK.Odp
{
    public class OdpEventManager : IOdpEventManager, IDisposable
    {
        private OdpConfig _odpConfig;
        private IOdpEventApiManager _odpEventApiManager;
        private int _batchSize;
        private TimeSpan _flushInterval;
        private TimeSpan _timeoutInterval;
        private ILogger _logger;
        private IErrorHandler _errorHandler;
        private BlockingCollection<object> _eventQueue;

        private readonly object _mutex = new object();
        private readonly object _shutdownSignal = new object();
        private readonly object _flushSignal = new object();

        public bool Disposed { get; private set; }
        public bool IsStarted { get; private set; }

        private Thread _executionThread;

        private readonly List<OdpEvent> _currentBatch = new List<OdpEvent>();
        private long _flushingIntervalDeadline;

        /// <summary>
        /// Valid C# types for ODP Data entries 
        /// </summary>
        private readonly List<string> _validOdpDataTypes = new List<string>()
        {
            "Char",
            "String",
            "Int16",
            "Int32",
            "Int64",
            "Single",
            "Double",
            "Decimal",
            "Boolean",
            "Guid",
        };

        private readonly Dictionary<string, object> _commonData = new Dictionary<string, object>
        {
            {
                "idempotence_id", Guid.NewGuid()
            },
            {
                "data_source_type", "sdk"
            },
            {
                "data_source", Optimizely.SDK_TYPE
            },
            {
                "data_source_version", Optimizely.SDK_VERSION
            },
        };

        private void DropQueue()
        {
            lock (_mutex)
            {
                _eventQueue = new BlockingCollection<object>();
            }
        }

        public void Start()
        {
            if ((IsStarted && !Disposed) || !_odpConfig.IsReady())
            {
                _logger.Log(LogLevel.WARN, Constants.ODP_NOT_INTEGRATED_MESSAGE);

                DropQueue();

                return;
            }
            _flushingIntervalDeadline = DateTime.Now.MillisecondsSince1970() +
                                        (long)_flushInterval.TotalMilliseconds;
            _executionThread = new Thread(Run);
            _executionThread.Start();
            IsStarted = true;
        }

        /// <summary>
        /// Scheduler method that periodically runs on provided polling interval.
        /// </summary>
        protected virtual void Run()
        {
            try
            {
                while (true)
                {
                    if (DateTime.Now.MillisecondsSince1970() > _flushingIntervalDeadline)
                    {
                        _logger.Log(LogLevel.DEBUG, $"Flushing queue.");
                        FlushQueue();
                    }

                    if (!_eventQueue.TryTake(out object item, 50))
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    if (item == _shutdownSignal)
                    {
                        _logger.Log(LogLevel.INFO, "Received shutdown signal.");
                        break;
                    }

                    if (item == _flushSignal)
                    {
                        _logger.Log(LogLevel.DEBUG, "Received flush signal.");
                        FlushQueue();
                        continue;
                    }

                    if (item is OdpEvent odpEvent)
                    {
                        AddToBatch(odpEvent);
                    }
                }
            }
            catch (InvalidOperationException ioe)
            {
                // An InvalidOperationException means that Take() was called on a completed collection
                _logger.Log(LogLevel.DEBUG,
                    "Unable to dequeue item from eventQueue: " + ioe.GetAllMessages());
            }
            catch (Exception e)
            {
                _errorHandler.HandleError(e);
                _logger.Log(LogLevel.ERROR,
                    "Uncaught exception processing queue. Error: " + e.GetAllMessages());
            }
            finally
            {
                _logger.Log(LogLevel.INFO,
                    "Exiting processing loop. Attempting to flush pending events.");
                FlushQueue();
            }
        }

        /// <summary>
        /// Immediately send all ODP events in queue
        /// </summary>
        public void Flush()
        {
            _flushingIntervalDeadline = DateTime.Now.MillisecondsSince1970() +
                                        (long)_flushInterval.TotalMilliseconds;
            _eventQueue.Add(_flushSignal);
        }

        private void FlushQueue()
        {
            _flushingIntervalDeadline = DateTime.Now.MillisecondsSince1970() +
                                        (long)_flushInterval.TotalMilliseconds;

            if (_currentBatch.Count == 0)
            {
                return;
            }

            List<OdpEvent> toProcessBatch;
            lock (_mutex)
            {
                toProcessBatch = new List<OdpEvent>(_currentBatch);
                _currentBatch.Clear();
            }

            try
            {
                bool shouldRetry;
                var attemptNumber = 0;
                do
                {
                    shouldRetry = _odpEventApiManager.SendEvents(_odpConfig.ApiKey,
                        _odpConfig.ApiHost, toProcessBatch);
                    attemptNumber += 1;
                } while (shouldRetry && attemptNumber < Constants.MAX_RETRIES);
            }
            catch (Exception e)
            {
                _errorHandler.HandleError(e);
                _logger.Log(LogLevel.ERROR, Constants.ODP_SEND_FAILURE_MESSAGE);
            }
        }

        /// <summary>
        /// Stops ODP event processor.
        /// </summary>
        public void Stop()
        {
            if (Disposed)
            {
                _logger.Log(LogLevel.WARN, Constants.ODP_NOT_INTEGRATED_MESSAGE);

                DropQueue();

                return;
            }

            _eventQueue.Add(_shutdownSignal);

            if (!_executionThread.Join(_timeoutInterval))
            {
                _logger.Log(LogLevel.ERROR,
                    $"Timeout exceeded attempting to close for {_timeoutInterval.Milliseconds} ms");
            }

            IsStarted = false;
            _logger.Log(LogLevel.WARN, $"Stopping scheduler.");
        }

        /// <summary>
        /// Add event to queue for sending to ODP
        /// </summary>
        /// <param name="odpEvent">Event to enqueue</param>
        public void SendEvent(OdpEvent odpEvent)
        {
            if (Disposed || !IsStarted || !_odpConfig.IsReady())
            {
                _logger.Log(LogLevel.WARN, Constants.ODP_NOT_INTEGRATED_MESSAGE);
                return;
            }

            if (InvalidDataFound(odpEvent.Data))
            {
                _logger.Log(LogLevel.ERROR, Constants.ODP_INVALID_DATA_MESSAGE);
                return;
            }

            odpEvent.Data = AugmentCommonData(odpEvent.Data);
            if (!_eventQueue.TryAdd(odpEvent))
            {
                _logger.Log(LogLevel.WARN, "Payload not accepted by the queue.");
            }
        }

        /// <summary>
        /// Adds an event to the current batch being created 
        /// </summary>
        /// <param name="odpEvent"></param>
        private void AddToBatch(OdpEvent odpEvent)
        {
            lock (_mutex)
            {
                // Reset the deadline if starting a new batch.
                if (_currentBatch.Count == 0)
                {
                    _flushingIntervalDeadline = DateTime.Now.MillisecondsSince1970() +
                                                (long)_flushInterval.TotalMilliseconds;
                }

                _currentBatch.Add(odpEvent);

                if (_currentBatch.Count >= _batchSize)
                {
                    FlushQueue();
                }
            }
        }

        /// <summary>
        /// Ensures queue processing is stopped marking this instance as disposed 
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Stop();
            Disposed = true;
        }

        /// <summary>
        /// Associate a full-stack userid with an established VUID
        /// </summary>
        /// <param name="userId">Full-stack User ID</param>
        public void IdentifyUser(string userId)
        {
            var identifiers = new Dictionary<string, string>
            {
                {
                    OdpUserKeyType.FS_USER_ID.ToString(), userId
                },
            };

            var odpEvent = new OdpEvent(Constants.ODP_EVENT_TYPE, "identified", identifiers);
            SendEvent(odpEvent);
        }

        /// <summary>
        /// Update ODP configuration settings
        /// </summary>
        /// <param name="odpConfig">Configuration object containing new values</param>
        public void UpdateSettings(OdpConfig odpConfig)
        {
            _odpConfig.Update(odpConfig.ApiKey, odpConfig.ApiHost, odpConfig.SegmentsToCheck);
        }

        /// <summary>
        /// ODP event data has invalid types
        /// </summary>
        /// <param name="data">Data to be analyzed</param>
        /// <returns>True if a type is not a valid type or is not null otherwise False</returns>
        private bool InvalidDataFound(Dictionary<string, dynamic> data)
        {
            return data.Any(item =>
                item.Value != null &&
                !_validOdpDataTypes.Any(t =>
                    t.Equals(item.Value.GetType().Name, StringComparison.OrdinalIgnoreCase))
            );
        }

        /// <summary>
        /// Add additional common data including an idempotent ID and execution context to event data
        /// </summary>
        /// <param name="sourceData">Existing event data to augment</param>
        /// <returns>Updated Dictionary with new key-values added</returns>
        private Dictionary<string, dynamic> AugmentCommonData(
            Dictionary<string, dynamic> sourceData
        )
        {
            return sourceData.MergeInPlace<string, object>(_commonData);
        }

        /// <summary>
        /// Builder pattern to create an instances of OdpEventManager
        /// </summary>
        public class Builder
        {
            private BlockingCollection<object> _eventQueue =
                new BlockingCollection<object>(Constants.DEFAULT_QUEUE_CAPACITY);

            private OdpConfig _odpConfig;
            private IOdpEventApiManager _odpEventApiManager;
            private int _batchSize;
            private TimeSpan _flushInterval;
            private TimeSpan _timeoutInterval;
            private ILogger _logger;
            private IErrorHandler _errorHandler;

            public Builder WithEventQueue(BlockingCollection<object> eventQueue)
            {
                _eventQueue = eventQueue;
                return this;
            }

            public Builder WithOdpConfig(OdpConfig odpConfig)
            {
                _odpConfig = odpConfig;
                return this;
            }

            public Builder WithOdpEventApiManager(IOdpEventApiManager odpEventApiManager)
            {
                _odpEventApiManager = odpEventApiManager;
                return this;
            }

            public Builder WithBatchSize(int batchSize)
            {
                _batchSize = batchSize;
                return this;
            }

            public Builder WithFlushInterval(TimeSpan flushInterval)
            {
                _flushInterval = flushInterval;
                return this;
            }

            public Builder WithTimeoutInterval(TimeSpan timeout)
            {
                _timeoutInterval = timeout;
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

            /// <summary>
            /// Build OdpEventManager instance using collected parameters
            /// </summary>
            /// <param name="startImmediately">Should start event processor upon initialization</param>
            /// <returns>OdpEventProcessor instance</returns>
            public OdpEventManager Build(bool startImmediately = true)
            {
                var manager = new OdpEventManager();
                manager._eventQueue = _eventQueue;
                manager._odpConfig = _odpConfig;
                manager._odpEventApiManager = _odpEventApiManager;
                manager._batchSize =
                    _batchSize < 1 ? Constants.DEFAULT_BATCH_SIZE : _batchSize;
                manager._flushInterval = _flushInterval <= TimeSpan.FromSeconds(0) ?
                    Constants.DEFAULT_FLUSH_INTERVAL :
                    _flushInterval;
                manager._timeoutInterval = _timeoutInterval <= TimeSpan.FromSeconds(0) ?
                    Constants.DEFAULT_TIMEOUT_INTERVAL :
                    _timeoutInterval;
                manager._logger = _logger ?? new NoOpLogger();
                manager._errorHandler = _errorHandler ?? new NoOpErrorHandler();

                if (startImmediately)
                {
                    manager.Start();
                }

                return manager;
            }
        }
    }
}
