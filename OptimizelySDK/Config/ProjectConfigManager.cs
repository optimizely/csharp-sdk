﻿/* 
 * Copyright 2019, 2022-2023 Optimizely
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

namespace OptimizelySDK.Config
{
    /// <summary>
    /// Interface for fetching ProjectConfig instance.
    /// </summary>
    public interface ProjectConfigManager
    {
        /// <summary>
        /// Implementations of this method should block until a datafile is available.
        /// </summary>
        /// <returns>ProjectConfig instance</returns>
        ProjectConfig GetConfig();

        /// <summary>
        /// SDK key in use for this project
        /// </summary>
        string SdkKey { get; }

        /// <summary>
        /// Access to current cached project configuration
        /// </summary>
        /// <returns>ProjectConfig instance or null if project config is not ready</returns>
        ProjectConfig CachedProjectConfig { get; }
    }
}
