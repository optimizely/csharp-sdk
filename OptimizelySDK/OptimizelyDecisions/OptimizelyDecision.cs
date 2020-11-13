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
        // variation key for optimizely decision.
        public string VariationKey { get; private set; }
        // boolean value indicating if the flag is enabled or not.
        public bool Enabled { get; private set; }
        // collection of variables associated with the decision.
        public OptimizelyJSON Variables { get; private set; }
        // rule key of the decision.
        public string RuleKey { get; private set; }
        // flag key for which the decision was made.
        public string FlagKey { get; private set; }
        // user context for which the  decision was made.
        public OptimizelyUserContext UserContext { get; private set; }
        // an array of error/info/debug messages describing why the decision has been made.
        public string[] Reasons { get; private set; }

        public OptimizelyDecision(string variationKey,
                              bool enabled,
                              OptimizelyJSON variables,
                              string ruleKey,
                              string flagKey,
                              OptimizelyUserContext userContext,
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
            OptimizelyUserContext optimizelyUserContext,
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
