﻿/* 
 * Copyright 2017, Optimizely
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
namespace OptimizelySDK.Event.Builder
{
    public static class Params
    {
        public const string ACCOUNT_ID = "account_id";
        public const string PROJECT_ID = "project_id";
        public const string ENTITY_ID = "entity_id";
        public const string TIMESTAMP = "timestamp";
        public const string VISITORS = "visitors";
        public const string REVISION = "revision";
        public const string EXPERIMENT_ID = "experiment_id";
        public const string VARIATION_ID = "variation_id";
        public const string CAMPAIGN_ID = "campaign_id";
        public const string VISITOR_ID = "visitorId";
        public const string EVENT_ID = "eventEntityId";
        public const string EVENT_NAME = "eventName";
        public const string EVENT_METRICS = "eventMetrics";
        public const string EVENT_FEATURES = "eventFeatures";
        public const string USER_FEATURES = "userFeatures";
        public const string DECISIONS = "decisions";
        public const string EVENTS = "events";
        public const string TIME = "timestamp";
        public const string CLIENT_ENGINE = "client_name";
        public const string CLIENT_VERSION = "client_version";
        public const string IS_LAYER_HOLDBACK = "isLayerHoldback";
        public const string ANONYMIZE_IP = "anonymize_ip";
    }
}
