// <summary>
// XmlRpc.Net10 - A modern XML-RPC client library for .NET 10
// Copyright (c) 2026 XmlRpc.Net10 Contributors
// Licensed under the MIT License
// </summary>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using XmlRpc.Core;

namespace XmlRpc.Serialization;

/// <summary>
///     Provides functionality for serializing and deserializing XML-RPC messages.
/// </summary>
public class XmlRpcSerializer
{
    private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);

    private readonly List<XmlRpcConverter> _converters = new();

    /// <summary>
    ///     Gets or sets a value indicating whether to use extended types (i8, nil, etc.).
    /// </summary>
    public bool UseExtendedTypes { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether to indent the XML output.
    /// </summary>
    public bool Indent { get; set; } = false;

    /// <summary>
    ///     Read-only view of registered converters, passed to ToObject calls.
    /// </summary>
    internal IReadOnlyList<XmlRpcConverter> Converters => _converters;

    /// <summary>
    ///     Registers a custom converter.
    /// </summary>
    public XmlRpcSerializer AddConverter(XmlRpcConverter converter)
    {
        _converters.Add(converter);
        return this;
    }

    /// <summary>
    ///     Serializes an XML-RPC request to a string.
    /// </summary>
    /// <param name="request">The request to serialize.</param>
    /// <returns>The XML string.</returns>
    public string SerializeRequest(XmlRpcRequest request)
    {
        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("methodCall",
                new XElement("methodName", request.MethodName),
                SerializeParameters(request.Parameters)
            )
        );

        return xml.Declaration + Environment.NewLine + xml.ToString(SaveOptions.DisableFormatting);
    }

    /// <summary>
    ///     Serializes an XML-RPC request to a stream.
    /// </summary>
    /// <param name="request">The request to serialize.</param>
    /// <param name="stream">The stream to write to.</param>
    public void SerializeRequest(XmlRpcRequest request, Stream stream)
    {
        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("methodCall",
                new XElement("methodName", request.MethodName),
                SerializeParameters(request.Parameters)
            )
        );

        var settings = new XmlWriterSettings
        {
            Encoding = Utf8Encoding,
            Indent = Indent,
            OmitXmlDeclaration = false
        };

        using var writer = XmlWriter.Create(stream, settings);
        xml.WriteTo(writer);
    }

    /// <summary>
    ///     Serializes an XML-RPC response to a string.
    /// </summary>
    /// <param name="response">The response to serialize.</param>
    /// <returns>The XML string.</returns>
    public string SerializeResponse(XmlRpcResponse response)
    {
        XElement innerElement;
        if (response.IsFault)
            innerElement = new XElement("fault",
                new XElement("value", SerializeValue(response.Fault!.ToValue()))
            );
        else
            innerElement = new XElement("params",
                new XElement("param",
                    new XElement("value", SerializeValue(response.Value!))
                )
            );

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("methodResponse", innerElement)
        );

        return xml.Declaration + Environment.NewLine + xml.ToString(SaveOptions.DisableFormatting);
    }

    /// <summary>
    ///     Deserializes an XML-RPC request from a string.
    /// </summary>
    /// <param name="xml">The XML string.</param>
    /// <returns>The deserialized request.</returns>
    /// <exception cref="XmlRpcSerializationException">Thrown if the XML is invalid.</exception>
    public XmlRpcRequest DeserializeRequest(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var methodCall = doc.Element("methodCall")
                             ?? throw new XmlRpcSerializationException("Missing methodCall element");

            var methodName = methodCall.Element("methodName")?.Value
                             ?? throw new XmlRpcSerializationException("Missing methodName element");

            var paramsElement = methodCall.Element("params");
            var parameters = paramsElement != null
                ? DeserializeParameters(paramsElement)
                : Array.Empty<XmlRpcValue>();

            return new XmlRpcRequest(methodName, parameters);
        }
        catch (XmlRpcSerializationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new XmlRpcSerializationException("Failed to deserialize XML-RPC request", ex);
        }
    }

    /// <summary>
    ///     Deserializes an XML-RPC request from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The deserialized request.</returns>
    public XmlRpcRequest DeserializeRequest(Stream stream)
    {
        using var reader = new StreamReader(stream, Utf8Encoding, false);
        return DeserializeRequest(reader.ReadToEnd());
    }

    /// <summary>
    ///     Deserializes an XML-RPC response from a string.
    /// </summary>
    /// <param name="xml">The XML string.</param>
    /// <returns>The deserialized response.</returns>
    /// <exception cref="XmlRpcSerializationException">Thrown if the XML is invalid.</exception>
    public XmlRpcResponse DeserializeResponse(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var methodResponse = doc.Element("methodResponse")
                                 ?? throw new XmlRpcSerializationException("Missing methodResponse element");

            // Check for fault
            var faultElement = methodResponse.Element("fault");
            if (faultElement != null)
            {
                var faultValue = DeserializeValue(faultElement.Element("value")!);
                var fault = XmlRpcFault.FromValue(faultValue);
                return new XmlRpcResponse(fault);
            }

            // Parse normal response
            var paramsElement = methodResponse.Element("params")
                                ?? throw new XmlRpcSerializationException("Missing params element in response");

            var paramElement = paramsElement.Element("param")
                               ?? throw new XmlRpcSerializationException("Missing param element in response");

            var valueElement = paramElement.Element("value")
                               ?? throw new XmlRpcSerializationException("Missing value element in response");

            var value = DeserializeValue(valueElement);
            return new XmlRpcResponse(value);
        }
        catch (XmlRpcSerializationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new XmlRpcSerializationException("Failed to deserialize XML-RPC response", ex);
        }
    }

    /// <summary>
    ///     Deserializes an XML-RPC response from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The deserialized response.</returns>
    public XmlRpcResponse DeserializeResponse(Stream stream)
    {
        using var reader = new StreamReader(stream, Utf8Encoding, false);
        return DeserializeResponse(reader.ReadToEnd());
    }

    #region Private Serialization Methods

    private XElement? SerializeParameters(XmlRpcValue[] parameters)
    {
        if (parameters.Length == 0)
            return null;

        return new XElement("params",
            parameters.Select(p => new XElement("param",
                new XElement("value", SerializeValue(p))
            ))
        );
    }

    private XElement SerializeValue(XmlRpcValue value)
    {
        return value.Type switch
        {
            XmlRpcType.Nil => new XElement(UseExtendedTypes ? "nil" : "string", string.Empty),
            XmlRpcType.Integer => new XElement("int", value.AsInteger.ToString()),
            XmlRpcType.Long => new XElement(UseExtendedTypes ? "i8" : "string", value.AsLong.ToString()),
            XmlRpcType.Boolean => new XElement("boolean", value.AsBoolean ? "1" : "0"),
            XmlRpcType.String => new XElement("string", value.AsString),
            XmlRpcType.Double => new XElement("double", value.AsDouble.ToString("G17")),
            XmlRpcType.DateTime => SerializeDateTime(value.AsDateTime),
            XmlRpcType.Base64 => new XElement("base64", Convert.ToBase64String(value.AsBase64)),
            XmlRpcType.Struct => SerializeStruct(value.AsStruct),
            XmlRpcType.Array => SerializeArray(value.AsArray),
            _ => throw new XmlRpcSerializationException($"Unknown value type: {value.Type}")
        };
    }

    private XElement SerializeDateTime(DateTime dateTime)
    {
        // Format as ISO 8601
        var formatted = dateTime.ToString("yyyyMMdd'T'HH':'mm':'ss");
        return new XElement("dateTime.iso8601", formatted);
    }

    private XElement SerializeStruct(Dictionary<string, XmlRpcValue> dict)
    {
        return new XElement("struct",
            dict.Select(kvp => new XElement("member",
                new XElement("name", kvp.Key),
                new XElement("value", SerializeValue(kvp.Value))
            ))
        );
    }

    private XElement SerializeArray(XmlRpcValue[] array)
    {
        return new XElement("array",
            new XElement("data",
                array.Select(v => new XElement("value", SerializeValue(v)))
            )
        );
    }

    #endregion

    #region Private Deserialization Methods

    private XmlRpcValue[] DeserializeParameters(XElement paramsElement)
    {
        return paramsElement.Elements("param")
            .Select(p => DeserializeValue(p.Element("value")!))
            .ToArray();
    }

    private XmlRpcValue DeserializeValue(XElement valueElement)
    {
        // Check if there's a type element
        var typeElement = valueElement.FirstNode as XElement;

        if (typeElement == null)
            // No type element means string value (XML-RPC spec allows this)
            return XmlRpcValue.FromString(valueElement.Value);

        return typeElement.Name.LocalName switch
        {
            "int" or "i4" => XmlRpcValue.FromInt(int.Parse(typeElement.Value)),
            "i8" or "ex:i8" => XmlRpcValue.FromLong(long.Parse(typeElement.Value)),
            "boolean" => XmlRpcValue.FromBoolean(typeElement.Value == "1"),
            "string" => XmlRpcValue.FromString(typeElement.Value),
            "double" => XmlRpcValue.FromDouble(double.Parse(typeElement.Value)),
            "dateTime.iso8601" => DeserializeDateTime(typeElement.Value),
            "base64" => XmlRpcValue.FromBase64(Convert.FromBase64String(typeElement.Value)),
            "nil" or "ex:nil" => XmlRpcValue.Nil,
            "struct" => DeserializeStruct(typeElement),
            "array" => DeserializeArray(typeElement),
            _ => XmlRpcValue.FromString(typeElement.Value) // Unknown type, treat as string
        };
    }

    private XmlRpcValue DeserializeDateTime(string value)
    {
        // Try various ISO 8601 formats
        // Standard format: 19980717T14:08:55
        // Extended format: 1998-07-17T14:08:55

        string[] formats =
        [
            "yyyyMMdd'T'HH':'mm':'ss'Z'", // XAPI: 20250518T08:39:07Z
            "yyyyMMdd'T'HH':'mm':'ss" // Standard: 19980717T14:08:55
        ];

        if (DateTime.TryParseExact(value, formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal |
                DateTimeStyles.AssumeUniversal,
                out var dateTime))
            return XmlRpcValue.FromDateTime(dateTime);

        if (DateTime.TryParse(value, out dateTime)) return XmlRpcValue.FromDateTime(dateTime);

        throw new XmlRpcSerializationException($"Invalid dateTime format: {value}");
    }

    private XmlRpcValue DeserializeStruct(XElement structElement)
    {
        var dict = new Dictionary<string, XmlRpcValue>();

        foreach (var member in structElement.Elements("member"))
        {
            var name = member.Element("name")?.Value;
            if (name == null) throw new XmlRpcSerializationException("Struct member missing name element");

            var valueElement = member.Element("value");
            if (valueElement == null)
                throw new XmlRpcSerializationException($"Struct member '{name}' missing value element");

            dict[name] = DeserializeValue(valueElement);
        }

        return XmlRpcValue.FromStruct(dict);
    }

    private XmlRpcValue DeserializeArray(XElement arrayElement)
    {
        var dataElement = arrayElement.Element("data")
                          ?? throw new XmlRpcSerializationException("Array missing data element");

        var values = dataElement.Elements("value")
            .Select(DeserializeValue)
            .ToArray();

        return XmlRpcValue.FromArray(values);
    }

    #endregion
}