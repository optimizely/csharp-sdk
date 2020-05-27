---
title: "Initialize SDK"
slug: "initialize-sdk-csharp"
hidden: false
createdAt: "2019-09-11T14:15:47.848Z"
updatedAt: "2020-04-10T00:11:24.283Z"
---
Use the `instantiate` method to initialize the C# SDK and instantiate an instance of the Optimizely client class that exposes API methods like [Get Enabled Features](doc:get-enabled-features-csharp). Each client corresponds to the datafile representing the state of a project for a certain environment.
[block:api-header]
{
  "title": "Version"
}
[/block]
SDK v3.2.0
[block:api-header]
{
  "title": "Description"
}
[/block]
The constructor accepts a configuration object to configure Optimizely.

Some parameters are optional because the SDK provides a default implementation, but you may want to override these for your production environments. For example, you may want override these to set up an [error handler](doc:customize-error-handler-csharp) and [logger](doc:customize-logger-csharp) to catch issues, an event dispatcher to manage network calls, and a User Profile Service to ensure sticky bucketing.
[block:api-header]
{
  "title": "Parameters"
}
[/block]
The table below lists the required and optional parameters in C#.
[block:parameters]
{
  "data": {
    "h-0": "Parameter",
    "h-1": "Type",
    "h-2": "Description",
    "0-0": "**datafile**\n*optional* ",
    "0-1": "string",
    "0-2": "The JSON string representing the project.",
    "1-0": "**configManager**\n*optional*",
    "1-1": "ProjectConfigManager",
    "1-2": "The project config manager provides the project config to the client.",
    "2-0": "**eventDispatcher**\n*optional*",
    "2-1": "IEventDispatcher",
    "2-2": "An event handler to manage network calls.",
    "3-0": "**logger**\n*optional*",
    "3-1": "ILogger",
    "3-2": "A logger implementation to log issues.",
    "4-0": "**errorHandler**\n*optional*",
    "4-1": "IErrorHandler",
    "4-2": "An error handler object to handle errors.",
    "5-0": "**userProfileService**\n*optional*",
    "5-1": "UserProfileService",
    "5-2": "A user profile service.",
    "6-0": "**skipJsonValidation**\n*optional*",
    "6-1": "boolean",
    "6-2": "Specifies whether the JSON should be validated. Set to `true` to skip JSON validation on the schema, or `false` to perform validation."
  },
  "cols": 3,
  "rows": 7
}
[/block]

[block:api-header]
{
  "title": "Returns"
}
[/block]
Instantiates an instance of the Optimzely class.
[block:api-header]
{
  "title": "Automatic datafile management (ADM)"
}
[/block]
Optimizely provides out-of-the-box functionality to dynamically manage datafiles (configuration files) on either the client or the server. The C# SDK provides default implementations of an Optimizely `ProjectConfigManager`. The package also includes a factory class, OptimizelyFactory, which you can use to instantiate the Optimizely SDK with the default configuration of HttpProjectConfigManager.

Whenever the experiment configuration changes, the SDK uses automatic datafile management (ADM) to handle the change for you. In the C# SDK, you can provide either `sdkKey` or `datafile` or both.

* When initializing with just the SDK key, the SDK will poll for datafile changes in the background at regular intervals.
* When initializing with just the datafile, the SDK will NOT poll for datafile changes in the background.
* When initializing with both the SDK key and datafile, the SDK will use the given datafile and start polling for datafile changes in the background.

### Basic example

The following code example shows basic C# ADM usage.
[block:code]
{
  "codes": [
    {
      "code": "using OptimizelySDK;\n\npublic class App\n{\n    public static void Main(string[] args)\n    {\n        string sdkKey = args[0];\n     \t Optimizely optimizely = OptimizelyFactory.NewDefaultInstance(sdkKey);\n    }\n}\n\n",
      "language": "csharp"
    }
  ]
}
[/block]
### Advanced examples


[block:callout]
{
  "type": "warning",
  "body": "If you are configuring a logger, make sure to pass it into the `ProjectConfigManager` instance as well."
}
[/block]
In the C# SDK, you only need to pass the SDK key value to instantiate a client. Whenever the experiment configuration changes, the SDK handles the change for you.

