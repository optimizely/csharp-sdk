/* 
 * Copyright 2019-2020, Optimizely
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
using Newtonsoft.Json;

namespace OptimizelySDK.Event.Entity
{
    public class Decision
    {
        [JsonProperty("campaign_id")]
        public string CampaignId { get; private set; }
        [JsonProperty("experiment_id")]
        public string ExperimentId { get; private set; }
        [JsonProperty("metadata")]
        public DecisionMetadata Metadata { get; private set; }
        [JsonProperty("variation_id")]
        public string VariationId { get; private set; }
        public Decision() {}

        public Decision(string campaignId, string experimentId, string variationId, DecisionMetadata metadata = null)
        {
            CampaignId = campaignId;
            ExperimentId = experimentId;
            Metadata = metadata;
            VariationId = variationId;
        }
    }
}
