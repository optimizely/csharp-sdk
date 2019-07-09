﻿/* 
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
using Newtonsoft.Json;

namespace OptimizelySDK.Event.Entity
{
    public class EventContext
    {
        [JsonProperty("account_id")]
        public string AccountId {get; protected set;}

        [JsonProperty("project_id")]
        public string ProjectId { get; protected set; }

        [JsonProperty("revision")]
        public string Revision { get; protected set; }

        [JsonProperty("client_name")]
        public string ClientName { get; protected set; }

        [JsonProperty("client_version")]
        public string ClientVersion { get; protected set; }

        [JsonProperty("anonymize_ip")]
        public bool AnonymizeIP { get; protected set; }

        public class Builder
        {
            private string AccountId;
            private string ProjectId;
            private string Revision;
            private string ClientName;
            private string ClientVersion;
            private bool AnonymizeIP;

            public Builder WithAccountId(string accountId)
            {
                AccountId = accountId;
                return this;
            }

            public Builder WithProjectId(string projectId)
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
            
            public EventContext Build()
            {
                var eventContext = new EventContext();

                eventContext.AccountId = AccountId;
                eventContext.ProjectId = ProjectId;
                eventContext.Revision = Revision;
                eventContext.ClientName = ClientName;
                eventContext.ClientVersion = ClientVersion;
                eventContext.AnonymizeIP = AnonymizeIP;

                return eventContext;
            }
        }

    }
}
