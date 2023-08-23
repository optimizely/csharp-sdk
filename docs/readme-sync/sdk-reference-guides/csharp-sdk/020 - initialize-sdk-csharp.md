---
title: "Initialize SDK"
slug: "initialize-sdk-csharp"
hidden: false
createdAt: "2019-09-11T14:15:47.848Z"
updatedAt: "2020-04-10T00:11:24.283Z"
---

# Initialize SDK

Use the `instantiate` method to initialize the C# SDK and instantiate an instance of the Optimizely client class that exposes API methods like [Get Enabled Features](doc:get-enabled-features-csharp). Each client corresponds to the datafile representing the state of a project for a certain environment.

## Version

SDK v3.2.0

## Description

The constructor accepts a configuration object to configure Optimizely.

Some parameters are optional because the SDK provides a default implementation, but you may want to override these for your production environments. For example, you may want override these to set up an [error handler](doc:customize-error-handler-csharp) and [logger](doc:customize-logger-csharp) to catch issues, an event dispatcher to manage network calls, and a User Profile Service to ensure sticky bucketing.

## Parameters

The table below lists the required and optional parameters in C#.

| Parameter            | Type                 | Description                                              |
|----------------------|----------------------|----------------------------------------------------------|
| **datafile**         | *optional* string    | The JSON string representing the project.                |
| **configManager**    | *optional* ProjectConfigManager | The project config manager provides the project config to the client. |
| **eventDispatcher**  | *optional* IEventDispatcher | An event handler to manage network calls.               |
| **logger**           | *optional* ILogger   | A logger implementation to log issues.                   |
| **errorHandler**     | *optional* IErrorHandler | An error handler object to handle errors.              |
| **userProfileService** | *optional* UserProfileService | A user profile service.                               |
| **skipJsonValidation** | *optional* boolean | Specifies whether the JSON should be validated.         |

## Returns

Instantiates an instance of the Optimzely class.

## Automatic datafile management (ADM)

Optimizely provides out-of-the-box functionality to dynamically manage datafiles (configuration files) on either the client or the server. The C# SDK provides default implementations of an Optimizely `ProjectConfigManager`. The package also includes a factory class, OptimizelyFactory, which you can use to instantiate the Optimizely SDK with the default configuration of HttpProjectConfigManager.

Whenever the experiment configuration changes, the SDK uses automatic datafile management (ADM) to handle the change for you. In the C# SDK, you can provide either `sdkKey` or `datafile` or both.

- When initializing with just the SDK key, the SDK will poll for datafile changes in the background at regular intervals.
- When initializing with just the datafile, the SDK will NOT poll for datafile changes in the background.
- When initializing with both the SDK key and datafile, the SDK will use the given datafile and start polling for datafile changes in the background.

### Basic example

The following code example shows basic C# ADM usage.

```csharp
using OptimizelySDK;

public class App
{
    public static void Main(string[] args)
    {
        string sdkKey = args[0];
        Optimizely optimizely = OptimizelyFactory.NewDefaultInstance(sdkKey);
    }
}
```
# Advanced examples

> If you are configuring a logger, make sure to pass it into the `ProjectConfigManager` instance as well.

In the C# SDK, you only need to pass the SDK key value to instantiate a client. Whenever the experiment configuration changes, the SDK handles the change for you.

Include `sdkKey` as a string property in the options object you pass to the `createInstance` method.

When you provide the `sdkKey`, the SDK instance downloads the datafile associated with that `sdkKey`. When the download completes, the SDK instance updates itself to use the downloaded datafile.

> Pass all components (Logger, ErrorHandler, NotificationCenter) to the Optimizely constructor. Not passing a component will fail to enable its respective functionality. In other words, components only work when passed to the constructor.

```csharp
// Initialize with SDK key and default configuration
var sdkKey = "<Your_SDK_Key>" // replace with your own SDK Key
Optimizely optimizely = OptimizelyFactory.newDefaultInstance(sdkKey);

// You can also customize the SDK instance with custom configuration. In this example we are customizing the project config manager to poll every 5 minutes for the datafile.
var projectConfigManager =
  new HttpProjectConfigManager.Builder()
    .WithSdkKey(sdkKey)
    .WithPollingInterval(TimeSpan.FromMinutes(5))
    // .WithLogger(logger) - this is needed if you are configuring a logger for the optimizely instance
    // .WithErrorHandler(errorHandler) - this is needed if you are configuring an errorhandler for the optimizely instance.
    // .WithNotificationCenter(notificationCenter) this is needed if you are subscribing config update
    .Build();

var Optimizely = new Optimizely(projectConfigManager);

// Initialize with Logger
// var Optimizely = new Optimizely(projectConfigManager, logger: logger);

// Initialize with Logger, ErrorHandler
// var Optimizely = new Optimizely(projectConfigManager, errorHandler: errorHandler, logger: logger);

// Initialize with NotificationCenter, Logger, ErrorHandler
// var Optimizely = new Optimizely(projectConfigManager, notificationCenter: NotificationCenter, errorHandler: errorHandler, logger: logger);

// Note: Use OptimizelyFactory NewDefaultInstance method to use same logger, errorHandler and notificationCenter for all of its Components (Optimizely, EventProcessor, HttpProjectConfigManager)
```
# Advanced Configuration for C# ADM

