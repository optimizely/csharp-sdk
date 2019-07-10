# Optimizely C# SDK
![Semantic](https://img.shields.io/badge/sem-ver-lightgrey.svg?style=plastic)
[![Build Status](https://travis-ci.org/optimizely/csharp-sdk.svg?branch=master)](https://travis-ci.org/optimizely/csharp-sdk)
[![NuGet](https://img.shields.io/nuget/v/Optimizely.SDK.svg?style=plastic)](https://www.nuget.org/packages/Optimizely.SDK/)
[![Apache 2.0](https://img.shields.io/github/license/nebula-plugins/gradle-extra-configurations-plugin.svg)](http://www.apache.org/licenses/LICENSE-2.0)

This repository houses the .Net based C# SDK for use with Optimizely Full Stack and Optimizely Rollouts.

Optimizely Full Stack is A/B testing and feature flag management for product development teams. Experiment in any application. Make every feature on your roadmap an opportunity to learn. Learn more at https://www.optimizely.com/platform/full-stack/, or see the [documentation](https://docs.developers.optimizely.com/full-stack/docs).

Optimizely Rollouts is free feature flags for development teams. Easily roll out and roll back features in any application without code deploys. Mitigate risk for every feature on your roadmap. Learn more at https://www.optimizely.com/rollouts/, or see the [documentation](https://docs.developers.optimizely.com/rollouts/docs).

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

See the Optimizely Full Stack [developer documentation](https://developers.optimizely.com/x/solutions/sdks/reference/?language=csharp) to learn how to set up your first Full Stack project and use the SDK.

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

These are optional plug-ins and default behavior is implement if none are provided.

### OptimizelyFactory

[`OptimizelyFactory`](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/OptimizelyFactory.cs)
provides basic utility to instantiate the Optimizely SDK with a minimal number of configuration options.

`OptimizelyFactory` does not capture all configuration and initialization options. For more use cases,
build the resources via their respective builder classes.

#### Use `OptimizelyFactory`

You must provide the SDK key at runtime, either directly via the factory method:
```
Optimizely optimizely = OptimizelyFactory.newDefaultInstance(<<SDK_KEY>>);
```

You can also provide default datafile with the SDK key.
```
Optimizely optimizely = OptimizelyFactory.newDefaultInstance(<<SDK_KEY>>, <<Fallback>>);
```

### HttpProjectConfigManager

[`HttpProjectConfigManager`](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Config/HttpProjectConfigManager.cs)
is an implementation of the abstract [`PollingProjectConfigManager`](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Config/PollingProjectConfigManager.cs).
The `Poll` method is extended and makes an HTTP GET request to the configured URL to asynchronously download the
project datafile and initialize an instance of the ProjectConfig.

By default, `HttpProjectConfigManager` will block until the first successful datafile retrieval, up to a configurable timeout.
Set the frequency of the polling method and the blocking timeout with `HttpProjectConfigManager.Builder`.

#### Use `HttpProjectConfigManager`

```
HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
	.WithSdkKey(sdkKey)
	.WithPollingInterval(TimeSpan.FromMinutes(1))
	.Build();
```

#### SDK key

The SDK key is used to compose the outbound HTTP request to the default datafile location on the Optimizely CDN.

#### Polling interval

The polling interval is used to specify a fixed delay between consecutive HTTP requests for the datafile.

#### Blocking Timeout Period

The blocking timeout period is used to specify a maximum time to wait for initial bootstrapping.

#### Initial datafile

You can provide an initial datafile via the builder to bootstrap the `ProjectConfigManager` so that it can be used immediately without blocking execution.

#### URL

The URL is used to specify the location of datafile.

#### Format

This option enables user to provide a custom URL format to fetch the datafile.

#### Start by default

This option is used to specify whether to start the config manager on initialization.

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
