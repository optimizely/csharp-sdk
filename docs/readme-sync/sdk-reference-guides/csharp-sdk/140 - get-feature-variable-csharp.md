---
title: "Get Feature Variable"
slug: "get-feature-variable-csharp"
hidden: false
createdAt: "2019-09-12T13:51:45.265Z"
updatedAt: "2019-09-16T23:48:13.270Z"
---

Evaluates the specified feature variable of a specific variable type and returns its value.

This method is used to evaluate and return a feature variable. Multiple versions of this method are available and are named according to the data type they return:
  * [Boolean](#section-boolean)
  * [Double](#section-double)
  * [Integer](#section-integer)
  * [String](#section-string)

This method takes into account the user `attributes` passed in, to determine if the user is part of the audience that qualifies for the experiment.

### Boolean

Returns the value of the specified Boolean variable.

```csharp
public bool? GetFeatureVariableBoolean(string featureKey, string variableKey, string userId, UserAttributes userAttributes = null)
```

### Double

Returns the value of the specified double variable.

```csharp
public double? GetFeatureVariableDouble(string featureKey, string variableKey, string userId, UserAttributes userAttributes = null)
```

### Integer

Returns the value of the specified integer variable.

```csharp
public int? GetFeatureVariableInteger(string featureKey, string variableKey, string userId, UserAttributes userAttributes = null)
```

### String

Returns the value of the specified string variable.

```csharp
public string GetFeatureVariableString(string featureKey, string variableKey, string userId, UserAttributes userAttributes = null)
```
## Version

SDK v3.0, v3.1

## Description

Each of the Get Feature Variable methods follows the same logic as [Is Feature Enabled](doc:is-feature-enabled-csharp):
1. Evaluate any feature tests running for a user.
2. Check the default configuration on a rollout.

The default value is returned if neither of these are applicable for the specified user, or if the user is in a variation where the feature is disabled.

> **Important**
> Unlike [Is Feature Enabled](doc:is-feature-enabled-csharp), the Get Feature Variable methods do not trigger an impression event. This means that if you're running a feature test, events won't be counted until you call Is Feature Enabled. If you don't call Is Feature Enabled, you won't see any visitors on your results page.

## Parameters

Required and optional parameters in C# are listed below.

| Parameter        | Type   | Description                                                                                              |
|------------------|--------|----------------------------------------------------------------------------------------------------------|
| **featureKey**<br>*required*       | string | The feature key is defined from the Features dashboard.                                                |
| **variableKey**<br>*required*      | string | The key that identifies the feature variable.                                                          |
| **userId**<br>*required*          | string | The user ID string uniquely identifies the participant in the experiment.                             |
| **userAttributes**<br>*required*  | map    | A map of custom key-value string pairs specifying attributes for the user that are used for audience targeting and results segmentation. Non-string values are only supported in the 3.0 SDK and above. |
## Returns

Feature variable value or `null`

## Example

```csharp
using OptimizelySDK.Entity;

var attributes = new UserAttributes {
  { "device", "iPhone" },
  { "lifetime", 24738388 },
  { "is_logged_in", true },
};

var featureVariableValue = optimizelyClient.GetFeatureVariableDouble("my_feature_key", "double_variable_key", "user_123", attributes);
```
## Side effects

In SDKs v3.1 and later: Invokes the `DECISION` [notification listener](doc:set-up-notification-listener-csharp) if this listener is enabled.

## Source files

The language/platform source files containing the implementation for C# is [Optimizely.cs](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Optimizely.cs).
