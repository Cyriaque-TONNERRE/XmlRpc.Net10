// <summary>
// XmlRpc.Net10 - A modern XML-RPC client library for .NET 10
// Copyright (c) 2026 XmlRpc.Net10 Contributors
// Licensed under the MIT License
// </summary>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XmlRpc.Core;

/// <summary>
/// Represents a value in the XML-RPC protocol.
/// This class provides a type-safe wrapper around XML-RPC values with automatic conversion.
/// </summary>
[JsonConverter(typeof(XmlRpcValueJsonConverter))]
public class XmlRpcValue : IEquatable<XmlRpcValue>, IComparable<XmlRpcValue>
{
    private readonly object? _value;
    private readonly XmlRpcType _type;

    /// <summary>
    /// Gets the type of this XML-RPC value.
    /// </summary>
    public XmlRpcType Type => _type;

    /// <summary>
    /// Gets the raw internal value.
    /// </summary>
    public object? RawValue => _value;

    #region Static Factory Methods

    /// <summary>
    /// Creates a nil/null value.
    /// </summary>
    public static XmlRpcValue Nil { get; } = new XmlRpcValue(null, XmlRpcType.Nil);

    /// <summary>
    /// Creates an integer value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>A new XmlRpcValue representing the integer.</returns>
    public static XmlRpcValue FromInt(int value) => new XmlRpcValue(value, XmlRpcType.Integer);

    /// <summary>
    /// Creates a long value.
    /// </summary>
    /// <param name="value">The long value.</param>
    /// <returns>A new XmlRpcValue representing the long.</returns>
    public static XmlRpcValue FromLong(long value) => new XmlRpcValue(value, XmlRpcType.Long);

    /// <summary>
    /// Creates a boolean value.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>A new XmlRpcValue representing the boolean.</returns>
    public static XmlRpcValue FromBoolean(bool value) => new XmlRpcValue(value, XmlRpcType.Boolean);

    /// <summary>
    /// Creates a string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>A new XmlRpcValue representing the string.</returns>
    public static XmlRpcValue FromString(string value) => new XmlRpcValue(value ?? string.Empty, XmlRpcType.String);

    /// <summary>
    /// Creates a double value.
    /// </summary>
    /// <param name="value">The double value.</param>
    /// <returns>A new XmlRpcValue representing the double.</returns>
    public static XmlRpcValue FromDouble(double value) => new XmlRpcValue(value, XmlRpcType.Double);

    /// <summary>
    /// Creates a DateTime value.
    /// </summary>
    /// <param name="value">The DateTime value.</param>
    /// <returns>A new XmlRpcValue representing the DateTime.</returns>
    public static XmlRpcValue FromDateTime(DateTime value) => new XmlRpcValue(value, XmlRpcType.DateTime);

    /// <summary>
    /// Creates a DateTimeOffset value.
    /// </summary>
    /// <param name="value">The DateTimeOffset value.</param>
    /// <returns>A new XmlRpcValue representing the DateTime.</returns>
    public static XmlRpcValue FromDateTime(DateTimeOffset value) => new XmlRpcValue(value.DateTime, XmlRpcType.DateTime);

    /// <summary>
    /// Creates a base64 binary value.
    /// </summary>
    /// <param name="value">The byte array value.</param>
    /// <returns>A new XmlRpcValue representing the binary data.</returns>
    public static XmlRpcValue FromBase64(byte[] value) => new XmlRpcValue(value, XmlRpcType.Base64);

    /// <summary>
    /// Creates a struct value from a dictionary.
    /// </summary>
    /// <param name="value">The dictionary with string keys.</param>
    /// <returns>A new XmlRpcValue representing the struct.</returns>
    public static XmlRpcValue FromStruct(Dictionary<string, XmlRpcValue> value) => new XmlRpcValue(value, XmlRpcType.Struct);

    /// <summary>
    /// Creates an array value.
    /// </summary>
    /// <param name="value">The array of values.</param>
    /// <returns>A new XmlRpcValue representing the array.</returns>
    public static XmlRpcValue FromArray(XmlRpcValue[] value) => new XmlRpcValue(value, XmlRpcType.Array);

