/* 
 * Copyright 2019, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
#if !NETSTANDARD1_6 && !NET35
using System.Configuration;
#endif

using OptimizelySDK.Bucketing;
using OptimizelySDK.Config;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Logger;
using OptimizelySDK.Notifications;

namespace OptimizelySDK
{
    /// <summary>
    /// Optimizely factory to provides basic utility to instantiate the Optimizely SDK with a minimal number of configuration options.
    /// </summary>
    public static class OptimizelyFactory
    {
        private static int MaxEventBatchSize;
        private static TimeSpan MaxEventFlushInterval;
        private static TimeSpan PollingInterval;
        private static TimeSpan BlockingTimeoutPeriod;
        private static ILogger OptimizelyLogger;
        private const string ConfigSectionName = "optlySDKConfigSection";

#if !NETSTANDARD1_6 && !NET35
        public static void SetPollingInterval(TimeSpan pollingInterval)
        {
            PollingInterval = pollingInterval;
        }

        public static void SetBlockingTimeoutPeriod(TimeSpan blockingTimeoutPeriod)
        {
            BlockingTimeoutPeriod = blockingTimeoutPeriod;
        }

        public static void SetBatchSize(int batchSize)
        {
            MaxEventBatchSize = batchSize;
        }

        public static void SetFlushInterval(TimeSpan flushInterval)
        {
            MaxEventFlushInterval = flushInterval;
        }

        public static void SetLogger(ILogger logger)
        {
            OptimizelyLogger = logger;
        }

        public static Optimizely NewDefaultInstance()
        {
            var logger = OptimizelyLogger ?? new DefaultLogger();
            OptimizelySDKConfigSection OptlySDKConfigSection = null;
            try
            {
                OptlySDKConfigSection = ConfigurationManager.GetSection(ConfigSectionName) as OptimizelySDKConfigSection;
            }
            catch (ConfigurationErrorsException ex)
            {
                logger.Log(LogLevel.ERROR, "Invalid App.Config. Unable to initialize optimizely instance" + ex.Message);
                return null;    
            }            

            HttpProjectConfigElement httpProjectConfigElement = OptlySDKConfigSection.HttpProjectConfig;

            if (httpProjectConfigElement == null) return null;

            var errorHandler = new DefaultErrorHandler();
            var eventDispatcher = new DefaultEventDispatcher(logger);
            var builder = new HttpProjectConfigManager.Builder();
            var notificationCenter = new NotificationCenter();
            
            var configManager = builder
                .WithSdkKey(httpProjectConfigElement.SDKKey)
                .WithUrl(httpProjectConfigElement.Url)
                .WithFormat(httpProjectConfigElement.Format)
                .WithPollingInterval(TimeSpan.FromMilliseconds(httpProjectConfigElement.PollingInterval))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(httpProjectConfigElement.BlockingTimeOutPeriod))
                .WithLogger(logger)
                .WithErrorHandler(errorHandler)
                .WithNotificationCenter(notificationCenter)
                .Build(true);

            EventProcessor eventProcessor = null;

            var batchEventProcessorElement = OptlySDKConfigSection.BatchEventProcessor;

            if (batchEventProcessorElement == null) return null;

            eventProcessor = new BatchEventProcessor.Builder()
                .WithMaxBatchSize(batchEventProcessorElement.BatchSize)
                .WithFlushInterval(TimeSpan.FromMilliseconds(batchEventProcessorElement.FlushInterval))
                .WithTimeoutInterval(TimeSpan.FromMilliseconds(batchEventProcessorElement.TimeoutInterval))
                .WithLogger(logger)
                .WithEventDispatcher(eventDispatcher)
                .WithNotificationCenter(notificationCenter)
                .Build();

            return NewDefaultInstance(configManager, notificationCenter, eventDispatcher, errorHandler, logger, eventProcessor: eventProcessor);

        }
#endif

        public static Optimizely NewDefaultInstance(string sdkKey)
        {
            return NewDefaultInstance(sdkKey, null);
        }

        public static Optimizely NewDefaultInstance(string sdkKey, string fallback)
        {
            var logger = OptimizelyLogger ?? new DefaultLogger();
            var errorHandler = new DefaultErrorHandler();
            var eventDispatcher = new DefaultEventDispatcher(logger);
            var builder = new HttpProjectConfigManager.Builder();
            var notificationCenter = new NotificationCenter();

            var configManager = builder
                .WithSdkKey(sdkKey)
                .WithDatafile(fallback)
                .WithPollingInterval(PollingInterval)
                .WithBlockingTimeoutPeriod(BlockingTimeoutPeriod)
                .WithLogger(logger)
                .WithErrorHandler(errorHandler)
                .WithNotificationCenter(notificationCenter)
                .Build(true);

            EventProcessor eventProcessor = null;

#if !NETSTANDARD1_6 && !NET35
            eventProcessor = new BatchEventProcessor.Builder()
                .WithLogger(logger)
                .WithMaxBatchSize(MaxEventBatchSize)
                .WithFlushInterval(MaxEventFlushInterval)
                .WithEventDispatcher(eventDispatcher)
                .WithNotificationCenter(notificationCenter)
                .Build();
#endif

            return NewDefaultInstance(configManager, notificationCenter, eventDispatcher, errorHandler, logger, eventProcessor: eventProcessor);
        }

        public static Optimizely NewDefaultInstance(ProjectConfigManager configManager, NotificationCenter notificationCenter = null, IEventDispatcher eventDispatcher = null,
                                                    IErrorHandler errorHandler = null, ILogger logger = null, UserProfileService userprofileService = null, EventProcessor eventProcessor = null)
        {
            return new Optimizely(configManager, notificationCenter, eventDispatcher, logger, errorHandler, userprofileService, eventProcessor);      
        }
    }
}
