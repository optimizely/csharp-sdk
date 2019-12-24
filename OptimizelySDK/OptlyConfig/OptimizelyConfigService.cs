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
            var featureVariableIdMap = GetVariableIdMap(projectConfig);
            var experiments = projectConfig?.Experiments?.ToList();
            experiments = projectConfig?.Groups?.SelectMany(g => g.Experiments).Concat(experiments)?.ToList();

            foreach (Experiment experiment in experiments)
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
                var optimizelyExperiment = new OptimizelyExperiment(experiment.Id,
                    experiment.Key,
                    variationsMap);

                experimentsMap.Add(experiment.Key, optimizelyExperiment);
            }
            
            return experimentsMap;
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
            var featureKey = projectConfig.GetExperimentFeatureList(experimentId)?.FirstOrDefault();
            var featureIdVariablesMap = GetFeatureIdVariablesMap(projectConfig);
            var variablesMap = new Dictionary<string, OptimizelyVariable>();

            if (featureKey?.Any() ?? false)
            {
                variablesMap = featureIdVariablesMap[featureKey]?.Select(f => new OptimizelyVariable(f.Id,
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
            var featureIdVariablesMap = projectConfig?.FeatureFlags?.ToDictionary(k => k.Id, v => v.Variables);
            
            return featureIdVariablesMap ?? new Dictionary<string, List<FeatureVariable>>();
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
