/*
 * Copyright 2019-2020, Optimizely
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

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{condition}"" uses an unknown condition type. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);
        }

        [Test]
        public void TestEvaluateWithMissingTypeProperty()
        {
            var condition = new BaseCondition { Name = "input_value", Value = "Android", Match = "exists" };
            Assert.That(condition.Evaluate(null, new UserAttributes { { "device_type", "iPhone" } }, Logger), Is.Null);

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{condition}"" uses an unknown condition type. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);
        }

        [Test]
        public void TestEvaluateWithInvalidMatchProperty()
        {
            BaseCondition condition = new BaseCondition { Name = "device_type", Value = "Android", Match = "invalid_match", Type = "custom_attribute" };
            Assert.That(condition.Evaluate(null, new UserAttributes { { "device_type", "Android" } }, Logger), Is.Null);

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{condition}"" uses an unknown match type. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);
        }

        [Test]
        public void TestEvaluateLogsWarningAndReturnNullWhenAttributeIsNotProvidedAndConditionTypeIsNotExists()
        {
            Assert.That(ExactBoolCondition.Evaluate(null, new UserAttributes { }, Logger), Is.Null);
            Assert.That(SubstrCondition.Evaluate(null, new UserAttributes { }, Logger), Is.Null);
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { }, Logger), Is.Null);
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { }, Logger), Is.Null);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""is_registered_user"",""value"":false} evaluated to UNKNOWN because no value was passed for user attribute ""is_registered_user""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""substring"",""name"":""location"",""value"":""USA""} evaluated to UNKNOWN because no value was passed for user attribute ""location""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":10} evaluated to UNKNOWN because no value was passed for user attribute ""distance_lt""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":10} evaluated to UNKNOWN because no value was passed for user attribute ""distance_gt""."), Times.Once);
        }

        [Test]
        public void TestEvaluateLogsAndReturnNullWhenAttributeValueIsNullAndConditionTypeIsNotExists()
        {
            Assert.That(ExactBoolCondition.Evaluate(null, new UserAttributes { { "is_registered_user", null } }, Logger), Is.Null);
            Assert.That(SubstrCondition.Evaluate(null, new UserAttributes { { "location", null } }, Logger), Is.Null);
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", null } }, Logger), Is.Null);
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", null } }, Logger), Is.Null);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""is_registered_user"",""value"":false} evaluated to UNKNOWN because a null value was passed for user attribute ""is_registered_user""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""substring"",""name"":""location"",""value"":""USA""} evaluated to UNKNOWN because a null value was passed for user attribute ""location""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":10} evaluated to UNKNOWN because a null value was passed for user attribute ""distance_lt""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":10} evaluated to UNKNOWN because a null value was passed for user attribute ""distance_gt""."), Times.Once);
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

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""is_registered_user"",""value"":false} evaluated to UNKNOWN because a value of type ""Int32"" was passed for user attribute ""is_registered_user""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""substring"",""name"":""location"",""value"":""USA""} evaluated to UNKNOWN because a value of type ""Boolean"" was passed for user attribute ""location""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":10} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""distance_lt""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":10} evaluated to UNKNOWN because a value of type ""Boolean"" was passed for user attribute ""distance_gt""."), Times.Once);
        }

        [Test]
        public void TestEvaluateLogsWarningAndReturnNullWhenConditionTypeIsInvalid()
        {
            var invalidCondition = new BaseCondition { Name = "is_registered_user", Value = new string[] { }, Match = "exact", Type = "custom_attribute" };
            Assert.That(invalidCondition.Evaluate(null, new UserAttributes { { "is_registered_user", true } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""is_registered_user"",""value"":[]} has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);

            invalidCondition = new BaseCondition { Name = "location", Value = 25, Match = "substring", Type = "custom_attribute" };
            Assert.That(invalidCondition.Evaluate(null, new UserAttributes { { "location", "USA" } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""substring"",""name"":""location"",""value"":25} has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);

            invalidCondition = new BaseCondition { Name = "distance_lt", Value = "invalid", Match = "lt", Type = "custom_attribute" };
            Assert.That(invalidCondition.Evaluate(null, new UserAttributes { { "distance_lt", 5 } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":""invalid""} has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);

            invalidCondition = new BaseCondition { Name = "distance_gt", Value = "invalid", Match = "gt", Type = "custom_attribute" };
            Assert.That(invalidCondition.Evaluate(null, new UserAttributes { { "distance_gt", "invalid" } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":""invalid""} has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);
        }

        #endregion Evaluate Tests

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

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""browser_type"",""value"":""firefox""} evaluated to UNKNOWN because a value of type ""Boolean"" was passed for user attribute ""browser_type""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""is_registered_user"",""value"":false} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""is_registered_user""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""pi_value"",""value"":3.14} evaluated to UNKNOWN because a value of type ""Boolean"" was passed for user attribute ""pi_value""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""lasers_count"",""value"":9000} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""lasers_count""."), Times.Once);
        }

        [Test]
        public void TestExactMatcherReturnsNullForOutOfBoundNumericValues()
        {
            Assert.That(ExactIntCondition.Evaluate(null, new UserAttributes { { "lasers_count", double.NegativeInfinity } }, Logger), Is.Null);
            Assert.That(ExactDecimalCondition.Evaluate(null, new UserAttributes { { "pi_value", Math.Pow(2, 53) + 2 } }, Logger), Is.Null);
            Assert.That(InfinityIntCondition.Evaluate(null, new UserAttributes { { "max_num_value", 15 } }, Logger), Is.Null);

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""lasers_count"",""value"":9000} evaluated to UNKNOWN because the number value for user attribute ""lasers_count"" is not in the range [-2^53, +2^53]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""pi_value"",""value"":3.14} evaluated to UNKNOWN because the number value for user attribute ""pi_value"" is not in the range [-2^53, +2^53]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""max_num_value"",""value"":9223372036854775807} has an unsupported condition value. You may need to upgrade to a newer release of the Optimizely SDK."), Times.Once);
        }

        [Test]
        public void TestExactMatcherReturnsTrueWhenAttributeValueMatches()
        {
            Assert.That(ExactStrCondition.Evaluate(null, new UserAttributes { { "browser_type", "firefox" } }, Logger), Is.True);
            Assert.That(ExactBoolCondition.Evaluate(null, new UserAttributes { { "is_registered_user", false } }, Logger), Is.True);
            Assert.That(ExactDecimalCondition.Evaluate(null, new UserAttributes { { "pi_value", 3.14 } }, Logger), Is.True);
            Assert.That(ExactIntCondition.Evaluate(null, new UserAttributes { { "lasers_count", 9000 } }, Logger), Is.True);
        }

        #endregion ExactMatcher Tests

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

        #endregion ExistsMatcher Tests

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
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""substring"",""name"":""location"",""value"":""USA""} evaluated to UNKNOWN because a value of type ""Double"" was passed for user attribute ""location""."), Times.Once);
        }

        [Test]
        public void TestSubstringMatcherReturnsTrueWhenAttributeValueIsASubstring()
        {
            Assert.That(SubstrCondition.Evaluate(null, new UserAttributes { { "location", "USA" } }, Logger), Is.True);
            Assert.That(SubstrCondition.Evaluate(null, new UserAttributes { { "location", "San Francisco, USA" } }, Logger), Is.True);
        }

        #endregion SubstringMatcher Tests

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
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":10} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""distance_gt""."), Times.Once);
        }

        [Test]
        public void TestGTMatcherReturnsNullWhenAttributeValueIsOutOfBounds()
        {
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", double.PositiveInfinity } }, Logger), Is.Null);
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", Math.Pow(2, 53) + 2 } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""gt"",""name"":""distance_gt"",""value"":10} evaluated to UNKNOWN because the number value for user attribute ""distance_gt"" is not in the range [-2^53, +2^53]."), Times.Exactly(2));
        }

        [Test]
        public void TestGTMatcherReturnsTrueWhenAttributeValueIsGreaterThanConditionValue()
        {
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", 15 } }, Logger), Is.True);
        }

        [Test]
        public void TestSemVerGTTargetBetaComplex()
        {
            var semverGTCondition = new BaseCondition { Name = "semversion_gt", Value = "2.1.3-beta+1", Match = "semver_gt", Type = "custom_attribute" };
            Assert.IsTrue(semverGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "2.1.3-beta+1.2.3" } }, Logger) ?? false);
        }

        [Test]
        public void TestSemVerGTCompareAgainstPreReleaseToPreRelease()
        {
            var semverGTCondition = new BaseCondition { Name = "semversion_gt", Value = "3.7.1-prerelease+build", Match = "semver_gt", Type = "custom_attribute" };
            Assert.IsTrue(semverGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.7.1-prerelease+rc" } }, Logger) ?? false);
        }

        [Test]
        public void TestSemVerGTComparePrereleaseSmallerThanBuild()
        {
            var semverGTCondition = new BaseCondition { Name = "semversion_gt", Value = "3.7.1-prerelease", Match = "semver_gt", Type = "custom_attribute" };
            Assert.IsTrue(semverGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.7.1+build" } }, Logger) ?? false);
        }

        #endregion GTMatcher Tests

        #region GEMatcher Tests

        [Test]
        public void TestGEMatcherReturnsFalseWhenAttributeValueIsLessButTrueForEqualToConditionValue()
        {
            Assert.IsFalse(GECondition.Evaluate(null, new UserAttributes { { "distance_ge", 5 } }, Logger) ?? true);
            Assert.IsTrue(GECondition.Evaluate(null, new UserAttributes { { "distance_ge", 10 } }, Logger) ?? false);
        }

        [Test]
        public void TestGEMatcherReturnsNullWhenAttributeValueIsNotANumericValue()
        {
            Assert.IsNull(GECondition.Evaluate(null, new UserAttributes { { "distance_ge", "invalid_type" } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""ge"",""name"":""distance_ge"",""value"":10} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""distance_ge""."), Times.Once);
        }

        [Test]
        public void TestGEMatcherReturnsNullWhenAttributeValueIsOutOfBounds()
        {
            Assert.IsNull(GECondition.Evaluate(null, new UserAttributes { { "distance_ge", double.PositiveInfinity } }, Logger));
            Assert.IsNull(GECondition.Evaluate(null, new UserAttributes { { "distance_ge", Math.Pow(2, 53) + 2 } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""ge"",""name"":""distance_ge"",""value"":10} evaluated to UNKNOWN because the number value for user attribute ""distance_ge"" is not in the range [-2^53, +2^53]."), Times.Exactly(2));
        }

        [Test]
        public void TestGEMatcherReturnsTrueWhenAttributeValueIsGreaterThanConditionValue()
        {
            Assert.IsTrue(GECondition.Evaluate(null, new UserAttributes { { "distance_ge", 15 } }, Logger) ?? false);
        }

        #endregion GEMatcher Tests

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
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":10} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""distance_lt""."), Times.Once);
        }

        [Test]
        public void TestLTMatcherReturnsNullWhenAttributeValueIsOutOfBounds()
        {
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", double.NegativeInfinity } }, Logger), Is.Null);
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", -Math.Pow(2, 53) - 2 } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""lt"",""name"":""distance_lt"",""value"":10} evaluated to UNKNOWN because the number value for user attribute ""distance_lt"" is not in the range [-2^53, +2^53]."), Times.Exactly(2));
        }

        [Test]
        public void TestLTMatcherReturnsTrueWhenAttributeValueIsLessThanConditionValue()
        {
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", 5 } }, Logger), Is.True);
        }

        [Test]
        public void TestSemVerLTTargetBuildComplex()
        {
            var semverLTCondition = new BaseCondition { Name = "semversion_lt", Value = "2.1.3-beta+1.2.3", Match = "semver_lt", Type = "custom_attribute" };
            Assert.IsTrue(semverLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "2.1.3-beta+1" } }, Logger) ?? false);
        }

        [Test]
        public void TestSemVerLTCompareMultipleDash()
        {
            var semverLTCondition = new BaseCondition { Name = "semversion_lt", Value = "2.1.3-beta-1.2.3", Match = "semver_lt", Type = "custom_attribute" };
            Assert.IsTrue(semverLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "2.1.3-beta-1" } }, Logger) ?? false);
        }

        #endregion LTMatcher Tests

        #region LEMatcher Tests

        [Test]
        public void TestLEMatcherReturnsFalseWhenAttributeValueIsGreaterAndTrueIfEqualToConditionValue()
        {
            Assert.IsFalse(LECondition.Evaluate(null, new UserAttributes { { "distance_le", 15 } }, Logger) ?? true);
            Assert.IsTrue(LECondition.Evaluate(null, new UserAttributes { { "distance_le", 10 } }, Logger) ?? false);
        }

        [Test]
        public void TestLEMatcherReturnsNullWhenAttributeValueIsNotANumericValue()
        {
            Assert.IsNull(LECondition.Evaluate(null, new UserAttributes { { "distance_le", "invalid_type" } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""le"",""name"":""distance_le"",""value"":10} evaluated to UNKNOWN because a value of type ""String"" was passed for user attribute ""distance_le""."), Times.Once);
        }

        [Test]
        public void TestLEMatcherReturnsNullWhenAttributeValueIsOutOfBounds()
        {
            Assert.IsNull(LECondition.Evaluate(null, new UserAttributes { { "distance_le", double.NegativeInfinity } }, Logger));
            Assert.IsNull(LECondition.Evaluate(null, new UserAttributes { { "distance_le", -Math.Pow(2, 53) - 2 } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, @"Audience condition {""type"":""custom_attribute"",""match"":""le"",""name"":""distance_le"",""value"":10} evaluated to UNKNOWN because the number value for user attribute ""distance_le"" is not in the range [-2^53, +2^53]."), Times.Exactly(2));
        }

        [Test]
        public void TestLEMatcherReturnsTrueWhenAttributeValueIsLessThanConditionValue()
        {
            Assert.IsTrue(LECondition.Evaluate(null, new UserAttributes { { "distance_le", 5 } }, Logger) ?? false);
        }

        #endregion LEMatcher Tests

        #region SemVerLTMatcher Tests

        [Test]
        public void TestSemVerLTMatcherReturnsFalseWhenAttributeValueIsGreaterThanOrEqualToConditionValue()
        {
            Assert.IsFalse(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.7.2" } }, Logger) ?? true);
            Assert.IsFalse(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.7.1" } }, Logger) ?? true);
            Assert.IsFalse(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.8" } }, Logger) ?? true);
            Assert.IsFalse(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "4" } }, Logger) ?? true);
        }

        [Test]
        public void TestSemVerLTMatcherReturnsTrueWhenAttributeValueIsLessThanConditionValue()
        {
            Assert.IsTrue(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.7.0" } }, Logger) ?? false);
            Assert.IsTrue(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.7.1-beta" } }, Logger) ?? false);
            Assert.IsTrue(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "2.7.1" } }, Logger) ?? false);
            Assert.IsTrue(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.7" } }, Logger) ?? false);
            Assert.IsTrue(SemVerLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3" } }, Logger) ?? false);
        }

        [Test]
        public void TestSemVerLTMatcherReturnsTrueWhenAttributeValueIsLessThanConditionValueBeta()
        {
            var semverLTCondition = new BaseCondition { Name = "semversion_lt", Value = "3.7.0-beta.2.3", Match = "semver_lt", Type = "custom_attribute" };
            Assert.IsTrue(semverLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.7.0-beta.2.1" } }, Logger) ?? false);
            Assert.IsTrue(semverLTCondition.Evaluate(null, new UserAttributes { { "semversion_lt", "3.7.0-beta" } }, Logger) ?? false);
        }

        #endregion SemVerLTMatcher Tests

        #region SemVerGTMatcher Tests

        [Test]
        public void TestSemVerGTMatcherReturnsFalseWhenAttributeValueIsLessThanOrEqualToConditionValue()
        {
            Assert.IsFalse(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.7.0" } }, Logger) ?? true);
            Assert.IsFalse(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.7.1" } }, Logger) ?? true);
            Assert.IsFalse(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.6" } }, Logger) ?? true);
            Assert.IsFalse(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "2" } }, Logger) ?? true);
        }

        [Test]
        public void TestSemVerGTMatcherReturnsTrueWhenAttributeValueIsGreaterThanConditionValue()
        {
            Assert.IsTrue(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.7.2" } }, Logger) ?? false);
            Assert.IsTrue(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.7.2-beta" } }, Logger) ?? false);
            Assert.IsTrue(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "4.7.1" } }, Logger) ?? false);
            Assert.IsTrue(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.8" } }, Logger) ?? false);
            Assert.IsTrue(SemVerGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "4" } }, Logger) ?? false);
        }

        [Test]
        public void TestSemVerGTMatcherReturnsTrueWhenAttributeValueIsGreaterThanConditionValueBeta()
        {
            var semverGTCondition = new BaseCondition { Name = "semversion_gt", Value = "3.7.0-beta.2.3", Match = "semver_gt", Type = "custom_attribute" };
            Assert.IsTrue(semverGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.7.0-beta.2.4" } }, Logger) ?? false);
            Assert.IsTrue(semverGTCondition.Evaluate(null, new UserAttributes { { "semversion_gt", "3.7.0" } }, Logger) ?? false);
        }

        #endregion SemVerGTMatcher Tests

        #region SemVerEQMatcher Tests

        [Test]
        public void TestSemVerEQMatcherReturnsFalseWhenAttributeValueIsNotEqualToConditionValue()
        {
            Assert.IsFalse(SemVerEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3.7.0" } }, Logger) ?? true);
            Assert.IsFalse(SemVerEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3.7.2" } }, Logger) ?? true);
            Assert.IsFalse(SemVerEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3.6" } }, Logger) ?? true);
            Assert.IsFalse(SemVerEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "2" } }, Logger) ?? true);
            Assert.IsFalse(SemVerEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "4" } }, Logger) ?? true);
            Assert.IsFalse(SemVerEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3" } }, Logger) ?? true);
        }

        [Test]
        public void TestSemVerEQMatcherReturnsTrueWhenAttributeValueIsEqualToConditionValue()
        {
            Assert.IsTrue(SemVerEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3.7.1" } }, Logger) ?? false);
        }

        [Test]
        public void TestSemVerEQMatcherReturnsTrueWhenAttributeValueIsEqualToConditionValueMajorOnly()
        {
            var semverEQCondition = new BaseCondition { Name = "semversion_eq", Value = "3", Match = "semver_eq", Type = "custom_attribute" };
            Assert.IsTrue(semverEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3.0.0" } }, Logger) ?? false);
            Assert.IsTrue(semverEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3.1" } }, Logger) ?? false);
        }

        [Test]
        public void TestSemVerEQMatcherReturnsFalseOrFalseWhenAttributeValueIsNotEqualToConditionValueMajorOnly()
        {
            var semverEQCondition = new BaseCondition { Name = "semversion_eq", Value = "3", Match = "semver_eq", Type = "custom_attribute" };
            Assert.IsFalse(semverEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "4.0" } }, Logger) ?? true);
            Assert.IsFalse(semverEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "2" } }, Logger) ?? true);
        }

        [Test]
        public void TestSemVerEQMatcherReturnsTrueWhenAttributeValueIsEqualToConditionValueBeta()
        {
            var semverEQCondition = new BaseCondition { Name = "semversion_eq", Value = "3.7.0-beta.2.3", Match = "semver_eq", Type = "custom_attribute" };
            Assert.IsTrue(semverEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "3.7.0-beta.2.3" } }, Logger) ?? false);
        }

        [Test]
        public void TestSemVerEQTargetBuildIgnores()
        {
            var semverEQCondition = new BaseCondition { Name = "semversion_eq", Value = "2.1.3", Match = "semver_eq", Type = "custom_attribute" };
            Assert.IsTrue(semverEQCondition.Evaluate(null, new UserAttributes { { "semversion_eq", "2.1.3+build" } }, Logger) ?? false);
        }

        #endregion SemVerEQMatcher Tests

        #region SemVerGEMatcher Tests

        [Test]
        public void TestSemVerGEMatcherReturnsFalseWhenAttributeValueIsNotGreaterOrEqualToConditionValue()
        {
            Assert.IsFalse(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.0" } }, Logger) ?? true);
            Assert.IsFalse(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.1-beta" } }, Logger) ?? true);
            Assert.IsFalse(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.6" } }, Logger) ?? true);
            Assert.IsFalse(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "2" } }, Logger) ?? true);
            Assert.IsFalse(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3" } }, Logger) ?? true);
        }

        [Test]
        public void TestSemVerGEMatcherReturnsTrueWhenAttributeValueIsGreaterOrEqualToConditionValue()
        {
            Assert.IsTrue(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.1" } }, Logger) ?? false);
            Assert.IsTrue(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.2" } }, Logger) ?? false);
            Assert.IsTrue(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.8.1" } }, Logger) ?? false); ;
            Assert.IsTrue(SemVerGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "4.7.1" } }, Logger) ?? false);
        }

        [Test]
        public void TestSemVerGEMatcherReturnsTrueWhenAttributeValueIsGreaterOrEqualToConditionValueMajorOnly()
        {
            var semverGECondition = new BaseCondition { Name = "semversion_ge", Value = "3", Match = "semver_ge", Type = "custom_attribute" };
            Assert.IsTrue(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.0" } }, Logger) ?? false);
            Assert.IsTrue(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.0.0" } }, Logger) ?? false);
            Assert.IsTrue(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "4.0" } }, Logger) ?? false);
        }

        [Test]
        public void TestSemVerGEMatcherReturnsFalseWhenAttributeValueIsNotGreaterOrEqualToConditionValueMajorOnly()
        {
            var semverGECondition = new BaseCondition { Name = "semversion_ge", Value = "3", Match = "semver_ge", Type = "custom_attribute" };
            Assert.IsFalse(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "2" } }, Logger) ?? true);
        }

        [Test]
        public void TestSemVerGEMatcherReturnsTrueWhenAttributeValueIsGreaterOrEqualToConditionValueBeta()
        {
            var semverGECondition = new BaseCondition { Name = "semversion_ge", Value = "3.7.0-beta.2.3", Match = "semver_ge", Type = "custom_attribute" };
            Assert.IsTrue(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.0-beta.2.3" } }, Logger) ?? false);
            Assert.IsTrue(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.0-beta.2.4" } }, Logger) ?? false);
            Assert.IsTrue(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.0-beta.2.3+1.2.3" } }, Logger) ?? false);
            Assert.IsTrue(semverGECondition.Evaluate(null, new UserAttributes { { "semversion_ge", "3.7.1-beta.2.3" } }, Logger) ?? false);
        }

        #endregion SemVerGEMatcher Tests

        #region SemVerLEMatcher Tests

        [Test]
        public void TestSemVerLEMatcherReturnsFalseWhenAttributeValueIsNotLessOrEqualToConditionValue()
        {
            Assert.IsFalse(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.2" } }, Logger) ?? true);
            Assert.IsFalse(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.8" } }, Logger) ?? true);
            Assert.IsFalse(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "4" } }, Logger) ?? true);
        }

        [Test]
        public void TestSemVerLEMatcherReturnsTrueWhenAttributeValueIsLessOrEqualToConditionValue()
        {
            Assert.IsTrue(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.1" } }, Logger) ?? false);
            Assert.IsTrue(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.0" } }, Logger) ?? false);
            Assert.IsTrue(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.6.1" } }, Logger) ?? false);
            Assert.IsTrue(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "2.7.1" } }, Logger) ?? false);
            Assert.IsTrue(SemVerLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.1-beta" } }, Logger) ?? false);
        }

        [Test]
        public void TestSemVerLEMatcherReturnsTrueWhenAttributeValueIsLessOrEqualToConditionValueMajorOnly()
        {
            var semverLECondition = new BaseCondition { Name = "semversion_le", Value = "3", Match = "semver_le", Type = "custom_attribute" };
            Assert.IsTrue(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.0-beta.2.4" } }, Logger) ?? false);
            Assert.IsTrue(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.1-beta" } }, Logger) ?? false);
            Assert.IsTrue(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.0.0" } }, Logger) ?? false);
            Assert.IsTrue(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "2.0" } }, Logger) ?? false);
        }

        [Test]
        public void TestSemVerLEMatcherReturnsFalseWhenAttributeValueIsNotLessOrEqualToConditionValueMajorOnly()
        {
            var semverLECondition = new BaseCondition { Name = "semversion_le", Value = "3", Match = "semver_le", Type = "custom_attribute" };
            Assert.IsFalse(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "4" } }, Logger) ?? true);
        }

        [Test]
        public void TestSemVerLEMatcherReturnsTrueWhenAttributeValueIsLessOrEqualToConditionValueBeta()
        {
            var semverLECondition = new BaseCondition { Name = "semversion_le", Value = "3.7.0-beta.2.3", Match = "semver_le", Type = "custom_attribute" };
            Assert.IsTrue(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.0-beta.2.2" } }, Logger) ?? false);
            Assert.IsTrue(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.0-beta.2.3" } }, Logger) ?? false);
            Assert.IsTrue(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.7.0-beta.2.2+1.2.3" } }, Logger) ?? false);
            Assert.IsTrue(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", "3.6.1-beta.2.3+1.2" } }, Logger) ?? false);
        }

        #endregion SemVerLEMatcher Tests

        #region SemVer Invalid Scenarios

        [Test]
        public void TestInvalidSemVersions()
        {
            var invalidValues = new string[] {"-", ".", "..", "+", "+test", " ", "2 .3. 0", "2.",
                ".2.2", "3.7.2.2", "3.x", ",", "+build-prerelese"};
            var semverLECondition = new BaseCondition { Name = "semversion_le", Value = "3", Match = "semver_le", Type = "custom_attribute" };
            foreach (var invalidValue in invalidValues)
            {
                Assert.IsNull(semverLECondition.Evaluate(null, new UserAttributes { { "semversion_le", invalidValue } }, Logger), $"returned for {invalidValue}");
            }
        }

        [Test]
        public void TestNullTargetedVersion()
        {
            Assert.Throws<Exception>(() => new SemanticVersion("").CompareTo(null));
        }

        [Test]
        public void TestNullVersion()
        {
            var semver = new SemanticVersion("");

            Assert.Throws<Exception>(() => new SemanticVersion("").CompareTo(semver));
        }

        [Test]
        public void TestNullSemverVersion()
        {
            var semver = new SemanticVersion("");

            Assert.Throws<Exception>(() => semver.CompareTo(new SemanticVersion("1")));
        }

        #endregion SemVer Invalid Scenarios
    }
}