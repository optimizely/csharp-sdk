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

using System.Collections.Generic;

namespace OptimizelySDK.OptlyConfig
{
    public interface IOptimizelyConfig
    {
        /// <summary>
        /// Revision of the dataflie.
        /// </summary>
        string Revision { get; }

        /// <summary>
        /// Associative array of experiment key to OptimizelyExperiment(s) in the datafile
        /// </summary>
        Dictionary<string, OptimizelyExperiment> ExperimentsMap { get; }

        /// <summary>
        /// Associative array of feature key to OptimizelyFeature(s) in the datafile
        /// </summary>
        Dictionary<string, OptimizelyFeature> FeaturesMap { get; }


        /// <summary>
        /// Create maps for experiment and features to be returned as one object
        /// </summary>
        /// <param name="configObj">The project config</param>
        /// <returns>OptimizelyConfig Object</returns>
        OptimizelyConfig GetOptimizelyConfig(ProjectConfig configObj);
    }
}
