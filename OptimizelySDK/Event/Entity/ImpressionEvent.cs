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
using OptimizelySDK.Entity;
using System;

namespace OptimizelySDK.Event.Entity
{
    public class ImpressionEvent : UserEvent
    {
        public string UserId { get; private set; }
        public VisitorAttribute[] VisitorAttributes { get; private set; }

        public Experiment Experiment { get; set; }
        public Variation Variation { get; set; }
        public bool? BotFiltering { get; set; }

        public class Builder
        {
            private string UserId;            
            private string UUID;
            private long Timestamp;
            private EventContext EventContext;

            public VisitorAttribute[] VisitorAttributes;
            private Experiment Experiment;
            private Variation Variation;
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

            public Builder WithExperiment(Experiment experiment)
            {
                Experiment = experiment;

                return this;
            }

            public Builder WithVisitorAttributes(VisitorAttribute[] visitorAttributes)
            {
                VisitorAttributes = visitorAttributes;

                return this;
            }

            public Builder WithVariation(Variation variation)
            {
                Variation = variation;

                return this;
            }

            public Builder WithBotFilteringEnabled(bool? botFiltering)
            {
                BotFiltering = botFiltering;

                return this;
            }

            public ImpressionEvent Build()
            {
                var impressionEvent = new ImpressionEvent();

                impressionEvent.Context = EventContext;
                impressionEvent.UUID = Guid.NewGuid().ToString();
                impressionEvent.Timestamp = SecondsSince1970 * 1000;

                impressionEvent.Experiment = Experiment;
                impressionEvent.VisitorAttributes = VisitorAttributes;
                impressionEvent.UserId = UserId;
                impressionEvent.Variation = Variation;
                impressionEvent.BotFiltering = BotFiltering;

                return impressionEvent;
            }
        }

    }
}
