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
    public interface IGraphQLManager
    {
        /// <summary>
        /// Retrieves the audience segments from ODP
        /// </summary>
        /// <param name="apiKey">ODP public key</param>
        /// <param name="apiHost">Fully-qualified URL of ODP</param>
        /// <param name="userKey">'vuid' or 'fs_user_id key'</param>
        /// <param name="userValue">Associated value to query for the user key</param>
        /// <param name="segmentsToCheck">Audience segments to check for experiment inclusion</param>
        /// <returns>Array of audience segments</returns>
        string[] FetchSegments(string apiKey,
            string apiHost,
            OdpUserKeyType userKey,
            string userValue,
            List<string> segmentsToCheck
        );
    }
}
