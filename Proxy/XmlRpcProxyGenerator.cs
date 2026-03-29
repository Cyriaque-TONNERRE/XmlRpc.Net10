using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using XmlRpc.Client;
using XmlRpc.Core;

namespace XmlRpc.Proxy;

/// <summary>
///     Generates dynamic proxy classes for XML-RPC interfaces using <see cref="DispatchProxy"/>.
///     
///     Unlike the previous IL-emit implementation, this version is truly asynchronous:
///     methods returning <see cref="Task"/> or <see cref="Task{T}"/> will not block
///     the calling thread while waiting for the HTTP response.
/// </summary>
public static class XmlRpcProxyGenerator
{
    /// <summary>
    ///     Creates a proxy instance for the specified interface type.
    /// </summary>
    /// <typeparam name="T">The interface type.</typeparam>
    /// <param name="client">The XML-RPC client to use.</param>
    /// <returns>A proxy instance implementing the interface.</returns>
    public static T CreateProxy<T>(IXmlRpcClient client) where T : class
    {
        var interfaceType = typeof(T);
        if (!interfaceType.IsInterface)
            throw new ArgumentException($"Type {interfaceType.Name} must be an interface.", nameof(T));

        return XmlRpcDispatchProxy.Create<T>(client);
    }

    /// <summary>
    ///     Creates a proxy instance for the specified interface type using the given server URL.
    /// </summary>
    /// <typeparam name="T">The interface type.</typeparam>
    /// <param name="serverUrl">The XML-RPC server URL.</param>
    /// <returns>A proxy instance implementing the interface.</returns>
    public static T CreateProxy<T>(string serverUrl) where T : class
    {
        var client = new XmlRpcClient(serverUrl);
        return CreateProxy<T>(client);
    }
}

/// <summary>
///     DispatchProxy-based implementation that provides true async support.
///     
///     Each interface method call is intercepted and dispatched as an XML-RPC
///     request via the underlying <see cref="IXmlRpcClient"/>. Methods returning
///     <see cref="Task{T}"/> properly propagate the async chain from
///     <see cref="System.Net.Http.HttpClient.PostAsync"/> without blocking.
/// </summary>
public class XmlRpcDispatchProxy : DispatchProxy
{
    private IXmlRpcClient _client = null!;
    private string? _methodPrefix;

    /// <summary>
    ///     Cache for the open generic method <see cref="InvokeTypedAsync{T}"/>,
    ///     used to avoid repeated reflection lookups.
    /// </summary>
    private static readonly MethodInfo InvokeTypedAsyncMethod =
        typeof(XmlRpcDispatchProxy).GetMethod(
            nameof(InvokeTypedAsync),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>
    ///     Creates a proxy of type <typeparamref name="T"/> backed by the given client.
    /// </summary>
    internal static T Create<T>(IXmlRpcClient client) where T : class
    {
        // DispatchProxy.Create<TInterface, TProxy>() returns an instance of a
        // runtime-generated class that implements TInterface and extends TProxy.
        var proxy = Create<T, XmlRpcDispatchProxy>();
        var dispatchProxy = (XmlRpcDispatchProxy)(object)proxy;
        dispatchProxy._client = client;

        var interfaceType = typeof(T);
        var serviceAttr = interfaceType.GetCustomAttribute<XmlRpcServiceAttribute>();
        dispatchProxy._methodPrefix = serviceAttr?.MethodPrefix;
        if (serviceAttr?.UseInterfaceNameAsPrefix == true && string.IsNullOrEmpty(dispatchProxy._methodPrefix))
            dispatchProxy._methodPrefix = interfaceType.Name;

        return proxy;
    }

    /// <inheritdoc />
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null)
            throw new ArgumentNullException(nameof(targetMethod));

        // Resolve the XML-RPC method name
        var methodAttr = targetMethod.GetCustomAttribute<XmlRpcMethodAttribute>();
        var methodName = methodAttr?.MethodName ?? targetMethod.Name;
        if (!string.IsNullOrEmpty(_methodPrefix))
            methodName = $"{_methodPrefix}{methodName}";

        var returnType = targetMethod.ReturnType;

        // Task (void async) — e.g. VM.start, Session.logout
        if (returnType == typeof(Task))
            return InvokeVoidAsync(methodName, args);

        // Task<T> (async with result) — e.g. VM.get_name_label, Session.login_with_password
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var genericMethod = InvokeTypedAsyncMethod.MakeGenericMethod(resultType);
            return genericMethod.Invoke(this, [methodName, args]);
        }

        // Synchronous return (non-Task) — fallback, should not occur in XAPI interfaces
        return InvokeSync(methodName, args, returnType);
    }

    /// <summary>
    ///     Handles methods returning <see cref="Task"/> (void async).
    ///     The returned Task completes when the XML-RPC response is received.
    /// </summary>
    private async Task InvokeVoidAsync(string methodName, object?[]? args)
    {
        var response = await _client
            .InvokeAsync(methodName, args, CancellationToken.None)
            .ConfigureAwait(false);

        response.GetValueOrThrow();
    }

    /// <summary>
    ///     Handles methods returning <see cref="Task{T}"/> (async with typed result).
    ///     The returned Task completes when the XML-RPC response is received and converted.
    /// </summary>
    private async Task<T> InvokeTypedAsync<T>(string methodName, object?[]? args)
    {
        var response = await _client
            .InvokeAsync(methodName, args, CancellationToken.None)
            .ConfigureAwait(false);

        var value = response.GetValueOrThrow();
        return (T)_client.ConvertValue(value, typeof(T))!;
    }

    /// <summary>
    ///     Fallback for synchronous return types.
    ///     This blocks the thread and should not be used in normal XAPI interfaces.
    /// </summary>
    private object? InvokeSync(string methodName, object?[]? args, Type returnType)
    {
        var response = _client
            .InvokeAsync(methodName, args, CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        if (returnType == typeof(void))
        {
            response.GetValueOrThrow();
            return null;
        }

        var value = response.GetValueOrThrow();
        return _client.ConvertValue(value, returnType);
    }
}
