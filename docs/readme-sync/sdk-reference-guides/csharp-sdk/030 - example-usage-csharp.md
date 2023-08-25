---
title: "Example usage"
slug: "example-usage-csharp"
hidden: false
createdAt: "2019-09-11T22:26:38.446Z"
updatedAt: "2019-09-12T20:28:26.570Z"
---
Once you've installed the C# SDK, import the Optimizely library into your code, get your Optimizely project's datafile, and instantiate a client. Then, you can use the client to evaluate feature flags, activate an A/B test, or feature test.

This example demonstrates the basic usage of each of these concepts. This example shows how to: 
1. Evaluate a feature with the key `price_filter` and check a configuration variable on it called `min_price`. The SDK evaluates your feature test and rollouts to determine whether the feature is enabled for a particular user, and which minimum price they should see if so.

2. Run an A/B test called `app_redesign`. This experiment has two variations, `control` and `treatment`. It uses the `activate` method to assign the user to a variation, returning its key. As a side effect, the activate function also sends an impression event to Optimizely to record that the current user has been exposed to the experiment. 

3. Use event tracking to track an event called `purchased`. This conversion event measures the impact of an experiment. Using the track method, the purchase is automatically attributed back to the running A/B and feature tests we've activated, and the SDK sends a network request to Optimizely via the customizable event dispatcher so we can count it in your results page.
```csharp
// Import Optimizely SDK
using OptimizelySDK;

// Instantiate an Optimizely client
var optimizelyClient = new Optimizely(datafile);

// Evaluate a feature flag and a variable
bool isFeatureEnabled = optimizelyClient.IsFeatureEnabled("price_filter", userId);
int? min_price = optimizelyClient.GetFeatureVariableInteger("price_filter", "min_price", userId);

// Activate an A/B test
var variation = optimizelyClient.Activate("app_redesign", userId);

if (variation != null && !string.IsNullOrEmpty(variation.Key))
{
    if (variation.Key == "control")
    {
        // Execute code for variation A
    }
    else if (variation.Key == "treatment")
    {
        // Execute code for variation B
    }
}
else
{
    // Execute code for your users who don’t qualify for the experiment
}

// Track an event
optimizelyClient.Track("purchased", userId);

```