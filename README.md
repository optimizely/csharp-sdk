# Optimizely C# SDK

![Semantic](https://img.shields.io/badge/sem-ver-lightgrey.svg?style=plastic)
![CI](https://github.com/optimizely/csharp-sdk/actions/workflows/csharp.yml/badge.svg?branch=master)
[![NuGet](https://img.shields.io/nuget/v/Optimizely.SDK.svg?style=plastic)](https://www.nuget.org/packages/Optimizely.SDK/)
[![Apache 2.0](https://img.shields.io/github/license/nebula-plugins/gradle-extra-configurations-plugin.svg)](http://www.apache.org/licenses/LICENSE-2.0)

This is the official C# SDK for use with Optimizely Feature Experimentation and Optimizely Full Stack (legacy).

Optimizely Feature Experimentation is an A/B testing and feature management tool for product development teams that enables you to experiment at every step. Using Optimizely Feature Experimentation allows for every feature on your roadmap to be an opportunity to discover hidden insights. Learn more at [Optimizely.com](https://www.optimizely.com/products/experiment/feature-experimentation/), or see the [developer documentation](https://docs.developers.optimizely.com/experimentation/v4.0.0-full-stack/docs/welcome).

Optimizely Rollouts is [free feature flags](https://www.optimizely.com/free-feature-flagging/) for development teams. You can easily roll out and roll back features in any application without code deploys, mitigating risk for every feature on your roadmap.

---

## Get Started

> Refer to the [C# SDK's developer documentation](https://docs.developers.optimizely.com/experimentation/v4.0.0-full-stack/docs/csharp-sdk) for detailed instructions on getting started with using the SDK.

### Prerequisites

Ensure the SDK supports the .NET platforms you're targeting. We officially support:

- **.NET Framework**: 4.0, 4.5+
- **.NET Standard**: 2.0+
- **.NET Core**: 2.0+
- **.NET**: 5.0+

> **Note**: .NET Framework 3.5 and .NET Standard 1.6 are deprecated as of SDK version 4.0.0.

> **Feature Availability**: ODP (Optimizely Data Platform) and CMAB (Contextual Multi-Armed Bandit) features require .NET Framework 4.5+ or .NET Standard 2.0+.

### Install the SDK

Install via [NuGet](https://www.nuget.org/packages/Optimizely.SDK/):

**Package Manager Console:**
```
PM> Install-Package Optimizely.SDK
```

**.NET CLI:**
```sh
dotnet add package Optimizely.SDK
```

**PackageReference:**
```xml
<PackageReference Include="Optimizely.SDK" Version="4.2.0" />
```

An ASP.NET MVC sample project is also available:
```
PM> Install-Package Optimizely.SDK.Sample
```

### Feature Management Access

To access the Feature Management configuration in the Optimizely dashboard, please contact your Optimizely customer success manager.

## Use the C# SDK

See the [developer documentation](https://docs.developers.optimizely.com/experimentation/v4.0-full-stack/docs/csharp-sdk) to learn how to set up your first C# project and use the SDK.

### Initialization

Initialize the SDK using `OptimizelyFactory`:

```csharp
using OptimizelySDK;

// Initialize with SDK key - automatically polls for datafile updates
var optimizely = OptimizelyFactory.NewDefaultInstance("YOUR_SDK_KEY");
```

For advanced initialization with custom components:

```csharp
using OptimizelySDK.Config;
using OptimizelySDK.Event;

var httpConfigManager = new HttpProjectConfigManager.Builder()
    .WithSdkKey("YOUR_SDK_KEY")
    .WithPollingInterval(TimeSpan.FromMinutes(5))
    .Build();

var batchEventProcessor = new BatchEventProcessor.Builder()
    .WithMaxBatchSize(10)
    .WithFlushInterval(TimeSpan.FromSeconds(30))
    .Build();

var optimizely = new Optimizely(
    configManager: httpConfigManager,
    eventProcessor: batchEventProcessor
);
```

### Making Decisions

Use the **User Context** and **Decide** methods for feature flag decisions:

```csharp
// Create a user context
var user = optimizely.CreateUserContext("user123", new UserAttributes
{
    { "country", "US" },
    { "subscription_tier", "premium" }
});

// Make a decision
var decision = user.Decide("feature_flag_key");

if (decision.Enabled)
{
    var theme = decision.Variables.ToDictionary()["theme"].ToString();
    Console.WriteLine($"Feature enabled with theme: {theme}");
}

// Decide for multiple flags
var decisions = user.DecideForKeys(new[] { "flag1", "flag2", "flag3" });

// Decide for all flags
var allDecisions = user.DecideAll();
```

#### Decide Options

Customize decision behavior:

```csharp
var decision = user.Decide("feature_key", new[]
{
    OptimizelyDecideOption.DISABLE_DECISION_EVENT,
    OptimizelyDecideOption.INCLUDE_REASONS
});
```

**Available Options:**
- `DISABLE_DECISION_EVENT` - Don't send impression events
- `ENABLED_FLAGS_ONLY` - Return only enabled flags
- `IGNORE_USER_PROFILE_SERVICE` - Skip user profile service
- `INCLUDE_REASONS` - Include decision reasons for debugging
- `EXCLUDE_VARIABLES` - Exclude feature variables
- `IGNORE_CMAB_CACHE` - Bypass CMAB cache (CMAB only)
- `RESET_CMAB_CACHE` - Clear CMAB cache (CMAB only)
- `INVALIDATE_USER_CMAB_CACHE` - Invalidate user-specific CMAB cache (CMAB only)

### CMAB (Contextual Multi-Armed Bandit)

> Available in SDK 4.2.0+ for .NET Framework 4.5+ and .NET Standard 2.0+

Configure CMAB for dynamic variation optimization:

```csharp
var cmabConfig = new CmabConfig()
    .SetCacheSize(1000)
    .SetCacheTtl(TimeSpan.FromMinutes(30));

OptimizelyFactory.SetCmabConfig(cmabConfig);
var optimizely = OptimizelyFactory.NewDefaultInstance("YOUR_SDK_KEY");
```

For details, see the [CMAB documentation](https://docs.developers.optimizely.com/feature-experimentation/docs/contextual-bandits).

### ODP (Optimizely Data Platform)

> Available in SDK 4.0.0+ for .NET Framework 4.5+ and .NET Standard 2.0+

ODP enables Advanced Audience Targeting:

```csharp
// Synchronous fetch
var user = optimizely.CreateUserContext("user123");
bool success = user.FetchQualifiedSegments();
var decision = user.Decide("personalized_feature");

// Asynchronous fetch with callback
var task = user.FetchQualifiedSegments((success) =>
{
    Console.WriteLine($"Segments fetched: {success}");
});

// Send custom events
optimizely.SendOdpEvent(
    action: "purchase_completed",
    identifiers: new Dictionary<string, string> { { "vuid", "user123" } },
    data: new Dictionary<string, object> { { "order_total", 99.99 } }
);
```

For more details:
- [Advanced Audience Targeting](https://docs.developers.optimizely.com/feature-experimentation/docs/optimizely-data-platform-advanced-audience-targeting)
- [ODP segment qualification methods](https://docs.developers.optimizely.com/feature-experimentation/docs/advanced-audience-targeting-segment-qualification-methods-csharp)

### Feature Flags

```csharp
// Check if feature is enabled
bool isEnabled = optimizely.IsFeatureEnabled("feature_key", "user123");

// Get feature variables
string value = optimizely.GetFeatureVariableString("feature_key", "variable_key", "user123");
int intValue = optimizely.GetFeatureVariableInteger("feature_key", "count", "user123");
double doubleValue = optimizely.GetFeatureVariableDouble("feature_key", "price", "user123");
bool boolValue = optimizely.GetFeatureVariableBoolean("feature_key", "enabled", "user123");
var jsonValue = optimizely.GetFeatureVariableJSON("feature_key", "config", "user123");

// Get all variables
var allVariables = optimizely.GetAllFeatureVariables("feature_key", "user123");
```

### Event Tracking

```csharp
// Track a conversion event
optimizely.Track("purchase", "user123");

// Track with event tags
optimizely.Track("purchase", "user123", userAttributes, new EventTags
{
    { "revenue", 4200 },
    { "category", "electronics" }
});
```

### Forced Decisions

Override decisions for QA and testing:

```csharp
var user = optimizely.CreateUserContext("qa_user");
var context = new OptimizelyDecisionContext("flag_key", "rule_key");
var forcedDecision = new OptimizelyForcedDecision("variation_key");

user.SetForcedDecision(context, forcedDecision);
user.RemoveForcedDecision(context);
user.RemoveAllForcedDecisions();
```

## Configuration

### HttpProjectConfigManager

```csharp
var configManager = new HttpProjectConfigManager.Builder()
    .WithSdkKey("YOUR_SDK_KEY")
    .WithPollingInterval(TimeSpan.FromMinutes(5))
    .WithBlockingTimeoutPeriod(TimeSpan.FromSeconds(15))
    .WithDatafileAccessToken("token")  // For secure environments
    .WithDatafile(fallbackDatafile)    // Fallback datafile
    .Build();
```

| Parameter | Default |
|-----------|---------|
| PollingInterval | 5 minutes |
| BlockingTimeoutPeriod | 15 seconds |
| DatafileAccessToken | null |

### BatchEventProcessor

```csharp
var eventProcessor = new BatchEventProcessor.Builder()
    .WithMaxBatchSize(10)
    .WithFlushInterval(TimeSpan.FromSeconds(30))
    .Build();
```

| Parameter | Default |
|-----------|---------|
| MaxBatchSize | 10 |
| FlushInterval | 30 seconds |
| TimeoutInterval | 5 minutes |

### App.config Configuration

```xml
<configuration>
  <configSections>
    <section name="optlySDKConfigSection" type="OptimizelySDK.OptimizelySDKConfigSection, OptimizelySDK" />
  </configSections>
  <optlySDKConfigSection>
    <HttpProjectConfig sdkKey="YOUR_SDK_KEY" pollingInterval="300000" />
    <BatchEventProcessor batchSize="10" flushInterval="30000" />
  </optlySDKConfigSection>
</configuration>
```

Then initialize:
```csharp
var optimizely = OptimizelyFactory.NewDefaultInstance();
```

## Advanced Features

### Notification Listeners

```csharp
// Define a decision callback method
void OnDecision(string type, string userId, UserAttributes userAttributes, Dictionary<string, object> decisionInfo)
{
    Console.WriteLine($"Decision type: {type}, User: {userId}");
}

// Subscribe to decision notifications
optimizely.NotificationCenter.AddNotification(
    NotificationCenter.NotificationType.Decision,
    OnDecision
);
```

**Notification Types:** `Decision`, `Track`, `LogEvent`, `OptimizelyConfigUpdate`

### User Profile Service

Implement `UserProfileService` for sticky bucketing:

```csharp
public class MyUserProfileService : UserProfileService
{
    public Dictionary<string, object> Lookup(string userId)
    {
        // Retrieve user profile from your database
        return GetUserProfileFromDatabase(userId);
    }

    public void Save(Dictionary<string, object> userProfile)
    {
        // Save user profile to your database
        SaveUserProfileToDatabase(userProfile);
    }
}
```

### OptimizelyConfig

```csharp
var config = optimizely.GetOptimizelyConfig();
foreach (var feature in config.FeaturesMap.Values)
{
    Console.WriteLine($"Feature: {feature.Key}");
}
```

### Disposal

```csharp
optimizely.Dispose();  // Clean up resources

// Or use with 'using' statement
using (var optimizely = OptimizelyFactory.NewDefaultInstance("SDK_KEY"))
{
    // Use SDK
}
```

## SDK Development

### Contributing

Please see [CONTRIBUTING](CONTRIBUTING.md).

### Unit Tests

```sh
dotnet test
```

### Third Party Licenses

- [murmurhash-signed](https://www.nuget.org/packages/murmurhash-signed/)
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/)
- [NJsonSchema](https://www.nuget.org/packages/NJsonSchema/)

## Credits

This SDK is developed and maintained by [Optimizely](https://optimizely.com) and many [contributors](https://github.com/optimizely/csharp-sdk/graphs/contributors).

### Other Optimizely SDKs

- Agent - https://github.com/optimizely/agent
- Android - https://github.com/optimizely/android-sdk
- Flutter - https://github.com/optimizely/optimizely-flutter-sdk
- Go - https://github.com/optimizely/go-sdk
- Java - https://github.com/optimizely/java-sdk
- JavaScript - https://github.com/optimizely/javascript-sdk
- PHP - https://github.com/optimizely/php-sdk
- Python - https://github.com/optimizely/python-sdk
- React - https://github.com/optimizely/react-sdk
- Ruby - https://github.com/optimizely/ruby-sdk
- Swift - https://github.com/optimizely/swift-sdk
