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

using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using System.Collections.Generic;

namespace OptimizelySDK.OptimizelyDecisions
{
    /// <summary>
    /// OptimizelyDecision defines the decision returned by decide api.
    /// </summary>
    public class OptimizelyDecision
    {
        /// <summary>
        /// variation key for optimizely decision.
        /// </summary>
        public string VariationKey { get; private set; }
        
        /// <summary>
        /// boolean value indicating if the flag is enabled or not.
        /// </summary>
        public bool Enabled { get; private set; }

        /// <summary>
        /// collection of variables associated with the decision.
        /// </summary>
        public OptimizelyJSON Variables { get; private set; }
        
        /// <summary>
        /// rule key of the decision.
        /// </summary>
        public string RuleKey { get; private set; }
        
        /// <summary>
        /// flag key for which the decision was made.
        /// </summary>
        public string FlagKey { get; private set; }
        
        /// <summary>
        /// user context for which the  decision was made.
        /// </summary>
        public IOptimizelyUserContext UserContext { get; private set; }
        
        /// <summary>
        /// an array of error/info/debug messages describing why the decision has been made.
        /// </summary>
        public string[] Reasons { get; private set; }

        public OptimizelyDecision(string variationKey,
                              bool enabled,
                              OptimizelyJSON variables,
                              string ruleKey,
                              string flagKey,
                              IOptimizelyUserContext userContext,
                              string[] reasons)
        {
            VariationKey = variationKey;
            Enabled = enabled;
            Variables = variables;
            RuleKey = ruleKey;
            FlagKey = flagKey;
            UserContext = userContext;
            Reasons = reasons;
        }

        /// <summary>
        /// Static function to return OptimizelyDecision
        /// when there are errors for example like OptimizelyConfig is not valid, etc.
        /// OptimizelyDecision will have null variation key, false enabled, empty variables, null rule key
        /// and error reason array
        /// </summary>
        public static OptimizelyDecision NewErrorDecision(string key,
            IOptimizelyUserContext optimizelyUserContext,
            string error,
            IErrorHandler errorHandler,
            ILogger logger)
        {
            return new OptimizelyDecision(
                null,
                false,
                new OptimizelyJSON(new Dictionary<string, object>(), errorHandler, logger),
                null,
                key,
                optimizelyUserContext,
                new string[] { error });
        }
    }
}
