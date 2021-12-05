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
        private Dictionary<string, OptimizelyForcedDecision> ForcedDecisionsMap { get; set; }
        private static ForcedDecisionsStore NullForcedDecisionStore;

        /// <summary>
        /// Instantiates a NULL object when ForcedDecisionStore first time is used.
        /// </summary>
        static ForcedDecisionsStore()
        {
            NullForcedDecisionStore = new ForcedDecisionsStore();
        }
        public ForcedDecisionsStore()
        {
            ForcedDecisionsMap = new Dictionary<string, OptimizelyForcedDecision>();
        }

        /// <summary>
        /// This method will return instance of ForcedDecisionStore that won't be accessible from outside.
        /// Instead of copying everytime or putting NULL for every forced decision condition, this approach looks fine to me.
        /// </summary>
        /// <returns></returns>
        internal static ForcedDecisionsStore NullForcedDecision()
        {
            return NullForcedDecisionStore;
        }

        public ForcedDecisionsStore(ForcedDecisionsStore forcedDecisionsStore)
        {
            ForcedDecisionsMap = new Dictionary<string, OptimizelyForcedDecision>(forcedDecisionsStore.ForcedDecisionsMap);
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
            return ForcedDecisionsMap.Remove(context.GetKey());
        }

        public void RemoveAll()
        {
            ForcedDecisionsMap.Clear();
        }

        public OptimizelyForcedDecision this[OptimizelyDecisionContext context]
        {
            get
            {
                if (context != null && context.IsValid
                    && ForcedDecisionsMap.TryGetValue(context.GetKey(), out OptimizelyForcedDecision flagForcedDecision))
                {
                    return flagForcedDecision;
                }
                return null;
            }
            set
            {
                if (context != null && context.FlagKey != null)
                {
                    ForcedDecisionsMap[context.GetKey()] = value;
                }
            }

        }
    }
}
