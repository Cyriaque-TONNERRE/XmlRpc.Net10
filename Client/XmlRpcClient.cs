// <summary>
// XmlRpc.Net10 - A modern XML-RPC client library for .NET 10
// Copyright (c) 2026 XmlRpc.Net10 Contributors
// Licensed under the MIT License
// </summary>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XmlRpc.Core;
using XmlRpc.Proxy;
using XmlRpc.Serialization;

namespace XmlRpc.Client;

/// <summary>
/// A modern XML-RPC client for .NET 10.
/// </summary>
public class XmlRpcClient : IXmlRpcClient
{
    private readonly HttpClient _httpClient;
    private readonly XmlRpcSerializer _serializer;
    private readonly Uri _serverUri;
    private bool _disposed;

    /// <summary>
    /// Gets the server URI.
    /// </summary>
    public Uri ServerUri => _serverUri;

    /// <summary>
    /// Gets or sets the timeout for HTTP requests.
    /// </summary>
    public TimeSpan Timeout
    {
        get => _httpClient.Timeout;
        set => _httpClient.Timeout = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use extended types (i8, nil, etc.).
    /// </summary>
    public bool UseExtendedTypes
    {
        get => _serializer.UseExtendedTypes;
        set => _serializer.UseExtendedTypes = value;
    }

    /// <summary>
    /// Gets the headers that will be sent with each request.
    /// </summary>
    public Dictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets or sets the user agent string.
    /// </summary>
    public string? UserAgent
    {
        get => _httpClient.DefaultRequestHeaders.UserAgent.FirstOrDefault()?.ToString();
        set
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            if (!string.IsNullOrEmpty(value))
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(value);
            }
        }
    }

    /// <summary>
    /// Occurs when a request is about to be sent.
    /// </summary>
    public event EventHandler<XmlRpcRequestEventArgs>? RequestSending;

    /// <summary>
    /// Occurs when a response is received.
    /// </summary>
    public event EventHandler<XmlRpcResponseEventArgs>? ResponseReceived;

    /// <summary>
    /// Occurs when an error occurs.
    /// </summary>
    public event EventHandler<XmlRpcErrorEventArgs>? ErrorOccurred;

    /// <summary>
    /// Initializes a new instance of the XmlRpcClient class.
    /// </summary>
    /// <param name="serverUrl">The URL of the XML-RPC server.</param>
    public XmlRpcClient(string serverUrl)
        : this(new Uri(serverUrl), new HttpClient())
    {
    }

    /// <summary>
    /// Initializes a new instance of the XmlRpcClient class.
    /// </summary>
    /// <param name="serverUri">The URI of the XML-RPC server.</param>
    public XmlRpcClient(Uri serverUri)
        : this(serverUri, new HttpClient())
    {
    }

    /// <summary>
    /// Initializes a new instance of the XmlRpcClient class with a custom HttpClient.
    /// </summary>
    /// <param name="serverUrl">The URL of the XML-RPC server.</param>
    /// <param name="httpClient">The HttpClient to use for requests.</param>
    public XmlRpcClient(string serverUrl, HttpClient httpClient)
        : this(new Uri(serverUrl), httpClient)
    {
    }

    /// <summary>
    /// Initializes a new instance of the XmlRpcClient class with a custom HttpClient.
    /// </summary>
    /// <param name="serverUri">The URI of the XML-RPC server.</param>
    /// <param name="httpClient">The HttpClient to use for requests.</param>
    public XmlRpcClient(Uri serverUri, HttpClient httpClient)
    {
        _serverUri = serverUri ?? throw new ArgumentNullException(nameof(serverUri));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _serializer = new XmlRpcSerializer();
        Headers = new Dictionary<string, string>();

        // Set default user agent
        UserAgent = $"XmlRpc.Net10/1.0.0 (.NET 10)";
    }

    /// <summary>
    /// Invokes an XML-RPC method on the server.
    /// </summary>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameters">The parameters for the method.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the server.</returns>
    public async Task<XmlRpcResponse> InvokeAsync(
        string methodName,
        object?[]? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var request = new XmlRpcRequest(methodName, parameters ?? Array.Empty<object?>());
        return await InvokeAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Invokes an XML-RPC method on the server.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the server.</returns>
    public async Task<XmlRpcResponse> InvokeAsync(
        XmlRpcRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var startTime = DateTime.UtcNow;
        string? requestXml = null;
        string? responseXml = null;

        try
        {
            // Serialize the request
            requestXml = _serializer.SerializeRequest(request);

            // Create the HTTP content
            var content = new StringContent(requestXml, Encoding.UTF8, "text/xml");

            // Add custom headers
            foreach (var header in Headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Raise the request sending event
            RequestSending?.Invoke(this, new XmlRpcRequestEventArgs(request, requestXml));

            // Send the request
            var httpResponse = await _httpClient.PostAsync(_serverUri, content, cancellationToken)
                .ConfigureAwait(false);

            // Check for HTTP errors
            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

                var error = new XmlRpcHttpException(
                    (int)httpResponse.StatusCode,
                    httpResponse.ReasonPhrase ?? "Unknown HTTP error",
                    errorContent);

                ErrorOccurred?.Invoke(this, new XmlRpcErrorEventArgs(request, error, requestXml));
                throw error;
            }

            // Read the response
            responseXml = await httpResponse.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            // Deserialize the response
            var response = _serializer.DeserializeResponse(responseXml);

            // Raise the response received event
            var duration = DateTime.UtcNow - startTime;
            ResponseReceived?.Invoke(this, new XmlRpcResponseEventArgs(request, response, responseXml, duration));

            return response;
        }
        catch (XmlRpcHttpException)
        {
            throw;
        }
        catch (XmlRpcSerializationException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, new XmlRpcErrorEventArgs(request, ex, requestXml));
            throw new XmlRpcSerializationException("Failed to invoke XML-RPC method", ex);
        }
    }

    /// <summary>
    /// Invokes an XML-RPC method on the server and returns the result.
    /// </summary>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameters">The parameters for the method.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The return value from the method.</returns>
    /// <exception cref="XmlRpcFaultException">Thrown if the server returns a fault.</exception>
    public async Task<XmlRpcValue> InvokeAndGetResultAsync(
        string methodName,
        object?[]? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var response = await InvokeAsync(methodName, parameters, cancellationToken).ConfigureAwait(false);
        return response.GetValueOrThrow();
    }

    /// <summary>
    /// Invokes an XML-RPC method on the server and returns the result converted to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to convert the result to.</typeparam>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameters">The parameters for the method.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The return value converted to the specified type.</returns>
    /// <exception cref="XmlRpcFaultException">Thrown if the server returns a fault.</exception>
    public async Task<T?> InvokeAsync<T>(
        string methodName,
        object?[]? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var result = await InvokeAndGetResultAsync(methodName, parameters, cancellationToken)
            .ConfigureAwait(false);
        return result.ToObject<T>();
    }

    /// <summary>
    /// Creates a proxy interface for the XML-RPC server.
    /// </summary>
    /// <typeparam name="T">The interface type.</typeparam>
    /// <returns>A proxy that implements the interface.</returns>
    public T CreateProxy<T>() where T : class
    {
        return XmlRpcProxyGenerator.CreateProxy<T>(this);
    }

    /// <summary>
    /// Disposes the client and releases resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the client and releases resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Interface for XML-RPC clients.
