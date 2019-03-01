## 3.0.0
March 1st, 2019

The 3.0 release improves event tracking and supports additional audience targeting functionality.
### New Features:
* Event tracking:
  * The `track` method now dispatches its conversion event _unconditionally_, without first determining whether the user is targeted by a known experiment that uses the event. This may increase outbound network traffic.
  * In Optimizely results, conversion events sent by 3.0 SDKs are automatically attributed to variations that the user has previously seen, as long as our backend has actually received the impression events for those variations.
  * Altogether, this allows you to track conversion events and attribute them to variations even when you don't know all of a user's attribute values, and even if the user's attribute values or the experiment's configuration have changed such that the user is no longer affected by the experiment. As a result, **you may observe an increase in the conversion rate for previously-instrumented events.** If that is undesirable, you can reset the results of previously-running experiments after upgrading to the 3.0 SDK.
  * This will also allow you to attribute events to variations from other Optimizely projects in your account, even though those experiments don't appear in the same datafile.
  * Note that for results segmentation in Optimizely results, the user attribute values from one event are automatically applied to all other events in the same session, as long as the events in question were actually received by our backend. This behavior was already in place and is not affected by the 3.0 release.
* Support for all types of attribute values, not just strings.
  * All values are passed through to notification listeners.
  * Strings, booleans, and valid numbers are passed to the event dispatcher and can be used for Optimizely results segmentation. A valid number is a finite number in the inclusive range [-2⁵³, 2⁵³].
  * Strings, booleans, and valid numbers are relevant for audience conditions.
* Support for additional matchers in audience conditions:
  * An `exists` matcher that passes if the user has a non-null value for the targeted user attribute and fails otherwise.
  * A `substring` matcher that resolves if the user has a string value for the targeted attribute.
  * `gt` (greater than) and `lt` (less than) matchers that resolve if the user has a valid number value for the targeted attribute. A valid number is a finite number in the inclusive range [-2⁵³, 2⁵³].
  * The original (`exact`) matcher can now be used to target booleans and valid numbers, not just strings.
* Support for A/B tests, feature tests, and feature rollouts whose audiences are combined using `"and"` and `"not"` operators, not just the `"or"` operator.
* Datafile-version compatibility check: The SDK will remain uninitialized (i.e., will gracefully fail to activate experiments and features) if given a datafile version greater than 4.
* Updated Pull Request template and commit message guidelines.
### Breaking Changes:
* `UserAttributes` objects, which are passed to API methods and returned to notification listeners, can now contain non-string values. More concretely, `UserAttributes` now extends `Dictionary<string, object>` instead of `Dictionary<string, string>`.
### Bug Fixes:
* Experiments and features can no longer activate when a negatively targeted attribute has a missing, null, or malformed value. ([#132](https://github.com/optimizely/csharp-sdk/pull/132))
  * Audience conditions (except for the new `exists` matcher) no longer resolve to `false` when they fail to find an legitimate value for the targeted user attribute. The result remains `null` (unknown). Therefore, an audience that negates such a condition (using the `"not"` operator) can no longer resolve to `true` unless there is an unrelated branch in the condition tree that itself resolves to `true`.
* `SetForcedVariation` now treats an empty variation key as invalid and does not reset the variation. ([#113](https://github.com/optimizely/csharp-sdk/pull/113))
* All methods now treat an empty user ID as valid.
* `HttpClientEventDispatcher45` now logs full exception ([#112](https://github.com/optimizely/csharp-sdk/pull/112))
* You can now specify `0` or `1` as the `revenue` or `value` for a conversion event when using the `Track` method. Previously, `0` and `1` were withheld, would not appear in your data export, and in the case of `1` would not contribute to the user's total revenue or value.

## 2.2.2
January 31, 2019
### Bug fixes
* fix(eventtagsutils) : fixes bug where values of 0 and 1 are excluded from the event value in the conversion event payload. ([#132](https://github.com/optimizely/csharp-sdk/pull/132))

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
