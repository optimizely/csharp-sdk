---
title: "Get Variation"
slug: "get-variation-csharp"
hidden: false
createdAt: "2019-09-12T13:51:54.916Z"
updatedAt: "2019-09-12T20:35:30.799Z"
---

Returns a variation where the visitor will be bucketed, without triggering an impression.

## Version

SDK v3.0, v3.1

## Description

Takes the same arguments and returns the same values as [Activate](doc:activate-csharp), but without sending an impression network request. The behavior of the two methods is identical otherwise.

Use Get Variation if Activate has been called and the current variation assignment is needed for a given experiment and user. This method bypasses redundant network requests to Optimizely.

See [Implement impressions](doc:implement-impressions) for guidance on when to use each method.

## Parameters

This table lists the required and optional parameters for the C# SDK.

| Parameter       | Type   | Description                                                                                          |
|-----------------|--------|------------------------------------------------------------------------------------------------------|
| **experimentKey**<br>*required* | string | The key of the experiment.                                                                         |
| **userID**<br>*required*       | string | The ID of the user.                                                                                |
| **attributes**<br>*optional*   | map    | A map of custom key-value string pairs specifying attributes for the user that are used for audience targeting and results segmentation. Non-string values are only supported in the 3.0 SDK and above. |


The specific parameter names for each supported language are as follows:
## Parameter Names for Supported Languages

The specific parameter names for each supported language are as follows:

### Android

experimentKey
userId
attributes

### C#
experimentKey
userId
userAttributes

### Java
experimentKey
userId
attributes

### JavaScript
experimentKey
userId
attributes

### Node
experimentKey
userId
attributes

### Objective-C
experimentKey
userId
attributes

and same for PHP, Python, Ruby and Swift.

## Returns

The table below lists the specific information returned for each supported language.

| Language                | Return                                                                                           |
|-------------------------|-------------------------------------------------------------------------------------------------|
| Android                 | `@return` the variation for the provided experiment key, user id, and attributes              |
| C#                      | `@return` the variation for the provided experiment key, user id, and attributes              |
| Java                    | `@return` the variation for the provided experiment key, user id, and attributes              |
| JavaScript (browser)    | `@return` {string|null} variation key                                                          |
| JavaScript (Node)       | `@return` {string|null} variation key                                                          |
| Objective-C             | `@return` The variation into which the user is bucketed. This value can be nil.               |
| PHP                     | `@return` null|string Representing the variation key                                           |
| Python                  | Returns: Variation key representing the variation in which the user will be bucketed. None if user is not in experiment or if experiment is not running. |
| Ruby                    | `@return` [variation key] where visitor will be bucketed.\n`@return` [nil] if the experiment is not running, if the user is not in the experiment, or if the datafile is invalid. |
| Swift                   | `@return` The variation into which the user was bucketed. This value can be nil.              |

## Example

Here are usage examples for each supported language:

### Android

```java
import com.optimizely.ab.config.Variation;

Map<String, Object> attributes = new HashMap<>();
attributes.put("device", "iPhone");
attributes.put("lifetime", 24738388);
attributes.put("is_logged_in", true);

Variation variation = optimizelyClient.getVariation("my_experiment_key", "user_123", attributes);
```

### C#

```csharp
using OptimizelySDK.Entity;

var attributes = new UserAttributes {
  { "device", "iPhone" },
  { "lifetime", 24738388 },
  { "is_logged_in", true },
};

var variation = optimizelyClient.GetVariation("my_experiment_key", "user_123", attributes);
```

### Android

```java
import com.optimizely.ab.config.Variation;

Map<String, Object> attributes = new HashMap<>();
attributes.put("device", "iPhone");
attributes.put("lifetime", 24738388);
attributes.put("is_logged_in", true);

Variation variation = optimizelyClient.getVariation("my_experiment_key", "user_123", attributes);
```

### JavaScript (browser)
```javascript
var attributes = {
  device: 'iPhone',
  lifetime: 24738388,
  is_logged_in: true,
};

var variationKey = optimizelyClient.getVariation('my_experiment_key', 'user_123', attributes);

```

### JavaScript (Node)
```javascript
var attributes = {
  device: 'iPhone',
  lifetime: 24738388,
  is_logged_in: true,
};

var variationKey = optimizelyClient.getVariation('my_experiment_key', 'user_123', attributes);

```

### Objective-C
```objective-c
NSDictionary *attributes = @{
  @"device": @"iPhone",
  @"lifetime": @24738388,
  @"is_logged_in": @true
};

NSString *variationKey = [optimizely getVariationKeyWithExperimentKey: @"my_experiment_key"
                          userId: @"user_123"
                          attributes: attributes
                          error:nil];

```

### PHP
```php
$attributes = [
  'device' => 'iPhone',
  'lifetime' => 24738388,
  'is_logged_in' => true
];

$variationKey = $optimizelyClient->getVariation('my_experiment_key', 'user_123', $attributes);

```

### Python
```python
attributes = {
  'device': 'iPhone',
  'lifetime': 24738388,
  'is_logged_in': True,
}

variation_key = optimizely_client.get_variation('my_experiment_key', 'user_123', attributes)

```

### Ruby
```ruby
attributes = {
  'device' => 'iPhone',
  'lifetime' => 24738388,
  'is_logged_in' => true,
}

variation_key = optimizely_client.get_variation('my_experiment_key', 'user_123', attributes)

```

### Swift
```swift
let attributes = [
  "device": "iPhone",
  "lifetime": 24738388,
  "is_logged_in": true,
]

let variationKey = try? optimizely.getVariationKey(experimentKey: "my_experiment_key",
                                          userId: "user_123",
                                          attributes: attributes)

```

## Notes

### Activate versus Get Variation

Use Activate when the visitor actually sees the experiment. Use Get Variation when you need to know which bucket a visitor is in before showing the visitor the experiment. Impressions are tracked by [Is Feature Enabled](doc:is-feature-enabled-csharp) when there is a feature test running on the feature and the visitor qualifies for that feature test.

For example, suppose you want your web server to show a visitor variation_1 but don't want the visitor to count until they open a feature that isn't visible when the variation loads, like a modal. In this case, use Get Variation in the backend to specify that your web server should respond with variation_1, and use Activate in the front end when the visitor sees the experiment.

Also, use Get Variation when you're trying to align your Optimizely results with client-side third-party analytics. In this case, use Get Variation to retrieve the variation, and even show it to the visitor, but only call Activate when the analytics call goes out.

See [Implement impressions](doc:implement-impressions) for more information about whether to use Activate or Get Variation for a call.

> **Important:** Conversion events can only be attributed to experiments with previously tracked impressions. Impressions are tracked by Activate, not by Get Variation. As a general rule, Optimizely impressions are required for experiment results and not only for billing.

## Source files

The language/platform source files containing the implementation for C# is [Optimizely.cs](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Optimizely.cs).
