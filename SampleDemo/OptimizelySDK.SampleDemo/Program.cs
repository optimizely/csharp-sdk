using OptimizelySDK;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event.Dispatcher;
using Newtonsoft.Json;

namespace OptimizelySDK.SampleDemo
{
    /// <summary>
    /// Sample demo application showing how to use the Optimizely C# SDK
    /// This demo demonstrates:
    /// 1. SDK initialization with a sample datafile
    /// 2. User bucketing into experiment variations
    /// 3. Feature flag evaluation  
    /// 4. Event tracking
    /// 5. Best practices for SDK usage
    /// </summary>
    class Program
    {
        // Sample datafile JSON for demonstration (minimal valid datafile)
        private static readonly string SampleDatafile = @"{
            ""version"": ""4"",
            ""projectId"": ""sample_project_123"",
            ""experiments"": [
                {
                    ""id"": ""exp_001"",
                    ""key"": ""sort_products_experiment"",
                    ""layerId"": ""layer_001"",
                    ""status"": ""Running"",
                    ""audienceIds"": [],
                    ""variations"": [
                        {
                            ""id"": ""var_001"",
                            ""key"": ""sort_by_price"",
                            ""variables"": []
                        },
                        {
                            ""id"": ""var_002"",
                            ""key"": ""sort_by_name"",
                            ""variables"": []
                        }
                    ],
                    ""trafficAllocation"": [
                        {
                            ""entityId"": ""var_001"",
                            ""endOfRange"": 5000
                        },
                        {
                            ""entityId"": ""var_002"",
                            ""endOfRange"": 10000
                        }
                    ]
                }
            ],
            ""events"": [
                {
                    ""id"": ""event_001"",
                    ""key"": ""add_to_cart"",
                    ""experimentIds"": [""exp_001""]
                }
            ],
            ""groups"": [],
            ""attributes"": [
                {
                    ""id"": ""attr_001"",
                    ""key"": ""user_type""
                },
                {
                    ""id"": ""attr_002"",
                    ""key"": ""age""
                }
            ],
            ""audiences"": [],
            ""layers"": [],
            ""featureFlags"": [
                {
                    ""id"": ""feature_001"",
                    ""key"": ""product_sorting"",
                    ""layerId"": ""layer_001"",
                    ""status"": ""launched"",
                    ""rolloutId"": """",
                    ""experimentIds"": [""exp_001""],
                    ""variables"": [
                        {
                            ""id"": ""var_001"",
                            ""key"": ""sort_method"",
                            ""type"": ""string"",
                            ""defaultValue"": ""name""
                        }
                    ]
                }
            ],
            ""rollouts"": [],
            ""typedAudiences"": [],
            ""revision"": ""1""
        }";

        // Sample users for testing
        private static readonly (string UserId, string Name, int Age, string UserType)[] SampleUsers = {
            ("user_123", "Alice", 25, "premium"),
            ("user_456", "Bob", 35, "standard"),
            ("user_789", "Charlie", 28, "premium"),
            ("user_101", "Diana", 32, "standard"),
            ("user_112", "Eva", 22, "premium")
        };

        static void Main(string[] args)
        {
            Console.WriteLine("==========================================");
            Console.WriteLine("Optimizely C# SDK - Sample Demo");
            Console.WriteLine("==========================================\n");

            try
            {
                // Step 1: Initialize the SDK
                Console.WriteLine("Step 1: Initializing Optimizely SDK...\n");
                var optimizely = InitializeOptimizely();
                
                // Step 2: Demonstrate user bucketing
                Console.WriteLine("Step 2: Demonstrating user bucketing into experiment variations...\n");
                DemonstrateUserBucketing(optimizely);
                
                // Step 3: Demonstrate feature flags
                Console.WriteLine("Step 3: Demonstrating feature flag evaluation...\n");
                DemonstrateFeatureFlags(optimizely);
                
                // Step 4: Demonstrate event tracking
                Console.WriteLine("Step 4: Demonstrating event tracking...\n");
                DemonstrateEventTracking(optimizely);
                
                // Step 5: Best practices summary
                Console.WriteLine("Step 5: Best Practices Summary\n");
                DisplayBestPractices();
                
                Console.WriteLine("Demo completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during demo: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Initialize the Optimizely SDK with proper configuration
        /// </summary>
        private static Optimizely InitializeOptimizely()
        {
            // Create a custom logger to see SDK activity
            var logger = new DefaultLogger();
            
            // Create error handler
            var errorHandler = new DefaultErrorHandler(logger);
            
            // Create event dispatcher (for tracking events)
            var eventDispatcher = new DefaultEventDispatcher(logger);
            
            // Initialize the SDK with the sample datafile
            var optimizely = new Optimizely(
                datafile: SampleDatafile,
                eventDispatcher: eventDispatcher,
                logger: logger,
                errorHandler: errorHandler,
                skipJsonValidation: false);
            
            Console.WriteLine("SDK initialized successfully with sample datafile");
            Console.WriteLine($"   Project ID: {optimizely.ProjectConfigManager?.GetConfig()?.ProjectId}");
            Console.WriteLine($"   Number of experiments: {optimizely.ProjectConfigManager?.GetConfig()?.ExperimentKeyMap?.Count}");
            Console.WriteLine($"   Number of feature flags: {optimizely.ProjectConfigManager?.GetConfig()?.FeatureKeyMap?.Count}");
            Console.WriteLine();
            
            return optimizely;
        }

        /// <summary>
        /// Demonstrate how users are bucketed into experiment variations
        /// </summary>
        private static void DemonstrateUserBucketing(Optimizely optimizely)
        {
            const string experimentKey = "sort_products_experiment";
            
            Console.WriteLine($"Experiment: {experimentKey}");
            Console.WriteLine("   Testing: Product sorting by price vs name\n");
            
            foreach (var user in SampleUsers)
            {
                // Create user attributes
                var userAttributes = new UserAttributes
                {
                    { "user_type", user.UserType },
                    { "age", user.Age }
                };
                
                // Get the variation for this user
                var variation = optimizely.GetVariation(experimentKey, user.UserId, userAttributes);
                
                // Display results
                var variationName = variation?.Key ?? "No variation (user not bucketed)";
                var sortMethod = variation?.Key == "sort_by_price" ? "Price (Low to High)" : "Alphabetical (A-Z)";
                
                Console.WriteLine($"   User: {user.Name} (ID: {user.UserId})");
                Console.WriteLine($"      -> Variation: {variationName}");
                Console.WriteLine($"      -> Will see products sorted by: {sortMethod}");
                Console.WriteLine($"      -> User attributes: Type={user.UserType}, Age={user.Age}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Demonstrate feature flag evaluation
        /// </summary>
        private static void DemonstrateFeatureFlags(Optimizely optimizely)
        {
            const string featureFlagKey = "product_sorting";
            
            Console.WriteLine($"Feature Flag: {featureFlagKey}");
            Console.WriteLine("   Controls: Whether product sorting feature is enabled\n");
            
            foreach (var user in SampleUsers.Take(3)) // Just show first 3 users for brevity
            {
                var userAttributes = new UserAttributes
                {
                    { "user_type", user.UserType },
                    { "age", user.Age }
                };
                
                // Check if feature is enabled for this user
                var isEnabled = optimizely.IsFeatureEnabled(featureFlagKey, user.UserId, userAttributes);
                
                // Get feature variable value
                var sortMethod = optimizely.GetFeatureVariableString(featureFlagKey, "sort_method", user.UserId, userAttributes);
                
                Console.WriteLine($"   User: {user.Name}");
                Console.WriteLine($"      -> Feature enabled: {isEnabled}");
                Console.WriteLine($"      -> Sort method variable: {sortMethod}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Demonstrate event tracking for conversion metrics
        /// </summary>
        private static void DemonstrateEventTracking(Optimizely optimizely)
        {
            const string eventKey = "add_to_cart";
            
            Console.WriteLine($"Event Tracking: {eventKey}");
            Console.WriteLine("   Tracking: When users add products to their cart\n");
            
            // Simulate some users performing the conversion event
            var convertingUsers = SampleUsers.Take(3);
            
            foreach (var user in convertingUsers)
            {
                var userAttributes = new UserAttributes
                {
                    { "user_type", user.UserType },
                    { "age", user.Age }
                };
                
                // Simulate event with revenue data
                var eventTags = new EventTags
                {
                    { "revenue", Random.Shared.Next(10, 200) * 100 }, // Revenue in cents
                    { "product_category", "electronics" },
                    { "cart_size", Random.Shared.Next(1, 5) }
                };
                
                // Track the conversion event
                optimizely.Track(eventKey, user.UserId, userAttributes, eventTags);
                
                Console.WriteLine($"   Tracked event for user: {user.Name}");
                Console.WriteLine($"      -> Event: {eventKey}");
                Console.WriteLine($"      -> Revenue: ${eventTags["revenue"]:N0} cents");
                Console.WriteLine($"      -> Category: {eventTags["product_category"]}");
                Console.WriteLine();
            }
            
            Console.WriteLine("Events tracked successfully! In a real application, these would be sent to Optimizely for analysis.");
            Console.WriteLine();
        }

        /// <summary>
        /// Display best practices for using the Optimizely SDK
        /// </summary>
        private static void DisplayBestPractices()
        {
            Console.WriteLine("Optimizely C# SDK Best Practices:");
            Console.WriteLine();
            Console.WriteLine("1. SDK Initialization:");
            Console.WriteLine("   - Initialize the SDK once at application startup");
            Console.WriteLine("   - Use dependency injection to share the Optimizely instance");
            Console.WriteLine("   - Consider using OptimizelyFactory for simple setups");
            Console.WriteLine();
            Console.WriteLine("2. Datafile Management:");
            Console.WriteLine("   - Use HttpProjectConfigManager for automatic datafile updates");
            Console.WriteLine("   - Provide a fallback datafile for offline scenarios");
            Console.WriteLine("   - Monitor datafile update notifications");
            Console.WriteLine();
            Console.WriteLine("3. User Management:");
            Console.WriteLine("   - Use consistent user IDs across sessions");
            Console.WriteLine("   - Pass relevant user attributes for targeting");
            Console.WriteLine("   - Don't include PII in user attributes");
            Console.WriteLine();
            Console.WriteLine("4. Experimentation:");
            Console.WriteLine("   - Call Activate() when exposing users to experiments");
            Console.WriteLine("   - Use GetVariation() when you only need the variation");
            Console.WriteLine("   - Handle null variations gracefully (user not bucketed)");
            Console.WriteLine();
            Console.WriteLine("5. Event Tracking:");
            Console.WriteLine("   - Track events immediately after user actions");
            Console.WriteLine("   - Include relevant event tags (revenue, product info, etc.)");
            Console.WriteLine("   - Use BatchEventProcessor for high-volume applications");
            Console.WriteLine();
            Console.WriteLine("6. Feature Flags:");
            Console.WriteLine("   - Use IsFeatureEnabled() to check feature status");
            Console.WriteLine("   - Leverage feature variables for dynamic configuration");
            Console.WriteLine("   - Implement feature flag fallbacks for reliability");
            Console.WriteLine();
            Console.WriteLine("7. Error Handling:");
            Console.WriteLine("   - Implement custom error handlers for production");
            Console.WriteLine("   - Use appropriate log levels (ERROR, WARN, INFO, DEBUG)");
            Console.WriteLine("   - Monitor SDK performance and errors");
            Console.WriteLine();
        }
    }
}