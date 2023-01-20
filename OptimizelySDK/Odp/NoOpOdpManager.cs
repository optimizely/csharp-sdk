﻿using System;
using System.Collections.Generic;

/* 
 * Copyright 2023 Optimizely
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

namespace OptimizelySDK.Odp
{
    public class NoOpOdpManager : IOdpManager, IDisposable
    {
        public bool UpdateSettings(string apiKey, string apiHost, List<string> segmentsToCheck)
        {
            return false;
        }

        public string[] FetchQualifiedSegments(string userId, List<OdpSegmentOption> options)
        {
            return null;
        }

        public void IdentifyUser(string userId) { }

        public void SendEvent(string type, string action, Dictionary<string, string> identifiers,
            Dictionary<string, object> data
        ) { }

        public void Dispose() { }
    }
}
