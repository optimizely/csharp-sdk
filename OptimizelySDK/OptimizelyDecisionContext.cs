/*
 * Copyright 2021, Optimizely
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
    /// <summary>
    /// OptimizelyDecisionContext contains flag key and rule key to be used for setting 
    /// and getting forced decision.
    /// </summary>
    public class OptimizelyDecisionContext
    {
        public const string OPTI_NULL_RULE_KEY = "$opt-null-rule-key";
        public const string OPTI_KEY_DIVIDER = "-$opt$-";

        private string flagKey;
        private string ruleKey;
        private string decisionKey;

        /// <summary>
        /// Represents the object is valid or not.
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Flag key of the context.
        /// </summary>
        public string FlagKey => flagKey;

        /// <summary>
        /// Rule key, it can be experiment or rollout key and nullable.
        /// </summary>
        public string RuleKey => ruleKey;

        public OptimizelyDecisionContext(string flagKey, string ruleKey = null)
        {
            if (flagKey != null)
            {
                IsValid = true;
            }

            this.flagKey = flagKey;
            this.ruleKey = ruleKey;
        }

        public string GetKey()
        {
            return string.Format("{0}{1}{2}", FlagKey, OPTI_KEY_DIVIDER,
                RuleKey ?? OPTI_NULL_RULE_KEY);
        }
    }
}
