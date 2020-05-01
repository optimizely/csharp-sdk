---
title: "Configure event dispatcher"
slug: "configure-event-dispatcher-csharp"
hidden: true
createdAt: "2019-09-12T13:43:55.726Z"
updatedAt: "2019-09-12T13:45:58.817Z"
---
The Optimizely SDKs make HTTP requests for every impression or conversion that gets triggered. Each SDK has a built-in **event dispatcher** for handling these events, but we recommend overriding it based on the specifics of your environment.

The C# SDK has an out-of-the-box asynchronous dispatcher. We recommend customizing the event dispatcher you use in production to ensure that you queue and send events in a manner that scales to the volumes handled by your application. Customizing the event dispatcher allows you to take advantage of features like batching, which makes it easier to handle large event volumes efficiently or to implement retry logic when a request fails. You can build your dispatcher from scratch or start with the provided dispatcher.

The examples show that to customize the event dispatcher, initialize the Optimizely client (or manager) with an event dispatcher instance.
[block:code]
{
  "codes": [
    {
      "code": "using OptimizelySDK;\nusing OptimizelySDK.Event.Dispatcher;\n\n// Create an Optimizely client with the default event dispatcher\n\tOptimizely OptimizelyClient = new Optimizely(\n\t\t\tdatafile: datafile,\n\t\t\teventDispatcher: new DefaultEventDispatcher(new OptimizelySDK.Logger.DefaultLogger()));\n\n",
      "language": "csharp"
    }
  ]
}
[/block]
The event dispatcher should implement a `dispatchEvent` function, which takes in three arguments: `httpVerb`, `url`, and `params`, all of which are created by the internal `EventBuilder` class. In this function, you should send a `POST` request to the given `url` using the `params` as the body of the request (be sure to stringify it to JSON) and `{content-type: 'application/json'}` in the headers.
[block:callout]
{
  "type": "warning",
  "title": "Important",
  "body": "If you are using a custom event dispatcher, do not modify the event payload returned from Optimizely. Modifying this payload will alter your results."
}
[/block]