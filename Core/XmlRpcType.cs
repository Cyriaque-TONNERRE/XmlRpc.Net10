// <summary>
// XmlRpc.Net10 - A modern XML-RPC client library for .NET 10
// Copyright (c) 2026 XmlRpc.Net10 Contributors
// Licensed under the MIT License
// </summary>

namespace XmlRpc.Core;

/// <summary>
/// Represents the type of an XML-RPC value.
/// </summary>
public enum XmlRpcType
{
    /// <summary>
    /// 32-bit signed integer (i4 or int)
    /// </summary>
    Integer,

    /// <summary>
    /// 64-bit signed integer (i8 or ex:i8)
    /// </summary>
    Long,

    /// <summary>
    /// Boolean value (0 or 1)
    /// </summary>
    Boolean,

    /// <summary>
    /// String value
    /// </summary>
    String,

    /// <summary>
    /// Double-precision floating point
    /// </summary>
    Double,

    /// <summary>
    /// Date and time in ISO 8601 format
    /// </summary>
    DateTime,

    /// <summary>
    /// Base64 encoded binary data
    /// </summary>
    Base64,

    /// <summary>
    /// Struct (key-value dictionary)
    /// </summary>
    Struct,

    /// <summary>
    /// Array of values
    /// </summary>
    Array,

    /// <summary>
    /// Null/nil value (extension)
    /// </summary>
    Nil
}
