using System;
using System.Collections.Generic;
using System.Linq;

namespace XmlRpc.Core;

/// <summary>
///     Represents an XML-RPC fault response.
/// </summary>
public class XmlRpcFault : IEquatable<XmlRpcFault>
{
    /// <summary>
    ///     Initializes a new instance of the XmlRpcFault class.
    /// </summary>
    /// <param name="faultCode">The fault code.</param>
    /// <param name="faultString">The fault string.</param>
    public XmlRpcFault(int faultCode, string faultString)
    {
        FaultCode = faultCode;
        FaultString = faultString ?? throw new ArgumentNullException(nameof(faultString));
    }

    /// <summary>
    ///     Gets the fault code.
    /// </summary>
    public int FaultCode { get; }

    /// <summary>
    ///     Gets the fault string (error message).
    /// </summary>
    public string FaultString { get; }

    /// <inheritdoc />
    public bool Equals(XmlRpcFault? other)
    {
        if (other is null) return false;
        return FaultCode == other.FaultCode && FaultString == other.FaultString;
    }

    /// <summary>
    ///     Creates a fault from an XmlRpcValue struct.
    /// </summary>
    /// <param name="value">The XmlRpcValue containing the fault struct.</param>
    /// <returns>An XmlRpcFault instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the value is not a valid fault struct.</exception>
    public static XmlRpcFault FromValue(XmlRpcValue value)
    {
        if (value.Type != XmlRpcType.Struct) throw new ArgumentException("Fault value must be a struct", nameof(value));

        var dict = value.AsStruct;

        var faultCode = 0;
        var faultString = "Unknown error";

        // Try to get faultCode (case-insensitive)
        var faultCodeKey = dict.Keys.FirstOrDefault(k =>
            string.Equals(k, "faultCode", StringComparison.OrdinalIgnoreCase));
        if (faultCodeKey != null) faultCode = dict[faultCodeKey].AsInteger;

        // Try to get faultString (case-insensitive)
        var faultStringKey = dict.Keys.FirstOrDefault(k =>
            string.Equals(k, "faultString", StringComparison.OrdinalIgnoreCase));
        if (faultStringKey != null) faultString = dict[faultStringKey].AsString;

        return new XmlRpcFault(faultCode, faultString);
    }

    /// <summary>
    ///     Converts the fault to an XmlRpcValue struct.
    /// </summary>
    /// <returns>An XmlRpcValue representing the fault.</returns>
    public XmlRpcValue ToValue()
    {
        return XmlRpcValue.FromStruct(new Dictionary<string, XmlRpcValue>
        {
            ["faultCode"] = XmlRpcValue.FromInt(FaultCode),
            ["faultString"] = XmlRpcValue.FromString(FaultString)
        });
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as XmlRpcFault);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(FaultCode, FaultString);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Fault {FaultCode}: {FaultString}";
    }

    /// <summary>
    ///     Equality operator.
    /// </summary>
    public static bool operator ==(XmlRpcFault? left, XmlRpcFault? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    /// <summary>
    ///     Inequality operator.
    /// </summary>
    public static bool operator !=(XmlRpcFault? left, XmlRpcFault? right)
    {
        return !(left == right);
    }
}