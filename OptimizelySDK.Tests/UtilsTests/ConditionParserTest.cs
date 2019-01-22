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
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests.UtilsTests
{
    [TestFixture]
    public class ConditionParserTest
    {
        JToken Conditions;
        JToken BaseCondition;
        JToken AudienceConditions;
        JToken NoOpAudienceConditions;

        [TestFixtureSetUp]
        public void Initialize()
        {
            string conditionStr = @"[""and"", [""or"", [""or"", {""name"": ""device_type"", ""type"": ""custom_attribute"", ""value"": ""iPhone"", ""match"": ""substring""}]]]";
            string baseConditionStr = @"{""name"": ""browser_type"", ""type"": ""custom_attribute"", ""value"": ""Chrome"", ""match"": ""exact""}";
            string audienceConditionStr = @"[""or"", [""or"", ""3468206642"", ""3988293898""], [""or"", ""3988293899"", ""3468206646"", ""3468206647""]]";
            string noOpAudienceConditionStr = @"[""3468206642"", ""3988293898""]";

            Conditions = JToken.Parse(conditionStr);
            BaseCondition = JToken.Parse(baseConditionStr);
            AudienceConditions = JToken.Parse(audienceConditionStr);
            NoOpAudienceConditions = JToken.Parse(noOpAudienceConditionStr);
        }

        [Test]
        public void TestParseCondtionsParsesConditionsArray()
        {
            var condition = ConditionParser.ParseConditions(Conditions);
            Assert.NotNull(condition);
            Assert.IsInstanceOf(typeof(AndCondition), condition);
        }

        [Test]
        public void TestParseCondtionsParsesBaseCondition()
        {
            var condition = ConditionParser.ParseConditions(BaseCondition);
            Assert.NotNull(condition);
            Assert.IsInstanceOf(typeof(BaseCondition), condition);
        }

        [Test]
        public void TestParseAudienceConditionsParsesAudienceConditionsArray()
        {
            var condition = ConditionParser.ParseAudienceConditions(AudienceConditions);
            Assert.NotNull(condition);
            Assert.IsInstanceOf(typeof(OrCondition), condition);
        }

        [Test]
        public void TestParseAudienceConditionsParsesAudienceConditionsWithNoOperator()
        {
            var condition = ConditionParser.ParseAudienceConditions(NoOpAudienceConditions);
            Assert.NotNull(condition);
            Assert.IsInstanceOf(typeof(OrCondition), condition);
        }
    }
}
