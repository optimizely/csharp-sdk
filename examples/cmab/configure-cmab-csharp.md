# Configure CMAB Settings

Configure Contextual Multi-Armed Bandit (CMAB) settings in the C# SDK, including caching behavior and prediction endpoint customization. The CMAB cache stores variation assignments to reduce latency and minimize API calls to the CMAB service.

## Prerequisites

- C# SDK version 4.2.0 or higher
- A CMAB-enabled experiment in your Optimizely project

## Minimum SDK version

4.2.0

## Description

When a user is bucketed into a CMAB experiment, the SDK makes an API call to the CMAB service to determine which variation to show. You can configure several aspects of CMAB behavior:

### Caching

To improve performance and reduce latency, the SDK caches decisions based on:

- User ID
- Experiment ID
- CMAB attribute values

The cache is automatically invalidated when CMAB attributes change for a user, ensuring fresh decisions when context changes.

By default, the SDK uses an in-memory LRU (Least Recently Used) cache with a maximum size of 10000 entries and a TTL of 30 minutes. You can customize these settings or provide your own cache implementation.

### Prediction Endpoint

You can customize the prediction endpoint to route CMAB requests through a proxy server or use a custom endpoint URL. This is useful for network configurations that require requests to go through specific routing paths.

## Parameters

Configure CMAB behavior by passing a `CmabConfig` object to `SetCmabConfig` when creating your Optimizely client using `OptimizelyFactory`.

```csharp
public class CmabConfig
{
    public int? CacheSize { get; private set; }
    public TimeSpan? CacheTtl { get; private set; }
    public ICacheWithRemove<CmabCacheEntry> Cache { get; private set; }
    public string PredictionEndpointTemplate { get; private set; }
}
```

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| CacheSize | int? | No | 10000 | Maximum number of decisions to cache. Increase for applications with many users or experiments. |
| CacheTtl | TimeSpan? | No | 30 minutes | How long cache entries remain valid. Lower values ensure fresher decisions but increase API calls. |
| Cache | ICacheWithRemove<CmabCacheEntry> | No | null | Custom cache implementation for distributed systems. If null, the SDK uses an in-memory cache. |
| PredictionEndpointTemplate | string | No | `https://prediction.cmab.optimizely.com/predict/{0}` | Custom prediction endpoint template. Use `{0}` as placeholder for rule ID. Useful for proxy configurations or custom routing scenarios. |

Note: When CacheSize or CacheTtl are set to null, the SDK uses the default values.

## Returns

The `SetCmabConfig` method configures the Optimizely client factory with your CMAB settings. The configuration applies to all Optimizely instances created after calling `SetCmabConfig`.

## Example

### Basic configuration

Adjust cache size and TTL for your application's needs:

```csharp
using System;
using OptimizelySDK;

class Program
{
    static void Main()
    {
        // Configure CMAB cache with custom size and TTL
        var cmabConfig = new CmabConfig()
            .SetCacheSize(500)                         // Cache up to 500 decisions
            .SetCacheTtl(TimeSpan.FromMinutes(10));    // Refresh cache every 10 minutes

        // Apply CMAB configuration to the factory
        OptimizelyFactory.SetCmabConfig(cmabConfig);

        // Create Optimizely client with CMAB configuration
        var optimizelyClient = OptimizelyFactory.NewDefaultInstance("your-sdk-key");

        // Use the client normally
        var user = optimizelyClient.CreateUserContext("user123", new UserAttributes
        {
            { "age", 25 },
            { "location", "US" }
        });

        var decision = user.Decide("my-cmab-flag");
        // Cache will store this decision for 10 minutes

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
            .SetCacheSize(500)
            .SetCacheTtl(TimeSpan.FromMinutes(10))
            .SetPredictionEndpointTemplate("https://proxy.example.com/cmab/predict/{0}");

        // Apply CMAB configuration to the factory
        OptimizelyFactory.SetCmabConfig(cmabConfig);

        // Create Optimizely client
        var optimizelyClient = OptimizelyFactory.NewDefaultInstance("your-sdk-key");

        // CMAB decisions will now use the custom endpoint
        var user = optimizelyClient.CreateUserContext("user123", new UserAttributes
        {
            { "age", 25 },
            { "location", "US" }
        });

        var decision = user.Decide("my-cmab-flag");

        // Clean up
        optimizelyClient.Dispose();
    }
}
```

### Advanced: Custom cache implementation

For multi-instance deployments, you can provide your own cache implementation that satisfies the `ICacheWithRemove<CmabCacheEntry>` interface:

