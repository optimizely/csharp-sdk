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

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OptimizelySDK.Entity;

namespace OptimizelySDK.OptlyConfig
{
    public class OptimizelyConfigService
    {
        private OptimizelyConfig OptimizelyConfig;

        private IDictionary<string, List<FeatureVariable>> featureIdVariablesMap;
        

        public OptimizelyConfigService(ProjectConfig projectConfig)
        {
            if (projectConfig == null)
            {
                return;
            }
            featureIdVariablesMap = GetFeatureVariablesByIdMap(projectConfig);
            var attributes = GetAttributes(projectConfig);
            var audiences = GetAudiences(projectConfig);
            var experimentsMapById = GetExperimentsMapById(projectConfig);
            var experimentsKeyMap = GetExperimentsKeyMap(experimentsMapById);
            
            var featureMap = GetFeaturesMap(projectConfig, experimentsMapById);
            var events = GetEvents(projectConfig);

            OptimizelyConfig = new OptimizelyConfig(projectConfig.Revision,
                projectConfig.SDKKey,
                projectConfig.EnvironmentKey,
                attributes,
                audiences,
                events,
                experimentsKeyMap,
                featureMap,
                projectConfig.ToDatafile());
        }

        private OptimizelyEvent[] GetEvents(ProjectConfig projectConfig)
        {
            var optimizelyEvents = new List<OptimizelyEvent>();
            foreach (var ev in projectConfig.Events)
            {
                var optimizelyEvent = new OptimizelyEvent();
                optimizelyEvent.Id = ev.Id;
                optimizelyEvent.Key = ev.Key;
                optimizelyEvent.ExperimentIds = ev.ExperimentIds;
                optimizelyEvents.Add(optimizelyEvent);
            }
            return optimizelyEvents.ToArray();
        }

        private OptimizelyAudience[] GetAudiences(ProjectConfig projectConfig)
        {
            var typedAudiences = projectConfig.TypedAudiences?.Select(aud => new OptimizelyAudience(aud.Id,
                aud.Name,
                JsonConvert.SerializeObject(aud.Conditions)));
            var typedAudienceIds = typedAudiences.Select(ta => ta.Id).ToList();
            var filteredAudiencesArr = Array.FindAll(projectConfig.Audiences, aud => !aud.Id.Equals("$opt_dummy_audience")
                && !typedAudienceIds.Contains(aud.Id));
            var optimizelyAudience = filteredAudiencesArr.Select(aud => new OptimizelyAudience(aud.Id, aud.Name, aud.Conditions));

            optimizelyAudience = optimizelyAudience.Concat(typedAudiences).OrderBy( aud => aud.Name);

            return optimizelyAudience.ToArray<OptimizelyAudience>();
        }

        private OptimizelyAttribute[] GetAttributes(ProjectConfig projectConfig)
        {
            var attributes = new List<OptimizelyAttribute>();
            foreach (var attr in projectConfig.Attributes)
            {
                var attribute = new OptimizelyAttribute();
                attribute.Id = attr.Id;
                attribute.Key = attr.Key;
                attributes.Add(attribute);
            }
            return attributes.ToArray();
        }

        /// <summary>
        /// Converts Experiment Id map to Experiment Key map.
        /// </summary>
        /// <param name="experimentsMapById"></param>
        /// <returns>Map of experiment key.</returns>

        private IDictionary<string, OptimizelyExperiment> GetExperimentsKeyMap(IDictionary<string, OptimizelyExperiment> experimentsMapById)
        {
            var experimentKeyMaps = new Dictionary<string, OptimizelyExperiment>();

            foreach(var experiment in experimentsMapById.Values) {
                experimentKeyMaps[experiment.Key] = experiment;
            }
            return experimentKeyMaps;
        }

