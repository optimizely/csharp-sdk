/* 
 * Copyright 2020, Optimizely
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

using Moq;
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using System;
using Xunit;

namespace OptimizelySDK.XUnitTests.AudienceConditionsTests
{
    public class ConditionEvaluationTest
    {
        private BaseCondition LegacyCondition = new BaseCondition { Name = "device_type", Value = "iPhone", Type = "custom_attribute" };
        private BaseCondition ExistsCondition = new BaseCondition { Name = "input_value", Match = "exists", Type = "custom_attribute" };
        private BaseCondition SubstrCondition = new BaseCondition { Name = "location", Value = "USA", Match = "substring", Type = "custom_attribute" };
        private BaseCondition GTCondition = new BaseCondition { Name = "distance_gt", Value = 10, Match = "gt", Type = "custom_attribute" };
        private BaseCondition GECondition = new BaseCondition { Name = "distance_ge", Value = 10, Match = "ge", Type = "custom_attribute" };
        private BaseCondition LTCondition = new BaseCondition { Name = "distance_lt", Value = 10, Match = "lt", Type = "custom_attribute" };
        private BaseCondition LECondition = new BaseCondition { Name = "distance_le", Value = 10, Match = "le", Type = "custom_attribute" };
        private BaseCondition ExactStrCondition = new BaseCondition { Name = "browser_type", Value = "firefox", Match = "exact", Type = "custom_attribute" };
        private BaseCondition ExactBoolCondition = new BaseCondition { Name = "is_registered_user", Value = false, Match = "exact", Type = "custom_attribute" };
        private BaseCondition ExactDecimalCondition = new BaseCondition { Name = "pi_value", Value = 3.14, Match = "exact", Type = "custom_attribute" };
        private BaseCondition ExactIntCondition = new BaseCondition { Name = "lasers_count", Value = 9000, Match = "exact", Type = "custom_attribute" };
        private BaseCondition InfinityIntCondition = new BaseCondition { Name = "max_num_value", Value = 9223372036854775807, Match = "exact", Type = "custom_attribute" };
        private BaseCondition SemVerLTCondition = new BaseCondition { Name = "semversion_lt", Value = "3.7.1", Match = "semver_lt", Type = "custom_attribute" };
        private BaseCondition SemVerGTCondition = new BaseCondition { Name = "semversion_gt", Value = "3.7.1", Match = "semver_gt", Type = "custom_attribute" };
        private BaseCondition SemVerEQCondition = new BaseCondition { Name = "semversion_eq", Value = "3.7.1", Match = "semver_eq", Type = "custom_attribute" };
        private BaseCondition SemVerGECondition = new BaseCondition { Name = "semversion_ge", Value = "3.7.1", Match = "semver_ge", Type = "custom_attribute" };
        private BaseCondition SemVerLECondition = new BaseCondition { Name = "semversion_le", Value = "3.7.1", Match = "semver_le", Type = "custom_attribute" };

        private ILogger Logger;
        private Mock<ILogger> LoggerMock;

        public ConditionEvaluationTest()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            Logger = LoggerMock.Object;
        }

        #region Evaluate Tests

        [Fact]
        public void TestEvaluateWithDifferentTypedAttributes()
        {
            var userAttributes = new UserAttributes
            {
                {"browser_type", "firefox" },
                {"is_registered_user", false },
                {"distance_gt", 15 },
                {"pi_value", 3.14 },
            };

            Assert.True(ExactStrCondition.Evaluate(null, userAttributes, Logger));
            Assert.True(ExactBoolCondition.Evaluate(null, userAttributes, Logger));
            Assert.True(GTCondition.Evaluate(null, userAttributes, Logger));
            Assert.True(ExactDecimalCondition.Evaluate(null, userAttributes, Logger));
        }

        [Fact]
        public void TestEvaluateWithNoMatchType()
        {
            Assert.True(LegacyCondition.Evaluate(null, new UserAttributes { { "device_type", "iPhone" } }, Logger));

            // Assumes exact evaluator if no match type is provided.
            Assert.False(LegacyCondition.Evaluate(null, new UserAttributes { { "device_type", "IPhone" } }, Logger));
        }

        [Fact]
        public void TestEvaluateWithInvalidTypeProperty()
        {
            BaseCondition condition = new BaseCondition { Name = "input_value", Value = "Android", Match = "exists", Type = "invalid_type" };
            Assert.Null(condition.Evaluate(null, new UserAttributes { { "device_type", "iPhone" } }, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{condition}"" uses an unknown condition type. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);
        }

        [Fact]
        public void TestEvaluateWithMissingTypeProperty()
        {
            var condition = new BaseCondition { Name = "input_value", Value = "Android", Match = "exists" };
            Assert.Null(condition.Evaluate(null, new UserAttributes { { "device_type", "iPhone" } }, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{condition}"" uses an unknown condition type. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);
        }

        [Fact]
        public void TestEvaluateWithInvalidMatchProperty()
        {
            BaseCondition condition = new BaseCondition { Name = "device_type", Value = "Android", Match = "invalid_match", Type = "custom_attribute" };
            Assert.Null(condition.Evaluate(null, new UserAttributes { { "device_type", "Android" } }, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{condition}"" uses an unknown match type. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);
        }

        [Fact]
        public void TestEvaluateLogsWarningAndReturnNullWhenAttributeIsNotProvidedAndConditionTypeIsNotExists()
        {
            Assert.Null(ExactBoolCondition.Evaluate(null, new UserAttributes { }, Logger));
            Assert.Null(SubstrCondition.Evaluate(null, new UserAttributes { }, Logger));
            Assert.Null(LTCondition.Evaluate(null, new UserAttributes { }, Logger));
            Assert.Null(GTCondition.Evaluate(null, new UserAttributes { }, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""is_registered_user"",""value"":false} evaluated to UNKNOWN because no value was passed for user attribute ""is_registered_user""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""substring"",""name"":""location"",""value"":""USA""} evaluated to UNKNOWN because no value was passed for user attribute ""location""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":10} evaluated to UNKNOWN because no value was passed for user attribute ""distance_lt""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":10} evaluated to UNKNOWN because no value was passed for user attribute ""distance_gt""."), Times.Once);
        }

        [Fact]
        public void TestEvaluateLogsAndReturnNullWhenAttributeValueNullAndConditionTypeIsNotExists()
        {
            Assert.Null(ExactBoolCondition.Evaluate(null, new UserAttributes { { "is_registered_user", null } }, Logger));
            Assert.Null(SubstrCondition.Evaluate(null, new UserAttributes { { "location", null } }, Logger));
            Assert.Null(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", null } }, Logger));
            Assert.Null(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", null } }, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""is_registered_user"",""value"":false} evaluated to UNKNOWN because a null value was passed for user attribute ""is_registered_user""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""substring"",""name"":""location"",""value"":""USA""} evaluated to UNKNOWN because a null value was passed for user attribute ""location""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":10} evaluated to UNKNOWN because a null value was passed for user attribute ""distance_lt""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":10} evaluated to UNKNOWN because a null value was passed for user attribute ""distance_gt""."), Times.Once);
        }

        [Fact]
        public void TestEvaluateReturnsFalseAndDoesNotLogForExistsConditionWhenAttributeIsNotProvided()
        {
            Assert.False(ExistsCondition.Evaluate(null, new UserAttributes(), Logger));
            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void TestEvaluateLogsWarningAndReturnNullWhenAttributeTypeIsInvalid()
        {
            Assert.Null(ExactBoolCondition.Evaluate(null, new UserAttributes { { "is_registered_user", 5 } }, Logger));
            Assert.Null(SubstrCondition.Evaluate(null, new UserAttributes { { "location", false } }, Logger));
            Assert.Null(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", "invalid" } }, Logger));
            Assert.Null(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", true } }, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""is_registered_user"",""value"":false} evaluated to UNKNOWN because a value of type ""Int32"" was passed for user attribute ""is_registered_user""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""substring"",""name"":""location"",""value"":""USA""} evaluated to UNKNOWN because a value of type ""Boolean"" was passed for user attribute ""location""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":10} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""distance_lt""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":10} evaluated to UNKNOWN because a value of type ""Boolean"" was passed for user attribute ""distance_gt""."), Times.Once);
        }

        [Fact]
        public void TestEvaluateLogsWarningAndReturnNullWhenConditionTypeIsInvalid()
        {
            var invalidCondition = new BaseCondition { Name = "is_registered_user", Value = new string[] { }, Match = "exact", Type = "custom_attribute" };
            Assert.Null(invalidCondition.Evaluate(null, new UserAttributes { { "is_registered_user", true } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""is_registered_user"",""value"":[]} has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);

            invalidCondition = new BaseCondition { Name = "location", Value = 25, Match = "substring", Type = "custom_attribute" };
            Assert.Null(invalidCondition.Evaluate(null, new UserAttributes { { "location", "USA" } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""substring"",""name"":""location"",""value"":25} has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);

            invalidCondition = new BaseCondition { Name = "distance_lt", Value = "invalid", Match = "lt", Type = "custom_attribute" };
            Assert.Null(invalidCondition.Evaluate(null, new UserAttributes { { "distance_lt", 5 } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":""invalid""} has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);

            invalidCondition = new BaseCondition { Name = "distance_gt", Value = "invalid", Match = "gt", Type = "custom_attribute" };
            Assert.Null(invalidCondition.Evaluate(null, new UserAttributes { { "distance_gt", "invalid" } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":""invalid""} has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);
        }

        #endregion // Evaluate Tests

        #region ExactMatcher Tests

        [Fact]
        public void TestExactMatcherReturnsFalseWhenAttributeValueDoesNotMatch()
        {
            Assert.False(ExactStrCondition.Evaluate(null, new UserAttributes { { "browser_type", "chrome" } }, Logger));
            Assert.False(ExactBoolCondition.Evaluate(null, new UserAttributes { { "is_registered_user", true } }, Logger));
            Assert.False(ExactDecimalCondition.Evaluate(null, new UserAttributes { { "pi_value", 2.5 } }, Logger));
            Assert.False(ExactIntCondition.Evaluate(null, new UserAttributes { { "lasers_count", 55 } }, Logger));
        }

        [Fact]
        public void TestExactMatcherReturnsNullWhenTypeMismatch()
        {
            Assert.Null(ExactStrCondition.Evaluate(null, new UserAttributes { { "browser_type", true } }, Logger));
            Assert.Null(ExactBoolCondition.Evaluate(null, new UserAttributes { { "is_registered_user", "abcd" } }, Logger));
            Assert.Null(ExactDecimalCondition.Evaluate(null, new UserAttributes { { "pi_value", false } }, Logger));
            Assert.Null(ExactIntCondition.Evaluate(null, new UserAttributes { { "lasers_count", "infinity" } }, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""browser_type"",""value"":""firefox""} evaluated to UNKNOWN because a value of type ""Boolean"" was passed for user attribute ""browser_type""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""is_registered_user"",""value"":false} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""is_registered_user""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""pi_value"",""value"":3.14} evaluated to UNKNOWN because a value of type ""Boolean"" was passed for user attribute ""pi_value""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""lasers_count"",""value"":9000} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""lasers_count""."), Times.Once);
        }

        [Fact]
        public void TestExactMatcherReturnsNullForOutOfBoundNumericValues()
        {
            Assert.Null(ExactIntCondition.Evaluate(null, new UserAttributes { { "lasers_count", double.NegativeInfinity } }, Logger));
            Assert.Null(ExactDecimalCondition.Evaluate(null, new UserAttributes { { "pi_value", Math.Pow(2, 53) + 2 } }, Logger));
            Assert.Null(InfinityIntCondition.Evaluate(null, new UserAttributes { { "max_num_value", 15 } }, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""lasers_count"",""value"":9000} evaluated to UNKNOWN because the number value for user attribute ""lasers_count"" is not in the range [-2^53, +2^53]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""pi_value"",""value"":3.14} evaluated to UNKNOWN because the number value for user attribute ""pi_value"" is not in the range [-2^53, +2^53]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""max_num_value"",""value"":9223372036854775807} has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);
        }

        [Fact]
        public void TestExactMatcherReturnsTrueWhenAttributeValueMatches()
        {
            Assert.True(ExactStrCondition.Evaluate(null, new UserAttributes { { "browser_type", "firefox" } }, Logger));
            Assert.True(ExactBoolCondition.Evaluate(null, new UserAttributes { { "is_registered_user", false } }, Logger));
            Assert.True(ExactDecimalCondition.Evaluate(null, new UserAttributes { { "pi_value", 3.14 } }, Logger));
            Assert.True(ExactIntCondition.Evaluate(null, new UserAttributes { { "lasers_count", 9000 } }, Logger));
        }

        #endregion // ExactMatcher Tests

        #region ExistsMatcher Tests

        [Fact]
        public void TestExistsMatcherReturnsFalseWhenAttributeIsNotProvided()
        {
            Assert.False(ExistsCondition.Evaluate(null, new UserAttributes { }, Logger));
        }

        [Fact]
        public void TestExistsMatcherReturnsFalseWhenAttributeNull()
        {
            Assert.False(ExistsCondition.Evaluate(null, new UserAttributes { { "input_value", null } }, Logger));
        }

        [Fact]
        public void TestExistsMatcherReturnsTrueWhenAttributeValueIsProvided()
        {
            Assert.True(ExistsCondition.Evaluate(null, new UserAttributes { { "input_value", "" } }, Logger));
            Assert.True(ExistsCondition.Evaluate(null, new UserAttributes { { "input_value", "iPhone" } }, Logger));
            Assert.True(ExistsCondition.Evaluate(null, new UserAttributes { { "input_value", 10 } }, Logger));
            Assert.True(ExistsCondition.Evaluate(null, new UserAttributes { { "input_value", false } }, Logger));
        }

        #endregion // ExistsMatcher Tests

        #region SubstringMatcher Tests

        [Fact]
        public void TestSubstringMatcherReturnsFalseWhenAttributeValueIsNotASubstring()
        {
            Assert.False(SubstrCondition.Evaluate(null, new UserAttributes { { "location", "Los Angeles" } }, Logger));
        }

        [Fact]
        public void TestSubstringMatcherReturnsNullWhenAttributeValueIsNotAString()
        {
            Assert.Null(SubstrCondition.Evaluate(null, new UserAttributes { { "location", 10.5 } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""substring"",""name"":""location"",""value"":""USA""} evaluated to UNKNOWN because a value of type ""Double"" was passed for user attribute ""location""."), Times.Once);
        }

        [Fact]
        public void TestSubstringMatcherReturnsTrueWhenAttributeValueIsASubstring()
        {
            Assert.True(SubstrCondition.Evaluate(null, new UserAttributes { { "location", "USA" } }, Logger));
            Assert.True(SubstrCondition.Evaluate(null, new UserAttributes { { "location", "San Francisco, USA" } }, Logger));
        }

        #endregion // SubstringMatcher Tests

        #region GTMatcher Tests

        [Fact]
        public void TestGTMatcherReturnsFalseWhenAttributeValueIsLessThanOrEqualToConditionValue()
        {
            Assert.False(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", 5 } }, Logger));
            Assert.False(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", 10 } }, Logger));
        }

        [Fact]
        public void TestGTMatcherReturnsNullWhenAttributeValueIsNotANumericValue()
        {
            Assert.Null(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", "invalid_type" } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":10} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""distance_gt""."), Times.Once);
        }

        [Fact]
        public void TestGTMatcherReturnsNullWhenAttributeValueIsOutOfBounds()
        {
            Assert.Null(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", double.PositiveInfinity } }, Logger));
            Assert.Null(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", Math.Pow(2, 53) + 2 } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":10} evaluated to UNKNOWN because the number value for user attribute ""distance_gt"" is not in the range [-2^53, +2^53]."), Times.Exactly(2));
        }

        [Fact]
        public void TestGTMatcherReturnsTrueWhenAttributeValueIsGreaterThanConditionValue()
        {
            Assert.True(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", 15 } }, Logger));
        }

        #endregion // GTMatcher Tests
        
        #region GEMatcher Tests

        [Fact]
        public void TestGEMatcherReturnsFalseWhenAttributeValueIsLessButTrueForEqualToConditionValue()
        {
            Assert.False(GECondition.Evaluate(null, new UserAttributes { { "distance_ge", 5 } }, Logger));
            Assert.True(GECondition.Evaluate(null, new UserAttributes { { "distance_ge", 10 } }, Logger));
        }

        [Fact]
        public void TestGEMatcherReturnsNullWhenAttributeValueIsNotANumericValue()
        {
            Assert.Null(GECondition.Evaluate(null, new UserAttributes { { "distance_ge", "invalid_type" } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""ge"",""name"":""distance_ge"",""value"":10} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""distance_ge""."), Times.Once);
        }

        [Fact]
        public void TestGEMatcherReturnsNullWhenAttributeValueIsOutOfBounds()
        {
            Assert.Null(GECondition.Evaluate(null, new UserAttributes { { "distance_ge", double.PositiveInfinity } }, Logger));
            Assert.Null(GECondition.Evaluate(null, new UserAttributes { { "distance_ge", Math.Pow(2, 53) + 2 } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""ge"",""name"":""distance_ge"",""value"":10} evaluated to UNKNOWN because the number value for user attribute ""distance_ge"" is not in the range [-2^53, +2^53]."), Times.Exactly(2));
        }

        [Fact]
        public void TestGEMatcherReturnsTrueWhenAttributeValueIsGreaterThanConditionValue()
        {
            Assert.True(GECondition.Evaluate(null, new UserAttributes { { "distance_ge", 15 } }, Logger));
        }

        #endregion // GEMatcher Tests

        #region LTMatcher Tests

        [Fact]
        public void TestLTMatcherReturnsFalseWhenAttributeValueIsGreaterThanOrEqualToConditionValue()
        {
            Assert.False(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", 15 } }, Logger));
            Assert.False(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", 10 } }, Logger));
        }

        [Fact]
        public void TestLTMatcherReturnsNullWhenAttributeValueIsNotANumericValue()
        {
            Assert.Null(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", "invalid_type" } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":10} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""distance_lt""."), Times.Once);
        }

        [Fact]
        public void TestLTMatcherReturnsNullWhenAttributeValueIsOutOfBounds()
        {
            Assert.Null(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", double.NegativeInfinity } }, Logger));
            Assert.Null(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", -Math.Pow(2, 53) - 2 } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":10} evaluated to UNKNOWN because the number value for user attribute ""distance_lt"" is not in the range [-2^53, +2^53]."), Times.Exactly(2));
        }

        [Fact]
        public void TestLTMatcherReturnsTrueWhenAttributeValueIsLessThanConditionValue()
        {
            Assert.True(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", 5 } }, Logger));
        }

        #endregion // LTMatcher Tests

        #region LEMatcher Tests
        [Fact]
        public void TestLEMatcherReturnsFalseWhenAttributeValueIsGreaterAndTrueIfEqualToConditionValue()
        {
            Assert.False(LECondition.Evaluate(null, new UserAttributes { { "distance_le", 15 } }, Logger)  );
            Assert.True(LECondition.Evaluate(null, new UserAttributes { { "distance_le", 10 } }, Logger) );
        }

        [Fact]
        public void TestLEMatcherReturnsNullWhenAttributeValueIsNotANumericValue()
        {
            Assert.Null(LECondition.Evaluate(null, new UserAttributes { { "distance_le", "invalid_type" } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""le"",""name"":""distance_le"",""value"":10} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""distance_le""."), Times.Once);
        }

        [Fact]
        public void TestLEMatcherReturnsNullWhenAttributeValueIsOutOfBounds()
        {
            Assert.Null(LECondition.Evaluate(null, new UserAttributes { { "distance_le", double.NegativeInfinity } }, Logger));
            Assert.Null(LECondition.Evaluate(null, new UserAttributes { { "distance_le", -Math.Pow(2, 53) - 2 } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""le"",""name"":""distance_le"",""value"":10} evaluated to UNKNOWN because the number value for user attribute ""distance_le"" is not in the range [-2^53, +2^53]."), Times.Exactly(2));
        }

        [Fact]
        public void TestLEMatcherReturnsTrueWhenAttributeValueIsLessThanConditionValue()
        {
            Assert.True(LECondition.Evaluate(null, new UserAttributes { { "distance_le", 5 } }, Logger) );
        }

        #endregion // LEMatcher Tests

        #region SemVerLTMatcher Tests
        [Fact]
        public void TestSemVerLTMatcherReturnsFalseWhenAttributeValueIsGreaterThanOrEqualToConditionValue()
        {
            Assert.False(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.7.2" } }, Logger) );
            Assert.False(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.7.1" } }, Logger) );
            Assert.False(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.8" } }, Logger) );
            Assert.False(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "4" } }, Logger) );
        }

        [Fact]
        public void TestSemVerLTMatcherReturnsTrueWhenAttributeValueIsLessThanConditionValue()
        {
            Assert.True(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.7.0" } }, Logger) );
            Assert.True(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.7.1-beta" } }, Logger) );
            Assert.True(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "2.7.1" } }, Logger) );
            Assert.True(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.7" } }, Logger) );
            Assert.True(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3" } }, Logger) );
        }

        [Fact]
        public void TestSemVerLTMatcherReturnsTrueWhenAttributeValueIsLessThanConditionValueBeta()
        {
            var semverLTCondition = new BaseCondition { Name = "semversion_lt", Value = "3.7.0-beta.2.3", Match = "semver_lt", Type = "custom_attribute" };
            Assert.True(semverLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.7.0-beta.2.1" } }, Logger) );
            Assert.True(semverLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.7.0-beta" } }, Logger) );
        }
        #endregion // SemVerLTMatcher Tests

        #region SemVerGTMatcher Tests
        [Fact]
        public void TestSemVerGTMatcherReturnsFalseWhenAttributeValueIsLessThanOrEqualToConditionValue()
        {
            Assert.False(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.7.0" } }, Logger));
            Assert.False(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.7.1" } }, Logger));
            Assert.False(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.6" } }, Logger));
            Assert.False(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "2" } }, Logger));
        }

        [Fact]
        public void TestSemVerGTMatcherReturnsTrueWhenAttributeValueIsGreaterThanConditionValue()
        {
            Assert.True(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.7.2" } }, Logger));
            Assert.True(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.7.2-beta" } }, Logger));
            Assert.True(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "4.7.1" } }, Logger));
            Assert.True(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.8" } }, Logger));
            Assert.True(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "4" } }, Logger));
        }

        [Fact]
        public void TestSemVerGTMatcherReturnsTrueWhenAttributeValueIsGreaterThanConditionValueBeta()
        {
            var semverGTCondition = new BaseCondition { Name = "semversion_gt", Value = "3.7.0-beta.2.3", Match = "semver_gt", Type = "custom_attribute" };
            Assert.True(semverGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.7.0-beta.2.4" } }, Logger));
            Assert.True(semverGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.7.0" } }, Logger));
        }
        #endregion // SemVerGTMatcher Tests

        #region SemVerEQMatcher Tests
        [Fact]
        public void TestSemVerEQMatcherReturnsFalseWhenAttributeValueIsNotEqualToConditionValue()
        {
            Assert.False(SemVerEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3.7.0" } }, Logger) );
            Assert.False(SemVerEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3.7.2" } }, Logger) );
            Assert.False(SemVerEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3.6" } }, Logger));
            Assert.False(SemVerEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "2" } }, Logger));
            Assert.False(SemVerEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "4" } }, Logger));
            Assert.False(SemVerEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3" } }, Logger));
        }

        [Fact]
        public void TestSemVerEQMatcherReturnsTrueWhenAttributeValueIsEqualToConditionValue()
        {
            Assert.True(SemVerEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3.7.1" } }, Logger) );
        }

        [Fact]
        public void TestSemVerEQMatcherReturnsTrueWhenAttributeValueIsEqualToConditionValueMajorOnly()
        {
            var semverEQCondition = new BaseCondition { Name = "semversion_eq", Value = "3", Match = "semver_eq", Type = "custom_attribute" };
            Assert.True(semverEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3.0.0" } }, Logger) );
            Assert.True(semverEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3.1" } }, Logger) );
        }

        [Fact]
        public void TestSemVerEQMatcherReturnsFalseOrFalseWhenAttributeValueIsNotEqualToConditionValueMajorOnly()
        {
            var semverEQCondition = new BaseCondition { Name = "semversion_eq", Value = "3", Match = "semver_eq", Type = "custom_attribute" };
            Assert.False(semverEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "4.0" } }, Logger) );
            Assert.False(semverEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "2" } }, Logger) );
        }

        [Fact]
        public void TestSemVerEQMatcherReturnsTrueWhenAttributeValueIsEqualToConditionValueBeta()
        {
            var semverEQCondition = new BaseCondition { Name = "semversion_eq", Value = "3.7.0-beta.2.3", Match = "semver_eq", Type = "custom_attribute" };
            Assert.True(semverEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3.7.0-beta.2.3" } }, Logger) );
        }
        #endregion // SemVerEQMatcher Tests

        #region SemVerGEMatcher Tests
        [Fact]
        public void TestSemVerGEMatcherReturnsFalseWhenAttributeValueIsNotGreaterOrEqualToConditionValue()
        {
            Assert.False(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.0" } }, Logger) );
            Assert.False(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.1-beta" } }, Logger) );
            Assert.False(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.6" } }, Logger) );
            Assert.False(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "2" } }, Logger) );
            Assert.False(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3" } }, Logger) );
        }

        [Fact]
        public void TestSemVerGEMatcherReturnsTrueWhenAttributeValueIsGreaterOrEqualToConditionValue()
        {
            Assert.True(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.1" } }, Logger) );
            Assert.True(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.2" } }, Logger) );
            Assert.True(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.8.1" } }, Logger) ); ;
            Assert.True(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "4.7.1" } }, Logger) );
        }

        [Fact]
        public void TestSemVerGEMatcherReturnsTrueWhenAttributeValueIsGreaterOrEqualToConditionValueMajorOnly()
        {
            var semverGECondition = new BaseCondition { Name = "semversion_ge", Value = "3", Match = "semver_ge", Type = "custom_attribute" };
            Assert.True(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.0" } }, Logger) );
            Assert.True(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.0.0" } }, Logger) );
            Assert.True(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "4.0" } }, Logger) );
        }

        [Fact]
        public void TestSemVerGEMatcherReturnsFalseWhenAttributeValueIsNotGreaterOrEqualToConditionValueMajorOnly()
        {
            var semverGECondition = new BaseCondition { Name = "semversion_ge", Value = "3", Match = "semver_ge", Type = "custom_attribute" };
            Assert.False(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "2" } }, Logger) );
        }

        [Fact]
        public void TestSemVerGEMatcherReturnsTrueWhenAttributeValueIsGreaterOrEqualToConditionValueBeta()
        {
            var semverGECondition = new BaseCondition { Name = "semversion_ge", Value = "3.7.0-beta.2.3", Match = "semver_ge", Type = "custom_attribute" };
            Assert.True(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.0-beta.2.3" } }, Logger) );
            Assert.True(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.0-beta.2.4" } }, Logger) );
            Assert.True(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.0-beta.2.3+1.2.3" } }, Logger) );
            Assert.True(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.1-beta.2.3" } }, Logger) );
        }
        #endregion // SemVerGEMatcher Tests

        #region SemVerLEMatcher Tests
        [Fact]
        public void TestSemVerLEMatcherReturnsFalseWhenAttributeValueIsNotLessOrEqualToConditionValue()
        {
            Assert.False(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.2" } }, Logger) );
            Assert.False(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.8" } }, Logger) );
            Assert.False(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "4" } }, Logger) );
        }

        [Fact]
        public void TestSemVerLEMatcherReturnsTrueWhenAttributeValueIsLessOrEqualToConditionValue()
        {
            Assert.True(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.1" } }, Logger) );
            Assert.True(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.0" } }, Logger) );
            Assert.True(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.6.1" } }, Logger) );
            Assert.True(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "2.7.1" } }, Logger) );
            Assert.True(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.1-beta" } }, Logger) );
        }

        [Fact]
        public void TestSemVerLEMatcherReturnsTrueWhenAttributeValueIsLessOrEqualToConditionValueMajorOnly()
        {
            var semverLECondition = new BaseCondition { Name = "semversion_le", Value = "3", Match = "semver_le", Type = "custom_attribute" };
            Assert.True(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.0-beta.2.4" } }, Logger) );
            Assert.True(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.1-beta" } }, Logger) );
            Assert.True(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.0.0" } }, Logger) );
            Assert.True(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "2.0" } }, Logger) );
        }

        [Fact]
        public void TestSemVerLEMatcherReturnsFalseWhenAttributeValueIsNotLessOrEqualToConditionValueMajorOnly()
        {
            var semverLECondition = new BaseCondition { Name = "semversion_le", Value = "3", Match = "semver_le", Type = "custom_attribute" };
            Assert.False(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "4" } }, Logger) );
        }

        [Fact]
        public void TestSemVerLEMatcherReturnsTrueWhenAttributeValueIsLessOrEqualToConditionValueBeta()
        {
            var semverLECondition = new BaseCondition { Name = "semversion_le", Value = "3.7.0-beta.2.3", Match = "semver_le", Type = "custom_attribute" };
            Assert.True(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.0-beta.2.2" } }, Logger) );
            Assert.True(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.0-beta.2.3" } }, Logger) );
            Assert.True(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.0-beta.2.2+1.2.3" } }, Logger) );
            Assert.True(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.6.1-beta.2.3+1.2" } }, Logger) );
        }
        #endregion // SemVerLEMatcher Tests

        #region SemVer Invalid Scenarios

        [Fact]
        public void TestInvalidSemVersions()
        {
            var invalidValues = new string[] {"-", ".", "..", "+", "+test", " ", "2 .3. 0", "2.",
                ".2.2", "3.7.2.2", "3.x", ",", "+build-prerelese"};
            var semverLECondition = new BaseCondition { Name = "semversion_le", Value = "3", Match = "semver_le", Type = "custom_attribute" };
            foreach(var invalidValue in invalidValues) {
                Assert.Null(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", invalidValue } }, Logger));
            }
        }

        #endregion
    }
}
