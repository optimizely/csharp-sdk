/* 
 * Copyright 2019, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
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
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Event.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.Notifications;

namespace OptimizelySDK.Event
{
    public class ForwardingEventProcessor : EventProcessor
    {
        private ILogger Logger;
        private IErrorHandler ErrorHandler;
        private IEventDispatcher EventDispatcher;
        private NotificationCenter NotificationCenter;

        public ForwardingEventProcessor(IEventDispatcher eventDispatcher,
            NotificationCenter notificationCenter, ILogger logger = null,
            IErrorHandler errorHandler = null
        )
        {
            EventDispatcher = eventDispatcher;
            NotificationCenter = notificationCenter;
            Logger = logger ?? new DefaultLogger();
            ErrorHandler = errorHandler ?? new DefaultErrorHandler(Logger, false);
        }

        public void Process(UserEvent userEvent)
        {
            var logEvent = EventFactory.CreateLogEvent(userEvent, Logger);

            try
            {
                EventDispatcher.DispatchEvent(logEvent);
                NotificationCenter?.SendNotifications(NotificationCenter.NotificationType.LogEvent,
                    logEvent);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.ERROR,
                    $"Error dispatching event: {logEvent.GetParamsAsJson()}. {ex.Message}");
                ErrorHandler.HandleError(ex);
            }
        }
    }
}
