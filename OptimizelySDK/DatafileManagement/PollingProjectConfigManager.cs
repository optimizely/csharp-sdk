using System;
using System.Timers;
using System.Threading.Tasks;

namespace OptimizelySDK.DatafileManagement
{
    public abstract class PollingProjectConfigManager : Timer, ProjectConfigManager
    {
        protected Task _onReady;
        private bool isStarted = false;
        private bool autoUpdate = false;
        private ProjectConfig currentProjectConfig = null;

        public delegate void EventHandler();
        public event EventHandler UpdateHandler;

        public PollingProjectConfigManager(TimeSpan period, bool autoUpdate)
        {
            //this.AutoReset = autoUpdate;
            // When app finishes task then start polling,
            // not at fixed interval
            this.autoUpdate = autoUpdate;
            this.Interval = period.TotalMilliseconds;
            this.AutoReset = false;
            this.Elapsed += Run;
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

            return projectConfig == null ? currentProjectConfig : projectConfig;
        }

        public bool SetConfig(ProjectConfig projectConfig)
        {
            if (projectConfig == null) {
                return false;
            }

            //String previousVersion = currentProjectConfig.get() == null ? "null" : currentProjectConfig.get().getRevision();

            //if (projectConfig.getRevision().equals(previousVersion)) {
            //    return false;
            //}

            //logger.info("New datafile set with revision: {}. Old version: {}", projectConfig.getRevision(), previousVersion);

            currentProjectConfig = projectConfig;

            return true;
        }

        protected abstract ProjectConfig FetchConfig();

        private void Run(object sender, ElapsedEventArgs e)
        {
            // 4.0 < frameworks
            Task t = new Task(() => {
                // if not modified, no need to send
                var fetchConfig = this.FetchConfig();
                if (fetchConfig == null) return;
                this.SetConfig(fetchConfig);
            });

            t.ContinueWith((arg) => {
                if(autoUpdate) Start();
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