This code example demonstrates advanced configuration for C# ADM (Application Development Manager). The advanced configuration properties are described in the sections below. This example showcases how to construct individual components directly to override various configurations, allowing full control over which implementations to use and how to use them.

## Code Example

```csharp
using OptimizelySDK;
using OptimizelySDK.Config;

public class App
{
    public static void Main(string[] args)
    {
        string sdkKey = args[0];
        // You can also use your own implementation of the ProjectConfigManager interface
        ProjectConfigManager projectConfigManager =
            new HttpProjectConfigManager.Builder()
                .WithSdkKey(sdkKey)
                .WithPollingInterval(TimeSpan.FromMinutes(1))
                .Build();

        Optimizely optimizely = new Optimizely(configManager);
    }
}
```
## HttpProjectConfigManager

[HttpProjectConfigManager](https://github.com/optimizely/csharp-sdk/blob/fahad/dfm-readme/OptimizelySDK/Config/HttpProjectConfigManager.cs) is an implementation of the abstract [PollingProjectConfigManager](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Config/PollingProjectConfigManager.cs). The `Poll` method is extended and makes an HTTP `GET` request to the configured URL to asynchronously download the project datafile and initialize an instance of the `ProjectConfig`.

By default, `HttpProjectConfigManager` will block until the first successful datafile retrieval, up to a configurable timeout. You can set the frequency of the polling method and the blocking timeout using the `HttpProjectConfigManager.Builder`.

```csharp
ProjectConfigManager projectConfigManager =
    new HttpProjectConfigManager.Builder()
        .WithSdkKey(sdkKey)
        .WithPollingInterval(TimeSpan.FromMinutes(1))
        .Build();
```
#### SDK key

The SDK key is used to compose the outbound HTTP request to the default datafile location on the Optimizely CDN.

#### Polling interval

The polling interval is used to specify a fixed delay between consecutive HTTP requests for the datafile. The valid interval duration is between 1 to 4294967294 milliseconds.

#### Initial datafile

You can provide an initial datafile via the builder to bootstrap the `ProjectConfigManager` so that it can be used immediately without blocking execution. The initial datafile also serves as a fallback datafile if HTTP connection cannot be established. This is useful in mobile environments, where internet connectivity is not guaranteed.

The initial datafile will be discarded after the first successful datafile poll.

## Builder Methods

Use the following builder methods to customize the `HttpProjectConfigManager` configuration:

| Property                          | Default value | Description                                                                                     |
|-----------------------------------|---------------|-------------------------------------------------------------------------------------------------|
| **WithDatafile(string)**          | null          | Initial datafile, typically sourced from a local cached source.                                |
| **WithUrl(string)**               | null          | URL override location used to specify a custom HTTP source for the Optimizely datafile.         |
| **WithFormat(string)**            | null          | Parameterized datafile URL by SDK key.                                                          |
| **WithPollingInterval(TimeSpan)** | 5 minutes     | Fixed delay between fetches for the datafile.                                                   |
| **WithBlockingTimeoutPeriod(TimeSpan)** | 15 seconds | Maximum time to wait for initial bootstrapping. The valid timeout duration is 1 to 4294967294 milliseconds. |
| **WithSdkKey(string)**            | null          | Optimizely project SDK key; required unless the source URL is overridden.                        |

## Update Config Notifications

A notification signal will be triggered whenever a new datafile is fetched. To subscribe to these notifications, use the method `NotificationCenter.AddNotification()`.

```csharp
optimizely.NotificationCenter.AddNotification(
    NotificationCenter.NotificationType.OptimizelyConfigUpdate,
    () => Console.WriteLine("Received new datafile configuration")
);
```
## OptimizelyFactory

[OptimizelyFactory](https://github.com/optimizely/csharp-sdk/blob/fahad/dfm-readme/OptimizelySDK/OptimizelyFactory.cs) provides a basic utility to instantiate the Optimizely SDK with a minimal number of configuration options.

OptimizelyFactory does not capture all configuration and initialization options. For more use cases, consider building the necessary resources using their constructors.

To instantiate the Optimizely SDK, you need to provide the SDK key at runtime directly via the factory method:

```csharp
Optimizely optimizely = OptimizelyFactory.NewDefaultInstance(<<SDK_KEY>>);
```
### Instantiate using datafile

You can also instantiate with a hard-coded datafile. If you don't pass in an SDK key, the Optimizely Client will not automatically sync newer versions of the datafile. Any time you retrieve an updated datafile, just re-instantiate the same client.

For simple applications, all you need to provide to instantiate a client is a datafile specifying the project configuration for a given environment. For most advanced implementations, you'll want to [customize the logger](doc:customize-logger-csharp) or [error handler](doc:customize-error-handler-csharp) for your specific requirements.

```csharp
using OptimizelySDK;

// Instantiate an Optimizely client
var datafile = "<Datafile_JSON_string>";
Optimizely OptimizelyClient = new Optimizely(datafile);
````

### Source files

The language/platform source files containing the implementation for C# can be found at [Optimizely.cs](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Optimizely.cs).
