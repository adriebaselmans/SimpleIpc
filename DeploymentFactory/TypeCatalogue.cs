using System;
using System.Collections.Generic;
using System.Linq;

namespace DeploymentFactory
{
    public static class TypeCatalogue
    {
        private static readonly IDictionary<Type, object> TypeDictionary = new Dictionary<Type, object>();

        public static void Register<T>(object instance)
        {
            var type = typeof (T);

            if (!type.IsInstanceOfType(instance))
            {
                throw new ArgumentException(string.Format("Object '{0}' is not of type '{1}'", instance, type));
            }

            if (TypeDictionary.ContainsKey(type))
            {
                throw new ArgumentException(string.Format("The type '{0}' was already registered as '{1}'", type, TypeDictionary[type]));
            }

            TypeDictionary.Add(new KeyValuePair<Type, object>(type, instance));
        }

        public static dynamic Resolve<T>()
        {
            var type = typeof (T);

            if (TypeDictionary.ContainsKey(type))
            {
                return TypeDictionary[type];
            }

            return null;
        }

        public static void Clear(bool disposeInstances = true)
        {
            if (disposeInstances)
            {
                foreach (var disposable in TypeDictionary.Values.Select(value => value as IDisposable))
                {
                    if (disposable != null) disposable.Dispose();
                }
            }
            
            TypeDictionary.Clear();
        }
    }
}