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

using Moq;
using NUnit.Framework;
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using System;

namespace OptimizelySDK.Tests.AudienceConditionsTests
{
    [TestFixture]
    public class ConditionEvaluationTest
    {
        private BaseCondition LegacyCondition = new BaseCondition { Name = "device_type", Value = "iPhone", Type = "custom_attribute" };
        private BaseCondition ExistsCondition = new BaseCondition { Name = "input_value", Match = "exists", Type = "custom_attribute" };
        private BaseCondition SubstrCondition = new BaseCondition { Name = "location", Value = "USA", Match = "substring", Type = "custom_attribute" };
        private BaseCondition GTCondition = new BaseCondition { Name = "distance_gt", Value = 10, Match = "gt", Type = "custom_attribute" };
        private BaseCondition LTCondition = new BaseCondition { Name = "distance_lt", Value = 10, Match = "lt", Type = "custom_attribute" };
        private BaseCondition ExactStrCondition = new BaseCondition { Name = "browser_type", Value = "firefox", Match = "exact", Type = "custom_attribute" };
        private BaseCondition ExactBoolCondition = new BaseCondition { Name = "is_registered_user", Value = false, Match = "exact", Type = "custom_attribute" };
        private BaseCondition ExactDecimalCondition = new BaseCondition { Name = "pi_value", Value = 3.14, Match = "exact", Type = "custom_attribute" };
        private BaseCondition ExactIntCondition = new BaseCondition { Name = "lasers_count", Value = 9000, Match = "exact", Type = "custom_attribute" };
        private BaseCondition InfinityIntCondition = new BaseCondition { Name = "max_num_value", Value = 9223372036854775807, Match = "exact", Type = "custom_attribute" };

        private ILogger Logger;
        private Mock<ILogger> LoggerMock;

        [SetUp]
        public void Initialize()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            Logger = LoggerMock.Object;
        }

        #region Evaluate Tests

        [Test]
        public void TestEvaluateWithDifferentTypedAttributes()
        {
            var userAttributes = new UserAttributes
            {
                {"browser_type", "firefox" },
                {"is_registered_user", false },
                {"distance_gt", 15 },
                {"pi_value", 3.14 },
            };

            Assert.That(ExactStrCondition.Evaluate(null, userAttributes, Logger), Is.True);
            Assert.That(ExactBoolCondition.Evaluate(null, userAttributes, Logger), Is.True);
            Assert.That(GTCondition.Evaluate(null, userAttributes, Logger), Is.True);
            Assert.That(ExactDecimalCondition.Evaluate(null, userAttributes, Logger), Is.True);
        }

        [Test]
        public void TestEvaluateWithNoMatchType()
        {
            Assert.That(LegacyCondition.Evaluate(null, new UserAttributes { { "device_type", "iPhone" } }, Logger), Is.True);

            // Assumes exact evaluator if no match type is provided.
            Assert.That(LegacyCondition.Evaluate(null, new UserAttributes { { "device_type", "IPhone" } }, Logger), Is.False);
        }

        [Test]
        public void TestEvaluateWithInvalidTypeProperty()
        {
            BaseCondition condition = new BaseCondition { Name = "input_value", Value = "Android", Match = "exists", Type = "invalid_type" };
            Assert.That(condition.Evaluate(null, new UserAttributes { { "device_type", "iPhone" } }, Logger), Is.Null);

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{condition}"" has an unknown condition type. You may need to upgrade to a newer release of the Optimizely SDK"), Times.Once);
        }

        [Test]
        public void TestEvaluateWithMissingTypeProperty()
        {
            var condition = new BaseCondition { Name = "input_value", Value = "Android", Match = "exists" };
            Assert.That(condition.Evaluate(null, new UserAttributes { { "device_type", "iPhone" } }, Logger), Is.Null);

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{condition}"" has an unknown condition type. You may need to upgrade to a newer release of the Optimizely SDK"), Times.Once);
        }

        [Test]
        public void TestEvaluateWithInvalidMatchProperty()
        {
            BaseCondition condition = new BaseCondition { Name = "device_type", Value = "Android", Match = "invalid_match", Type = "custom_attribute" };
            Assert.That(condition.Evaluate(null, new UserAttributes { { "device_type", "Android" } }, Logger), Is.Null);

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{condition}"" uses an unknown match type. You may need to upgrade to a newer release of the Optimizely SDK"), Times.Once);
        }

