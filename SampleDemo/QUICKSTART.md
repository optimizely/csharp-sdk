# Optimizely C# SDK - Quick Start Guide

This guide gets you up and running with the Optimizely C# SDK in minutes. Perfect for developers who want to quickly understand and implement A/B testing and feature flags.

## ⚡ Quick Setup (5 minutes)

### 1. Install the SDK
```bash
dotnet add package Optimizely.SDK
```

### 2. Initialize the SDK
```csharp
using OptimizelySDK;

// Simple initialization
var optimizely = OptimizelyFactory.NewDefaultInstance("YOUR_SDK_KEY");
```

### 3. Run an A/B Test
```csharp
// Get variation for a user
var variation = optimizely.GetVariation("my_experiment", "user_123");

// Apply the variation
if (variation?.Key == "treatment")
{
    // Show treatment experience
}
else
{
    // Show control experience  
}
```

### 4. Track Conversions
```csharp
// Track when user converts
optimizely.Track("purchase", "user_123");
```

## 🎯 Sample Demo Application

We've created a complete sample demo that shows all SDK features in action:

```bash
# Clone and run the demo
cd SampleDemo/OptimizelySDK.SampleDemo
dotnet run
```

**What the demo shows:**
- ✅ SDK initialization with sample data
- ✅ User bucketing into experiment variations  
- ✅ Feature flag evaluation
- ✅ Event tracking with revenue data
- ✅ Production best practices

## 🏪 Real-World Example: E-commerce Product Sorting

Here's a practical example testing different product sorting methods:

### The Experiment Setup
```csharp
public class ProductController : Controller
{
    private readonly IOptimizely _optimizely;
    
    public ProductController(IOptimizely optimizely)
    {
        _optimizely = optimizely;
    }
    
    public IActionResult ProductList(string userId)
    {
        // Get user's variation
        var variation = _optimizely.GetVariation("product_sort_test", userId);
        
        // Get products from database
        var products = GetProducts();
        
        // Apply sorting based on variation
        var sortedProducts = variation?.Key switch
        {
            "sort_by_price" => products.OrderBy(p => p.Price),
            "sort_by_rating" => products.OrderByDescending(p => p.Rating),
            _ => products.OrderBy(p => p.Name) // Control: alphabetical
        };
        
        return View(sortedProducts);
    }
}
```

### Track When Users Buy
```csharp
[HttpPost]
public IActionResult Purchase(string userId, int productId, decimal amount)
{
    // Process the purchase
    ProcessPurchase(userId, productId, amount);
    
    // Track conversion event with revenue
    var eventTags = new EventTags
    {
        { "revenue", (int)(amount * 100) }, // Revenue in cents
        { "product_id", productId }
    };
    
    _optimizely.Track("purchase", userId, null, eventTags);
    
    return View("PurchaseComplete");
}
```

## 🏁 Feature Flags Example

Test new features safely with feature flags:

```csharp
public class CheckoutController : Controller
{
    private readonly IOptimizely _optimizely;
    
    public IActionResult Checkout(string userId)
    {
        // Check if new checkout flow is enabled for this user
        bool useNewCheckout = _optimizely.IsFeatureEnabled("new_checkout_flow", userId);
        
        if (useNewCheckout)
        {
            // Get configuration variables
            string buttonColor = _optimizely.GetFeatureVariableString(
                "new_checkout_flow", "button_color", userId);
            int maxItems = _optimizely.GetFeatureVariableInteger(
                "new_checkout_flow", "max_items", userId);
            
            return View("NewCheckout", new CheckoutViewModel 
            { 
                ButtonColor = buttonColor,
                MaxItems = maxItems 
            });
        }
        
        return View("OriginalCheckout");
    }
}
```

## 🎨 Setting Up Your First Experiment

### 1. In Optimizely Dashboard
1. Create a new experiment: "Product Sort Test"
2. Add variations:
   - **Control**: `sort_by_name` 
   - **Treatment**: `sort_by_price`
3. Create conversion event: `purchase`
4. Start the experiment

### 2. In Your Code
```csharp
public class ProductService
{
    private readonly IOptimizely _optimizely;
    
    public IEnumerable<Product> GetSortedProducts(string userId, List<Product> products)
    {
        // Bucket user into experiment  
        var variation = _optimizely.Activate("product_sort_test", userId);
        
        // This automatically tracks an impression
        return variation?.Key switch
        {
            "sort_by_price" => products.OrderBy(p => p.Price),
            _ => products.OrderBy(p => p.Name)
        };
    }
    
    public void TrackPurchase(string userId, decimal revenue)
    {
        var eventTags = new EventTags { { "revenue", (int)(revenue * 100) } };
        _optimizely.Track("purchase", userId, null, eventTags);
    }
}
```

