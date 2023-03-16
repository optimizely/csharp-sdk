---
title: "OptimizelyConfig"
slug: "optimizelyconfig-csharp"
hidden: false
createdAt: "2020-01-17T11:52:50.547Z"
updatedAt: "2020-01-28T21:53:11.290Z"
---
[block:api-header]
{
  "title": "Overview"
}
[/block]
Optimizely Feature Experimentation SDKs open a well-defined set of public APIs, hiding all implementation details. However, some clients may need access to project configuration data within the "datafile". 

In this document, we extend our public APIs to define data models and access methods, which clients can use to access project configuration data. 

[block:api-header]
{
  "title": "OptimizelyConfig API"
}
[/block]

A public configuration data model (OptimizelyConfig) is defined below as a structured format of static Optimizely Project data.

OptimizelyConfig can be accessed from OptimizelyClient (top-level) with this public API call:
[block:code]
{
  "codes": [
    {
      "code": "public OptimizelyConfig GetOptimizelyConfig()",
      "language": "csharp"
    }
  ]
}
[/block]
`GetOptimizelyConfig` returns an `OptimizelyConfig` instance which include a datafile revision number, all experiments, and feature flags mapped by their key values.
[block:callout]
{
  "type": "info",
  "title": "Note",
  "body": "When the SDK datafile is updated (the client can add a notification listener for `OPTIMIZELY_CONFIG_UPDATE` to get notified), the client is expected to call the method to get the updated OptimizelyConfig data. See examples below."
}
[/block]

[block:code]
{
  "codes": [
    {
      "code": "// OptimizelyConfig is class describing the current project configuration data being used by this SDK instance.\n public class OptimizelyConfig\n {\n   public string Revision { get; private set; }\n   public IDictionary<string, OptimizelyExperiment> ExperimentsMap { get; private set; }\n   public IDictionary<string, OptimizelyFeature> FeaturesMap { get; private set; }        \n }\n\n// Entity.IdKeyEntity is an abstract class used for inheritance in OptimizelyExperiment, OptimizelyFeature, OptimizelyVariation and OptimizelyVariable classes.\npublic abstract class IdKeyEntity : Entity, IEquatable<object>\n{\n  public string Id { get; set; }\n  public string Key { get; set; }\n}\n\n// OptimizelyFeature is a class describing a feature and inherited from Entity.IdKeyEntity.\npublic class OptimizelyFeature : Entity.IdKeyEntity\n{\n  public IDictionary<string, OptimizelyExperiment> ExperimentsMap { get; private set; }\n  public IDictionary<string, OptimizelyVariable> VariablesMap { get; private set; }\n}\n\n\n// OptimizelyExperiment is a class describing a feature test or an A/B test and inherited from Entity.IdKeyEntity.\npublic class OptimizelyExperiment : Entity.IdKeyEntity\n{\n  public IDictionary<string, OptimizelyVariation> VariationsMap { get; private set; }\n}\n\n\n// OptimizelyVariation is a class describing a variation in a feature test or A/B test and inherited from Entity.IdKeyEntity.\npublic class OptimizelyVariation : Entity.IdKeyEntity\n{\n  [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]\n  public bool? FeatureEnabled { get; private set; }\n  public IDictionary<string, OptimizelyVariable> VariablesMap { get; private set; }\n}\n\n\n// OptimizelyVariable is a class describing a feature variable and inherited from Entity.IdKeyEntity.\npublic class OptimizelyVariable : Entity.IdKeyEntity\n{\n  public string Type { get; private set; }\n  public string Value { get; private set; }\n}",
      "language": "csharp"
    }
  ]
}
[/block]

[block:api-header]
{
  "title": "Examples"
}
[/block]
OptimizelyConfig can be accessed from OptimizelyClient (top-level) like this:

[block:code]
{
  "codes": [
    {
      "code": "var optimizelyConfig = optimizely.GetOptimizelyConfig();\n\n// all experiment keys\nvar experimentKeys = optimizelyConfig.ExperimentsMap.Keys;\nforeach(var experimentKey in experimentKeys) {\n  // use experiment key data here.\n}\n\n// all experiments\nvar experiments = optimizelyConfig.ExperimentsMap.Values;\nforeach(var experiment in experiments) {\n  // all variations\n  var variations = experiment.VariationsMap.Values;\n  foreach(var variation in variations) {\n    var variables = variation.VariablesMap.Values;\n    foreach(var variable in variables) {\n      // use variable data here.\n    }\n  }\n}\n\n\n\n// all features\nvar features = optimizelyConfig.FeaturesMap.Values;\nforeach(var feature in features) {\n  var experiments = feature.ExperimentsMap.Values;\n  foreach(var experiment in experiments) {\n    // use experiment data here.\n  }\n}\n\n\n// listen to OPTIMIZELY_CONFIG_UPDATE to get updated data\nNotificationCenter.OptimizelyConfigUpdateCallback configUpdateListener = () => {\n                    var optimizelyConfig = optimizely.GetOptimizelyConfig();\n                };\n                optimizely.NotificationCenter.AddNotification(NotificationCenter.NotificationType.OptimizelyConfigUpdate, configUpdateListener);\n",
      "language": "csharp"
    }
  ]
}
[/block]