# Optimizely C# SDK
![Semantic](https://img.shields.io/badge/sem-ver-lightgrey.svg?style=plastic)
[![Build Status](https://travis-ci.org/optimizely/csharp-sdk.svg?branch=master)](https://travis-ci.org/optimizely/csharp-sdk)
[![NuGet](https://img.shields.io/nuget/v/Optimizely.SDK.svg?style=plastic)](https://www.nuget.org/packages/Optimizely.SDK/)
[![Apache 2.0](https://img.shields.io/github/license/nebula-plugins/gradle-extra-configurations-plugin.svg)](http://www.apache.org/licenses/LICENSE-2.0)

This repository houses the .Net based C# SDK for Optimizely Full Stack.

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
Optimizely's NuGet package digitally signs these third party DLL's.
Third party software licenses are included in the folder Licenses when Optimizely is installed.
 



