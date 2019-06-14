using System;
using OptimizelySDK.Bucketing;
using OptimizelySDK.Config;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Logger;

namespace OptimizelySDK
{
    /// after component emitting notification.
    /// 
    /// <summary>
    /// Optimizely factory.
    /// </summary>
    /// TODO: Add documentation of this class
    /// TODO: Add unit test of this class.
    public static class OptimizelyFactory
    {
        public static Optimizely NewDefaultInstance(string sdkKey)
        {
            return NewDefaultInstance(sdkKey, null);
        }

        public static Optimizely NewDefaultInstance(string sdkKey, string fallback)
        {
            var logger = new DefaultLogger();
            var errorHandler = new DefaultErrorHandler();
            var eventDispatcher = new DefaultEventDispatcher(logger);
            var builder = new HttpProjectConfigManager.Builder();
            // don't block
            var configManager = builder
                .WithSdkKey(sdkKey)
                .WithDatafile(fallback)
                .WithLogger(logger)
                .WithErrorHandler(errorHandler)
                .Build(true);

            return NewDefaultInstance(configManager, eventDispatcher, errorHandler, logger);
        }

        public static Optimizely NewDefaultInstance(ProjectConfigManager configManager, IEventDispatcher eventDispatcher = null,
                                                    IErrorHandler errorHandler = null, ILogger logger = null, UserProfileService userprofileService = null)
        {
            return new Optimizely(configManager, eventDispatcher, logger, errorHandler, userprofileService);      
        }
    }
}
