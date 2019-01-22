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

using NUnit.Framework;
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.Entity;

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

        #region Evaluate Tests

        [Test]
        public void TestEvaluateWithNoMatchType()
        {
            Assert.That(LegacyCondition.Evaluate(null, new UserAttributes { { "device_type", "iPhone" } }), Is.True);
            Assert.That(LegacyCondition.Evaluate(null, new UserAttributes { { "device_type", "Android" } }), Is.False);
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

            Assert.That(ExactStrCondition.Evaluate(null, userAttributes), Is.True);
            Assert.That(ExactBoolCondition.Evaluate(null, userAttributes), Is.True);
            Assert.That(GTCondition.Evaluate(null, userAttributes), Is.True);
            Assert.That(ExactDecimalCondition.Evaluate(null, userAttributes), Is.True);
        }

        [Test]
        public void TestEvaluateWithInvalidTypeProperty()
        {
            BaseCondition condition = new BaseCondition { Name = "input_value", Value = "Android", Match = "exists", Type = "invalid_type" };
            Assert.That(condition.Evaluate(null, new UserAttributes { { "device_type", "iPhone" } }), Is.Null);

            condition = new BaseCondition { Name = "input_value", Value = "Android", Match = "exists" };
            Assert.That(condition.Evaluate(null, new UserAttributes { { "device_type", "iPhone" } }), Is.Null);
        }

        [Test]
        public void TestEvaluateWithInvalidMatchProperty()
        {
            BaseCondition condition = new BaseCondition { Name = "input_value", Value = "Android", Match = "exists", Type = "invalid_match" };
            Assert.That(condition.Evaluate(null, new UserAttributes { { "device_type", "iPhone" } }), Is.Null);
        }

        [Test]
        public void TestEvaluateReturnsNullWithMismatchedConditionType()
        {
            BaseCondition condition = new BaseCondition { Name = "is_valid", Value = false, Match = "substring", Type = "custom_attribute" };
            Assert.That(condition.Evaluate(null, new UserAttributes { { "is_valid", false } }), Is.Null);
        }

        #endregion // Evaluate Tests

        #region ExactMatcher Tests

        [Test]
        public void TestExactMatcherReturnsFalseWhenAttributeValueDoesNotMatch()
        {
            Assert.That(ExactStrCondition.Evaluate(null, new UserAttributes { { "browser_type", "chrome" } }), Is.False);
            Assert.That(ExactBoolCondition.Evaluate(null, new UserAttributes { { "is_registered_user", true } }), Is.False);
            Assert.That(ExactDecimalCondition.Evaluate(null, new UserAttributes { { "pi_value", 2.5 } }), Is.False);
            Assert.That(ExactIntCondition.Evaluate(null, new UserAttributes { { "lasers_count", 55 } }), Is.False);
        }

        [Test]
        public void TestExactMatcherReturnsNullWhenTypeMismatch()
        {
            Assert.That(ExactStrCondition.Evaluate(null, new UserAttributes { { "browser_type", true } }), Is.Null);
            Assert.That(ExactBoolCondition.Evaluate(null, new UserAttributes { { "is_registered_user", "abcd" } }), Is.Null);
            Assert.That(ExactDecimalCondition.Evaluate(null, new UserAttributes { { "pi_value", false } }), Is.Null);
            Assert.That(ExactIntCondition.Evaluate(null, new UserAttributes { { "lasers_count", "infinity" } }), Is.Null);
        }

        [Test]
        public void TestExactMatcherReturnsNullWithNumericInfinity()
        {
            Assert.That(ExactIntCondition.Evaluate(null, new UserAttributes { { "lasers_count", double.NegativeInfinity } }), Is.Null);
            Assert.That(InfinityIntCondition.Evaluate(null, new UserAttributes { { "max_num_value", 15 } }), Is.Null);
        }

        [Test]
        public void TestExactMatcherReturnsTrueWhenAttributeValueMatches()
        {
            Assert.That(ExactStrCondition.Evaluate(null, new UserAttributes { { "browser_type", "firefox" } }), Is.True);
            Assert.That(ExactBoolCondition.Evaluate(null, new UserAttributes { { "is_registered_user", false } }), Is.True);
            Assert.That(ExactDecimalCondition.Evaluate(null, new UserAttributes { { "pi_value", 3.14 } }), Is.True);
            Assert.That(ExactIntCondition.Evaluate(null, new UserAttributes { { "lasers_count", 9000 } }), Is.True);
        }

        #endregion // ExactMatcher Tests

        #region ExistsMatcher Tests

        [Test]
        public void TestExistsMatcherReturnsFalseWhenAttributeIsNotProvided()
        {
            Assert.That(ExistsCondition.Evaluate(null, new UserAttributes { }), Is.False);
        }

        [Test]
        public void TestExistsMatcherReturnsFalseWhenAttributeIsNull()
        {
            Assert.That(ExistsCondition.Evaluate(null, new UserAttributes { { "input_value", null } }), Is.False);
        }

        [Test]
        public void TestExistsMatcherReturnsTrueWhenAttributeValueIsProvided()
        {
            Assert.That(ExistsCondition.Evaluate(null, new UserAttributes { { "input_value", "" } }), Is.True);
            Assert.That(ExistsCondition.Evaluate(null, new UserAttributes { { "input_value", "iPhone" } }), Is.True);
            Assert.That(ExistsCondition.Evaluate(null, new UserAttributes { { "input_value", 10 } }), Is.True);
            Assert.That(ExistsCondition.Evaluate(null, new UserAttributes { { "input_value", false } }), Is.True);
        }

        #endregion // ExistsMatcher Tests

        #region SubstringMatcher Tests

        [Test]
        public void TestSubstringMatcherReturnsFalseWhenAttributeValueIsNotASubstring()
        {
            Assert.That(SubstrCondition.Evaluate(null, new UserAttributes { { "location", "Los Angeles" } }), Is.False);
        }

        [Test]
        public void TestSubstringMatcherReturnsNullWhenAttributeValueIsNotAString()
        {
            Assert.That(SubstrCondition.Evaluate(null, new UserAttributes { { "attr_value", 10.5 } }), Is.Null);
        }

        [Test]
        public void TestSubstringMatcherReturnsTrueWhenAttributeValueIsASubstring()
        {
            Assert.That(SubstrCondition.Evaluate(null, new UserAttributes { { "location", "USA" } }), Is.True);
            Assert.That(SubstrCondition.Evaluate(null, new UserAttributes { { "location", "San Francisco, USA" } }), Is.True);
        }

        #endregion // SubstringMatcher Tests

        #region GTMatcher Tests

        [Test]
        public void TestGTMatcherReturnsFalseWhenAttributeValueIsLessThanOrEqualToConditionValue()
        {
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", 5 } }), Is.False);
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", 10 } }), Is.False);
        }

        [Test]
        public void TestGTMatcherReturnsNullWhenAttributeValueIsNotANumericValue()
        {
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", "invalid_type" } }), Is.Null);
        }

        [Test]
        public void TestGTMatcherReturnsNullWhenAttributeValueIsInfinity()
        {
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", double.PositiveInfinity } }), Is.Null);
        }

        [Test]
        public void TestGTMatcherReturnsTrueWhenAttributeValueIsGreaterThanConditionValue()
        {
            Assert.That(GTCondition.Evaluate(null, new UserAttributes { { "distance_gt", 15 } }), Is.True);
        }

        #endregion // GTMatcher Tests

        #region LTMatcher Tests

        [Test]
        public void TestLTMatcherReturnsFalseWhenAttributeValueIsGreaterThanOrEqualToConditionValue()
        {
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", 15 } }), Is.False);
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", 10 } }), Is.False);
        }

        [Test]
        public void TestLTMatcherReturnsNullWhenAttributeValueIsNotANumericValue()
        {
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_gt", "invalid_type" } }), Is.Null);
        }

        [Test]
        public void TestLTMatcherReturnsNullWhenAttributeValueIsInfinity()
        {
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_gt", double.NegativeInfinity } }), Is.Null);
        }

        [Test]
        public void TestLTMatcherReturnsTrueWhenAttributeValueIsLessThanConditionValue()
        {
            Assert.That(LTCondition.Evaluate(null, new UserAttributes { { "distance_lt", 5 } }), Is.True);
        }

        #endregion // LTMatcher Tests
    }
}
