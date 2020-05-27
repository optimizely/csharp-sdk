---
title: "Get Variation"
slug: "get-variation-csharp"
hidden: false
createdAt: "2019-09-12T13:51:54.916Z"
updatedAt: "2019-09-12T20:35:30.799Z"
---
Returns a variation where the visitor will be bucketed, without triggering an impression.
[block:api-header]
{
  "title": "Version"
}
[/block]
SDK v3.0, v3.1
[block:api-header]
{
  "title": "Description"
}
[/block]
Takes the same arguments and returns the same values as [Activate](doc:activate-csharp), but without sending an impression network request. The behavior of the two methods is identical otherwise. 

Use Get Variation if Activate has been called and the current variation assignment is needed for a given experiment and user. This method bypasses redundant network requests to Optimizely.

See [Implement impressions](doc:implement-impressions) for guidance on when to use each method.
[block:api-header]
{
  "title": "Parameters"
}
[/block]
This table lists the required and optional parameters for the C# SDK.
[block:parameters]
{
  "data": {
    "h-0": "Parameter",
    "h-1": "Type",
    "h-2": "Description",
    "0-0": "**experimentey**\n*required*",
    "0-1": "string",
    "1-0": "**userID**\n*required*",
    "1-1": "string",
    "0-2": "The key of the experiment.",
    "1-2": "The ID of the user.",
    "2-2": "A map of custom key-value string pairs specifying attributes for the user that are used for audience targeting and results segmentation. Non-string values are only supported in the 3.0 SDK and above.",
    "2-1": "map",
    "2-0": "**attributes**\n*optional*"
  },
  "cols": 3,
  "rows": 3
}
[/block]
The specific parameter names for each supported language are as follows:
[block:code]
{
  "codes": [
    {
      "code": "experimentKey\nuserId\nattributes\n\n",
      "language": "text",
      "name": "Android"
    },
    {
      "code": "experimentKey\nuserId\nuserAttributes\n\n",
      "language": "text",
      "name": "C#"
    },
    {
      "code": "experimentKey\nuserId\nattributes\n\n",
      "language": "text",
      "name": "Java"
    },
    {
      "code": "experimentKey\nuserId\nattributes\n\n",
      "language": "text",
      "name": "JavaScript"
    },
    {
      "code": "experimentKey\nuserId\nattributes\n\n",
      "language": "text",
      "name": "Node"
    },
    {
      "code": "experimentKey\nuserId\nattributes\n\n",
      "language": "text",
      "name": "Objective-C"
    },
    {
      "code": "experimentKey\nuserId\nattributes\n\n",
      "language": "php",
      "name": "PHP"
    },
    {
      "code": "experiment_key\nuser_id\nattributes\n\n",
      "language": "text",
      "name": "Python"
    },
    {
      "code": "experiment_key\nuser_id\nattributes\n\n",
      "language": "text",
      "name": "Ruby"
    },
    {
      "code": "experimentKey\nuserId\nattributes\n\n",
      "language": "text",
      "name": "Swift"
    }
  ]
}
[/block]

[block:api-header]
{
  "title": "Returns"
}
[/block]
The table below lists the specific information returned for each supported language.
[block:parameters]
{
  "data": {
    "h-0": "Language",
    "h-1": "Return",
    "0-0": "Android",
    "0-1": "@return the variation for the provided experiment key, user id, and attributes",
    "1-1": "<returns>null|Variation Representing variation</returns>",
    "1-0": "C#",
    "2-1": "@return the variation for the provided experiment key, user id, and attributes",
    "2-0": "Java",
    "3-0": "JavaScript (browser)",
    "4-0": "JavaScript (Node)",
    "5-0": "Objective-C",
    "6-0": "PHP",
    "7-0": "Python",
    "8-0": "Ruby",
    "9-0": "Swift",
    "3-1": "@return {string|null} variation key",
    "4-1": "@return {string|null} variation key",
    "5-1": "@return The variation into which the user is bucketed. This value can be nil.",
    "6-1": "@return null|string Representing the variation key",
    "7-1": "Returns: Variation key representing the variation in which the user will be bucketed. None if user is not in experiment or if experiment is not running.",
    "8-1": "@return [variation key] where visitor will be bucketed.\n@return [nil] if the experiment is not running, if the user is not in the experiment, or if the datafile is invalid.",
    "9-1": "@return The variation into which the user was bucketed. This value can be nil."
  },
  "cols": 2,
  "rows": 10
}
[/block]

[block:api-header]
{
  "title": "Example"
}
[/block]

