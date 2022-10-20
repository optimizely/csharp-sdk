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
        private const string TYPE = "fullstack";

        private ExecutionState State { get; set; } = ExecutionState.Stopped;

        private CancellationTokenSource _timeoutToken;

        private readonly IOdpConfig _odpConfig;
        private readonly IRestApiManager _apiManager;
        private readonly ILogger _logger;
        private readonly int _queueSize;
        private readonly int _batchSize;
        private readonly int _flushInterval;

        private readonly List<string> _validOdpDataTypes = new List<string>()
        {
            "String",
            "Int16",
            "Int32",
            "Decimal",
            "Char",
            "Double",
            "Boolean",
        };

        private ConcurrentQueue<OdpEvent> _queue;

        public OdpEventManager(IOdpConfig odpConfig, IRestApiManager apiManager, ILogger logger,
            int queueSize = DEFAULT_SERVER_QUEUE_SIZE, int batchSize = DEFAULT_BATCH_SIZE,
            int flushInterval = DEFAULT_FLUSH_INTERVAL_MSECS
        )
        {
            _odpConfig = odpConfig;
            _apiManager = apiManager;
            _logger = logger;
            _queueSize = queueSize;
            _batchSize = batchSize;
            _flushInterval = flushInterval;

            _queue = new ConcurrentQueue<OdpEvent>();
            _timeoutToken = new CancellationTokenSource();
        }

        public void UpdateSettings(OdpConfig odpConfig)
        {
            _odpConfig.Update( odpConfig.ApiKey, odpConfig.ApiHost, odpConfig.SegmentsToCheck);
        }

        public void Start()
        {
            State = ExecutionState.Running;
        }

        public void Stop()
        {
            _logger.Log(LogLevel.DEBUG, "Stop requested");

            // process queue with flush
            ProcessQueue(true);

            State = ExecutionState.Stopped;
            _logger.Log(LogLevel.DEBUG, $"Stopped. Queue Count: {_queue.Count}");
        }

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

        public void IdentifyUser(string userId, string vuid)
        {
            var identifiers = new Dictionary<string, string>
            {
                {
                    OdpUserKeyType.FS_USER_KEY.ToString(), userId
                },
            };

            if (!string.IsNullOrWhiteSpace(vuid))
            {
                identifiers.Add(OdpUserKeyType.VUID.ToString(), vuid);
            }

            var odpEvent = new OdpEvent(TYPE, "identified", identifiers);
            SendEvent(odpEvent);
        }

        public void SendEvent(OdpEvent odpEvent)
        {
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

        private void Enqueue(OdpEvent odpEvent)
        {
            if (State == ExecutionState.Stopped)
            {
                _logger.Log(LogLevel.WARN,
                    "Failed to Process ODP Event. ODPEventManager is not running.");
                return;
            }

            if (!_odpConfig.IsReady())
            {
                _logger.Log(LogLevel.DEBUG, "Unable to Process ODP Event. ODPConfig is not ready.");
                return;
            }

            if (_queue.Count >= _queueSize)
            {
                _logger.Log(LogLevel.WARN,
                    $"Failed to Process ODP Event. Event Queue full. queueSize = {_queue.Count}.");
                return;
            }

            _queue.Enqueue(odpEvent);

            ProcessQueue();
        }

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
                Task.Run(async () =>
                {
                    var shouldRetry = false;
                    var attemptNumber = 0;
                    do
                    {
                        shouldRetry = await _apiManager.SendEvents(_odpConfig.ApiKey,
                            _odpConfig.ApiHost, batch);
                        attemptNumber += 1;
                    } while (shouldRetry && attemptNumber < MAX_RETRIES);
                }).Start();
            }
        }

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

        private void ClearCurrentTimeout()
        {
            _timeoutToken.Cancel();
            _timeoutToken = new CancellationTokenSource();
        }

        private bool IsOdpConfigurationReady()
        {
            if (_odpConfig.IsReady())
            {
                return true;
            }

            _logger.Log(LogLevel.WARN, "ODPConfig not ready. Discarding events in queue.");
            _queue = new ConcurrentQueue<OdpEvent>();

            return false;
        }

        private bool QueueHasBatches()
        {
            return QueueContainsItems() && _queue.Count % _batchSize == 0;
        }

        private bool QueueContainsItems()
        {
            return _queue.Count > 0;
        }

        private bool InvalidDataFound(Dictionary<string, dynamic> data)
        {
            return data.Any(item =>
                item.Value != null &&
                !_validOdpDataTypes.Contains(item.Value.GetType().Name)
            );
        }

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
