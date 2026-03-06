// <summary>
// XmlRpc.Net10 - A modern XML-RPC client library for .NET 10
// Copyright (c) 2026 XmlRpc.Net10 Contributors
// Licensed under the MIT License
// </summary>

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using XmlRpc.Client;
using XmlRpc.Core;
using Xunit;

namespace XmlRpc.Tests;

/// <summary>
/// Unit tests for XmlRpcClient.
/// </summary>
public class XmlRpcClientTests : IDisposable
{
    private readonly HttpMessageHandlerMock _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly XmlRpcClient _client;

    public XmlRpcClientTests()
    {
        _httpHandlerMock = new HttpMessageHandlerMock();
        _httpClient = new HttpClient(_httpHandlerMock);
        _client = new XmlRpcClient("http://example.com/xmlrpc", _httpClient);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _client.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidUrl_ShouldInitializeClient()
    {
        var client = new XmlRpcClient("http://example.com/xmlrpc");

        Assert.Equal(new Uri("http://example.com/xmlrpc"), client.ServerUri);
    }

    [Fact]
    public void Constructor_WithValidUri_ShouldInitializeClient()
    {
        var uri = new Uri("http://example.com/xmlrpc");
        var client = new XmlRpcClient(uri);

        Assert.Equal(uri, client.ServerUri);
    }

    [Fact]
    public void Constructor_WithNullUrl_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new XmlRpcClient((string)null!));
    }

    #endregion

    #region InvokeAsync Tests

    [Fact]
    public async Task InvokeAsync_WithSuccessfulResponse_ShouldReturnResult()
    {
        // Arrange
        var responseXml = @"<?xml version=""1.0""?>
<methodResponse>
    <params>
        <param><value><string>Success</string></value></param>
    </params>
</methodResponse>";

        _httpHandlerMock.SetupResponse(responseXml, HttpStatusCode.OK);

        // Act
        var response = await _client.InvokeAsync("test.method");

        // Assert
        Assert.True(response.IsSuccess);
        Assert.Equal("Success", response.Value!.AsString);
    }

    [Fact]
    public async Task InvokeAsync_WithFaultResponse_ShouldReturnFault()
    {
        // Arrange
        var responseXml = @"<?xml version=""1.0""?>
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

        _httpHandlerMock.SetupResponse(responseXml, HttpStatusCode.OK);

        // Act
        var response = await _client.InvokeAsync("test.method");

        // Assert
        Assert.True(response.IsFault);
        Assert.Equal(404, response.Fault!.FaultCode);
        Assert.Equal("Not found", response.Fault.FaultString);
    }

    [Fact]
    public async Task InvokeAsync_WithHttpError_ShouldThrow()
    {
        // Arrange
        _httpHandlerMock.SetupResponse("Internal Server Error", HttpStatusCode.InternalServerError);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<XmlRpcHttpException>(() => _client.InvokeAsync("test.method"));
        Assert.Equal(500, ex.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithParameters_ShouldSendCorrectRequest()
    {
        // Arrange
        string? requestBody = null;
        _httpHandlerMock.SetupResponseCallback(@"<?xml version=""1.0""?>
<methodResponse>
    <params>
        <param><value><string>OK</string></value></param>
    </params>
</methodResponse>", HttpStatusCode.OK, content => requestBody = content);

        // Act
        await _client.InvokeAsync("test.method", new object?[] { 42, "hello" });

        // Assert
        Assert.NotNull(requestBody);
        Assert.Contains("<methodName>test.method</methodName>", requestBody);
        Assert.Contains("<int>42</int>", requestBody);
        Assert.Contains("<string>hello</string>", requestBody);
    }

    #endregion

    #region InvokeAndGetResultAsync Tests

    [Fact]
    public async Task InvokeAndGetResultAsync_WithSuccess_ShouldReturnValue()
    {
        // Arrange
        var responseXml = @"<?xml version=""1.0""?>
<methodResponse>
    <params>
        <param><value><int>42</int></value></param>
    </params>
</methodResponse>";

        _httpHandlerMock.SetupResponse(responseXml, HttpStatusCode.OK);

        // Act
        var result = await _client.InvokeAndGetResultAsync("test.method");

        // Assert
        Assert.Equal(42, result.AsInteger);
    }

    [Fact]
    public async Task InvokeAndGetResultAsync_WithFault_ShouldThrow()
    {
        // Arrange
        var responseXml = @"<?xml version=""1.0""?>
<methodResponse>
    <fault>
        <value>
            <struct>
                <member>
                    <name>faultCode</name>
                    <value><int>500</int></value>
                </member>
                <member>
                    <name>faultString</name>
                    <value><string>Error</string></value>
                </member>
            </struct>
        </value>
    </fault>
</methodResponse>";

        _httpHandlerMock.SetupResponse(responseXml, HttpStatusCode.OK);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<XmlRpcFaultException>(() => _client.InvokeAndGetResultAsync("test.method"));
        Assert.Equal(500, ex.FaultCode);
    }

    #endregion

    #region InvokeAsync<T> Tests

    [Fact]
    public async Task InvokeAsync_Generic_WithInt_ShouldReturnTypedResult()
    {
        // Arrange
        var responseXml = @"<?xml version=""1.0""?>
<methodResponse>
    <params>
        <param><value><int>42</int></value></param>
    </params>
</methodResponse>";

        _httpHandlerMock.SetupResponse(responseXml, HttpStatusCode.OK);

        // Act
        var result = await _client.InvokeAsync<int>("test.method");

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task InvokeAsync_Generic_WithString_ShouldReturnTypedResult()
    {
        // Arrange
        var responseXml = @"<?xml version=""1.0""?>
<methodResponse>
    <params>
        <param><value><string>Hello</string></value></param>
    </params>
</methodResponse>";

        _httpHandlerMock.SetupResponse(responseXml, HttpStatusCode.OK);

        // Act
        var result = await _client.InvokeAsync<string>("test.method");

        // Assert
        Assert.Equal("Hello", result);
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task RequestSending_ShouldBeRaised()
    {
        // Arrange
        _httpHandlerMock.SetupResponse(@"<?xml version=""1.0""?>
<methodResponse>
    <params>
        <param><value><string>OK</string></value></param>
    </params>
</methodResponse>", HttpStatusCode.OK);

        var eventRaised = false;
        _client.RequestSending += (sender, args) =>
        {
            eventRaised = true;
            Assert.Equal("test.method", args.Request.MethodName);
            Assert.False(string.IsNullOrEmpty(args.XmlContent));
        };

        // Act
        await _client.InvokeAsync("test.method");

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public async Task ResponseReceived_ShouldBeRaised()
    {
        // Arrange
        _httpHandlerMock.SetupResponse(@"<?xml version=""1.0""?>
<methodResponse>
    <params>
        <param><value><string>OK</string></value></param>
    </params>
</methodResponse>", HttpStatusCode.OK);

        var eventRaised = false;
        _client.ResponseReceived += (sender, args) =>
        {
            eventRaised = true;
            Assert.True(args.Response.IsSuccess);
            Assert.True(args.Duration > TimeSpan.Zero);
        };

        // Act
        await _client.InvokeAsync("test.method");

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public async Task ErrorOccurred_ShouldBeRaisedOnHttpError()
    {
        // Arrange
        _httpHandlerMock.SetupResponse("Error", HttpStatusCode.InternalServerError);

        var eventRaised = false;
        _client.ErrorOccurred += (sender, args) =>
        {
            eventRaised = true;
            Assert.IsType<XmlRpcHttpException>(args.Exception);
        };

        // Act
        try
        {
            await _client.InvokeAsync("test.method");
        }
        catch { }

        // Assert
        Assert.True(eventRaised);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void UserAgent_ShouldBeSettable()
    {
        _client.UserAgent = "MyApp/1.0";
        Assert.Equal("MyApp/1.0", _client.UserAgent);
    }

    [Fact]
    public void Timeout_ShouldBeSettable()
    {
        var timeout = TimeSpan.FromSeconds(30);
        _client.Timeout = timeout;
        Assert.Equal(timeout, _client.Timeout);
    }

    [Fact]
    public void Headers_ShouldBeAddable()
    {
        _client.Headers["X-Custom"] = "value";
        Assert.True(_client.Headers.ContainsKey("X-Custom"));
        Assert.Equal("value", _client.Headers["X-Custom"]);
    }

    #endregion
}

/// <summary>
/// Mock for HttpMessageHandler.
/// </summary>
internal class HttpMessageHandlerMock : HttpMessageHandler
{
    private string? _responseContent;
    private HttpStatusCode _statusCode;
    private Action<string>? _callback;

    public void SetupResponse(string content, HttpStatusCode statusCode)
    {
        _responseContent = content;
        _statusCode = statusCode;
        _callback = null;
    }

    public void SetupResponseCallback(string content, HttpStatusCode statusCode, Action<string> callback)
    {
        _responseContent = content;
        _statusCode = statusCode;
        _callback = callback;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_callback != null && request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync(cancellationToken);
            _callback(content);
        }

        return new HttpResponseMessage
        {
            StatusCode = _statusCode,
            Content = new StringContent(_responseContent ?? string.Empty)
        };
    }
}
