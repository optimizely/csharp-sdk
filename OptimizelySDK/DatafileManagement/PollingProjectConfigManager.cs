using System;
using System.Timers;

namespace OptimizelySDK.DatafileManagement
{
    public abstract class PollingProjectConfigManager : Timer, ProjectConfigManager
    {
        public ProjectConfig GetConfig()
        {
            return null;
        }

        public bool SetConfig(ProjectConfig projectConfig)
        {
            return false;
        }

        protected abstract ProjectConfig FetchConfig();
    }
}
