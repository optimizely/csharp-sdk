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

namespace OptimizelySDK.DatafileManagement
{
    public class PollingProjectConfigManager : ProjectConfigManager
    {
        private ProjectConfig CurrentProjectConfig;
        private ProjectConfigManager ConfigManager;
        private Timer SchedulerService;
        private double PollingIntervalMS;

        private ILogger Logger { get; set; }
        public bool IsStarted { get; set; } = false;
        private TaskCompletionSource<bool> OnReadyFuture = new TaskCompletionSource<bool>();

        public PollingProjectConfigManager(TimeSpan period, ProjectConfigManager configManager = null, ILogger logger = null)
        {
            ConfigManager = configManager;
            PollingIntervalMS = period.TotalMilliseconds;
            Logger = logger;

            SchedulerService = new Timer(PollingIntervalMS);
            SchedulerService.AutoReset = true;
            SchedulerService.Elapsed += Run;

            Start();
        }

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
            return CurrentProjectConfig;
        }

        public bool SetConfig(ProjectConfig projectConfig)
        {
            if (projectConfig == null)
                return false;

            var previousVersion = CurrentProjectConfig == null ? "null" : CurrentProjectConfig.Revision;
            if (projectConfig.Revision == previousVersion)
                return false;
            
            CurrentProjectConfig = projectConfig;
            OnReadyFuture.SetResult(true);
            return true;
        }

        /// <summary>
        /// OnReady future's task that gets completed when ConfigManager
        /// retrieved ProjectConfig for the fist time.
        /// </summary>
        /// <returns>OnReady future's Task</returns>
        public Task<bool> OnReady()
        {
            return OnReadyFuture.Task;
        }
        
        public virtual void Run(object sender, ElapsedEventArgs e)
        {
            var config = ConfigManager.GetConfig();
            if (config != null)
                SetConfig(config);
        }
    }
}
