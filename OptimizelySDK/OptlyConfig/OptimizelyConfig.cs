/* 
 * Copyright 2019-2021, Optimizely
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

using Newtonsoft.Json;
using OptimizelySDK.Entity;
using System.Collections.Generic;

namespace OptimizelySDK.OptlyConfig
{
    public class OptimizelyConfig
    {
        public string Revision { get; private set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SDKKey { get; private set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EnvironmentKey { get; private set; }
        public Entity.Event[] Events { get; private set; }
        public OptimizelyAudience[] Audiences { get; private set; }
        public Attribute[] Attributes { get; private set; }
        public IDictionary<string, OptimizelyExperiment> ExperimentsMap { get; private set; }
        public IDictionary<string, OptimizelyFeature> FeaturesMap { get; private set; }

        private string _datafile;

        public OptimizelyConfig(string revision, IDictionary<string, OptimizelyExperiment> experimentsMap, IDictionary<string, OptimizelyFeature> featuresMap, string datafile = null)
        {
            Revision = revision;
            ExperimentsMap = experimentsMap;
            FeaturesMap = featuresMap;
            _datafile = datafile;
        }

        public OptimizelyConfig(string revision, string sdkKey, string environmentKey, Attribute[] attributes, OptimizelyAudience[] audiences, Entity.Event[] events, IDictionary<string, OptimizelyExperiment> experimentsMap, IDictionary<string, OptimizelyFeature> featuresMap, string datafile = null)
        {
            Revision = revision;
            SDKKey = sdkKey;
            Attributes = attributes;
            Audiences = audiences;
            Events = events;
            EnvironmentKey = environmentKey;
            ExperimentsMap = experimentsMap;
            FeaturesMap = featuresMap;
            _datafile = datafile;
        }

        /// <summary>
        /// Get the datafile associated with OptimizelyConfig.
        /// </summary>
        /// <returns>the datafile string associated with OptimizelyConfig.</returns>
        public string GetDatafile()
        {
            return _datafile;
        }
    }
}
    