using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OptimizelySDK.Tests.Utils
{
    public static class Reflection
    {
        /// <summary>
        /// Returns field value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">Object from which you want to get variable</param>
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

        /// <summary>
        /// Returns field value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">Object from which you want to get variable</param>
        /// <param name="fieldName">Name of the field</param>
        /// <returns></returns>
        public static T GetFieldValue<T, U>(U obj, string fieldName, IEnumerable<FieldInfo> fieldsInfo)
        {
            foreach (var fieldInfo in fieldsInfo)
            {
                if (fieldInfo.Name == fieldName)
                {
                    return (T)fieldInfo.GetValue(obj);
                }
            }
            return (T)default(T);
        }


        /// <summary>
        /// Returns property value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">Object from which you want to get property value.</param>
        /// <param name="propertyName">Name of the property</param>
        /// <returns></returns>
        public static T GetPropertyValue<T, U>(U obj, string propertyName)
        {
            var propertyInfo = GetAllProperties(typeof(U));
            foreach (var popertyInfo in propertyInfo)
            {
                if (popertyInfo.Name == propertyName)
                {
                    return (T)popertyInfo.GetValue(obj);
                }
            }
            return (T)default(T);
        }

        /// <summary>
        /// Returns all properties info of provided type.
        /// </summary>
        /// <param name="type"> Type of which to Get all properties info.</param>
        /// <returns>IEnumerable<PropertyInfo></returns>
        public static IEnumerable<PropertyInfo> GetAllProperties(Type type)
        {
            if (type == null)
                return Enumerable.Empty<PropertyInfo>();
            var flags = BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Static | BindingFlags.Instance |
                                 BindingFlags.DeclaredOnly;
            return type.GetProperties(flags).Concat(GetAllProperties(type.BaseType));
        }

        /// <summary>
        /// Returns all fields info of provided type.
        /// </summary>
        /// <param name="type"> Type of which to Get all fields info.</param>
        /// <returns>IEnumerable<FieldInfo></returns>
        public static IEnumerable<FieldInfo> GetAllFields(Type t)
        {
            if (t == null)
                return Enumerable.Empty<FieldInfo>();
            var flags = BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Static | BindingFlags.Instance |
                                 BindingFlags.DeclaredOnly;
            return t.GetFields(flags).Concat(GetAllFields(t.BaseType));
        }
    }
}
