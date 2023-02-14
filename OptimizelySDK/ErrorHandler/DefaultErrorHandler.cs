/* 
 * Copyright 2017, Optimizely
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

using OptimizelySDK.Logger;
using System;

namespace OptimizelySDK.ErrorHandler
{
    public class DefaultErrorHandler : IErrorHandler
    {
        /// <summary>
        /// Create a DefaultErrorHandler
        /// </summary>
        /// <param name="logger">Optional logger to be used to include exception message in the log</param>
        /// <param name="throwExceptions">Whether or not to actaully throw the exceptions, true by default</param>
        public DefaultErrorHandler(ILogger logger = null, bool throwExceptions = true)
        {
            Logger = logger;
            ThrowExceptions = throwExceptions;
        }

        public void HandleError(Exception exception)
        {
            if (Logger != null)
            {
                Logger.Log(LogLevel.ERROR, exception.Message);
            }

            if (ThrowExceptions)
            {
                throw exception;
            }
        }

        /// <summary>
        /// An optional Logger include exceptions in your log
        /// </summary>
        private ILogger Logger;

        private bool ThrowExceptions;
    }
}
