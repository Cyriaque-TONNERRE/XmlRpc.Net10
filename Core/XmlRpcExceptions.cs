// <summary>
// XmlRpc.Net10 - A modern XML-RPC client library for .NET 10
// Copyright (c) 2026 XmlRpc.Net10 Contributors
// Licensed under the MIT License
// </summary>

using System;
using System.Runtime.Serialization;

namespace XmlRpc.Core;

/// <summary>
///     The exception that is thrown when an XML-RPC fault response is received.
/// </summary>
public class XmlRpcFaultException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the XmlRpcFaultException class.
    /// </summary>
    /// <param name="fault">The fault information.</param>
    public XmlRpcFaultException(XmlRpcFault fault)
        : base($"XML-RPC Fault {fault.FaultCode}: {fault.FaultString}")
    {
        Fault = fault;
        FaultCode = fault.FaultCode;
    }

    /// <summary>
    ///     Initializes a new instance of the XmlRpcFaultException class.
    /// </summary>
    /// <param name="faultCode">The fault code.</param>
    /// <param name="faultString">The fault string.</param>
    public XmlRpcFaultException(int faultCode, string faultString)
        : this(new XmlRpcFault(faultCode, faultString))
    {
    }

    /// <summary>
    ///     Initializes a new instance of the XmlRpcFaultException class with serialized data.
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">The streaming context.</param>
    protected XmlRpcFaultException(SerializationInfo info,
        StreamingContext context)
        : base(info, context)
    {
        FaultCode = info.GetInt32(nameof(FaultCode));
        Fault = new XmlRpcFault(FaultCode, info.GetString("FaultString") ?? string.Empty);
    }

    /// <summary>
    ///     Gets the fault code.
    /// </summary>
    public int FaultCode { get; }

    /// <summary>
    ///     Gets the fault information.
    /// </summary>
    public XmlRpcFault Fault { get; }

    /// <inheritdoc />
    public override void GetObjectData(SerializationInfo info,
        StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(FaultCode), FaultCode);
        info.AddValue("FaultString", Fault.FaultString);
    }
}

/// <summary>
///     The exception that is thrown when there is an error in the XML-RPC serialization or deserialization.
/// </summary>
public class XmlRpcSerializationException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the XmlRpcSerializationException class.
    /// </summary>
    public XmlRpcSerializationException()
        : base("An error occurred during XML-RPC serialization or deserialization.")
    {
    }

    /// <summary>
    ///     Initializes a new instance of the XmlRpcSerializationException class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public XmlRpcSerializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the XmlRpcSerializationException class with a specified error message
    ///     and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public XmlRpcSerializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
///     The exception that is thrown when an XML-RPC HTTP request fails.
/// </summary>
public class XmlRpcHttpException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the XmlRpcHttpException class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="message">The error message.</param>
    public XmlRpcHttpException(int statusCode, string message)
        : base($"HTTP error {statusCode}: {message}")
    {
        StatusCode = statusCode;
    }

    /// <summary>
    ///     Initializes a new instance of the XmlRpcHttpException class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="responseContent">The HTTP response content.</param>
    public XmlRpcHttpException(int statusCode, string message, string? responseContent)
        : base($"HTTP error {statusCode}: {message}")
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }

    /// <summary>
    ///     Initializes a new instance of the XmlRpcHttpException class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public XmlRpcHttpException(int statusCode, string message, Exception innerException)
        : base($"HTTP error {statusCode}: {message}", innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    ///     Gets the HTTP status code.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    ///     Gets the HTTP response content, if available.
    /// </summary>
    public string? ResponseContent { get; }
}

/// <summary>
///     The exception that is thrown when a method is not found on the XML-RPC server.
/// </summary>
public class XmlRpcMethodNotFoundException : XmlRpcFaultException
{
    /// <summary>
    ///     Initializes a new instance of the XmlRpcMethodNotFoundException class.
    /// </summary>
    /// <param name="methodName">The name of the method that was not found.</param>
    public XmlRpcMethodNotFoundException(string methodName)
        : base(-32601, $"Method not found: {methodName}")
    {
        MethodName = methodName;
    }

    /// <summary>
    ///     Gets the name of the method that was not found.
    /// </summary>
    public string MethodName { get; }
}

/// <summary>
///     The exception that is thrown when there are invalid parameters for an XML-RPC method call.
/// </summary>
public class XmlRpcInvalidParamsException : XmlRpcFaultException
{
    /// <summary>
    ///     Initializes a new instance of the XmlRpcInvalidParamsException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public XmlRpcInvalidParamsException(string message)
        : base(-32602, message)
    {
    }
}