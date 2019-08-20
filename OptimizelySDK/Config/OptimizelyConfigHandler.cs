using System.Configuration;

namespace OptimizelySDK.Config
{
    public class HttpProjectConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("sdkKey", IsRequired = true, IsKey = true)]
        public string SDKKey
        {
            get { return (string)base["sdkKey"]; }
        }

        [ConfigurationProperty("url")]
        public string Url
        {
            get { return (string)base["url"]; }
        }

        [ConfigurationProperty("datafile")]
        public string Datafile
        {
            get { return (string)base["datafile"]; }
        }
        
        [ConfigurationProperty("datafileUrlFormat")]
        public string DatafileUrlFormat
        {
            get { return (string)base["datafileUrlFormat"]; }
        }

        [ConfigurationProperty("pollingIntervalInMs")]
        public int PollingIntervalInMs
        {
            get { return base["pollingIntervalInMs"] is int ? (int)base["pollingIntervalInMs"] : 0; }
        }

        [ConfigurationProperty("blockingTimeOutInMs")]
        public int BlockingTimeOutInMs
        {
            get { return base["blockingTimeOutInMs"] is int ? (int)base["blockingTimeOutInMs"] : 0; }
        }

        [ConfigurationProperty("autoUpdate")]
        public bool AutoUpdate
        {
            get { return (bool)base["autoUpdate"]; }
        }

        [ConfigurationProperty("defaultStart")]
        public bool DefaultStart
        {
            get { return (bool)base["defaultStart"]; }
        }
    }

    public class BatchEventProcessorElement : ConfigurationElement
    {
        [ConfigurationProperty("batchSize")]
        public int BatchSize {
            get { return (int)base["batchSize"]; }
        }

        [ConfigurationProperty("flushIntervalInMs")]
        public double FlushIntervalInMs
        {
            get { return base["flushIntervalInMs"] is int ? (int)base["flushIntervalInMs"] : 0; }
        }

        [ConfigurationProperty("timeoutIntervalInMs")]
        public int TimeoutIntervalInMs
        {
            get { return base["timeoutIntervalInMs"] is int ? (int)base["timeoutIntervalInMs"] : 0; }
        }

        [ConfigurationProperty("defaultStart")]
        public bool DefaultStart {
            get { return (bool)base["defaultStart"]; }
        }
    }

    public class OptimizelySDKConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("HttpProjectConfig")]
        public HttpProjectConfigElement HttpProjectConfig
        {
            get { return ((HttpProjectConfigElement)(base["HttpProjectConfig"])); }
            set { base["HttpProjectConfig"] = value; }
        }

        [ConfigurationProperty("BatchEventProcessor")]
        public BatchEventProcessorElement BatchEventProcessor {
            get { return ((BatchEventProcessorElement)(base["BatchEventProcessor"])); }
            set { base["BatchEventProcessor"] = value; }
        }
    }
}
