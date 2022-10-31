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
using OptimizelySDK.Odp.Entity;
using OptimizelySDK.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizelySDK.Odp
{
    public class OdpEventManager : IOdpEventManager
    {
        /// <summary>
        /// Enumeration of acceptable states of the Event Manager
        /// </summary>
        private enum ExecutionState
        {
            Stopped = 0,
            Running = 1,
            Processing = 2,
        }

        private const int MAX_RETRIES = 3;
        private const int DEFAULT_BATCH_SIZE = 10;
        private const int DEFAULT_FLUSH_INTERVAL_MSECS = 1000;
        private const int DEFAULT_SERVER_QUEUE_SIZE = 10000;
        public const string TYPE = "fullstack";

        /// <summary>
        /// Current state of the event processor
        /// </summary>
        private ExecutionState State { get; set; } = ExecutionState.Stopped;

        /// <summary>
        /// Identifier of the currently running timeout
        /// </summary>
        private CancellationTokenSource _timeoutToken;

        /// <summary>
        /// ODP configuration settings in used
        /// </summary>
        private readonly IOdpConfig _odpConfig;
        /// <summary>
        /// REST API Manager used to send the events
        /// </summary>
        private readonly IOdpEventApiManager _odpEventApiManager;
        /// <summary>
        /// Handler for recording execution logs
        /// </summary>
        private readonly ILogger _logger;
        /// <summary>
        /// Maximum queue size
        /// </summary>
        private readonly int _queueSize;
        /// <summary>
        /// Maximum number of events to process at once
        /// </summary>
        private readonly int _batchSize;
        /// <summary>
        /// Milliseconds between setTimeout() to process new batches
        /// </summary>
        private readonly int _flushInterval;

        /// <summary>
        /// Valid C# types for ODP Data entries 
        /// </summary>
        private readonly List<string> _validOdpDataTypes = new List<string>()
        {
            "String",
            "Int16",
            "Int32",
            "Decimal",
            "Char",
            "Double",
            "Boolean",
            "Guid",
        };

        /// <summary>
        /// Queue for holding all events to be eventually dispatched
        /// </summary>
        private ConcurrentQueue<OdpEvent> _queue;

        public OdpEventManager(IOdpConfig odpConfig, IOdpEventApiManager odpEventApiManager,
            ILogger logger,
            int queueSize = DEFAULT_SERVER_QUEUE_SIZE, int batchSize = DEFAULT_BATCH_SIZE,
            int flushInterval = DEFAULT_FLUSH_INTERVAL_MSECS
        )
        {
            _odpConfig = odpConfig;
            _odpEventApiManager = odpEventApiManager;
            _logger = logger;
            _queueSize = queueSize;
            _batchSize = batchSize;
            _flushInterval = flushInterval;

            _queue = new ConcurrentQueue<OdpEvent>();
            _timeoutToken = new CancellationTokenSource();
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
        /// Start processing events in the queue
        /// </summary>
        public void Start()
        {
            State = ExecutionState.Running;

            SetNewTimeout();
        }

        /// <summary>
        /// Drain the queue sending all remaining events in batches then stop processing
        /// </summary>
        public void Stop()
        {
            _logger.Log(LogLevel.DEBUG, "Stop requested.");

            // process queue with flush
            ProcessQueue(true);

            State = ExecutionState.Stopped;
            _logger.Log(LogLevel.DEBUG, $"Stopped. Queue Count: {_queue.Count}.");
        }

        /// <summary>
        /// Register a new visitor user id (VUID) in ODP
        /// </summary>
        /// <param name="vuid">Visitor ID to register</param>
        public void RegisterVuid(string vuid)
        {
            var identifiers = new Dictionary<string, string>
            {
                {
                    OdpUserKeyType.VUID.ToString(), vuid
                },
            };

            var odpEvent = new OdpEvent(TYPE, "client_initialized", identifiers);
            SendEvent(odpEvent);
        }

        /// <summary>
        /// Associate a full-stack userid with an established VUID
        /// </summary>
        /// <param name="userId">Full-stack User ID</param>
        /// <param name="vuid">Visitor User ID</param>
        public void IdentifyUser(string userId, string vuid)
        {
            var identifiers = new Dictionary<string, string>
            {
                {
                    OdpUserKeyType.FS_USER_ID.ToString(), userId
                },
            };

            if (!string.IsNullOrWhiteSpace(vuid))
            {
                identifiers.Add(OdpUserKeyType.VUID.ToString(), vuid);
            }

            var odpEvent = new OdpEvent(TYPE, "identified", identifiers);
            SendEvent(odpEvent);
        }

        /// <summary>
        /// Send an event to ODP via dispatch queue
        /// </summary>
        /// <param name="odpEvent">ODP Event to forward</param>
        public void SendEvent(OdpEvent odpEvent)
        {
            if (State == ExecutionState.Stopped)
            {
                _logger.Log(LogLevel.WARN,
                    "Failed to Process ODP Event. ODPEventManager is not running.");
                return;
            }

            if (!IsOdpConfigurationReady())
            {
                return;
            }

            if (InvalidDataFound(odpEvent.Data))
            {
                _logger.Log(LogLevel.ERROR, "Event data found to be invalid.");
            }
            else
            {
                odpEvent.Data = AugmentCommonData(odpEvent.Data);
                Enqueue(odpEvent);
            }
        }

        /// <summary>
        /// Add a new ODP event to the queue
        /// </summary>
        /// <param name="odpEvent"></param>
        private void Enqueue(OdpEvent odpEvent)
        {
            if (_queue.Count >= _queueSize)
            {
                _logger.Log(LogLevel.WARN,
                    $"Failed to Process ODP Event. Event Queue full. queueSize = {_queue.Count}.");
                return;
            }

            _queue.Enqueue(odpEvent);

            ProcessQueue();
        }

        /// <summary>
        /// Process the queue
        /// </summary>
        /// <param name="shouldFlush">True if complete flush of queue is needed</param>
        private void ProcessQueue(bool shouldFlush = false)
        {
            if (State != ExecutionState.Running)
            {
                return;
            }

            if (!IsOdpConfigurationReady())
            {
                return;
            }
            
            _logger.Log(LogLevel.DEBUG,
                $"Processing Queue {(shouldFlush ? "(flush)" : string.Empty)}");

            if (shouldFlush)
            {
                ClearCurrentTimeout();

                State = ExecutionState.Processing;

                while (QueueContainsItems())
                {
                    MakeAndSend1Batch();
                }
            }
            else if (QueueHasBatches())
            {
                ClearCurrentTimeout();

                State = ExecutionState.Processing;

                while (QueueHasBatches())
                {
                    MakeAndSend1Batch();
                }
            }

            State = ExecutionState.Running;
            SetNewTimeout();
        }

        /// <summary>
        /// Make a single batch and send it
        /// </summary>
        private void MakeAndSend1Batch()
        {
            var batch = new List<OdpEvent>(_batchSize);

            // remove a batch from the queue
            for (int i = 0; i < _batchSize && _queue.Count > 0; i += 1)
            {
                if (_queue.TryDequeue(out OdpEvent dequeuedOdpEvent))
                {
                    batch.Add(dequeuedOdpEvent);
                }
            }

            if (batch.Count > 0)
            {
                Task.Run(() =>
                {
                    var shouldRetry = false;
                    var attemptNumber = 0;
                    do
                    {
                        shouldRetry = _odpEventApiManager.SendEvents(_odpConfig.ApiKey,
                            _odpConfig.ApiHost, batch);
                        attemptNumber += 1;
                    } while (shouldRetry && attemptNumber < MAX_RETRIES);
                });
            }
        }

        /// <summary>
        /// Start a new timer to begin the next flush interval
        /// </summary>
        private void SetNewTimeout()
        {
            _timeoutToken = new CancellationTokenSource();
            var ct = _timeoutToken.Token;
            Task.Run(() =>
            {
                Thread.Sleep(_flushInterval);
                if (!ct.IsCancellationRequested)
                    ProcessQueue(true);
            }, ct);
        }

        /// <summary>
        /// Clear the running timer/flush interval
        /// </summary>
        private void ClearCurrentTimeout()
        {
            _timeoutToken.Cancel();
            _timeoutToken = new CancellationTokenSource();
        }

        /// <summary>
        /// Determines if the ODP configuration is ready
        /// </summary>
        /// <returns>True if required parameters are available otherwise False</returns>
        private bool IsOdpConfigurationReady()
        {
            if (_odpConfig.IsReady())
            {
                return true;
            }

            _logger.Log(LogLevel.WARN,
                "Unable to Process ODP Event. ODPConfig not ready. Discarding events in queue.");
            _queue = new ConcurrentQueue<OdpEvent>();

            return false;
        }

        /// <summary>
        /// Queue count has enough items to send at least one batche
        /// </summary>
        /// <returns>True if even batches exist otherwise False</returns>
        private bool QueueHasBatches()
        {
            return QueueContainsItems() && _queue.Count % _batchSize == 0;
        }

        /// <summary>
        /// Queue contains a items
        /// </summary>
        /// <returns>True if count is > 0 otherwise False</returns>
        private bool QueueContainsItems()
        {
            return _queue.Count > 0;
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
                !_validOdpDataTypes.Contains(item.Value.GetType().Name)
            );
        }

        /// <summary>
        /// Add additional common data including an idempotent ID and execution context to event data
        /// </summary>
        /// <param name="sourceData">Existing event data to augment</param>
        /// <returns>Updated Dictionary with new key-values added</returns>
        private static Dictionary<string, dynamic> AugmentCommonData(
            Dictionary<string, dynamic> sourceData
        )
        {
            var commonData = new Dictionary<string, dynamic>
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

            return commonData.MergeInPlace(sourceData);
        }
    }
}
