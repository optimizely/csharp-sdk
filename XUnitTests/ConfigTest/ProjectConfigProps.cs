/**
 *
 *    Copyright 2020, Optimizely and contributors
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */
using System;
using OptimizelySDK.Config;
using OptimizelySDK.XUnitTests.Utils;

namespace OptimizelySDK.XUnitTests.ConfigTest
{
    /// <summary>
    /// This is a helper class for optimizely factory unit testing and is only to expose private properties of HttpProjectConfigManager and its super classes.
    /// </summary>
    public class ProjectConfigManagerProps
    {
        public string LastModified { get; set; }
        public string Url { get; set; }
        public string DatafileAccessToken { get; set; }
        public TimeSpan PollingInterval { get; set; }
        public TimeSpan BlockingTimeout { get; set; }
        public bool AutoUpdate { get; set; }

        public ProjectConfigManagerProps(HttpProjectConfigManager projectConfigManager)
        {
            LastModified = Reflection.GetFieldValue<string, HttpProjectConfigManager>(projectConfigManager, "LastModifiedSince");
            Url = Reflection.GetFieldValue<string, HttpProjectConfigManager>(projectConfigManager, "Url");
            DatafileAccessToken = Reflection.GetFieldValue<string, HttpProjectConfigManager>(projectConfigManager, "DatafileAccessToken");

            AutoUpdate = Reflection.GetPropertyValue<bool, HttpProjectConfigManager>(projectConfigManager, "AutoUpdate");
            PollingInterval = Reflection.GetFieldValue<TimeSpan, HttpProjectConfigManager>(projectConfigManager, "PollingInterval");
            BlockingTimeout = Reflection.GetFieldValue<TimeSpan, HttpProjectConfigManager>(projectConfigManager, "BlockingTimeout");
        }

        /// <summary>
        /// To create default instance of expected values.
        /// </summary>
        public ProjectConfigManagerProps()
        {

        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var projectConfigManager = obj as ProjectConfigManagerProps;
            if (projectConfigManager == null)
            {
                return false;
            }

            if (LastModified != projectConfigManager.LastModified ||
                Url != projectConfigManager.Url ||
                DatafileAccessToken != projectConfigManager.DatafileAccessToken ||
                BlockingTimeout != projectConfigManager.BlockingTimeout ||
                AutoUpdate != projectConfigManager.AutoUpdate ||
                PollingInterval != projectConfigManager.PollingInterval)
            {
                return false;
            }

            return true;
        }
    }
}
