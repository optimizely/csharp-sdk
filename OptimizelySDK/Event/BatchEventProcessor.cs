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
using System.Threading.Tasks;
using System.Diagnostics;

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
    public class BatchEventProcessor: EventProcessor, IDisposable {

        // Don't make it static. make it const. 
        private static object SHUTDOWN_SIGNAL = new object();
        private static object FLUSH_SIGNAL = new object();

        public bool Disposed { get; private set; }

        private TimeSpan FlushInterval;
        private int BatchSize;

        public bool IsStarted { get; private set; }
        
        private Task Executer;
        private Stopwatch StopWatch = new Stopwatch();

        protected ILogger Logger { get; set; }
        protected IErrorHandler ErrorHandler { get; set; }
        public static object mutex = new object();
        public TimeSpan WaitingTimeout { get; set; }

        private IEventDispatcher EventDispatcher;
        BlockingCollection<object> EventQueue; 
        private List<UserEvent> CurrentBatch = new List<UserEvent>();


        // Variables to control blocking/syncing.
        public int resourceInUse = 0;

        public BatchEventProcessor() {
        }
        /// <summary>
        ///  This doesn't fit here, please copy Java sdk code.
        /////Thread t = new Thread(Run);
        /////t.Start();
        /// </summary>
        public void Start()
        {
            if (IsStarted && !Disposed)
            {
                Logger.Log(LogLevel.WARN, "Service already started.");
                return;
            }

            StopWatch.Start();
            Executer = Task.Factory.StartNew(() => Run());
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
                // we don't really need of these stuff.
                if (Interlocked.Exchange(ref resourceInUse, 1) == 0)
                {
                    try
                    {
                        while (true)
                        {
                            if (StopWatch.ElapsedMilliseconds > FlushInterval.Milliseconds)
                            {
                                Logger.Log(LogLevel.DEBUG, "Deadline exceeded flushing current batch.");
                                Flush();
                            }

                            // Consume the BlockingCollection
                            // Specify timeout
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
                                Flush();
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

                    Logger.Log(LogLevel.DEBUG, "Deadline exceeded flushing current batch.");
                    Flush();
                }
            }
            catch (Exception exception)
            {
                Logger.Log(LogLevel.ERROR, "Uncaught exception processing buffer. Error: " + exception.GetAllMessages());
            }
            finally
            {
                Logger.Log(LogLevel.INFO, "Exiting processing loop. Attempting to flush pending events.");
                Interlocked.Exchange(ref resourceInUse, 0);
                Flush();
            }
        }
            
        private void Flush()
        {
            if (CurrentBatch.Count == 0)
            {
                return;
            }

            List<UserEvent> toProcessBatch = null;
            // This should be mutex
            lock (mutex) {
                toProcessBatch = new List<UserEvent>(CurrentBatch);
                CurrentBatch.Clear();
            }
            

            LogEvent logEvent = EventFactory.CreateLogEvent(toProcessBatch.ToArray(), Logger);

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
        /// Stops datafile scheduler.
        /// </summary>
        public void Stop()
        {
            if (Disposed) return;

            EventQueue.Add(SHUTDOWN_SIGNAL);

            if (!Executer.Wait(WaitingTimeout))
                Logger.Log(LogLevel.ERROR, $"Timeout exceeded attempting to close for {WaitingTimeout.Milliseconds} ms");

            IsStarted = false;
            Logger.Log(LogLevel.WARN, $"Stopping scheduler.");
        }
        
        public void Process(UserEvent userEvent) {
            Logger.Log(LogLevel.DEBUG, "Received userEvent: " + userEvent);

            if (Disposed) { 
                Logger.Log(LogLevel.WARN, "Executor shutdown, not accepting tasks.");
                return;
            }

            if (EventQueue.TryAdd(userEvent))
            {
                Logger.Log(LogLevel.WARN, "Payload not accepted by the queue.");
            }
        }
        
        private void AddToBatch(UserEvent userEvent) {
            if (ShouldSplit(userEvent))
            {
                Flush();
                CurrentBatch = new List<UserEvent>();
            }

            // Reset the deadline if starting a new batch.
            if (CurrentBatch.Count == 0)
                StopWatch.Restart();

            CurrentBatch.Add(userEvent);
            if (CurrentBatch.Count >= BatchSize) {
                Flush();
            }
        }

        private bool ShouldSplit(UserEvent userEvent) {
            if (CurrentBatch.Count == 0) {
                return false;
            }

            EventContext currentContext = CurrentBatch.Last().Context;
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

            //SchedulerService.Change(-1, -1);
            //SchedulerService.Dispose();
            Disposed = true;
        }

        public class Builder
        {
            private BlockingCollection<object> EventQueue;
            private IEventDispatcher EventDispatcher;
            private int BatchSize;
            private TimeSpan FlushInterval;
            private IErrorHandler ErrorHandler;
            private bool StartByDefault;
            private ILogger Logger;
            private TimeSpan WaitingTimeout;

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

            public Builder WithStartByDefault(bool startByDefault = true)
            {
                StartByDefault = startByDefault;

                return this;
            }

            public Builder WithLogger(ILogger logger = null)
            {
                Logger = logger;

                return this;
            }

            public Builder WithWaitingTimeout(TimeSpan timeout)
            {
                WaitingTimeout = timeout;

                return this;
            }

            /// <summary>
            /// Build BatchEventProcessor instance.
            /// </summary>
            /// <returns>BatchEventProcessor instance</returns>
            public BatchEventProcessor Build()
            {
                var batchEventProcessor = new BatchEventProcessor();
                batchEventProcessor.Logger = Logger;
                batchEventProcessor.ErrorHandler = ErrorHandler;
                batchEventProcessor.EventDispatcher = EventDispatcher;
                batchEventProcessor.FlushInterval = FlushInterval;
                batchEventProcessor.EventQueue = EventQueue;
                batchEventProcessor.BatchSize = BatchSize;
                batchEventProcessor.WaitingTimeout = WaitingTimeout;

                if (StartByDefault)
                    batchEventProcessor.Start();

                return batchEventProcessor;
            }
        }

    }
}
