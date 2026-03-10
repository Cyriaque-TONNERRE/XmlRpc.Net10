using System;

namespace XmlRpc.Core;

/// <summary>
/// Base class for custom XML-RPC converters.
/// Inspired by System.Text.Json's JsonConverter pattern.
/// </summary>
public abstract class XmlRpcConverter
{
    /// <summary>Returns true if this converter can handle the given type.</summary>
    public abstract bool CanConvert(Type typeToConvert);

    /// <summary>Converts an XmlRpcValue to the target .NET type.</summary>
    public abstract object? Read(XmlRpcValue value, Type typeToConvert);
}

/// <summary>
/// A converter factory for handling open generic types (e.g. XenRef&lt;&gt;, List&lt;&gt;).
/// Mirrors the JsonConverterFactory pattern from System.Text.Json.
/// </summary>
public abstract class XmlRpcConverterFactory : XmlRpcConverter
{
    /// <summary>
    /// Creates a concrete converter for the specific closed generic type.
    /// E.g.: given XenRef&lt;VM&gt;, returns a XenRefConverter&lt;VM&gt;.
    /// </summary>
    public abstract XmlRpcConverter CreateConverter(Type typeToConvert);
    
    /// <summary>
    /// The factory delegates Read() to the concrete converter it creates.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="typeToConvert"></param>
    /// <returns></returns>
    public sealed override object? Read(XmlRpcValue value, Type typeToConvert)
        => CreateConverter(typeToConvert).Read(value, typeToConvert);
}