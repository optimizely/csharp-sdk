/* 
 * Copyright 2019-2020, Optimizely
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
        public string SDKKey => (string)base["sdkKey"];

        [ConfigurationProperty("url")]
        public string Url => (string)base["url"];

        [ConfigurationProperty("format")]
        public string Format => (string)base["format"];

        [ConfigurationProperty("pollingInterval")]
        public int PollingInterval =>
            base["pollingInterval"] is int ? (int)base["pollingInterval"] : 0;

        [ConfigurationProperty("blockingTimeOutPeriod")]
        public int BlockingTimeOutPeriod =>
            base["blockingTimeOutPeriod"] is int ? (int)base["blockingTimeOutPeriod"] : 0;

        [ConfigurationProperty("autoUpdate")]
        public bool AutoUpdate => (bool)base["autoUpdate"];

        [ConfigurationProperty("defaultStart")]
        public bool DefaultStart => (bool)base["defaultStart"];

        [ConfigurationProperty("datafileAccessToken")]
        public string DatafileAccessToken => (string)base["datafileAccessToken"];
    }

    public class BatchEventProcessorElement : ConfigurationElement
    {
        [ConfigurationProperty("batchSize")]
        public int BatchSize => (int)base["batchSize"];

        [ConfigurationProperty("flushInterval")]
        public int FlushInterval => base["flushInterval"] is int ? (int)base["flushInterval"] : 0;

        [ConfigurationProperty("timeoutInterval")]
        public int TimeoutInterval =>
            base["timeoutInterval"] is int ? (int)base["timeoutInterval"] : 0;

        [ConfigurationProperty("defaultStart")]
        public bool DefaultStart => (bool)base["defaultStart"];
    }

    public class OptimizelySDKConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("HttpProjectConfig")]
        public HttpProjectConfigElement HttpProjectConfig
        {
            get => (HttpProjectConfigElement)base["HttpProjectConfig"];
            set => base["HttpProjectConfig"] = value;
        }

        [ConfigurationProperty("BatchEventProcessor")]
        public BatchEventProcessorElement BatchEventProcessor
        {
            get => (BatchEventProcessorElement)base["BatchEventProcessor"];
            set => base["BatchEventProcessor"] = value;
        }
    }
}
