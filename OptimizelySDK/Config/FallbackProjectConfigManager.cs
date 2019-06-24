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

namespace OptimizelySDK.Config
{
    /// <summary>
    /// Implementation of ProjectConfigManager interface that simply
    /// returns the stored ProjectConfig instance which is immmutable.
    /// </summary>
    public class FallbackProjectConfigManager : ProjectConfigManager
    {
        private ProjectConfig ProjectConfig;        
        
        /// <summary>
        /// Initializes a new instance of the FallbackProjectConfigManager class
        /// with the given ProjectConfig instance.
        /// </summary>
        /// <param name="config">Instance of ProjectConfig.</param>
        public FallbackProjectConfigManager(ProjectConfig config)
        {
            ProjectConfig = config;
        }

        /// <summary>
        /// Returns the stored ProjectConfig instance.
        /// </summary>
        /// <returns>ProjectConfig instance</returns>
        public ProjectConfig GetConfig()
        {
            return ProjectConfig;
        }
    }
}
