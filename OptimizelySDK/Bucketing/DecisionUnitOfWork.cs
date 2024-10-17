/*
 * Copyright 2024 Optimizely
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

namespace OptimizelySDK.Bucketing
{
    public class DecisionUnitOfWork
    {
        private readonly Dictionary<string, Dictionary<string, Decision>> _decisions =
            new Dictionary<string, Dictionary<string, Decision>>();

        public void AddDecision(string userId, string experimentId, Decision decision)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(experimentId))
            {
                return;
            }

            if (!_decisions.ContainsKey(userId))
            {
                _decisions[userId] = new Dictionary<string, Decision>();
            }

            _decisions[userId][experimentId] = decision;
        }

        public Dictionary<string, Dictionary<string, Decision>> GetDecisions()
        {
            return _decisions;
        }

        public bool HasDecisions => _decisions.Count > 0;

        public void ClearDecisions()
        {
            _decisions.Clear();
        }
    }
}
