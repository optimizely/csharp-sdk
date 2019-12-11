/* 
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

using System.Collections.Generic;
using System.Linq;
using OptimizelySDK.Entity;
using OptimizelySDK.Utils;


namespace OptimizelySDK.OptlyConfig
{
    public class OptimizelyConfigService
    {
        OptimizelyConfig OptimizelyConfig;
        public OptimizelyConfigService(ProjectConfig projectConfig)
        {
            var experimentMap = GetExperimentsMap(projectConfig);
            var featureMap = GetFeaturesMap(projectConfig, experimentMap);
            OptimizelyConfig = new OptimizelyConfig(projectConfig.Revision,
                experimentMap,
                featureMap);
        }

        /// <summary>
        /// Gets Map of all experiments except rollouts
        /// </summary>
        /// <param name="projectConfig">The project config</param>
        /// <returns>Dictionary | Dictionary of experiment key and value as experiment object</returns>
        private IDictionary<string, OptimizelyExperiment> GetExperimentsMap(ProjectConfig projectConfig)
        {
            var experimentsMap = new Dictionary<string, OptimizelyExperiment>();
            var rolloutExperimentIds = GetRolloutExperimentIds(projectConfig.Rollouts);
            var featureVariableIdMap = GetVariableIdMap(projectConfig);

            foreach (Experiment experiment in projectConfig.Experiments)
            {
                if (!rolloutExperimentIds.Contains(experiment.Id))
                {
                    var variationsMap = new Dictionary<string, OptimizelyVariation>();
                    foreach (Variation variation in experiment.Variations)
                    {
                        var variablesMap = MergeFeatureVariables(projectConfig,
                            featureVariableIdMap,
                            experiment.Id,
                            variation.FeatureEnabled ?? false,
                            variation.FeatureVariableUsageInstances);

                        var optimizelyVariation = new OptimizelyVariation(variation.Id,
                            variation.Key,
                            variation.FeatureEnabled,
                            variablesMap);

                        variationsMap.Add(variation.Key, optimizelyVariation);
                    }
                    var optimizelyExperiment = new OptimizelyExperiment(experiment.Id,
                        experiment.Key,
                        variationsMap);

                    experimentsMap.Add(experiment.Key, optimizelyExperiment);
                }
            }

            return experimentsMap;
        }

        /// <summary>
        /// Make map of featureVariable which are associated with given feature experiment 
        /// </summary>
        /// <param name="projectConfig">The project config</param>
        /// <param name="variableIdMap">Map containing variable ID as key and Object of featureVariable</param>
        /// <param name="experimentId">experimentId of featureExperiment</param>
        /// <param name="featureEnabled">featureEnabled of variation</param>
        /// <param name="featureVariableUsageInstances">list of FeatureVariableUsage containing key and value</param>
        /// <returns>Dictionary | Dictionary of FeatureVariable key and value as FeatureVariable object</returns>
        private IDictionary<string, OptimizelyVariable> MergeFeatureVariables(
           ProjectConfig projectConfig,
           IDictionary<string, FeatureVariable> variableIdMap,
           string experimentId,
           bool featureEnabled,
           List<FeatureVariableUsage> featureVariableUsageInstances)
        {
            var variablesMap = new Dictionary<string, OptimizelyVariable>();
            var featureList = projectConfig.GetExperimentFeatureList(experimentId);
            var featureIdVariablesMap = GetFeatureIdVariablesMap(projectConfig);
            if (featureList != null)
            {
                if (featureVariableUsageInstances != null)
                {
                    featureVariableUsageInstances.ForEach(featureVariableUsage =>
                    {
                        var optimizelyVariable = new OptimizelyVariable(featureVariableUsage.Id,
                            variableIdMap[featureVariableUsage.Id].Key,
                            variableIdMap[featureVariableUsage.Id].Type.ToString().ToLower(),
                            featureEnabled ? featureVariableUsage.Value : variableIdMap[featureVariableUsage.Id].DefaultValue);

                        variablesMap.Add(variableIdMap[featureVariableUsage.Id].Key, optimizelyVariable);
                    });
                }

                featureList.ForEach(featureId =>
                {
                    featureIdVariablesMap[featureId].ForEach(featureVariable =>
                    {
                        if (!variablesMap.ContainsKey(featureVariable.Key))
                        {
                            var optimizelyVariable = new OptimizelyVariable(featureVariable.Id,
                                featureVariable.Key,
                                featureVariable.Type.ToString().ToLower(),
                                featureVariable.DefaultValue);
                            
                            variablesMap.Add(featureVariable.Key, optimizelyVariable);
                        }
                    });
                });

            }
            return variablesMap;
        }

        /// <summary>
        /// Gets Map of all FeatureFlags and associated experiment map inside it
        /// </summary>
        /// <param name="projectConfig">The project config</param>
        /// <returns>Dictionary | Dictionary of FeatureFlag key and value as OptimizelyFeature object</returns>
        private IDictionary<string, OptimizelyFeature> GetFeaturesMap(ProjectConfig projectConfig, IDictionary<string, OptimizelyExperiment> experimentsMap)
        {
            var FeaturesMap = new Dictionary<string, OptimizelyFeature>();
            foreach (var featureFlag in projectConfig.FeatureFlags)
            {
                var featureExperimentMap = new Dictionary<string, OptimizelyExperiment>();
                var featureVariableMap = new Dictionary<string, OptimizelyVariable>();
                featureFlag.ExperimentIds.ForEach(exId =>
                {
                    foreach (var expMap in experimentsMap)
                    {
                        if (expMap.Value.Id == exId)
                            featureExperimentMap.Add(expMap.Key, expMap.Value);
                    }
                });

                featureFlag.Variables.ForEach(variable =>
                {
                    var optimizelyVariable = new OptimizelyVariable(variable.Id,
                        variable.Key,
                        variable.Type.ToString().ToLower(),
                        variable.DefaultValue);
                    featureVariableMap.Add(variable.Key, optimizelyVariable);
                });

                var optimizelyFeature = new OptimizelyFeature(featureFlag.Id, featureFlag.Key, featureExperimentMap, featureVariableMap);

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
            var featureIDVariablesMap = new Dictionary<string, List<FeatureVariable>>();
            foreach (var featureFlag in projectConfig.FeatureFlags)
            {
                featureIDVariablesMap.Add(featureFlag.Id, featureFlag.Variables);
            }
            return featureIDVariablesMap;
        }

        /// <summary>
        /// Gets Map of FeatureVariable with respect to featureVariableId
        /// </summary>
        /// <param name="projectConfig">The project config</param>
        /// <returns>Dictionary | Dictionary of FeatureVariableId as key and value as object of FeatureVariable</returns>
        private IDictionary<string, FeatureVariable> GetVariableIdMap(ProjectConfig projectConfig)
        {
            var featureIdMap = new Dictionary<string, FeatureVariable>();
            foreach (FeatureFlag featureFlag in projectConfig.FeatureFlags)
            {
                var featureIdLocalMap = ConfigParser<FeatureVariable>.GenerateMap(entities: featureFlag.Variables, getKey: a => a.Id, clone: true);
                featureIdMap = featureIdMap.Union(featureIdLocalMap).ToDictionary(k => k.Key, v => v.Value);
            }
            return featureIdMap;
        }

        /// <summary>
        /// Gets list of all rollout experiment Ids
        /// </summary>
        /// <param name="Rollouts">Array of rollout experiments</param>
        /// <returns>List | List of rollout experiments</returns>
        private List<string> GetRolloutExperimentIds(Rollout[] Rollouts)
        {
            var rolloutexperimentMap = new List<string>();
            foreach (Rollout rollout in Rollouts)
            {
                rollout.Experiments.ForEach(e =>
                {
                    rolloutexperimentMap.Add(e.Id);
                });
            }
            return rolloutexperimentMap;
        }
        #endregion

        public OptimizelyConfig GetOptimizelyConfig()
        {
            return OptimizelyConfig;
        }

    }
}
