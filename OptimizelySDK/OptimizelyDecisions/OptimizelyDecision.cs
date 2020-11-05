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
    public class OptimizelyDecision
    {
        private string VariationKey { get; set; }
        private bool Enabled { get; set; }
        private OptimizelyJSON Variables { get; set; }
        private string RuleKey { get; set; }
        private string FlagKey { get; set; }
        private OptimizelyUserContext UserContext { get; set; }
        private List<string> Reasons { get; set; }

        public OptimizelyDecision(string variationKey,
                              bool enabled,
                              OptimizelyJSON variables,
                              string ruleKey,
                              string flagKey,
                              OptimizelyUserContext userContext,
                              List<string> reasons)
        {
            VariationKey = variationKey;
            Enabled = enabled;
            Variables = variables;
            RuleKey = ruleKey;
            FlagKey = flagKey;
            UserContext = userContext;
            Reasons = reasons;
        }

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
                new List<string>() { error });
        }
    }
}
