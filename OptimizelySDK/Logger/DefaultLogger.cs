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
using System;

namespace OptimizelySDK.Logger
{
    /// <summary>
    /// TODO: use Log4Net as the default logger?
    /// </summary>
    public class DefaultLogger : ILogger
    {
        public volatile LogLevel Verbosity;
        public DefaultLogger()
        {
            Verbosity=LogLevel.ALL;
        }
        public void Log(LogLevel level, string message)
        {
            // NOTE: Optimizely's csharp-sdk *.nupkg only includes Release *.dll's .
            // Hence, csharp-sdk DefaultLogger's Log must use Console.WriteLine here
            // instead of Debug.WriteLine because the latter would turn into NOP's
            // in *.nupkg Release *.dll's .
            if (level>=Verbosity) {
                string line = string.Format("[{0}] : {1}",level,message);
                Console.WriteLine(line);
            }
        }
    }
}
