using System;
using System.Reflection;
using Ipc;

namespace UnitTests.Stubs
{
    public class ProxyDispatchStub<T> : IProxyDispatch
    {
        private readonly T _instance;

        public ProxyDispatchStub(T instance)
        {
            var type = typeof (T);
            if (!type.IsInterface)
            {
                throw new ArgumentException("ProxyDispatchStub<T> requires T to be an interface");
            }

            _instance = instance;
        }

        public object OnProxyDispatch(MethodInfo methodInfo, object[] args)
        {
            return methodInfo.Invoke(_instance, args);
        }
    }
}