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

using OptimizelySDK.Logger;
using System.Collections.Generic;

namespace OptimizelySDK.Odp
{
    public class OdpManager
    {
        private volatile OdpConfig _odpConfig;

        public OdpSegmentManager SegmentManager { get; }

        public OdpEventManager EventManager { get; }

        private ILogger _logger;

        public OdpManager(OdpConfig odpConfig, OdpSegmentManager segmentManager,
            OdpEventManager eventManager, ILogger logger = null
        )
        {
            _odpConfig = odpConfig;
            SegmentManager = segmentManager;
            EventManager = eventManager;
            _logger = logger;

            EventManager.Start();
        }

        public bool UpdateSettings(string apiHost, string apiKey, List<string> segmentsToCheck)
        {
            var newConfig = new OdpConfig(apiKey, apiHost, segmentsToCheck);
            if (_odpConfig.Equals(newConfig))
            {
                return false;
            }

            _odpConfig = newConfig;

            EventManager.UpdateSettings(_odpConfig);

            SegmentManager.ResetCache();
            SegmentManager.UpdateSettings(_odpConfig);

            return true;
        }

        public void Close()
        {
            EventManager.Stop();
        }
    }
}
