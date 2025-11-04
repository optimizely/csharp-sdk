/**
 * CMAB Testing Example for Optimizely C# SDK
 *
 * This file contains comprehensive test scenarios for CMAB functionality
 * Based on: https://github.com/optimizely/javascript-sdk/tree/main/examples/cmab
 *
 * To run:
 *   dotnet build
 *   dotnet run --test=basic
 *   dotnet run --test=cache_hit
 *   dotnet run (runs all tests)
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OptimizelySDK;
using OptimizelySDK.Cmab;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.OptimizelyDecisions;

namespace CmabTests
{
    /// <summary>
    /// Console logger with log level filtering
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private readonly LogLevel _minLevel;

        public ConsoleLogger(LogLevel minLevel = LogLevel.INFO)
        {
            _minLevel = minLevel;
        }

        public void Log(LogLevel level, string message)
        {
            if (level < _minLevel)
                return;

            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var levelStr = level.ToString().PadRight(5);
            Console.WriteLine($"[{timestamp}] [{levelStr}] {message}");
        }
    }

    class Program
    {
        // ========================================
        // CONFIGURATION
        // ========================================

        // SDK Key from rc (prep) environment
        private const string SDK_KEY = "YOUR_SDK_KEY"; // rc (prep)
        private const string FLAG_KEY = "cmab_test";

        // Test user IDs
        private const string USER_QUALIFIED = "test_user_99"; // Will be bucketed into CMAB
        private const string USER_NOT_BUCKETED = "test_user"; // Won't be bucketed (traffic allocation)
        private const string USER_CACHE_TEST = "cache_user_123";

        // ========================================
        // HELPER FUNCTIONS
        // ========================================

        /// <summary>
        /// Print decision details
        /// </summary>
        private static void PrintDecision(string label, OptimizelyDecision decision)
        {
            Console.WriteLine($"\n{label}:");
            Console.WriteLine($"  Enabled: {decision.Enabled}");
        Console.WriteLine($"  Variation: {decision.VariationKey}");
        Console.WriteLine($"  Rule: {decision.RuleKey ?? "N/A"}");

        var variablesDict = decision.Variables?.ToDictionary();
        if (variablesDict != null && variablesDict.Count > 0)
        {
            Console.WriteLine($"  Variables: {string.Join(", ", variablesDict.Select(kv => $"{kv.Key}={kv.Value}"))}");
        }            if (decision.Reasons != null && decision.Reasons.Length > 0)
            {
                Console.WriteLine("  Reasons:");
                foreach (var reason in decision.Reasons)
                {
                    Console.WriteLine($"    - {reason}");
                }
            }

            Console.WriteLine("  [Check debug logs above for CMAB UUID and calls]");
        }

        /// <summary>
        /// Sleep utility
        /// </summary>
        private static void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }

        // ========================================
        // TEST FUNCTIONS
        // ========================================

        /// <summary>
        /// Test 1: Basic CMAB functionality
        /// </summary>
        private static void TestBasicCMAB(Optimizely optimizelyClient)
        {
            Console.WriteLine("\n--- Test: Basic CMAB Functionality ---");
            Console.WriteLine("=== CMAB Testing Suite for C# SDK ===");
            Console.WriteLine($"Testing CMAB with rc environment");
            Console.WriteLine($"SDK Key: {SDK_KEY}");
            Console.WriteLine($"Flag Key: {FLAG_KEY}\n");

            // Test with user who qualifies for CMAB
            var userContext = optimizelyClient.CreateUserContext(USER_QUALIFIED, new UserAttributes
            {
                { "hello", true }
            });

            var decision = userContext.Decide(FLAG_KEY);
            PrintDecision("CMAB Qualified User", decision);

            // cache miss - different attributes
            var userContext2 = optimizelyClient.CreateUserContext(USER_QUALIFIED, new UserAttributes
            {
                { "country", "ru" }
            });

            var decision2 = userContext2.Decide(FLAG_KEY);
            PrintDecision("CMAB Qualified User2", decision2);

            Console.WriteLine("==========================");
        }

        /// <summary>
        /// Test 2: Cache hit - same user and attributes
        /// Expected:
        /// 1. Decision 1: "hello" → Passes audience → CMAB API call → Cache stored for user + "hello"
        /// 2. Decision 2: Same user, same "hello" → Passes audience → Cache hit (same cache key) → Returns cached result (no API call)
        /// </summary>
        private static void TestCacheHit(Optimizely optimizelyClient)
        {
            Console.WriteLine("\n--- Test: Cache Hit (Same User & Attributes) ---");

            var userContext = optimizelyClient.CreateUserContext(USER_CACHE_TEST, new UserAttributes
            {
                { "hello", true }
            });

            // First decision - should call CMAB service
            Console.WriteLine("First decision (CMAB call):");
            var decision1 = userContext.Decide(FLAG_KEY);
            PrintDecision("Decision 1", decision1);

            var userContext2 = optimizelyClient.CreateUserContext(USER_CACHE_TEST, new UserAttributes
            {
                { "hello", true }
            });

            // Second decision - hit cache
            Console.WriteLine("\nSecond decision (Cache hit):");
            var decision2 = userContext2.Decide(FLAG_KEY);
            PrintDecision("Decision 2", decision2);
        }

        /// <summary>
        /// Test 3: Cache miss when relevant attributes change
        /// Expected:
        ///  1. Decision 1: "hello" → Passes audience → CMAB API call → Cache stored for "hello"
        ///  2. Decision 2: "world" → Passes audience → Cache miss (different attribute value) → New CMAB API call → Cache stored for "world"
        ///  3. Decision 3: "world" → Passes audience → Cache hit (same attribute) → Uses cached result
        /// </summary>
        private static void TestCacheMissOnAttributeChange(Optimizely optimizelyClient)
        {
            Console.WriteLine("\n--- Test: Cache Miss on Attribute Change ---");

            // First decision with valid attribute
            var userContext1 = optimizelyClient.CreateUserContext(USER_CACHE_TEST + "_attr", new UserAttributes
            {
                { "hello", true }
            });

            Console.WriteLine("Decision with 'hello':");
            var decision1 = userContext1.Decide(FLAG_KEY);
            PrintDecision("Decision 1", decision1);

            // Second decision with changed valid attribute
            var userContext2 = optimizelyClient.CreateUserContext(USER_CACHE_TEST + "_attr", new UserAttributes
            {
                { "world", true }
            });

            Console.WriteLine("\nDecision with 'world' (cache miss expected):");
            var decision2 = userContext2.Decide(FLAG_KEY);
            PrintDecision("Decision 2", decision2);

            // Third decision with same user and attributes
            var userContext3 = optimizelyClient.CreateUserContext(USER_CACHE_TEST + "_attr", new UserAttributes
            {
                { "world", true }
            });

            Console.WriteLine("\nDecision with same user and attributes (cache hit expected):");
            var decision3 = userContext3.Decide(FLAG_KEY);
            PrintDecision("Decision 3", decision3);
        }

        /// <summary>
        /// Test 4: IGNORE_CMAB_CACHE option
        /// Expected:
        /// 1. Decision 1: "hello" → Passes audience → CMAB API call → Cache stored for user + "hello"
        /// 2. Decision 2: Same user, same "hello" + IGNORE_CMAB_CACHE → Passes audience → Cache bypassed → New CMAB API call (original cache preserved)
        /// 3. Decision 3: Same user, same "hello" → Passes audience → Cache hit → Uses original cached result (no API call)
        /// </summary>
        private static void TestIgnoreCacheOption(Optimizely optimizelyClient)
        {
            Console.WriteLine("\n--- Test: IGNORE_CMAB_CACHE Option ---");

            var userContext = optimizelyClient.CreateUserContext(USER_CACHE_TEST + "_ignore", new UserAttributes
            {
                { "hello", true }
            });

            // First decision - populate cache
            Console.WriteLine("First decision (populate cache):");
            var decision1 = userContext.Decide(FLAG_KEY);
            PrintDecision("Decision 1", decision1);

            // Second decision with IGNORE_CMAB_CACHE
            Console.WriteLine("\nSecond decision with IGNORE_CMAB_CACHE:");
            var decision2 = userContext.Decide(FLAG_KEY, new[] { OptimizelyDecideOption.IGNORE_CMAB_CACHE });
            PrintDecision("Decision 2 (ignored cache)", decision2);

            // Third decision - should use original cache
            Console.WriteLine("\nThird decision (should use original cache):");
            var decision3 = userContext.Decide(FLAG_KEY);
            PrintDecision("Decision 3", decision3);
        }

        /// <summary>
        /// Test 5: RESET_CMAB_CACHE option
        /// Expected:
        /// 1. User 1: "hello" → CMAB API call → Cache stored for User 1
        /// 2. User 2: "hello" → CMAB API call → Cache stored for User 2
        /// 3. User 1: RESET_CMAB_CACHE → Clears entire cache → New CMAB API call for User 1
        /// 4. User 2: Same "hello" → Cache was cleared → New CMAB API call for User 2 (no cached result)
        /// </summary>
        private static void TestResetCacheOption(Optimizely optimizelyClient)
        {
            Console.WriteLine("\n--- Test: RESET_CMAB_CACHE Option ---");

            // Setup two different users
            var userContext1 = optimizelyClient.CreateUserContext("reset_user_1", new UserAttributes
            {
                { "hello", true }
            });

            var userContext2 = optimizelyClient.CreateUserContext("reset_user_2", new UserAttributes
            {
                { "hello", true }
            });

            // Populate cache for both users
            Console.WriteLine("Populating cache for User 1:");
            var decision1 = userContext1.Decide(FLAG_KEY);
            PrintDecision("User 1 Decision", decision1);

            Console.WriteLine("\nPopulating cache for User 2:");
            var decision2 = userContext2.Decide(FLAG_KEY);
            PrintDecision("User 2 Decision", decision2);

            // Reset entire cache
            Console.WriteLine("\nResetting entire CMAB cache:");
            var decision3 = userContext1.Decide(FLAG_KEY, new[] { OptimizelyDecideOption.RESET_CMAB_CACHE });
            PrintDecision("User 1 after RESET", decision3);

            // Check if User 2's cache was also cleared
            Console.WriteLine("\nUser 2 after cache reset (should refetch):");
            var decision4 = userContext2.Decide(FLAG_KEY);
            PrintDecision("User 2 after reset", decision4);
        }

        /// <summary>
        /// Test 6: INVALIDATE_USER_CMAB_CACHE option
        /// Expected:
        /// 1. User 1: "hello" → CMAB API call → Cache stored for User 1
        /// 2. User 2: "hello" → CMAB API call → Cache stored for User 2
        /// 3. User 1: INVALIDATE_USER_CMAB_CACHE → Clears only User 1's cache → New CMAB API call for User 1
        /// 4. User 2: Same "hello" → User 2's cache preserved → Cache hit (no API call)
        /// </summary>
        private static void TestInvalidateUserCacheOption(Optimizely optimizelyClient)
        {
            Console.WriteLine("\n--- Test: INVALIDATE_USER_CMAB_CACHE Option ---");

            // Setup two different users
            var userContext1 = optimizelyClient.CreateUserContext("invalidate_user_1", new UserAttributes
            {
                { "hello", true }
            });

            var userContext2 = optimizelyClient.CreateUserContext("invalidate_user_2", new UserAttributes
            {
                { "hello", true }
            });

            // Populate cache for both users
            Console.WriteLine("Populating cache for User 1:");
            var decision1 = userContext1.Decide(FLAG_KEY);
            PrintDecision("User 1 Initial", decision1);

            Console.WriteLine("\nPopulating cache for User 2:");
            var decision2 = userContext2.Decide(FLAG_KEY);
            PrintDecision("User 2 Initial", decision2);

            // Invalidate only User 1's cache
            Console.WriteLine("\nInvalidating User 1's cache only:");
            var decision3 = userContext1.Decide(FLAG_KEY, new[] { OptimizelyDecideOption.INVALIDATE_USER_CMAB_CACHE });
            PrintDecision("User 1 after INVALIDATE", decision3);

            // Check if User 2's cache is still valid
            Console.WriteLine("\nUser 2 after User 1 invalidation (should use cache):");
            var decision4 = userContext2.Decide(FLAG_KEY);
            PrintDecision("User 2 still cached", decision4);
        }

        /// <summary>
        /// Test 7: Concurrent requests for same user - verify thread safety
        /// Expected: 1 CMAB API call + 4 cache hits
        /// All concurrent requests should return same variation for consistency
        /// </summary>
        private static void TestConcurrentRequests(Optimizely optimizelyClient)
        {
            Console.WriteLine("\n--- Test: Concurrent Requests ---");

            var userContext = optimizelyClient.CreateUserContext("concurrent_user", new UserAttributes
            {
                { "hello", true }
            });

            // Launch 5 concurrent requests
            Console.WriteLine("Launching 5 concurrent decide calls...");
            var tasks = new List<Task<OptimizelyDecision>>();
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                tasks.Add(Task.Run(() =>
                {
                    return userContext.Decide(FLAG_KEY);
                }));
            }

            Task.WaitAll(tasks.ToArray());
            var decisions = tasks.Select(t => t.Result).ToArray();

            // Check variations
            var variations = new Dictionary<string, int>();
            for (int i = 0; i < decisions.Length; i++)
            {
                var decision = decisions[i];
                Console.WriteLine($"  Task {i} completed - Variation: {decision.VariationKey ?? "null"}");
                var key = decision.VariationKey ?? "null";
                if (variations.ContainsKey(key))
                    variations[key]++;
                else
                    variations[key] = 1;
            }

            Console.WriteLine("\nResults:");
            foreach (var kvp in variations)
            {
                Console.WriteLine($"  Variation '{kvp.Key}': {kvp.Value} times");
            }

            if (variations.Count == 1)
            {
                Console.WriteLine("✓ Concurrent handling correct: All returned same variation");
            }
            else
            {
                Console.WriteLine("✗ Issue with concurrent handling: Different variations returned");
            }

            Console.WriteLine("\nExpected: Only 1 CMAB API call, all return same variation");
        }

        /// <summary>
        /// Test 8: Fallback when user doesn't qualify for CMAB
        /// </summary>
        private static void TestFallbackWhenNotQualified(Optimizely optimizelyClient)
        {
            Console.WriteLine("\n--- Test: Fallback When Not Qualified for CMAB ---");

            // User with attributes that don't match CMAB audience
            var userContext = optimizelyClient.CreateUserContext("fallback_user", new UserAttributes());

            var decision = userContext.Decide(FLAG_KEY);
            PrintDecision("Non-CMAB User", decision);

            Console.WriteLine("\nExpected: No CMAB API call in debug logs above, falls through to next rule");
        }

        /// <summary>
        /// Test 9: Traffic allocation check with 50% traffic allocation (0 - 5000) - test_user_1
        /// </summary>
        private static void TestTrafficAllocation(Optimizely optimizelyClient)
        {
            Console.WriteLine("\n--- Test: Traffic Allocation Check ---");

            // User in traffic allocation (test_user_1)
            var userContext1 = optimizelyClient.CreateUserContext(USER_NOT_BUCKETED, new UserAttributes
            {
                { "hello", true }
            });

            var decision1 = userContext1.Decide(FLAG_KEY);
            PrintDecision("User in Traffic", decision1);

            // User not in traffic allocation (test_user_99)
            var userContext2 = optimizelyClient.CreateUserContext(USER_QUALIFIED, new UserAttributes
            {
                { "hello", true }
            });

            var decision2 = userContext2.Decide(FLAG_KEY);
            PrintDecision("User not in Traffic", decision2);

            Console.WriteLine("\nExpected: Only first user triggers CMAB API call");
        }

        /// <summary>
        /// Test 10: Event tracking with CMAB UUID
        /// </summary>
        private static void TestEventTracking(Optimizely optimizelyClient)
        {
            Console.WriteLine("\n--- Test: Event Tracking with CMAB UUID ---");
            // Do this for 10 different users to see CMAB UUID in logs
            for (int i = 0; i < 10; i++)
            {
                var userId = $"{USER_NOT_BUCKETED}_{i}";
                var userContextLoop = optimizelyClient.CreateUserContext(userId, new UserAttributes
                {
                    { "hello", true }
                });

                // Make CMAB decision
                var decisionLoop = userContextLoop.Decide(FLAG_KEY);
                PrintDecision($"Decision for Events - User {i + 1}", decisionLoop);

                // Track a conversion event
                userContextLoop.TrackEvent("cmab_event");
                Console.WriteLine($"Conversion event tracked: 'cmab_event' for user {userId}\n");
            }
        }

        /// <summary>
        /// Test 11: Performance benchmarks
        /// Expected: Cache hits should be significantly faster than API calls
        /// </summary>
        private static void TestPerformanceBenchmarks(Optimizely optimizelyClient)
        {
            Console.WriteLine("\n--- Test: Performance Benchmarks ---");

            var userContext = optimizelyClient.CreateUserContext("perf_user", new UserAttributes
            {
                { "hello", true }
            });

            // Measure first call (API call)
            var sw = Stopwatch.StartNew();
            var decision1 = userContext.Decide(FLAG_KEY);
            sw.Stop();
            var apiDuration = sw.ElapsedMilliseconds;

            PrintDecision("First Call (API)", decision1);
            Console.WriteLine($"API call duration: {apiDuration}ms");

            // Measure cached calls
            var cachedDurations = new List<long>();
            for (int i = 0; i < 10; i++)
            {
                sw.Restart();
                userContext.Decide(FLAG_KEY);
                sw.Stop();
                cachedDurations.Add(sw.ElapsedMilliseconds);
            }

            var avgCached = cachedDurations.Average();

            Console.WriteLine($"Average cached call duration: {avgCached:F2}ms (10 calls)");
            Console.WriteLine("\nPerformance Targets:");
            Console.WriteLine($"- Cached calls: <10ms (actual: {avgCached:F2}ms)");
            Console.WriteLine($"- API calls: <500ms (actual: {apiDuration}ms)");

            if (avgCached < 10)
            {
                Console.WriteLine("✓ Cached performance: PASS");
            }
            else
            {
                Console.WriteLine("✗ Cached performance: FAIL");
            }

            if (apiDuration < 500)
            {
                Console.WriteLine("✓ API performance: PASS");
            }
            else
            {
                Console.WriteLine("✗ API performance: FAIL");
            }
        }

        /// <summary>
        /// Test 12: Cache expiry - verify TTL-based cache invalidation
        /// Expected: Cached decisions expire after TTL and trigger new API calls
        /// </summary>
        private static void TestCacheExpiry(Optimizely optimizelyClient)
        {
            Console.WriteLine("\n--- Test: Cache Expiry (Simulated) ---");

            var userContext = optimizelyClient.CreateUserContext("expiry_user", new UserAttributes
            {
                { "hello", true }
            });

            // First decision
            Console.WriteLine("Decision at T=0:");
            var decision1 = userContext.Decide(FLAG_KEY);
            PrintDecision("Initial Decision", decision1);

            // Simulate time passing (cache TTL is 5 seconds for testing)
            Console.WriteLine("\nSimulating cache expiry...");
            Sleep(6000);

            Console.WriteLine("Decision after simulated expiry:");
            var decision2 = userContext.Decide(FLAG_KEY);
            PrintDecision("After Expiry", decision2);

            Console.WriteLine("\nNote: Cache TTL configured to 5 seconds for testing");
            Console.WriteLine("Expected: New CMAB API call after expiry");
        }

        // ========================================
        // MAIN FUNCTION
        // ========================================

        static void Main(string[] args)
        {
            // Parse command line arguments
            var testCase = "all";
            foreach (var arg in args)
            {
                if (arg.StartsWith("--test="))
                {
                    testCase = arg.Substring(7);
                }
            }

            Console.WriteLine("=== CMAB Testing Suite for C# SDK ===");
            Console.WriteLine($"Testing CMAB with rc environment");
            Console.WriteLine($"SDK Key: {SDK_KEY}");
            Console.WriteLine($"Flag Key: {FLAG_KEY}\n");

            // Create Optimizely client
            Console.WriteLine("Initializing Optimizely SDK...");

            // Configure SDK with DEBUG logging to see all CMAB calls
            OptimizelyFactory.SetLogger(new ConsoleLogger(LogLevel.DEBUG));
            OptimizelyFactory.SetCmabConfig(new CmabConfig()
                .SetCacheSize(10000)
                .SetCacheTtl(TimeSpan.FromSeconds(5))); // For testing

            var optimizely = OptimizelyFactory.NewDefaultInstance(SDK_KEY);

            // Wait for SDK to be ready
            var maxWait = 10; // seconds
            var waited = 0;
            while (!optimizely.IsValid && waited < maxWait)
            {
                Console.WriteLine($"Waiting for datafile to load... ({waited}s)");
                Sleep(1000);
                waited++;
            }

            if (!optimizely.IsValid)
            {
                Console.WriteLine("✗ Failed to initialize SDK - datafile not loaded");
                return;
            }

            Console.WriteLine("✓ SDK initialized\n");

            // Run tests based on test case
            try
            {
                switch (testCase)
                {
                    case "basic":
                        TestBasicCMAB(optimizely);
                        break;
                    case "cache_hit":
                        TestCacheHit(optimizely);
                        break;
                    case "cache_miss":
                        TestCacheMissOnAttributeChange(optimizely);
                        break;
                    case "ignore_cache":
                        TestIgnoreCacheOption(optimizely);
                        break;
                    case "reset_cache":
                        TestResetCacheOption(optimizely);
                        break;
                    case "invalidate_user":
                        TestInvalidateUserCacheOption(optimizely);
                        break;
                    case "concurrent":
                        TestConcurrentRequests(optimizely);
                        break;
                    case "fallback":
                        TestFallbackWhenNotQualified(optimizely);
                        break;
                    case "traffic":
                        TestTrafficAllocation(optimizely);
                        break;
                    case "event_tracking":
                        TestEventTracking(optimizely);
                        break;
                    case "performance":
                        TestPerformanceBenchmarks(optimizely);
                        break;
                    case "cache_expiry":
                        TestCacheExpiry(optimizely);
                        break;
                    case "all":
                        Console.WriteLine("Running all tests...\n");
                        TestBasicCMAB(optimizely);
                        TestCacheHit(optimizely);
                        TestCacheMissOnAttributeChange(optimizely);
                        TestIgnoreCacheOption(optimizely);
                        TestResetCacheOption(optimizely);
                        TestInvalidateUserCacheOption(optimizely);
                        TestConcurrentRequests(optimizely);
                        TestFallbackWhenNotQualified(optimizely);
                        TestTrafficAllocation(optimizely);
                        TestEventTracking(optimizely);
                        TestPerformanceBenchmarks(optimizely);
                        TestCacheExpiry(optimizely);
                        break;
                    default:
                        Console.WriteLine($"Unknown test case: {testCase}");
                        Console.WriteLine("\nAvailable test cases:");
                        Console.WriteLine("  basic, cache_hit, cache_miss, ignore_cache, reset_cache,");
                        Console.WriteLine("  invalidate_user, concurrent, fallback, traffic,");
                        Console.WriteLine("  event_tracking, performance, cache_expiry, all");
                        Environment.Exit(1);
                        break;
                }

                Console.WriteLine("\n==========================");
                Console.WriteLine("Tests completed!");
                Console.WriteLine("==========================\n");

                // Clean up
                optimizely.Dispose();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error running tests: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                optimizely?.Dispose();
                Environment.Exit(1);
            }
        }
    }
}
