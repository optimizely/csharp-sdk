/* 
 * Copyright 2019-2020, Optimizely
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
using OptimizelySDK.Event.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;


namespace OptimizelySDK.Event
{
    /// <summary>
    /// EventFactory builds LogEvent objects from a given UserEvent.
    /// This class serves to separate concerns between events in the SDK and the API used 
    /// to record the events via the <see href="https://developers.optimizely.com/x/events/api/index.html">Optimizely Events API</see>.
    /// </summary>
    public class EventFactory
    {
        private const string CUSTOM_ATTRIBUTE_FEATURE_TYPE = "custom";

        // Supported regions for event endpoints
        public static readonly string[] SupportedRegions = { "US", "EU" };

        // Dictionary of event endpoints for different regions
        public static readonly Dictionary<string, string> EventEndpoints = new Dictionary<string, string>
        {
            {"US", "https://logx.optimizely.com/v1/events"},
            {"EU", "https://eu.logx.optimizely.com/v1/events"}
        };

        private const string ACTIVATE_EVENT_KEY = "campaign_activated";

        /// <summary>
        /// Create LogEvent instance
        /// </summary>
        /// <param name="userEvent">The UserEvent entity</param>
        /// <param name="logger">The ILogger entity</param>
        /// <returns>LogEvent instance</returns>
        public static LogEvent CreateLogEvent(UserEvent userEvent, ILogger logger)
        {
            return CreateLogEvent(new UserEvent[] { userEvent }, logger);
        }

        /// <summary>
        /// Create LogEvent instance
        /// </summary>
        /// <param name="userEvents">The UserEvent array</param>
        /// <param name="logger">The ILogger entity</param>
        /// <returns>LogEvent instance</returns>
        public static LogEvent CreateLogEvent(UserEvent[] userEvents, ILogger logger)
        {
            var builder = new EventBatch.Builder();

            var visitors = new List<Visitor>(userEvents.Count());

            // Default to US region
            string region = "US";

            foreach (var userEvent in userEvents)
            {
                if (userEvent is ImpressionEvent)
                {
                    visitors.Add(CreateVisitor((ImpressionEvent)userEvent));
                }
                else if (userEvent is ConversionEvent)
                {
                    visitors.Add(CreateVisitor((ConversionEvent)userEvent, logger));
                }
                else
                {
                    //TODO: Need to log a message, invalid UserEvent added in a list.
                    continue;
                }

                var userContext = userEvent.Context;

                // Get region from the event's context, default to US if not specified
                region = !string.IsNullOrEmpty(userContext.Region) ? userContext.Region : "US";

                builder.WithClientName(userContext.ClientName).
                    WithClientVersion(userContext.ClientVersion).
                    WithAccountId(userContext.AccountId).
                    WithAnonymizeIP(userContext.AnonymizeIP).
                    WithProjectID(userContext.ProjectId).
                    WithRevision(userContext.Revision).
                    WithEnrichDecisions(true);
            }

            if (visitors.Count == 0)
            {
                return null;
            }

            builder.WithVisitors(visitors.ToArray());

            var eventBatch = builder.Build();

            var eventBatchDictionary =
                JObject.FromObject(eventBatch).ToObject<Dictionary<string, object>>();

            // Use the region to determine the endpoint URL, falling back to US if the region is not found or not supported
            string endpointUrl = EventEndpoints["US"]; // Default to US endpoint

            // Only try to use the region-specific endpoint if it's a supported region
            if (SupportedRegions.Contains(region) && EventEndpoints.ContainsKey(region))
            {
                endpointUrl = EventEndpoints[region];
            }

            return new LogEvent(endpointUrl, eventBatchDictionary, "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                });
        }

        /// <summary>
        /// Create Visitor instance
        /// </summary>
        /// <param name="impressionEvent">The ImpressionEvent entity</param>
        /// <returns>Visitor instance if ImpressionEvent is valid, null otherwise</returns>
        private static Visitor CreateVisitor(ImpressionEvent impressionEvent)
        {
            if (impressionEvent == null)
            {
                return null;
            }

            var decision = new Decision(impressionEvent.Experiment?.LayerId,
                impressionEvent.Experiment?.Id ?? string.Empty,
                impressionEvent.Variation?.Id,
                impressionEvent.Metadata);

            var snapshotEvent = new SnapshotEvent.Builder().WithUUID(impressionEvent.UUID).
                WithEntityId(impressionEvent.Experiment?.LayerId).
                WithKey(ACTIVATE_EVENT_KEY).
                WithTimeStamp(impressionEvent.Timestamp).
                Build();

            var snapshot = new Snapshot(
                new SnapshotEvent[] { snapshotEvent },
                new Decision[] { decision });

            var visitor = new Visitor(new Snapshot[] { snapshot },
                impressionEvent.VisitorAttributes, impressionEvent.UserId);

            return visitor;
        }

        /// <summary>
        /// Create Visitor instance
        /// </summary>
        /// <param name="conversionEvent">The ConversionEvent entity</param>
        /// <param name="logger">The ILogger entity</param>
        /// <returns>Visitor instance if ConversionEvent is valid, null otherwise</returns>
        private static Visitor CreateVisitor(ConversionEvent conversionEvent, ILogger logger)
        {
            if (conversionEvent == null)
            {
                return null;
            }

            var userContext = conversionEvent.Context;
            var revenue = EventTagUtils.GetRevenueValue(conversionEvent.EventTags, logger) as int?;
            var value = EventTagUtils.GetNumericValue(conversionEvent.EventTags, logger) as float?;
            var snapshotEvent = new SnapshotEvent.Builder().WithUUID(conversionEvent.UUID).
                WithEntityId(conversionEvent.Event.Id).
                WithKey(conversionEvent.Event?.Key).
                WithTimeStamp(conversionEvent.Timestamp).
                WithRevenue(revenue).
                WithValue(value).
                WithEventTags(conversionEvent.EventTags).
                Build();


            var snapshot = new Snapshot(new SnapshotEvent[] { snapshotEvent });

            var visitor = new Visitor(new Snapshot[] { snapshot },
                conversionEvent.VisitorAttributes, conversionEvent.UserId);

            return visitor;
        }

        /// <summary>
        /// Create Visitor Attributes list
        /// </summary>
        /// <param name="userAttributes">The user's attributes</param>
        /// <param name="config">ProjectConfig instance</param>
        /// <returns>VisitorAttribute array if config is valid, null otherwise</returns>
        public static VisitorAttribute[] BuildAttributeList(UserAttributes userAttributes,
            ProjectConfig config
        )
        {
            if (config == null)
            {
                return null;
            }

            var attributesList = new List<VisitorAttribute>();

            if (userAttributes != null)
            {
                foreach (var validUserAttribute in userAttributes.Where(attribute =>
                             Validator.IsUserAttributeValid(attribute)))
                {
                    var attributeId = config.GetAttributeId(validUserAttribute.Key);
                    if (!string.IsNullOrEmpty(attributeId))
                    {
                        attributesList.Add(new VisitorAttribute(attributeId, validUserAttribute.Key,
                            CUSTOM_ATTRIBUTE_FEATURE_TYPE, validUserAttribute.Value));
                    }
                }
            }

            //checks if botFiltering value is not set in the project config file.
            if (config.BotFiltering.HasValue)
            {
                attributesList.Add(new VisitorAttribute(ControlAttributes.BOT_FILTERING_ATTRIBUTE,
                    ControlAttributes.BOT_FILTERING_ATTRIBUTE, CUSTOM_ATTRIBUTE_FEATURE_TYPE,
                    config.BotFiltering));
            }

            return attributesList.ToArray();
        }
    }
}
