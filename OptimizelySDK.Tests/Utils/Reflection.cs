using System;
using System.Collections.Generic;
using System.Linq;
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
            var fieldsInfo = GetAllFields(typeof(U));
            foreach (var fieldInfo in fieldsInfo)
            {
                if (fieldInfo.Name == fieldName)
                {
                    return (T)fieldInfo.GetValue(obj);
                }
            }
            return (T)default(T);
        }

        public static T GetPropertyValue<T, U>(U obj, string fieldName)
        {
            var propertyInfo = GetAllProperties(typeof(U));
            foreach (var popertyInfo in propertyInfo)
            {
                if (popertyInfo.Name == fieldName)
                {
                    return (T)popertyInfo.GetValue(obj);
                }
            }
            return (T)default(T);
        }

        public static IEnumerable<PropertyInfo> GetAllProperties(Type t)
        {
            if (t == null)
                return Enumerable.Empty<PropertyInfo>();
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Static | BindingFlags.Instance |
                                 BindingFlags.DeclaredOnly;
            return t.GetProperties(flags).Concat(GetAllProperties(t.BaseType));
        }

        public static IEnumerable<FieldInfo> GetAllFields(Type t)
        {
            if (t == null)
                return Enumerable.Empty<FieldInfo>();
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Static | BindingFlags.Instance |
                                 BindingFlags.DeclaredOnly;
            return t.GetFields(flags).Concat(GetAllFields(t.BaseType));
        }
    }
}
