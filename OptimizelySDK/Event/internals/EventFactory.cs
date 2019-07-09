﻿/* 
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

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OptimizelySDK.Entity;
using OptimizelySDK.Event.Builder;
using OptimizelySDK.Event.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;


namespace OptimizelySDK.Event.internals
{
    public class EventFactory
    {
        private const string CUSTOM_ATTRIBUTE_FEATURE_TYPE = "custom";
        public const string EVENT_ENDPOINT = "https://logx.optimizely.com/v1/events";  // Should be part of the datafile

        private const string ACTIVATE_EVENT_KEY = "campaign_activated";

        public static LogEvent CreateLogEvent(UserEvent userEvent, ILogger logger) {
                        
            return CreateLogEvent(new UserEvent[] { userEvent }, logger);
        }

        public static LogEvent CreateLogEvent(UserEvent[] userEvents, ILogger logger) {

            EventBatch.Builder builder = new EventBatch.Builder();

            List<Visitor> visitors = new List<Visitor>(userEvents.Count());

            foreach (UserEvent userEvent in userEvents) {

                if (userEvent is ImpressionEvent) {
                    visitors.Add(CreateVisitor((ImpressionEvent) userEvent));
                }
                else if (userEvent is ConversionEvent) {                
                    visitors.Add(CreateVisitor((ConversionEvent) userEvent, logger));
                }
                else {
                    //TODO: Need to log a message, invalid UserEvent added in a list.
                    continue;
                }
               
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

            EventBatch eventBatch = builder.Build();

            var eventBatchDictionary = JObject.FromObject(eventBatch).ToObject<Dictionary<string, object>>();

            return new LogEvent(EVENT_ENDPOINT, eventBatchDictionary, "POST", headers: new Dictionary<string, string> {
                { "Content-type", "application/json" }
            });
        }

        private static Visitor CreateVisitor(ImpressionEvent impressionEvent) {

            if (impressionEvent == null) {
                return null;
            }

            var eventContext = impressionEvent.Context;

            Decision decision = new Decision(impressionEvent.Experiment?.LayerId,
                impressionEvent.Experiment?.Id,
                impressionEvent.Variation?.Id);

            SnapshotEvent snapshotEvent = new SnapshotEvent(impressionEvent.Experiment.LayerId,
                impressionEvent.UUID,
                ACTIVATE_EVENT_KEY,
                impressionEvent.Timestamp);

            Snapshot snapshot = new Snapshot(
                new SnapshotEvent[] { snapshotEvent },
                new Decision[] { decision });
            
            var visitor = new Visitor(new Snapshot[] { snapshot }, impressionEvent.VisitorAttributes, impressionEvent.UserId);

            return visitor;
        }

        private static Visitor CreateVisitor(ConversionEvent conversionEvent, ILogger logger) {
            if (conversionEvent == null) {
                return null;
            }

            EventContext userContext = conversionEvent.Context;

            SnapshotEvent snapshotEvent = new SnapshotEvent(conversionEvent.Event.Id,
                conversionEvent.UUID,
                conversionEvent.Event?.Key,
                conversionEvent.Timestamp,
                (int?) EventTagUtils.GetRevenueValue(conversionEvent.EventTags, logger),
                (long?) EventTagUtils.GetNumericValue(conversionEvent.EventTags, logger),
                conversionEvent.EventTags);

            Snapshot snapshot = new Snapshot(new SnapshotEvent[] { snapshotEvent });
            
            var visitor = new Visitor(new Snapshot[] { snapshot }, conversionEvent.VisitorAttributes, conversionEvent.UserId);

            return visitor;
        }
        
        public static VisitorAttribute[] BuildAttributeList(UserAttributes userAttributes, ProjectConfig config)
        {            
            if (config == null)
                return null;

            List<VisitorAttribute> attributesList = new List<VisitorAttribute>();

            if (userAttributes != null)
            {
                foreach (var validUserAttribute in userAttributes.Where(attribute => Validator.IsUserAttributeValid(attribute))) {

                    var attributeId = config.GetAttributeId(validUserAttribute.Key);
                    if (!string.IsNullOrEmpty(attributeId)) {
                        attributesList.Add(new VisitorAttribute(entityId: attributeId, key: validUserAttribute.Key,
                            type: CUSTOM_ATTRIBUTE_FEATURE_TYPE, value: validUserAttribute.Value));
                    }
                }
            }

            //checks if botFiltering value is not set in the project config file.
            if (config.BotFiltering.HasValue) {

                attributesList.Add(new VisitorAttribute(entityId: ControlAttributes.BOT_FILTERING_ATTRIBUTE,
                    key: ControlAttributes.BOT_FILTERING_ATTRIBUTE, type: CUSTOM_ATTRIBUTE_FEATURE_TYPE, value: config.BotFiltering));                
            }

            return attributesList.ToArray();
        }
    }
}