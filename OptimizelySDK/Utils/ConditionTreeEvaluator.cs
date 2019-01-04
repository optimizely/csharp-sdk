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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OptimizelySDK.Logger;
using System;

namespace OptimizelySDK.Utils
{
    public class ConditionTreeEvaluator
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

        ///// <summary>
        ///// Logger instance.
        ///// </summary>
        //private ILogger Logger;

        //public ConditionTreeEvaluator(ILogger logger)
        //{
        //    Logger = logger;
        //}

        /// <summary>
        /// Evaluates an array of conditions as if the evaluator had been applied
        /// to each entry and the results AND-ed together.
        /// </summary>
        /// <param name="conditions">Array of conditions ex: [operand_1, operand_2]</param>
        /// <param name="userAttributes">Hash representing user attributes</param>
        /// <returns>true/false if the user attributes match/don't match the given conditions,
        /// null if the user attributes and conditions can't be evaluated</returns>
        private bool? AndEvaluator(JArray conditions, Func<JToken, bool?> leafEvaluator)
        {
            // According to the matrix:
            // false and true is false
            // false and null is false
            // true and null is null.
            // true and false is false
            // true and true is true
            // null and null is null
            var foundNull = false;
            foreach(var condition in conditions)
            {
                var result = Evaluate(condition, leafEvaluator);
                if (result == null)
                    foundNull = true;
                else if (result == false)
                    return false;
            }

            if (foundNull)
                return null;

            return true;
        }

        /// <summary>
        /// Evaluates an array of conditions as if the evaluator had been applied
        /// to each entry and the results OR-ed together.
        /// </summary>
        /// <param name="conditions">Array of conditions ex: [operand_1, operand_2]</param>
        /// <param name="userAttributes">Hash representing user attributes</param>
        /// <returns>true/false if the user attributes match/don't match the given conditions,
        /// null if the user attributes and conditions can't be evaluated</returns>
        private bool? OrEvaluator(JArray conditions, Func<JToken, bool?> leafEvaluator)
        {
            // According to the matrix:
            // true returns true
            // false or null is null
            // false or false is false
            // null or null is null
            var foundNull = false;
            foreach (var condition in conditions)
            {
                var result = Evaluate(condition, leafEvaluator);
                if (result == null)
                    foundNull = true;
                else if (result == true)
                    return true;
            }

            if (foundNull)
                return null;

            return false;
        }

        /// <summary>
        /// Evaluates an array of conditions as if the evaluator had been applied
        /// to a single entry and NOT was applied to the result.
        /// </summary>
        /// <param name="conditions">Array of conditions ex: [operand_1, operand_2]</param>
        /// <param name="userAttributes">Hash representing user attributes</param>
        /// <returns>true/false if the user attributes match/don't match the given conditions,
        /// null if the user attributes and conditions can't be evaluated</returns>
        private bool? NotEvaluator(JArray conditions, Func<JToken, bool?> leafEvaluator)
        {
            if (conditions.Count > 0)
            {
                var result = Evaluate(conditions[0], leafEvaluator);
                return result == null ? null : !result;
            }

            return null;
        }

        public bool? Evaluate(JToken conditions, Func<JToken, bool?> leafEvaluator)
        {
            //Cloning is because it is reference type
            var conditionsArray = conditions.DeepClone() as JArray;
            if (conditionsArray != null)
            {
                switch (conditions[0].ToString())
                {
                    case AND_OPERATOR: conditionsArray.RemoveAt(0); return AndEvaluator(conditionsArray, leafEvaluator);
                    case OR_OPERATOR:  conditionsArray.RemoveAt(0); return OrEvaluator(conditionsArray,  leafEvaluator);
                    case NOT_OPERATOR: conditionsArray.RemoveAt(0); return NotEvaluator(conditionsArray, leafEvaluator);
                    default:
                        // Operator to apply is not explicit - assume 'or'.
                        return OrEvaluator(conditionsArray, leafEvaluator);
                }
            }

            var leafCondition = conditions;
            return leafEvaluator(leafCondition);
        }

        public bool? Evaluate(object[] conditions, Func<JToken, bool?> leafEvaluator)
        {
            return Evaluate(ConvertObjectArrayToJToken(conditions), leafEvaluator);
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
    }
}