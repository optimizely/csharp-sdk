/* 
 * Copyright 2022-2023 Optimizely
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

using System.Collections.Generic;

namespace OptimizelySDK.Odp.Entity
{
    /// <summary>
    /// Event data object targeted to requirements of the Optimizely Data Platform
    /// </summary>
    public class OdpEvent
    {
        /// <summary>
        /// Type of event (typically `fullstack` from SDK events)
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Subcategory of the event type
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// Dictionary for identifiers. The caller must provide at least one key-value pair.
        /// </summary>
        public Dictionary<string, string> Identifiers { get; set; }

        /// <summary>
        /// Event data in a key-value pair format
        /// </summary>
        public Dictionary<string, object> Data { get; set; }

        /// <summary>
        /// Event to be sent and stored in the Optimizely Data Platform
        /// </summary>
        /// <param name="type">Type of event (typically `fullstack` from SDK events)</param>
        /// <param name="action">Subcategory of the event type</param>
        /// <param name="identifiers">Key-value map of user identifiers</param>
        /// <param name="data">Event data in a key-value pair format</param>
        public OdpEvent(string type, string action, Dictionary<string, string> identifiers,
            Dictionary<string, object> data = null
        )
        {
            Type = type ?? Constants.ODP_EVENT_TYPE;
            Action = action;
            Identifiers = identifiers ?? new Dictionary<string, string>();
            Data = data ?? new Dictionary<string, object>();
        }
    }
}
