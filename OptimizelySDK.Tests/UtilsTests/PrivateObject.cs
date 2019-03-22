using System;
using System.Reflection;

namespace OptimizelySDK.Tests.UtilsTests
{
    internal class PrivateObject
    {
        private object createdInstance;
        private Type instanceType;

        public PrivateObject(Type privateObject, Type[] parameterTypes, object[] parameterValues)
        {
            instanceType = privateObject;
            createdInstance = Activator.CreateInstance(instanceType, parameterValues);
        }
        private PrivateObject(Type privateObject, object[] parameterValues)
        {
            instanceType = privateObject;
            createdInstance = Activator.CreateInstance(instanceType, parameterValues);
        }

        public void SetFieldOrProperty(string propertyName, object value)
        {
            instanceType.InvokeMember(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.NonPublic | BindingFlags.SetField,
                Type.DefaultBinder, createdInstance, new object[] { value });
        }

        public object Invoke(string name, params object[] args)
        {
            return instanceType.InvokeMember(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.NonPublic,
                Type.DefaultBinder, createdInstance, args);
        }

        public object InvokeGeneric<T>(string name, params object[] args)
        {
            MethodInfo method = instanceType.GetMethod(name);
            MethodInfo genericMethod = method.MakeGenericMethod(typeof(T));
            return genericMethod.Invoke(createdInstance, BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.NonPublic,
                Type.DefaultBinder, args, null);
        }

        public object GetObject()
        {
            return createdInstance;
        }
    }
}
