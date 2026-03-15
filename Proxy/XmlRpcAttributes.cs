// <summary>
// XmlRpc.Net10 - A modern XML-RPC client library for .NET 10
// Copyright (c) 2026 XmlRpc.Net10 Contributors
// Licensed under the MIT License
// </summary>

using System;

namespace XmlRpc.Proxy;

/// <summary>
///     Attribute to mark an interface as an XML-RPC service.
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class XmlRpcServiceAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the base method name prefix.
    /// </summary>
    public string? MethodPrefix { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to use the interface name as prefix.
    /// </summary>
    public bool UseInterfaceNameAsPrefix { get; set; } = false;
}

/// <summary>
///     Attribute to mark a method as an XML-RPC method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class XmlRpcMethodAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the XmlRpcMethodAttribute class.
    /// </summary>
    public XmlRpcMethodAttribute()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the XmlRpcMethodAttribute class with a specific method name.
    /// </summary>
    /// <param name="methodName">The name of the XML-RPC method.</param>
    public XmlRpcMethodAttribute(string methodName)
    {
        MethodName = methodName;
    }

    /// <summary>
    ///     Gets the name of the XML-RPC method.
    /// </summary>
    public string? MethodName { get; }

    /// <summary>
    ///     Gets or sets a value indicating whether the method returns a struct that should be
    ///     deserialized as the return type.
    /// </summary>
    public bool ReturnAsStruct { get; set; } = true;
}

/// <summary>
///     Attribute to specify the parameter name in an XML-RPC struct parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class XmlRpcParameterAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the XmlRpcParameterAttribute class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    public XmlRpcParameterAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    ///     Gets the name of the parameter in the XML-RPC struct.
    /// </summary>
    public string Name { get; }
}

/// <summary>
///     Attribute to indicate that a parameter should be passed as a struct.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class XmlRpcStructParameterAttribute : Attribute
{
}

/// <summary>
///     Attribute to mark a property as part of an XML-RPC struct.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class XmlRpcStructMemberAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the member name. If null, the property name is used.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this member is required.
    /// </summary>
    public bool Required { get; set; } = false;
}

/// <summary>
///     Attribute to ignore a property in XML-RPC serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class XmlRpcIgnoreAttribute : Attribute
{
}