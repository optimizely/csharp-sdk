---
title: "Track"
slug: "track-csharp"
hidden: false
createdAt: "2019-09-12T13:53:40.495Z"
updatedAt: "2020-02-21T19:34:45.905Z"
---
Tracks a conversion event. Logs an error message if the specified event key doesn't match any existing events.
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
Use this method to track events across multiple experiments. You should only send one tracking event per conversion, even if many feature tests or A/B tests are measuring it.
[block:callout]
{
  "type": "info",
  "title": "Note",
  "body": "Events are counted in an experiment when an impression was sent as a result of the [Activate](doc:activate-csharp) or [Is Feature Enabled](doc:is-feature-enabled-csharp) method being called."
}
[/block]
The attributes passed to Track are only used for [results segmentation](doc:analyze-results#section-segment-results).
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
    "0-0": "**eventKey**\n*required*",
    "0-1": "string",
    "1-0": "**userId**\n*required*",
    "1-1": "string",
    "2-0": "**userAttributes**\n*optional*",
    "3-0": "**eventTags**\n*optional*",
    "3-1": "map",
    "2-1": "map",
    "0-2": "The key of the event to be tracked. This key must match the event key provided when the event was created in the Optimizely app.",
    "1-2": "The ID of the user associated with the event being tracked. \n\n**Important**: This ID must match the user ID provided to Activate or Is Feature Enabled.",
    "2-2": "A map of custom key-value string pairs specifying attributes for the user that are used for [results segmentation](doc:analyze-results#section-segment-results). Non-string values are only supported in the 3.0 SDK and above.",
    "3-2": "A map of key-value pairs specifying tag names and their corresponding tag values for this particular event occurrence. Values can be strings, numbers, or booleans.\n\nThese can be used to track numeric metrics, allowing you to track actions beyond conversions, for example: revenue, load time, or total value. [See details on reserved tag keys.](https://docs.developers.optimizely.com/full-stack/docs/include-event-tags#section-reserved-tag-keys)"
  },
  "cols": 3,
  "rows": 4
}
[/block]

[block:api-header]
{
  "title": "Returns"
}
[/block]
This method sends conversion data to Optimizely. It doesn't provide return values. 
[block:api-header]
{
  "title": "Example"
}
[/block]

[block:code]
{
  "codes": [
    {
      "code": "using OptimizelySDK.Entity;\n\nvar attributes = new UserAttributes {\n  { \"device\", \"iPhone\" },\n  { \"lifetime\", 24738388 },\n  { \"is_logged_in\", true },\n};\n\nvar tags = new EventTags {\n  { \"category\", \"shoes\" },\n  { \"count\", 2 },\n};\n\noptimizelyClient.Track(\"my_purchase_event_key\", \"user_123\", attributes, tags);\n\n",
      "language": "csharp"
    }
  ]
}
[/block]

[block:api-header]
{
  "title": "Side effects"
}
[/block]
The table lists other other Optimizely functionality that may be triggered by using this method.
[block:parameters]
{
  "data": {
    "h-0": "Functionality",
    "h-1": "Description",
    "0-0": "Conversions",
    "0-1": "Calling this method records a conversion and attributes it to the variations that the user has seen.\n \nOptimizely Feature Experimentation 3.x supports retroactive metrics calculation. You can create [metrics](doc:choose-metrics) on this conversion event and add metrics to experiments even after the conversion has been tracked.\n\nFor more information, see the paragraph **Events are always on** in the introduction of [Events: Tracking clicks, pageviews, and other visitor actions](https://help.optimizely.com/Measure_success%3A_Track_visitor_behaviors/Events%3A_Tracking_clicks%2C_pageviews%2C_and_other_visitor_actions).\n\n**Important!** \n - This method won't track events when the specified event key is invalid.\n - Changing the traffic allocation of running experiments affects how conversions are recorded and variations are attributed to users.",
    "1-0": "Impressions",
    "1-1": "Track doesn't trigger impressions.",
    "2-0": "Notification Listeners",
    "2-1": "Accessing this method triggers a call to the  `TRACK` notification listener. \n\n**Important!** This method won't call the `TRACK` notification listener when the specified event key is invalid."
  },
  "cols": 2,
  "rows": 3
}
[/block]

[block:api-header]
{
  "title": "Source files"
}
[/block]
The language/platform source files containing the implementation for C# is [Optimizely.cs](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Optimizely.cs).