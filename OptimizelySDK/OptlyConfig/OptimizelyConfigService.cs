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
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.Utils;


namespace OptimizelySDK.OptlyConfig
{
    public class OptimizelyConfigService : OptimizelyConfig
    {
        /// <summary>
        /// Gets Map of all experiments except rollouts
        /// </summary>
        /// <param name="configObj">The project config</param>
        /// <returns>Dictionary | Dictionary of experiment key and value as experiment object</returns>
        private Dictionary<string, OptimizelyExperiment> GetExperimentsMap(ProjectConfig configObj)
        {
            var ExperimentsMap = new Dictionary<string, OptimizelyExperiment>();
            var rolloutExperimentIds = GetRolloutExperimentIds(configObj.Rollouts);
            var featureIdMap = GetVariableIdMap(configObj);

            foreach (Experiment ex in configObj.Experiments)
            {
                if (!rolloutExperimentIds.Contains(ex.Id))
                {
                    var variationMap = new Dictionary<string, OptimizelyVariation>();
                    foreach (Variation variation in ex.Variations)
                    {
                        var variablesMap = MergeFeatureVariables((DatafileProjectConfig)configObj,
                            featureIdMap,
                            ex.Id,
                            variation.FeatureEnabled ?? false,
                            variation.FeatureVariableUsageInstances);

                        var optimizelyVariation = new OptimizelyVariation();
                        optimizelyVariation.Id = variation.Id;
                        optimizelyVariation.Key = variation.Key;
                        optimizelyVariation.FeatureEnabled = variation.FeatureEnabled;
                        optimizelyVariation.VariablesMap = variablesMap;
                        variationMap.Add(variation.Key, optimizelyVariation);
                    }
                    var optimizelyExperiment = new OptimizelyExperiment();
                    optimizelyExperiment.Id = ex.Id;
                    optimizelyExperiment.Key = ex.Key;
                    optimizelyExperiment.VariationsMap = variationMap;
                    ExperimentsMap.Add(ex.Key, optimizelyExperiment);
                }
            }

            return ExperimentsMap;
        }

