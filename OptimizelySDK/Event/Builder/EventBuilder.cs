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

        private const string SDK_VERSION = "0.0.1";

        private const string IMPRESSION_ENDPOINT = "https://logx.optimizely.com/log/decision";

        private const string CONVERSION_ENDPOINT = "https://logx.optimizely.com/log/event";

        private const string HTTP_VERB = "POST";

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
        public void SetCommonParams(ProjectConfig config, string userId, UserAttributes userAttributes)
        {
            EventParams[Params.PROJECT_ID] = config.ProjectId;
            EventParams[Params.ACCOUNT_ID] = config.AccountId;
            EventParams[Params.VISITOR_ID] = userId;
            EventParams[Params.CLIENT_ENGINE] = SDK_TYPE;
            EventParams[Params.CLIENT_VERSION] = SDK_VERSION;
            EventParams[Params.IS_GLOBAL_HOLDBACK] = false;
            EventParams[Params.TIME] = SecondsSince1970 * 1000L;
            userAttributes = userAttributes ?? new UserAttributes();

            var userFeatures = new List<Dictionary<string, object>>();

            foreach (var userAttribute in userAttributes.Where(a => !string.IsNullOrEmpty(a.Key)))
            {
                var attributeEntity = config.GetAttribute(userAttribute.Key);
                if (attributeEntity != null && attributeEntity.Key != null)
                {
                    var userFeature = new Dictionary<string, object>
                    {
                        { "id", attributeEntity.Id },
                        { "name", attributeEntity.Key },
                        { "type", "custom" },
                        { "value",  userAttribute.Value},
                        { "shouldIndex", true }
                    };
                    userFeatures.Add(userFeature);
                }
            }
            EventParams[Params.USER_FEATURES] = userFeatures;
        }

        private void SetImpressionParams(Experiment experiment, string variationId)
        {
            EventParams[Params.LAYER_ID] = experiment.LayerId;
            EventParams[Params.DECISION] = new Dictionary<string, object>
            {
                { Params.EXPERIMENT_ID, experiment.Id },
                { Params.VARIATION_ID, variationId },
                { Params.IS_LAYER_HOLDBACK, false }
            };
        }

        private void SetConversionParams(ProjectConfig config, string eventKey, Experiment[] experiments, string userId, Dictionary<string, object> eventTags)
        {
            EventParams[Params.EVENT_FEATURES] = new object[0];
            EventParams[Params.EVENT_METRICS] = new object[0];

            var eventFeatures = new List<Dictionary<string, object>>();
            var eventMetrics = new List<Dictionary<string, object>>();

            if (eventTags != null)
            {
                foreach (var keyValuePair in eventTags)
                {
                    if (keyValuePair.Value == null)
                    {
                        continue;
                    }

                    var eventFeature = new Dictionary<string, object>
                    {
                        {"name", keyValuePair.Key },
                        {"type", "custom" },
                        {"value", keyValuePair.Value },
                        {"shouldIndex", false }
                    };
                    eventFeatures.Add(eventFeature);
                }
                var eventValue = EventTagUtils.GetRevenueValue(eventTags);

                if (eventValue != null)
                {
                    var eventMetric = new Dictionary<string, object>
                        {
                            {"name", EventTagUtils.REVENUE_EVENT_METRIC_NAME },
                            {"value", eventValue }
                        };
                    eventMetrics.Add(eventMetric);
                }

                EventParams[Params.EVENT_FEATURES] = eventFeatures;
                EventParams[Params.EVENT_METRICS] = eventMetrics;
            }

            //if (eventValue.HasValue && eventValue != 0)
            //{
            //    EventParams[Params.EVENT_METRICS] = new object[]
            //    {
            //        new Dictionary<string, object>
            //        {
            //            {"name", "revenue" },
            //            {"value", eventValue }
            //        }
            //    };
            //}

            var eventEntity = config.GetEvent(eventKey);
            EventParams[Params.EVENT_ID] = eventEntity.Id;
            EventParams[Params.EVENT_NAME] = eventKey;
            var layerStates = new List<Dictionary<string, object>>();

            foreach (var experiment in experiments)
            {
                var variation = Bucketer.Bucket(config, experiment, userId);
                if (!string.IsNullOrEmpty(variation.Key))
                {
                    layerStates.Add(new Dictionary<string, object>
                    {
                        { Params.LAYER_ID, experiment.LayerId },
                        { Params.ACTION_TRIGGERED, true },
                        {
                            Params.DECISION, new Dictionary<string, object>
                            {
                                { Params.EXPERIMENT_ID, experiment.Id },
                                { Params.VARIATION_ID, variation.Id },
                                { Params.IS_LAYER_HOLDBACK, false }
                            }
                        }
                    });
                }
            }

            EventParams[Params.LAYER_STATES] = layerStates;
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
            ResetParams();
            SetCommonParams(config, userId, userAttributes);
            SetImpressionParams(experiment, variationId);

            return new LogEvent(IMPRESSION_ENDPOINT, EventParams, HTTP_VERB, HTTP_HEADERS);
        }


        /// <summary>
        /// Create conversion event to be sent to the logging endpoint.
        /// </summary>
        /// <param name="config">ProjectConfig Configuration for the project.</param>
        /// <param name="eventKey">Event Key representing the event</param>
        /// <param name="experiments">collection of Experiments for which conversion event needs to be recorded</param>
        /// <param name="userId">ID of user</param>
        /// <param name="userAttributes">associative array of Attributes for the user</param>
        /// <param name="eventValue">integer Value associated with the event</param>
        /// <returns>LogEvent object to be sent to dispatcher</returns>
        public virtual LogEvent CreateConversionEvent(ProjectConfig config, string eventKey, IEnumerable<Experiment> experiments,
            string userId, UserAttributes userAttributes, EventTags eventTags)
        {
            ResetParams();
            SetCommonParams(config, userId, userAttributes);
            SetConversionParams(config, eventKey, experiments.ToArray(), userId, eventTags);
            return new LogEvent(CONVERSION_ENDPOINT, EventParams, HTTP_VERB, HTTP_HEADERS);
        }
    }
}