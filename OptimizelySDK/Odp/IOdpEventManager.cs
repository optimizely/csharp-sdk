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
    /// <summary>
    /// Interfacte for a manager responsible for queuing and sending events to the
    /// Optimizely Data Platform
    /// </summary>
    public interface IOdpEventManager
    {
        /// <summary>
        /// Begin the execution thread to process the queue into bathes and send events
        /// </summary>
        void Start();
        
        /// <summary>
        /// Signal that all ODP events in queue should be sent
        /// </summary>
        void Flush();
        
        /// <summary>
        /// Stops ODP event processor.
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Add event to queue for sending to ODP
        /// </summary>
        /// <param name="odpEvent">Event to enqueue</param>
        void SendEvent(OdpEvent odpEvent);
        
        /// <summary>
        /// Ensures queue processing is stopped marking this instance as disposed 
        /// </summary>
        void Dispose();
        
        /// <summary>
        /// Associate a full-stack userid with an established VUID
        /// </summary>
        /// <param name="userId">Full-stack User ID</param>
        void IdentifyUser(string userId);

        /// <summary>
        /// Update ODP configuration settings
        /// </summary>
        /// <param name="odpConfig">Configuration object containing new values</param>
        void UpdateSettings(OdpConfig odpConfig);
    }
}
