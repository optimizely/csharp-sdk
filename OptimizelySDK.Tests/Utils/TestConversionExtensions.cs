/* 
 * Copyright 2017-2022, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using Moq;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Tests.Utils
{
    public static class TestConversionExtensions
    {
        public static OptimizelyUserContext ToUserContext(this UserAttributes attributes)
        {
            var mockLogger = new Mock<ILogger>();
            mockLogger.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            
            var errorHandler = new NoOpErrorHandler();
            var config = DatafileProjectConfig.Create(
                content: TestData.Datafile,
                logger: mockLogger.Object,
                errorHandler: errorHandler);
            var configManager = new FallbackProjectConfigManager(config);
            var optimizely = new Optimizely(configManager);
            
            return new OptimizelyUserContext(optimizely, "any-user", attributes, errorHandler,
                mockLogger.Object);
        }
    }
}