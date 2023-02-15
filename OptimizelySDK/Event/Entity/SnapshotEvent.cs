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
    /// <summary>
    /// Class represents snapshot event
    /// </summary>
    public class SnapshotEvent
    {
        [JsonProperty("entity_id")]
        public string EntityId { get; private set; }

        [JsonProperty(PropertyName = "uuid")]
        public string UUID { get; private set; }

        [JsonProperty("key")]
        public string Key { get; private set; }

        [JsonProperty("timestamp")]
        public long TimeStamp { get; private set; }

        // The following properties are for Conversion that's why ignore if null.
        [JsonProperty("revenue", NullValueHandling = NullValueHandling.Ignore)]
        public int? Revenue { get; private set; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public float? Value { get; private set; }

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public EventTags EventTags { get; private set; }

        public class Builder
        {
            private string EntityId;
            private string UUID;
            private string Key;
            private long TimeStamp;
            private int? Revenue;
            private float? Value;
            private EventTags EventTags;

            public SnapshotEvent Build()
            {
                var snapshotEvent = new SnapshotEvent();
                snapshotEvent.EntityId = EntityId;
                snapshotEvent.UUID = UUID;
                snapshotEvent.Key = Key;
                snapshotEvent.TimeStamp = TimeStamp;
                snapshotEvent.Revenue = Revenue;
                snapshotEvent.Value = Value;
                snapshotEvent.EventTags = EventTags;

                return snapshotEvent;
            }

            public Builder WithEntityId(string entityId)
            {
                EntityId = entityId;

                return this;
            }

            public Builder WithUUID(string uuid)
            {
                UUID = uuid;

                return this;
            }

            public Builder WithKey(string key)
            {
                Key = key;

                return this;
            }

            public Builder WithTimeStamp(long timeStamp)
            {
                TimeStamp = timeStamp;

                return this;
            }

            public Builder WithRevenue(int? revenue)
            {
                Revenue = revenue;

                return this;
            }

            public Builder WithValue(float? value)
            {
                Value = value;
                return this;
            }

            public Builder WithEventTags(EventTags eventTags)
            {
                EventTags = eventTags;
                return this;
            }
        }
    }
}
