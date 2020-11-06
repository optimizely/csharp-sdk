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

using NUnit.Framework;
using OptimizelySDK.OptimizelyDecisions;
using System.Collections.Generic;

namespace OptimizelySDK.Tests.OptimizelyDecisions
{
    [TestFixture]
    public class DecisionReasonsTest
    {
        
        [Test]
        public void TestNewDecisionReasonWith()
        {
            var decisionReasons = DefaultDecisionReasons.NewInstance(new List<OptimizelyDecideOption>() { OptimizelyDecideOption.INCLUDE_REASONS });
            decisionReasons.AddError(DecisionMessage.Reason(DecisionMessage.FLAG_KEY_INVALID, "invalid_key"));
            Assert.AreEqual(decisionReasons.ToReport()[0], "No flag was found for key \"invalid_key\".");
            decisionReasons.AddError(DecisionMessage.Reason(DecisionMessage.VARIABLE_VALUE_INVALID, "invalid_key"));
            Assert.AreEqual(decisionReasons.ToReport()[1], "Variable value for key \"invalid_key\" is invalid or wrong type.");
          // decisionReasons.AddError(DecisionMessage.Reason(DecisionMessage.SDK_NOT_READY, ""));
          // Assert.AreEqual(decisionReasons.ToReport()[2], "Optimizely SDK not configured properly yet.");
        }

    }
}