        [Test]
        public void TestEvaluateLogsWarningAndReturnNullWhenAttributeIsNotProvidedAndConditionTypeIsNotExists()
        {
            Assert.That(ExactBoolCondition.Evaluate(null, new UserAttributes { }, Logger), Is.Null);
            Assert.That(SubstrCondition.Evaluate(null, new UserAttributes { }, Logger), Is.Null);
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { }, Logger), Is.Null);
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { }, Logger), Is.Null);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""is_registered_user"",""value"":false} evaluated to UNKNOWN because no value was passed for user attribute ""is_registered_user"""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""substring"",""name"":""location"",""value"":""USA""} evaluated to UNKNOWN because no value was passed for user attribute ""location"""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":10} evaluated to UNKNOWN because no value was passed for user attribute ""distance_lt"""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":10} evaluated to UNKNOWN because no value was passed for user attribute ""distance_gt"""), Times.Once);
        }

        [Test]
        public void TestEvaluateLogsAndReturnNullWhenAttributeValueIsNullAndConditionTypeIsNotExists()
        {
            Assert.That(ExactBoolCondition.Evaluate(null, new UserAttributes { { "is_registered_user", null } }, Logger), Is.Null);
            Assert.That(SubstrCondition.Evaluate(null, new UserAttributes { { "location", null } }, Logger), Is.Null);
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", null } }, Logger), Is.Null);
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", null } }, Logger), Is.Null);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""is_registered_user"",""value"":false} evaluated to UNKNOWN because a null value was passed for user attribute ""is_registered_user"""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""substring"",""name"":""location"",""value"":""USA""} evaluated to UNKNOWN because a null value was passed for user attribute ""location"""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":10} evaluated to UNKNOWN because a null value was passed for user attribute ""distance_lt"""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":10} evaluated to UNKNOWN because a null value was passed for user attribute ""distance_gt"""), Times.Once);
        }

        [Test]
        public void TestEvaluateReturnsFalseAndDoesNotLogForExistsConditionWhenAttributeIsNotProvided()
        {
            Assert.That(ExistsCondition.Evaluate(null, new UserAttributes(), Logger), Is.False);
            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void TestEvaluateLogsWarningAndReturnNullWhenAttributeTypeIsInvalid()
        {
            Assert.That(ExactBoolCondition.Evaluate(null, new UserAttributes { { "is_registered_user", 5 } }, Logger), Is.Null);
            Assert.That(SubstrCondition.Evaluate(null, new UserAttributes { { "location", false } }, Logger), Is.Null);
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", "invalid" } }, Logger), Is.Null);
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", true } }, Logger), Is.Null);

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""is_registered_user"",""value"":false} evaluated to UNKNOWN because a value of type ""Int32"" was passed for user attribute ""is_registered_user"""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""substring"",""name"":""location"",""value"":""USA""} evaluated to UNKNOWN because a value of type ""Boolean"" was passed for user attribute ""location"""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":10} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""distance_lt"""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":10} evaluated to UNKNOWN because a value of type ""Boolean"" was passed for user attribute ""distance_gt"""), Times.Once);
        }

        [Test]
        public void TestEvaluateLogsWarningAndReturnNullWhenConditionTypeIsInvalid()
        {
            var invalidCondition = new BaseCondition { Name = "is_registered_user", Value = new string[] { }, Match = "exact", Type = "custom_attribute" };
            Assert.That(invalidCondition.Evaluate(null, new UserAttributes { { "is_registered_user", true } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""is_registered_user"",""value"":[]} has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK"), Times.Once);

            invalidCondition = new BaseCondition { Name = "location", Value = 25, Match = "substring", Type = "custom_attribute" };
            Assert.That(invalidCondition.Evaluate(null, new UserAttributes { { "location", "USA" } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""substring"",""name"":""location"",""value"":25} has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK"), Times.Once);

            invalidCondition = new BaseCondition { Name = "distance_lt", Value = "invalid", Match = "lt", Type = "custom_attribute" };
            Assert.That(invalidCondition.Evaluate(null, new UserAttributes { { "distance_lt", 5 } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":""invalid""} has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK"), Times.Once);

            invalidCondition = new BaseCondition { Name = "distance_gt", Value = "invalid", Match = "gt", Type = "custom_attribute" };
            Assert.That(invalidCondition.Evaluate(null, new UserAttributes { { "distance_gt", "invalid" } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":""invalid""} has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK"), Times.Once);
        }

        #endregion // Evaluate Tests

        #region ExactMatcher Tests

        [Test]
        public void TestExactMatcherReturnsFalseWhenAttributeValueDoesNotMatch()
        {
            Assert.That(ExactStrCondition.Evaluate(null, new UserAttributes { { "browser_type", "chrome" } }, Logger), Is.False);
            Assert.That(ExactBoolCondition.Evaluate(null, new UserAttributes { { "is_registered_user", true } }, Logger), Is.False);
            Assert.That(ExactDecimalCondition.Evaluate(null, new UserAttributes { { "pi_value", 2.5 } }, Logger), Is.False);
            Assert.That(ExactIntCondition.Evaluate(null, new UserAttributes { { "lasers_count", 55 } }, Logger), Is.False);
        }

        [Test]
        public void TestExactMatcherReturnsNullWhenTypeMismatch()
        {
            Assert.That(ExactStrCondition.Evaluate(null, new UserAttributes { { "browser_type", true } }, Logger), Is.Null);
            Assert.That(ExactBoolCondition.Evaluate(null, new UserAttributes { { "is_registered_user", "abcd" } }, Logger), Is.Null);
            Assert.That(ExactDecimalCondition.Evaluate(null, new UserAttributes { { "pi_value", false } }, Logger), Is.Null);
            Assert.That(ExactIntCondition.Evaluate(null, new UserAttributes { { "lasers_count", "infinity" } }, Logger), Is.Null);

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""browser_type"",""value"":""firefox""} evaluated to UNKNOWN because a value of type ""Boolean"" was passed for user attribute ""browser_type"""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""is_registered_user"",""value"":false} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""is_registered_user"""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""pi_value"",""value"":3.14} evaluated to UNKNOWN because a value of type ""Boolean"" was passed for user attribute ""pi_value"""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""lasers_count"",""value"":9000} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""lasers_count"""), Times.Once);
        }

        [Test]
        public void TestExactMatcherReturnsNullForOutOfBoundNumericValues()
        {
            Assert.That(ExactIntCondition.Evaluate(null, new UserAttributes { { "lasers_count", double.NegativeInfinity } }, Logger), Is.Null);
            Assert.That(ExactDecimalCondition.Evaluate(null, new UserAttributes { { "pi_value", Math.Pow(2, 53) + 2 } }, Logger), Is.Null);
            Assert.That(InfinityIntCondition.Evaluate(null, new UserAttributes { { "max_num_value", 15 } }, Logger), Is.Null);

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""lasers_count"",""value"":9000} evaluated to UNKNOWN because the number value for user attribute ""lasers_count"" is not in the range [-2^53, +2^53]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""pi_value"",""value"":3.14} evaluated to UNKNOWN because the number value for user attribute ""pi_value"" is not in the range [-2^53, +2^53]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""max_num_value"",""value"":9223372036854775807} has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK"), Times.Once);
        }

        [Test]
        public void TestExactMatcherReturnsTrueWhenAttributeValueMatches()
        {
            Assert.That(ExactStrCondition.Evaluate(null, new UserAttributes { { "browser_type", "firefox" } }, Logger), Is.True);
            Assert.That(ExactBoolCondition.Evaluate(null, new UserAttributes { { "is_registered_user", false } }, Logger), Is.True);
            Assert.That(ExactDecimalCondition.Evaluate(null, new UserAttributes { { "pi_value", 3.14 } }, Logger), Is.True);
            Assert.That(ExactIntCondition.Evaluate(null, new UserAttributes { { "lasers_count", 9000 } }, Logger), Is.True);
        }

        #endregion // ExactMatcher Tests

        #region ExistsMatcher Tests

        [Test]
        public void TestExistsMatcherReturnsFalseWhenAttributeIsNotProvided()
        {
            Assert.That(ExistsCondition.Evaluate(null, new UserAttributes { }, Logger), Is.False);
        }

        [Test]
        public void TestExistsMatcherReturnsFalseWhenAttributeIsNull()
        {
            Assert.That(ExistsCondition.Evaluate(null, new UserAttributes { { "input_value", null } }, Logger), Is.False);
        }

        [Test]
        public void TestExistsMatcherReturnsTrueWhenAttributeValueIsProvided()
        {
            Assert.That(ExistsCondition.Evaluate(null, new UserAttributes { { "input_value", "" } }, Logger), Is.True);
            Assert.That(ExistsCondition.Evaluate(null, new UserAttributes { { "input_value", "iPhone" } }, Logger), Is.True);
            Assert.That(ExistsCondition.Evaluate(null, new UserAttributes { { "input_value", 10 } }, Logger), Is.True);
            Assert.That(ExistsCondition.Evaluate(null, new UserAttributes { { "input_value", false } }, Logger), Is.True);
        }

        #endregion // ExistsMatcher Tests

        #region SubstringMatcher Tests

        [Test]
        public void TestSubstringMatcherReturnsFalseWhenAttributeValueIsNotASubstring()
        {
            Assert.That(SubstrCondition.Evaluate(null, new UserAttributes { { "location", "Los Angeles" } }, Logger), Is.False);
        }

        [Test]
        public void TestSubstringMatcherReturnsNullWhenAttributeValueIsNotAString()
        {
            Assert.That(SubstrCondition.Evaluate(null, new UserAttributes { { "location", 10.5 } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""substring"",""name"":""location"",""value"":""USA""} evaluated to UNKNOWN because a value of type ""Double"" was passed for user attribute ""location"""), Times.Once);
        }

        [Test]
        public void TestSubstringMatcherReturnsTrueWhenAttributeValueIsASubstring()
        {
            Assert.That(SubstrCondition.Evaluate(null, new UserAttributes { { "location", "USA" } }, Logger), Is.True);
            Assert.That(SubstrCondition.Evaluate(null, new UserAttributes { { "location", "San Francisco, USA" } }, Logger), Is.True);
        }

        #endregion // SubstringMatcher Tests

        #region GTMatcher Tests

        [Test]
        public void TestGTMatcherReturnsFalseWhenAttributeValueIsLessThanOrEqualToConditionValue()
        {
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", 5 } }, Logger), Is.False);
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", 10 } }, Logger), Is.False);
        }

        [Test]
        public void TestGTMatcherReturnsNullWhenAttributeValueIsNotANumericValue()
        {
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", "invalid_type" } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":10} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""distance_gt"""), Times.Once);
        }

        [Test]
        public void TestGTMatcherReturnsNullWhenAttributeValueIsOutOfBounds()
        {
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", double.PositiveInfinity } }, Logger), Is.Null);
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", Math.Pow(2, 53) + 2 } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":10} evaluated to UNKNOWN because the number value for user attribute ""distance_gt"" is not in the range [-2^53, +2^53]"), Times.Exactly(2));
        }

        [Test]
        public void TestGTMatcherReturnsTrueWhenAttributeValueIsGreaterThanConditionValue()
        {
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", 15 } }, Logger), Is.True);
        }

        #endregion // GTMatcher Tests

        #region LTMatcher Tests

        [Test]
        public void TestLTMatcherReturnsFalseWhenAttributeValueIsGreaterThanOrEqualToConditionValue()
        {
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", 15 } }, Logger), Is.False);
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", 10 } }, Logger), Is.False);
        }

        [Test]
        public void TestLTMatcherReturnsNullWhenAttributeValueIsNotANumericValue()
        {
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", "invalid_type" } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":10} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""distance_lt"""), Times.Once);
        }

        [Test]
        public void TestLTMatcherReturnsNullWhenAttributeValueIsOutOfBounds()
        {
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", double.NegativeInfinity } }, Logger), Is.Null);
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", -Math.Pow(2, 53) - 2 } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":10} evaluated to UNKNOWN because the number value for user attribute ""distance_lt"" is not in the range [-2^53, +2^53]"), Times.Exactly(2));
        }

        [Test]
        public void TestLTMatcherReturnsTrueWhenAttributeValueIsLessThanConditionValue()
        {
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", 5 } }, Logger), Is.True);
        }

        #endregion // LTMatcher Tests
    }
}
