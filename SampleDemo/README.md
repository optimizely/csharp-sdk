# Optimizely C# SDK - Sample Demo Application

This is a comprehensive sample demo application that demonstrates how to use the Optimizely C# SDK for Feature Experimentation and A/B testing. The demo provides a practical, hands-on guide to implementing the key features of the Optimizely SDK.

## What This Demo Covers

This sample demo walks you through all the essential aspects of using the Optimizely C# SDK:

1. **SDK Initialization** - How to properly initialize the Optimizely client
2. **User Bucketing** - How users are assigned to experiment variations
3. **Feature Flag Evaluation** - How to check if features are enabled for users
4. **Event Tracking** - How to track conversion events for analytics
5. **Best Practices** - Production-ready patterns and recommendations

## Demo Scenario

The demo simulates an e-commerce application testing different product sorting algorithms:
- **Control**: Sort products alphabetically by name
- **Treatment**: Sort products by price (low to high)

This is a common real-world A/B testing scenario where you want to determine which sorting method leads to better user engagement and conversions.

## Prerequisites

- .NET 8.0 SDK or later
- Basic understanding of C# programming
- Familiarity with A/B testing concepts (helpful but not required)

## Running the Demo

### Option 1: Quick Start (Standalone)

```bash
# Navigate to the demo directory
cd SampleDemo/OptimizelySDK.SampleDemo

# Run the demo
dotnet run
```

### Option 2: Build and Run

```bash
# Build the project first
dotnet build

# Then run the executable
dotnet run
```

### Option 3: From Repository Root

```bash
# If you're at the repository root
cd SampleDemo/OptimizelySDK.SampleDemo && dotnet run
```

## Understanding the Demo Output

The demo will display the following sections:

### 1. SDK Initialization
```
Step 1: Initializing Optimizely SDK...
SDK initialized successfully with sample datafile
   Project ID: sample_project_123
   Number of experiments: 1
   Number of feature flags: 1
```

### 2. User Bucketing
```
Step 2: Demonstrating user bucketing into experiment variations...
Experiment: sort_products_experiment
   Testing: Product sorting by price vs name

   User: Alice (ID: user_123)
      -> Variation: sort_by_price
      -> Will see products sorted by: Price (Low to High)
      -> User attributes: Type=premium, Age=25
```

### 3. Feature Flag Evaluation
```
Step 3: Demonstrating feature flag evaluation...
Feature Flag: product_sorting
   Controls: Whether product sorting feature is enabled

   User: Alice
      -> Feature enabled: True
      -> Sort method variable: price
```

### 4. Event Tracking
```
Step 4: Demonstrating event tracking...
Event Tracking: add_to_cart
   Tracking: When users add products to their cart

   Tracked event for user: Alice
      -> Event: add_to_cart
      -> Revenue: $11,000 cents
      -> Category: electronics
```

### 5. Best Practices Guide
The demo concludes with a comprehensive list of best practices for production use.

## Demo Components Explained

### Sample Datafile
The demo uses a minimal, valid Optimizely datafile that includes:
- One experiment (`sort_products_experiment`) with two variations
- One event (`add_to_cart`) for tracking conversions
- One feature flag (`product_sorting`) with variables
- User attributes for targeting (`user_type`, `age`)

### Sample Users
The demo includes 5 sample users with different attributes:
- **Alice**: Premium user, age 25
- **Bob**: Standard user, age 35  
- **Charlie**: Premium user, age 28
- **Diana**: Standard user, age 32
- **Eva**: Premium user, age 22

### Key SDK Methods Demonstrated

1. **Optimizely Constructor**: Initialize the SDK with configuration
2. **GetVariation()**: Determine which variation a user should see
3. **IsFeatureEnabled()**: Check if a feature flag is enabled
4. **GetFeatureVariableString()**: Get feature variable values
5. **Track()**: Send conversion events to Optimizely

## Customizing the Demo

### Modifying the Experiment
To test different scenarios, you can modify:

1. **User Attributes**: Change the sample users' attributes in the `SampleUsers` array
2. **Experiment Configuration**: Modify the `SampleDatafile` JSON to test different experiments
3. **Event Data**: Adjust the event tags in the tracking demonstration

### Adding Your Own Datafile
Replace the `SampleDatafile` with your actual project datafile:

```csharp
// Replace this with your actual datafile URL or JSON
private static readonly string SampleDatafile = @"{ your datafile JSON }";
```

## Connecting to a Real Optimizely Project

To use this demo with your actual Optimizely project:

1. **Get Your Datafile**: Download your project's datafile from the Optimizely dashboard
2. **Update Configuration**: Replace the sample datafile with your project's datafile
3. **Update Keys**: Change the experiment keys, event keys, and feature flag keys to match your project
4. **Test with Real Users**: Use actual user IDs from your application

### Using OptimizelyFactory (Recommended for Production)

For production applications, use `OptimizelyFactory` instead of the manual initialization:

```csharp
// Initialize with SDK key for automatic datafile management
var optimizely = OptimizelyFactory.NewDefaultInstance("your_sdk_key");
```

## Common Use Cases

This demo demonstrates patterns for common A/B testing scenarios:

### E-commerce Testing
- Product sorting algorithms
- Checkout flow variations
- Pricing display methods
- Search result layouts

### Feature Rollouts
- Gradual feature enablement
- User segment targeting
- Configuration management
- Fallback behaviors

### Conversion Tracking
- Purchase events
- Sign-up completions
- Feature usage metrics
- Revenue attribution

## Troubleshooting

### Common Issues

1. **Build Errors**: Ensure you have .NET 8.0 SDK installed
2. **Missing Dependencies**: Run `dotnet restore` to restore NuGet packages
3. **API Errors**: Check that you're using the correct SDK method signatures

### Getting More Detailed Logging

To see more detailed SDK activity, you can modify the logger configuration:

```csharp
// For more verbose logging, implement a custom logger
public class VerboseLogger : ILogger
{
    public void Log(LogLevel level, string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {message}");
    }
}
```

## Next Steps

After running this demo, consider:

1. **Reading the Documentation**: Visit the [Optimizely C# SDK docs](https://docs.developers.optimizely.com/experimentation/v4.0.0-full-stack/docs/csharp-sdk)
2. **Exploring the Web Demo**: Check out the ASP.NET MVC demo in `OptimizelySDK.DemoApp`
3. **Setting Up Your Project**: Create your own Optimizely project and experiment
4. **Implementing in Your App**: Integrate the SDK into your actual application

## Additional Resources

- [Optimizely Developer Documentation](https://docs.developers.optimizely.com/)
- [C# SDK GitHub Repository](https://github.com/optimizely/csharp-sdk)
- [Optimizely Community Forum](https://community.optimizely.com/)
- [Feature Experimentation Product Guide](https://help.optimizely.com/Experiment_Build_and_Launch/Build_an_A_B_test)

## Support

If you have questions about this demo or the Optimizely C# SDK:

1. **Documentation**: Check the official SDK documentation first
2. **Community**: Post questions in the Optimizely Community Forum
3. **GitHub Issues**: Report bugs or request features on the GitHub repository
4. **Support**: Contact Optimizely support if you're a customer

---

*This demo is designed to be educational and may use simplified patterns for clarity. For production applications, always follow security best practices and refer to the official documentation.*