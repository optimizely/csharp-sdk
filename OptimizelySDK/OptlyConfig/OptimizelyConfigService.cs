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

        public OptimizelyConfigService(ProjectConfig projectConfig)
        {
            if (projectConfig == null)
            {
                return;
            }
            var attributes = GetAttributes(projectConfig);
            var audiences = GetAudiences(projectConfig);
            var experimentMap = GetExperimentsMap(projectConfig);
            var featureMap = GetFeaturesMap(projectConfig, experimentMap);
            var events = GetEvents(projectConfig);

            OptimizelyConfig = new OptimizelyConfig(projectConfig.Revision,
                projectConfig.SDKKey,
                projectConfig.EnvironmentKey,
                attributes,
                audiences,
                events,
                experimentMap,
                featureMap,
                projectConfig.ToDatafile());
        }

        private Entity.Event[] GetEvents(ProjectConfig projectConfig)
        {
            return projectConfig.Events ?? new Entity.Event[0];
        }

        private OptimizelyAudience[] GetAudiences(ProjectConfig projectConfig)
        {
            var audiencesArr = Array.FindAll(projectConfig.Audiences, aud => !aud.Id.Equals("$opt_dummy_audience"));
            audiencesArr.Concat(projectConfig.TypedAudiences);
            return audiencesArr.Select(aud => new OptimizelyAudience(aud.Id, aud.Name, aud.Conditions)).ToArray<OptimizelyAudience>();
        }

        private Entity.Attribute[] GetAttributes(ProjectConfig projectConfig)
        {
            return projectConfig.Attributes;
        }

        /// <summary>
        /// Gets Map of all experiments except rollouts
        /// </summary>
        /// <param name="projectConfig">The project config</param>
        /// <returns>Dictionary | Dictionary of experiment key and value as experiment object</returns>
        private IDictionary<string, OptimizelyExperiment> GetExperimentsMap(ProjectConfig projectConfig)
        {
            var experimentsMap = new Dictionary<string, OptimizelyExperiment>();
            var featureVariableIdMap = GetVariableIdMap(projectConfig);
            var experiments = projectConfig?.Experiments?.ToList();
            experiments = projectConfig?.Groups?.SelectMany(g => g.Experiments).Concat(experiments)?.ToList();

            foreach (Experiment experiment in experiments)
            {
                var variationsMap = GetVariationsMap(experiment, featureVariableIdMap, projectConfig);

                var optimizelyExperiment = new OptimizelyExperiment(experiment.Id,
                    experiment.Key,
                    GetExperimentAudiences(experiment, projectConfig),
                    variationsMap);

                experimentsMap.Add(experiment.Key, optimizelyExperiment);
            }

            return experimentsMap;
        }

        /// <summary>
        /// Gets Map of all experiment variations and variables including rollouts
        /// </summary>
        /// <param name="experiment">Experiment</param>
        /// <param name="featureVariableIdMap">The map of feature variables and id</param>
        /// <param name="projectConfig">The project config</param>
        /// <param name="rolloutId">Rollout Id if the feature Id is null then use rollout id to get feature Id</param>
        /// <returns>Dictionary | Dictionary of experiment key and value as experiment object</returns>
        private IDictionary<string, OptimizelyVariation> GetVariationsMap(Experiment experiment,
            IDictionary<string, FeatureVariable> featureVariableIdMap,
            ProjectConfig projectConfig,
            string rolloutId = null)
        {
            var variationsMap = new Dictionary<string, OptimizelyVariation>();
            foreach (Variation variation in experiment.Variations)
            {
                var variablesMap = MergeFeatureVariables(projectConfig,
                    featureVariableIdMap,
                    experiment.Id,
                    variation,
                    rolloutId);

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
        /// <param name="projectConfig">The project config</param>
        /// <param name="variableIdMap">Map containing variable ID as key and Object of featureVariable</param>
        /// <param name="experimentId">experimentId of featureExperiment</param>
        /// <param name="variation">variation</param>
        /// <param name="rolloutId">rollout id to get feature id</param>
        /// <returns>Dictionary | Dictionary of FeatureVariable key and value as FeatureVariable object</returns>
        private IDictionary<string, OptimizelyVariable> MergeFeatureVariables(
           ProjectConfig projectConfig,
           IDictionary<string, FeatureVariable> variableIdMap,
           string experimentId,
           Variation variation,
           string rolloutId = null)
        {
            var featureId = projectConfig.GetExperimentFeatureList(experimentId)?.FirstOrDefault();
            var featureIdVariablesMap = GetFeatureIdVariablesMap(projectConfig);
            var variablesMap = new Dictionary<string, OptimizelyVariable>();
            string featureIdRollout = null;
            if (rolloutId != null)
            {
                featureIdRollout = projectConfig.FeatureFlags.Where(feat => feat.RolloutId == rolloutId).FirstOrDefault()?.Id;
            }

            featureId = featureId ?? featureIdRollout;
            
            if (featureId?.Any() ?? false)
            {
                variablesMap = featureIdVariablesMap[featureId]?.Select(f => new OptimizelyVariable(f.Id,
                        f.Key,
                        f.Type.ToString().ToLower(),
                        f.DefaultValue)
                ).ToDictionary(k => k.Key, v => v);

                foreach (var featureVariableUsage in variation.FeatureVariableUsageInstances)
                {
                    var defaultVariable = variableIdMap[featureVariableUsage.Id];
                    var optimizelyVariable = new OptimizelyVariable(featureVariableUsage.Id,
                        defaultVariable.Key,
                        defaultVariable.Type.ToString().ToLower(),
                        variation.IsFeatureEnabled ? featureVariableUsage.Value : defaultVariable.DefaultValue);

                    variablesMap[defaultVariable.Key] = optimizelyVariable;
                }
            }

            return variablesMap;
        }

        /// <summary>
        /// Gets Map of all FeatureFlags and associated experiment map inside it
        /// </summary>
        /// <param name="projectConfig">The project config</param>
        /// <param name="experimentsMap">Dictionary of experiment key and value as experiment object</param>
        /// <returns>Dictionary | Dictionary of FeatureFlag key and value as OptimizelyFeature object</returns>
        private IDictionary<string, OptimizelyFeature> GetFeaturesMap(ProjectConfig projectConfig, IDictionary<string, OptimizelyExperiment> experimentsMap)
        {
            var FeaturesMap = new Dictionary<string, OptimizelyFeature>();

            foreach (var featureFlag in projectConfig.FeatureFlags)
            {
                var featureExperimentMap = experimentsMap.Where(expMap => featureFlag.ExperimentIds.Contains(expMap.Value.Id)).ToDictionary(k => k.Key, v => v.Value);

                var featureVariableMap = featureFlag.Variables.Select(v => (OptimizelyVariable)v).ToDictionary(k => k.Key, v => v) ?? new Dictionary<string, OptimizelyVariable>();

                var experimentRules = featureExperimentMap.Select(exMap => exMap.Value).ToList();

                var optimizelyFeature = new OptimizelyFeature(featureFlag.Id,
                    featureFlag.Key,
                    experimentRules,
                    GetDeliveryRules(featureFlag.RolloutId, projectConfig),
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
        private IDictionary<string, List<FeatureVariable>> GetFeatureIdVariablesMap(ProjectConfig projectConfig)
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
                var audConditions = new string[] { "and", "or", "not" };
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
                    else if (audConditions.Contains(item.ToString())) // Checks if item is an audience condition
                    {
                        cond = item.ToString().ToUpper();
                    }
                    else
                    {   // Checks if item is audience id
                        var itemStr = item.ToString();
                        // if audience condition is "NOT" then add "NOT" at start. Otherwise check if there is already audience id in sAudience then append condition between saudience and item
                        if (!string.IsNullOrEmpty(sAudience.ToString()) || cond.Equals("NOT"))
                        {
                            cond = string.IsNullOrEmpty(cond) ? cond : "OR";

                            sAudience = sAudience.Append(" " + cond + " \"" + audienceIdMap[itemStr]?.Name + "\"");
                        }
                        else
                        {
                            sAudience = new StringBuilder("\"" + audienceIdMap[itemStr]?.Name + "\"");
                        }
                    }
                    // Checks if sub audience is empty or not
                    if (!string.IsNullOrEmpty(subAudience))
                    {
                        if (!string.IsNullOrEmpty(sAudience.ToString()) || cond == "NOT")
                        {
                            cond = !string.IsNullOrEmpty(cond) ? cond : "OR";

                            sAudience = sAudience.Append(" " + cond + " " + subAudience);
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
        /// <param name="rolloutID">Rollout ID</param>
        /// <param name="projectConfig">Project Config</param>
        /// <returns>List | List of Optimizely rollout experiments.</returns>
        private List<OptimizelyExperiment> GetDeliveryRules(string rolloutID, ProjectConfig projectConfig)
        {
            var rollout = projectConfig.GetRolloutFromId(rolloutID);
            if (rollout?.Experiments == null)
            {
                return new List<OptimizelyExperiment>();
            }

            var featureVariableIdMap = GetVariableIdMap(projectConfig);

            var deliveryRules = new List<OptimizelyExperiment>();

            foreach (var experiment in rollout?.Experiments)
            {
                var optimizelyExperiment = new OptimizelyExperiment(
                        id: experiment.Id,
                        key: experiment.Key,
                        audiences: GetExperimentAudiences(experiment, projectConfig),
                        variationsMap: GetVariationsMap(experiment, featureVariableIdMap, projectConfig, rolloutId: rollout.Id)
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
