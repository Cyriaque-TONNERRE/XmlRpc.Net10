using System;
using System.Collections.Generic;
using XmlRpc.Core;
using XmlRpc.Serialization;
using Xunit;

namespace XmlRpc.Tests;

/// <summary>
///     Unit tests for XmlRpcSerializer.
/// </summary>
public class XmlRpcSerializerTests
{
    private readonly XmlRpcSerializer _serializer = new();

    #region Request Serialization Tests

    [Fact]
    public void SerializeRequest_WithNoParams_ShouldProduceValidXml()
    {
        var request = new XmlRpcRequest("system.listMethods");

        var xml = _serializer.SerializeRequest(request);

        Assert.Contains("<methodName>system.listMethods</methodName>", xml);
        Assert.DoesNotContain("<params>", xml);
    }

    [Fact]
    public void SerializeRequest_WithParams_ShouldIncludeParams()
    {
        var request = new XmlRpcRequest("test.method", 42, "hello", true);

        var xml = _serializer.SerializeRequest(request);

        Assert.Contains("<methodName>test.method</methodName>", xml);
        Assert.Contains("<params>", xml);
        Assert.Contains("<int>42</int>", xml);
        Assert.Contains("<string>hello</string>", xml);
        Assert.Contains("<boolean>1</boolean>", xml);
    }

    [Fact]
    public void SerializeRequest_WithStruct_ShouldIncludeStruct()
    {
        var request = new XmlRpcRequest("test.method",
            XmlRpcValue.FromStruct(new Dictionary<string, XmlRpcValue>
            {
                ["name"] = XmlRpcValue.FromString("John"),
                ["age"] = XmlRpcValue.FromInt(30)
            }));

        var xml = _serializer.SerializeRequest(request);

        Assert.Contains("<struct>", xml);
        Assert.Contains("<name>name</name>", xml);
        Assert.Contains("<name>age</name>", xml);
        Assert.Contains("<string>John</string>", xml);
        Assert.Contains("<int>30</int>", xml);
    }

    [Fact]
    public void SerializeRequest_WithArray_ShouldIncludeArray()
    {
        var request = new XmlRpcRequest("test.method",
            XmlRpcValue.FromArray(new[] { XmlRpcValue.FromInt(1), XmlRpcValue.FromInt(2) }));

        var xml = _serializer.SerializeRequest(request);

        Assert.Contains("<array>", xml);
        Assert.Contains("<data>", xml);
        Assert.Contains("<int>1</int>", xml);
        Assert.Contains("<int>2</int>", xml);
    }

    [Fact]
    public void SerializeRequest_WithDateTime_ShouldFormatCorrectly()
    {
        var dateTime = new DateTime(2024, 6, 15, 14, 30, 45, DateTimeKind.Utc);
        var request = new XmlRpcRequest("test.method", dateTime);

        var xml = _serializer.SerializeRequest(request);

        Assert.Contains("<dateTime.iso8601>", xml);
        Assert.Contains("20240615T14:30:45", xml);
    }

    [Fact]
    public void SerializeRequest_WithBase64_ShouldEncodeCorrectly()
    {
        var bytes = new byte[] { 72, 101, 108, 108, 111 }; // "Hello"
        var request = new XmlRpcRequest("test.method", bytes);

        var xml = _serializer.SerializeRequest(request);

        Assert.Contains("<base64>", xml);
        Assert.Contains("SGVsbG8=", xml); // Base64 of "Hello"
    }

    #endregion

    #region Request Deserialization Tests

    [Fact]
    public void DeserializeRequest_WithNoParams_ShouldReturnRequest()
    {
        var xml = @"<?xml version=""1.0""?>
<methodCall>
    <methodName>test.method</methodName>
</methodCall>";

        var request = _serializer.DeserializeRequest(xml);

        Assert.Equal("test.method", request.MethodName);
        Assert.Empty(request.Parameters);
    }

    [Fact]
    public void DeserializeRequest_WithParams_ShouldParseParams()
    {
        var xml = @"<?xml version=""1.0""?>
<methodCall>
    <methodName>test.method</methodName>
    <params>
        <param><value><int>42</int></value></param>
        <param><value><string>hello</string></value></param>
    </params>
</methodCall>";

        var request = _serializer.DeserializeRequest(xml);

        Assert.Equal("test.method", request.MethodName);
        Assert.Equal(2, request.Parameters.Length);
        Assert.Equal(42, request.Parameters[0].AsInteger);
        Assert.Equal("hello", request.Parameters[1].AsString);
    }

    [Fact]
    public void DeserializeRequest_WithStruct_ShouldParseStruct()
    {
        var xml = @"<?xml version=""1.0""?>
<methodCall>
    <methodName>test.method</methodName>
    <params>
        <param><value>
            <struct>
                <member>
                    <name>key</name>
                    <value><string>value</string></value>
                </member>
            </struct>
        </value></param>
    </params>
</methodCall>";

        var request = _serializer.DeserializeRequest(xml);

        Assert.Single(request.Parameters);
        Assert.Equal(XmlRpcType.Struct, request.Parameters[0].Type);
        Assert.Equal("value", request.Parameters[0].AsStruct["key"].AsString);
    }

    [Fact]
    public void DeserializeRequest_WithArray_ShouldParseArray()
    {
        var xml = @"<?xml version=""1.0""?>
<methodCall>
    <methodName>test.method</methodName>
    <params>
        <param><value>
            <array>
                <data>
                    <value><int>1</int></value>
                    <value><int>2</int></value>
                    <value><int>3</int></value>
                </data>
            </array>
        </value></param>
    </params>
</methodCall>";

        var request = _serializer.DeserializeRequest(xml);

        Assert.Single(request.Parameters);
        Assert.Equal(XmlRpcType.Array, request.Parameters[0].Type);
        Assert.Equal(3, request.Parameters[0].AsArray.Length);
    }

