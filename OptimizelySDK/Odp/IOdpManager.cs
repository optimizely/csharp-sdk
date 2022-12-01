/* 
 * Copyright 2022 Optimizely
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

namespace OptimizelySDK.Odp
{
    /// <summary>
    /// Interface describing orchestration of segment manager, event manager, and ODP config
    /// </summary>
    public interface IOdpManager
    {
        /// <summary>
        /// Update the settings being used for ODP configuration and reset/restart dependent processes
        /// </summary>
        /// <param name="apiKey">Public API key from ODP</param>
        /// <param name="apiHost">Host portion of the URL of ODP</param>
        /// <param name="segmentsToCheck">Audience segments to consider</param>
        /// <returns>True if settings were update otherwise False</returns>
        bool UpdateSettings(string apiKey, string apiHost, List<string> segmentsToCheck);

        /// <summary>
        /// Attempts to fetch and return a list of a user's qualified segments.
        /// </summary>
        /// <param name="userId">FS User ID</param>
        /// <param name="options">Options used during segment cache handling</param>
        /// <returns>Qualified segments for the user from the cache or the ODP server</returns>
        List<string> FetchQualifiedSegments(string userId, List<OdpSegmentOption> options);

        /// <summary>
        /// Send identification event to ODP for a given full-stack User ID
        /// </summary>
        /// <param name="userId">User ID to send</param>
        void IdentifyUser(string userId);

        /// <summary>
        /// Add event to queue for sending to ODP
        /// </summary>
        /// <param name="type">Type of event (typically `fullstack` from server-side SDK events)</param>
        /// <param name="action">Subcategory of the event type</param>
        /// <param name="identifiers">Key-value map of user identifiers</param>
        /// <param name="data">Event data in a key-value pair format</param>
        void SendEvent(string type, string action, Dictionary<string, string> identifiers,
            Dictionary<string, object> data
        );

        /// <summary>
        /// Sends signal to stop Event Manager and clean up ODP Manager use
        /// </summary>
        void Dispose();
    }
}
