## 2.2.1
November 5, 2018
### Bug fixes
* Fix package signing for installation via Nuget.

## 2.2.0
October 26, 2018

### New Features
* refactor(interface): Adds IOptimizely interface ([#93](https://github.com/optimizely/csharp-sdk/pull/93))
* feat(api): Accepting all types for attributes values ([#102](https://github.com/optimizely/csharp-sdk/pull/102))

### Bug fixes
* fix(whitelistng): Removed logic from bucketing since it is checked in Decision Service. ([#98](https://github.com/optimizely/csharp-sdk/pull/98))
* fix(track): Send decisions for all experiments using an event when using track. ([#100](https://github.com/optimizely/csharp-sdk/pull/100))
* fix(datafile-parsing): Prevent newer versions datafile ([#101](https://github.com/optimizely/csharp-sdk/pull/101))
* fix(api): Only track attributes with valid attribute types. ([#103](https://github.com/optimizely/csharp-sdk/pull/103))

## 2.1.0
June 28, 2018

### New Features
* Introduces support for bot filtering. ([#79](https://github.com/optimizely/csharp-sdk/pull/79))

## 2.0.1
June 20, 2018

### Bug Fixes
* Fix events are not sent from the SDK for a variation in a feature test if the
feature is disabled.

## 2.0.0
April 16, 2018

This major release of the Optimizely SDK introduces APIs for Feature Management.

### New Features
* Introduces the `IsFeatureEnabled` API to determine whether to show a feature to a user or not.
```
var enabled = OptimizelyClient.IsFeatureEnabled("my_feature_key", "user_1", userAttributes);
```

* You can also get all the enabled features for the user by calling the following method which returns a list of strings representing the feature keys:
```
var enabledFeatures = OptimizelyClient.GetEnabledFeatures("user_1", userAttributes);
```

* Introduces Feature Variables to configure or parameterize your feature. There are four variable types: `Integer`, `String`, `Double`, `Boolean`.
```
var stringVariable = OptimizelyClient.GetFeatureVariableString("my_feature_key", "string_variable_key", "user_1", userAttributes);
var integerVariable = OptimizelyClient.GetFeatureVariableInteger("my_feature_key", "integer_variable_key", "user_1", userAttributes);
var doubleVariable = OptimizelyClient.GetFeatureVariableDouble("my_feature_key", "double_variable_key", "user_1", userAttributes);
var booleanVariable = OptimizelyClient.GetFeatureVariableBoolean("my_feature_key", "boolean_variable_key", "user_1", userAttributes);
```

## 1.3.1
February 14, 2018

### Bug Fixes
* Change 'murmurhash' to 'murmurhash-signed' in OptimizelySDK.nuspec fixing:
```
System.IO.FileLoadException 'Could not load file or assembly 'MurmurHash, ... PublicKeyToken ...'
```

## 1.3.0
January 5, 2018

### New Features
* Feature Notification Center
* Third party component DLL's dependencies unbundled to NUGET.ORG .
* DemoApp README.md

### Bug Fixes
* Httpclient issues - object initializing multiple times and default timeout.

## 1.2.1
November 6, 2017

### New Features
* Package DLL's including third party component DLL's are strongnamed and digitally signed by Optimizely.
* DecisionService GetVariationForFeatureRollout added.
* Feature Flag and Rollout models added.
* Implemented Bucketing ID feature.

## 1.2.0
October 4, 2017

### New Features
* Introduce Numeric Metrics - This allows you to include a floating point value that is used to track numeric values in your experiments.
```
var eventTags = new EventTags()
    {
        { "value", 10.00 },
    };

OptimizelyClient.Track(eventKey, userId, attributes, eventTags);
```

* Introduce Forced Variation - This allows you to force users into variations programmatically in real time for QA purposes without requiring datafile downloads from the network.
```
var result = OptimizelyClient.SetForcedVariation(experimentKey, userId, forcedVariationKey);
```

* Upgrade to use new [event API](https://developers.optimizely.com/x/events/api/index.html).

## 1.1.1
 - Add .Net 4.0 build in nuget package.

## 1.1.0
 - Introduce the user profile service.

## 1.0.0
- General release of Optimizely X Full Stack C# SDK. No breaking changes from previous version.

## 0.1.0
- Beta release of the Optimizely X Full Stack C# SDK.
