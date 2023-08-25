---
title: "Get Enabled Features"
slug: "get-enabled-features-csharp"
hidden: false
createdAt: "2019-09-12T13:53:46.225Z"
updatedAt: "2019-09-12T20:33:07.026Z"
---

Retrieves a list of features that are enabled for the user. Invoking this method is equivalent to running [Is Feature Enabled](doc:is-feature-enabled-csharp) for each feature in the datafile sequentially.

This method takes into account the user `attributes` passed in, to determine if the user is part of the audience that qualifies for the experiment.

## Version

SDK v3.0

## Description
This method iterates through all feature flags and for each feature flag invokes [Is Feature Enabled](doc:is-feature-enabled-csharp). If a feature is enabled, this method adds the feature’s key to the return list.
## Parameters

The table below lists the required and optional parameters in C#.

| Parameter        | Type   | Description                                                                                              |
|------------------|--------|----------------------------------------------------------------------------------------------------------|
| **userId**<br>*required*         | string | The ID of the user to check.                                                                            |
| **userAttributes**<br>*optional* | map    | A map of custom key-value string pairs specifying attributes for the user that are used for audience targeting. Non-string values are only supported in the 3.0 SDK and above. |

## Returns

A list of the feature keys that are enabled for the user, or an empty list if no features could be found for the specified user.

## Examples

This section shows a simple example of how you can use the method.

```csharp
var actualFeaturesList = OptimizelyMock.Object.GetEnabledFeatures(TestUserId, userAttributes);

```
## Source files

The language/platform source files containing the implementation for C# is [Optimizely.cs](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Optimizely.cs).
