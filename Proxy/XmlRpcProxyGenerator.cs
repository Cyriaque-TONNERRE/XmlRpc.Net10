// <summary>
// XmlRpc.Net10 - A modern XML-RPC client library for .NET 10
// Copyright (c) 2026 XmlRpc.Net10 Contributors
// Licensed under the MIT License
// </summary>

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using XmlRpc.Client;

namespace XmlRpc.Proxy;

/// <summary>
/// Generates dynamic proxy classes for XML-RPC interfaces.
/// </summary>
public static class XmlRpcProxyGenerator
{
    private static readonly AssemblyBuilder AssemblyBuilder;
    private static readonly ModuleBuilder ModuleBuilder;
    private static readonly object LockObject = new();
    private static int _typeCounter;

    static XmlRpcProxyGenerator()
    {
        var assemblyName = new AssemblyName("XmlRpc.Proxies.Dynamic");
        AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);

        ModuleBuilder = AssemblyBuilder.DefineDynamicModule("MainModule");
    }

    /// <summary>
    /// Creates a proxy instance for the specified interface type.
    /// </summary>
    /// <typeparam name="T">The interface type.</typeparam>
    /// <param name="client">The XML-RPC client to use.</param>
    /// <returns>A proxy instance implementing the interface.</returns>
    public static T CreateProxy<T>(IXmlRpcClient client) where T : class
    {
        var interfaceType = typeof(T);
        if (!interfaceType.IsInterface)
        {
            throw new ArgumentException($"Type {interfaceType.Name} must be an interface.", nameof(T));
        }

        var proxyType = GenerateProxyType(interfaceType);
        var proxy = (T)Activator.CreateInstance(proxyType, client)!;
        return proxy;
    }

    /// <summary>
    /// Creates a proxy instance for the specified interface type using the given server URL.
    /// </summary>
    /// <typeparam name="T">The interface type.</typeparam>
    /// <param name="serverUrl">The XML-RPC server URL.</param>
    /// <returns>A proxy instance implementing the interface.</returns>
    public static T CreateProxy<T>(string serverUrl) where T : class
    {
        var client = new Client.XmlRpcClient(serverUrl);
        return CreateProxy<T>(client);
    }

    private static Type GenerateProxyType(Type interfaceType)
    {
        lock (LockObject)
        {
            var typeName = $"Proxy_{interfaceType.Name}_{Interlocked.Increment(ref _typeCounter)}";
            var typeBuilder = ModuleBuilder.DefineType(
                typeName,
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed,
                typeof(object),
                new[] { interfaceType });

            // Define the _client field
            var clientField = typeBuilder.DefineField(
                "_client",
                typeof(IXmlRpcClient),
                FieldAttributes.Private | FieldAttributes.InitOnly);

            // Define the constructor
            var constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new[] { typeof(IXmlRpcClient) });

            var ilGenerator = constructorBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, clientField);
            ilGenerator.Emit(OpCodes.Ret);

            // Get method prefix from service attribute
            var serviceAttr = interfaceType.GetCustomAttribute<XmlRpcServiceAttribute>();
            var methodPrefix = serviceAttr?.MethodPrefix;
            if (serviceAttr?.UseInterfaceNameAsPrefix == true && string.IsNullOrEmpty(methodPrefix))
            {
                methodPrefix = interfaceType.Name;
            }

            // Implement each interface method
            foreach (var method in interfaceType.GetMethods())
            {
                ImplementMethod(typeBuilder, method, clientField, methodPrefix);
            }

            return typeBuilder.CreateType()!;
        }
    }

    private static void ImplementMethod(
        TypeBuilder typeBuilder,
        MethodInfo method,
        FieldBuilder clientField,
        string? methodPrefix)
    {
        var methodAttr = method.GetCustomAttribute<XmlRpcMethodAttribute>();
        var methodName = methodAttr?.MethodName ?? method.Name;
        if (!string.IsNullOrEmpty(methodPrefix))
        {
            methodName = $"{methodPrefix}.{methodName}";
        }

        var parameters = method.GetParameters();

        // Determine the method signature
        var paramTypes = parameters.Select(p => p.ParameterType).ToArray();
        var methodBuilder = typeBuilder.DefineMethod(
            method.Name,
            MethodAttributes.Public | MethodAttributes.Virtual,
            method.ReturnType,
            paramTypes);

        var il = methodBuilder.GetILGenerator();

        // Local variables
        var paramsArrayLocal = il.DeclareLocal(typeof(object[]));
        var requestLocal = il.DeclareLocal(typeof(Core.XmlRpcRequest));
        var responseLocal = il.DeclareLocal(typeof(Task<Core.XmlRpcResponse>));
        var resultLocal = il.DeclareLocal(typeof(Core.XmlRpcResponse));

        // Create parameters array
        il.Emit(OpCodes.Ldc_I4, parameters.Length);
        il.Emit(OpCodes.Newarr, typeof(object));
        il.Emit(OpCodes.Stloc, paramsArrayLocal);

        for (var i = 0; i < parameters.Length; i++)
        {
            il.Emit(OpCodes.Ldloc, paramsArrayLocal);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldarg, i + 1); // +1 because 0 is 'this'

            var paramType = parameters[i].ParameterType;
            if (paramType.IsValueType)
            {
                il.Emit(OpCodes.Box, paramType);
            }

            il.Emit(OpCodes.Stelem_Ref);
        }

        // Create XmlRpcRequest
        il.Emit(OpCodes.Ldstr, methodName);
        il.Emit(OpCodes.Ldloc, paramsArrayLocal);
        il.Emit(OpCodes.Newobj, typeof(Core.XmlRpcRequest).GetConstructor(new[] { typeof(string), typeof(object[]) })!);
        il.Emit(OpCodes.Stloc, requestLocal);

        // Call client.InvokeAsync
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, clientField);
        il.Emit(OpCodes.Ldloc, requestLocal);
        il.Emit(OpCodes.Ldc_I4_0); // CancellationToken.None
        il.Emit(OpCodes.Call, typeof(CancellationToken).GetProperty("None")!.GetMethod!);
        il.Emit(OpCodes.Callvirt, typeof(IXmlRpcClient).GetMethod("InvokeAsync", new[] { typeof(Core.XmlRpcRequest), typeof(CancellationToken) })!);
        il.Emit(OpCodes.Stloc, responseLocal);

        // await the response
        var awaitMethod = typeof(Task<Core.XmlRpcResponse>).GetMethod("GetAwaiter")!;
        var getResultMethod = typeof(TaskAwaiter<Core.XmlRpcResponse>).GetMethod("GetResult")!;

        il.Emit(OpCodes.Ldloc, responseLocal);
        il.Emit(OpCodes.Callvirt, awaitMethod);
        il.Emit(OpCodes.Callvirt, getResultMethod);
        il.Emit(OpCodes.Stloc, resultLocal);

        // Handle the response
        var returnType = method.ReturnType;

        if (returnType == typeof(void))
        {
            // Just call and ignore result
            il.Emit(OpCodes.Ret);
        }
        else if (typeof(Task).IsAssignableFrom(returnType))
        {
            // Async method
            if (returnType.IsGenericType)
            {
                // Task<T>
                var resultType = returnType.GetGenericArguments()[0];
                ImplementAsyncGenericReturn(il, resultLocal, resultType);
            }
            else
            {
                // Task (non-generic)
                il.Emit(OpCodes.Ldloc, resultLocal);
                il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcResponse).GetMethod("GetValueOrThrow")!);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Call, typeof(Task).GetProperty("CompletedTask")!.GetMethod!);
            }
        }
        else
        {
            // Synchronous return
            ImplementSyncReturn(il, resultLocal, returnType);
        }

        typeBuilder.DefineMethodOverride(methodBuilder, method);
    }

    private static void ImplementAsyncGenericReturn(ILGenerator il, LocalBuilder resultLocal, Type resultType)
    {
        // Get the value or throw
        il.Emit(OpCodes.Ldloc, resultLocal);
        il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcResponse).GetMethod("GetValueOrThrow")!);

        // Convert to result type
        if (resultType == typeof(Core.XmlRpcValue))
        {
            // Return as-is
            il.Emit(OpCodes.Call, typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType)!);
        }
        else if (resultType == typeof(string))
        {
            il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcValue).GetProperty("AsString")!.GetMethod!);
            il.Emit(OpCodes.Call, typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType)!);
        }
        else if (resultType == typeof(int))
        {
            il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcValue).GetProperty("AsInteger")!.GetMethod!);
            il.Emit(OpCodes.Call, typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType)!);
        }
        else if (resultType == typeof(long))
        {
            il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcValue).GetProperty("AsLong")!.GetMethod!);
            il.Emit(OpCodes.Call, typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType)!);
        }
        else if (resultType == typeof(bool))
        {
            il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcValue).GetProperty("AsBoolean")!.GetMethod!);
            il.Emit(OpCodes.Call, typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType)!);
        }
        else if (resultType == typeof(double))
        {
            il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcValue).GetProperty("AsDouble")!.GetMethod!);
            il.Emit(OpCodes.Call, typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType)!);
        }
        else if (resultType == typeof(DateTime))
        {
            il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcValue).GetProperty("AsDateTime")!.GetMethod!);
            il.Emit(OpCodes.Call, typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType)!);
        }
        else if (resultType == typeof(byte[]))
        {
            il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcValue).GetProperty("AsBase64")!.GetMethod!);
            il.Emit(OpCodes.Call, typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType)!);
        }
        else
        {
            // Use ToObject<T>
            var toObjectMethod = typeof(Core.XmlRpcValue).GetMethod("ToObject", Type.EmptyTypes)!
                .MakeGenericMethod(resultType);
            il.Emit(OpCodes.Callvirt, toObjectMethod);
            il.Emit(OpCodes.Call, typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType)!);
        }
    }

    private static void ImplementSyncReturn(ILGenerator il, LocalBuilder resultLocal, Type returnType)
    {
        // Get the value or throw
        il.Emit(OpCodes.Ldloc, resultLocal);
        il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcResponse).GetMethod("GetValueOrThrow")!);

        // Convert to return type
        if (returnType == typeof(Core.XmlRpcValue))
        {
            // Return as-is
            il.Emit(OpCodes.Ret);
        }
        else if (returnType == typeof(string))
        {
            il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcValue).GetProperty("AsString")!.GetMethod!);
            il.Emit(OpCodes.Ret);
        }
        else if (returnType == typeof(int))
        {
            il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcValue).GetProperty("AsInteger")!.GetMethod!);
            il.Emit(OpCodes.Ret);
        }
        else if (returnType == typeof(long))
        {
            il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcValue).GetProperty("AsLong")!.GetMethod!);
            il.Emit(OpCodes.Ret);
        }
        else if (returnType == typeof(bool))
        {
            il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcValue).GetProperty("AsBoolean")!.GetMethod!);
            il.Emit(OpCodes.Ret);
        }
        else if (returnType == typeof(double))
        {
            il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcValue).GetProperty("AsDouble")!.GetMethod!);
            il.Emit(OpCodes.Ret);
        }
        else if (returnType == typeof(DateTime))
        {
            il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcValue).GetProperty("AsDateTime")!.GetMethod!);
            il.Emit(OpCodes.Ret);
        }
        else if (returnType == typeof(byte[]))
        {
            il.Emit(OpCodes.Callvirt, typeof(Core.XmlRpcValue).GetProperty("AsBase64")!.GetMethod!);
            il.Emit(OpCodes.Ret);
        }
        else
        {
            // Use ToObject<T>
            var toObjectMethod = typeof(Core.XmlRpcValue).GetMethod("ToObject", Type.EmptyTypes)!
                .MakeGenericMethod(returnType);
            il.Emit(OpCodes.Callvirt, toObjectMethod);
            il.Emit(OpCodes.Ret);
        }
    }
}
