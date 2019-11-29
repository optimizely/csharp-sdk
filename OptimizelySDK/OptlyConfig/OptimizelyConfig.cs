using OptimizelySDK.Entity;
using OptimizelySDK.Utils;
using System.Collections.Generic;
using System.Linq;

namespace OptimizelySDK.OptlyConfig
{
    public class OptimizelyConfig
    {
        public string Revision { get; private set; }
        public string Key { get; private set; }
        public Dictionary<string, OptimizelyExperiment> ExperimentsMap { get; private set; }
        public Dictionary<string, OptimizelyFeature> FeaturesMap { get; private set; }

        // Gets Map of all experiments except rollouts
        private Dictionary<string, OptimizelyExperiment> GetExperimentsMap(ProjectConfig configObj)
        {
            ExperimentsMap = new Dictionary<string, OptimizelyExperiment>();
            var rolloutExperimentIds = GetRolloutExperimentIds(configObj.Rollouts);
            var featureIdMap = GetVariableIdMap(configObj);
            var experimentMap = new Dictionary<string, OptimizelyExperiment>();

            foreach (Experiment ex in configObj.Experiments)
            {
                if (!rolloutExperimentIds.ContainsKey(ex.Id))
                {
                    var variationMap = new Dictionary<string, OptimizelyVariation>();
                    foreach (Variation variation in ex.Variations)
                    {
                        var variablesMap = new Dictionary<string, OptimizelyVariable>();
                        if (variation.FeatureVariableUsageInstances != null)
                        {
                            foreach (FeatureVariableUsage featureVariable in variation.FeatureVariableUsageInstances)
                            {
                                var optimizelyVariable = new OptimizelyVariable();
                                optimizelyVariable.ID = featureVariable.Id;
                                if (variation.FeatureEnabled == true)
                                {
                                    optimizelyVariable.Value = featureVariable.Value;
                                }
                                else
                                {
                                    optimizelyVariable.Value = featureIdMap[featureVariable.Id].DefaultValue;
                                }
                                optimizelyVariable.Key = featureIdMap[featureVariable.Id].Key;
                                optimizelyVariable.Type = FeatureVariable.GetFeatureVariableTypeName(featureIdMap[featureVariable.Id].Type);

                                variablesMap.Add(featureVariable.Id, optimizelyVariable);
                            }
                        }
                        var optimizelyVariation = new OptimizelyVariation();
                        optimizelyVariation.ID = variation.Id;
                        optimizelyVariation.Key = variation.Key;
                        optimizelyVariation.FeatureEnabled = variation.FeatureEnabled;
                        optimizelyVariation.VariablesMap = variablesMap;
                        variationMap.Add(variation.Key, optimizelyVariation);
                    }
                    var optimizelyExperiment = new OptimizelyExperiment();
                    optimizelyExperiment.ID = ex.Id;
                    optimizelyExperiment.Key = ex.Key;
                    optimizelyExperiment.VariationsMap = variationMap;
                    ExperimentsMap.Add(ex.Key, optimizelyExperiment);
                }
            }

            return ExperimentsMap;
        }

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

        private Dictionary<string, bool> GetRolloutExperimentIds(Rollout[] Rollouts)
        {
            // TODO: we can use list over here
            var rolloutexperimentMap = new Dictionary<string, bool>();
            foreach (Rollout rollout in Rollouts)
            {
                rollout.Experiments.ForEach(e => 
                {
                    rolloutexperimentMap.Add(e.Id, true);
                });
            }
            return rolloutexperimentMap;
        }

        private Dictionary<string, OptimizelyFeature> GetFeaturesMap(ProjectConfig configObj)
        {
            FeaturesMap = new Dictionary<string, OptimizelyFeature>();
            foreach (var featureFlag in configObj.FeatureFlags)
            {
                var optimizelyFeature = new OptimizelyFeature();
                optimizelyFeature.ID = featureFlag.Id;
                optimizelyFeature.Key = featureFlag.Key;
                featureFlag.ExperimentIds.ForEach (exId =>
                {
                    if (ExperimentsMap.ContainsKey(exId))
                        optimizelyFeature.ExperimentsMap.Add(ExperimentsMap[exId].Key, ExperimentsMap[exId]);
                });
                featureFlag.Variables.ForEach(variable =>
                {
                    var optimizelyVariable = new OptimizelyVariable();
                    optimizelyVariable.ID = variable.Id;
                    optimizelyVariable.Key = variable.Key;
                    optimizelyVariable.Type = FeatureVariable.GetFeatureVariableTypeName(variable.Type);
                    optimizelyVariable.Value = variable.DefaultValue;
                    optimizelyFeature.VariablesMap.Add(variable.Key, optimizelyVariable);
                });

                FeaturesMap.Add(featureFlag.Key, optimizelyFeature);
            }
            return FeaturesMap;
        }

        public OptimizelyConfig GetOptimizelyConfig(ProjectConfig configObj)
        {
            Revision = configObj.Revision;
            Key = configObj.ProjectId;
            ExperimentsMap = GetExperimentsMap(configObj);
            FeaturesMap = GetFeaturesMap(configObj);
            return this;
        }
    }
}