    /// <summary>
    /// Creates an array value from an enumerable.
    /// </summary>
    /// <param name="value">The enumerable of values.</param>
    /// <returns>A new XmlRpcValue representing the array.</returns>
    public static XmlRpcValue FromArray(IEnumerable<XmlRpcValue> value) => new XmlRpcValue(value.ToArray(), XmlRpcType.Array);

    /// <summary>
    /// Creates an XmlRpcValue from a .NET object with automatic type detection.
    /// </summary>
    /// <param name="value">The .NET object to convert.</param>
    /// <returns>A new XmlRpcValue representing the object.</returns>
    /// <exception cref="ArgumentException">Thrown when the object type is not supported.</exception>
    public static XmlRpcValue FromObject(object? value, IReadOnlyList<XmlRpcConverter>? converters)
    {
        if (converters != null && value != null)
        {
            var type = value.GetType();
            foreach (var converter in converters)
            {
                if (converter.CanConvert(type))
                    return converter.Write(value, type);
            }
        }
        return FromObject(value);
    }

    public static XmlRpcValue FromObject(object? value) => value switch
    {
        null => Nil,
        XmlRpcValue xmlRpcValue => xmlRpcValue,
        int i => FromInt(i),
        long l => FromLong(l),
        short s => FromInt(s),
        byte b => FromInt(b),
        sbyte sb => FromInt(sb),
        ushort us => FromInt(us),
        uint ui => FromLong(ui),
        ulong ul => FromLong((long)ul),
        bool bo => FromBoolean(bo),
        string s => FromString(s),
        char c => FromString(c.ToString()),
        double d => FromDouble(d),
        float f => FromDouble(f),
        decimal dec => FromDouble((double)dec),
        DateTime dt => FromDateTime(dt),
        DateTimeOffset dto => FromDateTime(dto.DateTime),
        byte[] bytes => FromBase64(bytes),
        IDictionary<string, XmlRpcValue> dict => FromStruct(new Dictionary<string, XmlRpcValue>(dict)),
        IDictionary<string, object> objDict => FromStruct(objDict.ToDictionary(kvp => kvp.Key, kvp => FromObject(kvp.Value))),
        IDictionary dict => FromStruct(dict.Keys.Cast<object>()
            .Where(k => k != null)
            .ToDictionary(k => k.ToString()!, k => FromObject(dict[k]))),
        IEnumerable<XmlRpcValue> values => FromArray(values),
        IEnumerable<object> objects => FromArray(objects.Select(FromObject)),
        IEnumerable enumerable => FromArray(enumerable.Cast<object>().Select(FromObject)),
        _ => throw new ArgumentException($"Unsupported type for XML-RPC conversion: {value.GetType().FullName}", nameof(value))
    };

    #endregion

    #region Constructors

    private XmlRpcValue(object? value, XmlRpcType type)
    {
        _value = value;
        _type = type;
    }

    #endregion

    #region Conversion Properties

    /// <summary>
    /// Gets the value as an integer.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the value is not an integer type.</exception>
    public int AsInteger => _type switch
    {
        XmlRpcType.Integer => (int)_value!,
        XmlRpcType.Long => Convert.ToInt32((long)_value!),
        XmlRpcType.Double => Convert.ToInt32((double)_value!),
        XmlRpcType.Boolean => (bool)_value! ? 1 : 0,
        XmlRpcType.String => int.Parse((string)_value!),
        _ => throw new InvalidOperationException($"Cannot convert {_type} to integer")
    };

    /// <summary>
    /// Gets the value as a long.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the value is not a numeric type.</exception>
    public long AsLong => _type switch
    {
        XmlRpcType.Integer => (int)_value!,
        XmlRpcType.Long => (long)_value!,
        XmlRpcType.Double => Convert.ToInt64((double)_value!),
        XmlRpcType.Boolean => (bool)_value! ? 1 : 0,
        XmlRpcType.String => long.Parse((string)_value!),
        _ => throw new InvalidOperationException($"Cannot convert {_type} to long")
    };

    /// <summary>
    /// Gets the value as a boolean.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the value is not a boolean or numeric type.</exception>
    public bool AsBoolean => _type switch
    {
        XmlRpcType.Boolean => (bool)_value!,
        XmlRpcType.Integer => (int)_value! != 0,
        XmlRpcType.Long => (long)_value! != 0,
        XmlRpcType.Double => (double)_value! != 0,
        XmlRpcType.String => bool.Parse((string)_value!),
        _ => throw new InvalidOperationException($"Cannot convert {_type} to boolean")
    };

