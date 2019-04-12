/* 
 * Copyright 2017, 2019, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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

        public object InvokeGeneric(string name, Type[] genericTypes, params object[] args)
        {
            MethodInfo method = instanceType.GetMethod(name);
            MethodInfo genericMethod = method.MakeGenericMethod(genericTypes);
            return genericMethod.Invoke(createdInstance, BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.NonPublic,
                Type.DefaultBinder, args, null);
        }

        public object GetObject()
        {
            return createdInstance;
        }
    }
}
