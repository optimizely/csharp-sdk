﻿/* 
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

using Newtonsoft.Json.Linq;
using OptimizelySDK.AudienceConditions;
using System.Collections.Generic;

namespace OptimizelySDK.Utils
{
    /// <summary>
    /// Utility class for parsing audience conditions.
    /// </summary>
    public static class ConditionParser
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

        public static ICondition ParseAudienceConditions(JToken audienceConditions)
        {
            if (audienceConditions.Type != JTokenType.Array)
                return new AudienceIdCondition { AudienceId = (string)audienceConditions };

            var conditionsArray = audienceConditions as JArray;
            if (conditionsArray.Count == 0)
                return new EmptyCondition();

            var startIndex = 0;
            var conditionOperator = GetOperator(conditionsArray.First.ToString());

            if (conditionOperator != null)
                startIndex = 1;
            else
                conditionOperator = OR_OPERATOR;

            List<ICondition> conditions = new List<ICondition>();
            for (int i = startIndex; i < conditionsArray.Count; ++i)
                conditions.Add(ParseAudienceConditions(conditionsArray[i]));

            return GetConditions(conditions, conditionOperator);
        }

        public static ICondition ParseConditions(JToken conditionObj)
        {
            if (conditionObj.Type != JTokenType.Array)
                return new BaseCondition
                {
                    Match = conditionObj["match"]?.ToString(),
                    Type = conditionObj["type"]?.ToString(),
                    Name = conditionObj["name"].ToString(),
                    Value = conditionObj["value"]?.ToObject<object>(),
                };

            var startIndex = 0;
            var conditionsArray = conditionObj as JArray;
            var conditionOperator = GetOperator(conditionsArray.First.ToString());

            if (conditionOperator != null)
                startIndex = 1;
            else
                conditionOperator = OR_OPERATOR;

            List<ICondition> conditions = new List<ICondition>();
            for (int i = startIndex; i < conditionsArray.Count; ++i)
            {
                conditions.Add(ParseConditions(conditionsArray[i]));
            }

            return GetConditions(conditions, conditionOperator);
        }

        public static string GetOperator(object condition)
        {
            string conditionOperator = (string)condition;
            switch (conditionOperator)
            {
                case OR_OPERATOR:
                case AND_OPERATOR:
                case NOT_OPERATOR:
                    return conditionOperator;
                default:
                    return null;
            }
        }

        public static ICondition GetConditions(List<ICondition> conditions, string conditionOperator)
        {
            ICondition condition = null;
            switch (conditionOperator)
            {
                case AND_OPERATOR:
                    condition = new AndCondition() { Conditions = conditions.ToArray() };
                    break;
                case OR_OPERATOR:
                    condition = new OrCondition() { Conditions = conditions.ToArray() };
                    break;
                case NOT_OPERATOR:
                    condition = new NotCondition() { Condition = conditions.Count == 0 ? null : conditions[0] };
                    break;
                default:
                    break;
            }

            return condition;
        }
    }
}
