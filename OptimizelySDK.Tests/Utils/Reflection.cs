using System;
using System.Reflection;

namespace OptimizelySDK.Tests.Utils
{
    public static class Reflection
    {
        /// <summary>
        /// Returns non-public field value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type">Object metadata information</param>
        /// <param name="fieldName">Name of the field</param>
        /// <returns></returns>
        public static T GetFieldValue<T, U>(U obj, string fieldName)
        {
            var fieldInfo = Reflection.GetFieldInfo(typeof(U), fieldName);
            return (T)fieldInfo.GetValue(obj);
        }

        public static T GetFieldValue<T, U>(Type t,U obj, string fieldName)
        {
            var fieldInfo = GetFieldInfo(t, fieldName);
            return (T)fieldInfo.GetValue(obj);
        }

        public static FieldInfo GetFieldInfo(Type t, string fieldName)
        {
            return t.GetField(fieldName, System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.NonPublic);
        }
    }
}
