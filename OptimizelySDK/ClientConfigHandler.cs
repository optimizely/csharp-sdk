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

using System.Configuration;

namespace OptimizelySDK
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
        public int BatchSize
        {
            get { return (int)base["batchSize"]; }
        }

        [ConfigurationProperty("flushIntervalInMs")]
        public int FlushIntervalInMs
        {
            get { return base["flushIntervalInMs"] is int ? (int)base["flushIntervalInMs"] : 0; }
        }

        [ConfigurationProperty("timeoutIntervalInMs")]
        public int TimeoutIntervalInMs
        {
            get { return base["timeoutIntervalInMs"] is int ? (int)base["timeoutIntervalInMs"] : 0; }
        }

        [ConfigurationProperty("defaultStart")]
        public bool DefaultStart
        {
            get { return (bool)base["defaultStart"]; }
        }
    }

    public class OptimizelySDKConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("HttpProjectConfig")]
        public HttpProjectConfigElement HttpProjectConfig
        {
            get { return (HttpProjectConfigElement)base["HttpProjectConfig"]; }
            set { base["HttpProjectConfig"] = value; }
        }

        [ConfigurationProperty("BatchEventProcessor")]
        public BatchEventProcessorElement BatchEventProcessor {
            get { return (BatchEventProcessorElement)(base["BatchEventProcessor"]); }
            set { base["BatchEventProcessor"] = value; }
        }
    }
}
