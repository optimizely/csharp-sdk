/* 
 * Copyright 2017-2019, Optimizely
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
using Newtonsoft.Json.Linq;
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Entity
{
    public class Audience : Entity
    {
        /// <summary>
        /// Audience ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Audience Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Audience Conditions
        /// </summary>
        public object Conditions { get; set; }

        private ICondition _decodedConditions = null;

        /// <summary>
        /// De-serialized audience conditions
        /// </summary>
        public ICondition ConditionList
        {
            get
            {
                if (Conditions == null)
                    return null;

                if (_decodedConditions == null)
                {
                    if (Conditions is string)
                    {
                        var conditions = JToken.Parse((string)Conditions);
                        _decodedConditions = ConditionParser.ParseConditions(conditions);
                    }
                    else
                    {
                        _decodedConditions = ConditionParser.ParseConditions((JToken)Conditions);
                    }
                }

                return _decodedConditions;
            }
        }

        private string _conditionsString = null;

        /// <summary>
        /// Stringified audience conditions
        /// </summary>
        public string ConditionsString
        {
            get
            {
                if (Conditions == null)
                    return null;

                if (_conditionsString == null)
                {
                    if (Conditions is JToken token)
                        _conditionsString = token.ToString(Formatting.None);
                    else
                        _conditionsString = Conditions.ToString();
                }

                return _conditionsString;
            }
        }
    }
}
