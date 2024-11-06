# Optimizely C# SDK Changelog

## 4.1.0
November 7th, 2024

### Enhancement

- Added support for batch processing in `DecideAll` and `DecideForKeys`, enabling more efficient handling of multiple decisions in the User Profile Service. ([#375](https://github.com/optimizely/csharp-sdk/pull/375))

### Bug Fixes
- GitHub Actions YAML files vulnerable to script injections ([#372](https://github.com/optimizely/csharp-sdk/pull/372))

## 4.0.0
January 16th, 2024

### New Features

#### Advanced Audience Targeting

The 4.0.0 release introduces a new primary feature, [Advanced Audience Targeting]( https://docs.developers.optimizely.com/feature-experimentation/docs/optimizely-data-platform-advanced-audience-targeting)
  enabled through integration with [Optimizely Data Platform (ODP)](https://docs.developers.optimizely.com/optimizely-data-platform/docs) (
  [#305](https://github.com/optimizely/csharp-sdk/pull/305),
  [#310](https://github.com/optimizely/csharp-sdk/pull/310),
  [#311](https://github.com/optimizely/csharp-sdk/pull/311),
  [#315](https://github.com/optimizely/csharp-sdk/pull/315),
  [#321](https://github.com/optimizely/csharp-sdk/pull/321),
  [#322](https://github.com/optimizely/csharp-sdk/pull/322),
  [#323](https://github.com/optimizely/csharp-sdk/pull/323),
  [#324](https://github.com/optimizely/csharp-sdk/pull/324)
  ).

You can use ODP, a high-performance [Customer Data Platform (CDP)]( https://www.optimizely.com/optimization-glossary/customer-data-platform/), to easily create complex
real-time segments (RTS) using first-party and 50+ third-party data sources out of the box. You    can create custom schemas that support the user attributes important
for your business, and stitch together user behavior done on different devices to better understand and target your customers for personalized user experiences. ODP can
be used as a single source of truth for these segments in any Optimizely or 3rd party tool.

With ODP accounts integrated into Optimizely projects, you can build audiences using segments pre-defined in ODP. The SDK will fetch the segments for given users and
make decisions using the segments. For access to ODP audience targeting in your Feature Experimentation account, please contact your Optimizely Customer Success Manager.

This version includes the following changes:
- New API added to `OptimizelyUserContext`:
    - `FetchQualifiedSegments()`: this API will retrieve user segments from the ODP server. The fetched segments will be used for audience evaluation. The fetched data will be stored in the local cache to avoid repeated network delays.
    - When an `OptimizelyUserContext` is created, the SDK will automatically send an identify request  to the ODP server to facilitate observing user activities.
- New APIs added to `OptimizelyClient`:
    - `SendOdpEvent()`: customers can build/send arbitrary ODP events that will bind user identifiers and data to user profiles in ODP.

For details, refer to our documentation pages:
- [Advanced Audience Targeting](https://docs.developers.optimizely.com/feature-experimentation/docs/optimizely-data-platform-advanced-audience-targeting)
- [Server SDK Support](https://docs.developers.optimizely.com/feature-experimentation/v1.0/docs/advanced-audience-targeting-for-server-side-sdks)
- [Initialize C# SDK](https://docs.developers.optimizely.com/feature-experimentation/docs/initialize-sdk-csharp)
- [OptimizelyUserContext C# SDK](https://docs.developers.optimizely.com/feature-experimentation/docs/optimizelyusercontext-csharp)
- [Advanced Audience Targeting segment qualification methods](https://docs.developers.optimizely.com/feature-experimentation/docs/advanced-audience-targeting-segment-qualification-methods-csharp)
- [Send Optimizely Data Platform data using Advanced Audience Targeting](https://docs.developers.optimizely.com/feature-experimentation/docs/send-odp-data-using-advanced-audience-targeting-csharp)

#### Polling warning

Add warning to polling intervals below 30 seconds ([#365](https://github.com/optimizely/csharp-sdk/pull/365))

### Breaking Changes
- `OdpManager` in the SDK is enabled by default. Unless an ODP account is integrated into the Optimizely projects, most `OdpManager` functions will be ignored. If needed, `OdpManager` can be disabled when `OptimizelyClient` is instantiated.
- `ProjectConfigManager` interface additions + implementing class updates
- `Evaluate()` updates in `BaseCondition`

### Bug Fixes
- Return Latest Experiment When Duplicate Keys in Config  enhancement 

### Documentation
- Corrections to markdown files in docs directory ([#368](https://github.com/optimizely/csharp-sdk/pull/368))
- GitHub template updates ([#366](https://github.com/optimizely/csharp-sdk/pull/366))

## 3.11.4
July 26th, 2023

### Bug Fixes
- Fix Last-Modified date & time format for If-Modified-Since ([#361](https://github.com/optimizely/csharp-sdk/pull/361))

## 3.11.3
July 18th, 2023

### Bug Fixes
- Last-Modified in header not found and used to reduce polling payload ([#355](https://github.com/optimizely/csharp-sdk/pull/355)).

## 4.0.0-beta
April 28th, 2023

### New Features 
The 4.0.0-beta release introduces a new primary feature, [Advanced Audience Targeting]( https://docs.developers.optimizely.com/feature-experimentation/docs/optimizely-data-platform-advanced-audience-targeting) 
enabled through integration with [Optimizely Data Platform (ODP)](https://docs.developers.optimizely.com/optimizely-data-platform/docs) (
[#305](https://github.com/optimizely/csharp-sdk/pull/305),
[#310](https://github.com/optimizely/csharp-sdk/pull/310),
[#311](https://github.com/optimizely/csharp-sdk/pull/311),
[#315](https://github.com/optimizely/csharp-sdk/pull/315),
[#321](https://github.com/optimizely/csharp-sdk/pull/321),
[#322](https://github.com/optimizely/csharp-sdk/pull/322),
[#323](https://github.com/optimizely/csharp-sdk/pull/323),
[#324](https://github.com/optimizely/csharp-sdk/pull/324)
). 

You can use ODP, a high-performance [Customer Data Platform (CDP)]( https://www.optimizely.com/optimization-glossary/customer-data-platform/), to easily create complex 
real-time segments (RTS) using first-party and 50+ third-party data sources out of the box. You    can create custom schemas that support the user attributes important 
for your business, and stitch together user behavior done on different devices to better understand and target your customers for personalized user experiences. ODP can 
be used as a single source of truth for these segments in any Optimizely or 3rd party tool. 

With ODP accounts integrated into Optimizely projects, you can build audiences using segments pre-defined in ODP. The SDK will fetch the segments for given users and 
make decisions using the segments. For access to ODP audience targeting in your Feature Experimentation account, please contact your Optimizely Customer Success Manager.

This version includes the following changes:
- New API added to `OptimizelyUserContext`:
  - `FetchQualifiedSegments()`: this API will retrieve user segments from the ODP server. The fetched segments will be used for audience evaluation. The fetched data will be stored in the local cache to avoid repeated network delays.
  - When an `OptimizelyUserContext` is created, the SDK will automatically send an identify request  to the ODP server to facilitate observing user activities.
- New APIs added to `OptimizelyClient`:
  - `SendOdpEvent()`: customers can build/send arbitrary ODP events that will bind user identifiers and data to user profiles in ODP. 

For details, refer to our documentation pages: 
- [Advanced Audience Targeting](https://docs.developers.optimizely.com/feature-experimentation/docs/optimizely-data-platform-advanced-audience-targeting) 
- [Server SDK Support](https://docs.developers.optimizely.com/feature-experimentation/v1.0/docs/advanced-audience-targeting-for-server-side-sdks)
- [Initialize C# SDK](https://docs.developers.optimizely.com/feature-experimentation/docs/initialize-sdk-csharp)
- [OptimizelyUserContext C# SDK](https://docs.developers.optimizely.com/feature-experimentation/docs/optimizelyusercontext-csharp)
- [Advanced Audience Targeting segment qualification methods](https://docs.developers.optimizely.com/feature-experimentation/docs/advanced-audience-targeting-segment-qualification-methods-csharp)
- [Send Optimizely Data Platform data using Advanced Audience Targeting](https://docs.developers.optimizely.com/feature-experimentation/docs/send-odp-data-using-advanced-audience-targeting-csharp)

### Breaking Changes
- `OdpManager` in the SDK is enabled by default. Unless an ODP account is integrated into the Optimizely projects, most `OdpManager` functions will be ignored. If needed, `OdpManager` can be disabled when `OptimizelyClient` is instantiated.
- `ProjectConfigManager` interface additions + implementing class updates
- `Evaluate()` updates in `BaseCondition`

## 3.11.2
March 15th, 2023

- Update README.md and other non-functional code to reflect that this SDK supports both Optimizely Feature Experimentation and Optimizely Full Stack. ([#331](https://github.com/optimizely/csharp-sdk/pull/331), [#332](https://github.com/optimizely/csharp-sdk/pull/332)).

### Bug Fixes
- Fix for incorrect documentation on Optimizely.IsFeatureEnabled ([#304](https://github.com/optimizely/csharp-sdk/pull/329))

## 3.11.1
July 27th, 2022

### Bug Fixes
- Handle possible empty string `rolloutId` in datafile ([#304](https://github.com/optimizely/csharp-sdk/pull/304))

## 3.11.0
January 6th, 2022

### New Features
* Add a set of new APIs for overriding and managing user-level flag, experiment and delivery rule decisions. These methods can be used for QA and automated testing purposes. They are an extension of the OptimizelyUserContext interface ([#285](https://github.com/optimizely/csharp-sdk/pull/285), [#292](https://github.com/optimizely/csharp-sdk/pull/292))
  - SetForcedDecision
  - GetForcedDecision
  - RemoveForcedDecision
  - RemoveAllForcedDecisions

- For details, refer to our documentation pages: [OptimizelyUserContext](https://docs.developers.optimizely.com/full-stack/v4.0/docs/optimizelyusercontext-csharp) and [Forced Decision methods](https://docs.developers.optimizely.com/full-stack/v4.0/docs/forced-decision-methods-csharp).

## 3.10.0
September 16th, 2021

### New Features
- Add new public properties to `OptimizelyConfig`. ([#265](https://github.com/optimizely/csharp-sdk/pull/265), [#266](https://github.com/optimizely/csharp-sdk/pull/266), [#273](https://github.com/optimizely/csharp-sdk/pull/273), [#276](https://github.com/optimizely/csharp-sdk/pull/276), [#279](https://github.com/optimizely/csharp-sdk/pull/279))
	- SDKKey
 	- EnvironmentKey
	- Attributes
	- Audiences
	- Events
	- ExperimentRules and DeliveryRules to OptimizelyFeature
	- Audiences to OptimizelyExperiment
- For details, refer to our documentation page: [https://docs.developers.optimizely.com/full-stack/v4.0/docs/optimizelyconfig-csharp](https://docs.developers.optimizely.com/full-stack/v4.0/docs/optimizelyconfig-csharp).

- Add new methods in `OptimizelyFactory` class. ([#264](https://github.com/optimizely/csharp-sdk/pull/264))
  - SetBlockingTimeOutPeriod
  - SetPollingInterval
- Add virtual methods to support mocking in `OptimizelyUserContext` ([#280](https://github.com/optimizely/csharp-sdk/pull/280))

### Deprecated:

* `OptimizelyFeature.ExperimentsMap` of `OptimizelyConfig` is deprecated as of this release. Please use `OptimizelyFeature.ExperimentRules` and `OptimizelyFeature.DeliveryRules`. ([#276](https://github.com/optimizely/csharp-sdk/pull/276))


## 3.9.1
July 16th, 2021

### Bug Fixes:
- Duplicate experiment key issue with multiple feature flags. While trying to get variation from the variationKeyMap, it was unable to find because the latest experimentKey was overriding the previous one. [#267](https://github.com/optimizely/csharp-sdk/pull/267)

## 3.9.0
March 29th, 2021

### Bug Fixes:
- When no error handler is given for HttpProjectConfigManager, then default error handler should be used without raise exception. [#260](https://github.com/optimizely/csharp-sdk/pull/260)
- .Net Standard 2.0 was missing Configuration manager library in nugget package. [#262](https://github.com/optimizely/csharp-sdk/pull/262)

## [3.8.0]
February 16th, 2021

### New Features
- Introducing a new primary interface for retrieving feature flag status, configuration and associated experiment decisions for users ([#248](https://github.com/optimizely/csharp-sdk/pull/248), [#250](https://github.com/optimizely/csharp-sdk/pull/250), [#251](https://github.com/optimizely/csharp-sdk/pull/251), [#253](https://github.com/optimizely/csharp-sdk/pull/253), [#254](https://github.com/optimizely/csharp-sdk/pull/254), [#255](https://github.com/optimizely/csharp-sdk/pull/255), [#256](https://github.com/optimizely/csharp-sdk/pull/256), [#257](https://github.com/optimizely/csharp-sdk/pull/257), [#258](https://github.com/optimizely/csharp-sdk/pull/258)). The new `OptimizelyUserContext` class is instantiated with `CreateUserContext` and exposes the following APIs to get `OptimizelyDecision`:

	- SetAttribute
	- GetAttributes
	- Decide
	- DecideAll
	- DecideForKeys
	- TrackEvent

- For details, refer to our documentation page: [https://docs.developers.optimizely.com/full-stack/v4.0/docs/csharp-sdk](https://docs.developers.optimizely.com/full-stack/v4.0/docs/csharp-sdk).

### Bug Fixes:
- Disposed during inflight request of datafile was causing issues in the PollingProjectConfigManager. ([#258](https://github.com/optimizely/csharp-sdk/pull/258))

## 3.7.1
November 18th, 2020

### New Features
- Add "enabled" field to decision metadata structure. ([#249](https://github.com/optimizely/csharp-sdk/pull/249))

## 3.7.0
November 3rd, 2020

### New Features
- Add support for sending flag decisions along with decision metadata. ([#244](https://github.com/optimizely/csharp-sdk/pull/244))

## 3.6.0
October 1st, 2020

### New Features
- Add support for version audience condition which follows the semantic version (http://semver.org) ([#236](https://github.com/optimizely/csharp-sdk/pull/236), [#242](https://github.com/optimizely/csharp-sdk/pull/242))

- Add support for datafile accessor [#240](https://github.com/optimizely/csharp-sdk/pull/240).
- `datafileAccessToken` supported from `App.config` ([#237](https://github.com/optimizely/csharp-sdk/pull/237))


### Bug Fixes:
- No rollout rule in datafile, should return false when `IsFeatureEnabled` is called. ([#235](https://github.com/optimizely/csharp-sdk/pull/235))
- `NewDefaultInstance` method of `OptimizelyFactory` class, set ErrorHandler not to  raise exception while handling error ([#241](https://github.com/optimizely/csharp-sdk/pull/241))
- Audience evaluation logs revised ([#229](https://github.com/optimizely/csharp-sdk/pull/229))

## 3.5.0
July 7th, 2020

### New Features
- Add support for JSON feature variables. ([#214](https://github.com/optimizely/csharp-sdk/pull/214), [#216](https://github.com/optimizely/csharp-sdk/pull/216), [#217](https://github.com/optimizely/csharp-sdk/pull/217))
- Add support for authenticated datafiles. ([#222](https://github.com/optimizely/csharp-sdk/pull/222))
- Add gzip support for framework 4.5 or above. ([#218](https://github.com/optimizely/csharp-sdk/pull/218))

### Bug Fixes:
- Adjust audience evaluation log level to debug. ([#221](https://github.com/optimizely/csharp-sdk/pull/221))

## 3.4.1
April 29th, 2020

### Bug Fixes:
- Change FeatureVariable type from enum to string for forward compatibility. [#211](https://github.com/optimizely/csharp-sdk/pull/211)
- GetFeatureVariableDouble was returning 0 for FR culture. Fixed this issue by returning Invariant culture. [#209](https://github.com/optimizely/csharp-sdk/pull/209)

## 3.4.0
January 23rd, 2020

### New Features
- Added a new API to get a project configuration static data.
  - Call `GetOptimizelyConfig()` to get a snapshot copy of project configuration static data.
  - It returns an `OptimizelyConfig` instance which includes a datafile revision number, all experiments, and feature flags mapped by their key values.
  - Added caching for `GetOptimizelyConfig` - `OptimizelyConfig` object will be cached and reused for the lifetime of the datafile
  - For details, refer to a documentation page: https://docs.developers.optimizely.com/full-stack/docs/optimizelyconfig-csharp

### Bug Fixes:
- Blocking timeout was not being assigned. When not providing any value, it was just logging not setting up periodinterval and blocking timeout value. [#202](https://github.com/optimizely/csharp-sdk/pull/202)

## 3.3.0
September 26th, 2019

### New Features:
- Configuration manager is set to PollingProjectConfigManager and for datafile will be started by default. Requests to download and update datafile are made in a separate thread and are scheduled with fixed delay.
- Added support for event batching via the event processor.
- Events generated by methods like `Activate`, `Track`, and `IsFeatureEnabled` will be held in a queue until the configured batch size is reached, or the configured flush interval has elapsed. Then, they will be combined into a request and sent to the event dispatcher.
- To configure event batching, set the `MaxEventBatchSize` and `MaxEventFlushInterval` properties in the `OptimizelyFactory` using `OptimizelyFactory.SetBatchSize(int batchSize)` and `OptimizelyFactory.SetFlushInterval(TimeSpan flushInterval)` and then creating using `OptimizelyFactory.NewDefaultInstance`.
- Event batching is enabled by default. `eventBatchSize` defaults to `10`. `eventFlushInterval` defaults to `30000` milliseconds.
- Updated the `Dispose` method representing the process of closing the instance. When `Dispose` is called, any events waiting to be sent as part of a batched event request will be immediately batched and sent to the event dispatcher.
- If any such requests were sent to the event dispatcher, `Stop` waits for provided `TimeoutInterval` before stopping, so that events get successfully dispatched.
- `OptimizelyFactory` now provides support of setting configuration variables from ***App.config***, User will now be able to provide configuration variables of `HttpConfigManager` and `BatchEventProcessor` in ***App.config***. Steps of usage are provided in [README.md](https://github.com/optimizely/csharp-sdk#using-appconfig-in-optimizelyfactory).

### Deprecated
- `EventBuilder` was deprecated and now we will be using `UserEventFactory` and `EventFactory` to create LogEvent Object.
- Deprecated `Track` notifications in favor of explicit `LogEvent` notification.
- New features will no longer be supported on `.net standard 1.6` and `.net 3.5`

## 3.2.0
July 22nd, 2019

### New Features:
* Added support for automatic datafile management via `HttpProjectConfigManager` for framework 4.0 or above:
  * The [`HttpProjectConfigManager`](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Config/HttpProjectConfigManager.cs) is an implementation of the abstract
      [`PollingProjectConfigManager`](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Config/PollingProjectConfigManager.cs) class.
    - Users must first build the `HttpProjectConfigManager` with an SDK key and then and provide that instance to the `Optimizely` instance.
    - An initial datafile can be provided to the `HttpProjectConfigManager` to bootstrap before making HTTP requests for the hosted datafile.
    - Requests for the datafile are made in a separate thread and are scheduled with fixed delay.
    - Configuration updates can be subscribed to via the NotificationCenter built with the `HttpProjectConfigManager`.
    - `Optimizely` instance must be disposed after the use or `HttpProjectConfigManager` must be disposed after the use to release resources.
* The [`OptimizelyFactory`](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/OptimizelyFactory.cs) provides basic methods for instantiating the Optimizely SDK with a minimal number of parameters. Check [`README.md`](https://github.com/optimizely/csharp-sdk#use-optimizelyfactory) for more details.

## 3.1.1
June 19th, 2019

### Bug Fixes:
* Build OptimizelySDK.NetStandard16.dll in Release mode

## 3.1.0
May 9th, 2019

### New Features:
* Introduced Decision notification listener to be able to record:
  * Variation assignments for users activated in an experiment.
  * Feature access for users.
  * Feature variable value for users.

### Bug Fixes:
* Feature variable APIs return default variable value when featureEnabled property is false. ([#151](https://github.com/optimizely/csharp-sdk/pull/151))

### Deprecated:
* Activate notification listener is deprecated as of this release. Recommendation is to use the new Decision notification listener. Activate notification listener will be removed in the next major release.

## 3.0.0
March 1st, 2019

The 3.0 release improves event tracking and supports additional audience targeting functionality.

### New Features:
* Event tracking:
  * The `track` method now dispatches its conversion event _unconditionally_, without first determining whether the user is targeted by a known experiment that uses the event. This may increase outbound network traffic.
  * In Optimizely results, conversion events sent by 3.0 SDKs don't explicitly name the experiments and variations that are currently targeted to the user. Instead, conversions are automatically attributed to variations that the user has previously seen, as long as those variations were served via 3.0 SDKs or by other clients capable of automatic attribution, and as long as our backend actually received the impression events for those variations.
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
* Conversion events sent by 3.0 SDKs don't explicitly name the experiments and variations that are currently targeted to the user, so these events are unattributed in raw events data export. You must use the new _results_ export to determine the variations to which events have been attributed.
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
* fix(whitelisting): Removed logic from bucketing since it is checked in Decision Service. ([#98](https://github.com/optimizely/csharp-sdk/pull/98))
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
