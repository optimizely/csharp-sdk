/* 
 * Copyright 2019, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
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
using OptimizelySDK.Event.Entity;
using System.Threading;
using OptimizelySDK.Utils;
using OptimizelySDK.Logger;
using OptimizelySDK.ErrorHandler;
using System.Linq;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Notifications;

namespace OptimizelySDK.Event
{
    /**
     * BatchEventProcessor is a batched implementation of the {@link EventProcessor}
     *
     * Events passed to the BatchEventProcessor are immediately added to a BlockingQueue.
     *
     * The BatchEventProcessor maintains a single consumer thread that pulls events off of
     * the BlockingQueue and buffers them for either a configured batch size or for a
     * maximum duration before the resulting LogEvent is sent to the NotificationManager.
     */
    public class BatchEventProcessor: EventProcessor, IDisposable
    {
        private const int DEFAULT_BATCH_SIZE = 10;
        private const int DEFAULT_QUEUE_CAPACITY = 1000;

        private static readonly TimeSpan DEFAULT_FLUSH_INTERVAL = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan DEFAULT_TIMEOUT_INTERVAL = TimeSpan.FromMinutes(5);

        private int BatchSize;
        private TimeSpan FlushInterval;
        private TimeSpan TimeoutInterval;

        private readonly object SHUTDOWN_SIGNAL = new object();
        private readonly object FLUSH_SIGNAL = new object();

        public bool Disposed { get; private set; }

        public bool IsStarted { get; private set; }
        private Thread Executer;
        
        protected ILogger Logger { get; set; }
        protected IErrorHandler ErrorHandler { get; set; }
        public NotificationCenter NotificationCenter { get; set; }

        private readonly object mutex = new object();

        private IEventDispatcher EventDispatcher;
        public BlockingCollection<object> EventQueue { get; private set; } 
        private List<UserEvent> CurrentBatch = new List<UserEvent>();
        private long FlushingIntervalDeadline;
              
        public void Start()
        {
            if (IsStarted && !Disposed)
            {
                Logger.Log(LogLevel.WARN, "Service already started.");
                return;
            }

            FlushingIntervalDeadline = DateTime.Now.MillisecondsSince1970() + (long)FlushInterval.TotalMilliseconds;
            Executer = new Thread(() => Run());
            Executer.Start();
            IsStarted = true;
        }

        /// <summary>
        /// Scheduler method that periodically runs on provided
        /// polling interval.
        /// </summary>
        public virtual void Run()
        {
            try
            {
                while (true)
                {
                    if (DateTime.Now.MillisecondsSince1970() > FlushingIntervalDeadline)
                    {
                        Logger.Log(LogLevel.DEBUG, $"Deadline exceeded flushing current batch, {DateTime.Now.Millisecond}, {FlushingIntervalDeadline}.");
                        FlushQueue();
                    }
                    
                    if (!EventQueue.TryTake(out object item, 50))
                    {
                        Logger.Log(LogLevel.DEBUG, "Empty item, sleeping for 50ms.");
                        Thread.Sleep(50);
                        continue;
                    }

                    if (item == SHUTDOWN_SIGNAL)
                    {
                        Logger.Log(LogLevel.INFO, "Received shutdown signal.");
                        break;
                    }

                    if (item == FLUSH_SIGNAL)
                    {
                        Logger.Log(LogLevel.DEBUG, "Received flush signal.");
                        FlushQueue();
                        continue;
                    }

                    if (item is UserEvent userEvent)
                        AddToBatch(userEvent);
                }
            }
            catch (InvalidOperationException e)
            {
                // An InvalidOperationException means that Take() was called on a completed collection
                Logger.Log(LogLevel.DEBUG, "Unable to take item from eventQueue: " + e.GetAllMessages());
            }
            catch (Exception exception)
            {
                Logger.Log(LogLevel.ERROR, "Uncaught exception processing buffer. Error: " + exception.GetAllMessages());
            }
            finally
            {
                Logger.Log(LogLevel.INFO, "Exiting processing loop. Attempting to flush pending events.");
                FlushQueue();
            }
        }

        public void Flush()
        {
            EventQueue.Add(FLUSH_SIGNAL);
        }

        private void FlushQueue()
        {
            if (CurrentBatch.Count == 0)
            {
                return;
            }

            List<UserEvent> toProcessBatch = null;
            lock (mutex)
            {
                toProcessBatch = new List<UserEvent>(CurrentBatch);
                CurrentBatch.Clear();
            }
            

            LogEvent logEvent = EventFactory.CreateLogEvent(toProcessBatch.ToArray(), Logger);

            NotificationCenter?.SendNotifications(NotificationCenter.NotificationType.LogEvent, logEvent);

            try
            {
                EventDispatcher?.DispatchEvent(logEvent);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.ERROR, "Error dispatching event: " + logEvent + " " + e);
            }
        }

        /// <summary>
        /// Stops batch event processor.
        /// </summary>
        public void Stop()
        {
            if (Disposed) return;

            EventQueue.Add(SHUTDOWN_SIGNAL);
            
            if (!Executer.Join(TimeoutInterval))
                Logger.Log(LogLevel.ERROR, $"Timeout exceeded attempting to close for {TimeoutInterval.Milliseconds} ms");

            IsStarted = false;
            Logger.Log(LogLevel.WARN, $"Stopping scheduler.");
        }
        
        public void Process(UserEvent userEvent) {
            Logger.Log(LogLevel.DEBUG, "Received userEvent: " + userEvent);

            if (Disposed) { 
                Logger.Log(LogLevel.WARN, "Executor shutdown, not accepting tasks.");
                return;
            }

            if (!EventQueue.TryAdd(userEvent))
            {
                Logger.Log(LogLevel.WARN, "Payload not accepted by the queue.");
            }
        }
        
        private void AddToBatch(UserEvent userEvent)
        {
            if (ShouldSplit(userEvent))
            {
                FlushQueue();
                CurrentBatch = new List<UserEvent>();
            }

            // Reset the deadline if starting a new batch.
            if (CurrentBatch.Count == 0)
                FlushingIntervalDeadline = DateTime.Now.MillisecondsSince1970() + (long)FlushInterval.TotalMilliseconds;

            lock (mutex) {
                CurrentBatch.Add(userEvent);
            }
            
            if (CurrentBatch.Count >= BatchSize) {
                FlushQueue();
            }
        }

        private bool ShouldSplit(UserEvent userEvent) {
            if (CurrentBatch.Count == 0) {
                return false;
            }

            EventContext currentContext;
            lock (mutex)
            {
                currentContext = CurrentBatch.Last().Context;
            }

            EventContext newContext = userEvent.Context;

            // Revisions should match
            if (currentContext.Revision != newContext.Revision) {
                return true;
            }

            // Projects should match
            if (currentContext.ProjectId != newContext.ProjectId)
            {
                return true;
            }

            return false;
        }
        
        public void Dispose()
        {
            if (Disposed) return;

            Stop();
            Disposed = true;
        }

        public class Builder
        {
            private BlockingCollection<object> EventQueue = new BlockingCollection<object>(DEFAULT_QUEUE_CAPACITY);

            private IEventDispatcher EventDispatcher;
            private int? BatchSize;
            private TimeSpan? FlushInterval;
            private TimeSpan? TimeoutInterval;
            private IErrorHandler ErrorHandler;
            private ILogger Logger;
            private NotificationCenter NotificationCenter;

            public Builder WithEventQueue(BlockingCollection<object> eventQueue)
            {
                EventQueue = eventQueue;

                return this;
            }

            public Builder WithEventDispatcher(IEventDispatcher eventDispatcher)
            {
                EventDispatcher = eventDispatcher;

                return this;
            }

            public Builder WithMaxBatchSize(int batchSize)
            {
                BatchSize = batchSize;

                return this;
            }

            public Builder WithFlushInterval(TimeSpan flushInterval)
            {
                FlushInterval = flushInterval;

                return this;
            }

            public Builder WithErrorHandler(IErrorHandler errorHandler = null)
            {
                ErrorHandler = errorHandler;

                return this;
            }
            
            public Builder WithLogger(ILogger logger = null)
            {
                Logger = logger;

                return this;
            }

            public Builder WithNotificationCenter(NotificationCenter notificationCenter)
            {
                NotificationCenter = notificationCenter;

                return this;
            }

            public Builder WithTimeoutInterval(TimeSpan timeout)
            {
                TimeoutInterval = timeout;

                return this;
            }

            /// <summary>
            /// Build BatchEventProcessor instance.
            /// </summary>
            /// <returns>BatchEventProcessor instance</returns>
            public BatchEventProcessor Build()
            {
                return Build(true);
            }

            /// <summary>
            /// Build BatchEventProcessor instance.
            /// </summary>
            /// <param name="start">Should start event processor on initializtion</param>
            /// <returns>BatchEventProcessor instance</returns>
            public BatchEventProcessor Build(bool start)
            {
                var batchEventProcessor = new BatchEventProcessor();
                batchEventProcessor.Logger = Logger;
                batchEventProcessor.ErrorHandler = ErrorHandler;
                batchEventProcessor.EventDispatcher = EventDispatcher;
                batchEventProcessor.EventQueue = EventQueue;
                batchEventProcessor.NotificationCenter = NotificationCenter;

                batchEventProcessor.BatchSize = BatchSize ?? BatchEventProcessor.DEFAULT_BATCH_SIZE;
                batchEventProcessor.FlushInterval = FlushInterval ?? BatchEventProcessor.DEFAULT_FLUSH_INTERVAL;
                batchEventProcessor.TimeoutInterval = TimeoutInterval ?? BatchEventProcessor.DEFAULT_TIMEOUT_INTERVAL;

                if (start)
                    batchEventProcessor.Start();

                return batchEventProcessor;
            }
        }

    }
}
