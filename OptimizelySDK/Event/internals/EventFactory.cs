/* 
 * Copyright 2017-2019, Optimizely
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
using OptimizelySDK.Event.Builder;
using OptimizelySDK.Event.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimizelySDK.Event.internals
{
    public class EventFactory
    {
        private static ILogger Logger;
        public const string EVENT_ENDPOINT = "https://logx.optimizely.com/v1/events";  // Should be part of the datafile
        private const string ACTIVATE_EVENT_KEY = "campaign_activated";

        public static LogEvent CreateLogEvent(UserEvent userEvent, ILogger logger) {
            Logger = logger;
            List<UserEvent> userEventList = new List<UserEvent>();
            userEventList.Add(userEvent);
            return CreateLogEvent(userEventList);
        }

        public static LogEvent CreateLogEvent(List<UserEvent> userEvents) {
            EventBatch.Builder builder = new EventBatch.Builder();
            List<Visitor> visitors = new List<Visitor>(userEvents.Count());

            foreach (UserEvent userEvent in userEvents) {

                if (userEvent == null) {
                    continue;
                }

                if (userEvent is ImpressionEvent) {
                    visitors.Add(CreateVisitor((ImpressionEvent) userEvent));
                }

                if (userEvent is ConversionEvent) {
                    visitors.Add(CreateVisitor((ConversionEvent) userEvent));
                }

                // This needs an interface.
                var userContext = userEvent.Context;

                builder.WithClientName(Params.CLIENT_ENGINE)
                    .WithClientVersion(Optimizely.SDK_VERSION)
                    .WithAccountId(userContext.AccountId)
                    .WithAnonymizeIP(userContext.AnonymizeIP)
                    .WithProjectID(userContext.ProjectId)
                    .WithRevision(userContext.Revision);
            }

            if (visitors.Count == 0) {
                return null;
            }

            builder.WithVisitors(visitors.ToArray());
            var json = JsonConvert.SerializeObject(builder.Build());
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            return new LogEvent(EVENT_ENDPOINT, dictionary, "POST", headers: new Dictionary<string, string>
                {
                    { "Content-type", "application/json" }
                });
        }

        private static Visitor CreateVisitor(ImpressionEvent impressionEvent) {
            if (impressionEvent == null) {
                return null;
            }

            var userContext = impressionEvent.Context;

            Decision decision = new Decision(impressionEvent.Experiment.LayerId,
                impressionEvent.Experiment.Id,
                impressionEvent.Variation.Id);

            SnapshotEvent snapshotEvent = new SnapshotEvent(impressionEvent.Experiment.LayerId,
                impressionEvent.UUID,
                ACTIVATE_EVENT_KEY,
                impressionEvent.TimeStamp,
                null,
                null,
                null);

            Snapshot snapshot = new Snapshot(new SnapshotEvent[] { snapshotEvent }, new Decision[] { decision });

            return new Visitor(new Snapshot[] { snapshot }, impressionEvent.UserAttributes, impressionEvent.UserId);
            
        }

        private static Visitor CreateVisitor(ConversionEvent conversionEvent) {
            if (conversionEvent == null) {
                return null;
            }

            EventContext userContext = conversionEvent.Context;

            SnapshotEvent snapshotEvent = new SnapshotEvent(conversionEvent.Event.Id,
                conversionEvent.UUID,
                conversionEvent.Event.Key,
                conversionEvent.TimeStamp,
                (int?) EventTagUtils.GetRevenueValue(conversionEvent.EventTags, Logger),
                (long?) EventTagUtils.GetNumericValue(conversionEvent.EventTags, Logger),
                conversionEvent.EventTags);

            Snapshot snapshot = new Snapshot(new SnapshotEvent[] { snapshotEvent });

            return new Visitor(new Snapshot[] { snapshot }, conversionEvent.UserAttributes, conversionEvent.UserId);
      
        }

        /**
        private static VisitorAttribute[] BuildAttributeList(ProjectConfig projectConfig, VisitorAttribute[] attributes)
        {
            List<VisitorAttribute> attributesList = new List<VisitorAttribute>();

            if (attributes != null)
            {
                foreach (VisitorAttribute visitorAttribute in attributes)
                {

                    // Ignore attributes with empty key
                    if (string.IsNullOrEmpty(visitorAttribute.Key))
                    {
                        continue;
                    }

                    // Filter down to the types of values we're allowed to track.
                    // Don't allow Longs, BigIntegers, or BigDecimals - they /can/ theoretically be serialized as JSON numbers
                    // but may take on values that can't be faithfully parsed by the backend.
                    // https://developers.optimizely.com/x/events/api/#Attribute
                    if (visitorAttribute.Value == null ||
                        !((visitorAttribute.Value is string) ||
                            (visitorAttribute.Value is bool) ||
                            (Validator.IsValidNumericValue(visitorAttribute.Value))))
                    {
                        continue;
                    }

                    string attributeId = projectConfig.GetAttributeId(visitorAttribute.Key);
                    if (attributeId == null)
                    {
                        continue;
                    }

                    VisitorAttribute visitorAttr = new VisitorAttribute(attributeId, visitorAttribute.Key, "custom_attribute", visitorAttribute.Value);

                    attributesList.Add(visitorAttr);
                }
            }   

            //checks if botFiltering value is not set in the project config file.
            if (projectConfig.BotFiltering != null)
            {
                VisitorAttribute attribute = new VisitorAttribute(ControlAttributes.BOT_FILTERING_ATTRIBUTE,
                    ControlAttributes.BOT_FILTERING_ATTRIBUTE,
                    "custom_attribute",
                    projectConfig.BotFiltering);

                attributesList.Add(attribute);
            }

            return attributesList.ToArray();
        }
        **/
    }
}
