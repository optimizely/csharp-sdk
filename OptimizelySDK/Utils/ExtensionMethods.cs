using System;
using System.Text;

namespace OptimizelySDK.Utils
{
    public static class ExtensionMethods
    {
        public static string GetAllMessages(this Exception exception)
        {
            StringBuilder sb = new StringBuilder();
            while (exception != null)
            {
                if (!string.IsNullOrEmpty(exception.Message))
                {
                    if (sb.Length > 0)
                        sb.Append(" ");

                    sb.Append(exception.Message);
                }

                exception = exception.InnerException;
            }

            return sb.ToString();
        }
    }
}
