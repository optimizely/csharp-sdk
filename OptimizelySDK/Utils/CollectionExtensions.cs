using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimizelySDK.Utils
{
    public static class CollectionExtensions
    {
        public static Dictionary<TKey, TValue> MergeInPlace<TKey, TValue>(
            this Dictionary<TKey, TValue> left, Dictionary<TKey, TValue> right
        )
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left),
                    "Cannot merge into a null dictionary");
            }

            if (right == null)
            {
                return left;
            }

            foreach (KeyValuePair<TKey, TValue> kvp in right.Where(
                         kvp => !left.ContainsKey(kvp.Key)))
            {
                left.Add(kvp.Key, kvp.Value);
            }

            return left;
        }
    }
}