    #endregion

    #region Response Serialization Tests

    [Fact]
    public void SerializeResponse_WithValue_ShouldProduceValidXml()
    {
        var response = XmlRpcResponse.Success("Hello");

        var xml = _serializer.SerializeResponse(response);

        Assert.Contains("<methodResponse>", xml);
        Assert.Contains("<params>", xml);
        Assert.Contains("<string>Hello</string>", xml);
    }

    [Fact]
    public void SerializeResponse_WithFault_ShouldProduceFaultXml()
    {
        var response = XmlRpcResponse.CreateFault(404, "Not found");

        var xml = _serializer.SerializeResponse(response);

        Assert.Contains("<methodResponse>", xml);
        Assert.Contains("<fault>", xml);
        Assert.Contains("faultCode", xml);
        Assert.Contains("404", xml);
        Assert.Contains("faultString", xml);
        Assert.Contains("Not found", xml);
    }

    #endregion

    #region Response Deserialization Tests

    [Fact]
    public void DeserializeResponse_WithValue_ShouldParseValue()
    {
        var xml = @"<?xml version=""1.0""?>
<methodResponse>
    <params>
        <param><value><string>Hello</string></value></param>
    </params>
</methodResponse>";

        var response = _serializer.DeserializeResponse(xml);

        Assert.True(response.IsSuccess);
        Assert.Equal("Hello", response.Value!.AsString);
    }

    [Fact]
    public void DeserializeResponse_WithFault_ShouldParseFault()
    {
        var xml = @"<?xml version=""1.0""?>
<methodResponse>
    <fault>
        <value>
            <struct>
                <member>
                    <name>faultCode</name>
                    <value><int>404</int></value>
                </member>
                <member>
                    <name>faultString</name>
                    <value><string>Not found</string></value>
                </member>
            </struct>
        </value>
    </fault>
</methodResponse>";

        var response = _serializer.DeserializeResponse(xml);

        Assert.True(response.IsFault);
        Assert.Equal(404, response.Fault!.FaultCode);
        Assert.Equal("Not found", response.Fault.FaultString);
    }

    #endregion

    #region Round-trip Tests

    [Fact]
    public void RoundTrip_Request_ShouldPreserveData()
    {
        var originalRequest = new XmlRpcRequest("test.method",
            42,
            "hello",
            true,
            3.14,
            new DateTime(2024, 6, 15, 14, 30, 45, DateTimeKind.Utc),
            new byte[] { 1, 2, 3 });

        var xml = _serializer.SerializeRequest(originalRequest);
        var deserializedRequest = _serializer.DeserializeRequest(xml);

        Assert.Equal(originalRequest.MethodName, deserializedRequest.MethodName);
        Assert.Equal(originalRequest.Parameters.Length, deserializedRequest.Parameters.Length);
        Assert.Equal(42, deserializedRequest.Parameters[0].AsInteger);
        Assert.Equal("hello", deserializedRequest.Parameters[1].AsString);
        Assert.True(deserializedRequest.Parameters[2].AsBoolean);
    }

    [Fact]
    public void RoundTrip_Response_ShouldPreserveData()
    {
        var originalResponse = XmlRpcResponse.Success(XmlRpcValue.FromStruct(new Dictionary<string, XmlRpcValue>
        {
            ["name"] = XmlRpcValue.FromString("Test"),
            ["value"] = XmlRpcValue.FromInt(123)
        }));

        var xml = _serializer.SerializeResponse(originalResponse);
        var deserializedResponse = _serializer.DeserializeResponse(xml);

        Assert.True(deserializedResponse.IsSuccess);
        Assert.Equal("Test", deserializedResponse.Value!.AsStruct["name"].AsString);
        Assert.Equal(123, deserializedResponse.Value.AsStruct["value"].AsInteger);
    }

    #endregion

    #region Type Element Tests

    [Fact]
    public void Deserialize_WithI4Element_ShouldParseAsInteger()
    {
        var xml = @"<?xml version=""1.0""?>
<methodCall>
    <methodName>test</methodName>
    <params>
        <param><value><i4>42</i4></value></param>
    </params>
</methodCall>";

        var request = _serializer.DeserializeRequest(xml);
        Assert.Equal(42, request.Parameters[0].AsInteger);
    }

    [Fact]
    public void Deserialize_WithNilElement_ShouldParseAsNil()
    {
        var xml = @"<?xml version=""1.0""?>
<methodCall>
    <methodName>test</methodName>
    <params>
        <param><value><nil/></value></param>
    </params>
</methodCall>";

        var request = _serializer.DeserializeRequest(xml);
        Assert.True(request.Parameters[0].IsNil);
    }

    [Fact]
    public void Deserialize_WithImplicitString_ShouldParseAsString()
    {
        // XML-RPC spec allows string values without the <string> element
        var xml = @"<?xml version=""1.0""?>
<methodCall>
    <methodName>test</methodName>
    <params>
        <param><value>implicit string</value></param>
    </params>
</methodCall>";

        var request = _serializer.DeserializeRequest(xml);
        Assert.Equal("implicit string", request.Parameters[0].AsString);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void DeserializeRequest_WithInvalidXml_ShouldThrow()
    {
        var xml = "not valid xml";

        Assert.Throws<XmlRpcSerializationException>(() => _serializer.DeserializeRequest(xml));
    }

    [Fact]
    public void DeserializeRequest_WithMissingMethodName_ShouldThrow()
    {
        var xml = @"<?xml version=""1.0""?>
<methodCall>
</methodCall>";

        Assert.Throws<XmlRpcSerializationException>(() => _serializer.DeserializeRequest(xml));
    }

    #endregion
}