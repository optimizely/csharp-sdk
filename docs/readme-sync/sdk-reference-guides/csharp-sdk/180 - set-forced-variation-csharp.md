## Set Forced Variation

Forces a user into a variation for a given experiment for the lifetime of the Optimizely client.

The purpose of this method is to force a user into a specific variation or personalized experience for a given experiment. The forced variation value doesn't persist across application launches.

### Version

SDK v3.0, v3.1

### Description

Forces a user into a variation for a given experiment for the lifetime of the Optimizely client. Any future calls to [Activate](doc:activate-csharp), [Is Feature Enabled](doc:is-feature-enabled-csharp), [Get Feature Variable](doc:get-feature-variable-csharp), and [Track](doc:track-csharp) for the given user ID returns the forced variation.

Forced bucketing variations take precedence over whitelisted variations, variations saved in a User Profile Service (if one exists), and the normal bucketed variation. Impression and conversion events are still tracked when forced bucketing is enabled.

Variations are overwritten with each set method call. To clear the forced variations so that the normal bucketing flow can occur, pass null as the variation key parameter. To get the variation that has been forced, use [Get Forced Variation](doc:get-forced-variation-csharp).

This call will fail and return false if the experiment key is not in the project file or if the variation key is not in the experiment.

You can also use Set Forced Variation for [feature tests](doc:run-feature-tests-csharp).

### Parameters

The table below lists the required and optional parameters for the C# SDK.

| Parameter | Type    | Description                                                           |
|-----------|---------|-----------------------------------------------------------------------|
| experimentKey | string | The key of the experiment to set with the forced variation.            |
| userId | string | The ID of the user to force into the variation.                        |
| variationKey | string | The key of the forced variation. Set the value to `null` to clear the existing experiment-to-variation mapping. |

## Returns

A Boolean value that indicates if the set completed successfully.

## Example

```csharp
optimizelyClient.SetForcedVariation("my_experiment_key", "user_123", "some_variation_key");
```

## Side effects

In the receiving client instance, sets the forced variation for the specified user in the specified experiment. This forced variation is used instead of the variation that Optimizely would normally determine for that user and experiment.

## Source files

The language/platform source files containing the implementation for C# is [Optimizely.cs](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Optimizely.cs).
