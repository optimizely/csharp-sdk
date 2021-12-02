/*
 * Copyright 2019-2021, Optimizely
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
using System.Collections.Generic;

namespace OptimizelySDK
{
    public interface ProjectConfig
    {
        /// <summary>
        /// Version of the datafile.
        /// </summary>
        string Version { get; set; }

        /// <summary>
        /// Account ID of the account using the SDK.
        /// </summary>
        string AccountId { get; set; }

        /// <summary>
        /// Project ID of the Full Stack project.
        /// </summary>
        string ProjectId { get; set; }

        /// <summary>
        /// Revision of the datafile.
        /// </summary>
        string Revision { get; set; }

        /// <summary>
        /// SDK key of the datafile.
        /// </summary>
        string SDKKey { get; set; }

        /// <summary>
        /// Environment key of the datafile.
        /// </summary>
        string EnvironmentKey { get; set; }

        /// <summary>
        /// SendFlagDecisions determines whether impressions events are sent for ALL decision types.
        /// </summary>
        bool SendFlagDecisions { get; set; }

        /// <summary>
        /// Allow Anonymize IP by truncating the last block of visitors' IP address.
        /// </summary>
        bool AnonymizeIP { get; set; }

        /// <summary>
        /// Bot filtering flag.
        /// </summary>
        bool? BotFiltering { get; set; }

        //========================= Mappings ===========================

        /// <summary>
        /// Associative array of group ID to Group(s) in the datafile
        /// </summary>
        Dictionary<string, Group> GroupIdMap { get; }

        /// <summary>
        /// Associative array of experiment key to Experiment(s) in the datafile
        /// </summary>
        Dictionary<string, Experiment> ExperimentKeyMap { get; }

        /// <summary>
        /// Associative array of experiment ID to Experiment(s) in the datafile
        /// </summary>
        Dictionary<string, Experiment> ExperimentIdMap { get; }

        /// <summary>
        /// Associative array of experiment key to associative array of variation key to variations
        /// </summary>
        Dictionary<string, Dictionary<string, Variation>> VariationKeyMap { get; }

        /// <summary>
        /// Associative array of experiment key to associative array of variation ID to variations
        /// </summary>
        Dictionary<string, Dictionary<string, Variation>> VariationIdMap { get; }

        /// <summary>
        /// Associative array of event key to Event(s) in the datafile
        /// </summary>
        Dictionary<string, Entity.Event> EventKeyMap { get; }

        /// <summary>
        /// Associative array of attribute key to Attribute(s) in the datafile
        /// </summary>
        Dictionary<string, Attribute> AttributeKeyMap { get; }

        /// <summary>
        /// Associative array of audience ID to Audience(s) in the datafile
        /// </summary>
        Dictionary<string, Audience> AudienceIdMap { get; }

        /// <summary>
        /// Associative array of Feature Key to Feature(s) in the datafile
        /// </summary>
        Dictionary<string, FeatureFlag> FeatureKeyMap { get; }

        /// <summary>
        /// Associative array of Rollout ID to Rollout(s) in the datafile
        /// </summary>
        Dictionary<string, Rollout> RolloutIdMap { get; }

        /// <summary>
        /// Associative dictionary of Flag to Variation key and Variation in the datafile
        /// </summary>
        Dictionary<string, Dictionary<string, Variation>> FlagVariationMap { get; }

        //========================= Datafile Entities ===========================

        /// <summary>
        /// Associative list of groups to Group(s) in the datafile
        /// </summary>
        Group[] Groups { get; set; }

        /// <summary>
        /// Associative list of experiments to Experiment(s) in the datafile.
        /// </summary>
        Experiment[] Experiments { get; set; }

        /// <summary>
        /// Associative list of events.
        /// </summary>
        Entity.Event[] Events { get; set; }

        /// <summary>
        /// Associative list of Attributes.
        /// </summary>
        Attribute[] Attributes { get; set; }

        /// <summary>
        /// Associative list of Audiences.
        /// </summary>
        Audience[] Audiences { get; set; }

        /// <summary>
        /// Associative list of Typed Audiences.
        /// </summary>
        Audience[] TypedAudiences { get; set; }

        /// <summary>
        /// Associative list of FeatureFlags.
        /// </summary>
        FeatureFlag[] FeatureFlags { get; set; }

        /// <summary>
        /// Associative list of Rollouts.
        /// </summary>
        Rollout[] Rollouts { get; set; }

        //========================= Getters ===========================

        /// <summary>
        /// Get the group associated with groupId
        /// </summary>
        /// <param name="groupId">string ID of the group</param>
        /// <returns>Group Entity corresponding to the ID or a dummy entity if groupId is invalid</returns>
        Group GetGroup(string groupId);

        /// <summary>
        /// Get the experiment from the key
        /// </summary>
        /// <param name="experimentKey">Key of the experiment</param>
        /// <returns>Experiment Entity corresponding to the key or a dummy entity if key is invalid</returns>
        Experiment GetExperimentFromKey(string experimentKey);

        /// <summary>
        /// Get the experiment from the ID
        /// </summary>
        /// <param name="experimentId">ID of the experiment</param>
        /// <returns>Experiment Entity corresponding to the IDkey or a dummy entity if ID is invalid</returns>
        Experiment GetExperimentFromId(string experimentId);

        /// <summary>
        /// Get the Event from the key
        /// </summary>
        /// <param name="eventKey">Key of the event</param>
        /// <returns>Event Entity corresponding to the key or a dummy entity if key is invalid</returns>
        Entity.Event GetEvent(string eventKey);

        /// <summary>
        /// Get the Audience from the ID
        /// </summary>
        /// <param name="audienceId">ID of the Audience</param>
        /// <returns>Audience Entity corresponding to the ID or a dummy entity if ID is invalid</returns>
        Audience GetAudience(string audienceId);

        /// <summary>
        /// Get the Attribute from the key
        /// </summary>
        /// <param name="attributeKey">Key of the Attribute</param>
        /// <returns>Attribute Entity corresponding to the key or a dummy entity if key is invalid</returns>
        Attribute GetAttribute(string attributeKey);

        /// <summary>
        /// Get the Variation from the keys
        /// </summary>
        /// <param name="experimentKey">key for Experiment</param>
        /// <param name="variationKey">key for Variation</param>
        /// <returns>Variation Entity corresponding to the provided experiment key and variation key or a dummy
        /// entity if keys are invalid</returns>
        Variation GetVariationFromKey(string experimentKey, string variationKey);

        /// <summary>
        /// Get the Variation from the keys
        /// </summary>
        /// <param name="experimentId">ID for Experiment</param>
        /// <param name="variationKey">key for Variation</param>
        /// <returns>Variation Entity corresponding to the provided experiment key and variation key or a dummy
        /// entity if keys are invalid</returns>
        Variation GetVariationFromKeyByExperimentId(string experimentId, string variationKey);

        /// <summary>
        /// Get the Variation from the Key/ID
        /// </summary>
        /// <param name="experimentKey">key for Experiment</param>
        /// <param name="variationId">ID for Variation</param>
        /// <returns>Variation Entity corresponding to the provided experiment key and variation ID or a dummy
        /// entity if key or ID is invalid</returns>
        Variation GetVariationFromId(string experimentKey, string variationId);

        /// <summary>
        /// Get the Variation from the Key/ID
        /// </summary>
        /// <param name="experimentId">ID for Experiment</param>
        /// <param name="variationId">ID for Variation</param>
        /// <returns>Variation Entity corresponding to the provided experiment key and variation ID or a dummy
        /// entity if key or ID is invalid</returns>
        Variation GetVariationFromIdByExperimentId(string experimentId, string variationId);

        /// <summary>
        /// Get the feature from the key
        /// </summary>
        /// <param name="featureKey">Key of the feature</param>
        /// <returns>Feature Flag Entity corresponding to the key or a dummy entity if key is invalid</returns>
        FeatureFlag GetFeatureFlagFromKey(string featureKey);

        /// <summary>
        /// Gets the variation associated with an experiment or rollout for a given feature flag key
        /// </summary>
        /// <param name="flagKey">feature flag key</param>
        /// <param name="variationKey">variation key</param>
        /// <returns></returns>
        Variation GetFlagVariationByKey(string flagKey, string variationKey);

        /// <summary>
        /// Get the rollout from the ID
        /// </summary>
        /// <param name="rolloutId">ID for rollout</param>
        /// <returns>Rollout Entity corresponding to the rollout ID or a dummy entity if ID is invalid</returns>
        Rollout GetRolloutFromId(string rolloutId);

        /// <summary>
        /// Get attribute ID for the provided attribute key
        /// </summary>
        /// <param name="attributeKey">Key of the Attribute</param>
        /// <returns>Attribute ID corresponding to the provided attribute key. Attribute key if it is a reserved attribute</returns>
        string GetAttributeId(string attributeKey);

        /// <summary>
        /// Check if the provided experiment Id belongs to any feature, false otherwise.
        /// </summary>
        /// <param name="experimentId">Experiment Id</param>
        /// <returns>true if experiment belongs to any feature, false otherwise</returns>
        bool IsFeatureExperiment(string experimentId);

        /// <summary>
        /// provides List of features associated with given experiment.
        /// </summary>
        /// <param name="experimentId">Experiment Id</param>
        /// <returns>List| Feature flag ids list, null otherwise</returns>
        List<string> GetExperimentFeatureList(string experimentId);

        /// <summary>
        /// Returns the datafile corresponding to ProjectConfig
        /// </summary>
        string ToDatafile();
    }
}
