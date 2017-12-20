## 1.3.0
December 19, 2017

### New Features
* Feature Notification Center
* Third party component DLL's dependencies unbundled to NUGET.ORG .

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
