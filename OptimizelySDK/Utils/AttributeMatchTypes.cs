/* 
 * Copyright 2019-2020, Optimizely
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

namespace OptimizelySDK.Utils
{
    public static class AttributeMatchTypes
    {
        public const string EXACT = "exact";
        public const string EXIST = "exists";
        public const string GREATER_OR_EQUAL = "ge";
        public const string GREATER_THAN = "gt";
        public const string LESS_OR_EQUAL = "le";
        public const string LESS_THAN = "lt";
        public const string SUBSTRING = "substring";
        public const string SEMVER_EQ = "semver_eq";
        public const string SEMVER_GE = "semver_ge";
        public const string SEMVER_GT = "semver_gt";
        public const string SEMVER_LE = "semver_le";
        public const string SEMVER_LT = "semver_lt";
    }
}
