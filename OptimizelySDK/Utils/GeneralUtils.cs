using System;

namespace OptimizelySDK.Utils
{
    class GeneralUtils
    {
        /// <summary>
        /// Helper to compute Unix time (i.e. since Jan 1, 1970)
        /// </summary>
        public static long SecondsSince1970
        {
            get
            {
                return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            }
        }
    }
}
