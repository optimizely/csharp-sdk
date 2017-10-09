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
using OptimizelySDK.Bucketing;
using OptimizelySDK.Entity;
using OptimizelySDK.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimizelySDK.Event.Builder
{
    public class EventBuilder
    {
        private const string SDK_TYPE = "csharp-sdk";

        private const string SDK_VERSION = "1.2.0";

        private const string IMPRESSION_ENDPOINT = "https://logx.optimizely.com/v1/events";

        private const string CONVERSION_ENDPOINT = "https://logx.optimizely.com/v1/events";

        private const string HTTP_VERB = "POST";

        private const string CUSTOM_ATTRIBUTE_FEATURE_TYPE = "custom";

        private const string ACTIVATE_EVENT_KEY = "campaign_activated";

        public const string RESERVED_ATTRIBUTE_KEY_BUCKETING_ID_EVENT_PARAM_KEY = "optimizely_bucketing_id";

        private static readonly Dictionary<string, string> HTTP_HEADERS = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" },
        };

        public Dictionary<string, object> EventParams { get; private set; }

        private Bucketer Bucketer;

        public EventBuilder(Bucketer bucketer)
        {
            Bucketer = bucketer; 
            ResetParams();
        }

        /// <summary>
        /// Reset the Event Parameters
        /// </summary>
        public void ResetParams()
        {
            EventParams = new Dictionary<string, object>();
        }

        /// <summary>
        /// Helper to compute Unix time (i.e. since Jan 1, 1970)
        /// </summary>
        private static long SecondsSince1970
        {
            get
            {
                return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            }
        }

        /// <summary>
        /// Helper function to set parameters common to impression and conversion event
        /// </summary>
        /// <param name="config">ProjectConfig Configuration for the project</param>
        /// <param name="userId">string ID of user</param>
        /// <param name="userAttributes">associative array of Attributes for the user</param>
        private Dictionary<string, object> GetCommonParams(ProjectConfig config, string userId, UserAttributes userAttributes)
        {
            var comonParams = new Dictionary<string, object>();

            var visitor = new Dictionary<string, object>
            {
                { "snapshots", new object[0]},
                { "visitor_id", userId },
                { "attributes", new object[0] }
            };

            comonParams[Params.VISITORS] = new object[] { visitor };
            comonParams[Params.PROJECT_ID] = config.ProjectId;
            comonParams[Params.ACCOUNT_ID] = config.AccountId;
            comonParams[Params.CLIENT_ENGINE] = SDK_TYPE;
            comonParams[Params.CLIENT_VERSION] = SDK_VERSION;
            comonParams[Params.REVISION] = config.Revision;
            comonParams[Params.ANONYMIZE_IP] = config.AnonymizeIP;

            var userFeatures = new List<Dictionary<string, object>>();

            foreach (var userAttribute in userAttributes.Where(a => !string.IsNullOrEmpty(a.Key)))
            {
                if (string.Equals(userAttribute.Key, DecisionService.RESERVED_ATTRIBUTE_KEY_BUCKETING_ID))
                {
                    var userFeature = new Dictionary<string, object>
                    {
                        { "key", RESERVED_ATTRIBUTE_KEY_BUCKETING_ID_EVENT_PARAM_KEY },
                        { "type", CUSTOM_ATTRIBUTE_FEATURE_TYPE },
                        { "value", userAttribute.Value}
                    };
                    userFeatures.Add(userFeature);
                }
                else
                {
                    var attributeEntity = config.GetAttribute(userAttribute.Key);
                    if (attributeEntity != null && attributeEntity.Key != null)
                    {
                        var userFeature = new Dictionary<string, object>
                        {
                            { "entity_id", attributeEntity.Id },
                            { "key", attributeEntity.Key },
                            { "type", CUSTOM_ATTRIBUTE_FEATURE_TYPE },
                            { "value",  userAttribute.Value}
                        };
                        userFeatures.Add(userFeature);
                    }
                }
            }


            visitor["attributes"] = userFeatures;

            return comonParams;
        }

        private Dictionary<string, object> GetImpressionParams(Experiment experiment, string variationId)
        {

            var impressionEvent = new Dictionary<string, object>();

            var decisions = new object[]
            {
                new Dictionary<string, object>
                {
                    { Params.CAMPAIGN_ID,   experiment.LayerId },
                    { Params.EXPERIMENT_ID, experiment.Id },
                    { Params.VARIATION_ID,  variationId }
                }
            };
                

            var events = new object[]
            {
                new Dictionary<string, object>
                {
                    { "entity_id", experiment.LayerId },
                    {"timestamp", SecondsSince1970*1000 },
                    {"key", ACTIVATE_EVENT_KEY },
                    {"uuid", Guid.NewGuid() }
                }
            };

            impressionEvent[Params.DECISIONS] = decisions;
            impressionEvent[Params.EVENTS] = events;

            return impressionEvent;
        }

        private List<object> GetConversionParams(ProjectConfig config, string eventKey, Dictionary<string, Variation> experimentIdVariationMap, string userId, Dictionary<string, object> eventTags)
        {

            var conversionEventParams = new List<object>();
            foreach (var experimentId in experimentIdVariationMap.Keys)
            {
                var variation = experimentIdVariationMap[experimentId];
                var experiment = config.ExperimentIdMap[experimentId];
                var eventEntity = config.EventKeyMap[eventKey];

                if (string.IsNullOrEmpty(variation.Key)) continue;
                var decision = new Dictionary<string, object>
                {
                    {
                        Params.DECISIONS, new object[]
                        {
                            new Dictionary<string, object>
                            {
                                { Params.CAMPAIGN_ID, experiment.LayerId },
                                {Params.EXPERIMENT_ID, experiment.Id },
                                {Params.VARIATION_ID, variation.Id }
                            }
                        }
                    },
                    {
                        Params.EVENTS, new object[0]
                    }
                };

                var eventDict = new Dictionary<string, object>
                {
                    {Params.ENTITY_ID, eventEntity.Id },
                    {Params.TIMESTAMP, SecondsSince1970*1000 },
                    {"uuid", Guid.NewGuid()},
                    {"key", eventKey }
                };

                if(eventTags != null)
                {
                    var revenue = EventTagUtils.GetRevenueValue(eventTags);

                    if (revenue != null)
                    {
                        eventDict[EventTagUtils.REVENUE_EVENT_METRIC_NAME] = revenue;
                    }

                    var eventVallue = EventTagUtils.GetNumericValue(eventTags, new Logger.DefaultLogger());

                    if(eventVallue != null)
                    {
                        eventDict[EventTagUtils.VALUE_EVENT_METRIC_NAME] = eventVallue;
                    }

                    eventDict["tags"] = eventTags;
                }

                decision[Params.EVENTS] = new object[] { eventDict };

                conversionEventParams.Add(decision);
            }

            return conversionEventParams;
        }

        private Dictionary<string, object> GetImpressionOrConversionParamsWithCommonParams(Dictionary<string, object> commonParams, object[] conversionOrImpressionOnlyParams)
        {
            var visitors = commonParams[Params.VISITORS] as object[];

            if(visitors.Length > 0)
            {
                var visitor = visitors[0] as Dictionary<string, object>;
                visitor["snapshots"] = conversionOrImpressionOnlyParams;
            }

            return commonParams;
        }

        /// <summary>
        /// Create impression event to be sent to the logging endpoint.
        /// </summary>
        /// <param name="config">ProjectConfig Configuration for the project</param>
        /// <param name="experiment">Experiment being activated</param>
        /// <param name="variationId">Variation Id</param>
        /// <param name="userId">User Id</param>
        /// <param name="userAttributes">associative array of attributes for the user</param>
        /// <returns>LogEvent object to be sent to dispatcher</returns>
        public virtual LogEvent CreateImpressionEvent(ProjectConfig config, Experiment experiment, string variationId,
            string userId, UserAttributes userAttributes)
        {

            var commonParams = GetCommonParams(config, userId, userAttributes ?? new UserAttributes());
            var impressionOnlyParams = GetImpressionParams(experiment, variationId);

            var impressionParams = GetImpressionOrConversionParamsWithCommonParams(commonParams, new object[] { impressionOnlyParams });

            return new LogEvent(IMPRESSION_ENDPOINT, impressionParams, HTTP_VERB, HTTP_HEADERS);
        }


        /// <summary>
        /// Create conversion event to be sent to the logging endpoint.
        /// </summary>
        /// <param name="config">ProjectConfig Configuration for the project.</param>
        /// <param name="eventKey">Event Key representing the event</param>
        /// <param name="ExperimentIdVariationMap">Map of experiment ID to the variation that the user is bucketed into.</param>
        /// <param name="userId">ID of user</param>
        /// <param name="userAttributes">associative array of Attributes for the user</param>
        /// <param name="eventValue">integer Value associated with the event</param>
        /// <returns>LogEvent object to be sent to dispatcher</returns>
        public virtual LogEvent CreateConversionEvent(ProjectConfig config, string eventKey, Dictionary<string, Variation> experimentIdVariationMap, string userId, UserAttributes userAttributes, EventTags eventTags)
        {
            var commonParams = GetCommonParams(config, userId, userAttributes ?? new UserAttributes());

            var conversionOnlyParams = GetConversionParams(config, eventKey, experimentIdVariationMap, userId, eventTags).ToArray();

            var conversionParams = GetImpressionOrConversionParamsWithCommonParams(commonParams, conversionOnlyParams);

            return new LogEvent(CONVERSION_ENDPOINT, conversionParams, HTTP_VERB, HTTP_HEADERS);
        }
    }
}
