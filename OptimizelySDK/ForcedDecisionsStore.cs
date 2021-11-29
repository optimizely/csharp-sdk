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

using System.Collections.Generic;

namespace OptimizelySDK
{
    /// <summary>
    /// ForcedDecisionsStore defines helper methods that are used for optimizelyDecisionContext object manipulation.
    /// </summary>
    public class ForcedDecisionsStore
    {
        public const string OPTI_NULL_RULE_KEY = "$opt-null-rule-key";
        public const string OPTI_KEY_DIVIDER = "-$opt$-";

        private Dictionary<string, OptimizelyForcedDecision> ForcedDecisionsMap { get; set; }

        public ForcedDecisionsStore()
        {
            ForcedDecisionsMap = new Dictionary<string, OptimizelyForcedDecision>();
        }

        public int Count
        {
            get
            {
                return ForcedDecisionsMap.Count;
            }
        }
        public bool Remove(OptimizelyDecisionContext context)
        {
            var decisionKey = getKey(context);
            return ForcedDecisionsMap.Remove(decisionKey);
        }

        public void RemoveAll()
        {
            ForcedDecisionsMap.Clear();
        }

        private string getKey(OptimizelyDecisionContext context)
        {
            return string.Format("{0}{1}{2}", context.FlagKey, OPTI_KEY_DIVIDER, context.RuleKey ?? OPTI_NULL_RULE_KEY);
        }

        public OptimizelyForcedDecision this[OptimizelyDecisionContext context]
        {
            get
            {
                if (context != null && context.FlagKey != null
                    && ForcedDecisionsMap.TryGetValue(getKey(context), out OptimizelyForcedDecision flagForcedDecision))
                {
                    return flagForcedDecision;
                }
                return null;
            }
            set
            {
                if (context != null && context.FlagKey != null)
                {
                    ForcedDecisionsMap[getKey(context)] = value;
                }
            }

        }
    }
}
