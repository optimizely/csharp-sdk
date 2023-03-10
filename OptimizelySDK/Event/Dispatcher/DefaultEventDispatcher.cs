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

namespace OptimizelySDK.Event.Dispatcher
{
    /// <summary>
    /// Default Event  Dispatcher
    /// Selects the appropriate dispatcher based on the .Net Framework version
    /// </summary>
    public class DefaultEventDispatcher :
#if NET35 || NET40
        WebRequestClientEventDispatcher35
#else
        HttpClientEventDispatcher45
#endif
    {
        public DefaultEventDispatcher(Logger.ILogger logger)
        {
            Logger = logger;
        }
    }
}
