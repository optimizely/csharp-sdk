﻿/* 
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimizelySDK.Odp
{
    public class OdpConfig : IEquatable<OdpConfig>
    {
        /// <summary>
        /// Public API key for the ODP account from which the audience segments will be fetched (optional).
        /// </summary>
        public string ApiKey { get; private set; }

        /// <summary>
        /// Host of ODP audience segments API.
        /// </summary>
        public string ApiHost { get; private set; }

        /// <summary>
        /// All ODP segments used in the current datafile (associated with apiHost/apiKey).
        /// </summary>
        public List<string> SegmentsToCheck { get; private set; }


        public OdpConfig(string apiKey = null, string apiHost = null,
            List<string> segmentsToCheck = null
        )
        {
            ApiKey = apiKey ?? string.Empty;
            ApiHost = apiHost ?? string.Empty;
            SegmentsToCheck = segmentsToCheck ?? new List<string>(0);
        }

        /// <summary>
        /// Determines if ODP configuration has the minimum amount of information
        /// </summary>
        /// <returns>true if ODP configuration can be used otherwise false</returns>
        public virtual bool IsReady()
        {
            return !(string.IsNullOrWhiteSpace(ApiKey) || string.IsNullOrWhiteSpace(ApiHost));
        }

        /// <summary>
        /// Determines if ODP configuration contains segments
        /// </summary>
        /// <returns></returns>
        public bool HasSegments()
        {
            return SegmentsToCheck?.Count > 0;
        }

        /// <summary>
        /// Determine equality between two OdpConfig objects based on case-insensitive value comparisons
        /// </summary>
        /// <param name="otherConfig">OdpConfig object to compare current instance against</param>
        /// <returns>True if equal otherwise False</returns>
        public bool Equals(OdpConfig otherConfig)
        {
            // less expensive equality checks first
            if (otherConfig == null ||
                !string.Equals(ApiKey, otherConfig.ApiKey,
                    StringComparison.Ordinal) || // case-matters
                !string.Equals(ApiHost, otherConfig.ApiHost, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (SegmentsToCheck == null ||
                otherConfig.SegmentsToCheck == null ||
                SegmentsToCheck.Count != otherConfig.SegmentsToCheck.Count)
            {
                return false;
            }

            return SegmentsToCheck.TrueForAll(
                segment =>
                    otherConfig.SegmentsToCheck.Contains(segment,
                        StringComparer.OrdinalIgnoreCase));
        }
    }
}
