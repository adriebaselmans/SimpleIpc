using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Ipc
{
    public static class ProxyFactory
    {
        private static readonly ModuleBuilder ModuleBuilder;
        private static readonly Dictionary<string, Type> TypeCache;

        static ProxyFactory()
        {
            var assemblyBuilder =
                AppDomain.CurrentDomain.DefineDynamicAssembly(
                    new AssemblyName("Ipc.Proxies"),
                    AssemblyBuilderAccess.Run);

            var assemblyName = assemblyBuilder.GetName().Name;
            ModuleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName);
            TypeCache = new Dictionary<string, Type>();
        }

        public static T CreateProxy<T>(IProxyDispatch proxyDispatch)
        {
            var t = typeof (T);
            ThrowIfNoInterfaceIsUsed(t);

            var typeName = string.Format("{0}_IpcProxy", t.Name);
            Type proxyType;
            if (TypeCache.ContainsKey(typeName))
            {
                proxyType = TypeCache[typeName];
            }
            else
            {
                proxyType = GenerateProxyType<T>(typeName);
                TypeCache.Add(typeName, proxyType);
            }

            return CreateProxyInstance<T>(proxyDispatch, proxyType);
        }

        private static T CreateProxyInstance<T>(IProxyDispatch proxyDispatch, Type proxyType)
        {
            return (T) Activator.CreateInstance(proxyType, proxyDispatch);
        }

        private static Type GenerateProxyType<T>(string typeName)
        {
            var proxyInterfaceType = typeof (T);
            var typeBuilder = ModuleBuilder.DefineType(
                typeName,
                TypeAttributes.Class | TypeAttributes.Public,
                typeof (object),
                new[] {proxyInterfaceType});

            var proxyDispatchFieldBuilder =
                typeBuilder.DefineField("_proxyDispatch", typeof (IProxyDispatch), FieldAttributes.Private);

            GenerateProxyConstructor(typeBuilder, proxyDispatchFieldBuilder);

            var proxyDispatchMethodName = GetProxyInterfaceMethodName();

            CreateProxyMethods<T>(proxyDispatchMethodName, typeBuilder, proxyDispatchFieldBuilder);

            return typeBuilder.CreateType();
        }

        private static void CreateProxyMethods<T>(string proxyDispatchMethodName, TypeBuilder typeBuilder,
            FieldBuilder proxyDispatchFieldBuilder)
        {
            var proxyInterfaceType = typeof (T);
            var callRetMethod = typeof (IProxyDispatch).GetMethod(
                proxyDispatchMethodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] {typeof (MethodInfo), typeof (object[])},
                null);

            foreach (var mi in proxyInterfaceType.GetMethods())
            {
                var mb = typeBuilder.DefineMethod(
                    mi.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
                    MethodAttributes.Virtual,
                    mi.ReturnType,
                    mi.GetParameters().Select(pi => pi.ParameterType).ToArray());

                var il = mb.GetILGenerator();

                var privateParameterCount = mi.GetParameters().Length;

                var argArray = il.DeclareLocal(typeof (object[]));
                il.Emit(OpCodes.Ldc_I4, privateParameterCount);
                il.Emit(OpCodes.Newarr, typeof (object));
                il.Emit(OpCodes.Stloc, argArray);

                var methodInfo = il.DeclareLocal(typeof (MethodInfo));
                il.Emit(OpCodes.Ldtoken, mi);
                il.Emit(OpCodes.Call,
                    typeof (MethodBase).GetMethod("GetMethodFromHandle", new[] {typeof (RuntimeMethodHandle)}));
                il.Emit(OpCodes.Stloc, methodInfo);

                for (var i = 0; i < mi.GetParameters().Length; i++)
                {
                    var info = mi.GetParameters()[i];

                    il.Emit(OpCodes.Ldloc, argArray);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldarg_S, i + 1);
                    if (info.ParameterType.IsPrimitive || info.ParameterType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, info.ParameterType);
                    }
                    il.Emit(OpCodes.Stelem_Ref);
                }

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, proxyDispatchFieldBuilder);
                il.Emit(OpCodes.Ldloc, methodInfo);
                il.Emit(OpCodes.Ldloc, argArray);
                il.Emit(OpCodes.Call, callRetMethod);
                if (mi.ReturnType.IsValueType && mi.ReturnType != typeof (void))
                {
                    il.Emit(OpCodes.Unbox_Any, mi.ReturnType);
                }
                if (mi.ReturnType == typeof (void))
                {
                    il.Emit(OpCodes.Pop);
                }

                il.Emit(OpCodes.Ret);
            }
        }

        private static void GenerateProxyConstructor(TypeBuilder typeBuilder, FieldBuilder proxyDispatchFieldBuilder)
        {
            var objectType = typeof (object);
            var constructorInfo = objectType.GetConstructor(new Type[] {});

            var constructorBuilder =
                typeBuilder.DefineConstructor(
                    MethodAttributes.Public, CallingConventions.HasThis,
                    new[] {typeof (IProxyDispatch)});

            var cIl = constructorBuilder.GetILGenerator();

            cIl.Emit(OpCodes.Ldarg_0); // Load this to stack
            cIl.Emit(OpCodes.Call, constructorInfo); // Call base (object) constructor

            cIl.Emit(OpCodes.Ldarg_0); // Load this to stack
            cIl.Emit(OpCodes.Ldarg_1); // Load the IProxyHandler to stack
            cIl.Emit(OpCodes.Stfld, proxyDispatchFieldBuilder); // Set proxy to the actual proxy

            cIl.Emit(OpCodes.Ret);
        }

        private static string GetProxyInterfaceMethodName()
        {
            var proxyDispatchType = typeof (IProxyDispatch);
            var proxyDispatchTypeMethods = proxyDispatchType.GetMethods();
            var proxyDispatchMethodName = proxyDispatchTypeMethods[0].Name;
            return proxyDispatchMethodName;
        }

        private static void ThrowIfNoInterfaceIsUsed(Type type)
        {
            if (!type.IsInterface)
            {
                throw new ArgumentException("ProxyFactory<T> requires T to be an interface");
            }
        }
    }
}