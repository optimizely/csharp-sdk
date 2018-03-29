## 2.0.0-beta6

March 29th, 2018

This major release of the Optimizely SDK introduces APIs for Feature Management. It also introduces some breaking changes listed below.

### New Features
* Introduces the `isFeatureEnabled` API to determine whether to show a feature to a user or not.
```
var enabled = optimizelyClient.isFeatureEnabled("my_feature_key", "user_1", userAttributes);
```

* You can also get all the enabled features for the user by calling the following method which returns a list of strings representing the feature keys:
```
var enabledFeatures = optimizelyClient.getEnabledFeatures("user_1", userAttributes);
```

* Introduces Feature Variables to configure or parameterize your feature. There are four variable types: `Integer`, `String`, `Double`, `Boolean`.
```
var stringVariable = optimizelyClient.getFeatureVariableString("my_feature_key", "string_variable_key", "user_1");
var integerVariable = optimizelyClient.getFeatureVariableInteger("my_feature_key", "integer_variable_key", "user_1");
var doubleVariable = optimizelyClient.getFeatureVariableDouble("my_feature_key", "double_variable_key", "user_1");
var booleanVariable = optimizelyClient.getFeatureVariableBoolean("my_feature_key", "boolean_variable_key", "user_1");
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
var result = OptimizelyClient.setForcedVariation(experimentKey, userId, forcedVariationKey);
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
