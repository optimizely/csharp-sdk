﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using OptimizelySDK.Event.Entity;
using System.Threading;
using OptimizelySDK.Utils;
using OptimizelySDK.Logger;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event.internals;
using System.Linq;
using OptimizelySDK.Event.Dispatcher;

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
        
        private static object SHUTDOWN_SIGNAL = new object();

        public bool Disposed { get; private set; }

        private TimeSpan FlushInterval;
        private int BatchSize;

        public bool IsStarted { get; private set; }
        private bool ScheduleWhenFinished = false;
        public bool AutoUpdate { get; private set; }
        private Timer SchedulerService;

        protected ILogger Logger { get; set; }
        protected IErrorHandler ErrorHandler { get; set; }


        private IEventDispatcher EventDispatcher;
        //        BlockingCollection<object> EventQueue; to-do: add supported .net 3.5 lib
        private List<UserEvent> CurrentBatch = new List<UserEvent>();


        // Variables to control blocking/syncing.
        public int resourceInUse = 0;

        private BatchEventProcessor(List<UserEvent> currentBatch, IEventDispatcher eventDispatcher, int batchSize, TimeSpan flushInterval, bool autoUpdate = true, ILogger logger = null, IErrorHandler errorHandler = null) {
            Logger = logger;
            ErrorHandler = errorHandler;
            EventDispatcher = eventDispatcher;
            FlushInterval = flushInterval;
            AutoUpdate = autoUpdate;
            CurrentBatch = currentBatch;
            //EventQueue = eventQueue;
            BatchSize = batchSize;

            // Never start, start only when Start is called.
            SchedulerService = new Timer((object state) => { Run(); }, this, -1, -1);
            Start();
        }

        public void Start()
        {
            if (IsStarted && !Disposed)
            {
                Logger.Log(LogLevel.WARN, "Service already started.");
                return;
            }

            SchedulerService.Change(TimeSpan.Zero, AutoUpdate ? FlushInterval : TimeSpan.FromMilliseconds(-1));
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
                if (Interlocked.Exchange(ref resourceInUse, 1) == 0)
                {
                    Logger.Log(LogLevel.DEBUG, "Deadline exceeded flushing current batch.");
                    Flush();

                    if (CurrentBatch.Count == 0)
                    {
                        Logger.Log(LogLevel.DEBUG, "Empty item, sleeping for 50ms.");
                        Thread.Sleep(50);
                    }

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

            List<UserEvent> toProcessBatch = new List<UserEvent>(CurrentBatch);
            CurrentBatch.Clear(); 

            LogEvent logEvent = EventFactory.CreateLogEvent(toProcessBatch.ToArray(), Logger);

            try
            {
                EventDispatcher.DispatchEvent(logEvent);
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
            // don't call now and onwards.
            SchedulerService.Change(-1, -1);

            IsStarted = false;
            Logger.Log(LogLevel.WARN, $"Stopping scheduler.");
        }
        
        public void Process(object userEvent) {
            Logger.Log(LogLevel.DEBUG, "Received userEvent: " + userEvent);

            if (Disposed) { 
                Logger.Log(LogLevel.WARN, "Executor shutdown, not accepting tasks.");
                return;
            }

            if (userEvent == SHUTDOWN_SIGNAL)
            {
                Logger.Log(LogLevel.INFO, "Received shutdown signal.");
                Stop();
            }

            AddToBatch((UserEvent) userEvent);
        }
        
        private void AddToBatch(UserEvent userEvent) {
            if (ShouldSplit(userEvent))
            {
                Flush();
                CurrentBatch = new List<UserEvent>();
            }

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

            SchedulerService.Change(-1, -1);
            SchedulerService.Dispose();
            Disposed = true;
        }
    }
}
