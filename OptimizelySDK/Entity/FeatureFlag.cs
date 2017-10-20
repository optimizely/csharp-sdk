﻿/* 
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
    public class FeatureFlag : IdKeyEntity
    {
        public string RolloutId { get; set; }

        public List<string> ExperimentIds { get; set; }

        private List<FeatureVariable> _Variables;
        public List<FeatureVariable> Variables
        {
            get
            {
                return _Variables;
            }
            set
            {
                _Variables = value;

                // Generating Feature Variable key map.
                if (_Variables != null)
                    VariableKeyToFeatureVariableMap = ConfigParser<FeatureVariable>.GenerateMap(entities: _Variables, getKey: v => v.Key, clone: true);
            }
        }
        
        public Dictionary<string, FeatureVariable> VariableKeyToFeatureVariableMap { get; set; }
    }
}
