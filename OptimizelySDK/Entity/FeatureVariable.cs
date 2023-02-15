/* 
 * Copyright 2017, 2019-2020, Optimizely
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

namespace OptimizelySDK.Entity
{
    public class FeatureVariable : IdKeyEntity
    {
        public const string STRING_TYPE = "string";
        public const string INTEGER_TYPE = "integer";
        public const string DOUBLE_TYPE = "double";
        public const string BOOLEAN_TYPE = "boolean";
        public const string JSON_TYPE = "json";

        public enum VariableStatus
        {
            ACTIVE,
            ARCHIVED,
        }


        public string DefaultValue { get; set; }

        private string _subType;

        public string SubType
        {
            get => _subType;
            set => _subType = value;
        }

        private string _type;

        public string Type
        {
            get
            {
                if (_type == STRING_TYPE && _subType == JSON_TYPE)
                {
                    return JSON_TYPE;
                }

                return _type;
            }
            set => _type = value;
        }

        public VariableStatus Status { get; set; }

        /// <summary>
        /// Returns the feature variable api name based on VariableType.
        /// </summary>
        /// <returns>The feature variable type name.</returns>
        /// <param name="variableType">Variable type.</param>
        public static string GetFeatureVariableTypeName(string variableType)
        {
            switch (variableType)
            {
                case BOOLEAN_TYPE:
                    return "GetFeatureVariableBoolean";
                case DOUBLE_TYPE:
                    return "GetFeatureVariableDouble";
                case INTEGER_TYPE:
                    return "GetFeatureVariableInteger";
                case STRING_TYPE:
                    return "GetFeatureVariableString";
                case JSON_TYPE:
                    return "GetFeatureVariableJSON";
                default:
                    return null;
            }
        }
    }
}
