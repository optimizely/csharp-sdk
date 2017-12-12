/* 
 * Copyright 2017, Optimizely
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
using OptimizelySDK.Utils;
using System.Collections.Generic;

namespace OptimizelySDK.Entity
{
    public class Variation : IdKeyEntity
    {
        private List<FeatureVariableUsage> _FeatureVariableUsageInstances;
        [Newtonsoft.Json.JsonProperty("variables")]
        public List<FeatureVariableUsage> FeatureVariableUsageInstances
        {
            get
            {
                return _FeatureVariableUsageInstances;
            }
            set
            {
                _FeatureVariableUsageInstances = value;

                // Generating Variable Usage key map.
                if (_FeatureVariableUsageInstances != null)
                    VariableIdToVariableUsageInstanceMap = ConfigParser<FeatureVariableUsage>.GenerateMap(entities: _FeatureVariableUsageInstances, getKey: v => v.Id.ToString(), clone: true);
            }
        }

        public Dictionary<string, FeatureVariableUsage> VariableIdToVariableUsageInstanceMap { get; set; }
    }
}
