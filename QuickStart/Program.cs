using System;
using OptimizelySDK;

namespace QuickStart
{
    public static class QuickStart
    {
        public static void Main()
        {
            // This Optimizely initialization is synchronous. For other methods, see the C# SDK reference.
            var optimizelyClient = OptimizelyFactory.NewDefaultInstance("K4UmaV5Pk7cEh2hbcjgwe");
            if (!optimizelyClient.IsValid)
            {
                Console.WriteLine(
                    "Optimizely client invalid. " +
                    "Verify in Settings>Environments that you used the primary environment's SDK key");
                optimizelyClient.Dispose();
            }

            var hasOnFlags = false;

            /*
             * To get rapid demo results, generate random users.
             * Each user always sees the same variation unless you reconfigure the flag rule.
             */
            var rnd = new Random();
            for (var i = 0; i < 10; i++)
            {
                var userId = rnd.Next(1000, 9999).ToString();

                // Create a user context to bucket the user into a variation.
                var user = optimizelyClient.CreateUserContext(userId);

                // "product_sort" corresponds to a flag key in your Optimizely project
                var decision = user.Decide("product_sort");

                // Did decision fail with a critical error?
                if (string.IsNullOrEmpty(decision.VariationKey))
                {
                    Console.WriteLine(Environment.NewLine + Environment.NewLine +
                                      "Decision error: " + string.Join(" ", decision.Reasons));
                    continue;
                }

                /*
                 * Get a dynamic configuration variable.
                 * "sort_method" corresponds to a variable key in your Optimizely project.
                 */
                var sortMethod = decision.Variables.ToDictionary()["sort_method"];

                hasOnFlags = hasOnFlags || decision.Enabled;

                /*
                 * Mock what the user sees with print statements (in production, use flag variables to implement feature configuration)
                 */

                // always returns false until you enable a flag rule in your Optimizely project
                Console.WriteLine(Environment.NewLine +
                                  $"Flag {(decision.Enabled ? "on" : "off")}. " +
                                  $"User number {user.GetUserId()} saw " +
                                  $"flag variation {decision.VariationKey} and got " +
                                  $"products sorted by {sortMethod} config variable as part of " +
                                  $"flag rule {decision.RuleKey}");
            }

            if (!hasOnFlags)
            {
                var projectId = optimizelyClient.ProjectConfigManager.GetConfig().ProjectId;
                var projectSettingsUrl =
                    $"https://app.optimizely.com/v2/projects/{projectId}/settings/implementation";

                Console.WriteLine(Environment.NewLine + Environment.NewLine +
                                  "Flag was off for everyone. Some reasons could include:" +
                                  Environment.NewLine +
                                  "1. Your sample size of visitors was too small. Re-run, or increase the iterations in the FOR loop" +
                                  Environment.NewLine +
                                  "2. By default you have 2 keys for 2 project environments (dev/prod). Verify in Settings>Environments that you used the right key for the environment where your flag is toggled to ON." +
                                  Environment.NewLine + Environment.NewLine +
                                  $"Check your key at {projectSettingsUrl}");
            }

            optimizelyClient.Dispose();
        }
    }
}
