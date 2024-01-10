using System;
using System.Linq;
using Newtonsoft.Json;
using OptimizelySDK;

namespace QuickStart
{
    public static class QuickStart
    {
        public static void Main()
        {
            var optimizelyClient = OptimizelyFactory.NewDefaultInstance("TbrfRLeKvLyWGusqANoeR");
            if (!optimizelyClient.IsValid)
            {
                Console.WriteLine(
                    "Optimizely client invalid. " +
                    "Verify in Settings>Environments that you used the primary environment's SDK key");
                optimizelyClient.Dispose();
            }
            
            const string USER_ID = "matjaz-user-2";

            var user = optimizelyClient.CreateUserContext(USER_ID);

            // Fetch
            // Console.WriteLine("Fetch:" + user.FetchQualifiedSegments());
            // var qualifiedSegments = user.GetQualifiedSegments();
            // Console.WriteLine(JsonConvert.SerializeObject(qualifiedSegments));
            // const string SEGMENT_ID = "atsbugbashsegmentdob";
            // Console.WriteLine($"Is Qualified for {SEGMENT_ID}: {user.IsQualifiedFor(SEGMENT_ID)}");
            
            // TrackEvent
            // user.TrackEvent("myevent");
            
            // Decide
            // var decision = user.Decide("flag1");
            // Console.WriteLine(JsonConvert.SerializeObject(decision));
            // var variables = decision.Variables.ToDictionary();
            // Console.WriteLine(JsonConvert.SerializeObject(variables));
            
            // DecideForKeys
            var keys = new[] {"flag1", "flag2"};
            var decisions = user.DecideForKeys(keys);
            Console.WriteLine(JsonConvert.SerializeObject(decisions));
            
            // DecideAll
            // var decisions = user.DecideAll();
            // Console.WriteLine(JsonConvert.SerializeObject(decisions));
            
            // Set & Get & Remove ForcedDecision
            // var context = new OptimizelyDecisionContext("flag1", "default-rollout-34902-22583870382");
            // var forcedDecision = new OptimizelyForcedDecision("off");
            // user.SetForcedDecision(context, forcedDecision);
            // var result = user.GetForcedDecision(context);
            // Console.WriteLine(JsonConvert.SerializeObject(result));
            // var wasRemoved = user.RemoveForcedDecision(context);
            // Console.WriteLine($"Was removed: {wasRemoved}");
            
            // Activate
            // var variation = optimizelyClient.Activate("flag1", USER_ID);
            // Console.WriteLine("Variation: " + JsonConvert.SerializeObject(variation));
            
            optimizelyClient.Dispose();
        }
    }
}
