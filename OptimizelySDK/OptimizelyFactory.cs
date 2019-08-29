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
        private static ILogger OptimizelyLogger;

#if !NETSTANDARD1_6 && !NET35
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
#endif
        public static Optimizely NewDefaultInstance(string sdkKey)
        {
            return NewDefaultInstance(sdkKey, null);
        }

        public static Optimizely NewDefaultInstance(string sdkKey, string fallback)
        {
            var logger = OptimizelyLogger ?? new NoOpLogger();
            var errorHandler = new DefaultErrorHandler();
            var eventDispatcher = new DefaultEventDispatcher(logger);
            var builder = new HttpProjectConfigManager.Builder();
            var notificationCenter = new NotificationCenter();

            var configManager = builder
                .WithSdkKey(sdkKey)
                .WithDatafile(fallback)
                .WithLogger(logger)
                .WithErrorHandler(errorHandler)
                .WithNotificationCenter(notificationCenter)
                .Build(true);

            return NewDefaultInstance(configManager, notificationCenter, eventDispatcher, errorHandler, logger);
        }

        public static Optimizely NewDefaultInstance(ProjectConfigManager configManager, NotificationCenter notificationCenter = null, IEventDispatcher eventDispatcher = null,
                                                    IErrorHandler errorHandler = null, ILogger logger = null, UserProfileService userprofileService = null)
        {
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
            return new Optimizely(configManager, notificationCenter, eventDispatcher, logger, errorHandler, userprofileService, eventProcessor);      
        }
    }
}
