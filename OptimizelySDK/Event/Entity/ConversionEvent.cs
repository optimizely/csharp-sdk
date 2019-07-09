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
using OptimizelySDK.Entity;
using System;

namespace OptimizelySDK.Event.Entity
{
    public class ConversionEvent : UserEvent
    {
        public string UserId { get; private set; }
        public VisitorAttribute[] VisitorAttributes { get; private set; }

        public OptimizelySDK.Entity.Event Event { get; private set; }
        public EventTags EventTags { get; private set; }
        public bool? BotFiltering { get; private set; }

        public class Builder
        {
            private string UserId;
            private VisitorAttribute[] VisitorAttributes;
            private OptimizelySDK.Entity.Event Event;
            private EventTags EventTags;
            private EventContext EventContext;
            private bool? BotFiltering;

            public Builder WithUserId(string userId)
            {
                UserId = userId;

                return this;
            }            

            public Builder WithEventContext(EventContext eventContext)
            {
                EventContext = eventContext;

                return this;
            }

            public Builder WithEvent(OptimizelySDK.Entity.Event @event)
            {
                Event = @event;

                return this;
            }

            public Builder WithVisitorAttributes(VisitorAttribute[] visitorAttributes)
            {
                VisitorAttributes = visitorAttributes;

                return this;
            }

            public Builder WithEventTags(EventTags eventTags)
            {
                EventTags = eventTags;

                return this;
            }

            public Builder WithBotFilteringEnabled(bool? botFiltering)
            {
                BotFiltering = botFiltering;

                return this;
            }

            public ConversionEvent Build()
            {
                var conversionEvent = new ConversionEvent();

                conversionEvent.Context = EventContext;
                conversionEvent.UUID = Guid.NewGuid().ToString();
                conversionEvent.Timestamp = SecondsSince1970 * 1000;

                conversionEvent.EventTags = EventTags;
                conversionEvent.VisitorAttributes = VisitorAttributes;
                conversionEvent.UserId = UserId;
                conversionEvent.Event = Event;
                conversionEvent.BotFiltering = BotFiltering;

                return conversionEvent;
            }
        }

    }
}
