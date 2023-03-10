using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OptimizelySDK;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event;
using OptimizelySDK.Logger;
using OptimizelySDK.Notifications;
using OptimizelySDK.Odp;
using OptimizelySDK.OptimizelyDecisions;

namespace DemoConsoleApp
{
    class Program
    {
        private static void FetchAndDecide(OptimizelyUserContext user)
        {
            //=========================================
            // Fetch Qualified Segments + decide
            // =========================================

            user.FetchQualifiedSegments();     // to test segment options add one or both as argument: ['RESET_CACHE', 'IGNORE_CACHE']

            var options = new OptimizelyDecideOption[] { OptimizelyDecideOption.INCLUDE_REASONS };

            OptimizelyDecision decision = user.Decide("flag1", options);
            string decisionString = JsonConvert.SerializeObject(decision);
            Console.WriteLine(" >>> DECISION " + decisionString);

            var segments = user.GetQualifiedSegments();
            if (segments != null)
            {
                Console.WriteLine("  >>> SEGMENTS: [{0}]", string.Join(", ", segments));

                foreach (string segment in segments)
                {
                    bool isQualified = user.IsQualifiedFor(segment);
                    Console.WriteLine("  >>> IS QUALIFIED for " + segment + " : " + isQualified);
                }
            }
            else
            {
                Console.WriteLine("  >>> SEGMENTS: null");
            }
        }

        private static void SendEvent(Optimizely optimizely)
        {

            var identifiers = new Dictionary<string, string>();
            identifiers.Add("fs-user-id", "fs-id-12");
            identifiers.Add("'email'", "fs-bash-x@optimizely.com");

            // valid case
            optimizely.SendOdpEvent("any", "any", identifiers, null);

            //    // test missing/ empty identifiers should not be allowed
            //        optimizely.SendOdpEvent("any", "any", null, null);
            // optimizely.SendOdpEvent("any", "any", new Dictionary<string, string>(), null);
            //
            //    // test missing/ empty action should not be allowed
            //        optimizely.SendOdpEvent("", "any", identifiers, null);
            //        optimizely.SendOdpEvent(null, "any", identifiers, null);
        }

