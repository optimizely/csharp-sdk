## Is Feature Enabled

Determines whether a feature is enabled for a given user. The purpose of this method is to allow you to separate the process of developing and deploying features from the decision to turn on a feature. Build your feature and deploy it to your application behind this flag, then turn the feature on or off for specific users.

### Version

SDK v3.0

### Description

This method traverses the client's datafile and checks the feature flag for the feature key that you specify.
1. Analyzes the user's attributes.
2. Hashes the userID.

The method then evaluates the feature rollout for a user. The method checks whether the rollout is enabled, whether the user qualifies for the audience targeting, and then randomly assigns either `on` or `off` based on the appropriate traffic allocation. If the feature rollout is on for a qualified user, the method returns `true`.

### Parameters

The table below lists the required and optional parameters in C#.

| Parameter       | Type       | Description                                                                                                 |
|-----------------|------------|-------------------------------------------------------------------------------------------------------------|
| **featureKey**  | string     | _required_ - The key of the feature to check. The feature key is defined from the Features dashboard.    |
| **userId**      | string     | _required_ - The ID of the user to check.                                                                  |
| **userAttributes** | map        | _optional_ - A map of custom key-value string pairs specifying attributes for the user that are used for audience targeting. Non-string values are only supported in the 3.0 SDK and above. |


## Returns

True if the feature is enabled. Otherwise, false or null.

## Examples

This section shows a simple example of how you can use the `IsFeatureEnabled` method.

```csharp
// Evaluate a feature flag and a variable
bool enabled = optimizelyClient.IsFeatureEnabled("price_filter", userId);

int? min_price = optimizelyClient.GetFeatureVariableInteger("price_filter", variableKey: "min_price", userId: userId);
```

## Source files

The language/platform source files containing the implementation for C# is [Optimizely.cs](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Optimizely.cs).
