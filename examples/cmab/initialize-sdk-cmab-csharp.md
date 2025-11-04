# CMAB Configuration

Configure Contextual Multi-Armed Bandit (CMAB) settings at initialization time, including caching behavior and prediction endpoint customization.

Pass a `CmabConfig` object to `SetCmabConfig` on the `OptimizelyFactory` when initializing your client:

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| CacheSize | int? | 10,000 | Maximum number of cached decisions |
| CacheTtl | TimeSpan? | 30 minutes | Time-to-live for cache entries |
| Cache | ICacheWithRemove<CmabCacheEntry> | null | Optional custom cache implementation for distributed deployments |
| PredictionEndpointTemplate | string | `https://prediction.cmab.optimizely.com/predict/{0}` | Custom prediction endpoint template. Use `{0}` as placeholder for rule ID. Useful for proxy configurations or custom routing. |

Note: When `CacheSize` or `CacheTtl` are set to null, the SDK uses the default values shown above.

## Example

### Basic configuration

```csharp
using System;
using OptimizelySDK;

class Program
{
    static void Main()
    {
        // Configure CMAB settings
        var cmabConfig = new CmabConfig()
            .SetCacheSize(5000)                        // Cache up to 5000 decisions
            .SetCacheTtl(TimeSpan.FromMinutes(15));    // 15-minute TTL

        // Apply CMAB configuration to the factory
        OptimizelyFactory.SetCmabConfig(cmabConfig);

        // Create Optimizely client with CMAB configuration
        var optimizelyClient = OptimizelyFactory.NewDefaultInstance("your-sdk-key");

        // Use the client normally
        var user = optimizelyClient.CreateUserContext("user123");
        var decision = user.Decide("my-cmab-flag");

        // Clean up
        optimizelyClient.Dispose();
    }
}
```

### Custom prediction endpoint

Configure a custom prediction endpoint for proxy or custom routing scenarios:

```csharp
using System;
using OptimizelySDK;

class Program
{
    static void Main()
    {
        // Configure CMAB with custom prediction endpoint
        var cmabConfig = new CmabConfig()
            .SetCacheSize(5000)
            .SetCacheTtl(TimeSpan.FromMinutes(15))
            .SetPredictionEndpointTemplate("https://proxy.example.com/cmab/predict/{0}");

        // Apply CMAB configuration to the factory
        OptimizelyFactory.SetCmabConfig(cmabConfig);

        // Create Optimizely client with custom endpoint
        var optimizelyClient = OptimizelyFactory.NewDefaultInstance("your-sdk-key");

        // CMAB decisions will now use the custom endpoint
        var user = optimizelyClient.CreateUserContext("user123");
        var decision = user.Decide("my-cmab-flag");

        // Clean up
        optimizelyClient.Dispose();
    }
}
```

For detailed configuration examples, cache behavior, endpoint customization, and runtime control options, see [Configure CMAB](configure-cmab-csharp.md).
