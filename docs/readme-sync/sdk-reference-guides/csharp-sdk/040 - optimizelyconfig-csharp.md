---
title: "OptimizelyConfig"
slug: "optimizelyconfig-csharp"
hidden: false
createdAt: "2020-01-17T11:52:50.547Z"
updatedAt: "2020-01-28T21:53:11.290Z"
---

## Overview

Optimizely Feature Experimentation SDKs open a well-defined set of public APIs, hiding all implementation details. However, some clients may need access to project configuration data within the "datafile". 

In this document, we extend our public APIs to define data models and access methods, which clients can use to access project configuration data. 

## OptimizelyConfig API

A public configuration data model (OptimizelyConfig) is defined below as a structured format of static Optimizely Project data.

OptimizelyConfig can be accessed from OptimizelyClient (top-level) with this public API call:

```csharp
public OptimizelyConfig GetOptimizelyConfig()
```
`GetOptimizelyConfig` returns an `OptimizelyConfig` instance which includes a datafile revision number, all experiments, and feature flags mapped by their key values.

> **Note:** When the SDK datafile is updated (the client can add a notification listener for `OPTIMIZELY_CONFIG_UPDATE` to get notified), the client is expected to call the method to get the updated OptimizelyConfig data. See examples below.

```csharp
// OptimizelyConfig is a class describing the current project configuration data being used by this SDK instance.
public class OptimizelyConfig
{
   public string Revision { get; private set; }
   public IDictionary<string, OptimizelyExperiment> ExperimentsMap { get; private set; }
   public IDictionary<string, OptimizelyFeature> FeaturesMap { get; private set; }       
}

// Entity.IdKeyEntity is an abstract class used for inheritance in OptimizelyExperiment, OptimizelyFeature, OptimizelyVariation, and OptimizelyVariable classes.
public abstract class IdKeyEntity : Entity, IEquatable<object>
{
  public string Id { get; set; }
  public string Key { get; set; }
}

// OptimizelyFeature is a class describing a feature and inherited from Entity.IdKeyEntity.
public class OptimizelyFeature : Entity.IdKeyEntity
{
  public IDictionary<string, OptimizelyExperiment> ExperimentsMap { get; private set; }
  public IDictionary<string, OptimizelyVariable> VariablesMap { get; private set; }
}

// OptimizelyExperiment is a class describing a feature test or an A/B test and inherited from Entity.IdKeyEntity.
public class OptimizelyExperiment : Entity.IdKeyEntity
{
  public IDictionary<string, OptimizelyVariation> VariationsMap { get; private set; }
}

// OptimizelyVariation is a class describing a variation in a feature test or A/B test and inherited from Entity.IdKeyEntity.
public class OptimizelyVariation : Entity.IdKeyEntity
{
  [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
  public bool? FeatureEnabled { get; private set; }
  public IDictionary<string, OptimizelyVariable> VariablesMap { get; private set; }
}

// OptimizelyVariable is a class describing a feature variable and inherited from Entity.IdKeyEntity.
public class OptimizelyVariable : Entity.IdKeyEntity
{
  public string Type { get; private set; }
  public string Value { get; private set; }
}
```

## Examples

You can access the `OptimizelyConfig` from the `OptimizelyClient` (top-level) as shown below:

```csharp
var optimizelyConfig = optimizely.GetOptimizelyConfig();

// All experiment keys
var experimentKeys = optimizelyConfig.ExperimentsMap.Keys;
foreach(var experimentKey in experimentKeys) {
  // Use experiment key data here.
}

// All experiments
var experiments = optimizelyConfig.ExperimentsMap.Values;
foreach(var experiment in experiments) {
  // All variations
  var variations = experiment.VariationsMap.Values;
  foreach(var variation in variations) {
    var variables = variation.VariablesMap.Values;
    foreach(var variable in variables) {
      // Use variable data here.
    }
  }
}

// All features
var features = optimizelyConfig.FeaturesMap.Values;
foreach(var feature in features) {
  var experiments = feature.ExperimentsMap.Values;
  foreach(var experiment in experiments) {
    // Use experiment data here.
  }
}

// Listen to OPTIMIZELY_CONFIG_UPDATE to get updated data
NotificationCenter.OptimizelyConfigUpdateCallback configUpdateListener = () => {
    var optimizelyConfig = optimizely.GetOptimizelyConfig();
};
optimizely.NotificationCenter.AddNotification(NotificationCenter.NotificationType.OptimizelyConfigUpdate, configUpdateListener);
```