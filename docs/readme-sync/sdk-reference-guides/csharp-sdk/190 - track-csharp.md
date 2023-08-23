# Track

Tracks a conversion event. Logs an error message if the specified event key doesn't match any existing events.

## Version

SDK v3.0, v3.1

## Description

Use this method to track events across multiple experiments. You should only send one tracking event per conversion, even if many feature tests or A/B tests are measuring it.

**Note**: Events are counted in an experiment when an impression was sent as a result of the `Activate` or `Is Feature Enabled` method being called.

The attributes passed to `Track` are only used for results segmentation.

## Parameters

This table lists the required and optional parameters for the C# SDK.

| Parameter        | Type           | Description |
| ---------------- | -------------- | ----------- |
| **eventKey**     | string         | The key of the event to be tracked. This key must match the event key provided when the event was created in the Optimizely app. |
| **userId**       | string         | The ID of the user associated with the event being tracked. **Important**: This ID must match the user ID provided to `Activate` or `Is Feature Enabled`. |
| **userAttributes** | map (optional) | A map of custom key-value string pairs specifying attributes for the user that are used for results segmentation. Non-string values are only supported in the 3.0 SDK and above. |
| **eventTags**    | map (optional) | A map of key-value pairs specifying tag names and their corresponding tag values for this particular event occurrence. Values can be strings, numbers, or booleans. These can be used to track numeric metrics, allowing you to track actions beyond conversions, such as revenue, load time, or total value. [See details on reserved tag keys.](https://docs.developers.optimizely.com/full-stack/docs/include-event-tags#section-reserved-tag-keys) |

## Returns

This method sends conversion data to Optimizely. It doesn't provide return values.

## Example

```csharp
using OptimizelySDK.Entity;

var attributes = new UserAttributes {
  { "device", "iPhone" },
  { "lifetime", 24738388 },
  { "is_logged_in", true },
};

var tags = new EventTags {
  { "category", "shoes" },
  { "count", 2 },
};

optimizelyClient.Track("my_purchase_event_key", "user_123", attributes, tags);

```
# Side effects

The table lists other Optimizely functionality that may be triggered by using this method.

## Functionality | Description

- **Conversions**  
  Calling this method records a conversion and attributes it to the variations that the user has seen.  
  Optimizely Feature Experimentation 3.x supports retroactive metrics calculation. You can create [metrics](doc:choose-metrics) on this conversion event and add metrics to experiments even after the conversion has been tracked.  
  For more information, see the paragraph **Events are always on** in the introduction of [Events: Tracking clicks, pageviews, and other visitor actions](https://help.optimizely.com/Measure_success%3A_Track_visitor_behaviors/Events%3A_Tracking_clicks%2C_pageviews%2C_and_other_visitor_actions).  
  **Important!**  
   - This method won't track events when the specified event key is invalid.  
   - Changing the traffic allocation of running experiments affects how conversions are recorded and variations are attributed to users.

- **Impressions**  
  Track doesn't trigger impressions.

- **Notification Listeners**  
  Accessing this method triggers a call to the `TRACK` notification listener.  
  **Important!** This method won't call the `TRACK` notification listener when the specified event key is invalid.

# Source files

The language/platform source files containing the implementation for C# is [Optimizely.cs](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Optimizely.cs).
