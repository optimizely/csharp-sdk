# CMAB Additions to Decide Methods

These sections should be added to the "Decide methods for the C# SDK" documentation.

## Addition 1: In the "Decide" > "Key features" section

**Location:** After the third bullet point "Complement to decide all method"

### Add this new bullet:

- **CMAB support** – For CMAB experiments (SDK v4.2.0+), decisions are automatically cached to reduce latency. The cache is invalidated when CMAB attributes change or TTL expires. Use CMAB-specific decide options (`IGNORE_CMAB_CACHE`, `INVALIDATE_USER_CMAB_CACHE`, `RESET_CMAB_CACHE`) to override default caching behavior. See CMAB cache control below.

## Addition 2: In the "OptimizelyDecideOption" table

**Location:** After the last row (EXCLUDE_VARIABLES)

### Add these three new rows:

| OptimizelyDecideOption | Description |
|------------------------|-------------|
| OptimizelyDecideOption.IGNORE_CMAB_CACHE | Bypasses the CMAB cache and fetches a fresh decision from the CMAB service. Use when you need real-time decisions that reflect the latest context. Available in SDK v4.2.0+. |
| OptimizelyDecideOption.INVALIDATE_USER_CMAB_CACHE | Removes the cached CMAB decision for this user and experiment before making the decision. Use when user context has changed significantly and you want to ensure a fresh decision. Available in SDK v4.2.0+. |
| OptimizelyDecideOption.RESET_CMAB_CACHE | Clears all entries from the CMAB cache before making the decision. Use sparingly for testing or cache corruption scenarios. Available in SDK v4.2.0+. |

## Addition 3: New section after "Best practices"

**Location:** After the "Best practices" section, before "Source files"

### Add this entire new section:

## CMAB cache control

**Minimum SDK version** – v4.2.0+

For Contextual Multi-Armed Bandit (CMAB) experiments, the SDK automatically caches variation assignments to reduce latency and API calls. The cache stores decisions based on user ID, experiment ID, and CMAB attribute values.

### Automatic cache invalidation

The cache is automatically invalidated when:

- The cached entry's TTL expires (default: 30 minutes)
- CMAB attribute values change for a user (detected via attribute hash comparison)

### Manual cache control

Use CMAB-specific decide options to control cache behavior on a per-decision basis:

**C#**

```csharp
using OptimizelySDK;
using OptimizelySDK.OptimizelyDecisions;

var optimizelyClient = OptimizelyFactory.NewDefaultInstance("SDK_KEY_HERE");

// Example 1: Bypass cache for real-time decision
var user = optimizelyClient.CreateUserContext("user123", new UserAttributes
{
    { "age", 25 },
    { "location", "US" }
});
var decision = user.Decide("my-cmab-flag", new[]
{
    OptimizelyDecideOption.IGNORE_CMAB_CACHE  // Always fetch fresh from CMAB service
});

// Example 2: Invalidate cache when user context changes significantly
user.SetAttributes(new UserAttributes
{
    { "age", 26 },
    { "location", "UK" }  // Context changed
});
decision = user.Decide("my-cmab-flag", new[]
{
    OptimizelyDecideOption.INVALIDATE_USER_CMAB_CACHE  // Clear cached decision for this user
});

// Example 3: Reset entire CMAB cache (use sparingly)
decision = user.Decide("my-cmab-flag", new[]
{
    OptimizelyDecideOption.RESET_CMAB_CACHE  // Clear all CMAB cache entries
});
```

### CMAB decision reasons

When using `INCLUDE_REASONS`, CMAB-related information appears in the decision's `Reasons` field:

```csharp
var decision = user.Decide("my-cmab-flag", new[]
{
    OptimizelyDecideOption.INCLUDE_REASONS
});

// Print CMAB-related decision reasons
foreach (var reason in decision.Reasons)
{
    Console.WriteLine(reason);
    // Examples:
    // "CMAB decision retrieved from cache"
    // "CMAB decision fetched from service"
    // "CMAB cache invalidated due to attribute change"
}
```

> **Note**
> 
> Configure CMAB cache settings (size, TTL) at client initialization time. See [initialize-sdk-cmab-csharp](initialize-sdk-cmab-csharp.md) for details.
