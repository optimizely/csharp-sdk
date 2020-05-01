---
title: "Event batching"
slug: "event-batching-csharp"
hidden: true
createdAt: "2019-09-12T13:44:04.059Z"
updatedAt: "2019-12-13T00:25:39.892Z"
---
The [Optimizely Full Stack C# SDK](https://github.com/optimizely/csharp-sdk) now batches impression and conversion events into a single payload before sending it to Optimizely. This is achieved through a new SDK component called the event processor.

Event batching has the advantage of reducing the number of outbound requests to Optimizely depending on how you define, configure, and use the event processor. It means less network traffic for the same number of Impression and conversion events tracked.

In the C# SDK, `BatchEventProcessor` provides implementation of the `EventProcessor` interface and batches events. You can control batching based on two parameters:

- Batch size: Defines the number of events that are batched together before sending to Optimizely.
- Flush interval: Defines the amount of time after which any batched events should be sent to Optimizely.

An event consisting of the batched payload is sent as soon as the batch size reaches the specified limit or flush interval reaches the specified time limit. `BatchEventProcessor` options are described in more detail below.
[block:callout]
{
  "type": "info",
  "title": "Note",
  "body": "Event batching works with both out-of-the-box and custom event dispatchers.\n\nThe event batching process doesn't remove any personally identifiable information (PII) from events. You must still ensure that you aren't sending any unnecessary PII to Optimizely."
}
[/block]

[block:api-header]
{
  "title": "Basic example"
}
[/block]

[block:code]
{
  "codes": [
    {
      "code": "using OptimizelySDK;\n\nclass App\n{\n    static void Main(string[] args)\n    {\n        string sdkKey = args[0];\n        // Returns Optimizely Client\n        OptimizelyFactory.NewDefaultInstance(sdkKey);\n    }\n}",
      "language": "csharp"
    }
  ]
}
[/block]
By default, batch size is 10 and flush interval is 30 seconds.
[block:api-header]
{
  "title": "Advanced Example"
}
[/block]

[block:code]
{
  "codes": [
    {
      "code": "using OptimizelySDK;\n\nclass App\n{\n    static void Main(string[] args)\n    {\n        string sdkKey = args[0];\n        ProjectConfigManager projectConfigManager = HttpProjectConfigManager.builder()\n        .WithSdkKey(sdkKey)\n        .Build();\n\n        BatchEventProcessor batchEventProcessor = new BatchEventProcessor.Builder()\n            .WithMaxBatchSize(10)\n            .WithFlushInterval(TimeSpan.FromSeconds(30))\n            .Build();\n\n        Optimizely optimizely = new Optimizely(\n             projectConfigManager,\n                ..  // Other Params\n             ..batchEventProcessor\n           );\n    }\n}",
      "language": "csharp"
    }
  ]
}
[/block]

[block:api-header]
{
  "title": "BatchEventProcessor"
}
[/block]
`BatchEventProcessor` is an implementation of `EventProcessor` where events are batched. The class maintains a single consumer thread that pulls events off of the `BlockingCollection` and buffers them for either a configured batch size or a maximum duration before the resulting `LogEvent` is sent to the `EventDispatcher` and `NotificationCenter`.

The following properties can be used to customize the BatchEventProcessor configuration *using the Builder class*
[block:parameters]
{
  "data": {
    "h-0": "Property",
    "h-1": "Default value",
    "0-0": "**EventDispatcher**",
    "0-1": "DefautEventDispatcher",
    "1-1": "10",
    "1-0": "**BatchSize**",
    "h-2": "Description",
    "h-3": "Server",
    "0-2": "Used to dispatch event payload to Optimizely.",
    "1-2": "The maximum number of events to batch before dispatching. Once this number is reached, all queued events are flushed and sent to Optimizely.",
    "0-3": "Based on your organization's requirements.",
    "1-3": "Based on your organization's requirements.",
    "3-0": "**EventQueue**",
    "3-1": "1000",
    "3-2": "BlockingCollection that queues individual events to be batched and dispatched by the executor.",
    "2-0": "**FlushInterval**",
    "2-1": "30000 (30 Seconds)",
    "2-2": "Milliseconds to wait before batching and dispatching events.",
    "4-0": "**NotificationCenter**",
    "4-1": "null",
    "4-2": "Notification center instance to be used to trigger any notifications."
  },
  "cols": 3,
  "rows": 5
}
[/block]
For more information, see [Initialize SDK](doc:initialize-sdk-csharp).
[block:api-header]
{
  "title": "Side effects"
}
[/block]
The table lists other Optimizely functionality that may be triggered by using this class.
[block:parameters]
{
  "data": {
    "h-0": "Functionality",
    "h-1": "Description",
    "0-1": "Whenever the event processor produces a batch of events, a LogEvent object will be created using the EventFactory.\nIt contains batch of conversion and impression events. \nThis object will be dispatched using the provided event dispatcher and also it will be sent to the notification subscribers.",
    "1-1": "Flush invokes the LOGEVENT [notification listener](doc:set-up-notification-listener-csharp) if this listener is subscribed to.",
    "1-0": "Notification Listeners",
    "0-0": "[LogEvent](https://staging-optimizely-parent.readme.io/staging-optimizely-full-stack/docs/logevent-c#)"
  },
  "cols": 2,
  "rows": 2
}
[/block]
### Registering LogEvent listener

To register a LogEvent listener
[block:code]
{
  "codes": [
    {
      "code": "NotificationCenter.AddNotification(\n  \t\t\t\t\t\t\t\t\t\tNotificationType.LogEvent, \n                  \t  new LogEventCallback((logevent) => {\n                \t\t    // Your code here\n            \t\t\t\t\t})\n                  );",
      "language": "csharp"
    }
  ]
}
[/block]
###  LogEvent

LogEvent object gets created using [EventFactory](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Event/EventFactory.cs).It represents the batch of impression and conversion events we send to the Optimizely backend.
[block:parameters]
{
  "data": {
    "h-0": "Object",
    "h-1": "Type",
    "h-2": "Description",
    "0-0": "**Url**\nRequired",
    "0-1": " string ",
    "0-2": "URL to dispatch log event to.",
    "1-2": "Parameters to be set in the log event. It contains [EventBatch](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Event/Entity/EventBatch.cs) of all UserEvents inside [Visitors](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Event/Entity/EventBatch.cs#L45).",
    "2-2": "The HTTP verb to use when dispatching the log event. It can be GET or POST.",
    "3-2": "Headers to be set when sending the request.",
    "3-0": "**Headers** ",
    "2-0": "**HttpVerb**\nRequired",
    "1-0": "**Params**\nRequired",
    "1-1": "Dictionary<string, object>",
    "3-1": "Dictionary<string, string> Headers",
    "2-1": "string"
  },
  "cols": 3,
  "rows": 4
}
[/block]

[block:api-header]
{
  "title": "Dispose Optimizely on application exit"
}
[/block]
If you enable event batching, it's important that you call the Close method (`optimizely.Dispose()`) prior to exiting. This ensures that queued events are flushed as soon as possible to avoid any data loss.
[block:callout]
{
  "type": "warning",
  "title": "Important",
  "body": "Because the Optimizely client maintains a buffer of queued events, we recommend that you call `Dispose()` on the Optimizely instance before shutting down your application or whenever dereferencing the instance."
}
[/block]

[block:parameters]
{
  "data": {
    "0-0": "**Dispose()**",
    "h-0": "Method",
    "h-1": "Description",
    "0-1": "Stops all timers and flushes the event queue. This method will also stop any timers that are happening for the data-file manager."
  },
  "cols": 2,
  "rows": 1
}
[/block]