/// </summary>
public interface IXmlRpcClient : IDisposable
{
    /// <summary>
    /// Gets the server URI.
    /// </summary>
    Uri ServerUri { get; }

    /// <summary>
    /// Invokes an XML-RPC method on the server.
    /// </summary>
    Task<XmlRpcResponse> InvokeAsync(string methodName, object?[]? parameters, CancellationToken cancellationToken);

    /// <summary>
    /// Invokes an XML-RPC method on the server.
    /// </summary>
    Task<XmlRpcResponse> InvokeAsync(XmlRpcRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a proxy interface for the XML-RPC server.
    /// </summary>
    T CreateProxy<T>() where T : class;
}

/// <summary>
/// Event arguments for request events.
/// </summary>
public class XmlRpcRequestEventArgs : EventArgs
{
    /// <summary>
    /// Gets the request.
    /// </summary>
    public XmlRpcRequest Request { get; }

    /// <summary>
    /// Gets the serialized XML.
    /// </summary>
    public string XmlContent { get; }

    /// <summary>
    /// Initializes a new instance of the XmlRpcRequestEventArgs class.
    /// </summary>
    public XmlRpcRequestEventArgs(XmlRpcRequest request, string xmlContent)
    {
        Request = request;
        XmlContent = xmlContent;
    }
}

/// <summary>
/// Event arguments for response events.
/// </summary>
public class XmlRpcResponseEventArgs : EventArgs
{
    /// <summary>
    /// Gets the original request.
    /// </summary>
    public XmlRpcRequest Request { get; }

    /// <summary>
    /// Gets the response.
    /// </summary>
    public XmlRpcResponse Response { get; }

    /// <summary>
    /// Gets the raw XML content.
    /// </summary>
    public string XmlContent { get; }

    /// <summary>
    /// Gets the duration of the request.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Initializes a new instance of the XmlRpcResponseEventArgs class.
    /// </summary>
    public XmlRpcResponseEventArgs(XmlRpcRequest request, XmlRpcResponse response, string xmlContent, TimeSpan duration)
    {
        Request = request;
        Response = response;
        XmlContent = xmlContent;
        Duration = duration;
    }
}

/// <summary>
/// Event arguments for error events.
/// </summary>
public class XmlRpcErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the request that caused the error.
    /// </summary>
    public XmlRpcRequest Request { get; }

    /// <summary>
    /// Gets the exception that occurred.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the request XML content, if available.
    /// </summary>
    public string? RequestXml { get; }

    /// <summary>
    /// Initializes a new instance of the XmlRpcErrorEventArgs class.
    /// </summary>
    public XmlRpcErrorEventArgs(XmlRpcRequest request, Exception exception, string? requestXml)
    {
        Request = request;
        Exception = exception;
        RequestXml = requestXml;
    }
}
