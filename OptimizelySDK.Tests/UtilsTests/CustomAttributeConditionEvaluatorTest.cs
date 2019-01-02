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

using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OptimizelySDK.Entity;
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
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(LegacyCondition, new UserAttributes { { "device_type", "iPhone" } }), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(LegacyCondition, new UserAttributes { { "device_type", "Android" } }), Is.False);
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

            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactStrCondition, userAttributes), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactBoolCondition, userAttributes), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(GTCondition, userAttributes), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactDecimalCondition, userAttributes), Is.True);
        }

        [Test]
        public void TestEvaluateWithInvalidTypeProperty()
        {
            var invalidTypeCondition = JToken.Parse(@"{""name"": ""input_value"", ""match"": ""exists""}");
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(invalidTypeCondition, new UserAttributes { { "input_value", "test" } }), Is.Null);

            invalidTypeCondition = JToken.Parse(@"{""name"": ""input_value"", ""type"": ""invalid"", ""match"": ""exists""}");
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(invalidTypeCondition, new UserAttributes { { "input_value", "test" } }), Is.Null);
        }

        [Test]
        public void TestEvaluateWithInvalidMatchProperty()
        {
            var invalidMatchCondition = JToken.Parse(@"{""name"": ""distance_gt"", ""type"": ""custom_attribute"", ""value"": 10, ""match"": ""invalid""}");
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(invalidMatchCondition, new UserAttributes { { "distance", 15 } }), Is.Null);
        }

        [Test]
        public void TestEvaluateReturnsNullWithMismatchedConditionType()
        {
            var mismatchedTypeCondition = JToken.Parse(@"{""name"": ""is_firefox"", ""type"": ""custom_attribute"", ""value"": false, ""match"": ""substring""}");
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(mismatchedTypeCondition, new UserAttributes { { "is_firefox", false } }));
        }

        #endregion // Evaluate Tests

        #region ExactMatcher Tests

        [Test]
        public void TestExactMatcherReturnsFalseWhenAttributeValueDoesNotMatch()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactStrCondition, new UserAttributes { { "browser_type", "chrome" } }), Is.False);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactBoolCondition, new UserAttributes { { "is_registered_user", true } }), Is.False);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactDecimalCondition, new UserAttributes { { "pi_value", 2.5 } }), Is.False);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "lasers_count", 55 } }), Is.False);
        }

        [Test]
        public void TestExactMatcherReturnsNullWhenTypeMismatch()
        {
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(ExactStrCondition, new UserAttributes { { "browser_type", true } }));
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(ExactBoolCondition, new UserAttributes { { "is_registered_user", "abcd" } }));
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(ExactDecimalCondition, new UserAttributes { { "pi_value", false } }));
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "lasers_count", "infinity" } }));
        }

        [Test]
        public void TestExactMatcherReturnsNullWithNumericInfinity()
        {
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "lasers_count", double.NegativeInfinity } })); // Infinity value
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(InfinityIntCondition, new UserAttributes { { "max_num_value", 15 } })); // Infinity condition
        }

        [Test]
        public void TestExactMatcherReturnsTrueWhenAttributeValueMatches()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactStrCondition, new UserAttributes { { "browser_type", "firefox" } }), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactBoolCondition, new UserAttributes { { "is_registered_user", false } }), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactDecimalCondition, new UserAttributes { { "pi_value", 3.14 } }), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "lasers_count", 9000 } }), Is.True);
        }

        #endregion // ExactMatcher Tests

        #region ExistsMatcher Tests

        [Test]
        public void TestExistsMatcherReturnsFalseWhenAttributeIsNotProvided()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { }), Is.False);
        }

        [Test]
        public void TestExistsMatcherReturnsFalseWhenAttributeIsNull()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "input_value", null } }), Is.False);
        }

        [Test]
        public void TestExistsMatcherReturnsTrueWhenAttributeValueIsProvided()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "input_value", "" } }), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "input_value", "iPhone" } }), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "input_value", 10 } }), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "input_value", 10.5 } }), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "input_value", false } }), Is.True);
        }

        #endregion // ExistsMatcher Tests

        #region SubstringMatcher Tests

        [Test]
        public void TestSubstringMatcherReturnsFalseWhenAttributeValueIsNotASubstring()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(SubstrCondition, new UserAttributes { { "location", "Los Angeles" } }), Is.False);
        }

        [Test]
        public void TestSubstringMatcherReturnsNullWhenAttributeValueIsNotAString()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(SubstrCondition, new UserAttributes { { "attr_value", 10.5 } }), Is.Null);
        }

        [Test]
        public void TestSubstringMatcherReturnsTrueWhenAttributeValueIsASubstring()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(SubstrCondition, new UserAttributes { { "location", "USA" } }), Is.True);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(SubstrCondition, new UserAttributes { { "location", "San Francisco, USA" } }), Is.True);
        }

        #endregion // SubstringMatcher Tests

        #region GTMatcher Tests

        [Test]
        public void TestGTMatcherReturnsFalseWhenAttributeValueIsLessThanOrEqualToConditionValue()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "distance_gt", 5 } }), Is.False);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "distance_gt", 10 } }), Is.False);
        }

        [Test]
        public void TestGTMatcherReturnsNullWhenAttributeValueIsNotANumericValue()
        {
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "distance_gt", "invalid" } }));
        }

        [Test]
        public void TestGTMatcherReturnsNullWhenAttributeValueIsInfinity()
        {
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "distance_gt", double.PositiveInfinity } }));
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(InfinityIntCondition, new UserAttributes { { "max_num_value", 15 } }));
        }

        [Test]
        public void TestGTMatcherReturnsTrueWhenAttributeValueIsGreaterThanConditionValue()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "distance_gt", 15 } }), Is.True);
        }

        #endregion // GTMatcher Tests

        #region LTMatcher Tests

        [Test]
        public void TestLTMatcherReturnsFalseWhenAttributeValueIsGreaterThanOrEqualToConditionValue()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "distance_lt", 15 } }), Is.False);
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "distance_lt", 10 } }), Is.False);
        }

        [Test]
        public void TestLTMatcherReturnsNullWhenAttributeValueIsNotANumericValue()
        {
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "distance_lt", "invalid" } }));
        }

        [Test]
        public void TestLTMatcherReturnsNullWhenAttributeValueIsInfinity()
        {
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "distance_lt", double.NegativeInfinity } }));
            Assert.Null(CustomAttributeConditionEvaluator.Evaluate(InfinityIntCondition, new UserAttributes { { "max_num_value", 5 } }));
        }

        [Test]
        public void TestLTMatcherReturnsTrueWhenAttributeValueIsLessThanConditionValue()
        {
            Assert.That(CustomAttributeConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "distance_lt", 5 } }), Is.True);
        }

        #endregion // LTMatcher Tests
    }
}
