/**
 *
 *    Copyright 2017, Optimizely and contributors
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
using System.Collections.Generic;
using Moq;
using OptimizelySDK.Logger;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Entity;
using NUnit.Framework;
using OptimizelySDK.Bucketing;
using OptimizelySDK.Exceptions;

namespace OptimizelySDK.Tests
{
    public class DefaultErrorHandlerTest
    {
        private DefaultErrorHandler DefaultErrorHandler;
        private Mock<ILogger> LoggerMock;

        [SetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
        }

        [Test]
        public void TestErrorHandlerMessage()
        {
            DefaultErrorHandler = new DefaultErrorHandler(LoggerMock.Object, false);
            string testingException = "Testing exception";
            try
            {
                throw new OptimizelyException("Testing exception");
            }
            catch(OptimizelyException ex)
            {
                DefaultErrorHandler.HandleError(ex);
            }

            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, testingException), Times.Once);
        }

        [Test]
        [ExpectedException]
        public void TestErrorHandlerMessageWithThrowException()
        {
            DefaultErrorHandler = new DefaultErrorHandler(LoggerMock.Object, true);
            string testingException = "Testing and throwing exception";
            try
            {
                throw new OptimizelyException("Testing exception");
            }
            catch (OptimizelyException ex)
            {
                //have to throw exception. 
                DefaultErrorHandler.HandleError(ex);
            }

            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, testingException), Times.Once);
        }

    }
}
