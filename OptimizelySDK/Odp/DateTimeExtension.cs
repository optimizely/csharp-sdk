using System;

namespace OptimizelySDK.Odp
{
    public static class DateTimeExtension
    {
        // Because we're only on .NET Framework 4.5
        public static long ToUnixTimeMilliseconds(this DateTime dateTime)
        {
            var unixTimeStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64(dateTime.Subtract(unixTimeStart).TotalMilliseconds);
        }
    }
}