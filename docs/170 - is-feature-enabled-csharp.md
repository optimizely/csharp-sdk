---
title: "Is Feature Enabled"
slug: "is-feature-enabled-csharp"
hidden: false
createdAt: "2019-09-12T13:51:59.213Z"
updatedAt: "2019-09-12T20:36:35.281Z"
---
Determines whether a feature is enabled for a given user. The purpose of this method is to allow you to separate the process of developing and deploying features from the decision to turn on a feature. Build your feature and deploy it to your application behind this flag, then turn the feature on or off for specific users.
[block:api-header]
{
  "title": "Version"
}
[/block]
SDK v3.0
[block:api-header]
{
  "title": "Description"
}
[/block]
This method traverses the client's datafile and checks the feature flag for the feature key that you specify.
1. Analyzes the user's attributes.
2. Hashes the userID.

The method then evaluates the feature rollout for a user. The method checks whether the rollout is enabled, whether the user qualifies for the audience targeting, and then randomly assigns either `on` or `off` based on the appropriate traffic allocation. If the feature rollout is on for a qualified user, the method returns `true`. 
[block:api-header]
{
  "title": "Parameters"
}
[/block]
The table below lists the required and optional parameters in C#.
[block:parameters]
{
  "data": {
    "h-0": "Parameter",
    "h-1": "Type",
    "0-0": "**featureKey**\n_required_",
    "0-1": "string",
    "1-0": "**userId**\n_required_",
    "1-1": "string",
    "2-0": "**userAttributes**\n_optional_",
    "2-1": "map",
    "h-2": "Description",
    "0-2": "The key of the feature to check. The feature key is defined from the Features dashboard.",
    "1-2": "The ID of the user to check.",
    "2-2": "A map of custom key-value string pairs specifying attributes for the user that are used for audience targeting. Non-string values are only supported in the 3.0 SDK and above."
  },
  "cols": 3,
  "rows": 3
}
[/block]

[block:api-header]
{
  "title": "Returns"
}
[/block]
True if feature is enabled. Otherwise, false or null.
[block:api-header]
{
  "title": "Examples"
}
[/block]
This section shows a simple example of how you can use the `IsFeatureEnabled` method.
[block:code]
{
  "codes": [
    {
      "code": "// Evaluate a feature flag and a variable\nbool enabled = optimizelyClient.IsFeatureEnabled(\"price_filter\", userId);\n\nint? min_price = optimizelyClient.GetFeatureVariableInteger(\"price_filter\", variableKey: \"min_price\", userId: userId);\n\n",
      "language": "csharp"
    }
  ]
}
[/block]

[block:api-header]
{
  "title": "Source files"
}
[/block]
The language/platform source files containing the implementation for C# is [Optimizely.cs](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Optimizely.cs).