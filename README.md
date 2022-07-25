# Optimizely C# SDK
![Semantic](https://img.shields.io/badge/sem-ver-lightgrey.svg?style=plastic)
[![Build Status](https://travis-ci.org/optimizely/csharp-sdk.svg?branch=master)](https://travis-ci.org/optimizely/csharp-sdk)
[![NuGet](https://img.shields.io/nuget/v/Optimizely.SDK.svg?style=plastic)](https://www.nuget.org/packages/Optimizely.SDK/)
[![Apache 2.0](https://img.shields.io/github/license/nebula-plugins/gradle-extra-configurations-plugin.svg)](http://www.apache.org/licenses/LICENSE-2.0)

This repository houses the .Net based C# SDK for use with Optimizely Full Stack and Optimizely Rollouts.

Optimizely Full Stack is A/B testing and feature flag management for product development teams. Experiment in any application. Make every feature on your roadmap an opportunity to learn. Learn more at https://www.optimizely.com/platform/full-stack/, or see the [documentation](https://docs.developers.optimizely.com/experimentation/v4.0.0-full-stack/docs/welcome).

Optimizely Rollouts is free feature flags for development teams. Easily roll out and roll back features in any application without code deploys. Mitigate risk for every feature on your roadmap. Learn more at https://www.optimizely.com/rollouts/, or see the [documentation](https://docs.developers.optimizely.com/experimentation/v3.1.0-full-stack/docs/introduction-to-rollouts).

## Getting Started

### Installing the SDK

The SDK can be installed through [NuGet](https://www.nuget.org):

```
PM> Install-Package Optimizely.SDK
```

An ASP.Net MVC sample project demonstrating how to use the SDK is available as well:

```
PM> Install-Package Optimizely.SDK.Sample
```

Simply compile and run the Sample application to see it in use.
Note that the way the Demo App stores data in memory is not recommended for production use
and is merely illustrates how to use the SDK.

### Using the SDK

#### Documentation

See the Optimizely Full Stack C# SDK [developer documentation](https://docs.developers.optimizely.com/experimentation/v4.0.0-full-stack/docs/csharp-sdk) to learn how to set up your first Full Stack project and use the SDK.

#### Initialization

Create the Optimizely Client, for example:

```
private static Optimizely Optimizely =
    new Optimizely(
        datafile: myProjectConfig,
        eventDispatcher: myEventDispatcher,
        logger: myLogger,
        errorHandler: myErrorHandler,
        skipJsonValidation: false);
```

Since this class parses the Project Config file, you should not create this per request.

#### APIs

This class exposes three main calls:
1. Activate
2. Track
3. GetVariation

Activate and Track are used in the demonstration app.  See the Optimizely documentation regarding how to use these.

#### Plug-in Interfaces

The Optimizely client object accepts the following plug-ins:
1. `IEventDispatcher` handles the HTTP requests to Optimizely.  The default implementation is an asynchronous "fire and forget".
2. `ILogger` exposes a single method, Log, to record activity in the SDK.  An example of a class to bridge the SDK's Log to Log4Net is provided in the Demo Application.
3. `IErrorHandler` allows you to implement custom logic when Exceptions are thrown.  Note that Exception information is already included in the Log.
4. `ProjectConfigManager` exposes method for retrieving ProjectConfig instance. Examples include `FallbackProjectConfigManager` and `HttpProjectConfigManager`.
5. `EventProcessor` provides an intermediary processing stage within event production. It's assumed that the EventProcessor dispatches events via a provided EventDispatcher. Examples include `ForwardingEventProcessor` and `BatchEventProcessor`.
These are optional plug-ins and default behavior is implement if none are provided.

#### OptimizelyFactory

[`OptimizelyFactory`](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/OptimizelyFactory.cs)
provides basic utility to instantiate the Optimizely SDK with a minimal number of configuration options.

`OptimizelyFactory` does not capture all configuration and initialization options. For more use cases,
build the resources via their respective builder classes.

##### Use OptimizelyFactory

You must provide the SDK key at runtime, either directly via the factory method:
```
Optimizely optimizely = OptimizelyFactory.newDefaultInstance(<<SDK_KEY>>);
```

You can also provide default datafile with the SDK key.
```
Optimizely optimizely = OptimizelyFactory.newDefaultInstance(<<SDK_KEY>>, <<Fallback>>);
```
##### Using App.config in OptimizelyFactory

OptimizelyFactory provides support of setting configuration variables in App.config:
User can provide variables using following procedure:
1. In App.config file of your project in **<configuration>** add following:
```
<configSections>
    <section name="optlySDKConfigSection"
             type="OptimizelySDK.OptimizelySDKConfigSection, OptimizelySDK, Version=3.2.0.0, Culture=neutral, PublicKeyToken=null" />
  </configSections>
```
2. Now add **optlySDKConfigSection** below **<configSections>**. In this section you can add and set following **HttpProjectConfigManager** and **BatchEventProcessor** variables: 
```
<optlySDKConfigSection>
  
    <HttpProjectConfig sdkKey="43214321" 
                       url="www.testurl.com" 
                       format="https://cdn.optimizely.com/data/{0}.json" 
                       pollingInterval="2000" 
                       blockingTimeOutPeriod="10000" 
                       datafileAccessToken="testingtoken123"
                       autoUpdate="true" 
                       defaultStart="true">
    </HttpProjectConfig>

    <BatchEventProcessor batchSize="10"
                         flushInterval="2000"
                         timeoutInterval="10000"
                         defaultStart="true">
    </BatchEventProcessor>
  
  </optlySDKConfigSection>
```
3. After setting these variables you can instantiate the Optimizely SDK using function:
```
Optimizely optimizely = OptimizelyFactory.newDefaultInstance();
```

#### BatchEventProcessor
[BatchEventProcessor](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Event/BatchEventProcessor.cs) is a batched implementation of the [EventProcessor](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Event/EventProcessor.cs)
     * Events passed to the BatchEventProcessor are immediately added to a BlockingQueue.
     * The BatchEventProcessor maintains a single consumer thread that pulls events off of the BlockingQueue and buffers them for either a configured batch size or for a maximum duration before the resulting LogEvent is sent to the NotificationManager.
##### Use BatchEventProcessor

```
EventProcessor eventProcessor = new BatchEventProcessor.Builder()
                .WithMaxBatchSize(MaxEventBatchSize)
                .WithFlushInterval(MaxEventFlushInterval)
                .WithEventDispatcher(eventDispatcher)
                .WithNotificationCenter(notificationCenter)
                .Build();
```

##### Max Event Batch Size

The Max event batch size is used to limit eventQueue batch size and events will be dispatched when limit reaches.

##### Flush Interval

The FlushInterval is used to specify a delay between consecutive flush events call. Event batch will be dispatched after meeting this specified timeSpan.

##### Event Dispatcher 

Custom EventDispatcher can be passed.

##### Notification Center

Custom NotificationCenter can be passed. 

#### HttpProjectConfigManager

[`HttpProjectConfigManager`](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Config/HttpProjectConfigManager.cs)
is an implementation of the abstract [`PollingProjectConfigManager`](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Config/PollingProjectConfigManager.cs).
The `Poll` method is extended and makes an HTTP GET request to the configured URL to asynchronously download the
project datafile and initialize an instance of the ProjectConfig.

By default, `HttpProjectConfigManager` will block until the first successful datafile retrieval, up to a configurable timeout.
Set the frequency of the polling method and the blocking timeout with `HttpProjectConfigManager.Builder`.

##### Use HttpProjectConfigManager

```
HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
	.WithSdkKey(sdkKey)
	.WithPollingInterval(TimeSpan.FromMinutes(1))
	.Build();
```

##### SDK key

The SDK key is used to compose the outbound HTTP request to the default datafile location on the Optimizely CDN.

##### Polling interval

The polling interval is used to specify a fixed delay between consecutive HTTP requests for the datafile. Between 1 to 4294967294 miliseconds is valid duration. Otherwise default 5 minutes will be used.

##### Blocking Timeout Period

The blocking timeout period is used to specify a maximum time to wait for initial bootstrapping. Between 1 to 4294967294 miliseconds is valid blocking timeout period. Otherwise default value 15 seconds will be used.

##### Initial datafile

You can provide an initial datafile via the builder to bootstrap the `ProjectConfigManager` so that it can be used immediately without blocking execution.

##### URL

The URL is used to specify the location of datafile.

##### Format

This option enables user to provide a custom URL format to fetch the datafile.

##### Start by default

This option is used to specify whether to start the config manager on initialization or not. If no value is provided, by default it is true and will start polling datafile from remote immediately.

##### Datafile access token

This option is used to provide token for datafile belonging to a secure environment.

## Development

### Unit tests

The sample project contains unit tests as well which can be run from the built-in Visual Studio Test Runner.

### Contributing

Please see [CONTRIBUTING](CONTRIBUTING.md).

## Third Party Licenses

Optimizely SDK uses third party software:
[murmurhash-signed](https://www.nuget.org/packages/murmurhash-signed/),
[Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/), and
[NJsonSchema](https://www.nuget.org/packages/NJsonSchema/).
