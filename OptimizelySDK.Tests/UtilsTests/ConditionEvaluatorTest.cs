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

namespace OptimizelySDK.Tests.UtilsTests
{
    [TestFixture]
    public class ConditionEvaluatorTest
    {
        private ConditionEvaluator ConditionEvaluator = null;
        private object[] Conditions = null;
        private const string ConditionsStr = @"[""and"", [""or"", [""or"", {""name"": ""device_type"", ""type"": ""custom_attribute"", ""value"": ""iPhone""}]], [""or"", [""or"", {""name"": ""location"", ""type"": ""custom_attribute"", ""value"": ""San Francisco""}]], [""or"", [""not"", [""or"", {""name"": ""browser"", ""type"": ""custom_attribute"", ""value"": ""Firefox""}]]]]";

        [TestFixtureSetUp]
        public void Initialize()
        {
            ConditionEvaluator = new ConditionEvaluator();

            Conditions = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(ConditionsStr);
        }

        [TestFixtureTearDown]
        public void TestCleanUp()
        {
            Conditions = null;
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

            Assert.IsTrue(ConditionEvaluator.Evaluate(Conditions, userAttributes));
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

            Assert.IsFalse(ConditionEvaluator.Evaluate(Conditions, userAttributes));
        }

        [Test]
        public void TestEvaluateEmptyUserAttributes()
        {
            var userAttributes = new UserAttributes();

            Assert.IsFalse(ConditionEvaluator.Evaluate(Conditions, userAttributes));
        }

        [Test]
        public void TestEvaluateNullUserAttributes()
        {
            UserAttributes userAttributes = null;

            Assert.IsFalse(ConditionEvaluator.Evaluate(Conditions, userAttributes));
        }

        [Test]
        public void TestTypedUserAttributesEvaluateTrue()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"is_firefox", false },
                {"num_users", 15 },
                {"pi_value", 3.14 }
            };

            string typedConditionStr = @"[""and"", [""or"", [""or"", {""name"": ""device_type"", ""type"": ""custom_attribute"", ""value"": ""iPhone""}]], [""or"", [""or"", {""name"": ""is_firefox"", ""type"": ""custom_attribute"", ""value"": false}]], [""or"", [""or"", {""name"": ""num_users"", ""type"": ""custom_attribute"", ""value"": 15}]], [""or"", [""or"", {""name"": ""pi_value"", ""type"": ""custom_attribute"", ""value"": 3.14}]]]";
            var typedConditions = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(typedConditionStr);
            Assert.IsTrue(ConditionEvaluator.Evaluate(typedConditions, userAttributes));
        }
    }
}