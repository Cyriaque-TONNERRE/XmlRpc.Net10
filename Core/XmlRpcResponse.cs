// <summary>
// XmlRpc.Net10 - A modern XML-RPC client library for .NET 10
// Copyright (c) 2026 XmlRpc.Net10 Contributors
// Licensed under the MIT License
// </summary>

using System;

namespace XmlRpc.Core;

/// <summary>
/// Represents an XML-RPC response.
/// </summary>
public class XmlRpcResponse
{
    /// <summary>
    /// Gets the return value of the method call.
    /// </summary>
    public XmlRpcValue? Value { get; }

    /// <summary>
    /// Gets the fault information if the call failed.
    /// </summary>
    public XmlRpcFault? Fault { get; }

    /// <summary>
    /// Gets a value indicating whether the response is a fault.
    /// </summary>
    public bool IsFault => Fault != null;

    /// <summary>
    /// Gets a value indicating whether the response is successful.
    /// </summary>
    public bool IsSuccess => Fault == null;

    /// <summary>
    /// Initializes a new instance of the XmlRpcResponse class with a return value.
    /// </summary>
    /// <param name="value">The return value.</param>
    public XmlRpcResponse(XmlRpcValue value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Fault = null;
    }

    /// <summary>
    /// Initializes a new instance of the XmlRpcResponse class with a fault.
    /// </summary>
    /// <param name="fault">The fault information.</param>
    public XmlRpcResponse(XmlRpcFault fault)
    {
        Value = null;
        Fault = fault ?? throw new ArgumentNullException(nameof(fault));
    }

    /// <summary>
    /// Creates a successful response with the specified value.
    /// </summary>
    /// <param name="value">The return value.</param>
    /// <returns>A new XmlRpcResponse instance.</returns>
    public static XmlRpcResponse Success(XmlRpcValue value) => new(value);

    /// <summary>
    /// Creates a successful response with the specified value.
    /// </summary>
    /// <param name="value">The return value.</param>
    /// <returns>A new XmlRpcResponse instance.</returns>
    public static XmlRpcResponse Success(object? value) => new(XmlRpcValue.FromObject(value));

    /// <summary>
    /// Creates a fault response.
    /// </summary>
    /// <param name="faultCode">The fault code.</param>
    /// <param name="faultString">The fault string.</param>
    /// <returns>A new XmlRpcResponse instance.</returns>
    public static XmlRpcResponse CreateFault(int faultCode, string faultString) =>
        new(new XmlRpcFault(faultCode, faultString));

    /// <summary>
    /// Gets the return value, or throws an exception if the response is a fault.
    /// </summary>
    /// <returns>The return value.</returns>
    /// <exception cref="XmlRpcFaultException">Thrown if the response is a fault.</exception>
    public XmlRpcValue GetValueOrThrow()
    {
        if (IsFault)
        {
            throw new XmlRpcFaultException(Fault!);
        }
        return Value!;
    }

    /// <summary>
    /// Gets the return value converted to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <returns>The converted value.</returns>
    /// <exception cref="XmlRpcFaultException">Thrown if the response is a fault.</exception>
    public T? GetValue<T>() => GetValueOrThrow().ToObject<T>();

    /// <summary>
    /// Attempts to get the return value.
    /// </summary>
    /// <param name="value">The return value if successful.</param>
    /// <returns>True if the response is successful; otherwise false.</returns>
    public bool TryGetValue(out XmlRpcValue? value)
    {
        if (IsFault)
        {
            value = null;
            return false;
        }
        value = Value;
        return true;
    }

    /// <summary>
    /// Attempts to get the return value converted to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="value">The converted value if successful.</param>
    /// <returns>True if the response is successful; otherwise false.</returns>
    public bool TryGetValue<T>(out T? value)
    {
        if (IsFault || Value == null)
        {
            value = default;
            return false;
        }
        try
        {
            value = Value.ToObject<T>();
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    /// <inheritdoc />
    public override string ToString() =>
        IsFault ? $"XmlRpcResponse(Fault: {Fault})" : $"XmlRpcResponse(Value: {Value})";
}
