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

using OptimizelySDK.Entity;

namespace OptimizelySDK.OptlyConfig
{
    public class OptimizelyVariable : Entity.IdKeyEntity
    {
        public string Type { get; private set; }
        public string Value { get; private set; }

        public OptimizelyVariable(string id, string key, string type, string value)
        {
            Id = id;
            Key = key;
            Type = type;
            Value = value;
        }

        public OptimizelyVariable(FeatureVariable featureVariable, FeatureVariableUsage featureVariableUsage)
        {
            Id = featureVariable.Id;
            Key = featureVariable.Key;
            Type = featureVariable.Type.ToString().ToLower();
            Value = featureVariableUsage?.Value ?? featureVariable.DefaultValue;
        }


        public static explicit operator OptimizelyVariable(FeatureVariable featureVariable)
        {
            return new OptimizelyVariable(featureVariable, null);            
        }
    }
}