## 🛠️ Production Setup

### Dependency Injection (ASP.NET Core)
```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<IOptimizely>(provider =>
    {
        return OptimizelyFactory.NewDefaultInstance(
            Configuration["Optimizely:SdkKey"]
        );
    });
}
```

### Error Handling
```csharp
public string GetVariationSafely(string experimentKey, string userId, string fallback = "control")
{
    try
    {
        var variation = _optimizely.GetVariation(experimentKey, userId);
        return variation?.Key ?? fallback;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Optimizely error for experiment {Experiment}", experimentKey);
        return fallback;
    }
}
```

## 📊 Key Metrics to Track

### Conversion Events
```csharp
// E-commerce
_optimizely.Track("purchase", userId, null, new EventTags { { "revenue", 2999 } });
_optimizely.Track("add_to_cart", userId);
_optimizely.Track("checkout_started", userId);

// SaaS
_optimizely.Track("signup", userId);
_optimizely.Track("subscription", userId, null, new EventTags { { "plan", "premium" } });
_optimizely.Track("feature_used", userId);

// Content/Media
_optimizely.Track("video_watched", userId, null, new EventTags { { "duration", 120 } });
_optimizely.Track("article_read", userId);
_optimizely.Track("share", userId, null, new EventTags { { "platform", "facebook" } });
```

## 🚀 Advanced Features

### User Context (Recommended for Multiple Decisions)
```csharp
// Create user context once, reuse for multiple decisions
var user = _optimizely.CreateUserContext("user_123", new UserAttributes 
{
    { "subscription_type", "premium" },
    { "age", 25 }
});

// Make multiple decisions efficiently
var checkoutDecision = user.Decide("new_checkout_flow");
var sortingDecision = user.Decide("product_sorting");
var recommendationDecision = user.Decide("ai_recommendations");

if (checkoutDecision.Enabled)
{
    // Use new checkout with variables
    var buttonColor = checkoutDecision.Variables["button_color"].ToString();
}
```

### Batch Event Processing
```csharp
// For high-volume applications
var eventProcessor = new BatchEventProcessor.Builder()
    .WithMaxBatchSize(50)
    .WithFlushInterval(TimeSpan.FromSeconds(30))
    .Build();

var optimizely = new Optimizely(datafile, eventProcessor: eventProcessor);
```

## 🔍 Testing & Debugging

### Force Specific Variations (for testing)
```csharp
// Force user into specific variation for testing
_optimizely.SetForcedVariation("my_experiment", "test_user", "treatment");

// Check what variation a user is forced into  
var forcedVariation = _optimizely.GetForcedVariation("my_experiment", "test_user");
```

### Debug Logging
```csharp
public class ConsoleLogger : ILogger
{
    public void Log(LogLevel level, string message)
    {
        Console.WriteLine($"[Optimizely {level}] {message}");
    }
}

// Use when initializing
var optimizely = new Optimizely(datafile, logger: new ConsoleLogger());
```

## 📚 Learning Path

1. **Start Here**: Run the sample demo (`dotnet run` in `SampleDemo/OptimizelySDK.SampleDemo`)
2. **Read the Tutorial**: Check out `TUTORIAL.md` for detailed step-by-step instructions
3. **Explore the Web Demo**: Look at the ASP.NET MVC demo in `OptimizelySDK.DemoApp`
4. **Build Your First Test**: Create a simple A/B test in your application
5. **Add Feature Flags**: Implement feature flags for safer deployments
6. **Scale Up**: Use advanced features like user contexts and batch processing

## 🆘 Need Help?

- **Demo Issues**: All sample code is in `SampleDemo/OptimizelySDK.SampleDemo`
- **Documentation**: [Official C# SDK Docs](https://docs.developers.optimizely.com/experimentation/v4.0.0-full-stack/docs/csharp-sdk)
- **Community**: [Optimizely Developer Community](https://community.optimizely.com/)
- **Support**: Check the GitHub issues or contact Optimizely support

---

**🎉 You're ready to start experimenting!** Begin with simple A/B tests and gradually add more sophisticated feature flags and experiments as you get comfortable with the SDK.