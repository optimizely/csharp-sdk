# Optimizely C# SDK - Step-by-Step Tutorial

This tutorial provides detailed guidance on implementing A/B testing and feature flags using the Optimizely C# SDK. Follow these steps to get started with Feature Experimentation in your C# applications.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Setting Up Your First Experiment](#setting-up-your-first-experiment)
3. [Implementing A/B Testing](#implementing-ab-testing)
4. [Using Feature Flags](#using-feature-flags)
5. [Tracking Events](#tracking-events)
6. [Production Best Practices](#production-best-practices)

## Getting Started

### Step 1: Install the SDK

Add the Optimizely SDK to your project:

```bash
# Using Package Manager Console
Install-Package Optimizely.SDK

# Using .NET CLI
dotnet add package Optimizely.SDK
```

### Step 2: Create an Optimizely Account

1. Sign up at [Optimizely.com](https://www.optimizely.com/)
2. Create a new project
3. Note your Project ID and SDK Key

### Step 3: Basic SDK Setup

```csharp
using OptimizelySDK;
using OptimizelySDK.Logger;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event.Dispatcher;

// Simple initialization with SDK key
var optimizely = OptimizelyFactory.NewDefaultInstance("YOUR_SDK_KEY");

// Or with more control
var logger = new DefaultLogger();
var errorHandler = new DefaultErrorHandler(logger);
var eventDispatcher = new DefaultEventDispatcher(logger);

var optimizely = new Optimizely(
    datafile: "YOUR_DATAFILE_JSON",
    eventDispatcher: eventDispatcher,
    logger: logger,
    errorHandler: errorHandler
);
```

## Setting Up Your First Experiment

### Step 1: Create an Experiment in Optimizely Dashboard

1. Log into your Optimizely dashboard
2. Go to **Experiments** → **Create Experiment**
3. Set up your experiment:
   - **Name**: "Product Sort Test"
   - **Key**: "product_sort_experiment"
   - **Variations**:
     - Control: "sort_by_name" 
     - Treatment: "sort_by_price"

### Step 2: Define Your Event

1. Go to **Events** → **Create Event**
2. Set up your conversion event:
   - **Name**: "Add to Cart"
   - **Key**: "add_to_cart"
   - **Add to your experiment**

### Step 3: Configure Audiences (Optional)

1. Go to **Audiences** → **Create Audience**
2. Define targeting attributes:
   - **user_type** (string)
   - **age** (integer)
   - **country** (string)

## Implementing A/B Testing

### Step 1: Basic Variation Assignment

```csharp
// Get the variation for a user
var variation = optimizely.GetVariation(
    experimentKey: "product_sort_experiment",
    userId: "user_123",
    userAttributes: new UserAttributes
    {
        { "user_type", "premium" },
        { "age", 25 }
    }
);

// Handle the result
if (variation != null)
{
    switch (variation.Key)
    {
        case "sort_by_price":
            // Show products sorted by price
            products = products.OrderBy(p => p.Price);
            break;
        case "sort_by_name":
        default:
            // Show products sorted by name (control)
            products = products.OrderBy(p => p.Name);
            break;
    }
}
else
{
    // User not bucketed - show default experience
    products = products.OrderBy(p => p.Name);
}
```

### Step 2: Using Activate for Impression Tracking

```csharp
// Use Activate when you want to track impressions automatically
var variation = optimizely.Activate(
    experimentKey: "product_sort_experiment",
    userId: "user_123",
    userAttributes: userAttributes
);

// This automatically sends an impression event to Optimizely
```

### Step 3: Handling User Attributes

```csharp
public class UserAttributes : Dictionary<string, object>
{
    public static UserAttributes FromUser(User user)
    {
        return new UserAttributes
        {
            { "user_type", user.SubscriptionType },
            { "age", user.Age },
            { "country", user.Country },
            { "signup_date", user.SignupDate.ToString("yyyy-MM-dd") }
        };
    }
}

// Usage
var attributes = UserAttributes.FromUser(currentUser);
var variation = optimizely.GetVariation("experiment_key", userId, attributes);
```

## Using Feature Flags

### Step 1: Create a Feature Flag

1. In Optimizely dashboard: **Features** → **Create Feature**
2. Configure your feature:
   - **Name**: "New Checkout Flow"
   - **Key**: "new_checkout_flow"
   - **Variables**: 
     - `button_color` (string, default: "blue")
     - `max_items` (integer, default: 10)

### Step 2: Check Feature Status

```csharp
// Simple feature check
bool isEnabled = optimizely.IsFeatureEnabled(
    featureKey: "new_checkout_flow",
    userId: "user_123",
    userAttributes: userAttributes
);

if (isEnabled)
{
    // Show new checkout flow
    ShowNewCheckoutFlow();
}
else
{
    // Show original checkout flow
    ShowOriginalCheckoutFlow();
}
```

### Step 3: Get Feature Variables

```csharp
// Get feature variable values
string buttonColor = optimizely.GetFeatureVariableString(
    featureKey: "new_checkout_flow",
    variableKey: "button_color",
    userId: "user_123",
    userAttributes: userAttributes
);

int maxItems = optimizely.GetFeatureVariableInteger(
    featureKey: "new_checkout_flow",
    variableKey: "max_items",
    userId: "user_123",
    userAttributes: userAttributes
);

// Apply the configuration
SetCheckoutButtonColor(buttonColor);
SetMaxCartItems(maxItems);
```

### Step 4: Feature Flag with Experiments

```csharp
// Get feature decision (includes experiment data)
var decision = optimizely.Decide(
    user: optimizely.CreateUserContext("user_123", userAttributes),
    key: "new_checkout_flow"
);

if (decision.Enabled)
{
    var buttonColor = decision.Variables.GetValueForKey("button_color")?.ToString();
    var maxItems = decision.Variables.GetValueForKey("max_items")?.ToObject<int>() ?? 10;
    
    ShowNewCheckoutFlow(buttonColor, maxItems);
    
    // Track that user saw the new feature
    optimizely.Track("feature_viewed", "user_123", userAttributes);
}
```

## Tracking Events

### Step 1: Basic Event Tracking

```csharp
// Track a simple conversion event
optimizely.Track(
    eventKey: "add_to_cart",
    userId: "user_123",
    userAttributes: userAttributes
);
```

### Step 2: Event Tracking with Revenue

```csharp
// Track conversion with revenue data
var eventTags = new EventTags
{
    { "revenue", 2999 }, // Revenue in cents ($29.99)
    { "product_id", "prod_123" },
    { "category", "electronics" },
    { "quantity", 2 }
};

optimizely.Track(
    eventKey: "purchase",
    userId: "user_123",
    userAttributes: userAttributes,
    eventTags: eventTags
);
```

### Step 3: Custom Event Tracking

```csharp
// Track custom metrics
public void TrackProductView(string userId, string productId, decimal price)
{
    var eventTags = new EventTags
    {
        { "product_id", productId },
        { "price", (int)(price * 100) }, // Convert to cents
        { "timestamp", DateTime.UtcNow.ToString("O") }
    };
    
    optimizely.Track("product_viewed", userId, null, eventTags);
}

// Track user engagement
public void TrackTimeOnPage(string userId, int secondsOnPage)
{
    var eventTags = new EventTags
    {
        { "duration_seconds", secondsOnPage },
        { "page_type", "product_listing" }
    };
    
    optimizely.Track("page_engagement", userId, null, eventTags);
}
```

## Production Best Practices

### 1. SDK Initialization

```csharp
// Use dependency injection for SDK singleton
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IOptimizely>(provider =>
        {
            var logger = provider.GetService<ILogger<DefaultLogger>>();
            return OptimizelyFactory.NewDefaultInstance(
                sdkKey: Configuration["Optimizely:SdkKey"],
                fallback: Configuration["Optimizely:FallbackDatafile"]
            );
        });
    }
}
```

### 2. Error Handling

```csharp
public class SafeOptimizelyWrapper
{
    private readonly IOptimizely _optimizely;
    private readonly ILogger _logger;
    
    public SafeOptimizelyWrapper(IOptimizely optimizely, ILogger logger)
    {
        _optimizely = optimizely;
        _logger = logger;
    }
    
    public string GetVariationSafely(string experimentKey, string userId, 
        UserAttributes attributes = null, string defaultVariation = "control")
    {
        try
        {
            var variation = _optimizely.GetVariation(experimentKey, userId, attributes);
            return variation?.Key ?? defaultVariation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting variation for experiment {ExperimentKey}", experimentKey);
            return defaultVariation;
        }
    }
}
```

### 3. Asynchronous Event Tracking

```csharp
public class OptimizelyService
{
    private readonly IOptimizely _optimizely;
    private readonly SemaphoreSlim _semaphore;
    
    public OptimizelyService(IOptimizely optimizely)
    {
        _optimizely = optimizely;
        _semaphore = new SemaphoreSlim(10, 10); // Limit concurrent tracking
    }
    
    public async Task TrackEventAsync(string eventKey, string userId, 
        UserAttributes attributes = null, EventTags eventTags = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            await Task.Run(() => _optimizely.Track(eventKey, userId, attributes, eventTags));
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### 4. Configuration Management

```csharp
// appsettings.json
{
  "Optimizely": {
    "SdkKey": "your_sdk_key_here",
    "PollingInterval": "00:05:00",
    "BlockingTimeout": "00:00:15",
    "BatchSize": 10,
    "FlushInterval": "00:00:30"
  }
}

// Configuration class
public class OptimizelyConfig
{
    public string SdkKey { get; set; }
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan BlockingTimeout { get; set; } = TimeSpan.FromSeconds(15);
    public int BatchSize { get; set; } = 10;
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(30);
}
```

### 5. Testing and Debugging

```csharp
#if DEBUG
// Force specific variations for testing
public class TestOptimizelyWrapper : IOptimizely
{
    private readonly IOptimizely _wrapped;
    private readonly Dictionary<string, string> _forcedVariations;
    
    public TestOptimizelyWrapper(IOptimizely wrapped)
    {
        _wrapped = wrapped;
        _forcedVariations = new Dictionary<string, string>
        {
            { "product_sort_experiment", "sort_by_price" },
            { "checkout_experiment", "single_page" }
        };
    }
    
    public Variation GetVariation(string experimentKey, string userId, 
        UserAttributes userAttributes = null)
    {
        if (_forcedVariations.ContainsKey(experimentKey))
        {
            return new Variation 
            { 
                Key = _forcedVariations[experimentKey] 
            };
        }
        
        return _wrapped.GetVariation(experimentKey, userId, userAttributes);
    }
    
    // Implement other IOptimizely methods...
}
#endif
```

## Common Patterns

### 1. A/B Testing UI Components

```csharp
public class ProductListingComponent
{
    private readonly IOptimizely _optimizely;
    
    public IEnumerable<Product> GetSortedProducts(string userId, 
        IEnumerable<Product> products, UserAttributes attributes)
    {
        var variation = _optimizely.GetVariation("product_sort_experiment", userId, attributes);
        
        return variation?.Key switch
        {
            "sort_by_price" => products.OrderBy(p => p.Price),
            "sort_by_rating" => products.OrderByDescending(p => p.Rating),
            "sort_by_popularity" => products.OrderByDescending(p => p.ViewCount),
            _ => products.OrderBy(p => p.Name) // Default/control
        };
    }
}
```

### 2. Feature Flag Service

```csharp
public interface IFeatureFlagService
{
    bool IsFeatureEnabled(string featureKey, string userId, UserAttributes attributes = null);
    T GetFeatureVariable<T>(string featureKey, string variableKey, string userId, 
        UserAttributes attributes = null);
}

public class FeatureFlagService : IFeatureFlagService
{
    private readonly IOptimizely _optimizely;
    
    public bool IsFeatureEnabled(string featureKey, string userId, UserAttributes attributes = null)
    {
        return _optimizely.IsFeatureEnabled(featureKey, userId, attributes);
    }
    
    public T GetFeatureVariable<T>(string featureKey, string variableKey, string userId, 
        UserAttributes attributes = null)
    {
        return typeof(T) switch
        {
            Type t when t == typeof(string) => 
                (T)(object)_optimizely.GetFeatureVariableString(featureKey, variableKey, userId, attributes),
            Type t when t == typeof(int) => 
                (T)(object)_optimizely.GetFeatureVariableInteger(featureKey, variableKey, userId, attributes),
            Type t when t == typeof(bool) => 
                (T)(object)_optimizely.GetFeatureVariableBoolean(featureKey, variableKey, userId, attributes),
            Type t when t == typeof(double) => 
                (T)(object)_optimizely.GetFeatureVariableDouble(featureKey, variableKey, userId, attributes),
            _ => throw new ArgumentException($"Unsupported type: {typeof(T)}")
        };
    }
}
```

## Troubleshooting

### Common Issues

1. **Users not being bucketed**: Check experiment status and traffic allocation
2. **Events not tracking**: Verify event keys match your Optimizely configuration
3. **Performance issues**: Use BatchEventProcessor for high-volume applications
4. **Null variations**: Always handle cases where users aren't bucketed

### Debug Logging

```csharp
public class DebugLogger : ILogger
{
    public void Log(LogLevel level, string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}");
        
        // Also log to your application's logging system
        System.Diagnostics.Debug.WriteLine($"Optimizely: {message}");
    }
}
```

## Next Steps

1. **Start Small**: Begin with simple A/B tests on non-critical features
2. **Measure Everything**: Set up proper analytics and conversion tracking
3. **Iterate**: Use results to inform your next experiments
4. **Scale**: Gradually expand to more complex feature flags and experiments

For more advanced topics, refer to the [official Optimizely documentation](https://docs.developers.optimizely.com/experimentation/v4.0.0-full-stack/docs/csharp-sdk).