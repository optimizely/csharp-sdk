---
title: "Get Forced Variation"
slug: "get-forced-variation-csharp"
hidden: false
createdAt: "2019-09-12T13:51:51.169Z"
updatedAt: "2019-09-12T20:34:30.074Z"
---

Returns the forced variation set by [Set Forced Variation](doc:set-forced-variation-csharp), or `null` if no variation was forced.

A user can be forced into a variation for a given experiment for the lifetime of the Optimizely client. This method gets the variation that the user has been forced into. The forced variation value is runtime only and does not persist across application launches.

## Version

SDK v3.0, v3.1

## Description

Forced bucketing variations take precedence over whitelisted variations, variations saved in a User Profile Service (if one exists), and the normal bucketed variation. Variations are overwritten when [Set Forced Variation](doc:set-forced-variation-csharp) is invoked.

> **Note**
> A forced variation only persists for the lifetime of an Optimizely client.

## Parameters

This table lists the required and optional parameters for the C# SDK.

| Parameter         | Type   | Description                                 |
|-------------------|--------|---------------------------------------------|
| **experimentKey**<br>*required* | string | The key of the experiment to retrieve the forced variation. |
| **userId**<br>*required*       | string | The ID of the user in the forced variation. |

## Returns

`null|string`: The variation key.

## Example

```csharp
var variation = optimizelyClient.GetForcedVariation("my_experiment_key", "user_123");
```
## Source files

The language/platform source files containing the implementation for C# is [Optimizely.cs](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Optimizely.cs).
