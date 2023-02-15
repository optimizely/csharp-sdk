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
    public class EventBatch
    {
        [JsonProperty("account_id")]
        public string AccountId { get; private set; }

        [JsonProperty("project_id")]
        public string ProjectId { get; private set; }

        [JsonProperty("revision")]
        public string Revision { get; private set; }

        [JsonProperty("client_name")]
        public string ClientName { get; private set; }

        [JsonProperty("client_version")]
        public string ClientVersion { get; private set; }

        [JsonProperty("anonymize_ip")]
        public bool AnonymizeIP { get; private set; }

        [JsonProperty("enrich_decisions")]
        public bool EnrichDecisions { get; private set; }

        [JsonProperty("visitors")]
        public List<Visitor> Visitors { get; private set; }

        public class Builder
        {
            private string AccountId;
            private string ProjectId;
            private string Revision;
            private string ClientName;
            private string ClientVersion;
            private bool AnonymizeIP;
            private bool EnrichDecisions;
            private List<Visitor> Visitors;

            public EventBatch Build()
            {
                var eventBatch = new EventBatch();
                eventBatch.AccountId = AccountId;
                eventBatch.ProjectId = ProjectId;
                eventBatch.Revision = Revision;
                eventBatch.ClientName = ClientName;
                eventBatch.ClientVersion = ClientVersion;
                eventBatch.AnonymizeIP = AnonymizeIP;
                eventBatch.EnrichDecisions = EnrichDecisions;
                eventBatch.Visitors = Visitors ?? new List<Visitor>();

                return eventBatch;
            }

            public Builder WithAccountId(string accountId)
            {
                AccountId = accountId;

                return this;
            }

            public Builder WithProjectID(string projectId)
            {
                ProjectId = projectId;

                return this;
            }

            public Builder WithRevision(string revision)
            {
                Revision = revision;

                return this;
            }

            public Builder WithClientName(string clientName)
            {
                ClientName = clientName;

                return this;
            }

            public Builder WithClientVersion(string clientVersion)
            {
                ClientVersion = clientVersion;

                return this;
            }

            public Builder WithAnonymizeIP(bool anonymizeIP)
            {
                AnonymizeIP = anonymizeIP;
                return this;
            }

            public Builder WithEnrichDecisions(bool enrichDecisions)
            {
                EnrichDecisions = enrichDecisions;
                return this;
            }

            public Builder WithVisitors(Visitor[] visitors)
            {
                Visitors = new List<Visitor>(visitors);
                return this;
            }
        }
    }
}
