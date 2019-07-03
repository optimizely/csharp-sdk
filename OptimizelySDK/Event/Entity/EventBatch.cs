/* 
 * Copyright 2019, Optimizely
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
using Newtonsoft.Json;

namespace OptimizelySDK.Event.Entity
{
    public class EventBatch : EventContext
    {
        [JsonProperty("enrich_decisions")]
        public bool EnrichDecisions { get; private set; }

        [JsonProperty("visitors")]
        public List<Visitor> Visitors { get; private set; }

        public EventBatch(string accountId, string projectId, string revision, string clientName, string clientVersion, bool anonymizeIP, bool enrichDecisions)
        {
            AccountId = accountId;
            ProjectId = projectId;
            Revision = revision;
            ClientName = clientName;
            ClientVersion = clientVersion;
            AnonymizeIP = anonymizeIP;
            EnrichDecisions = enrichDecisions;

            Visitors = new List<Visitor>();
        }
    }
}
