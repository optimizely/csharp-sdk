/* 
 * Copyright 2022-2023, Optimizely
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

namespace OptimizelySDK.Utils
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// Merge a Dictionary into another without overwriting existing keys
        /// </summary>
        /// <param name="left">Dictionary that will be merged into</param>
        /// <param name="right">Dictionary to merge into another</param>
        /// <typeparam name="TKey">Key type</typeparam>
        /// <typeparam name="TValue">Value type</typeparam>
        /// <exception cref="ArgumentNullException">Thrown if target Dictionary is null</exception>
        public static void MergeInPlace<TKey, TValue>(this Dictionary<TKey, TValue> left,
            Dictionary<TKey, TValue> right
        )
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left),
                    "Cannot merge into a null dictionary");
            }

            if (right == null)
            {
                return;
            }

            foreach (var kvp in right.Where(
                         kvp => !left.ContainsKey(kvp.Key)))
            {
                left.Add(kvp.Key, kvp.Value);
            }
        }
    }
}
