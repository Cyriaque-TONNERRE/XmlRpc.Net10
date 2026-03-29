using System;
using System.Collections.Generic;
using XmlRpc.Core;
using Xunit;

namespace XmlRpc.Tests;

/// <summary>
///     Unit tests for XmlRpcValue.
/// </summary>
public class XmlRpcValueTests
{
    #region Factory Method Tests

    [Fact]
    public void FromInt_ShouldCreateIntegerValue()
    {
        var value = XmlRpcValue.FromInt(42);

        Assert.Equal(XmlRpcType.Integer, value.Type);
        Assert.Equal(42, value.AsInteger);
    }

    [Fact]
    public void FromLong_ShouldCreateLongValue()
    {
        var value = XmlRpcValue.FromLong(9876543210L);

        Assert.Equal(XmlRpcType.Long, value.Type);
        Assert.Equal(9876543210L, value.AsLong);
    }

    [Fact]
    public void FromBoolean_ShouldCreateBooleanValue()
    {
        var trueValue = XmlRpcValue.FromBoolean(true);
        var falseValue = XmlRpcValue.FromBoolean(false);

        Assert.Equal(XmlRpcType.Boolean, trueValue.Type);
        Assert.True(trueValue.AsBoolean);

        Assert.Equal(XmlRpcType.Boolean, falseValue.Type);
        Assert.False(falseValue.AsBoolean);
    }

    [Fact]
    public void FromString_ShouldCreateStringValue()
    {
        var value = XmlRpcValue.FromString("Hello, World!");

        Assert.Equal(XmlRpcType.String, value.Type);
        Assert.Equal("Hello, World!", value.AsString);
    }

    [Fact]
    public void FromDouble_ShouldCreateDoubleValue()
    {
        var value = XmlRpcValue.FromDouble(3.14159);

        Assert.Equal(XmlRpcType.Double, value.Type);
        Assert.InRange(value.AsDouble, 3.14158, 3.14160);
    }

    [Fact]
    public void FromDateTime_ShouldCreateDateTimeValue()
    {
        var now = DateTime.UtcNow;
        var value = XmlRpcValue.FromDateTime(now);

        Assert.Equal(XmlRpcType.DateTime, value.Type);
        Assert.Equal(now, value.AsDateTime);
    }

    [Fact]
    public void FromBase64_ShouldCreateBase64Value()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var value = XmlRpcValue.FromBase64(bytes);

