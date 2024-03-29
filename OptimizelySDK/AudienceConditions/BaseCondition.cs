﻿/* 
 * Copyright 2019-2022, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Linq;
using Newtonsoft.Json;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;

namespace OptimizelySDK.AudienceConditions
{
    /// <summary>
    /// Represents Base condition entity for audience evaluation.
    /// </summary>
    public class BaseCondition : ICondition
    {
        /// <summary>
        /// String constant representing custom attribute condition type.
        /// </summary>
        public const string CUSTOM_ATTRIBUTE = "custom_attribute";

        /// <summary>
        /// String constant representing a third-party condition type.
        /// </summary>
        public const string THIRD_PARTY_DIMENSION = "third_party_dimension";

        /// <summary>
        /// String constant to match status of qualified segments.  
        /// </summary>
        public const string QUALIFIED = "qualified";

        /// <summary>
        /// Valid types allowed for validation
        /// </summary>
        public static readonly string[] ValidTypes =
        {
            CUSTOM_ATTRIBUTE, THIRD_PARTY_DIMENSION,
        };

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("match")]
        public string Match { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }

        public bool? Evaluate(ProjectConfig config, OptimizelyUserContext userContext,
            ILogger logger
        )
        {
            if (!ValidTypes.Contains(Type))
            {
                logger.Log(LogLevel.WARN,
                    $@"Audience condition ""{this
                    }"" uses an unknown condition type. You may need to upgrade to a newer release of the Optimizely SDK.");
                return null;
            }

            if (Match == QUALIFIED)
            {
                if (Value is string)
                {
                    return userContext.IsQualifiedFor(Value.ToString());
                }

                logger.Log(LogLevel.WARN,
                    $@"Audience condition ""{this}"" has a qualified match but invalid value.");
                return null;
            }

            var userAttributes = userContext.GetAttributes();
            object attributeValue = null;
            if (userAttributes.TryGetValue(Name, out attributeValue) == false &&
                Match != AttributeMatchTypes.EXIST)
            {
                logger.Log(LogLevel.DEBUG,
                    $@"Audience condition {this
                    } evaluated to UNKNOWN because no value was passed for user attribute ""{Name
                    }"".");
                return null;
            }

            var evaluator = GetEvaluator();
            if (evaluator == null)
            {
                logger.Log(LogLevel.WARN,
                    $@"Audience condition ""{this
                    }"" uses an unknown match type. You may need to upgrade to a newer release of the Optimizely SDK.");
                return null;
            }

            return evaluator(attributeValue, logger);
        }

        public Func<object, ILogger, bool?> GetEvaluator()
        {
            switch (Match)
            {
                case AttributeMatchTypes.EXACT:
                    return ExactEvaluator;
                case AttributeMatchTypes.EXIST:
                    return ExistEvaluator;
                case AttributeMatchTypes.GREATER_OR_EQUAL:
                    return GreaterOrEqualThanEvaluator;
                case AttributeMatchTypes.GREATER_THAN:
                    return GreaterThanEvaluator;
                case AttributeMatchTypes.LESS_OR_EQUAL:
                    return LessOrEqualThanEvaluator;
                case AttributeMatchTypes.LESS_THAN:
                    return LessThanEvaluator;
                case AttributeMatchTypes.SUBSTRING:
                    return SubstringEvaluator;
                case AttributeMatchTypes.SEMVER_EQ:
                    return SemanticVersionEqualEvaluator;
                case AttributeMatchTypes.SEMVER_GE:
                    return SemanticVersionGreaterOrEqualEvaluator;
                case AttributeMatchTypes.SEMVER_GT:
                    return SemanticVersionGreaterEvaluator;
                case AttributeMatchTypes.SEMVER_LE:
                    return SemanticVersionLessOrEqualEvaluator;
                case AttributeMatchTypes.SEMVER_LT:
                    return SemanticVersionLessEvaluator;
                case null:
                    return ExactEvaluator;
            }

            return null;
        }

        public bool? ExactEvaluator(object attributeValue, ILogger logger)
        {
            if (!IsValueTypeValidForExactConditions(Value) || (Validator.IsNumericType(Value) &&
                                                               !Validator.
                                                                   IsValidNumericValue(Value)))
            {
                logger.Log(LogLevel.WARN,
                    $@"Audience condition {this
                    } has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK.");
                return null;
            }

            if (attributeValue == null)
            {
                logger.Log(LogLevel.DEBUG,
                    $@"Audience condition {this
                    } evaluated to UNKNOWN because a null value was passed for user attribute ""{
                        Name}"".");
                return null;
            }

            if (!IsValueTypeValidForExactConditions(attributeValue) ||
                !AreValuesSameType(Value, attributeValue))
            {
                logger.Log(LogLevel.WARN,
                    $@"Audience condition {this} evaluated to UNKNOWN because a value of type ""{
                        attributeValue.GetType().Name}"" was passed for user attribute ""{Name
                        }"".");
                return null;
            }

            if (Validator.IsNumericType(attributeValue) &&
                !Validator.IsValidNumericValue(attributeValue))
            {
                logger.Log(LogLevel.WARN,
                    $@"Audience condition {this
                    } evaluated to UNKNOWN because the number value for user attribute ""{Name
                    }"" is not in the range [-2^53, +2^53].");
                return null;
            }

            if (Validator.IsNumericType(Value) && Validator.IsNumericType(attributeValue))
            {
                return Convert.ToDouble(Value).Equals(Convert.ToDouble(attributeValue));
            }

            return Value.Equals(attributeValue);
        }

        public bool? ExistEvaluator(object attributeValue, ILogger logger)
        {
            return attributeValue != null;
        }

        public bool? GreaterThanEvaluator(object attributeValue, ILogger logger)
        {
            var result = NumberEvaluator(attributeValue, logger);
            return result == null ? null : (bool?)(result > 0);
        }

        public bool? GreaterOrEqualThanEvaluator(object attributeValue, ILogger logger)
        {
            var result = NumberEvaluator(attributeValue, logger);
            return result == null ? null : (bool?)(result >= 0);
        }

        public bool? LessThanEvaluator(object attributeValue, ILogger logger)
        {
            var result = NumberEvaluator(attributeValue, logger);
            return result == null ? null : (bool?)(result < 0);
        }

        public bool? LessOrEqualThanEvaluator(object attributeValue, ILogger logger)
        {
            var result = NumberEvaluator(attributeValue, logger);
            return result == null ? null : (bool?)(result <= 0);
        }

        public bool? SubstringEvaluator(object attributeValue, ILogger logger)
        {
            if (!(Value is string))
            {
                logger.Log(LogLevel.WARN,
                    $@"Audience condition {this
                    } has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK.");
                return null;
            }

            if (attributeValue == null)
            {
                logger.Log(LogLevel.DEBUG,
                    $@"Audience condition {this
                    } evaluated to UNKNOWN because a null value was passed for user attribute ""{
                        Name}"".");
                return null;
            }

            if (!(attributeValue is string))
            {
                logger.Log(LogLevel.WARN,
                    $@"Audience condition {this} evaluated to UNKNOWN because a value of type ""{
                        attributeValue.GetType().Name}"" was passed for user attribute ""{Name
                        }"".");
                return null;
            }

            var attrValue = (string)attributeValue;
            return attrValue != null && attrValue.Contains((string)Value);
        }

        /// <summary>
        /// Validates the value for exact conditions.
        /// </summary>
        /// <param name="value">Value to validate</param>
        /// <returns>true if the type of value is valid for exact conditions, false otherwise.</returns>
        public bool IsValueTypeValidForExactConditions(object value)
        {
            return value is string || value is bool || Validator.IsNumericType(value);
        }

        public bool? SemanticVersionEqualEvaluator(object attributeValue, ILogger logger)
        {
            var result = SemanticVersionEvaluator(attributeValue, logger);
            return result == null ? null : (bool?)(result == 0);
        }

        public bool? SemanticVersionGreaterEvaluator(object attributeValue, ILogger logger)
        {
            var result = SemanticVersionEvaluator(attributeValue, logger);
            return result == null ? null : (bool?)(result > 0);
        }

        public bool? SemanticVersionGreaterOrEqualEvaluator(object attributeValue, ILogger logger)
        {
            var result = SemanticVersionEvaluator(attributeValue, logger);
            return result == null ? null : (bool?)(result >= 0);
        }

        public bool? SemanticVersionLessEvaluator(object attributeValue, ILogger logger)
        {
            var result = SemanticVersionEvaluator(attributeValue, logger);
            return result == null ? null : (bool?)(result < 0);
        }

        public bool? SemanticVersionLessOrEqualEvaluator(object attributeValue, ILogger logger)
        {
            var result = SemanticVersionEvaluator(attributeValue, logger);
            return result == null ? null : (bool?)(result <= 0);
        }

        public int? SemanticVersionEvaluator(object attributeValue, ILogger logger)
        {
            if (!(Value is string))
            {
                logger.Log(LogLevel.WARN,
                    $@"Audience condition {this
                    } has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK.");
                return null;
            }

            if (attributeValue == null)
            {
                logger.Log(LogLevel.DEBUG,
                    $@"Audience condition {this
                    } evaluated to UNKNOWN because a null value was passed for user attribute ""{
                        Name}"".");
                return null;
            }

            if (!(attributeValue is string))
            {
                logger.Log(LogLevel.WARN,
                    $@"Audience condition {this} evaluated to UNKNOWN because a value of type ""{
                        attributeValue.GetType().Name}"" was passed for user attribute ""{Name
                        }"".");
                return null;
            }

            try
            {
                var conditionalVersion = new SemanticVersion(Value.ToString());
                var userSemanticVersion = new SemanticVersion(attributeValue.ToString());

                return userSemanticVersion.CompareTo(conditionalVersion);
            }
            catch
            {
                return null;
            }
        }

        public int? NumberEvaluator(object attributeValue, ILogger logger)
        {
            if (!Validator.IsValidNumericValue(Value))
            {
                logger.Log(LogLevel.WARN,
                    $@"Audience condition {this
                    } has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK.");
                return null;
            }

            if (attributeValue == null)
            {
                logger.Log(LogLevel.DEBUG,
                    $@"Audience condition {this
                    } evaluated to UNKNOWN because a null value was passed for user attribute ""{
                        Name}"".");
                return null;
            }

            if (!Validator.IsNumericType(attributeValue))
            {
                logger.Log(LogLevel.WARN,
                    $@"Audience condition {this} evaluated to UNKNOWN because a value of type ""{
                        attributeValue.GetType().Name}"" was passed for user attribute ""{Name
                        }"".");
                return null;
            }

            if (!Validator.IsValidNumericValue(attributeValue))
            {
                logger.Log(LogLevel.WARN,
                    $@"Audience condition {this
                    } evaluated to UNKNOWN because the number value for user attribute ""{Name
                    }"" is not in the range [-2^53, +2^53].");
                return null;
            }

            var userValue = Convert.ToDouble(attributeValue);
            var conditionalValue = Convert.ToDouble(Value);

            return userValue.CompareTo(conditionalValue);
        }

        /// <summary>
        /// Validates that the types of first and second value are same.
        /// </summary>
        /// <param name="firstValue"></param>
        /// <param name="secondValue"></param>
        /// <returns>true if the type of both values are same, false otherwise.</returns>
        public bool AreValuesSameType(object firstValue, object secondValue)
        {
            if (firstValue is string && secondValue is string)
            {
                return true;
            }

            if (firstValue is bool && secondValue is bool)
            {
                return true;
            }

            if (Validator.IsNumericType(firstValue) && Validator.IsNumericType(secondValue))
            {
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }
    }
}
