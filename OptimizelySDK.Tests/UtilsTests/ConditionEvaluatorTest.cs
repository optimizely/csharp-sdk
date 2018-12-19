/* 
 * Copyright 2017-2018, Optimizely
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
using OptimizelySDK.Entity;
using OptimizelySDK.Utils;
using System.Collections.Generic;

namespace OptimizelySDK.Tests.UtilsTests
{
    [TestFixture]
    public class ConditionEvaluatorTest
    {
        private ConditionEvaluator ConditionEvaluator = null;
        private object[] Conditions = null;

        private object[] AndConditions = null;
        private object[] OrConditions = null;
        private object[] NotCondition = null;

        private object[] ExistsCondition = null;
        private object[] SubstrCondition = null;
        private object[] GTCondition = null;
        private object[] LTCondition = null;

        private object[] ExactStrCondition = null;
        private object[] ExactBoolCondition = null;
        private object[] ExactDecimalCondition = null;
        private object[] ExactIntCondition = null;
        private object[] ExactNullCondition = null;

        [TestFixtureSetUp]
        public void Initialize()
        {
            string ConditionsStr = @"[""and"", [""or"", [""or"", {""name"": ""device_type"", ""type"": ""custom_attribute"", ""value"": ""iPhone""}]], [""or"", [""or"", {""name"": ""location"", ""type"": ""custom_attribute"", ""value"": ""San Francisco""}]], [""or"", [""not"", [""or"", {""name"": ""browser"", ""type"": ""custom_attribute"", ""value"": ""Firefox""}]]]]";

            string NotConditionStr = @"[""not"", [""or"", [""or"", {""name"": ""device_type"", ""type"": ""custom_attribute"", ""value"": ""iPhone"", ""match"": ""exact""}]]]";
            string AndConditionStr = @"[""and"", 
                                        [""or"", [""or"", {""name"": ""device_type"", ""type"": ""custom_attribute"", ""value"": ""iPhone"", ""match"": ""substring""}]], 
                                        [""or"", [""or"", {""name"": ""num_users"", ""type"": ""custom_attribute"", ""value"": 15, ""match"": ""exact""}]], 
                                        [""or"", [""or"", {""name"": ""decimal_value"", ""type"": ""custom_attribute"", ""value"": 3.14, ""match"": ""gt""}]]
                                       ]";
            string OrConditionStr = @"[""or"", 
                                        [""or"", [""or"", {""name"": ""device_type"", ""type"": ""custom_attribute"", ""value"": ""iPhone"", ""match"": ""substring""}]], 
                                        [""or"", [""or"", {""name"": ""num_users"", ""type"": ""custom_attribute"", ""value"": 15, ""match"": ""exact""}]], 
                                        [""or"", [""or"", {""name"": ""decimal_value"", ""type"": ""custom_attribute"", ""value"": 3.14, ""match"": ""gt""}]]
                                      ]";

            string ExactStrConditionStr = @"[""and"", [""or"", [""or"", {""name"": ""attr_value"", ""type"": ""custom_attribute"", ""value"": ""firefox"", ""match"": ""exact""}]]]";
            string ExactBoolConditionStr = @"[""and"", [""or"", [""or"", {""name"": ""attr_value"", ""type"": ""custom_attribute"", ""value"": false, ""match"": ""exact""}]]]";
            string ExactDecimalConditionStr = @"[""and"", [""or"", [""or"", {""name"": ""attr_value"", ""type"": ""custom_attribute"", ""value"": 1.5, ""match"": ""exact""}]]]";
            string ExactIntConditionStr = @"[""and"", [""or"", [""or"", {""name"": ""attr_value"", ""type"": ""custom_attribute"", ""value"": 10, ""match"": ""exact""}]]]";
            string ExactNullConditionStr = @"[""and"", [""or"", [""or"", {""name"": ""attr_value"", ""type"": ""custom_attribute"", ""value"": null, ""match"": ""exact""}]]]";

            string ExistsConditionStr = @"[""and"", [""or"", [""or"", {""name"": ""attr_value"", ""type"": ""custom_attribute"", ""match"": ""exists""}]]]";

            string SubstrConditionStr = @"[""and"", [""or"", [""or"", {""name"": ""attr_value"", ""type"": ""custom_attribute"", ""value"": ""firefox"", ""match"": ""substring""}]]]";

            string GTConditionStr = @"[""and"", [""or"", [""or"", {""name"": ""attr_value"", ""type"": ""custom_attribute"", ""value"": 10, ""match"": ""gt""}]]]";
            string LTConditionStr = @"[""and"", [""or"", [""or"", {""name"": ""attr_value"", ""type"": ""custom_attribute"", ""value"": 10, ""match"": ""lt""}]]]";
            
            ConditionEvaluator = new ConditionEvaluator();
            Conditions = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(ConditionsStr);

            AndConditions = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(AndConditionStr);
            OrConditions = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(OrConditionStr);
            NotCondition = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(NotConditionStr);

            ExactStrCondition = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(ExactStrConditionStr);
            ExactBoolCondition = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(ExactBoolConditionStr);
            ExactDecimalCondition = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(ExactDecimalConditionStr);
            ExactIntCondition = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(ExactIntConditionStr);
            ExactNullCondition = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(ExactNullConditionStr);

            ExistsCondition = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(ExistsConditionStr);

            SubstrCondition = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(SubstrConditionStr);

            GTCondition = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(GTConditionStr);
            LTCondition = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(LTConditionStr);
        }

        [TestFixtureTearDown]
        public void TestCleanUp()
        {
            Conditions = null;
            AndConditions = null;
            ConditionEvaluator = null;
        }

        [Test]
        public void TestEvaluateConditionsMatch()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"location", "San Francisco" },
                {"browser", "Chrome" }
            };

            Assert.That(ConditionEvaluator.Evaluate(Conditions, userAttributes), Is.True);
        }

        [Test]
        public void TestEvaluateConditionsDoNotMatch()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"location", "San Francisco" },
                {"browser", "Firefox" }
            };

            Assert.That(ConditionEvaluator.Evaluate(Conditions, userAttributes), Is.False);
        }

        [Test]
        public void TestEvaluateEmptyUserAttributes()
        {
            var userAttributes = new UserAttributes();

            Assert.That(ConditionEvaluator.Evaluate(Conditions, userAttributes), Is.Null);
        }

        [Test]
        public void TestEvaluateNullUserAttributes()
        {
            UserAttributes userAttributes = null;

            Assert.That(ConditionEvaluator.Evaluate(Conditions, userAttributes), Is.Null);
        }

        [Test]
        public void TestTypedUserAttributesEvaluateTrue()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"num_users", 15 },
                {"decimal_value", 3.15678 }
            };

            Assert.That(ConditionEvaluator.Evaluate(AndConditions, userAttributes), Is.True);
        }

        #region Invalid input Tests

        [Test]
        public void TestEvaluateReturnsNullWithInvalidConditionType()
        {
            var conditionsStr = @"[""and"", [""or"", [""or"", {""name"": ""device_type"", ""type"": ""invalid"", ""value"": ""iPhone"", ""match"": ""exact""}]]]";
            var conditions = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(conditionsStr);

            Assert.Null(ConditionEvaluator.Evaluate(conditions, new UserAttributes { { "device_type", "iPhone" } }));
        }

        [Test]
        public void TestEvaluateReturnsNullWithInvalidMatchType()
        {
            var conditionsStr = @"[""and"", [""or"", [""or"", {""name"": ""device_type"", ""type"": ""custom_attribute"", ""value"": ""iPhone"", ""match"": ""invalid""}]]]";
            var conditions = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(conditionsStr);

            Assert.Null(ConditionEvaluator.Evaluate(conditions, new UserAttributes { { "device_type", "iPhone" } }));
        }

        [Test]
        public void TestEvaluateReturnsNullWithMismatchMatcherType()
        {
            var conditionsStr = @"[""and"", [""or"", [""or"", {""name"": ""is_firefox"", ""type"": ""custom_attribute"", ""value"": false, ""match"": ""substring""}]]]";
            var conditions = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(conditionsStr);

            Assert.Null(ConditionEvaluator.Evaluate(conditions, new UserAttributes { { "is_firefox", false } }));
        }

        #endregion // Invalid input Tests

        #region AND condition Tests

        [Test]
        public void TestAndEvaluatorReturnsNullWhenAllOperandsReturnNull()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", 15 },
                {"num_users", "test" },
                {"decimal_value", false }
            };

            Assert.Null(ConditionEvaluator.Evaluate(AndConditions, userAttributes));
        }

        [Test]
        public void TestAndEvaluatorReturnsNullWhenOperandsEvaluateToTrueAndNull()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "my iPhone" },
                {"num_users", 15 },
                {"decimal_value", false } // This evaluates to null.
            };

            Assert.Null(ConditionEvaluator.Evaluate(AndConditions, userAttributes));
        }

        [Test]
        public void TestAndEvaluatorReturnsFalseWhenOperandsEvaluateToFalseAndNull()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "Android" }, // Evaluates to false.
                {"num_users", 20 }, // Evaluates to false.
                {"decimal_value", false } // Evaluates to null.
            };

            Assert.That(ConditionEvaluator.Evaluate(AndConditions, userAttributes), Is.False);
        }

        [Test]
        public void TestAndEvaluatorReturnsFalseWhenOperandsEvaluateToFalseTrueAndNull()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "Phone" }, // Evaluates to true.
                {"num_users", 20 }, // Evaluates to false.
                {"decimal_value", false } // Evaluates to null.
            };

            Assert.That(ConditionEvaluator.Evaluate(AndConditions, userAttributes), Is.False);
        }

        [Test]
        public void TestAndEvaluatorReturnsTrueWhenAllOperandsEvaluateToTrue()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone X" },
                {"num_users", 15 },
                {"decimal_value", 3.1567 }
            };

            Assert.That(ConditionEvaluator.Evaluate(AndConditions, userAttributes), Is.True);
        }

        #endregion // AND condition Tests

        #region OR condition Tests

        [Test]
        public void TestOrEvaluatorReturnsNullWhenAllOperandsReturnNull()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", 15 },
                {"num_users", "test" },
                {"decimal_value", false }
            };

            Assert.Null(ConditionEvaluator.Evaluate(OrConditions, userAttributes));
        }

        [Test]
        public void TestOrEvaluatorReturnsTrueWhenOperandsEvaluateToTruesAndNulls()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "hone" }, // Evaluates to true.
                {"num_users", 15 },
                {"decimal_value", false } // Evaluates to null.
            };

            Assert.That(ConditionEvaluator.Evaluate(OrConditions, userAttributes), Is.True);
        }

        [Test]
        public void TestOrEvaluatorReturnsNullWhenOperandsEvaluateToFalsesAndNulls()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "Android" }, // Evaluates to false.
                {"num_users", 20 }, // Evaluates to false.
                {"decimal_value", false } // Evaluates to null.
            };

            Assert.Null(ConditionEvaluator.Evaluate(OrConditions, userAttributes));
        }

        [Test]
        public void TestOrEvaluatorReturnsTrueWhenOperandsEvaluateToFalsesTruesAndNulls()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone file explorer" }, // Evaluates to true.
                {"num_users", 20 }, // Evaluates to false.
                {"decimal_value", false } // Evaluates to null.
            };

            Assert.That(ConditionEvaluator.Evaluate(OrConditions, userAttributes), Is.True);
        }

        [Test]
        public void TestOrEvaluatorReturnsFalseWhenAllOperandsEvaluateToFalse()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "Android" },
                {"num_users", 17 },
                {"decimal_value", 3.12 }
            };

            Assert.That(ConditionEvaluator.Evaluate(OrConditions, userAttributes), Is.False);
        }

        #endregion // OR condition Tests

        #region NOT condition Tests

        [Test]
        public void TestNotEvaluatorReturnsNullWhenOperandEvaluateToNull()
        {
            Assert.Null(ConditionEvaluator.Evaluate(NotCondition, new UserAttributes { { "device_type", 123 } }));
        }

        [Test]
        public void TestNotEvaluatorReturnsTrueWhenOperandEvaluateToFalse()
        {
            Assert.That(ConditionEvaluator.Evaluate(NotCondition, new UserAttributes { { "device_type", "Android" } }), Is.True);
        }

        [Test]
        public void TestNotEvaluatorReturnsFalseWhenOperandEvaluateToTrue()
        {
            Assert.That(ConditionEvaluator.Evaluate(NotCondition, new UserAttributes { { "device_type", "iPhone" } }), Is.False);
        }

        #endregion // NOT condition Tests

        #region ExactMatcher Tests

        [Test]
        public void TestExactMatcherReturnsFalseWhenAttributeValueDoesNotMatch()
        {
            Assert.That(ConditionEvaluator.Evaluate(ExactStrCondition, new UserAttributes { { "attr_value", "chrome" } }), Is.False);
            Assert.That(ConditionEvaluator.Evaluate(ExactBoolCondition, new UserAttributes { { "attr_value", true } }), Is.False);
            Assert.That(ConditionEvaluator.Evaluate(ExactDecimalCondition, new UserAttributes { { "attr_value", 2.5 } }), Is.False);
            Assert.That(ConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "attr_value", 55 } }), Is.False);
        }

        [Test]
        public void TestExactMatcherReturnsNullWhenTypeMismatch()
        {
            Assert.Null(ConditionEvaluator.Evaluate(ExactStrCondition, new UserAttributes { { "attr_value", true } }));
            Assert.Null(ConditionEvaluator.Evaluate(ExactBoolCondition, new UserAttributes { { "attr_value", "abcd" } }));
            Assert.Null(ConditionEvaluator.Evaluate(ExactDecimalCondition, new UserAttributes { { "attr_value", false } }));
            Assert.Null(ConditionEvaluator.Evaluate(ExactBoolCondition, new UserAttributes { { "attr_value", 0 } }));
        }

        [Test]
        public void TestExactMatcherReturnsNullForInvalidNumericValue()
        {
            var invalidPositiveValue = System.Math.Pow(2, 53) + 2;
            var invalidNegativeValue = (System.Math.Pow(2, 53) * -1) - 2;
            
            Assert.Null(ConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "attr_value", double.NaN } }));
            Assert.Null(ConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "attr_value", double.NegativeInfinity } }));
            Assert.Null(ConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "attr_value", double.PositiveInfinity } }));
            Assert.Null(ConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "attr_value", invalidPositiveValue } }));
            Assert.Null(ConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "attr_value", invalidNegativeValue } }));
            Assert.Null(ConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "attr_value", (ulong)invalidPositiveValue } }));
            Assert.Null(ConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "attr_value", (long)invalidNegativeValue } }));
        }

        [Test]
        public void TestExactMatcherReturnsTrueWhenAttributeValueMatches()
        {
            Assert.That(ConditionEvaluator.Evaluate(ExactStrCondition, new UserAttributes { { "attr_value", "firefox" } }), Is.True);
            Assert.That(ConditionEvaluator.Evaluate(ExactBoolCondition, new UserAttributes { { "attr_value", false } }), Is.True);
            Assert.That(ConditionEvaluator.Evaluate(ExactDecimalCondition, new UserAttributes { { "attr_value", 1.5 } }), Is.True);
            Assert.That(ConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "attr_value", 10 } }), Is.True);
            Assert.That(ConditionEvaluator.Evaluate(ExactIntCondition, new UserAttributes { { "attr_value", 10.0 } }), Is.True);
            Assert.That(ConditionEvaluator.Evaluate(ExactNullCondition, new UserAttributes { { "attr_value", null } }), Is.True);
        }

        #endregion // ExactMatcher Tests

        #region ExistsMatcher Tests

        [Test]
        public void TestExistsMatcherReturnsFalseWhenAttributeIsNotProvided()
        {
            Assert.That(ConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { }), Is.False);
        }

        [Test]
        public void TestExistsMatcherReturnsFalseWhenAttributeIsNull()
        {
            Assert.That(ConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "attr_value", null } }), Is.False);
        }

        [Test]
        public void TestExistsMatcherReturnsTrueWhenAttributeValueIsProvided()
        {
            Assert.That(ConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "attr_value", "" } }), Is.True);
            Assert.That(ConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "attr_value", "iPhone" } }), Is.True);
            Assert.That(ConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "attr_value", 10 } }), Is.True);
            Assert.That(ConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "attr_value", 10.5 } }), Is.True);
            Assert.That(ConditionEvaluator.Evaluate(ExistsCondition, new UserAttributes { { "attr_value", false } }), Is.True);
        }

        #endregion // ExistsMatcher Tests

        #region SubstringMatcher Tests

        [Test]
        public void TestSubstringMatcherReturnsFalseWhenAttributeValueIsNotASubstring()
        {
            Assert.That(ConditionEvaluator.Evaluate(SubstrCondition, new UserAttributes { { "attr_value", "chrome" } }), Is.False);
        }

        [Test]
        public void TestSubstringMatcherReturnsTrueWhenAttributeValueIsASubstring()
        {
            Assert.That(ConditionEvaluator.Evaluate(SubstrCondition, new UserAttributes { { "attr_value", "firefox" } }), Is.True);
            Assert.That(ConditionEvaluator.Evaluate(SubstrCondition, new UserAttributes { { "attr_value", "chrome vs firefox" } }), Is.True);
        }

        [Test]
        public void TestSubstringMatcherReturnsNullWhenAttributeValueIsNotAString()
        {
            Assert.Null(ConditionEvaluator.Evaluate(SubstrCondition, new UserAttributes { { "attr_value", 10.5} }));
        }

        #endregion // SubstringMatcher Tests

        #region GTMatcher Tests

        [Test]
        public void TestGTMatcherReturnsFalseWhenAttributeValueIsLessThanOrEqualToConditionValue()
        {
            Assert.That(ConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "attr_value", 5 } }), Is.False);
            Assert.That(ConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "attr_value", 10 } }), Is.False);
        }

        [Test]
        public void TestGTMatcherReturnsNullWhenForInvalidNumericValue()
        {
            var invalidPositiveValue = System.Math.Pow(2, 53) + 2;
            var invalidNegativeValue = (System.Math.Pow(2, 53) * -1) - 2;
            Assert.Null(ConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "attr_value", double.NaN } }));
            Assert.Null(ConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "attr_value", double.NegativeInfinity } }));
            Assert.Null(ConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "attr_value", double.PositiveInfinity } }));
            Assert.Null(ConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "attr_value", invalidPositiveValue } }));
            Assert.Null(ConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "attr_value", invalidNegativeValue } }));
            Assert.Null(ConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "attr_value", "invalid" } }));
            Assert.Null(ConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "attr_value", (ulong)invalidPositiveValue } }));
            Assert.Null(ConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "attr_value", (long)invalidNegativeValue } }));
        }

        [Test]
        public void TestGTMatcherReturnsTrueWhenAttributeValueIsGreaterThanConditionValue()
        {
            Assert.That(ConditionEvaluator.Evaluate(GTCondition, new UserAttributes { { "attr_value", 15 } }), Is.True);
        }

        #endregion // GTMatcher Tests

        #region LTMatcher Tests

        [Test]
        public void TestLTMatcherReturnsFalseWhenAttributeValueIsGreaterThanOrEqualToConditionValue()
        {
            Assert.That(ConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "attr_value", 15 } }), Is.False);
            Assert.That(ConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "attr_value", 10 } }), Is.False);
        }

        [Test]
        public void TestLTMatcherReturnsNullForInvalidNumericValue()
        {
            var invalidPositiveValue = System.Math.Pow(2, 53) + 2;
            var invalidNegativeValue = (System.Math.Pow(2, 53) * -1) - 2;
            Assert.Null(ConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "attr_value", double.NaN } }));
            Assert.Null(ConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "attr_value", double.NegativeInfinity } }));
            Assert.Null(ConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "attr_value", double.PositiveInfinity } }));
            Assert.Null(ConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "attr_value", invalidPositiveValue } }));
            Assert.Null(ConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "attr_value", invalidNegativeValue } }));
            Assert.Null(ConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "attr_value", "invalid" } }));
            Assert.Null(ConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "attr_value", (ulong)invalidPositiveValue } }));
            Assert.Null(ConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "attr_value", (long)invalidNegativeValue } }));
        }

        [Test]
        public void TestLTMatcherReturnsTrueWhenAttributeValueIsLessThanConditionValue()
        {
            Assert.That(ConditionEvaluator.Evaluate(LTCondition, new UserAttributes { { "attr_value", 5 } }), Is.True);
        }

        #endregion // LTMatcher Tests
    }
}