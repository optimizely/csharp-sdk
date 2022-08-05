using System.Collections;

namespace OptimizelySDK.Tests.OdpTests
{
    public static class CollectionTestExtensions
    {
        public static string[] ToKeyCollection(this ICollection keys)
        {
            var keyArray = new string[keys.Count];
            keys.CopyTo(keyArray, 0);
            return keyArray;
        }
    }
}