Include `sdkKey` as a string property in the options object you pass to the `createInstance` method.

When you provide the `sdkKey`, the SDK instance downloads the datafile associated with that `sdkKey`. When the download completes, the SDK instance updates itself to use the downloaded datafile.
[block:callout]
{
  "type": "warning",
  "title": "",
  "body": "Pass all components (Logger, ErrorHandler, NotificationCenter) to the Optimizely constructor. Not passing a component will fail to enable its respective functionality. In other words, components only work when passed to the constructor."
}
[/block]

[block:code]
{
  "codes": [
    {
      "code": "// Initialize with SDK key and default configuration\nvar sdkKey = \"<Your_SDK_Key>\" // replace with your own SDK Key\nOptimizely optimizely = OptimizelyFactory.newDefaultInstance(sdkKey);\n\n\n// You can also customize the SDK instance with custom configuration. In this example we are customizing the project config manager to poll every 5 minutes for the datafile.\nvar projectConfigManager =\n  new HttpProjectConfigManager.Builder()\n    .WithSdkKey(sdkKey)\n    .WithPollingInterval(TimeSpan.FromMinutes(5))\n // .WithLogger(logger) - this is needed if you are configuring a logger for the optimizely instance\n// .WithErrorHandler(errorHandler) - this is needed if you are configuring an errorhandler for the optimizely instance.\n// .WithNotificationCenter(notificationCenter) this is needed if you are subscribing config update\n    .Build();\n\nvar Optimizely = new Optimizely(projectConfigManager);\n\n// Initialize with Logger\n// var Optimizely = new Optimizely(projectConfigManager, logger: logger);\n\n// Initialize with Logger, ErrorHandler\n// var Optimizely = new Optimizely(projectConfigManager, errorHandler: errorHandler, logger: logger);\n\n// Initialize with NotificationCenter, Logger, ErrorHandler\n// var Optimizely = new Optimizely(projectConfigManager, notificationCenter: NotificationCenter, errorHandler: errorHandler, logger: logger);\n\n// Note: Use OptimizelyFactory NewDefaultInstance method to use same logger, errorHandler and notificationCenter for all of its Components (Optimizely, EventProcessor, HttpProjectConfigManager)\n",
      "language": "csharp"
    }
  ]
}
[/block]
Here is a code example showing advanced configuration for C# ADM. Advanced configuration properties are described in the sections below. This advanced example shows how to construct the individual components directly to override various configurations. This gives you full control over which implementations to use and how to use them.
[block:code]
{
  "codes": [
    {
      "code": "using OptimizelySDK;\nusing OptimizelySDK.Config;\n\npublic class App\n{\n    public static void Main(string[] args)\n    {\n        string sdkKey = args[0];\n        // You can also use your own implementation of the ProjectConfigManager interface\n        ProjectConfigManager projectConfigManager =\n        new HttpProjectConfigManager.Builder()\n\t   .WithSdkKey(sdkKey)\n\t   .WithPollingInterval(TimeSpan.FromMinutes(1))\n\t   .Build();\n\n       Optimizely optimizely = new Optimizely(configManager);\n    }\n}\n\n",
      "language": "csharp"
    }
  ]
}
[/block]
### HttpProjectConfigManager

