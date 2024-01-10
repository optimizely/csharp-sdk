using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using OptimizelySDK;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Logger;
using OptimizelySDK.Notifications;
using OptimizelySDK.Odp;

namespace QuickStart
{
    public static class QuickStart
    {
        public static void Main()
        {
            
            var logger = new DefaultLogger();
            var errorHandler = new DefaultErrorHandler(logger, false);
            var eventDispatcher = new DefaultEventDispatcher(logger);
            var builder = new HttpProjectConfigManager.Builder();
            var notificationCenter = new NotificationCenter();
            // Wire ConfigUpdate and LogEvent early
            // for ConfigUpdate
            // notificationCenter.AddNotification(
            //     NotificationCenter.NotificationType.OptimizelyConfigUpdate,
            //     NotificationCallbacks.ConfigUpdateCallback);
            // // for LogEvent
            // notificationCenter.AddNotification(
            //     NotificationCenter.NotificationType.LogEvent,
            //     NotificationCallbacks.LogEventCallback);

            var configManager = builder.WithSdkKey("TbrfRLeKvLyWGusqANoeR").
                WithLogger(logger).
                WithPollingInterval(TimeSpan.FromSeconds(1)).
                WithBlockingTimeoutPeriod(TimeSpan.FromSeconds(1)).
                WithErrorHandler(errorHandler).
                WithNotificationCenter(notificationCenter).
                Build();
            var eventProcessor = new BatchEventProcessor.Builder().WithLogger(logger).
                WithMaxBatchSize(1).
                WithFlushInterval(TimeSpan.FromSeconds(1)).
                WithEventDispatcher(eventDispatcher).
                WithNotificationCenter(notificationCenter).
                Build();
            var odpManager = new OdpManager.Builder()
                .WithErrorHandler(errorHandler)
                .WithLogger(logger)
                .Build();
            var optimizelyClient = new Optimizely(configManager, notificationCenter, eventDispatcher, logger,
                errorHandler, null, eventProcessor, null, odpManager);
            
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
            // // for Activate
            // notificationCenter.AddNotification(
            //     NotificationCenter.NotificationType.Activate,
            //     NotificationCallbacks.ActivateCallback);
            // optimizelyClient.Activate("flag1", USER_ID);
            // // for Track
            // notificationCenter.AddNotification(
            //     NotificationCenter.NotificationType.Track,
            //     NotificationCallbacks.TrackCallback);
            // optimizelyClient.Track("myevent", USER_ID);
            // // for Decision
            // notificationCenter.AddNotification(
            //     NotificationCenter.NotificationType.Decision,
            //     NotificationCallbacks.DecisionCallback);
            // user.Decide("flag1");
            
            optimizelyClient.Dispose();
        }
    }
    
    public static class NotificationCallbacks
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

        public static void ConfigUpdateCallback()
        {
            Console.WriteLine(">>> Config Update Callback");
        }

        public static void LogEventCallback(LogEvent logEvent)
        {
            Console.WriteLine(">>> Log Event Callback");
            Console.WriteLine(JsonConvert.SerializeObject(logEvent));
        }
    }
}
