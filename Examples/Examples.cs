// <summary>
// XmlRpc.Net10 - A modern XML-RPC client library for .NET 10
// Copyright (c) 2026 XmlRpc.Net10 Contributors
// Licensed under the MIT License
// </summary>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using XmlRpc.Client;
using XmlRpc.Core;
using XmlRpc.Proxy;

namespace XmlRpc.Examples;

/// <summary>
/// Exemples d'utilisation de la bibliothèque XmlRpc.Net10.
/// </summary>
public static class Examples
{
    /// <summary>
    /// Exemple basique d'utilisation du client XML-RPC.
    /// </summary>
    public static async Task BasicExample()
    {
        // Créer un client
        var client = new XmlRpcClient("https://api.example.com/xmlrpc");

        // Appeler une méthode sans paramètres
        var response = await client.InvokeAsync("system.listMethods");

        if (response.IsSuccess)
        {
            var methods = response.Value!.AsArray;
            Console.WriteLine($"Méthodes disponibles: {methods.Length}");
            foreach (var method in methods)
            {
                Console.WriteLine($"  - {method.AsString}");
            }
        }
    }

    /// <summary>
    /// Exemple avec paramètres.
    /// </summary>
    public static async Task WithParametersExample()
    {
        var client = new XmlRpcClient("https://api.example.com/xmlrpc");

        // Passer des paramètres simples
        var result = await client.InvokeAsync<int>("math.add", new object?[] { 5, 3 });
        Console.WriteLine($"5 + 3 = {result}");

        // Passer différents types de paramètres
        var response = await client.InvokeAsync("test.method", new object?[]
        {
            42,                    // int
            "hello world",         // string
            true,                  // bool
            3.14159,               // double
            DateTime.UtcNow,       // DateTime
            new byte[] { 1, 2, 3 } // base64
        });

        Console.WriteLine($"Réponse: {response.Value}");
    }

    /// <summary>
    /// Exemple avec un struct complexe.
    /// </summary>
    public static async Task StructExample()
    {
        var client = new XmlRpcClient("https://api.example.com/xmlrpc");

        // Créer un struct
        var userData = XmlRpcValue.FromStruct(new Dictionary<string, XmlRpcValue>
        {
            ["name"] = "John Doe",
            ["email"] = "john@example.com",
            ["age"] = 30,
            ["active"] = true,
            ["roles"] = XmlRpcValue.FromArray(new[]
            {
                XmlRpcValue.FromString("admin"),
                XmlRpcValue.FromString("user")
            })
        });

        var response = await client.InvokeAsync("user.create", new[] { userData });

        if (response.IsSuccess)
        {
            var userId = response.Value!.AsInteger;
            Console.WriteLine($"Utilisateur créé avec l'ID: {userId}");
        }
    }

