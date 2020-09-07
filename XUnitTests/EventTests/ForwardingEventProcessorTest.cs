/**
 *
 *    Copyright 2020, Optimizely and contributors
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */

using System;
using Moq;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.Notifications;
using Xunit;

namespace OptimizelySDK.XUnitTests.EventTests
{
    public class ForwardingEventProcessorTest
    {
        private const string UserId = "userId";
        private const string EventName = "purchase";

        private ForwardingEventProcessor EventProcessor;
        private TestForwardingEventDispatcher EventDispatcher;
        private NotificationCenter NotificationCenter = new NotificationCenter();

        Mock<ILogger> LoggerMock;
        Mock<IErrorHandler> ErrorHandlerMock;

        ProjectConfig ProjectConfig;

        public ForwardingEventProcessorTest()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            ErrorHandlerMock = new Mock<IErrorHandler>();
            ErrorHandlerMock.Setup(e => e.HandleError(It.IsAny<Exception>()));

            ProjectConfig = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, new NoOpErrorHandler());

            EventDispatcher = new TestForwardingEventDispatcher { IsUpdated = false };
            EventProcessor = new ForwardingEventProcessor(EventDispatcher, NotificationCenter, LoggerMock.Object, ErrorHandlerMock.Object);
        }

        [Fact]
        public void TestEventHandlerWithConversionEvent()
        {
            var userEvent = CreateConversionEvent(EventName);
            EventProcessor.Process(userEvent);            

            Assert.True(EventDispatcher.IsUpdated);
        }


        [Fact]
        public void TestExceptionWhileDispatching()
        {
            var eventProcessor = new ForwardingEventProcessor(new InvalidEventDispatcher(), NotificationCenter, LoggerMock.Object, ErrorHandlerMock.Object);
            var userEvent = CreateConversionEvent(EventName);

            eventProcessor.Process(userEvent);
            
            ErrorHandlerMock.Verify(errorHandler => errorHandler.HandleError(It.IsAny<Exception>()), Times.Once );            
        }

        [Fact]
        public void TestNotifications()
        {
            bool notificationTriggered = false;
            NotificationCenter.AddNotification(NotificationCenter.NotificationType.LogEvent, logEvent => notificationTriggered = true);
            var userEvent = CreateConversionEvent(EventName);

            EventProcessor.Process(userEvent);

            Assert.True(notificationTriggered);
        }


        private ConversionEvent CreateConversionEvent(string eventName)
        {
            return UserEventFactory.CreateConversionEvent(ProjectConfig, eventName, UserId, new UserAttributes(), new EventTags());
        }
    }
}
