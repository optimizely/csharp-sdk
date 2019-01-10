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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests.UtilsTests
{
    [TestFixture]
    public class CustomAttributeConditionEvaluatorTest
    {
        private JToken LegacyCondition = null;
        private JToken ExistsCondition = null;
        private JToken SubstrCondition = null;
        private JToken GTCondition = null;
        private JToken LTCondition = null;
        private JToken ExactStrCondition = null;
        private JToken ExactBoolCondition = null;
        private JToken ExactDecimalCondition = null;
        private JToken ExactIntCondition = null;
        private JToken InfinityIntCondition = null;
        private Mock<ILogger> LoggerMock;
        private ILogger Logger;

        [TestFixtureSetUp]
        public void Initialize()
        {
            string LegacyConditionsStr = @"{""name"": ""device_type"", ""type"": ""custom_attribute"", ""value"": ""iPhone""}";
            string ExistsConditionStr = @"{""name"": ""input_value"", ""type"": ""custom_attribute"", ""match"": ""exists""}";
            string SubstrConditionStr = @"{""name"": ""location"", ""type"": ""custom_attribute"", ""value"": ""USA"", ""match"": ""substring""}";
            string GTConditionStr = @"{""name"": ""distance_gt"", ""type"": ""custom_attribute"", ""value"": 10, ""match"": ""gt""}";
            string LTConditionStr = @"{""name"": ""distance_lt"", ""type"": ""custom_attribute"", ""value"": 10, ""match"": ""lt""}";
            string ExactStrConditionStr = @"{""name"": ""browser_type"", ""type"": ""custom_attribute"", ""value"": ""firefox"", ""match"": ""exact""}";
            string ExactBoolConditionStr = @"{""name"": ""is_registered_user"", ""type"": ""custom_attribute"", ""value"": false, ""match"": ""exact""}";
            string ExactDecimalConditionStr = @"{""name"": ""pi_value"", ""type"": ""custom_attribute"", ""value"": 3.14, ""match"": ""exact""}";
            string ExactIntConditionStr = @"{""name"": ""lasers_count"", ""type"": ""custom_attribute"", ""value"": 9000, ""match"": ""exact""}";
            string InfinityIntConditionStr = @"{""name"": ""max_num_value"", ""type"": ""custom_attribute"", ""value"": 9223372036854775807, ""match"": ""exact""}";

            LegacyCondition = JToken.Parse(LegacyConditionsStr);
            ExistsCondition = JToken.Parse(ExistsConditionStr);
            SubstrCondition = JToken.Parse(SubstrConditionStr);
            GTCondition = JToken.Parse(GTConditionStr);
            LTCondition = JToken.Parse(LTConditionStr);
            ExactStrCondition = JToken.Parse(ExactStrConditionStr);
            ExactBoolCondition = JToken.Parse(ExactBoolConditionStr);
            ExactDecimalCondition = JToken.Parse(ExactDecimalConditionStr);
            ExactIntCondition = JToken.Parse(ExactIntConditionStr);
            InfinityIntCondition = JToken.Parse(InfinityIntConditionStr);

            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            Logger = LoggerMock.Object;
        }

        [TestFixtureTearDown]
        public void TestCleanup()
        {
            LegacyCondition = null;
            ExistsCondition = null;
            SubstrCondition = null;
            GTCondition = null;
            LTCondition = null;
            ExactStrCondition = null;
            ExactBoolCondition = null;
            ExactDecimalCondition = null;
            ExactIntCondition = null;
            InfinityIntCondition = null;
        }

        #region Evaluate Tests

        [Test]
        public void TestEvaluateWithNoMatchType()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(LegacyCondition, new UserAttributes { { "device_type", "iPhone" } }, Logger), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(LegacyCondition, new UserAttributes { { "device_type", "Android" } }, Logger), Is.False);
        }
        
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

            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactStrCondition, userAttributes, Logger), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactBoolCondition, userAttributes, Logger), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(GTCondition, userAttributes, Logger), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactDecimalCondition, userAttributes, Logger), Is.True);
        }

        [Test]
        public void TestEvaluateWithInvalidTypeProperty()
        {
            var invalidTypeCondition = JToken.Parse(@"{""name"": ""input_value"", ""match"": ""exists""}");
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(invalidTypeCondition, new UserAttributes { { "input_value", "test" } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{invalidTypeCondition.ToString(Formatting.None)}"" uses an unknown condition type."), Times.Once);

            invalidTypeCondition = JToken.Parse(@"{""name"": ""input_value"", ""type"": ""invalid"", ""match"": ""exists""}");
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(invalidTypeCondition, new UserAttributes { { "input_value", "test" } }, Logger), Is.Null);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{invalidTypeCondition.ToString(Formatting.None)}"" uses an unknown condition type."), Times.Once);
        }

        [Test]
        public void TestEvaluateWithInvalidMatchProperty()
        {
            var invalidMatchCondition = JToken.Parse(@"{""name"": ""distance_gt"", ""type"": ""custom_attribute"", ""value"": 10, ""match"": ""invalid""}");
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(invalidMatchCondition, new UserAttributes { { "distance_gt", 15 } }, Logger), Is.Null);

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{invalidMatchCondition.ToString(Formatting.None)}"" uses an unknown match type."), Times.Once);
        }

        [Test]
        public void TestEvaluateReturnsNullWithMismatchedConditionType()
        {
            var mismatchedTypeCondition = JToken.Parse(@"{""name"": ""is_firefox"", ""type"": ""custom_attribute"", ""value"": false, ""match"": ""substring""}");
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(mismatchedTypeCondition, new UserAttributes { { "is_firefox", false } }, Logger));
        }

        [Test]
        public void TestEvaluateReturnsNullAndLogsWarningWhenAttributeIsNotProvidedAndConditionIsNotExists()
        {
            var condition = JToken.Parse(@"{""name"": ""is_firefox"", ""type"": ""custom_attribute"", ""value"": false, ""match"": ""substring""}");
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(condition, new UserAttributes {}, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{condition.ToString(Formatting.None)}"" evaluated as UNKNOWN because no value was passed for user attribute ""is_firefox""."), Times.Once);
        }

        [Test]
        public void TestEvaluateReturnsFalseAndDoesNotLogForExistsConditionWhenAttributeIsNotProvided()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { }, Logger), Is.False);
            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void TestEvaluateReturnsNullWhenAttributeTypeIsInvalid()
        {
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(SubstrCondition, new UserAttributes { { "location", false } }, Logger));
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "distance_lt", "invalid" } }, Logger));
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(ExactBoolCondition, new UserAttributes { { "is_registered_user", 5 } }, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{SubstrCondition.ToString(Formatting.None)}"" evaluated as UNKNOWN because the value for user attribute ""False"" is inapplicable: ""location"""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{LTCondition.ToString(Formatting.None)}"" evaluated as UNKNOWN because the value for user attribute ""invalid"" is inapplicable: ""distance_lt"""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, $@"Audience condition ""{ExactBoolCondition.ToString(Formatting.None)}"" evaluated as UNKNOWN because the value for user attribute is ""5"" while expected is ""False""."), Times.Once);
        }

        #endregion // Evaluate Tests

        #region ExactMatcher Tests

        [Test]
        public void TestExactMatcherReturnsFalseWhenAttributeValueDoesNotMatch()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactStrCondition, new UserAttributes { { "browser_type", "chrome" } }, Logger), Is.False);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactBoolCondition, new UserAttributes { { "is_registered_user", true } }, Logger), Is.False);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactDecimalCondition, new UserAttributes { { "pi_value", 2.5 } }, Logger), Is.False);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "lasers_count", 55 } }, Logger), Is.False);
        }

        [Test]
        public void TestExactMatcherReturnsNullWhenTypeMismatch()
        {
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(ExactStrCondition, new UserAttributes { { "browser_type", true } }, Logger));
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(ExactBoolCondition, new UserAttributes { { "is_registered_user", "abcd" } }, Logger));
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(ExactDecimalCondition, new UserAttributes { { "pi_value", false } }, Logger));
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "lasers_count", "infinity" } }, Logger));
        }

        [Test]
        public void TestExactMatcherReturnsNullWithNumericInfinity()
        {
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "lasers_count", double.NegativeInfinity } }, Logger)); // Infinity value
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(InfinityIntCondition, new UserAttributes { { "max_num_value", 15 } }, Logger)); // Infinity condition
        }

        [Test]
        public void TestExactMatcherReturnsTrueWhenAttributeValueMatches()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactStrCondition, new UserAttributes { { "browser_type", "firefox" } }, Logger), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactBoolCondition, new UserAttributes { { "is_registered_user", false } }, Logger), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactDecimalCondition, new UserAttributes { { "pi_value", 3.14 } }, Logger), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "lasers_count", 9000 } }, Logger), Is.True);
        }

        #endregion // ExactMatcher Tests

        #region ExistsMatcher Tests

        [Test]
        public void TestExistsMatcherReturnsFalseWhenAttributeIsNotProvided()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { }, Logger), Is.False);
        }

        [Test]
        public void TestExistsMatcherReturnsFalseWhenAttributeIsNull()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "input_value", null } }, Logger), Is.False);
        }

        [Test]
        public void TestExistsMatcherReturnsTrueWhenAttributeValueIsProvided()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "input_value", "" } }, Logger), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "input_value", "iPhone" } }, Logger), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "input_value", 10 } }, Logger), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "input_value", 10.5 } }, Logger), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "input_value", false } }, Logger), Is.True);
        }

        #endregion // ExistsMatcher Tests

        #region SubstringMatcher Tests

        [Test]
        public void TestSubstringMatcherReturnsFalseWhenAttributeValueIsNotASubstring()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(SubstrCondition, new UserAttributes { { "location", "Los Angeles" } }, Logger), Is.False);
        }

        [Test]
        public void TestSubstringMatcherReturnsNullWhenAttributeValueIsNotAString()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(SubstrCondition, new UserAttributes { { "attr_value", 10.5 } }, Logger), Is.Null);
        }

        [Test]
        public void TestSubstringMatcherReturnsTrueWhenAttributeValueIsASubstring()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(SubstrCondition, new UserAttributes { { "location", "USA" } }, Logger), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(SubstrCondition, new UserAttributes { { "location", "San Francisco, USA" } }, Logger), Is.True);
        }

        #endregion // SubstringMatcher Tests

        #region GTMatcher Tests

        [Test]
        public void TestGTMatcherReturnsFalseWhenAttributeValueIsLessThanOrEqualToConditionValue()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "distance_gt", 5 } }, Logger), Is.False);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "distance_gt", 10 } }, Logger), Is.False);
        }

        [Test]
        public void TestGTMatcherReturnsNullWhenAttributeValueIsNotANumericValue()
        {
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "distance_gt", "invalid" } }, Logger));
        }

        [Test]
        public void TestGTMatcherReturnsNullWhenAttributeValueIsInfinity()
        {
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "distance_gt", double.PositiveInfinity } }, Logger));
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(InfinityIntCondition, new UserAttributes { { "max_num_value", 15 } }, Logger));
        }

        [Test]
        public void TestGTMatcherReturnsTrueWhenAttributeValueIsGreaterThanConditionValue()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "distance_gt", 15 } }, Logger), Is.True);
        }

        #endregion // GTMatcher Tests

        #region LTMatcher Tests

        [Test]
        public void TestLTMatcherReturnsFalseWhenAttributeValueIsGreaterThanOrEqualToConditionValue()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "distance_lt", 15 } }, Logger), Is.False);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "distance_lt", 10 } }, Logger), Is.False);
        }

        [Test]
        public void TestLTMatcherReturnsNullWhenAttributeValueIsNotANumericValue()
        {
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "distance_lt", "invalid" } }, Logger));
        }

        [Test]
        public void TestLTMatcherReturnsNullWhenAttributeValueIsInfinity()
        {
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "distance_lt", double.NegativeInfinity } }, Logger));
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(InfinityIntCondition, new UserAttributes { { "max_num_value", 5 } }, Logger));
        }

        [Test]
        public void TestLTMatcherReturnsTrueWhenAttributeValueIsLessThanConditionValue()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "distance_lt", 5 } }, Logger), Is.True);
        }

        #endregion // LTMatcher Tests
    }
}