[HttpProjectConfigManager](https://github.com/optimizely/csharp-sdk/blob/fahad/dfm-readme/OptimizelySDK/Config/HttpProjectConfigManager.cs) is an implementation of the abstract [PollingProjectConfigManager](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Config/PollingProjectConfigManager.cs). The `Poll` method is extended and makes an HTTP `GET` request to the configured URL to asynchronously download the project datafile and initialize an instance of the `ProjectConfig`.

By default, `HttpProjectConfigManager` will block until the first successful datafile retrieval, up to a configurable timeout. Set the frequency of the polling method and the blocking timeout with `HttpProjectConfigManager.Builder`.
[block:code]
{
  "codes": [
    {
      "code": "ProjectConfigManager projectConfigManager =\n        new HttpProjectConfigManager.Builder()\n\t .WithSdkKey(sdkKey)\n\t .WithPollingInterval(TimeSpan.FromMinutes(1))\n\t .Build();\n\n",
      "language": "csharp"
    }
  ]
}
[/block]
#### SDK key

The SDK key is used to compose the outbound HTTP request to the default datafile location on the Optimizely CDN.

#### Polling interval

The polling interval is used to specify a fixed delay between consecutive HTTP requests for the datafile. The valid interval duration is between 1 to 4294967294 milliseconds.

#### Initial datafile

You can provide an initial datafile via the builder to bootstrap the `ProjectConfigManager` so that it can be used immediately without blocking execution. The initial datafile also serves as a fallback datafile if HTTP connection cannot be established. This is useful in mobile environments, where internet connectivity is not guaranteed.

The initial datafile will be discarded after the first successful datafile poll.

#### Builder methods

Use the following builder methods to customize the `HttpProjectConfigManager` configuration.
[block:parameters]
{
  "data": {
    "0-0": "**WithDatafile(string)**",
    "1-0": "**WithUrl(string)**",
    "2-0": "**WithFormat(string)**",
    "3-0": "**WithPollingInterval(TimeSpan)**",
    "0-1": "null",
    "1-1": "null",
    "2-1": "null",
    "3-2": "Fixed delay between fetches for the datafile",
    "3-1": "5 minutes",
    "1-2": "URL override location used to specify custom HTTP source for the Optimizely datafile",
    "0-2": "Initial datafile, typically sourced from a local cached source",
    "2-2": "Parameterized datafile URL by SDK key",
    "4-0": "**WithBlockingTimeoutPeriod(TimeSpan)**",
    "4-1": "15 seconds",
    "h-0": "Property",
    "h-1": "Default value",
    "h-2": "Description",
    "4-2": "Maximum time to wait for initial bootstrapping. The valid timeout duration is 1 to 4294967294 milliseconds.",
    "5-0": "**WithSdkKey(string)**",
    "5-1": "null",
    "5-2": "Optimizely project SDK key; required unless source URL is overridden"
  },
  "cols": 3,
  "rows": 6
}
[/block]
#### Update config notifications

A notification signal will be triggered whenever a new datafile is fetched. To subscribe to these notifications, use method `NotificationCenter.AddNotification()`.
[block:code]
{
  "codes": [
    {
      "code": "optimizely.NotificationCenter.AddNotification(\n    NotificationCenter.NotificationType.OptimizelyConfigUpdate,\n    () => Console.WriteLine(\"Received new datafile configuration\")\n);\n\n",
      "language": "csharp"
    }
  ]
}
[/block]
### OptimizelyFactory

[OptimizelyFactory](https://github.com/optimizely/csharp-sdk/blob/fahad/dfm-readme/OptimizelySDK/OptimizelyFactory.cs) provides basic utility to instantiate the Optimizely SDK with a minimal number of configuration options.

OptimizelyFactory does not capture all configuration and initialization options. For more use cases, build the resources with their constructors.

You must provide the SDK key at runtime, directly via the factory method:
[block:code]
{
  "codes": [
    {
      "code": "Optimizely optimizely = OptimizelyFactory.NewDefaultInstance(<<SDK_KEY>>);\n\n",
      "language": "csharp"
    }
  ]
}
[/block]

[block:api-header]
{
  "title": "Instantiate using datafile"
}
[/block]
You can also instantiate with a hard-coded datafile. If you don't pass in an SDK key, the Optimizely Client will not automatically sync newer versions of the datafile. Any time you retrieve an updated datafile, just re-instantiate the same client.

For simple applications, all you need to provide to instantiate a client is a datafile specifying the project configuration for a given environment. For most advanced implementations, you'll want to [customize the logger](doc:customize-logger-csharp) or [error handler](doc:customize-error-handler-csharp) for your specific requirements.
[block:code]
{
  "codes": [
    {
      "code": "using OptimizelySDK;\n\n// Instantiate an Optimizely client\nvar datafile = \"<Datafile_JSON_string>\"\nOptimizely OptimizelyClient = new Optimizely(datafile);\n\n",
      "language": "csharp"
    }
  ]
}
[/block]

[block:api-header]
{
  "title": "Source files"
}
[/block]
The language/platform source files containing the implementation for C# are at [Optimizely.cs](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Optimizely.cs).