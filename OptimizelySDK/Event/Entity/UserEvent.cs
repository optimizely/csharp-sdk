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

namespace OptimizelySDK.Event.Entity
{
    public class UserEvent
    {
        /// <summary>
        /// Helper to compute Unix time (i.e. since Jan 1, 1970)
        /// </summary>
        protected static long SecondsSince1970
        {
            get
            {
                return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            }
        }

        public EventContext Context { get; protected set; }
        public string UUID { get; protected set; }
        public long Timestamp { get; protected set; }
    }
}
