using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using OptimizelySDK;
using OptimizelySDK.Entity;
using OptimizelySDK.Event;
using OptimizelySDK.Notifications;

namespace QuickStart
{
    public static class QuickStart
    {
        public static void Main()
        {
            var optimizelyClient = OptimizelyFactory.NewDefaultInstance("TbrfRLeKvLyWGusqANoeR");
            if (!optimizelyClient.IsValid)
            {
                Console.WriteLine("Optimizely client invalid. Verify in Settings>Environments that you used the primary environment's SDK key");
                optimizelyClient.Dispose();
                return;
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
            // var keys = new[] {"flag1", "flag2"};
            // var decisions = user.DecideForKeys(keys);
            // Console.WriteLine(JsonConvert.SerializeObject(decisions));
            
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
            
            // NotificationCenter
            // var callbacks = new TestNotificationCallbacks();
            // // for Activate
            // optimizelyClient.NotificationCenter.AddNotification(
            //     NotificationCenter.NotificationType.Activate,
            //     TestNotificationCallbacks.ActivateCallback);
            // optimizelyClient.Activate("flag1", USER_ID);
            // // for Track
            // optimizelyClient.NotificationCenter.AddNotification(
            //     NotificationCenter.NotificationType.Track,
            //     TestNotificationCallbacks.TrackCallback);
            // optimizelyClient.Track("myevent", USER_ID);
            // // for Decision
            // optimizelyClient.NotificationCenter.AddNotification(
            //     NotificationCenter.NotificationType.Decision,
            //     TestNotificationCallbacks.DecisionCallback);
            // user.Decide("flag1");
            // // for ConfigUpdate
            // optimizelyClient.NotificationCenter.AddNotification(
            //     NotificationCenter.NotificationType.OptimizelyConfigUpdate,
            //     callbacks.ConfigUpdateCallback);
            // // for LogEvent
            // optimizelyClient.NotificationCenter.AddNotification(
            //     NotificationCenter.NotificationType.LogEvent,
            //     callbacks.LogEventCallback);
            
            optimizelyClient.Dispose();
        }
    }
    
    public class TestNotificationCallbacks
    {
        public static void ActivateCallback(Experiment experiment, string userId,
            UserAttributes userAttributes,
            Variation variation, LogEvent logEvent
        )
        {
            Console.WriteLine(">>> Activate Callback");
            Console.WriteLine(experiment.Key);
            Console.WriteLine(userId);
            Console.WriteLine(userAttributes);
            Console.WriteLine(variation.Key);
            Console.WriteLine(JsonConvert.SerializeObject(logEvent));
        }

        public static void TrackCallback(string eventKey, string userId,
            UserAttributes userAttributes,
            EventTags eventTags, LogEvent logEvent
        )
        {
            Console.WriteLine(">>> Track Callback");
            Console.WriteLine(eventKey);
            Console.WriteLine(userId);
            Console.WriteLine(userAttributes);
            Console.WriteLine(JsonConvert.SerializeObject(eventTags));
            Console.WriteLine(JsonConvert.SerializeObject(logEvent));
        }

        public static void DecisionCallback(string type, string userId,
            UserAttributes userAttributes,
            Dictionary<string, object> decisionInfo
        )
        {
            Console.WriteLine(">>> Decision Callback");
            Console.WriteLine(type);
            Console.WriteLine(userId);
            Console.WriteLine(userAttributes);
            Console.WriteLine(JsonConvert.SerializeObject(decisionInfo));
        }

        public void ConfigUpdateCallback()
        {
            Console.WriteLine(">>> Config Update Callback");
        }

        public void LogEventCallback(LogEvent logEvent)
        {
            Console.WriteLine(">>> Log Event Callback");
            Console.WriteLine(JsonConvert.SerializeObject(logEvent));
        }
    }
}