        /// <summary>
        /// Gets Map of all experiments except rollouts
        /// </summary>
        /// <param name="projectConfig">The project config</param>
        /// <returns>Dictionary | Dictionary of experiment key and value as experiment object</returns>
        private IDictionary<string, OptimizelyExperiment> GetExperimentsMapById(ProjectConfig projectConfig)
        {
            var experimentsMap = new Dictionary<string, OptimizelyExperiment>();
            var featureVariableIdMap = GetVariableIdMap(projectConfig);
            var experiments = projectConfig?.Experiments?.ToList();
            experiments = projectConfig?.Groups?.SelectMany(g => g.Experiments).Concat(experiments)?.ToList();

            foreach (Experiment experiment in experiments)
            {
                var featureId = projectConfig.GetExperimentFeatureList(experiment.Id)?.FirstOrDefault();
                var variationsMap = GetVariationsMap(experiment.Variations, featureVariableIdMap, featureId);
                var experimentAudience = GetExperimentAudiences(experiment, projectConfig);
                var optimizelyExperiment = new OptimizelyExperiment(experiment.Id,
                    experiment.Key,
                    experimentAudience,
                    variationsMap);

                experimentsMap.Add(experiment.Id, optimizelyExperiment);
            }

            return experimentsMap;
        }

        /// <summary>
        /// Gets Map of all experiment variations and variables including rollouts
        /// </summary>
        /// <param name="variations">variations</param>
        /// <param name="featureVariableIdMap">The map of feature variables and id</param>
        /// <param name="featureId">feature Id of the feature</param>
        /// <returns>Dictionary | Dictionary of experiment key and value as experiment object</returns>
        private IDictionary<string, OptimizelyVariation> GetVariationsMap(IEnumerable<Variation> variations,
            IDictionary<string, FeatureVariable> featureVariableIdMap,
            string featureId)
        {
            var variationsMap = new Dictionary<string, OptimizelyVariation>();
            foreach (Variation variation in variations)
            {
                var variablesMap = MergeFeatureVariables(
                    featureVariableIdMap,
                    featureId,
                    variation.FeatureVariableUsageInstances,
                    variation.IsFeatureEnabled);

                var optimizelyVariation = new OptimizelyVariation(variation.Id,
                    variation.Key,
                    variation.FeatureEnabled,
                    variablesMap);

                variationsMap.Add(variation.Key, optimizelyVariation);
            }

            return variationsMap;
        }

        /// <summary>
        /// Make map of featureVariable which are associated with given feature experiment 
        /// </summary>
        /// <param name="variableIdMap">Map containing variable ID as key and Object of featureVariable</param>
        /// <param name="featureId">feature Id of featureExperiment</param>
        /// <param name="featureVariableUsages">IEnumerable of features variable usage</param>
        /// <param name="isFeatureEnabled">isFeatureEnabled of variation</param>
        /// <returns>Dictionary | Dictionary of FeatureVariable key and value as FeatureVariable object</returns>
        private IDictionary<string, OptimizelyVariable> MergeFeatureVariables(
           IDictionary<string, FeatureVariable> variableIdMap,
           string featureId,
           IEnumerable<FeatureVariableUsage> featureVariableUsages,
           bool isFeatureEnabled)
        {
            var variablesMap = new Dictionary<string, OptimizelyVariable>();
            
            if (!string.IsNullOrEmpty(featureId))
            {
                variablesMap = featureIdVariablesMap[featureId]?.Select(f => new OptimizelyVariable(f.Id,
                        f.Key,
                        f.Type.ToString().ToLower(),
                        f.DefaultValue)
                ).ToDictionary(k => k.Key, v => v);

                foreach (var featureVariableUsage in featureVariableUsages)
                {
                    var defaultVariable = variableIdMap[featureVariableUsage.Id];
                    var optimizelyVariable = new OptimizelyVariable(featureVariableUsage.Id,
                        defaultVariable.Key,
                        defaultVariable.Type.ToString().ToLower(),
                        isFeatureEnabled ? featureVariableUsage.Value : defaultVariable.DefaultValue);

                    variablesMap[defaultVariable.Key] = optimizelyVariable;
                }
            }

            return variablesMap;
        }

