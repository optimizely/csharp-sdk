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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OptimizelySDK.Entity;
using OptimizelySDK.Utils;

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
            var experimentMap = GetExperimentsMap(projectConfig);
            var featureMap = GetFeaturesMap(projectConfig, experimentMap);
            OptimizelyConfig = new OptimizelyConfig(projectConfig.Revision,
                projectConfig.SDKKey,
                projectConfig.EnvironmentKey,
                experimentMap,
                featureMap,
                projectConfig.ToDatafile());
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

        private IDictionary<string, OptimizelyVariation> GetVariationsMap(Experiment experiment,
            IDictionary<string, FeatureVariable> featureVariableIdMap,
            ProjectConfig projectConfig)
        {
            var variationsMap = new Dictionary<string, OptimizelyVariation>();
            foreach (Variation variation in experiment.Variations)
            {
                var variablesMap = MergeFeatureVariables(projectConfig,
                    featureVariableIdMap,
                    experiment.Id,
                    variation);

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
        /// <returns>Dictionary | Dictionary of FeatureVariable key and value as FeatureVariable object</returns>
        private IDictionary<string, OptimizelyVariable> MergeFeatureVariables(
           ProjectConfig projectConfig,
           IDictionary<string, FeatureVariable> variableIdMap,
           string experimentId,
           Variation variation)
        {            
            var featureId = projectConfig.GetExperimentFeatureList(experimentId)?.FirstOrDefault();
            var featureIdVariablesMap = GetFeatureIdVariablesMap(projectConfig);
            var variablesMap = new Dictionary<string, OptimizelyVariable>();

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

        private string GetExperimentAudiences(Experiment experiment, ProjectConfig projectConfig)
        {
            var audienceConditions = experiment.AudienceConditions == null ? new List<object>() : (List<object>) experiment.AudienceConditions;
            return GetSerializedAudiences(audienceConditions, projectConfig.AudienceIdMap);
        }

        private List<OptimizelyExperiment> GetDeliveryRules(string rolloutID, ProjectConfig projectConfig)
        {
            var rollout = projectConfig.GetRolloutFromId(rolloutID);
            if (rollout?.Experiments == null || string.IsNullOrEmpty(rolloutID))
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
                        variationsMap: GetVariationsMap(experiment, featureVariableIdMap, projectConfig)
                    );
                deliveryRules.Add(optimizelyExperiment);
            }

            return deliveryRules;
        }


        private string GetSerializedAudiences(List<object> audienceConditions, Dictionary<string, Audience> audienceIdMap) 
        {
            var cond = "";

            var sAudience = "";

            if (audienceConditions != null)
            {
                foreach(var item in audienceConditions) 
                {
                    var subAudience = "";
                    if (item is List<object>)
                    {
                        subAudience = GetSerializedAudiences((List<object>) item, audienceIdMap);
                        subAudience = "(" + subAudience + ")";
                    }
                    else if (int.TryParse(item.ToString(), out int res))
                    {
                        cond = item.ToString().ToUpper();
                    }
                    else
                    {
                        var itemStr = item.ToString();
                        if (string.IsNullOrEmpty(sAudience) || cond.Equals("NOT"))
                        {
                            cond = string.IsNullOrEmpty(cond) ? cond : "OR";
        
                            sAudience = sAudience + " " + cond + " \"" + audienceIdMap[itemStr] + "\"";
                        }
                        else
                        {
                            sAudience = "\"" + audienceIdMap[itemStr] + "\"";
                        }
                    }
                    if (!string.IsNullOrEmpty(subAudience))
                    {
                        if (string.IsNullOrEmpty(sAudience) || cond == "NOT")
                        {
                            cond = string.IsNullOrEmpty(cond) ? cond : "OR";
        
                            sAudience = sAudience + " " + cond + " " + subAudience;
                        }
                        else
                        {
                            sAudience = sAudience + subAudience;
                        }
                    }
                }
            }
            return sAudience;
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
