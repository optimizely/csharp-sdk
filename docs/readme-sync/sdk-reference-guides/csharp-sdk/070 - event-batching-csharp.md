---
title: "Event batching"
slug: "event-batching-csharp"
hidden: false
createdAt: "2019-09-12T13:44:04.059Z"
updatedAt: "2019-12-13T00:25:39.892Z"
---

The [Optimizely Feature Experimentation C# SDK](https://github.com/optimizely/csharp-sdk) now supports event batching, a feature that reduces the number of outbound requests to Optimizely by batching impression and conversion events into a single payload. This is achieved through a new SDK component called the event processor.

Event batching has the advantage of reducing the number of outbound requests to Optimizely depending on how you define, configure, and use the event processor. It means less network traffic for the same number of Impression and conversion events tracked.

In the C# SDK, `BatchEventProcessor` provides implementation of the `EventProcessor` interface and batches events. You can control batching based on two parameters:

- Batch size: Defines the number of events that are batched together before sending to Optimizely.
- Flush interval: Defines the amount of time after which any batched events should be sent to Optimizely.

An event consisting of the batched payload is sent as soon as the batch size reaches the specified limit or flush interval reaches the specified time limit. `BatchEventProcessor` options are described in more detail below.

> **Note**
> Event batching works with both out-of-the-box and custom event dispatchers.
> The event batching process doesn't remove any personally identifiable information (PII) from events. You must still ensure that you aren't sending any unnecessary PII to Optimizely.
### Basic example

You can create an Optimizely Client using the `OptimizelyFactory.NewDefaultInstance` method. Here's a basic example of how to do this:

```csharp
using OptimizelySDK;

class App
{
    static void Main(string[] args)
    {
        string sdkKey = args[0];
        // Returns Optimizely Client
        OptimizelyFactory.NewDefaultInstance(sdkKey);
    }
}
```
### Advanced Example

In this advanced example, you can customize the batch size and flush interval of the `BatchEventProcessor` in the Optimizely Client.

```csharp
using OptimizelySDK;

class App
{
    static void Main(string[] args)
    {
        string sdkKey = args[0];

        ProjectConfigManager projectConfigManager = HttpProjectConfigManager.builder()
            .WithSdkKey(sdkKey)
            .Build();

        BatchEventProcessor batchEventProcessor = new BatchEventProcessor.Builder()
            .WithMaxBatchSize(10)  // Set the batch size to 10
            .WithFlushInterval(TimeSpan.FromSeconds(30))  // Set the flush interval to 30 seconds
            .Build();

        Optimizely optimizely = new Optimizely(
            projectConfigManager,
            ..  // Other Params
            ..batchEventProcessor
        );
    }
}
```
### BatchEventProcessor

`BatchEventProcessor` is an implementation of the `EventProcessor` interface that batches events. It maintains a single consumer thread that pulls events from a `BlockingCollection` and buffers them either until a configured batch size is reached or a maximum duration elapses. Once the batch size or time limit is met, the resulting `LogEvent` is sent to the `EventDispatcher` and `NotificationCenter`.

You can customize the configuration of `BatchEventProcessor` using its Builder class. Here are the configurable properties:

- **EventDispatcher**: The event dispatcher used to dispatch event payload to Optimizely. (Default: DefaultEventDispatcher)
- **BatchSize**: The maximum number of events to batch before dispatching. Once this number is reached, all queued events are flushed and sent to Optimizely. (Default: 10)
- **FlushInterval**: Milliseconds to wait before batching and dispatching events. (Default: 30000, or 30 seconds)
- **EventQueue**: A `BlockingCollection` that queues individual events to be batched and dispatched by the executor. (Default: 1000)
- **NotificationCenter**: Notification center instance to be used to trigger any notifications. (Default: null)

These properties allow you to tailor the batch processing behavior based on your organization's requirements and resource availability.

Keep in mind that the batch processing mechanism works seamlessly with both the out-of-the-box and custom event dispatchers provided by Optimizely.

For more information, refer to the [Optimizely C# SDK documentation](https://github.com/optimizely/csharp-sdk).

For more information, see [Initialize SDK](doc:initialize-sdk-csharp).
### Side Effects

When using the `BatchEventProcessor` class, there are certain Optimizely functionalities that may be triggered. Here's a summary of those functionalities:

- **LogEvent**: Whenever the event processor produces a batch of events, a `LogEvent` object will be created using the `EventFactory`. This `LogEvent` object contains a batch of conversion and impression events. The object will be dispatched using the provided event dispatcher and will also be sent to the notification subscribers.

- **Notification Listeners**: The `Flush` method invokes the `LOGEVENT` [notification listener](doc:set-up-notification-listener-csharp) if this listener is subscribed to. This allows you to be notified when events are being flushed, providing insights into the event batching process.

For more information, you can refer to the [Optimizely C# SDK documentation](https://github.com/optimizely/csharp-sdk).

Also, for detailed instructions on initializing the SDK, please see the [Initialize SDK](doc:initialize-sdk-csharp) documentation.
### Registering LogEvent Listener

To register a `LogEvent` listener, you can use the following code snippet:

```csharp
NotificationCenter.AddNotification(
    NotificationType.LogEvent, 
    new LogEventCallback((logevent) => {
        // Your code here
    })
);
```
### LogEvent

The `LogEvent` object is created using [EventFactory](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Event/EventFactory.cs). It represents the batch of impression and conversion events that are sent to the Optimizely backend.

| Object    | Type                  | Description                                             |
|-----------|-----------------------|---------------------------------------------------------|
| **Url**   | Required (string)     | URL to dispatch the log event to.                      |
| **Params**| Required (Dictionary<string, object>) | Parameters to be set in the log event. It contains an [EventBatch](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Event/Entity/EventBatch.cs) of all `UserEvents` inside [Visitors](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Event/Entity/EventBatch.cs#L45). |
| **HttpVerb** | Required (string)   | The HTTP verb to use when dispatching the log event. It can be GET or POST. |
| **Headers** | Dictionary<string, string>  | Headers to be set when sending the request.           |

The `LogEvent` object encapsulates the information needed to send a batch of events to the Optimizely backend for processing. It plays a crucial role in the event batching and dispatching process.

For more details on the `LogEvent` object and its usage, you can refer to the [Optimizely C# SDK documentation](https://github.com/optimizely/csharp-sdk).


# Dispose Optimizely on application exit

If you enable event batching, it's important that you call the Close method (`optimizely.Dispose()`) prior to exiting. This ensures that queued events are flushed as soon as possible to avoid any data loss.

> **Important**
> Because the Optimizely client maintains a buffer of queued events, we recommend that you call `Dispose()` on the Optimizely instance before shutting down your application or whenever dereferencing the instance.

## Method: Dispose()

**Description**
Stops all timers and flushes the event queue. This method will also stop any timers that are happening for the data-file manager.
