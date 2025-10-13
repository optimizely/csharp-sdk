/**
 *
 *    Copyright 2020, Optimizely and contributors
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */

using Newtonsoft.Json;

namespace OptimizelySDK.Event.Entity
{
    /// <summary>
    /// DecisionMetadata captures additional information regarding the decision
    /// </summary>
    public class DecisionMetadata
    {
        [JsonProperty("flag_key")]
        public string FlagKey { get; private set; }

        [JsonProperty("rule_key")]
        public string RuleKey { get; private set; }

        [JsonProperty("rule_type")]
        public string RuleType { get; private set; }

        [JsonProperty("variation_key")]
        public string VariationKey { get; private set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; private set; }

        [JsonProperty("cmab_uuid")]
        public string CmabUuid { get; private set; }

        public DecisionMetadata(string flagKey, string ruleKey, string ruleType,
            string variationKey = "", bool enabled = false, string cmabUuid = null
        )
        {
            FlagKey = flagKey;
            RuleKey = ruleKey;
            RuleType = ruleType;
            VariationKey = variationKey;
            Enabled = enabled;
            CmabUuid = cmabUuid;
        }
    }
}
