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

namespace OptimizelySDK
{
    public class OptimizelyDecisionContext
    {
        public const string OPTI_NULL_RULE_KEY = "$opt-null-rule-key";
        public const string OPTI_KEY_DIVIDER = "-$opt$-";
        private string flagKey;
        private string ruleKey;
        private string decisionKey;

        public OptimizelyDecisionContext(string flagKey, string ruleKey = null)
        {
            this.flagKey = flagKey;
            this.ruleKey = ruleKey;
            this.decisionKey = string.Format("{0}{1}{2}", flagKey, OPTI_KEY_DIVIDER, ruleKey ?? OPTI_NULL_RULE_KEY);
        }

        public string FlagKey { get { return flagKey; } }

        public string RuleKey { get { return ruleKey; } }

        public string DecisionKey { get { return decisionKey; } }
    }
}
