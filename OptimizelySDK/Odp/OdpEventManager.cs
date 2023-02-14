﻿/* 
 * Copyright 2022-2023, Optimizely
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Entity;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Odp
{
    /// <summary>
    /// Concrete implementation of a manager responsible for queuing and sending events to the
    /// Optimizely Data Platform
    /// </summary>
    public class OdpEventManager : IOdpEventManager, IDisposable
    {
        private volatile OdpConfig _odpConfig;
        private IOdpEventApiManager _odpEventApiManager;
        private int _batchSize;
        private TimeSpan _flushInterval;
        private TimeSpan _timeoutInterval;
        private ILogger _logger;
        private IErrorHandler _errorHandler;
        private BlockingCollection<object> _eventQueue;

        /// <summary>
        /// Object to ensure mutually exclusive locking for thread safety
        /// </summary>
        private readonly object _mutex = new object();

        /// <summary>
        /// Object passed into the queue indicating a need to stop processing 
        /// </summary>
        private readonly object _shutdownSignal = new object();

        /// <summary>
        /// Object passed into the queue indicating a need to flush all events from the queue
        /// </summary>
        private readonly object _flushSignal = new object();

        /// <summary>
        /// Indicates the ODP Event Manager has been stopped and disposed 
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// Indicates the ODP Event Manager instance is in a running state
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Thread used to execute the loop to process queued and batched events
        /// </summary>
        private Thread _executionThread;

        /// <summary>
        /// Separate batch collected from the primary queue
        /// </summary>
        private readonly List<OdpEvent> _currentBatch = new List<OdpEvent>();

        /// <summary>
        /// Time when the next flush interval will be hit and a flush is to be executed
        /// </summary>
        private long _flushingIntervalDeadline;

        /// <summary>
        /// Valid C# types for ODP Data entries
        /// </summary>
        private List<string> _validOdpDataTypes;

        /// <summary>
        /// Common data to be added to each ODP event prior to sending
        /// </summary>
        private Dictionary<string, object> _commonData;

        /// <summary>
        /// Indicates if OdpEventManager should start upon Build() and UpdateSettings()
        /// </summary>
        private bool _autoStart;

        /// <summary>
        /// Clear all entries from the queue
        /// </summary>
        private void DropQueue()
        {
            lock (_mutex)
            {
                _eventQueue = new BlockingCollection<object>();
            }
        }

        /// <summary>
        /// Begin the execution thread to process the queue into bathes and send events
        /// </summary>
        public void Start()
        {
            if (_odpConfig == null || !_odpConfig.IsReady())
            {
                _logger.Log(LogLevel.WARN, Constants.ODP_NOT_INTEGRATED_MESSAGE);

                DropQueue();

                return;
            }

            if (IsStarted && !Disposed)
            {
                _logger.Log(LogLevel.WARN, Constants.ODP_ALREADY_STARTED);

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
                    object item;
                    // If batch has events, set the timeout to remaining time for flush interval,
                    //      otherwise wait for the new event indefinitely
                    if (_currentBatch.Count > 0)
                    {
                        _eventQueue.TryTake(out item,
                            (int)(_flushingIntervalDeadline -
                                  DateTime.Now.MillisecondsSince1970()));
                    }
                    else
                    {
                        item = _eventQueue.Take();
                        // TODO: need to figure out why this is allowing item to read shutdown signal.
                        Thread.Sleep(1);
                    }

                    if (item == null)
                    {
                        // null means no new events received and flush interval is over, dispatch whatever is in the batch.
                        if (_currentBatch.Count != 0)
                        {
                            _logger.Log(LogLevel.DEBUG, $"Flushing queue.");
                            FlushQueue();
                        }

                        continue;
                    }
                    else if (item == _shutdownSignal)
                    {
                        _logger.Log(LogLevel.INFO, "Received shutdown signal.");
                        break;
                    }
                    else if (item == _flushSignal)
                    {
                        _logger.Log(LogLevel.DEBUG, "Received flush signal.");
                        FlushQueue();
                        continue;
                    }
                    else if (item is OdpEvent odpEvent)
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
        /// Signal that all ODP events in queue should be sent
        /// </summary>
        public void Flush()
        {
            _eventQueue.Add(_flushSignal);
        }

        /// <summary>
        /// Send all events in queue
        /// </summary>
        private void FlushQueue()
        {
            _flushingIntervalDeadline = DateTime.Now.MillisecondsSince1970() +
                                        (long)_flushInterval.TotalMilliseconds;

            if (_currentBatch.Count == 0)
            {
                return;
            }

            var toProcessBatch = new List<OdpEvent>(_currentBatch);
            _currentBatch.Clear();

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

            if (_executionThread != null && !_executionThread.Join(_timeoutInterval))
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
            if (_odpConfig == null || !_odpConfig.IsReady())
            {
                _logger.Log(LogLevel.WARN, Constants.ODP_NOT_INTEGRATED_MESSAGE);
                return;
            }

            if (Disposed || !IsStarted)
            {
                _logger.Log(LogLevel.WARN, Constants.ODP_NOT_ENABLED_MESSAGE);
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
                { OdpUserKeyType.FS_USER_ID.ToString(), userId },
            };

            var odpEvent = new OdpEvent(Constants.ODP_EVENT_TYPE, "identified", identifiers);
            SendEvent(odpEvent);
        }

        /// <summary>
        /// Update ODP configuration settings with a implied flush of the queued events
        /// </summary>
        /// <param name="odpConfig">Configuration object containing new values</param>
        public void UpdateSettings(OdpConfig odpConfig)
        {
            if (odpConfig == null)
            {
                return;
            }

            Flush();

            _odpConfig = odpConfig;

            if (_autoStart)
            {
                Start();
            }
        }

        /// <summary>
        /// ODP event data has invalid types
        /// </summary>
        /// <param name="data">Data to be analyzed</param>
        /// <returns>True if a type is not a valid type or is not null otherwise False</returns>
        private bool InvalidDataFound(Dictionary<string, object> data)
        {
            if (data == null || data.Count <= 0)
            {
                return false;
            }

            foreach (var item in data)
            {
                if (item.Value == null)
                {
                    continue;
                }

                var valueTypeName = item.Value.GetType().Name;
                if (!_validOdpDataTypes.Contains(valueTypeName, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Add additional common data including an idempotent ID and execution context to event
        /// data. Note: sourceData takes precedence over commonData in the event of a key conflict. 
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

            private IOdpEventApiManager _odpEventApiManager;
            private TimeSpan _flushInterval;
            private TimeSpan _timeoutInterval;
            private ILogger _logger;
            private IErrorHandler _errorHandler;
            private bool? _autoStart;

            /// <summary>
            /// Indicates if OdpEventManager should start upon Build() and UpdateSettings()
            /// </summary>
            /// <param name="autoStart"></param>
            /// <returns></returns>
            public Builder WithAutoStart(bool autoStart)
            {
                _autoStart = autoStart;
                return this;
            }

            /// <summary>
            /// Provide an Event Queue
            /// </summary>
            /// <param name="eventQueue">Concrete implementation of an event queue</param>
            /// <returns>Current Builder instance</returns>
            public Builder WithEventQueue(BlockingCollection<object> eventQueue)
            {
                _eventQueue = eventQueue;
                return this;
            }

            /// <summary>
            /// Provide an ODP Event Manager API
            /// </summary>
            /// <param name="odpEventApiManager">Concrete implementation of an Event API Manager</param>
            /// <returns>Current Builder instance</returns>
            public Builder WithOdpEventApiManager(IOdpEventApiManager odpEventApiManager)
            {
                _odpEventApiManager = odpEventApiManager;
                return this;
            }

            /// <summary>
            /// Provide an flush interval
            /// </summary>
            /// <param name="flushInterval">Frequency to flush the queue</param>
            /// <returns>Current Builder instance</returns>
            public Builder WithFlushInterval(TimeSpan flushInterval)
            {
                _flushInterval = flushInterval;
                return this;
            }

            /// <summary>
            /// Provide a timeout to wait for network communication
            /// </summary>
            /// <param name="timeout">Span to allow for communication timeout</param>
            /// <returns>Current Builder instance</returns>
            public Builder WithTimeoutInterval(TimeSpan timeout)
            {
                _timeoutInterval = timeout;
                return this;
            }

            /// <summary>
            /// Provide a logger to record code events
            /// </summary>
            /// <param name="logger">Concrete implementation of a logger</param>
            /// <returns>Current Builder instance</returns>
            public Builder WithLogger(ILogger logger = null)
            {
                _logger = logger;
                return this;
            }

            /// <summary>
            /// Provide an error handler
            /// </summary>
            /// <param name="errorHandler">Concrete implementation of an error handler</param>
            /// <returns>Current Builder instance</returns>
            public Builder WithErrorHandler(IErrorHandler errorHandler = null)
            {
                _errorHandler = errorHandler;
                return this;
            }

            /// <summary>
            /// Build OdpEventManager instance using collected parameters
            /// </summary>
            /// <returns>OdpEventProcessor instance</returns>
            public OdpEventManager Build()
            {
                var manager = new OdpEventManager();
                manager._eventQueue = _eventQueue;
                manager._odpEventApiManager = _odpEventApiManager;
                manager._flushInterval = _flushInterval > TimeSpan.Zero ?
                    _flushInterval :
                    Constants.DEFAULT_FLUSH_INTERVAL;
                manager._batchSize = _flushInterval == TimeSpan.Zero ?
                    1 :
                    Constants.DEFAULT_BATCH_SIZE;
                manager._timeoutInterval = _timeoutInterval <= TimeSpan.Zero ?
                    Constants.DEFAULT_TIMEOUT_INTERVAL :
                    _timeoutInterval;
                manager._logger = _logger ?? new NoOpLogger();
                manager._errorHandler = _errorHandler ?? new NoOpErrorHandler();
                manager._autoStart = _autoStart ?? true;

                manager._validOdpDataTypes = new List<string>()
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

                manager._commonData = new Dictionary<string, object>
                {
                    { "idempotence_id", Guid.NewGuid() },
                    { "data_source_type", "sdk" },
                    { "data_source", Optimizely.SDK_TYPE },
                    { "data_source_version", Optimizely.SDK_VERSION },
                };

                if (manager._autoStart)
                {
                    manager.Start();
                }

                return manager;
            }
        }

        /// <summary>
        /// For Testing Only: Read the current ODP config
        /// </summary>
        /// <returns>Current ODP settings</returns>
        internal OdpConfig OdpConfigForTesting => _odpConfig;

        /// <summary>
        /// For Testing Only: Read the current flush interval
        /// </summary>
        internal TimeSpan FlushIntervalForTesting => _flushInterval;

        /// <summary>
        /// For Testing Only: Read the current timeout interval
        /// </summary>
        internal TimeSpan TimeoutIntervalForTesting => _timeoutInterval;
    }
}
