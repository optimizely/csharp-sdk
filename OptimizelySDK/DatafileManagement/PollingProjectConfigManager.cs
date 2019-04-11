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
using OptimizelySDK.ErrorHandler;

namespace OptimizelySDK.DatafileManagement
{
    public class PollingProjectConfigManager : Timer, ProjectConfigManager
    {
        private bool isStarted = false;
        private bool autoUpdate = false;
        private ProjectConfig currentProjectConfig = null;
        private PollingProjectConfigManager ConfigManager;

        protected ILogger Logger { get; set; }
        protected IErrorHandler ErrorHandler { get; set; }

        //public delegate void ReadyCallback();
        //public ReadyCallback ReadyHandler;
        //public delegate void EventHandler();
        //public event EventHandler UpdateHandler;

        protected Task datafileUpdaterTask;

        public PollingProjectConfigManager(TimeSpan period, bool autoUpdate, ILogger logger = null, IErrorHandler errorHandler = null, PollingProjectConfigManager configManager = null)
        {
            this.autoUpdate = autoUpdate;
            Interval = period.TotalMilliseconds;
            Elapsed += Run;

            ConfigManager = configManager;

            // Setting AutoReset and Enabled to autoUpdate value for firing event.
            AutoReset = autoUpdate;
            Enabled = autoUpdate;
        }

        public new void Stop() 
        {
            isStarted = false;
            base.Stop();
        }

        public ProjectConfig GetConfig()
        {
            if (isStarted) 
            {
                try
                {
                    datafileUpdaterTask.Wait();
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.WARN, ex.Message);
                }

                return currentProjectConfig;
            }

            // Block execution
            ProjectConfig projectConfig = FetchConfig();

            // Update datafile if modified.
            if (projectConfig != null)
                SetConfig(projectConfig);

            return projectConfig;
        }

        public bool SetConfig(ProjectConfig projectConfig)
        {
            if (projectConfig == null)
                return false;

            var previousVersion = currentProjectConfig == null ? "null" : currentProjectConfig.Revision;
            if (projectConfig.Revision == previousVersion)
                return false;
            
            currentProjectConfig = projectConfig;
            return true;
        }

        public virtual ProjectConfig FetchConfig()
        {
            return ConfigManager.FetchConfig();
        }

        public virtual void Run(object sender, ElapsedEventArgs e)
        {
            // 4.0 < frameworks
            datafileUpdaterTask = new Task(() => {
                // if not modified, no need to send
                var config = FetchConfig();
                if (config != null)
                {
                    SetConfig(config);
                }
            });
            
            if (!isStarted)
                isStarted = true;

            datafileUpdaterTask.Start();
        }
    }
}
