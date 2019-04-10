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
using System.Diagnostics;

namespace OptimizelySDK.DatafileManagement
{
    public abstract class PollingProjectConfigManager : Timer, ProjectConfigManager
    {
        protected Task _onReady;
        private bool isStarted = false;
        private bool autoUpdate = false;
        private ProjectConfig currentProjectConfig = null;
        protected ILogger Logger { get; set; }
        protected IErrorHandler ErrorHandler { get; set; }

        //public delegate void ReadyCallback();
        //public ReadyCallback UpdateHandler;
        public delegate void EventHandler();
        public event EventHandler UpdateHandler;
        
        public PollingProjectConfigManager(TimeSpan period, bool autoUpdate, ILogger logger = null, IErrorHandler errorHandler = null)
        {
            //this.AutoReset = autoUpdate;
            // When app finishes task then start polling,
            // not at fixed interval
            this.autoUpdate = autoUpdate;
            Interval = period.TotalMilliseconds;
            AutoReset = false;
            Elapsed += Run;

            // Setting Enabled to autoUpdate value for firing event.
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
                try {
                    _onReady.Wait();
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                    //logger.warn("Interrupted waiting for valid ProjectConfig");
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

            //String previousVersion = currentProjectConfig.get() == null ? "null" : currentProjectConfig.get().getRevision();

            //if (projectConfig.getRevision().equals(previousVersion)) {
            //    return false;
            //}

            //logger.info("New datafile set with revision: {}. Old version: {}", projectConfig.getRevision(), previousVersion);

            currentProjectConfig = projectConfig;
            return true;
        }

        public abstract ProjectConfig FetchConfig();

        public virtual void Run(object sender, ElapsedEventArgs e)
        {
            // 4.0 < frameworks
            Task t = new Task(() => {
                // if not modified, no need to send
                var config = FetchConfig();
                if (config != null)
                {
                    SetConfig(config);
                }
            });

            t.ContinueWith((arg) => {
                if (autoUpdate)
                    Start();
            });

            if (isStarted == false)
            {
                isStarted = true;
                _onReady = t;
            }

            t.Start();
        }
    }
}
