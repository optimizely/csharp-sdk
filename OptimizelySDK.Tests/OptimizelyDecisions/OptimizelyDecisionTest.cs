/* 
 * Copyright 2020-2021, Optimizely
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
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.OptimizelyDecisions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimizelySDK.Tests.OptimizelyDecisions
{
    [TestFixture]
    public class OptimizelyDecisionTest
    {
        private Mock<ILogger> LoggerMock;
        private Mock<IErrorHandler> ErrorHandlerMock;

        [SetUp]
        public void Initialize()
        {
            ErrorHandlerMock = new Mock<IErrorHandler>();
            ErrorHandlerMock.Setup(e => e.HandleError(It.IsAny<Exception>()));

            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
        }
        
        [Test]
        public void TestNewErrorDecision()
        {
            var optimizelyDecision = OptimizelyDecision.NewErrorDecision("var_key", null, "some error message", ErrorHandlerMock.Object, LoggerMock.Object);
            Assert.IsNull(optimizelyDecision.VariationKey);
            Assert.AreEqual(optimizelyDecision.FlagKey, "var_key");
            Assert.AreEqual(optimizelyDecision.Variables.ToDictionary(), new Dictionary<string, object>());
            Assert.AreEqual(optimizelyDecision.Reasons, new List<string>() { "some error message" });
            Assert.IsNull(optimizelyDecision.RuleKey);
            Assert.False(optimizelyDecision.Enabled);
        }

        [Test]
        public void TestNewDecision()
        {
            var variableMap = new Dictionary<string, object>() {
                { "strField", "john doe" },
                { "intField", 12 },
                { "objectField", new Dictionary<string, object> () {
                        { "inner_field_int", 3 }
                    }
                }
            };
            var optimizelyJSONUsingMap = new OptimizelyJSON(variableMap, ErrorHandlerMock.Object, LoggerMock.Object);
            string expectedStringObj = "{\"strField\":\"john doe\",\"intField\":12,\"objectField\":{\"inner_field_int\":3}}";

            var optimizelyDecision = new OptimizelyDecision("var_key",
                true,
                optimizelyJSONUsingMap,
                "experiment",
                "feature_key",
                null,
                new string[0]);
            Assert.AreEqual(optimizelyDecision.VariationKey, "var_key");
            Assert.AreEqual(optimizelyDecision.FlagKey, "feature_key");
            Assert.AreEqual(optimizelyDecision.Variables.ToString(), expectedStringObj);
            Assert.AreEqual(optimizelyDecision.Reasons, new List<string>());
            Assert.AreEqual(optimizelyDecision.RuleKey, "experiment");
            Assert.True(optimizelyDecision.Enabled);
        }

        [Test]
        public void TestNewDecisionReasonWithIncludeReasons()
        {
            var decisionReasons = new DecisionReasons();
            var decideOptions = new OptimizelyDecideOption[] { OptimizelyDecideOption.INCLUDE_REASONS };
            decisionReasons.AddError(DecisionMessage.Reason(DecisionMessage.FLAG_KEY_INVALID, "invalid_key"));
            
            Assert.AreEqual(decisionReasons.ToReport(decideOptions.Contains(OptimizelyDecideOption.INCLUDE_REASONS))[0], "No flag was found for key \"invalid_key\".");
            decisionReasons.AddError(DecisionMessage.Reason(DecisionMessage.VARIABLE_VALUE_INVALID, "invalid_key"));
            Assert.AreEqual(decisionReasons.ToReport(decideOptions.Contains(OptimizelyDecideOption.INCLUDE_REASONS))[1], "Variable value for key \"invalid_key\" is invalid or wrong type.");
            decisionReasons.AddInfo("Some info message.");
            Assert.AreEqual(decisionReasons.ToReport(decideOptions.Contains(OptimizelyDecideOption.INCLUDE_REASONS))[2], "Some info message.");
        }

        [Test]
        public void TestNewDecisionReasonWithoutIncludeReasons()
        {
            var decisionReasons = new DecisionReasons();
            decisionReasons.AddError(DecisionMessage.Reason(DecisionMessage.FLAG_KEY_INVALID, "invalid_key"));

            Assert.AreEqual(decisionReasons.ToReport()[0], "No flag was found for key \"invalid_key\".");
            decisionReasons.AddError(DecisionMessage.Reason(DecisionMessage.VARIABLE_VALUE_INVALID, "invalid_key"));
            Assert.AreEqual(decisionReasons.ToReport()[1], "Variable value for key \"invalid_key\" is invalid or wrong type.");
            decisionReasons.AddInfo("Some info message.");
            Assert.AreEqual(decisionReasons.ToReport().Count, 2);
        }
    }
}