        static void Main(string[] args)
        {
            /** ============================================
     # CONFIG TYPE 1:
     # default config, minimal settings
     ============================================
     """
     TEST THE FOLLOWING:
     - test creating user context with regular attributes
     - test creating user context with prebuilt segments (no odp list), should get segments back, experiment should evaluate to true, decide response should look correct
     - truth table - TBD
     - test creating user context  with prebuilt segment list, should get back a list of segments, experiment should evaluate to true, decide response should look correct
     - may not need truth table test here (check)
     - add RESET_CACHE and/or IGNORE_CACHE to fetch qualified segments call and repeat
     - test send event
     - verify events on ODP event inspector
     - in send_event function uncomment lines of code that test missing identifiers and action keys, verify appropriate error is produced
     - test audience segments (see spreadsheet
     - test implicit/explicit ODP events?
     - test integrations (no ODP integration added to project, integration is on, is off)
     """
     **/
            var optimizely = OptimizelyFactory.NewDefaultInstance("TbrfRLeKvLyWGusqANoeR");

            var optimizelyConfig = optimizely.GetOptimizelyConfig();

            var attributes = new UserAttributes();
            attributes.Add("laptop_os", "mac");

            var user = optimizely.CreateUserContext("fs-id-6", attributes);

            FetchAndDecide(user);
            SendEvent(optimizely);

            optimizely.Dispose();

            /** ============================================
         # CONFIG TYPE 2:
         # with ODP integration changed at app.optimizely.com - changed public key or host url
         # VALID API key/HOST url- should work, INVALID KEY/URL-should get errors
         # ============================================
         """
         TEST THE FOLLOWING:
         - test the same as in "CONFIG TYPE 1" but use invalid API key or HOST url
         - TODO clarify with Jae what to test here !!!
         """ **/
            /*optimizely = OptimizelyFactory.NewDefaultInstance("TbrfRLeKvLyWGusqANoeR");

            attributes = new UserAttributes();
            attributes.Add("laptop_os", "mac");

            // CASE 1 - REGULAR ATTRIBUTES, NO ODP
            user = optimizely.CreateUserContext("user123", attributes);

            // CASE 2 - PREBUILT SEGMENTS, NO LIST SEGMENTS, valid user id is fs-id-12 (matehces DOB)
            // user = optimizely.CreateUserContext("fs-id-12", attributes);

            // CASE 3 - SEGMENT LIST/ARRAY, valid user id is fs-id-6
            // user = optimizely.CreateUserContext("fs-id-6", attributes);

            FetchAndDecide(user);
            SendEvent(optimizely);

            optimizely.Dispose();*/

            /** ============================================
 # CONFIG TYPE 3:
 # with different ODP configuration options (odpdisabled, segments_cache_size etc)
 # ============================================
 """
 TEST THE FOLLOWING:
 same as in "CONFIG TYPE 1", but add config options to fetch qualified segments function, for example:
 odp_disabled
 segments_cache_size
 segments_cache_timeout_in_secs
 odp_segments_cache
 odp_segment_manager
 odp_event_manager
 odp_segment_request_timeout
 odp_event_request_timeout
 odp_flush_interval

 Observe responses and verity the correct behavior.
 """
 **/
            var logger = new DefaultLogger();
            var errorHandler = new DefaultErrorHandler(logger, false);
            var notificationCenter = new NotificationCenter();
            var builder = new HttpProjectConfigManager.Builder();
            var configManager = builder.WithSdkKey("TbrfRLeKvLyWGusqANoeR").
                WithLogger(logger).
                WithErrorHandler(errorHandler).
                WithNotificationCenter(notificationCenter).
                Build(false);
            var handler = HttpProjectConfigManager.HttpClient.GetHttpClientHandler();
            handler.UseProxy = false;

            var fetchSegmentHttpClient = new HttpClient(handler);
            fetchSegmentHttpClient.Timeout = TimeSpan.FromMilliseconds(10000);
            var sendEventHttpClient = new HttpClient(handler);
            sendEventHttpClient.Timeout = TimeSpan.FromMilliseconds(10000);

            var odpEventApiManager = new OdpEventApiManager(logger, errorHandler, sendEventHttpClient);
            var odpSegmentApiManager = new OdpSegmentApiManager(logger, errorHandler, fetchSegmentHttpClient);

            var eventManagerBuilder = new OdpEventManager.Builder()
                .WithOdpEventApiManager(odpEventApiManager)
                .WithFlushInterval(TimeSpan.FromMilliseconds(1));
            
            OdpSegmentManager odpSegmentManager = new OdpSegmentManager(odpSegmentApiManager, cacheSize: 1, itemTimeout: TimeSpan.FromSeconds(10), logger: logger);

            bool disableODP = false; // Set this to true to disable odp and false to enable odp, There is no other way to disable odp, like passing any variable.
            OdpManager odpManager = null;
            if (!disableODP)
            {
                odpManager = new OdpManager.Builder()
                .WithEventManager(eventManagerBuilder.Build())
                .WithSegmentManager(odpSegmentManager)
                .WithErrorHandler(errorHandler)
                .WithLogger(logger)
                .Build();
            }
            optimizely = new Optimizely(configManager, notificationCenter, null, logger, errorHandler, null, null, null, odpManager);

            attributes = new UserAttributes();
            attributes.Add("laptop_os", "mac");

            // CASE 1 - REGULAR ATTRIBUTES, NO ODP
            user = optimizely.CreateUserContext("user123", attributes);

            // CASE 2 - PREBUILT SEGMENTS, NO LIST SEGMENTS, valid user id is fs-id-12 (matches DOB)
            // user = optimizely.CreateUserContext("fs-id-12", attributes);

            // CASE 3 - SEGMENT LIST/ARRAY, valid user id is fs-id-6
            // user = optimizely.CreateUserContext("fs-id-6", attributes);

            FetchAndDecide(user);
            SendEvent(optimizely);

            optimizely.Dispose();

            Console.ReadLine();
        }


    }
}
