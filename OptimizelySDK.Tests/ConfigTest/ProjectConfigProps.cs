using System;
using OptimizelySDK.Config;
using OptimizelySDK.Tests.Utils;

namespace OptimizelySDK.Tests.ConfigTest
{
    public class ProjectConfigManagerProps
    {
        public string LastModified { get; set; }
        public string Url { get; set; }
        public string DatafileAccessToken {get; set;}
        public TimeSpan PollingInterval { get; set; }
        public TimeSpan BlockingTimeout { get; set; }
        public bool AutoUpdate { get; set; }

        public ProjectConfigManagerProps(HttpProjectConfigManager projectConfigManager)
        {
            LastModified = Reflection.GetFieldValue<string, HttpProjectConfigManager>(projectConfigManager, "LastModified");
            Url = Reflection.GetFieldValue<string, HttpProjectConfigManager>(projectConfigManager, "Url");
            DatafileAccessToken = Reflection.GetFieldValue<string, HttpProjectConfigManager>(projectConfigManager, "DatafileAccessToken");
            AutoUpdate = Reflection.GetFieldValue<bool, HttpProjectConfigManager>(projectConfigManager, "AutoUpdate");

            PollingInterval = Reflection.GetFieldValue<TimeSpan, HttpProjectConfigManager>(projectConfigManager, "PollingInterval");
            BlockingTimeout = Reflection.GetFieldValue<TimeSpan, HttpProjectConfigManager>(projectConfigManager, "BlockingTimeout");
        }
        public ProjectConfigManagerProps()
        {

        }
    }
}
