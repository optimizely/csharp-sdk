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
using System.Threading;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;
using System.Threading.Tasks;
using OptimizelySDK.ErrorHandler;

namespace OptimizelySDK.Config
{
    /// <summary>
    /// Abstract class that implements ProjectConfigManager interface and provides 
    /// basic scheduling and caching.
    /// Instances of this class, must implement the <see cref="Poll()"/> method
    /// which is responsible for fetching a given ProjectConfig.
    /// </summary>
    public abstract class PollingProjectConfigManager : ProjectConfigManager
    {
        private TimeSpan PollingInterval;
        public bool IsStarted { get; private set; }
        private bool scheduleWhenFinished = false;
        public bool AutoUpdate { get; private set; }

        private ProjectConfig CurrentProjectConfig;
        private Timer SchedulerService;
        protected ILogger Logger { get; set; }
        protected IErrorHandler ErrorHandler { get; set; }
        protected TimeSpan BlockingTimeout;
        protected TaskCompletionSource<bool> CompletableConfigManager = new TaskCompletionSource<bool>();
        // Variables to control blocking/syncing.
        public int resourceInUse = 0;

        public event Action NotifyOnProjectConfigUpdate;

        public PollingProjectConfigManager(TimeSpan period, TimeSpan blockingTimeout, bool autoUpdate = false, ILogger logger = null, IErrorHandler errorHandler = null)
        {
            Logger = logger;
            ErrorHandler = errorHandler;
            BlockingTimeout = blockingTimeout;
            PollingInterval = period;
            AutoUpdate = autoUpdate;

            // Never start, start only when Start is called.
            SchedulerService = new Timer((object state) => { Run(); }, this, -1, -1);
        }

        /// <summary>
        /// Abstract method for fetching ProjectConfig instance.
        /// </summary>
        /// <returns>ProjectConfig instance</returns>
        protected abstract ProjectConfig Poll();

        /// <summary>
        /// Starts datafile scheduler.
        /// </summary>
        public void Start()
        {
            if (IsStarted)
            {
                Logger.Log(LogLevel.WARN, "Manager already started.");
                return;
            }

            Logger.Log(LogLevel.WARN, $"Starting Config scheduler with interval: {PollingInterval}.");
            SchedulerService.Change(TimeSpan.Zero, AutoUpdate ? PollingInterval : TimeSpan.FromMilliseconds(-1));
            IsStarted = true;
        }

        /// <summary>
        /// Stops datafile scheduler.
        /// </summary>
        public void Stop() 
        {
            // don't call now and onwards.
            SchedulerService.Change(-1, -1);

            IsStarted = false;
            Logger.Log(LogLevel.WARN, $"Stopping Config scheduler.");
        }

        /// <summary>
        /// Retrieve ProjectConfig instance and waits untill the instance
        /// gets available or blocking timeout expires.
        /// </summary>
        /// <returns>ProjectConfig</returns>
        public ProjectConfig GetConfig()
        {
            if (IsStarted)
            {
                try
                {
                    bool isCompleted = CompletableConfigManager.Task.Wait(BlockingTimeout);
                    if (!isCompleted)
                    {
                        // Don't wait next time.
                        BlockingTimeout = TimeSpan.FromMilliseconds(0);
                        Logger.Log(LogLevel.WARN, "Timeout exceeded waiting for ProjectConfig to be set, returning null.");
                    }
                }
                catch (AggregateException ex)
                {
                    Logger.Log(LogLevel.WARN, "Interrupted waiting for valid ProjectConfig. Error: " + ex.GetAllMessages());
                    throw;
                }

                return CurrentProjectConfig;
            }

            var projectConfig = Poll();
            return projectConfig ?? CurrentProjectConfig;
        }

        /// <summary>
        /// Sets the latest available ProjectConfig valid instance.
        /// </summary>
        /// <param name="projectConfig">ProjectConfig</param>
        /// <returns>true if the ProjectConfig saved successfully, false otherwise</returns>
        public bool SetConfig(ProjectConfig projectConfig)
        {
            // trigger now, due because of delayed latency response
            if (scheduleWhenFinished && IsStarted) {
                // Can't directly call Run, it will be part of previous thread then.
                // Call immediately, because it's due now.
                scheduleWhenFinished = false;
                SchedulerService.Change(TimeSpan.FromSeconds(0), PollingInterval);
            }

            if (projectConfig == null)
                return false;
                
            var previousVersion = CurrentProjectConfig == null ? "null" : CurrentProjectConfig.Revision;
            if (projectConfig.Revision == previousVersion)
                return false;
            
            CurrentProjectConfig = projectConfig;

            // SetResult raise exception if called again, that's why Try is used.
            CompletableConfigManager.TrySetResult(true);
            NotifyOnProjectConfigUpdate?.Invoke();

            return true;
        }

        public void Dispose()
        {
            SchedulerService.Dispose();
        }

        /// <summary>
        /// Scheduler method that periodically runs on provided
        /// polling interval.
        /// </summary>
        public virtual void Run()
        {
            if (Interlocked.Exchange(ref resourceInUse, 1) == 0){
                try {
                    var config = Poll();

                    // during in-flight, if PollingProjectConfigManagerStopped, then don't need to set.
                    if(IsStarted)
                        SetConfig(config);

                } catch (Exception exception) {
                    Logger.Log(LogLevel.ERROR, "Unable to get project config. Error: " + exception.Message);
                } finally {
                    Interlocked.Exchange(ref resourceInUse, 0);
                }
            }
            else {
                scheduleWhenFinished = true;
            }
        }
    }
}
