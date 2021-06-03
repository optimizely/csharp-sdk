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

using Newtonsoft.Json.Linq;
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.Utils;
using Xunit;

namespace OptimizelySDK.XUnitTests.UtilsTests
{
    public class ConditionParserTest
    {
        JToken Conditions;
        JToken BaseCondition;
        JToken AudienceConditions;
        JToken NoOpAudienceConditions;

        public ConditionParserTest()
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

        [Fact]
        public void TestParseCondtionsParsesConditionsArray()
        {
            var condition = ConditionParser.ParseConditions(Conditions);
            Assert.NotNull(condition);
            Assert.IsType<AndCondition>(condition);
        }

        [Fact]
        public void TestParseCondtionsParsesBaseCondition()
        {
            var condition = ConditionParser.ParseConditions(BaseCondition);
            Assert.NotNull(condition);
            Assert.IsType<BaseCondition>(condition);
        }

        [Fact]
        public void TestParseAudienceConditionsParsesAudienceConditionsArray()
        {
            var condition = ConditionParser.ParseAudienceConditions(AudienceConditions);
            Assert.NotNull(condition);
            Assert.IsType<OrCondition>(condition);
        }

        [Fact]
        public void TestParseAudienceConditionsParsesAudienceConditionsWithNoOperator()
        {
            var condition = ConditionParser.ParseAudienceConditions(NoOpAudienceConditions);
            Assert.NotNull(condition);

            // OR operator is assumed by default when no operator has been provided.
            Assert.IsType<OrCondition>(condition);
        }

        [Fact]
        public void TestParseConditionsAssignsNullConditionIfNoConditionIsProvidedInNotOperator()
        {
            JToken emptyNotCondition = JToken.Parse(@"[""not""]");
            var condition = ConditionParser.ParseConditions(emptyNotCondition);
            Assert.NotNull(condition);
            Assert.IsType<NotCondition>(condition);
            var notCondition = (NotCondition)condition;
            Assert.Null(notCondition.Condition);
        }
    }
}
