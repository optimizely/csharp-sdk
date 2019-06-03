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
    public abstract class PollingProjectConfigManager : ProjectConfigManager
    {
        private TimeSpan PollingInterval;
        public bool IsStarted { get; private set; }
        private bool scheduleWhenFinished = false;

        private ProjectConfig CurrentProjectConfig;
        private Timer SchedulerService;

        protected ILogger Logger { get; set; }
        protected IErrorHandler ErrorHandler { get; set; }
        protected TimeSpan BlockingTimeout;
        protected TaskCompletionSource<bool> CompletableConfigManager = new TaskCompletionSource<bool>();
        // Variables to control blocking/syncing.
        public object mutex = new Object();

        protected event Action<ProjectConfig> DatafileUpdate_Notification;

        public PollingProjectConfigManager(TimeSpan period, TimeSpan blockingTimeout, ILogger logger = null, IErrorHandler errorHandler = null, bool StartByDefault = true)
        {
            Logger = logger;
            ErrorHandler = errorHandler;
            BlockingTimeout = blockingTimeout;
            PollingInterval = period;


            // Never start, start only when Start is called.
            SchedulerService = new Timer((object state) => { Run(); }, this, -1, -1);
            if(StartByDefault) {
                Start();
            }

        }

        protected abstract ProjectConfig Poll();

        public void Start()
        {
            if (IsStarted)
            {
                Logger.Log(LogLevel.WARN, "Manager already started.");
                return;
            }

            Logger.Log(LogLevel.WARN, $"Starting Config scheduler with interval: {PollingInterval} milliseconds.");
            SchedulerService.Change(TimeSpan.Zero, PollingInterval);
            IsStarted = true;
        }

        public void Stop() 
        {
            // don't call now and onwards.
            SchedulerService.Change(-1, -1);

            IsStarted = false;
            Logger.Log(LogLevel.WARN, $"Stopping Config scheduler.");
        }

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

        public bool SetConfig(ProjectConfig projectConfig)
        {
            // trigger now, due because of delayed latency response
            if(scheduleWhenFinished && IsStarted) {
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

            DatafileUpdate_Notification?.Invoke(projectConfig);

            return true;
        }
        
        public virtual void Run()
        {
            if (Monitor.TryEnter(mutex)){
                try {
                    var config = Poll();
                    SetConfig(config);
                } catch (Exception exception) {
                    Logger.Log(LogLevel.ERROR, "Unable to get project config. Error: " + exception.Message);
                } finally {
                    Monitor.Exit(mutex);
                }
            }
            else {
                scheduleWhenFinished = true;
            }
        }
    }
}
