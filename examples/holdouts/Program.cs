/**
 * Holdout Testing Example for Optimizely C# SDK
 *
 * This file contains comprehensive test scenarios for Holdout functionality
 * Based on: JavaScript SDK holdout test setup
 *
 * To run:
 *   dotnet build
 *   dotnet run (runs all tests)
 *   TEST_KEY=hits_ho3_1 dotnet run (runs specific test)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OptimizelySDK;
using OptimizelySDK.Entity;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Logger;
using OptimizelySDK.Notifications;
using OptimizelySDK.OptimizelyDecisions;

namespace HoldoutTests
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

    /// <summary>
    /// Custom event dispatcher to capture and validate event data
    /// </summary>
    public class TestEventDispatcher : IEventDispatcher
    {
        public ILogger Logger { get; set; } = new ConsoleLogger(LogLevel.INFO);
        
        public Action<LogEvent>? OnEventDispatched { get; set; }

        public void DispatchEvent(LogEvent logEvent)
        {
            OnEventDispatched?.Invoke(logEvent);
        }
    }

    /// <summary>
    /// Test case definition
    /// </summary>
    public class TestCase
    {
        public required string Key { get; set; }
        public required string Title { get; set; }
        public required string Flag { get; set; }
        public required string UserId { get; set; }
        public Dictionary<string, object> Attribute { get; set; } = new();
        public required string RuleKey { get; set; }
        public required string RuleType { get; set; }
        public required string ExperimentId { get; set; }
        public required string VariationKey { get; set; }
        public required string VariationId { get; set; }
        public required bool Enabled { get; set; }
        public required bool Event { get; set; }
    }

    /// <summary>
    /// Resolvable promise-like wrapper for async coordination
    /// </summary>
    public class ResolvableTask<T>
    {
        private readonly TaskCompletionSource<T> _tcs = new();
        
        public Task<T> Task => _tcs.Task;
        
        public void Resolve(T value) => _tcs.TrySetResult(value);
        
        public void Reject(Exception ex) => _tcs.TrySetException(ex);
    }

    class Program
    {
        private static readonly ConsoleLogger _logger = new(LogLevel.DEBUG);

        /// <summary>
        /// Compare expected vs actual values and print result
        /// </summary>
        private static void LogResult(TestCase tc, object actual, string key, string prefix = "   ")
        {
            var expected = key switch
            {
                "userId" => tc.UserId,
                "experimentId" => tc.ExperimentId,
                "variationId" => tc.VariationId,
                "variationKey" => tc.VariationKey,
                "ruleKey" => tc.RuleKey,
                "ruleType" => tc.RuleType,
                "enabled" => tc.Enabled.ToString().ToLower(),
                _ => "unknown"
            };

            var actualStr = actual?.ToString()?.ToLower() ?? "null";
            var expectedStr = expected?.ToLower() ?? "null";
            var pass = actualStr == expectedStr ? "✅" : "❌";
            
            Console.WriteLine($"{prefix}{key}- want: {expected}, got: {actual}, passed: {pass}");
        }

        /// <summary>
        /// Run a single test case
        /// </summary>
        private static async Task RunTest(TestCase tc)
        {
            try
            {
                Console.WriteLine($" =============  start test: {tc.Title} =================");

                var eventTask = new ResolvableTask<bool>();
                var notificationTask = new ResolvableTask<bool>();
                var eventTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                // Create custom event dispatcher
                var eventDispatcher = new TestEventDispatcher();

                if (!tc.Event)
                {
                    // No event expected, resolve immediately
                    eventTask.Resolve(true);
                }
                else
                {
                    eventDispatcher.OnEventDispatched = logEvent =>
                    {
                        try
                        {
                            // Parse the event to extract decision data
                            var paramsDict = logEvent.Params;
                            
                            if (paramsDict.TryGetValue("visitors", out var visitorsObj) && 
                                visitorsObj is IList<object> visitors && 
                                visitors.Count > 0)
                            {
                                var visitor = visitors[0] as Dictionary<string, object>;
                                var visitorId = visitor?["visitor_id"]?.ToString();
                                
                                if (visitor?.TryGetValue("snapshots", out var snapshotsObj) == true &&
                                    snapshotsObj is IList<object> snapshots &&
                                    snapshots.Count > 0)
                                {
                                    var snapshot = snapshots[0] as Dictionary<string, object>;
                                    
                                    if (snapshot?.TryGetValue("decisions", out var decisionsObj) == true &&
                                        decisionsObj is IList<object> decisions &&
                                        decisions.Count > 0)
                                    {
                                        var decision = decisions[0] as Dictionary<string, object>;
                                        var experimentId = decision?["experiment_id"]?.ToString();
                                        var variationId = decision?["variation_id"]?.ToString();
                                        
                                        var metadata = decision?["metadata"] as Dictionary<string, object>;
                                        var variationKey = metadata?["variation_key"]?.ToString();
                                        var ruleKey = metadata?["rule_key"]?.ToString();
                                        var ruleType = metadata?["rule_type"]?.ToString();

                                        Console.WriteLine("\n📊 Event Result:");
                                        LogResult(tc, visitorId ?? "", "userId");
                                        LogResult(tc, experimentId ?? "", "experimentId");
                                        LogResult(tc, variationId ?? "", "variationId");
                                        LogResult(tc, variationKey ?? "", "variationKey");
                                        LogResult(tc, ruleKey ?? "", "ruleKey");
                                        LogResult(tc, ruleType ?? "", "ruleType");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing event: {ex.Message}");
                        }
                        
                        eventTask.Resolve(true);
                    };

                    // Set timeout for event
                    eventTimeout.Token.Register(() =>
                    {
                        eventTask.Reject(new TimeoutException("Event timeout"));
                    });
                }

                // Create Optimizely client with static datafile
                var optimizely = new Optimizely(
                    Datafile.Json,
                    eventDispatcher,
                    _logger
                );

                Console.WriteLine("✅ Optimizely client created successfully!");

                if (!optimizely.IsValid)
                {
                    Console.WriteLine("❌ SDK is not valid!");
                    return;
                }

                Console.WriteLine("✅ SDK is ready!");

                // Add decision notification listener
                optimizely.NotificationCenter.AddNotification(
                    NotificationCenter.NotificationType.Decision,
                    (string type, string userId, UserAttributes userAttributes, Dictionary<string, object> decisionInfo) =>
                    {
                        Console.WriteLine("\n📊 Notification Result:");
                        
                        var enabled = decisionInfo.TryGetValue("enabled", out var enabledVal) ? enabledVal : null;
                        var variationKey = decisionInfo.TryGetValue("variationKey", out var varKeyVal) ? varKeyVal?.ToString() : null;
                        var ruleKey = decisionInfo.TryGetValue("ruleKey", out var ruleKeyVal) ? ruleKeyVal?.ToString() : null;
                        
                        LogResult(tc, enabled ?? false, "enabled");
                        LogResult(tc, variationKey ?? "", "variationKey");
                        LogResult(tc, ruleKey ?? "", "ruleKey");
                        
                        notificationTask.Resolve(true);
                    }
                );

                // Create user context
                var userAttributes = new UserAttributes();
                foreach (var kvp in tc.Attribute)
                {
                    userAttributes[kvp.Key] = kvp.Value;
                }
                
                var userContext = optimizely.CreateUserContext(tc.UserId, userAttributes);

                Console.WriteLine($"📱 Created user context for user: {tc.UserId}");
                Console.WriteLine($"\n🎯 Making decision for {tc.Flag}...");

                // Make decision
                var decision = userContext!.Decide(tc.Flag);

                Console.WriteLine("\n📊 Decision Result:");
                Console.WriteLine($"   Flag Key- {decision.FlagKey}");
                LogResult(tc, decision.Enabled, "enabled");
                LogResult(tc, decision.VariationKey ?? "", "variationKey");
                LogResult(tc, decision.RuleKey ?? "", "ruleKey");

                // Wait for event and notification with timeout
                try
                {
                    await Task.WhenAll(
                        Task.WhenAny(eventTask.Task, Task.Delay(5000)),
                        Task.WhenAny(notificationTask.Task, Task.Delay(5000))
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: {ex.Message}");
                }

                // Clean up
                optimizely.Dispose();
                Console.WriteLine($" =============  end test: {tc.Title} =================\n\n");
            }
            catch (Exception error)
            {
                Console.WriteLine($"❌ Error: {error.Message}");
                Console.WriteLine(error.StackTrace);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// All test cases
        /// </summary>
        private static readonly List<TestCase> Tests = new()
        {
            new TestCase
            {
                Key = "miss_all_audience_1",
                Title = "flag_1, misses audience condition of all holdouts",
                Flag = "flag_1",
                VariationKey = "var_1",
                VariationId = "1559778",
                RuleKey = "default-rollout-490876-741763388721595",
                ExperimentId = "default-rollout-490876-741763388721595",
                RuleType = "rollout",
                UserId = "user-2",
                Attribute = new Dictionary<string, object>(),
                Enabled = true,
                Event = false
            },
            new TestCase
            {
                Key = "miss_all_audience_2",
                Title = "flag_2, misses audience condition of all holdouts",
                Flag = "flag_2",
                VariationKey = "var_2",
                VariationId = "1559781",
                RuleKey = "default-rollout-490881-741763388721595",
                ExperimentId = "default-rollout-490881-741763388721595",
                RuleType = "rollout",
                UserId = "user-8",
                Attribute = new Dictionary<string, object>(),
                Enabled = true,
                Event = false
            },
            new TestCase
            {
                Key = "hits_ho3_1",
                Title = "flag_1, hits ho3 (both audience and bucket)",
                Flag = "flag_1",
                VariationKey = "off",
                VariationId = "$opt_dummy_variation_id",
                RuleKey = "holdout_3",
                ExperimentId = "1656259",
                RuleType = "holdout",
                UserId = "user-19",
                Attribute = new Dictionary<string, object> { { "ho", 3 } },
                Enabled = false,
                Event = true
            },
            new TestCase
            {
                Key = "hits_ho3_2",
                Title = "flag_2, hits ho3 (both audience and bucket)",
                Flag = "flag_2",
                VariationKey = "off",
                VariationId = "$opt_dummy_variation_id",
                RuleKey = "holdout_3",
                ExperimentId = "1656259",
                RuleType = "holdout",
                UserId = "user-19",
                Attribute = new Dictionary<string, object> { { "ho", 3 } },
                Enabled = false,
                Event = true
            },
            new TestCase
            {
                Key = "hits_ho3_aud_miss_bucket_1",
                Title = "hits the ho_3 holdout audience but misses bucket",
                Flag = "flag_1",
                VariationKey = "var_1",
                VariationId = "1559778",
                RuleKey = "default-rollout-490876-741763388721595",
                ExperimentId = "default-rollout-490876-741763388721595",
                RuleType = "rollout",
                UserId = "user-2",
                Attribute = new Dictionary<string, object> { { "ho", 3 } },
                Enabled = true,
                Event = false
            },
            new TestCase
            {
                Key = "hits_ho3_aud_miss_bucket_2",
                Title = "hits the ho_3 holdout audience but misses bucket",
                Flag = "flag_2",
                VariationKey = "var_2",
                VariationId = "1559781",
                RuleKey = "default-rollout-490881-741763388721595",
                ExperimentId = "default-rollout-490881-741763388721595",
                RuleType = "rollout",
                UserId = "user-8",
                Attribute = new Dictionary<string, object> { { "ho", 3 } },
                Enabled = true,
                Event = false
            },
            new TestCase
            {
                Key = "hits_all_aud_hit_bucket_ho3_ho4_ho5_1",
                Title = "flag_1, hits all audiences, hits bucket of ho3, ho4 and ho5, should select first in order (ho3)",
                Flag = "flag_1",
                VariationKey = "off",
                VariationId = "$opt_dummy_variation_id",
                RuleKey = "holdout_3",
                ExperimentId = "1656259",
                RuleType = "holdout",
                UserId = "user-19",
                Attribute = new Dictionary<string, object> { { "all", 1 } },
                Enabled = false,
                Event = true
            },
            new TestCase
            {
                Key = "hits_all_aud_hit_bucket_ho3_ho4_ho5_2",
                Title = "flag_2, hits all audiences, hits bucket of ho3, ho4 and ho5, should select first in order (ho3)",
                Flag = "flag_2",
                VariationKey = "off",
                VariationId = "$opt_dummy_variation_id",
                RuleKey = "holdout_3",
                ExperimentId = "1656259",
                RuleType = "holdout",
                UserId = "user-19",
                Attribute = new Dictionary<string, object> { { "all", 1 } },
                Enabled = false,
                Event = true
            },
            new TestCase
            {
                Key = "hits_all_aud_hit_bucket_ho4_ho6_1",
                Title = "flag_1, hits all audiences, hits bucket of ho4 and ho6, should select first in order (ho4)",
                Flag = "flag_1",
                VariationKey = "off",
                VariationId = "$opt_dummy_variation_id",
                RuleKey = "holdout_4",
                ExperimentId = "1656260",
                RuleType = "holdout",
                UserId = "user-7",
                Attribute = new Dictionary<string, object> { { "all", 1 } },
                Enabled = false,
                Event = true
            },
            new TestCase
            {
                Key = "hits_all_aud_hit_bucket_ho4_ho6_2",
                Title = "flag_2, hits all audiences, hits bucket of ho4 and ho6, should select first in order (ho4)",
                Flag = "flag_2",
                VariationKey = "off",
                VariationId = "$opt_dummy_variation_id",
                RuleKey = "holdout_4",
                ExperimentId = "1656260",
                RuleType = "holdout",
                UserId = "user-7",
                Attribute = new Dictionary<string, object> { { "all", 1 } },
                Enabled = false,
                Event = true
            },
            new TestCase
            {
                Key = "hits_all_aud_hit_bucket_ho5_1",
                Title = "flag_1, hits all audiences, hits bucket of only ho5",
                Flag = "flag_1",
                VariationKey = "off",
                VariationId = "$opt_dummy_variation_id",
                RuleKey = "holdout_5",
                ExperimentId = "1656266",
                RuleType = "holdout",
                UserId = "user-11",
                Attribute = new Dictionary<string, object> { { "all", 1 } },
                Enabled = false,
                Event = true
            },
            new TestCase
            {
                Key = "hits_all_aud_hit_bucket_ho5_2",
                Title = "flag_2, hits all audiences, hits bucket of only ho5",
                Flag = "flag_2",
                VariationKey = "off",
                VariationId = "$opt_dummy_variation_id",
                RuleKey = "holdout_5",
                ExperimentId = "1656266",
                RuleType = "holdout",
                UserId = "user-11",
                Attribute = new Dictionary<string, object> { { "all", 1 } },
                Enabled = false,
                Event = true
            },
            new TestCase
            {
                Key = "hits_all_aud_hit_bucket_ho4_1",
                Title = "flag_1, hits all audiences, hits bucket of only ho4",
                Flag = "flag_1",
                VariationKey = "off",
                VariationId = "$opt_dummy_variation_id",
                RuleKey = "holdout_4",
                ExperimentId = "1656260",
                RuleType = "holdout",
                UserId = "user-1",
                Attribute = new Dictionary<string, object> { { "all", 1 } },
                Enabled = false,
                Event = true
            },
            new TestCase
            {
                Key = "hits_all_aud_hit_bucket_ho4_2",
                Title = "flag_2, hits all audiences, hits bucket of only ho4",
                Flag = "flag_2",
                VariationKey = "off",
                VariationId = "$opt_dummy_variation_id",
                RuleKey = "holdout_4",
                ExperimentId = "1656260",
                RuleType = "holdout",
                UserId = "user-1",
                Attribute = new Dictionary<string, object> { { "all", 1 } },
                Enabled = false,
                Event = true
            },
            new TestCase
            {
                Key = "hits_bucket_ho4_ho5_aud_only_ho5_1",
                Title = "flag_1, hits bucket of ho4 and ho5, but only audience of ho5",
                Flag = "flag_1",
                VariationKey = "off",
                VariationId = "$opt_dummy_variation_id",
                RuleKey = "holdout_5",
                ExperimentId = "1656266",
                RuleType = "holdout",
                UserId = "user-6",
                Attribute = new Dictionary<string, object> { { "ho", 5 } },
                Enabled = false,
                Event = true
            },
            new TestCase
            {
                Key = "hits_bucket_ho4_ho5_aud_only_ho5_2",
                Title = "flag_2, hits bucket of ho4 and ho5, but only audience of ho5",
                Flag = "flag_2",
                VariationKey = "off",
                VariationId = "$opt_dummy_variation_id",
                RuleKey = "holdout_5",
                ExperimentId = "1656266",
                RuleType = "holdout",
                UserId = "user-6",
                Attribute = new Dictionary<string, object> { { "ho", 5 } },
                Enabled = false,
                Event = true
            },
            new TestCase
            {
                Key = "hits_all_aud_miss_all_bucket_1",
                Title = "flag_1, hits all audiences but misses all buckets",
                Flag = "flag_1",
                VariationKey = "var_1",
                VariationId = "1559778",
                RuleKey = "default-rollout-490876-741763388721595",
                ExperimentId = "default-rollout-490876-741763388721595",
                RuleType = "rollout",
                UserId = "user-8",
                Attribute = new Dictionary<string, object> { { "all", 1 } },
                Enabled = true,
                Event = false
            },
            new TestCase
            {
                Key = "hits_all_aud_miss_all_bucket_2",
                Title = "flag_2, hits all audiences but misses all buckets",
                Flag = "flag_2",
                VariationKey = "var_2",
                VariationId = "1559781",
                RuleKey = "default-rollout-490881-741763388721595",
                ExperimentId = "default-rollout-490881-741763388721595",
                RuleType = "rollout",
                UserId = "user-10",
                Attribute = new Dictionary<string, object> { { "all", 1 } },
                Enabled = true,
                Event = false
            }
        };

        static async Task Main(string[] args)
        {
            Console.WriteLine($"total tests: {Tests.Count}\n");

            var testKey = Environment.GetEnvironmentVariable("TEST_KEY");

            if (!string.IsNullOrEmpty(testKey))
            {
                var test = Tests.FirstOrDefault(t => t.Key == testKey);
                if (test != null)
                {
                    await RunTest(test);
                }
                else
                {
                    Console.WriteLine($"Test with key '{testKey}' not found.");
                    Console.WriteLine("\nAvailable test keys:");
                    foreach (var t in Tests)
                    {
                        Console.WriteLine($"  {t.Key}");
                    }
                    Environment.Exit(1);
                }
            }
            else
            {
                foreach (var test in Tests)
                {
                    await RunTest(test);
                }
            }

            Console.WriteLine("\n==========================");
            Console.WriteLine("All tests completed!");
            Console.WriteLine("==========================\n");
        }
    }
}
