/* 
 * Copyright 2017, 2019-2020, Optimizely
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

using OptimizelySDK.Event.Entity;
using System;

namespace OptimizelySDK.Event.Builder
{
    [Obsolete("This class is deprecated.")]
    public static class Params
    {
        public const string ACCOUNT_ID = "account_id";
        public const string ANONYMIZE_IP = "anonymize_ip";
        public const string CAMPAIGN_ID = "campaign_id";
        public const string CLIENT_ENGINE = "client_name";
        public const string CLIENT_VERSION = "client_version";
        public const string DECISIONS = "decisions";
        public const string ENRICH_DECISIONS = "enrich_decisions";
        public const string ENTITY_ID = "entity_id";
        public const string EVENTS = "events";
        public const string EXPERIMENT_ID = "experiment_id";
        public const string METADATA = "metadata";
        public const string PROJECT_ID = "project_id";
        public const string REVISION = "revision";
        public const string TIME = "timestamp";
        public const string TIMESTAMP = "timestamp";
        public const string VARIATION_ID = "variation_id";
        public const string VISITOR_ID = "visitorId";
        public const string VISITORS = "visitors";
    }
}
