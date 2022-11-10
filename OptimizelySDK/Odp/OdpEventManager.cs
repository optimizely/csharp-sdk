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
    public class OdpEventManager : IOdpEventManager, IDisposable
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
        private ExecutionState CurrentState { get; set; } = ExecutionState.Stopped;

        /// <summary>
        /// Object to ensure thread-safe access
        /// </summary>
        private static readonly object lockObject = new object();

        /// <summary>
        /// ODP configuration settings in used
        /// </summary>
        private readonly OdpConfig _odpConfig;

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
        /// Queue for holding all events to be eventually dispatched
        /// </summary>
        private ConcurrentQueue<OdpEvent> _queue;

        /// <summary>
        /// Maximum number of events to process at once
        /// </summary>
        private readonly int _batchSize;

        /// <summary>
        /// Milliseconds between processing all events in queue
        /// </summary>
        private readonly int _flushInterval;

        /// <summary>
        /// Task to regularly flush the queue
        /// </summary>
        private readonly Task _flushQueueRegularly;

        /// <summary>
        /// Identifier to interrupt the flush task
        /// </summary>
        private readonly CancellationTokenSource _flushIntervalCancellation;

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

        public OdpEventManager(OdpConfig odpConfig, IOdpEventApiManager odpEventApiManager,
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
            _flushIntervalCancellation = new CancellationTokenSource();
            _flushQueueRegularly = new Task(RegularlyFlushQueue, _flushIntervalCancellation.Token);
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
            if (!IsOdpConfigurationReady())
            {
                return;
            }

            lock (lockObject)
            {
                CurrentState = ExecutionState.Running;
            }

            _flushQueueRegularly.Start();
        }

        /// <summary>
        /// Drain the queue sending all remaining events in batches then stop processing
        /// </summary>
        public void Stop()
        {
            if (!IsOdpConfigurationReady())
            {
                return;
            }

            if (CurrentState == ExecutionState.Stopped)
            {
                return;
            }

            _logger.Log(LogLevel.DEBUG, "Stop requested.");

            _flushIntervalCancellation.Cancel();
            try
            {
                _flushQueueRegularly.ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // exception raised by design from .Cancel()
                _logger.Log(LogLevel.DEBUG, "Cancel requested successfully.");
            }
            catch (Exception ex)
            {
                // other exceptions should be ignored
                _logger.Log(LogLevel.ERROR, ex.Message);
            }
            finally
            {
                _flushIntervalCancellation.Dispose();
            }

            // one final time
            FlushQueue();

            lock (lockObject)
            {
                CurrentState = ExecutionState.Stopped;
            }

            _logger.Log(LogLevel.DEBUG, $"Stopped. Queue Count: {_queue.Count}.");
        }

        /// <summary>
        /// Associate a full-stack userid with an established VUID
        /// </summary>
        /// <param name="userId">Full-stack User ID</param>
        public void IdentifyUser(string userId)
        {
            if (!IsOdpConfigurationReady())
            {
                return;
            }

            var identifiers = new Dictionary<string, string>
            {
                {
                    OdpUserKeyType.FS_USER_ID.ToString(), userId
                },
            };

            var odpEvent = new OdpEvent(TYPE, "identified", identifiers);
            SendEvent(odpEvent);
        }

        /// <summary>
        /// Send an event to ODP
        /// </summary>
        /// <param name="odpEvent">ODP Event to forward</param>
        public void SendEvent(OdpEvent odpEvent)
        {
            if (!IsOdpConfigurationReady())
            {
                return;
            }

            if (CurrentState == ExecutionState.Stopped)
            {
                _logger.Log(LogLevel.WARN, "ODP is not enabled.");
                return;
            }

            if (InvalidDataFound(odpEvent.Data))
            {
                _logger.Log(LogLevel.ERROR, "ODP data is not valid.");
                return;
            }

            odpEvent.Data = AugmentCommonData(odpEvent.Data);
            Enqueue(odpEvent);
        }

        /// <summary>
        /// Add a new ODP event to the queue
        /// </summary>
        /// <param name="odpEvent"></param>
        private void Enqueue(OdpEvent odpEvent)
        {
            if (_queue.Count >= _queueSize)
            {
                _logger.Log(LogLevel.WARN, $"ODP event send failed (queueSize = {_queue.Count}).");
                return;
            }

            _queue.Enqueue(odpEvent);

            ProcessQueue();
        }

        /// <summary>
        /// Process the queue
        /// </summary>
        private void ProcessQueue()
        {
            if (CurrentState != ExecutionState.Running)
            {
                return;
            }

            _logger.Log(LogLevel.DEBUG, $"Processing Queue.");

            lock (lockObject)
            {
                CurrentState = ExecutionState.Processing;
                while (QueueHasBatches())
                {
                    DequeueSendSingleBatch();
                }

                CurrentState = ExecutionState.Running;
            }
        }

        private void RegularlyFlushQueue()
        {
            while (!_flushIntervalCancellation.IsCancellationRequested)
            {
                Task.Delay(_flushInterval).ContinueWith(_ => FlushQueue()).Wait();
            }
        }

        private void FlushQueue()
        {
            if (CurrentState != ExecutionState.Running)
            {
                return;
            }

            _logger.Log(LogLevel.DEBUG, $"Flushing Queue.");

            lock (lockObject)
            {
                CurrentState = ExecutionState.Processing;
                while (QueueContainsItems())
                {
                    DequeueSendSingleBatch();
                }

                CurrentState = ExecutionState.Running;
            }
        }

        /// <summary>
        /// Dequeue a single batch and send it
        /// </summary>
        private void DequeueSendSingleBatch()
        {
            var batch = new List<OdpEvent>(_batchSize);

            // dequeue a batch 
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
                    bool shouldRetry;
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
        /// Determines if the ODP configuration is ready
        /// </summary>
        /// <returns>True if required parameters are available otherwise False</returns>
        private bool IsOdpConfigurationReady()
        {
            if (_odpConfig.IsReady())
            {
                return true;
            }

            _logger.Log(LogLevel.DEBUG, "ODP is not integrated.");

            // ensure empty queue
            _queue = new ConcurrentQueue<OdpEvent>();

            return false;
        }

        /// <summary>
        /// Queue count has enough items to send at least one batch
        /// </summary>
        /// <returns>True if even batches exist otherwise False</returns>
        private bool QueueHasBatches()
        {
            return _queue.Count >= _batchSize;
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

        public void Dispose()
        {
            _flushQueueRegularly?.Dispose();
            _flushIntervalCancellation?.Dispose();
        }
    }
}
