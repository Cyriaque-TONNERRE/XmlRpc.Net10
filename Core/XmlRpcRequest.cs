using System;
using System.Collections.Generic;
using System.Linq;

namespace XmlRpc.Core;

/// <summary>
///     Represents an XML-RPC request.
/// </summary>
public class XmlRpcRequest
{
    /// <summary>
    ///     Initializes a new instance of the XmlRpcRequest class.
    /// </summary>
    public XmlRpcRequest()
    {
        MethodName = string.Empty;
        Parameters = Array.Empty<XmlRpcValue>();
    }

    /// <summary>
    ///     Initializes a new instance of the XmlRpcRequest class with the specified method name.
    /// </summary>
    /// <param name="methodName">The name of the method to call.</param>
    public XmlRpcRequest(string methodName)
    {
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        Parameters = Array.Empty<XmlRpcValue>();
    }

    /// <summary>
    ///     Initializes a new instance of the XmlRpcRequest class with the specified method name and parameters.
    /// </summary>
    /// <param name="methodName">The name of the method to call.</param>
    /// <param name="parameters">The parameters for the method call.</param>
    public XmlRpcRequest(string methodName, params XmlRpcValue[] parameters)
    {
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        Parameters = parameters ?? Array.Empty<XmlRpcValue>();
    }

    /// <summary>
    ///     Initializes a new instance of the XmlRpcRequest class with the specified method name and object parameters.
    /// </summary>
    /// <param name="methodName">The name of the method to call.</param>
    /// <param name="parameters">The parameters for the method call.</param>
    public XmlRpcRequest(string methodName, params object?[] parameters)
    {
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        Parameters = parameters?.Select(XmlRpcValue.FromObject).ToArray() ?? Array.Empty<XmlRpcValue>();
    }

    /// <summary>
    ///     Gets or sets the name of the method to call.
    /// </summary>
    public string MethodName { get; set; }

    /// <summary>
    ///     Gets or sets the parameters for the method call.
    /// </summary>
    public XmlRpcValue[] Parameters { get; set; }

    /// <summary>
    ///     Gets the number of parameters.
    /// </summary>
    public int ParameterCount => Parameters.Length;

    /// <summary>
    ///     Gets a parameter by index.
    /// </summary>
    /// <param name="index">The parameter index.</param>
    /// <returns>The parameter value.</returns>
    public XmlRpcValue this[int index] => Parameters[index];

    /// <summary>
    ///     Creates a new request with the specified method name.
    /// </summary>
    /// <param name="methodName">The name of the method to call.</param>
    /// <returns>A new XmlRpcRequest instance.</returns>
    public static XmlRpcRequest Create(string methodName)
    {
        return new XmlRpcRequest(methodName);
    }

    /// <summary>
    ///     Creates a new request with the specified method name and parameters.
    /// </summary>
    /// <param name="methodName">The name of the method to call.</param>
    /// <param name="parameters">The parameters for the method call.</param>
    /// <returns>A new XmlRpcRequest instance.</returns>
    public static XmlRpcRequest Create(string methodName, params object?[] parameters)
    {
        return new XmlRpcRequest(methodName, parameters);
    }

    /// <summary>
    ///     Adds a parameter to the request.
    /// </summary>
    /// <param name="parameter">The parameter to add.</param>
    /// <returns>This request instance for method chaining.</returns>
    public XmlRpcRequest AddParameter(XmlRpcValue parameter)
    {
        Parameters = Parameters.Append(parameter).ToArray();
        return this;
    }

    /// <summary>
    ///     Adds a parameter to the request.
    /// </summary>
    /// <param name="parameter">The parameter to add.</param>
    /// <returns>This request instance for method chaining.</returns>
    public XmlRpcRequest AddParameter(object? parameter)
    {
        Parameters = Parameters.Append(XmlRpcValue.FromObject(parameter)).ToArray();
        return this;
    }

    /// <summary>
    ///     Adds multiple parameters to the request.
    /// </summary>
    /// <param name="parameters">The parameters to add.</param>
    /// <returns>This request instance for method chaining.</returns>
    public XmlRpcRequest AddParameters(IEnumerable<object?> parameters)
    {
        Parameters = Parameters.Concat(parameters.Select(XmlRpcValue.FromObject)).ToArray();
        return this;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"XmlRpcRequest({MethodName}, {Parameters.Length} params)";
    }
}