        /// <summary>
        /// Make map of featureVariable which are associated with given feature experiment 
        /// </summary>
        /// <param name="configObj">The Datafile project config</param>
        /// <param name="variableIdMap">Map containing variable ID as key and Object of featureVariable</param>
        /// <param name="experimentId">experimentId of featureExperiment</param>
        /// <param name="featureEnabled">featureEnabled of variation</param>
        /// <param name="featureVariableUsageInstances">list of FeatureVariableUsage containing key and value</param>
        /// <returns>Dictionary | Dictionary of FeatureVariable key and value as FeatureVariable object</returns>
        private Dictionary<string, OptimizelyVariable> MergeFeatureVariables(
           DatafileProjectConfig configObj,
           Dictionary<string, FeatureVariable> variableIdMap,
           string experimentId,
           bool featureEnabled,
           List<FeatureVariableUsage> featureVariableUsageInstances)
        {
            var variablesMap = new Dictionary<string, OptimizelyVariable>();
            var featureList = configObj.GetExperimentFeatureList(experimentId);
            var featureIdVariablesMap = GetFeatureIdVariablesMap(configObj);
            if (featureList != null)
            {
                if (featureVariableUsageInstances != null)
                {
                    featureVariableUsageInstances.ForEach(featureVariableUsage =>
                    {
                        var optimizelyVariable = new OptimizelyVariable();
                        optimizelyVariable.Id = featureVariableUsage.Id;
                        optimizelyVariable.Value = featureEnabled ? featureVariableUsage.Value : variableIdMap[featureVariableUsage.Id].DefaultValue;
                        optimizelyVariable.Key = variableIdMap[featureVariableUsage.Id].Key;
                        optimizelyVariable.Type = variableIdMap[featureVariableUsage.Id].Type.ToString().ToLower();

                        variablesMap.Add(variableIdMap[featureVariableUsage.Id].Key, optimizelyVariable);
                    });
                }

                featureList.ForEach(featureId =>
                {
                    featureIdVariablesMap[featureId].ForEach(featureVariable =>
                    {
                        if (!variablesMap.ContainsKey(featureVariable.Key))
                        {
                            var optimizelyVariable = new OptimizelyVariable();
                            optimizelyVariable.Id = featureVariable.Id;
                            optimizelyVariable.Value = featureVariable.DefaultValue;
                            optimizelyVariable.Key = featureVariable.Key;
                            optimizelyVariable.Type = featureVariable.Type.ToString().ToLower();

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
        /// <param name="configObj">The project config</param>
        /// <returns>Dictionary | Dictionary of FeatureFlag key and value as OptimizelyFeature object</returns>
        private Dictionary<string, OptimizelyFeature> GetFeaturesMap(ProjectConfig configObj, Dictionary<string, OptimizelyExperiment> experimentsMap)
        {
            var FeaturesMap = new Dictionary<string, OptimizelyFeature>();
            foreach (var featureFlag in configObj.FeatureFlags)
            {
                var optimizelyFeature = new OptimizelyFeature();
                optimizelyFeature.Id = featureFlag.Id;
                optimizelyFeature.Key = featureFlag.Key;
                featureFlag.ExperimentIds.ForEach(exId =>
                {
                    foreach (var expMap in experimentsMap)
                    {
                        if (expMap.Value.Id == exId)
                            optimizelyFeature.ExperimentsMap.Add(expMap.Key, expMap.Value);
                    }
                });
                featureFlag.Variables.ForEach(variable =>
                {
                    var optimizelyVariable = new OptimizelyVariable();
                    optimizelyVariable.Id = variable.Id;
                    optimizelyVariable.Key = variable.Key;
                    optimizelyVariable.Type = variable.Type.ToString().ToLower();
                    optimizelyVariable.Value = variable.DefaultValue;
                    optimizelyFeature.VariablesMap.Add(variable.Key, optimizelyVariable);
                });

                FeaturesMap.Add(featureFlag.Key, optimizelyFeature);
            }
            return FeaturesMap;
        }


        #region Helper Methods for creating id value maps

        /// <summary>
        /// Gets Map of all FeatureVariable with respect to featureId
        /// </summary>
        /// <param name="configObj">The project config</param>
        /// <returns>Dictionary | Dictionary of FeatureFlag key and value as list of all FeatureVariable inside it</returns>
        private Dictionary<string, List<FeatureVariable>> GetFeatureIdVariablesMap(ProjectConfig configObj)
        {
            var featureIDVariablesMap = new Dictionary<string, List<FeatureVariable>>();
            foreach (var featureFlag in configObj.FeatureFlags)
            {
                featureIDVariablesMap.Add(featureFlag.Id, featureFlag.Variables);
            }
            return featureIDVariablesMap;
        }

        /// <summary>
        /// Gets Map of FeatureVariable with respect to featureVariableId
        /// </summary>
        /// <param name="configObj">The project config</param>
        /// <returns>Dictionary | Dictionary of FeatureVariableId as key and value as object of FeatureVariable</returns>
        private Dictionary<string, FeatureVariable> GetVariableIdMap(ProjectConfig configObj)
        {
            var featureIdMap = new Dictionary<string, FeatureVariable>();
            foreach (FeatureFlag featureFlag in configObj.FeatureFlags)
            {
                var featureIdLocalMap = ConfigParser<FeatureVariable>.GenerateMap(entities: featureFlag.Variables, getKey: a => a.Id, clone: true);
                featureIdMap = featureIdMap.Union(featureIdLocalMap).ToDictionary(k => k.Key, v => v.Value);
            }
            return featureIdMap;
        }

        /// <summary>
        /// Gets list of all rollout experiment Ids
        /// </summary>
        /// <param name="configObj">The project config</param>
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

        public OptimizelyConfig GetOptimizelyConfig(ProjectConfig configObj)
        {
            Revision = configObj.Revision;
            ExperimentsMap = GetExperimentsMap(configObj);
            FeaturesMap = GetFeaturesMap(configObj, ExperimentsMap);
            return this;
        }

    }
}