[block:code]
{
  "codes": [
    {
      "code": "import com.optimizely.ab.config.Variation;\n\nMap<String, Object> attributes = new HashMap<>();\nattributes.put(\"device\", \"iPhone\");\nattributes.put(\"lifetime\", 24738388);\nattributes.put(\"is_logged_in\", true);\n\nVariation variation = optimizelyClient.getVariation(\"my_experiment_key\", \"user_123\", attributes);\n\n",
      "language": "java",
      "name": "Android"
    },
    {
      "code": "using OptimizelySDK.Entity;\n\nvar attributes = new UserAttributes {\n  { \"device\", \"iPhone\" },\n  { \"lifetime\", 24738388 },\n  { \"is_logged_in\", true },\n};\n\nvar variation = optimizelyClient.GetVariation(\"my_experiment_key\", \"user_123\", attributes);\n\n",
      "language": "csharp"
    },
    {
      "code": "import com.optimizely.ab.config.Variation;\n\nMap<String, Object> attributes = new HashMap<>();\nattributes.put(\"device\", \"iPhone\");\nattributes.put(\"lifetime\", 24738388);\nattributes.put(\"is_logged_in\", true);\n\nVariation variation = optimizelyClient.getVariation(\"my_experiment_key\", \"user_123\", attributes);\n\n",
      "language": "java"
    },
    {
      "code": "var attributes = {\n  device: 'iPhone',\n  lifetime: 24738388,\n  is_logged_in: true,\n};\n\nvar variationKey = optimizelyClient.getVariation('my_experiment_key', 'user_123', attributes);\n\n",
      "language": "javascript"
    },
    {
      "code": "var attributes = {\n  device: 'iPhone',\n  lifetime: 24738388,\n  is_logged_in: true,\n};\n\nvar variationKey = optimizelyClient.getVariation('my_experiment_key', 'user_123', attributes);\n\n",
      "language": "javascript",
      "name": "Node"
    },
    {
      "code": "NSDictionary *attributes = @{\n  @\"device\": @\"iPhone\",\n  @\"lifetime\": @24738388,\n  @\"is_logged_in\": @true\n};\n\nNSString *variationKey = [optimizely getVariationKeyWithExperimentKey: @\"my_experiment_key\" \n                          userId:@\"user_123\"\n                          attributes:attributes\n                          error:nil];\n\n\n",
      "language": "objectivec"
    },
    {
      "code": "$attributes = [\n  'device' => 'iPhone',\n  'lifetime' => 24738388,\n  'is_logged_in' => true\n];\n\n$variationKey = $optimizelyClient->getVariation('my_experiment_key', 'user_123', $attributes);\n\n",
      "language": "php"
    },
    {
      "code": "attributes = {\n  'device': 'iPhone',\n  'lifetime': 24738388,\n  'is_logged_in': True,\n}\n\nvariation_key = optimizely_client.get_variation('my_experiment_key', 'user_123', attributes)\n\n",
      "language": "python"
    },
    {
      "code": "attributes = {\n  'device' => 'iPhone',\n  'lifetime' => 24738388,\n  'is_logged_in' => true,\n}\n\nvariation_key = optimizely_client.get_variation('my_experiment_key', 'user_123', attributes)\n\n",
      "language": "ruby"
    },
    {
      "code": "let attributes = [\n  \"device\": \"iPhone\",\n  \"lifetime\": 24738388,\n  \"is_logged_in\": true,\n]\n\nlet variationKey = try? optimizely.getVariationKey(experimentKey: \"my_experiment_key\",\n\t\t\t\t\t\t\t\t\t\t\t\tuserId: \"user_123\",\n                        attributes: attributes)\n\n",
      "language": "swift"
    }
  ]
}
[/block]

[block:api-header]
{
  "title": "Notes"
}
[/block]
### Activate versus Get Variation

Use Activate when the visitor actually sees the experiment. Use Get Variation when you need to know which bucket a visitor is in before showing the visitor the experiment. Impressions are tracked by [Is Feature Enabled](doc:is-feature-enabled-csharp) when there is a feature test running on the feature and the visitor qualifies for that feature test.

For example, suppose you want your web server to show a visitor variation_1 but don't want the visitor to count until they open a feature that isn't visible when the variation loads, like a modal. In this case, use Get Variation in the backend to specify that your web server should respond with variation_1, and use Activate in the front end when the visitor sees the experiment.

Also, use Get Variation when you're trying to align your Optimizely results with client-side third-party analytics. In this case, use Get Variation to retrieve the variation, and even show it to the visitor, but only call Activate when the analytics call goes out.

See [Implement impressions](doc:implement-impressions) for more information about whether to use Activate or Get Variation for a call.
[block:callout]
{
  "type": "warning",
  "title": "Important",
  "body": "Conversion events can only be attributed to experiments with previously tracked impressions. Impressions are tracked by Activate, not by Get Variation. As a general rule, Optimizely impressions are required for experiment results and not only for billing."
}
[/block]

[block:api-header]
{
  "title": "Source files"
}
[/block]
The language/platform source files containing the implementation for C# is [Optimizely.cs](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Optimizely.cs).