        Assert.Equal(XmlRpcType.Base64, value.Type);
        Assert.Equal(bytes, value.AsBase64);
    }

    [Fact]
    public void FromStruct_ShouldCreateStructValue()
    {
        var dict = new Dictionary<string, XmlRpcValue>
        {
            ["name"] = XmlRpcValue.FromString("John"),
            ["age"] = XmlRpcValue.FromInt(30)
        };
        var value = XmlRpcValue.FromStruct(dict);

        Assert.Equal(XmlRpcType.Struct, value.Type);
        Assert.True(value.AsStruct.ContainsKey("name"));
        Assert.Equal("John", value.AsStruct["name"].AsString);
        Assert.Equal(30, value.AsStruct["age"].AsInteger);
    }

    [Fact]
    public void FromArray_ShouldCreateArrayValue()
    {
        var arr = new[]
        {
            XmlRpcValue.FromInt(1),
            XmlRpcValue.FromInt(2),
            XmlRpcValue.FromInt(3)
        };
        var value = XmlRpcValue.FromArray(arr);

        Assert.Equal(XmlRpcType.Array, value.Type);
        Assert.Equal(3, value.AsArray.Length);
        Assert.Equal(1, value.AsArray[0].AsInteger);
        Assert.Equal(2, value.AsArray[1].AsInteger);
        Assert.Equal(3, value.AsArray[2].AsInteger);
    }

    [Fact]
    public void Nil_ShouldCreateNilValue()
    {
        Assert.Equal(XmlRpcType.Nil, XmlRpcValue.Nil.Type);
        Assert.True(XmlRpcValue.Nil.IsNil);
    }

    #endregion

    #region FromObject Tests

    [Fact]
    public void FromObject_WithNull_ShouldReturnNil()
    {
        var value = XmlRpcValue.FromObject(null);
        Assert.Equal(XmlRpcType.Nil, value.Type);
    }

    [Fact]
    public void FromObject_WithInt_ShouldReturnInteger()
    {
        var value = XmlRpcValue.FromObject(42);
        Assert.Equal(XmlRpcType.Integer, value.Type);
        Assert.Equal(42, value.AsInteger);
    }

    [Fact]
    public void FromObject_WithString_ShouldReturnString()
    {
        var value = XmlRpcValue.FromObject("test");
        Assert.Equal(XmlRpcType.String, value.Type);
        Assert.Equal("test", value.AsString);
    }

    [Fact]
    public void FromObject_WithBool_ShouldReturnBoolean()
    {
        var value = XmlRpcValue.FromObject(true);
        Assert.Equal(XmlRpcType.Boolean, value.Type);
        Assert.True(value.AsBoolean);
    }

    [Fact]
    public void FromObject_WithDouble_ShouldReturnDouble()
    {
        var value = XmlRpcValue.FromObject(3.14);
        Assert.Equal(XmlRpcType.Double, value.Type);
        Assert.InRange(value.AsDouble, 3.139, 3.141);
    }

    [Fact]
    public void FromObject_WithDateTime_ShouldReturnDateTime()
    {
        var now = DateTime.Now;
        var value = XmlRpcValue.FromObject(now);
        Assert.Equal(XmlRpcType.DateTime, value.Type);
        Assert.Equal(now, value.AsDateTime);
    }

    [Fact]
    public void FromObject_WithByteArray_ShouldReturnBase64()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var value = XmlRpcValue.FromObject(bytes);
        Assert.Equal(XmlRpcType.Base64, value.Type);
        Assert.Equal(bytes, value.AsBase64);
    }

    [Fact]
    public void FromObject_WithStringObjectDictionary_ShouldReturnStruct()
    {
        var dict = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };
        var value = XmlRpcValue.FromObject(dict);

        Assert.Equal(XmlRpcType.Struct, value.Type);
        Assert.Equal("value1", value.AsStruct["key1"].AsString);
        Assert.Equal(42, value.AsStruct["key2"].AsInteger);
    }

    [Fact]
    public void FromObject_WithObjectArray_ShouldReturnArray()
    {
        var arr = new object[] { 1, "two", true };
        var value = XmlRpcValue.FromObject(arr);

        Assert.Equal(XmlRpcType.Array, value.Type);
        Assert.Equal(3, value.AsArray.Length);
        Assert.Equal(1, value.AsArray[0].AsInteger);
        Assert.Equal("two", value.AsArray[1].AsString);
        Assert.True(value.AsArray[2].AsBoolean);
    }

    #endregion

    #region Conversion Tests

    [Fact]
    public void AsInteger_WithLong_ShouldConvertToInt()
    {
        var value = XmlRpcValue.FromLong(42L);
        Assert.Equal(42, value.AsInteger);
    }

    [Fact]
    public void AsString_WithInteger_ShouldConvertToString()
    {
        var value = XmlRpcValue.FromInt(42);
        Assert.Equal("42", value.AsString);
    }

    [Fact]
    public void AsBoolean_WithNonZeroInteger_ShouldReturnTrue()
    {
        var value = XmlRpcValue.FromInt(1);
        Assert.True(value.AsBoolean);
    }

    [Fact]
    public void AsBoolean_WithZeroInteger_ShouldReturnFalse()
    {
        var value = XmlRpcValue.FromInt(0);
        Assert.False(value.AsBoolean);
    }

    #endregion

    #region Indexer Tests

    [Fact]
    public void Indexer_WithStringKey_ShouldReturnStructMember()
    {
        var value = XmlRpcValue.FromStruct(new Dictionary<string, XmlRpcValue>
        {
            ["name"] = XmlRpcValue.FromString("John")
        });

        Assert.Equal("John", value["name"].AsString);
    }

    [Fact]
    public void Indexer_WithIntIndex_ShouldReturnArrayElement()
    {
        var value = XmlRpcValue.FromArray(new[]
        {
            XmlRpcValue.FromInt(1),
            XmlRpcValue.FromInt(2)
        });

        Assert.Equal(1, value[0].AsInteger);
        Assert.Equal(2, value[1].AsInteger);
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitConversion_FromInt_ShouldWork()
    {
        XmlRpcValue value = 42;
        Assert.Equal(XmlRpcType.Integer, value.Type);
        Assert.Equal(42, value.AsInteger);
    }

    [Fact]
    public void ImplicitConversion_FromString_ShouldWork()
    {
        XmlRpcValue value = "test";
        Assert.Equal(XmlRpcType.String, value.Type);
        Assert.Equal("test", value.AsString);
    }

    [Fact]
    public void ImplicitConversion_FromBool_ShouldWork()
    {
        XmlRpcValue value = true;
        Assert.Equal(XmlRpcType.Boolean, value.Type);
        Assert.True(value.AsBoolean);
    }

    [Fact]
    public void ImplicitConversion_FromDouble_ShouldWork()
    {
        XmlRpcValue value = 3.14;
        Assert.Equal(XmlRpcType.Double, value.Type);
        Assert.InRange(value.AsDouble, 3.139, 3.141);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        var value1 = XmlRpcValue.FromInt(42);
        var value2 = XmlRpcValue.FromInt(42);

        Assert.True(value1.Equals(value2));
        Assert.True(value1 == value2);
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        var value1 = XmlRpcValue.FromInt(42);
        var value2 = XmlRpcValue.FromInt(43);

        Assert.False(value1.Equals(value2));
        Assert.True(value1 != value2);
    }

    [Fact]
    public void Equals_WithSameStructs_ShouldReturnTrue()
    {
        var struct1 = XmlRpcValue.FromStruct(new Dictionary<string, XmlRpcValue>
        {
            ["a"] = XmlRpcValue.FromInt(1),
            ["b"] = XmlRpcValue.FromString("test")
        });

        var struct2 = XmlRpcValue.FromStruct(new Dictionary<string, XmlRpcValue>
        {
            ["a"] = XmlRpcValue.FromInt(1),
            ["b"] = XmlRpcValue.FromString("test")
        });

        Assert.True(struct1.Equals(struct2));
    }

    #endregion

    #region TryGet Tests

    [Fact]
    public void TryGetInteger_WithInteger_ShouldReturnTrue()
    {
        var value = XmlRpcValue.FromInt(42);
        Assert.True(value.TryGetInteger(out var result));
        Assert.Equal(42, result);
    }

    [Fact]
    public void TryGetInteger_WithString_ShouldReturnFalse()
    {
        var value = XmlRpcValue.FromString("not a number");
        Assert.False(value.TryGetInteger(out _));
    }

    [Fact]
    public void TryGetStruct_WithStruct_ShouldReturnTrue()
    {
        var value = XmlRpcValue.FromStruct(new Dictionary<string, XmlRpcValue>());
        Assert.True(value.TryGetStruct(out var result));
        Assert.NotNull(result);
    }

    #endregion

    #region ToObject Tests

    [Fact]
    public void ToObject_WithInteger_ShouldReturnInt()
    {
        var value = XmlRpcValue.FromInt(42);
        var result = value.ToObject();
        Assert.Equal(42, result);
    }

    [Fact]
    public void ToObject_Generic_WithInteger_ShouldReturnInt()
    {
        var value = XmlRpcValue.FromInt(42);
        var result = value.ToObject<int>();
        Assert.Equal(42, result);
    }

    [Fact]
    public void ToObject_WithArray_ShouldReturnObjectArray()
    {
        var value = XmlRpcValue.FromArray(new[]
        {
            XmlRpcValue.FromInt(1),
            XmlRpcValue.FromInt(2)
        });

        var result = value.ToObject();
        Assert.IsType<object[]>(result);
        var arr = (object[])result!;
        Assert.Equal(2, arr.Length);
    }

    [Fact]
    public void ToObject_WithStruct_ShouldReturnDictionary()
    {
        var value = XmlRpcValue.FromStruct(new Dictionary<string, XmlRpcValue>
        {
            ["key"] = XmlRpcValue.FromString("value")
        });

        var result = value.ToObject();
        Assert.IsAssignableFrom<Dictionary<string, object?>>(result);
        var dict = (Dictionary<string, object?>)result!;
        Assert.Equal("value", dict["key"]);
    }

    #endregion
}