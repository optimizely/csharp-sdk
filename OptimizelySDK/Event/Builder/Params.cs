/* 
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
        public const string ACCOUNT_ID = "accountId";
        public const string PROJECT_ID = "projectId";
        public const string LAYER_ID = "layerId";
        public const string EXPERIMENT_ID = "experimentId";
        public const string VARIATION_ID = "variationId";
        public const string VISITOR_ID = "visitorId";
        public const string EVENT_ID = "eventEntityId";
        public const string EVENT_NAME = "eventName";
        public const string EVENT_METRICS = "eventMetrics";
        public const string EVENT_FEATURES = "eventFeatures";
        public const string USER_FEATURES = "userFeatures";
        public const string DECISION = "decision";
        public const string LAYER_STATES = "layerStates";
        public const string TIME = "timestamp";
        public const string CLIENT_ENGINE = "clientEngine";
        public const string CLIENT_VERSION = "clientVersion";
        public const string ACTION_TRIGGERED = "actionTriggered";
        public const string IS_GLOBAL_HOLDBACK = "isGlobalHoldback";
        public const string IS_LAYER_HOLDBACK = "isLayerHoldback";
    }
}
