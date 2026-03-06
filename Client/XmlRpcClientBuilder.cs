// <summary>
// XmlRpc.Net10 - A modern XML-RPC client library for .NET 10
// Copyright (c) 2026 XmlRpc.Net10 Contributors
// Licensed under the MIT License
// </summary>

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace XmlRpc.Client;

/// <summary>
/// A builder for creating XML-RPC clients with custom configuration.
/// </summary>
public class XmlRpcClientBuilder
{
    private Uri? _serverUri;
    private TimeSpan _timeout = TimeSpan.FromSeconds(100);
    private string? _userAgent;
    private bool _useExtendedTypes = true;
    private readonly Dictionary<string, string> _headers = new();
    private IWebProxy? _proxy;
    private ICredentials? _credentials;
    private Action<HttpClient>? _configureClient;
    private Action<HttpClientHandler>? _configureHandler;

    /// <summary>
    /// Sets the server URL.
    /// </summary>
    /// <param name="url">The server URL.</param>
    /// <returns>This builder instance.</returns>
    public XmlRpcClientBuilder WithServerUrl(string url)
    {
        _serverUri = new Uri(url);
        return this;
    }

    /// <summary>
    /// Sets the server URI.
    /// </summary>
    /// <param name="uri">The server URI.</param>
    /// <returns>This builder instance.</returns>
    public XmlRpcClientBuilder WithServerUri(Uri uri)
    {
        _serverUri = uri;
        return this;
    }

    /// <summary>
    /// Sets the request timeout.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>This builder instance.</returns>
    public XmlRpcClientBuilder WithTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the user agent string.
    /// </summary>
    /// <param name="userAgent">The user agent string.</param>
    /// <returns>This builder instance.</returns>
    public XmlRpcClientBuilder WithUserAgent(string userAgent)
    {
        _userAgent = userAgent;
        return this;
    }

    /// <summary>
    /// Enables or disables extended XML-RPC types (i8, nil, etc.).
    /// </summary>
    /// <param name="useExtendedTypes">Whether to use extended types.</param>
    /// <returns>This builder instance.</returns>
    public XmlRpcClientBuilder WithExtendedTypes(bool useExtendedTypes = true)
    {
        _useExtendedTypes = useExtendedTypes;
        return this;
    }

    /// <summary>
    /// Adds a custom header to all requests.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>This builder instance.</returns>
    public XmlRpcClientBuilder WithHeader(string name, string value)
    {
        _headers[name] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple custom headers to all requests.
    /// </summary>
    /// <param name="headers">The headers to add.</param>
    /// <returns>This builder instance.</returns>
    public XmlRpcClientBuilder WithHeaders(IEnumerable<KeyValuePair<string, string>> headers)
    {
        foreach (var header in headers)
        {
            _headers[header.Key] = header.Value;
        }
        return this;
    }

    /// <summary>
    /// Sets the proxy for HTTP requests.
    /// </summary>
    /// <param name="proxy">The proxy to use.</param>
    /// <returns>This builder instance.</returns>
    public XmlRpcClientBuilder WithProxy(IWebProxy proxy)
    {
        _proxy = proxy;
        return this;
    }

    /// <summary>
    /// Sets the credentials for authentication.
    /// </summary>
    /// <param name="credentials">The credentials.</param>
    /// <returns>This builder instance.</returns>
    public XmlRpcClientBuilder WithCredentials(ICredentials credentials)
    {
        _credentials = credentials;
        return this;
    }

    /// <summary>
    /// Sets basic authentication credentials.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <returns>This builder instance.</returns>
    public XmlRpcClientBuilder WithBasicAuth(string username, string password)
    {
        var credentials = new NetworkCredential(username, password);
        return WithCredentials(credentials);
    }

    /// <summary>
    /// Configures the HttpClient instance.
    /// </summary>
    /// <param name="configure">An action to configure the HttpClient.</param>
    /// <returns>This builder instance.</returns>
    public XmlRpcClientBuilder ConfigureHttpClient(Action<HttpClient> configure)
    {
        _configureClient = configure;
        return this;
    }

    /// <summary>
    /// Configures the HttpClientHandler instance.
    /// </summary>
    /// <param name="configure">An action to configure the HttpClientHandler.</param>
    /// <returns>This builder instance.</returns>
    public XmlRpcClientBuilder ConfigureHandler(Action<HttpClientHandler> configure)
    {
        _configureHandler = configure;
        return this;
    }

    /// <summary>
    /// Builds the XML-RPC client.
    /// </summary>
    /// <returns>The configured client.</returns>
    /// <exception cref="InvalidOperationException">Thrown if server URL is not set.</exception>
    public XmlRpcClient Build()
    {
        if (_serverUri == null)
        {
            throw new InvalidOperationException("Server URL must be set before building the client.");
        }

        var handler = new HttpClientHandler();

        if (_proxy != null)
        {
            handler.Proxy = _proxy;
            handler.UseProxy = true;
        }

        if (_credentials != null)
        {
            handler.Credentials = _credentials;
            handler.UseDefaultCredentials = false;
        }

        _configureHandler?.Invoke(handler);

        var httpClient = new HttpClient(handler)
        {
            Timeout = _timeout
        };

        _configureClient?.Invoke(httpClient);

        var client = new XmlRpcClient(_serverUri, httpClient)
        {
            UseExtendedTypes = _useExtendedTypes
        };

        if (_userAgent != null)
        {
            client.UserAgent = _userAgent;
        }

        foreach (var header in _headers)
        {
            client.Headers[header.Key] = header.Value;
        }

        return client;
    }

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    /// <returns>A new XmlRpcClientBuilder.</returns>
    public static XmlRpcClientBuilder Create() => new();
}
