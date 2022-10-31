/* 
 * Copyright 2022, Optimizely
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

using OptimizelySDK.Odp.Entity;

namespace OptimizelySDK.Odp
{
    public interface IOdpEventManager
    {
        /// <summary>
        /// Update ODP configuration settings
        /// </summary>
        /// <param name="odpConfig">Configuration object containing new values</param>
        void UpdateSettings(OdpConfig odpConfig);

        /// <summary>
        /// Start processing events in the queue
        /// </summary>
        void Start();

        /// <summary>
        /// Drain the queue sending all remaining events in batches then stop processing
        /// </summary>
        void Stop();

        /// <summary>
        /// Register a new visitor user id (VUID) in ODP
        /// </summary>
        /// <param name="vuid">Visitor ID to register</param>
        void RegisterVuid(string vuid);

        /// <summary>
        /// Associate a full-stack userid with an established VUID
        /// </summary>
        /// <param name="userId">Full-stack User ID</param>
        /// <param name="vuid">Visitor User ID</param>
        void IdentifyUser(string userId, string vuid);

        /// <summary>
        /// Send an event to ODP via dispatch queue
        /// </summary>
        /// <param name="odpEvent">ODP Event to forward</param>
        void SendEvent(OdpEvent odpEvent);
    }
}
