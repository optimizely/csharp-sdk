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
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using System;
using System.Collections.Generic;

namespace OptimizelySDK.DemoApp.Utils
{
    public class HandlerItem
    {
        public string Timestamp { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Helper class to collect Logs and Errors in memory for display to the user for troubleshooting
    /// </summary>
    public class InMemoryHandler : ILogger, IErrorHandler
    {
        public List<HandlerItem> HandlerItems { get; private set; } = new List<HandlerItem>();

        public void Clear()
        {
            HandlerItems = new List<HandlerItem>();
        }

        private string Timestamp
        {
            get
            {
                var now = DateTime.Now;
                return string.Format("{0} {1}", now.ToShortDateString(), now.ToLongTimeString());
            }
        }

        public void HandleError(Exception exception)
        {
            HandlerItems.Add(new HandlerItem
            {
                Timestamp = Timestamp,
                Type = "ERROR",
                Message = exception.Message
            });
        }

        public void Log(LogLevel level, string message)
        {
            HandlerItems.Add(new HandlerItem
            {
                Timestamp = Timestamp,
                Type = "LOG " + level,
                Message = message
            });
        }
    }
}