/* 
 * Copyright 2017, Optimizely
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
namespace OptimizelySDK.Entity
{
    public class Group : Entity
    {
        /// <summary>
        /// Group ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Group policy
        /// </summary>
        public string Policy { get; set; }

        /// <summary>
        /// List of Experiments in the group
        /// </summary>
        public Experiment[] Experiments { get; set; }

        /// <summary>
        /// List of Traffic allocation of experiments in the group
        /// </summary>
        public TrafficAllocation[] TrafficAllocation { get; set; }

#if false
    /**
     * @param $trafficAllocation array Traffic allocation of experiments in group.
     */
    public function setTrafficAllocation($trafficAllocation)
    {
        $this->_trafficAllocation = ConfigParser::generateMap($trafficAllocation, null, TrafficAllocation::class);
    }
#endif

    }
}