    /// <summary>
    /// Gets the value as a string.
    /// </summary>
    public string AsString => _type switch
    {
        XmlRpcType.String => (string)_value!,
        XmlRpcType.Integer => ((int)_value!).ToString(),
        XmlRpcType.Long => ((long)_value!).ToString(),
        XmlRpcType.Double => ((double)_value!).ToString("G17"),
        XmlRpcType.Boolean => (bool)_value! ? "true" : "false",
        XmlRpcType.DateTime => ((DateTime)_value!).ToString("o"),
        XmlRpcType.Base64 => Convert.ToBase64String((byte[])_value!),
        XmlRpcType.Nil => string.Empty,
        _ => _value?.ToString() ?? string.Empty
    };

    /// <summary>
    /// Gets the value as a double.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the value is not a numeric type.</exception>
    public double AsDouble => _type switch
    {
        XmlRpcType.Double => (double)_value!,
        XmlRpcType.Integer => (int)_value!,
        XmlRpcType.Long => (long)_value!,
        XmlRpcType.Boolean => (bool)_value! ? 1.0 : 0.0,
        XmlRpcType.String => double.Parse((string)_value!),
        _ => throw new InvalidOperationException($"Cannot convert {_type} to double")
    };

    /// <summary>
    /// Gets the value as a DateTime.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the value is not a DateTime.</exception>
    public DateTime AsDateTime => _type switch
    {
        XmlRpcType.DateTime => (DateTime)_value!,
        XmlRpcType.String => DateTime.Parse((string)_value!),
        _ => throw new InvalidOperationException($"Cannot convert {_type} to DateTime")
    };

    /// <summary>
    /// Gets the value as a byte array.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the value is not base64 data.</exception>
    public byte[] AsBase64 => _type switch
    {
        XmlRpcType.Base64 => (byte[])_value!,
        XmlRpcType.String => Convert.FromBase64String((string)_value!),
        _ => throw new InvalidOperationException($"Cannot convert {_type} to byte array")
    };

    /// <summary>
    /// Gets the value as a struct (dictionary).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the value is not a struct.</exception>
    public Dictionary<string, XmlRpcValue> AsStruct => _type == XmlRpcType.Struct
        ? (Dictionary<string, XmlRpcValue>)_value!
        : throw new InvalidOperationException($"Cannot convert {_type} to struct");

    /// <summary>
    /// Gets the value as an array.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the value is not an array.</exception>
    public XmlRpcValue[] AsArray => _type == XmlRpcType.Array
        ? (XmlRpcValue[])_value!
        : throw new InvalidOperationException($"Cannot convert {_type} to array");

    /// <summary>
    /// Gets a value indicating whether this value is nil.
    /// </summary>
    public bool IsNil => _type == XmlRpcType.Nil;

    /// <summary>
    /// Gets a struct member by key.
    /// </summary>
    /// <param name="key">The member key.</param>
    /// <returns>The member value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the value is not a struct.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the key is not found.</exception>
    public XmlRpcValue this[string key]
    {
        get
        {
            var dict = AsStruct;
            return dict[key];
        }
    }

    /// <summary>
    /// Gets an array element by index.
    /// </summary>
    /// <param name="index">The element index.</param>
    /// <returns>The element value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the value is not an array.</exception>
    /// <exception cref="IndexOutOfRangeException">Thrown if the index is out of range.</exception>
    public XmlRpcValue this[int index]
    {
        get
        {
            var arr = AsArray;
            return arr[index];
        }
    }

    #endregion

    #region Conversion Operators

    /// <summary>
    /// Implicitly converts an integer to an XmlRpcValue.
    /// </summary>
    public static implicit operator XmlRpcValue(int value) => FromInt(value);

    /// <summary>
    /// Implicitly converts a long to an XmlRpcValue.
    /// </summary>
    public static implicit operator XmlRpcValue(long value) => FromLong(value);

    /// <summary>
    /// Implicitly converts a boolean to an XmlRpcValue.
    /// </summary>
    public static implicit operator XmlRpcValue(bool value) => FromBoolean(value);

    /// <summary>
    /// Implicitly converts a string to an XmlRpcValue.
    /// </summary>
    public static implicit operator XmlRpcValue(string value) => FromString(value);

