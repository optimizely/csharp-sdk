﻿/* 
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

using Newtonsoft.Json;
using System.Collections.Generic;

namespace OptimizelySDK.OptlyConfig
{
    public class OptimizelyConfig
    {
        public string Revision { get; private set; }
        public IDictionary<string, OptimizelyExperiment> ExperimentsMap { get; private set; }
        public IDictionary<string, OptimizelyFeature> FeaturesMap { get; private set; }

        [JsonIgnore]
        public string Datafile { get; private set; }

        public OptimizelyConfig(string revision, IDictionary<string, OptimizelyExperiment> experimentsMap, IDictionary<string, OptimizelyFeature> featuresMap, string datafile = null)
        {
            Revision = revision;
            ExperimentsMap = experimentsMap;
            FeaturesMap = featuresMap;
            Datafile = datafile;
        }
    }
}
