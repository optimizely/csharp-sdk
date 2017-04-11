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
namespace OptimizelySDK.DemoApp.Utils
{
    /// <summary>
    /// Sample implementation of a log4net OptimizelySDK Logger implementation
    /// This maps the the logger calls to the corresponding log4net call
    /// </summary>
    public class Log4NetLogger : OptimizelySDK.Logger.ILogger
    {
        readonly static log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(Log4NetLogger));

        public void Log(OptimizelySDK.Logger.LogLevel level, string message)
        {
            // Add more info to the messages to demonstrate which logs are coming from this Sample Logger
            message = string.Format("[Optimizely Sample Log4NetLogger] {0}", message);

            // Log to log4net mapping to the appropriate level
            Logger.Logger.Log(typeof(Log4NetLogger), MapLogLevel(level), message, null);
        }

        private static log4net.Core.Level MapLogLevel(OptimizelySDK.Logger.LogLevel level)
        {
            switch (level)
            {
                default:
                case OptimizelySDK.Logger.LogLevel.INFO:  return log4net.Core.Level.Info;
                case OptimizelySDK.Logger.LogLevel.DEBUG: return log4net.Core.Level.Debug;
                case OptimizelySDK.Logger.LogLevel.ERROR: return log4net.Core.Level.Error;
            }
        }
    }
}