    /// <summary>
    /// Implicitly converts a double to an XmlRpcValue.
    /// </summary>
    public static implicit operator XmlRpcValue(double value) => FromDouble(value);

    /// <summary>
    /// Implicitly converts a DateTime to an XmlRpcValue.
    /// </summary>
    public static implicit operator XmlRpcValue(DateTime value) => FromDateTime(value);

    /// <summary>
    /// Implicitly converts a byte array to an XmlRpcValue.
    /// </summary>
    public static implicit operator XmlRpcValue(byte[] value) => FromBase64(value);

    /// <summary>
    /// Explicitly converts an XmlRpcValue to an integer.
    /// </summary>
    public static explicit operator int(XmlRpcValue value) => value.AsInteger;

    /// <summary>
    /// Explicitly converts an XmlRpcValue to a long.
    /// </summary>
    public static explicit operator long(XmlRpcValue value) => value.AsLong;

    /// <summary>
    /// Explicitly converts an XmlRpcValue to a boolean.
    /// </summary>
    public static explicit operator bool(XmlRpcValue value) => value.AsBoolean;

    /// <summary>
    /// Explicitly converts an XmlRpcValue to a string.
    /// </summary>
    public static explicit operator string(XmlRpcValue value) => value.AsString;

    /// <summary>
    /// Explicitly converts an XmlRpcValue to a double.
    /// </summary>
    public static explicit operator double(XmlRpcValue value) => value.AsDouble;

    /// <summary>
    /// Explicitly converts an XmlRpcValue to a DateTime.
    /// </summary>
    public static explicit operator DateTime(XmlRpcValue value) => value.AsDateTime;

    #endregion

    #region TryGet Methods

