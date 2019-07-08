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
using Newtonsoft.Json;
using OptimizelySDK.Entity;
namespace OptimizelySDK.Event.Entity
{
    public class SnapshotEvent
    {
        [JsonProperty("entity_id")]
        public string EntityId { get; private set; }

        [JsonProperty(PropertyName ="uuid")]
        public string UUID { get; private set; }

        [JsonProperty("key")]
        public string Key { get; private set; }

        [JsonProperty("timestamp")]
        public long TimeStamp { get; private set; }

        // The following properties are for Conversion that's why ignore if null.
        [JsonProperty("revenue", NullValueHandling = NullValueHandling.Ignore)]        
        public int? Revenue { get; private set; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public long? Value { get; private set; }

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public EventTags EventTags { get; private set; }

        public SnapshotEvent(string entityId, string uuid, string key, long timestamp, int? revenue = null, long? value = null, EventTags eventTags = null)
        {
            EntityId = entityId;
            UUID = uuid;
            Key = key;
            TimeStamp = timestamp;
            Revenue = revenue;
            Value = value;
            EventTags = eventTags;
        }

    }
}