        /// <summary>
        /// Gets Map of all FeatureFlags and associated experiment map inside it
        /// </summary>
        /// <param name="projectConfig">The project config</param>
        /// <param name="experimentsMapById">Dictionary of experiment Id as key and value as experiment object</param>
        /// <returns>Dictionary | Dictionary of FeatureFlag key and value as OptimizelyFeature object</returns>
        private IDictionary<string, OptimizelyFeature> GetFeaturesMap(ProjectConfig projectConfig, IDictionary<string, OptimizelyExperiment> experimentsMapById)
        {
            var FeaturesMap = new Dictionary<string, OptimizelyFeature>();

            foreach (var featureFlag in projectConfig.FeatureFlags)   
            {

                var featureExperimentMap = featureFlag.ExperimentIds.Select(experimentId => experimentsMapById[experimentId])
                    .ToDictionary(experiment => experiment.Key, experiment => experiment);

                var featureVariableMap = featureFlag.Variables.Select(v => (OptimizelyVariable)v).ToDictionary(k => k.Key, v => v) ?? new Dictionary<string, OptimizelyVariable>();

                var experimentRules = featureExperimentMap.Select(exMap => exMap.Value).ToList();
                var rollout = projectConfig.GetRolloutFromId(featureFlag.RolloutId);
                var deliveryRules = GetDeliveryRules(featureFlag.Id, rollout.Experiments, projectConfig);

                var optimizelyFeature = new OptimizelyFeature(featureFlag.Id,
                    featureFlag.Key,
                    experimentRules,
                    deliveryRules,
                    featureExperimentMap,
                    featureVariableMap);

                FeaturesMap.Add(featureFlag.Key, optimizelyFeature);
            }

            return FeaturesMap;
        }


        #region Helper Methods for creating id value maps

        /// <summary>
        /// Gets Map of all FeatureVariable with respect to featureId
        /// </summary>
        /// <param name="projectConfig">The project config</param>
        /// <returns>Dictionary | Dictionary of FeatureFlag key and value as list of all FeatureVariable inside it</returns>
        private IDictionary<string, List<FeatureVariable>> GetFeatureVariablesByIdMap(ProjectConfig projectConfig)
        {
            var featureIdVariablesMap = projectConfig?.FeatureFlags?.ToDictionary(k => k.Id, v => v.Variables);

            return featureIdVariablesMap ?? new Dictionary<string, List<FeatureVariable>>();
        }

        /// <summary>
        /// Gets stringify audiences used in given experiment
        /// </summary>
        /// <param name="experiment">The experiment</param>
        /// <param name="projectConfig">The project config</param>
        /// <returns>string | Audiences used in experiment.</returns>
        private string GetExperimentAudiences(Experiment experiment, ProjectConfig projectConfig)
        {
            if (experiment.AudienceConditionsString == null)
            {
                return "";
            }
            var s = JsonConvert.DeserializeObject<List<object>>(experiment.AudienceConditionsString);
            return GetSerializedAudiences(s, projectConfig.AudienceIdMap);
        }

        readonly string[] AUDIENCE_CONDITIONS = { "and", "or", "not" };

