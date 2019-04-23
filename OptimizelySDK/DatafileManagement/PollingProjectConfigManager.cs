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
using System.Timers;
using System.Threading.Tasks;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;

namespace OptimizelySDK.DatafileManagement
{
    public abstract class PollingProjectConfigManager : ProjectConfigManager
    {
        private ProjectConfig CurrentProjectConfig;
        private Timer SchedulerService;
        private double PollingIntervalMS;
        private TaskCompletionSource<ProjectConfigManager> CompletableConfigManager = new TaskCompletionSource<ProjectConfigManager>();

        protected ILogger Logger { get; set; }
        public bool IsStarted { get; set; }
        
        public PollingProjectConfigManager(TimeSpan period, ILogger logger = null)
        {
            Logger = logger;
            PollingIntervalMS = period.TotalMilliseconds;

            SchedulerService = new Timer(PollingIntervalMS);
            SchedulerService.AutoReset = true;
            SchedulerService.Elapsed += Run;

            Start();
        }

        protected abstract ProjectConfig Poll();

        public void Start()
        {
            if (IsStarted)
            {
                Logger.Log(LogLevel.WARN, "Manager already started.");
                return;
            }

            Logger.Log(LogLevel.WARN, $"Starting Config scheduler with interval: {PollingIntervalMS} milliseconds.");
            SchedulerService.Start();
            IsStarted = true;
        }

        public void Stop() 
        {
            SchedulerService.Stop();
            IsStarted = false;
            Logger.Log(LogLevel.WARN, $"Stopping Config scheduler.");
        }

        public ProjectConfig GetConfig()
        {
            if (IsStarted)
            {
                try
                {
                    var result = CompletableConfigManager.Task.Result;
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
            if (projectConfig == null)
                return false;

            var previousVersion = CurrentProjectConfig == null ? "null" : CurrentProjectConfig.Revision;
            if (projectConfig.Revision == previousVersion)
                return false;
            
            CurrentProjectConfig = projectConfig;
            CompletableConfigManager.SetResult(this);
            return true;
        }
        
        public virtual void Run(object sender, ElapsedEventArgs e)
        {
            var config = Poll();
            SetConfig(config);
        }
    }
}
