using OptimizelySDK.Utils;
using System;

namespace OptimizelySDK.Config.audience.match
{
    public class NumberComparator
    {
        public static int Compare(object o1, object o2)
        {
            if (!Validator.IsValidNumericValue(o1) || !Validator.IsValidNumericValue(o2))
            {
                throw new Exception("Unknown value type exception");
            }

            return CompareUnsafe(o1, o2);
        }

        static int CompareUnsafe(object o1, object o2)
        {
            return Convert.ToDouble(o1).CompareTo(Convert.ToDouble(o2));
        }
    }
}