```csharp
using System;
using OptimizelySDK;
using OptimizelySDK.Cmab;
using OptimizelySDK.Utils;

// Example custom cache implementation
public class MyCustomCache : ICacheWithRemove<CmabCacheEntry>
{
    // Your cache implementation fields
    
    public CmabCacheEntry Lookup(string key)
    {
        // Implement lookup logic
        return null;
    }

    public void Save(string key, CmabCacheEntry value)
    {
        // Implement save logic
    }

    public void Remove(string key)
    {
        // Implement remove logic
    }

    public void Reset()
    {
        // Implement reset logic
    }
}

class Program
{
    static void Main()
    {
        // Create custom cache
        var customCache = new MyCustomCache();

        // Use custom cache for CMAB
        var cmabConfig = new CmabConfig()
            .SetCache(customCache);

        // Apply CMAB configuration to the factory
        OptimizelyFactory.SetCmabConfig(cmabConfig);

        // Create Optimizely client with custom cache
        var optimizelyClient = OptimizelyFactory.NewDefaultInstance("your-sdk-key");

        // CMAB decisions now use your custom cache
        
        // Clean up
        optimizelyClient.Dispose();
    }
}
```

## Custom cache interface

To implement a custom cache, your cache must implement the `ICacheWithRemove<CmabCacheEntry>` interface:

```csharp
public interface ICacheWithRemove<T>
{
    // Lookup retrieves a value from the cache
    // Returns the value if found, null if not found
    T Lookup(string key);

    // Save stores a key-value pair in the cache
    void Save(string key, T value);

    // Remove deletes a key from the cache
    void Remove(string key);

    // Reset clears all entries from the cache
    void Reset();
}
```

## Cache behavior

### Cache invalidation

The cache is automatically invalidated when:

- The cached entry's TTL expires
- CMAB attribute values change for a user (detected via attribute hash comparison)
- INVALIDATE_USER_CMAB_CACHE decide option is used
- RESET_CMAB_CACHE decide option is used

## CMAB-specific decide options

Control cache behavior on a per-decision basis using decide options:

```csharp
using OptimizelySDK;
using OptimizelySDK.OptimizelyDecisions;

var optimizelyClient = OptimizelyFactory.NewDefaultInstance("your-sdk-key");

// Example 1: Bypass cache for real-time decision
var user = optimizelyClient.CreateUserContext("user123", new UserAttributes
{
    { "age", 25 },
    { "location", "US" }
});
var decision = user.Decide("my-flag", new[]
{
    OptimizelyDecideOption.IGNORE_CMAB_CACHE  // Always fetch fresh from CMAB service
});

// Example 2: Invalidate cache when user context changes significantly
var decision2 = user.Decide("my-flag", new[]
{
    OptimizelyDecideOption.INVALIDATE_USER_CMAB_CACHE  // Clear cached decision for this user
});

// Example 3: Reset entire CMAB cache (use sparingly)
var decision3 = user.Decide("my-flag", new[]
{
    OptimizelyDecideOption.RESET_CMAB_CACHE  // Clear all CMAB cache entries
});
```

| Option | Description |
|--------|-------------|
| IGNORE_CMAB_CACHE | Bypass cache and fetch a fresh decision from CMAB service |
| INVALIDATE_USER_CMAB_CACHE | Remove cached decision for this user and experiment before deciding |
| RESET_CMAB_CACHE | Clear all entries from the CMAB cache before deciding |

## Decision reasons

The C# SDK provides detailed reasons for CMAB decisions that help you understand cache behavior. Access reasons via `decision.Reasons`:

```csharp
using OptimizelySDK;
using OptimizelySDK.OptimizelyDecisions;

var optimizelyClient = OptimizelyFactory.NewDefaultInstance("your-sdk-key");

var user = optimizelyClient.CreateUserContext("user123", new UserAttributes
{
    { "age", 25 }
});

var decision = user.Decide("my-cmab-flag", new[]
{
    OptimizelyDecideOption.INCLUDE_REASONS
});

// Print decision reasons
foreach (var reason in decision.Reasons)
{
    Console.WriteLine(reason);
    // Examples:
    // "CMAB decision retrieved from cache"
    // "CMAB decision fetched from service"
    // "CMAB cache invalidated due to attribute change"
}
```

## Use cases

### When to use custom cache

Consider implementing a custom cache when:

- **Multi-instance deployments**: Share cache across multiple application instances
- **Distributed systems**: Use external caching systems for centralized caching
- **Persistent cache**: Maintain cache across application restarts
- **Custom eviction policies**: Implement domain-specific cache management
- **Monitoring**: Track cache metrics (hit rate, memory usage, etc.)

### When to use custom prediction endpoint

Consider configuring a custom prediction endpoint when:

- **Proxy requirements**: Route CMAB requests through a corporate proxy server
- **Monitoring and logging**: Route requests through intermediary services for observability
