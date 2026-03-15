// <summary>
// XmlRpc.Net10 - A modern XML-RPC client library for .NET 10
// Copyright (c) 2026 XmlRpc.Net10 Contributors
// Licensed under the MIT License
// </summary>

using System;
using System.Collections.Generic;
using XmlRpc.Client;
using Xunit;

namespace XmlRpc.Tests;

/// <summary>
///     Unit tests for XmlRpcClientBuilder.
/// </summary>
public class XmlRpcClientBuilderTests
{
    [Fact]
    public void Build_WithServerUrl_ShouldCreateClient()
    {
        var client = XmlRpcClientBuilder.Create()
            .WithServerUrl("http://example.com/xmlrpc")
            .Build();

        Assert.NotNull(client);
        Assert.Equal(new Uri("http://example.com/xmlrpc"), client.ServerUri);
    }

    [Fact]
    public void Build_WithoutServerUrl_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() => XmlRpcClientBuilder.Create().Build());
    }

    [Fact]
    public void WithTimeout_ShouldSetTimeout()
    {
        var timeout = TimeSpan.FromSeconds(30);

        var client = XmlRpcClientBuilder.Create()
            .WithServerUrl("http://example.com/xmlrpc")
            .WithTimeout(timeout)
            .Build();

        Assert.Equal(timeout, client.Timeout);
    }

    [Fact]
    public void WithUserAgent_ShouldSetUserAgent()
    {
        var client = XmlRpcClientBuilder.Create()
            .WithServerUrl("http://example.com/xmlrpc")
            .WithUserAgent("MyApp/1.0")
            .Build();

        Assert.Equal("MyApp/1.0", client.UserAgent);
    }

    [Fact]
    public void WithExtendedTypes_ShouldSetExtendedTypes()
    {
        var client = XmlRpcClientBuilder.Create()
            .WithServerUrl("http://example.com/xmlrpc")
            .WithExtendedTypes()
            .Build();

        Assert.True(client.UseExtendedTypes);
    }

    [Fact]
    public void WithExtendedTypes_False_ShouldDisableExtendedTypes()
    {
        var client = XmlRpcClientBuilder.Create()
            .WithServerUrl("http://example.com/xmlrpc")
            .WithExtendedTypes(false)
            .Build();

        Assert.False(client.UseExtendedTypes);
    }

    [Fact]
    public void WithHeader_ShouldAddHeader()
    {
        var client = XmlRpcClientBuilder.Create()
            .WithServerUrl("http://example.com/xmlrpc")
            .WithHeader("X-Custom", "value")
            .Build();

        Assert.True(client.Headers.ContainsKey("X-Custom"));
        Assert.Equal("value", client.Headers["X-Custom"]);
    }

    [Fact]
    public void WithHeaders_ShouldAddMultipleHeaders()
    {
        var headers = new Dictionary<string, string>
        {
            ["X-Header1"] = "value1",
            ["X-Header2"] = "value2"
        };

        var client = XmlRpcClientBuilder.Create()
            .WithServerUrl("http://example.com/xmlrpc")
            .WithHeaders(headers)
            .Build();

        Assert.Equal("value1", client.Headers["X-Header1"]);
        Assert.Equal("value2", client.Headers["X-Header2"]);
    }
}