        /// <summary>
        /// Converts list of audience conditions to serialized audiences used in experiment
        /// for examples:
        /// 1. Input: ["or", "1", "2"]
        ///    Output: "\"us\" OR \"female\""
        /// 2. Input: ["not", "1"]
        ///    Output: "NOT \"us\""
        /// 3. Input: ["or", "1"]
        ///    Output: "\"us\""
        /// 4. Input: ["and", ["or", "1", ["and", "2", "3"]], ["and", "11", ["or", "12", "13"]]]
        ///    Output: "(\"us\" OR (\"female\" AND \"adult\")) AND (\"fr\" AND (\"male\" OR \"kid\"))"
        /// </summary>
        /// <param name="audienceConditions">List of audience conditions in experiment</param>
        /// <param name="audienceIdMap">The audience Id map</param>
        /// <returns>string | Serialized audience in which IDs are replaced with audience name.</returns>
        private string GetSerializedAudiences(List<object> audienceConditions, Dictionary<string, Audience> audienceIdMap)
        {
            StringBuilder sAudience = new StringBuilder("");

            if (audienceConditions != null)
            {
                string cond = "";
                foreach (var item in audienceConditions)
                {
                    var subAudience = "";
                    // Checks if item is list of conditions means if it is sub audience
                    if (item is JArray)
                    {
                        subAudience = GetSerializedAudiences(((JArray)item).ToObject<List<object>>(), audienceIdMap);
                        subAudience = "(" + subAudience + ")";
                    }
                    else if (AUDIENCE_CONDITIONS.Contains(item.ToString())) // Checks if item is an audience condition
                    {
                        cond = item.ToString().ToUpper();
                    }
                    else
                    {   // Checks if item is audience id
                        var itemStr = item.ToString();
                        var audienceName = audienceIdMap.ContainsKey(itemStr) ? audienceIdMap[itemStr].Name : itemStr;
                        // if audience condition is "NOT" then add "NOT" at start. Otherwise check if there is already audience id in sAudience then append condition between saudience and item
                        if (!string.IsNullOrEmpty(sAudience.ToString()) || cond.Equals("NOT"))
                        {
                            cond = string.IsNullOrEmpty(cond) ? "OR" : cond;
                            sAudience = string.IsNullOrEmpty(sAudience.ToString()) ? new StringBuilder(cond + " \"" + audienceIdMap[itemStr]?.Name + "\"") :
                                        sAudience.Append(" " + cond + " \"" + audienceName + "\"");
                        }
                        else
                        {
                            sAudience = new StringBuilder("\"" + audienceName + "\"");
                        }
                    }
                    // Checks if sub audience is empty or not
                    if (!string.IsNullOrEmpty(subAudience))
                    {
                        if (!string.IsNullOrEmpty(sAudience.ToString()) || cond == "NOT")
                        {
                            cond = string.IsNullOrEmpty(cond) ? "OR" : cond;
                            sAudience = string.IsNullOrEmpty(sAudience.ToString()) ? new StringBuilder(cond + " " + subAudience) :
                                    sAudience.Append(" " + cond + " " + subAudience);
                        }
                        else
                        {
                            sAudience = sAudience.Append(subAudience);
                        }
                    }
                }
            }
            return sAudience.ToString();
        }

        /// <summary>
        /// Gets list of rollout experiments
        /// </summary>
        /// <param name="featureId">Feature ID</param>
        /// <param name="experiments">Experiments</param>
        /// <param name="projectConfig">Project Config</param>
        /// <returns>List | List of Optimizely rollout experiments.</returns>
        private List<OptimizelyExperiment> GetDeliveryRules(string featureId, IEnumerable<Experiment> experiments,
            ProjectConfig projectConfig)
        {
            if (experiments == null)
            {
                return new List<OptimizelyExperiment>();
            }

            var featureVariableIdMap = GetVariableIdMap(projectConfig);

            var deliveryRules = new List<OptimizelyExperiment>();

            foreach (var experiment in experiments)
            {
                var optimizelyExperiment = new OptimizelyExperiment(
                        id: experiment.Id,
                        key: experiment.Key,
                        audiences: GetExperimentAudiences(experiment, projectConfig),
                        variationsMap: GetVariationsMap(experiment.Variations, featureVariableIdMap, featureId)
                    );
                deliveryRules.Add(optimizelyExperiment);
            }

            return deliveryRules;
        }

        /// <summary>
        /// Gets Map of FeatureVariable with respect to featureVariableId
        /// </summary>
        /// <param name="projectConfig">The project config</param>
        /// <returns>Dictionary | Dictionary of FeatureVariableId as key and value as object of FeatureVariable</returns>
        private IDictionary<string, FeatureVariable> GetVariableIdMap(ProjectConfig projectConfig)
        {
            var featureVariablesIdMap = projectConfig?.FeatureFlags?.SelectMany(f => f.Variables).ToDictionary(k => k.Id, v => v);

            return featureVariablesIdMap ?? new Dictionary<string, FeatureVariable>();
        }

        #endregion

        public OptimizelyConfig GetOptimizelyConfig()
        {
            return OptimizelyConfig;
        }

    }
}