    /// <summary>
    /// Attempts to get the value as an integer.
    /// </summary>
    /// <param name="result">The integer value if successful.</param>
    /// <returns>True if conversion was successful; otherwise false.</returns>
    public bool TryGetInteger(out int result)
    {
        try
        {
            result = AsInteger;
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the value as a long.
    /// </summary>
    /// <param name="result">The long value if successful.</param>
    /// <returns>True if conversion was successful; otherwise false.</returns>
    public bool TryGetLong(out long result)
    {
        try
        {
            result = AsLong;
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the value as a boolean.
    /// </summary>
    /// <param name="result">The boolean value if successful.</param>
    /// <returns>True if conversion was successful; otherwise false.</returns>
    public bool TryGetBoolean(out bool result)
    {
        try
        {
            result = AsBoolean;
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the value as a string.
    /// </summary>
    /// <param name="result">The string value if successful.</param>
    /// <returns>True if conversion was successful; otherwise false.</returns>
    public bool TryGetString(out string result)
    {
        try
        {
            result = AsString;
            return true;
        }
        catch
        {
            result = string.Empty;
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the value as a double.
    /// </summary>
    /// <param name="result">The double value if successful.</param>
    /// <returns>True if conversion was successful; otherwise false.</returns>
    public bool TryGetDouble(out double result)
    {
        try
        {
            result = AsDouble;
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the value as a DateTime.
    /// </summary>
    /// <param name="result">The DateTime value if successful.</param>
    /// <returns>True if conversion was successful; otherwise false.</returns>
    public bool TryGetDateTime(out DateTime result)
    {
        try
        {
            result = AsDateTime;
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the value as a byte array.
    /// </summary>
    /// <param name="result">The byte array if successful.</param>
    /// <returns>True if conversion was successful; otherwise false.</returns>
    public bool TryGetBase64(out byte[] result)
    {
        try
        {
            result = AsBase64;
            return true;
        }
        catch
        {
            result = Array.Empty<byte>();
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the value as a struct.
    /// </summary>
    /// <param name="result">The struct dictionary if successful.</param>
    /// <returns>True if conversion was successful; otherwise false.</returns>
    public bool TryGetStruct(out Dictionary<string, XmlRpcValue> result)
    {
        try
        {
            result = AsStruct;
            return true;
        }
        catch
        {
            result = new Dictionary<string, XmlRpcValue>();
            return false;
        }
    }

    /// <summary>
    /// Attempts to get the value as an array.
    /// </summary>
    /// <param name="result">The array if successful.</param>
    /// <returns>True if conversion was successful; otherwise false.</returns>
    public bool TryGetArray(out XmlRpcValue[] result)
    {
        try
        {
            result = AsArray;
            return true;
        }
        catch
        {
            result = Array.Empty<XmlRpcValue>();
            return false;
        }
    }

    #endregion

    #region IEquatable<XmlRpcValue>

    /// <inheritdoc />
    public bool Equals(XmlRpcValue? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_type != other._type) return false;

        return _type switch
        {
            XmlRpcType.Nil => true,
            XmlRpcType.Integer => (int)_value! == (int)other._value!,
            XmlRpcType.Long => (long)_value! == (long)other._value!,
            XmlRpcType.Boolean => (bool)_value! == (bool)other._value!,
            XmlRpcType.String => (string)_value! == (string)other._value!,
            XmlRpcType.Double => Math.Abs((double)_value! - (double)other._value!) < double.Epsilon,
            XmlRpcType.DateTime => (DateTime)_value! == (DateTime)other._value!,
            XmlRpcType.Base64 => ((byte[])_value!).SequenceEqual((byte[])other._value!),
            XmlRpcType.Struct => StructEquals((Dictionary<string, XmlRpcValue>)_value!, (Dictionary<string, XmlRpcValue>)other._value!),
            XmlRpcType.Array => ((XmlRpcValue[])_value!).SequenceEqual((XmlRpcValue[])other._value!),
            _ => false
        };
    }

    private static bool StructEquals(Dictionary<string, XmlRpcValue> a, Dictionary<string, XmlRpcValue> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var kvp in a)
        {
            if (!b.TryGetValue(kvp.Key, out var otherValue)) return false;
            if (!kvp.Value.Equals(otherValue)) return false;
        }
        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as XmlRpcValue);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_type, _value);

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(XmlRpcValue? left, XmlRpcValue? right) =>
        left?.Equals(right) ?? right is null;

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(XmlRpcValue? left, XmlRpcValue? right) =>
        !(left == right);

    #endregion

    #region IComparable<XmlRpcValue>

    /// <inheritdoc />
    public int CompareTo(XmlRpcValue? other)
    {
        if (other is null) return 1;
        if (_type != other._type)
        {
            return _type.CompareTo(other._type);
        }

        return _type switch
        {
            XmlRpcType.Nil => 0,
            XmlRpcType.Integer => ((int)_value!).CompareTo((int)other._value!),
            XmlRpcType.Long => ((long)_value!).CompareTo((long)other._value!),
            XmlRpcType.Boolean => ((bool)_value!).CompareTo((bool)other._value!),
            XmlRpcType.String => string.Compare((string)_value!, (string)other._value!, StringComparison.Ordinal),
            XmlRpcType.Double => ((double)_value!).CompareTo((double)other._value!),
            XmlRpcType.DateTime => ((DateTime)_value!).CompareTo((DateTime)other._value!),
            XmlRpcType.Base64 => CompareByteArrays((byte[])_value!, (byte[])other._value!),
            XmlRpcType.Struct => ((Dictionary<string, XmlRpcValue>)_value!).Count.CompareTo(((Dictionary<string, XmlRpcValue>)other._value!).Count),
            XmlRpcType.Array => ((XmlRpcValue[])_value!).Length.CompareTo(((XmlRpcValue[])other._value!).Length),
            _ => 0
        };
    }

    private static int CompareByteArrays(byte[] a, byte[] b)
    {
        var minLength = Math.Min(a.Length, b.Length);
        for (var i = 0; i < minLength; i++)
        {
            var comparison = a[i].CompareTo(b[i]);
            if (comparison != 0) return comparison;
        }
        return a.Length.CompareTo(b.Length);
    }

    #endregion

    #region ToString

    /// <inheritdoc />
    public override string ToString()
    {
        return _type switch
        {
            XmlRpcType.Nil => "nil",
            XmlRpcType.Integer => $"int({AsInteger})",
            XmlRpcType.Long => $"long({AsLong})",
            XmlRpcType.Boolean => $"bool({AsBoolean})",
            XmlRpcType.String => $"string(\"{AsString}\")",
            XmlRpcType.Double => $"double({AsDouble})",
            XmlRpcType.DateTime => $"dateTime({AsDateTime:O})",
            XmlRpcType.Base64 => $"base64({AsBase64.Length} bytes)",
            XmlRpcType.Struct => $"struct({AsStruct.Count} members)",
            XmlRpcType.Array => $"array({AsArray.Length} elements)",
            _ => $"unknown({_type})"
        };
    }

    #endregion

    #region ToObject

    /// <summary>
    /// Converts the XmlRpcValue to a .NET object.
    /// </summary>
    /// <returns>The .NET object representation.</returns>
    public object? ToObject()
    {
        return _type switch
        {
            XmlRpcType.Nil => null,
            XmlRpcType.Integer => AsInteger,
            XmlRpcType.Long => AsLong,
            XmlRpcType.Boolean => AsBoolean,
            XmlRpcType.String => AsString,
            XmlRpcType.Double => AsDouble,
            XmlRpcType.DateTime => AsDateTime,
            XmlRpcType.Base64 => AsBase64,
            XmlRpcType.Struct => AsStruct.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToObject()),
            XmlRpcType.Array => AsArray.Select(v => v.ToObject()).ToArray(),
            _ => _value
        };
    }

    /// <summary>
    /// Converts the XmlRpcValue to a specific .NET type.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>The converted value.</returns>
    public T? ToObject<T>(IReadOnlyList<XmlRpcConverter>? converters = null)
        => (T?)ToObject(typeof(T), converters);
    

    /// <summary>
    /// Converts the XmlRpcValue to a specific .NET type.
    /// </summary>
    /// <param name="targetType">The target type.</param>
    /// <param name="converters">Optional list of custom converters to use during conversion.</param>
    /// <returns>The converted value.</returns>
    public object? ToObject(Type targetType, IReadOnlyList<XmlRpcConverter>? converters = null)
    {
        // Search for custom converters first
        if (converters != null)
        {
            foreach (var converter in converters)
            {
                if (converter.CanConvert(targetType))
                    return converter.Read(this, targetType);
            }
        }
        
        if (targetType == typeof(XmlRpcValue))
            return this;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (_type == XmlRpcType.Nil)
            return underlyingType.IsValueType ? null : null;

        if (underlyingType == typeof(int) || underlyingType == typeof(int?))
            return AsInteger;
        if (underlyingType == typeof(long) || underlyingType == typeof(long?))
            return AsLong;
        if (underlyingType == typeof(bool) || underlyingType == typeof(bool?))
            return AsBoolean;
        if (underlyingType == typeof(string))
            return AsString;
        if (underlyingType == typeof(double) || underlyingType == typeof(double?))
            return AsDouble;
        if (underlyingType == typeof(float) || underlyingType == typeof(float?))
            return (float)AsDouble;
        if (underlyingType == typeof(decimal) || underlyingType == typeof(decimal?))
            return (decimal)AsDouble;
        if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTime?))
            return AsDateTime;
        if (underlyingType == typeof(DateTimeOffset) || underlyingType == typeof(DateTimeOffset?))
            return new DateTimeOffset(AsDateTime);
        if (underlyingType == typeof(byte[]))
            return AsBase64;

        // Handle IDictionary<string, object>
        if (underlyingType == typeof(Dictionary<string, object>) ||
            (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
        {
            return AsStruct.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToObject(typeof(object), converters));
        }

        // Handle arrays and lists
        if (underlyingType.IsArray)
        {
            var elementType = underlyingType.GetElementType()!;
            var sourceArray = AsArray;
            var result = Array.CreateInstance(elementType, sourceArray.Length);
            for (var i = 0; i < sourceArray.Length; i++)
            {
                result.SetValue(sourceArray[i].ToObject(elementType, converters), i);
            }
            return result;
        }

        // Handle generic IEnumerable/List
        if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = underlyingType.GetGenericArguments()[0];
            var sourceArray = AsArray;
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (IList)Activator.CreateInstance(listType)!;
            foreach (var item in sourceArray)
            {
                list.Add(item.ToObject(elementType, converters));
            }
            return list;
        }

        // Handle complex objects via struct
        if (_type == XmlRpcType.Struct)
        {
            return ConvertStructToObject(underlyingType, converters);
        }

        throw new InvalidOperationException($"Cannot convert {_type} to {targetType.FullName}");
    }

    private object? ConvertStructToObject(Type targetType, IReadOnlyList<XmlRpcConverter>? converters = null)
    {
        var properties = targetType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var result = Activator.CreateInstance(targetType);

        foreach (var prop in properties)
        {
            if (!prop.CanWrite) continue;

            var name = prop.Name;
            var attr = prop.GetCustomAttributes(typeof(XmlRpcMemberAttribute), false).FirstOrDefault() as XmlRpcMemberAttribute;
            if (attr != null && !string.IsNullOrEmpty(attr.Name))
            {
                name = attr.Name;
            }

            // Try case-insensitive lookup
            var dict = AsStruct;
            var key = dict.Keys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
            if (key != null && dict.TryGetValue(key, out var value))
            {
                prop.SetValue(result, value.ToObject(prop.PropertyType, converters));
            }
        }

        return result;
    }

    #endregion
}

/// <summary>
/// Attribute to specify the XML-RPC member name for a property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class XmlRpcMemberAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the XML-RPC member name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Initializes a new instance of the XmlRpcMemberAttribute class.
    /// </summary>
    /// <param name="name">The XML-RPC member name.</param>
    public XmlRpcMemberAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// JSON converter for XmlRpcValue.
/// </summary>
internal class XmlRpcValueJsonConverter : JsonConverter<XmlRpcValue>
{
    public override XmlRpcValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return XmlRpcValue.Nil;

            case JsonTokenType.True:
                return XmlRpcValue.FromBoolean(true);

            case JsonTokenType.False:
                return XmlRpcValue.FromBoolean(false);

            case JsonTokenType.Number:
                if (reader.TryGetInt32(out var intVal))
                    return XmlRpcValue.FromInt(intVal);
                if (reader.TryGetInt64(out var longVal))
                    return XmlRpcValue.FromLong(longVal);
                if (reader.TryGetDouble(out var doubleVal))
                    return XmlRpcValue.FromDouble(doubleVal);
                throw new JsonException("Invalid number format");

            case JsonTokenType.String:
                var str = reader.GetString()!;
                // Try to parse as DateTime
                if (DateTime.TryParse(str, out var dateVal))
                    return XmlRpcValue.FromDateTime(dateVal);
                // Try to parse as base64
                try
                {
                    var bytes = Convert.FromBase64String(str);
                    return XmlRpcValue.FromBase64(bytes);
                }
                catch { }
                return XmlRpcValue.FromString(str);

            case JsonTokenType.StartArray:
                var values = new List<XmlRpcValue>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    values.Add(Read(ref reader, typeToConvert, options));
                }
                return XmlRpcValue.FromArray(values);

            case JsonTokenType.StartObject:
                var dict = new Dictionary<string, XmlRpcValue>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var key = reader.GetString()!;
                        reader.Read();
                        dict[key] = Read(ref reader, typeToConvert, options);
                    }
                }
                return XmlRpcValue.FromStruct(dict);

            default:
                throw new JsonException($"Unexpected token type: {reader.TokenType}");
        }
    }

    public override void Write(Utf8JsonWriter writer, XmlRpcValue value, JsonSerializerOptions options)
    {
        switch (value.Type)
        {
            case XmlRpcType.Nil:
                writer.WriteNullValue();
                break;

            case XmlRpcType.Integer:
                writer.WriteNumberValue(value.AsInteger);
                break;

            case XmlRpcType.Long:
                writer.WriteNumberValue(value.AsLong);
                break;

            case XmlRpcType.Boolean:
                writer.WriteBooleanValue(value.AsBoolean);
                break;

            case XmlRpcType.String:
                writer.WriteStringValue(value.AsString);
                break;

            case XmlRpcType.Double:
                writer.WriteNumberValue(value.AsDouble);
                break;

            case XmlRpcType.DateTime:
                writer.WriteStringValue(value.AsDateTime.ToString("O"));
                break;

            case XmlRpcType.Base64:
                writer.WriteStringValue(Convert.ToBase64String(value.AsBase64));
                break;

            case XmlRpcType.Struct:
                writer.WriteStartObject();
                foreach (var kvp in value.AsStruct)
                {
                    writer.WritePropertyName(kvp.Key);
                    Write(writer, kvp.Value, options);
                }
                writer.WriteEndObject();
                break;

            case XmlRpcType.Array:
                writer.WriteStartArray();
                foreach (var item in value.AsArray)
                {
                    Write(writer, item, options);
                }
                writer.WriteEndArray();
                break;

            default:
                throw new JsonException($"Unknown XmlRpcType: {value.Type}");
        }
    }
}