    /// <summary>
    /// Exemple avec gestion des erreurs.
    /// </summary>
    public static async Task ErrorHandlingExample()
    {
        var client = new XmlRpcClient("https://api.example.com/xmlrpc");

        try
        {
            var response = await client.InvokeAsync("method.that.might.fail");

            if (response.IsFault)
            {
                Console.WriteLine($"Erreur serveur: {response.Fault!.FaultCode} - {response.Fault.FaultString}");
                return;
            }

            Console.WriteLine($"Résultat: {response.Value}");
        }
        catch (XmlRpcFaultException ex)
        {
            Console.WriteLine($"Fault: {ex.FaultCode} - {ex.Fault.FaultString}");
        }
        catch (XmlRpcHttpException ex)
        {
            Console.WriteLine($"HTTP Error: {ex.StatusCode}");
        }
        catch (XmlRpcSerializationException ex)
        {
            Console.WriteLine($"Serialization Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Exemple avec le builder de client.
    /// </summary>
    public static async Task BuilderExample()
    {
        var client = XmlRpcClientBuilder.Create()
            .WithServerUrl("https://api.example.com/xmlrpc")
            .WithTimeout(TimeSpan.FromSeconds(30))
            .WithUserAgent("MyApp/1.0")
            .WithBasicAuth("username", "password")
            .WithHeader("X-API-Version", "2.0")
            .WithExtendedTypes(true)
            .Build();

        var response = await client.InvokeAsync("authenticated.method");
        Console.WriteLine($"Résultat: {response.Value}");
    }

    /// <summary>
    /// Exemple avec interface typée (proxy).
    /// </summary>
    public static async Task ProxyExample()
    {
        var client = new XmlRpcClient("https://api.example.com/xmlrpc");
        var api = client.CreateProxy<IMyApi>();

        // Appels typés
        var users = await api.GetUsers(10);
        foreach (var user in users)
        {
            Console.WriteLine($"User: {user.Name} ({user.Email})");
        }

        var newUserId = await api.CreateUser(new CreateUserRequest
        {
            Name = "John Doe",
            Email = "john@example.com",
            Role = "user"
        });

        Console.WriteLine($"Nouvel utilisateur créé: {newUserId}");
    }

    /// <summary>
    /// Exemple avec événements.
    /// </summary>
    public static async Task EventsExample()
    {
        var client = new XmlRpcClient("https://api.example.com/xmlrpc");

        client.RequestSending += (sender, args) =>
        {
            Console.WriteLine($"[REQUEST] {args.Request.MethodName}");
            Console.WriteLine($"[XML] {args.XmlContent}");
        };

        client.ResponseReceived += (sender, args) =>
        {
            Console.WriteLine($"[RESPONSE] Duration: {args.Duration.TotalMilliseconds}ms");
            if (args.Response.IsSuccess)
            {
                Console.WriteLine($"[RESULT] {args.Response.Value}");
            }
        };

        client.ErrorOccurred += (sender, args) =>
        {
            Console.WriteLine($"[ERROR] {args.Exception.Message}");
        };

        await client.InvokeAsync("test.method");
    }

    /// <summary>
    /// Exemple avec annulation (cancellation).
    /// </summary>
    public static async Task CancellationExample()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var client = new XmlRpcClient("https://api.example.com/xmlrpc");

        try
        {
            var response = await client.InvokeAsync(
                "long.running.method",
                null,
                cts.Token);

            Console.WriteLine($"Résultat: {response.Value}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("La requête a été annulée.");
        }
    }

    /// <summary>
    /// Exemple de désérialisation vers un objet complexe.
    /// </summary>
    public static async Task DeserializationExample()
    {
        var client = new XmlRpcClient("https://api.example.com/xmlrpc");

        var response = await client.InvokeAsync("user.getProfile", new object?[] { 123 });

        if (response.IsSuccess)
        {
            // Désérialiser vers un type spécifique
            var profile = response.Value!.ToObject<UserProfile>();

            Console.WriteLine($"Name: {profile.Name}");
            Console.WriteLine($"Email: {profile.Email}");
            Console.WriteLine($"Created: {profile.CreatedAt}");
        }
    }
}

#region Proxy Interfaces

/// <summary>
/// Interface exemple pour un API.
/// </summary>
[XmlRpcService]
public interface IMyApi
{
    [XmlRpcMethod]
    Task<int> CreateUser(CreateUserRequest request);

    [XmlRpcMethod]
    Task<UserInfo[]> GetUsers(int limit);

    [XmlRpcMethod("user.getById")]
    Task<UserInfo> GetUserById(int userId);

    [XmlRpcMethod]
    Task<bool> DeleteUser(int userId);
}

/// <summary>
/// Requête de création d'utilisateur.
/// </summary>
public class CreateUserRequest
{
    [XmlRpcMember("name")]
    public string? Name { get; set; }

    [XmlRpcMember("email")]
    public string? Email { get; set; }

    [XmlRpcMember("role")]
    public string? Role { get; set; }
}

/// <summary>
/// Informations utilisateur.
/// </summary>
public class UserInfo
{
    [XmlRpcMember("id")]
    public int Id { get; set; }

    [XmlRpcMember("name")]
    public string? Name { get; set; }

    [XmlRpcMember("email")]
    public string? Email { get; set; }

    [XmlRpcMember("role")]
    public string? Role { get; set; }

    [XmlRpcMember("active")]
    public bool Active { get; set; }
}

/// <summary>
/// Profil utilisateur.
/// </summary>
public class UserProfile
{
    [XmlRpcMember("id")]
    public int Id { get; set; }

    [XmlRpcMember("name")]
    public string? Name { get; set; }

    [XmlRpcMember("email")]
    public string? Email { get; set; }

    [XmlRpcMember("created_at")]
    public DateTime CreatedAt { get; set; }

    [XmlRpcMember("settings")]
    public Dictionary<string, object>? Settings { get; set; }
}

#endregion
