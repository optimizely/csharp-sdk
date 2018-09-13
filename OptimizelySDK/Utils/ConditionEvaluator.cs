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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OptimizelySDK.Entity;
using System;
using System.Linq;

namespace OptimizelySDK.Utils
{
    public class ConditionEvaluator
    {
        /// <summary>
        /// const string Representing AND operator.
        /// </summary>
        const string AND_OPERATOR = "and";

        /// <summary>
        /// const string Representing OR operator.
        /// </summary>
        const string OR_OPERATOR = "or";

        /// <summary>
        /// const string Representing NOT operator.
        /// </summary>
        const string NOT_OPERATOR = "not";

        private bool AndEvaluator(JArray conditions, UserAttributes userAttributes)
        {
            return conditions.All(condition => Evaluate(condition, userAttributes));
        }

        private bool OrEvaluator(JArray conditions, UserAttributes userAttributes)
        {
            return conditions.Any(condition => Evaluate(condition, userAttributes));
        }

        private bool NotEvaluator(JArray conditions, UserAttributes userAttributes)
        {
            return conditions.Count == 1 && !Evaluate(conditions[0], userAttributes);

        }

        public bool Evaluate(JToken conditions, UserAttributes userAttributes)
        {
            //Cloning is because it is reference type
            var conditionsArray = conditions.DeepClone() as JArray;
            if (conditionsArray != null)
            {
                switch (conditions[0].ToString())
                {
                    case AND_OPERATOR: conditionsArray.RemoveAt(0); return AndEvaluator(conditionsArray, userAttributes);
                    case OR_OPERATOR:  conditionsArray.RemoveAt(0); return OrEvaluator(conditionsArray, userAttributes);
                    case NOT_OPERATOR: conditionsArray.RemoveAt(0); return NotEvaluator(conditionsArray, userAttributes);
                    default:
                        return false;
                }
            }

            string conditionName = conditions["name"].ToString();
            return userAttributes != null && userAttributes.ContainsKey(conditionName) && CompareValues(userAttributes[conditionName], conditions["value"]);
        }

        public bool Evaluate(object[] conditions, UserAttributes userAttributes)
        {
            return Evaluate(ConvertObjectArrayToJToken(conditions), userAttributes);
        }

        private JToken ConvertObjectArrayToJToken(object[] conditions)
        {
            var serializeConditions = JsonConvert.SerializeObject(conditions);

            return JToken.Parse(serializeConditions);
        }

        public static JToken DecodeConditions(string conditions)
        {
            return JToken.Parse(conditions);
        }

        private bool CompareValues(object attribute, JToken condition)
        {
            try
            {
                switch (condition.Type)
                {
                    case JTokenType.Integer:
                        return (int)condition == Convert.ToInt32(attribute);
                    case JTokenType.Float:
                        return (double)condition == Convert.ToDouble(attribute);
                    case JTokenType.String:
                        return (string)condition == Convert.ToString(attribute);
                    case JTokenType.Boolean:
                        return (bool)condition == Convert.ToBoolean(attribute);
                    default:
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}