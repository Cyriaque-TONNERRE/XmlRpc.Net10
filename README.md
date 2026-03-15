# XmlRpc.Net10

Une bibliothèque XML-RPC client moderne pour .NET 10, conçue pour être performante, type-safe et facile à utiliser.

## Caractéristiques

- ✅ **.NET 10 natif** - Exploite toutes les dernières fonctionnalités de .NET 10
- ✅ **Type-safe** - Conversion automatique entre types XML-RPC et types .NET
- ✅ **Async/Await** - Support complet de la programmation asynchrone
- ✅ **Proxy dynamique** - Génération de proxies pour appels de méthodes typées
- ✅ **HttpClient** - Utilisation moderne de HttpClient pour les requêtes HTTP
- ✅ **Types étendus** - Support des extensions XML-RPC (i8, nil, etc.)
- ✅ **Testé** - Couverture de tests complète avec xUnit

## Utilisation rapide

### Appel simple

```csharp
using XmlRpc.Client;
using XmlRpc.Core;

// Créer un client
var client = new XmlRpcClient("https://api.example.com/xmlrpc");

// Appeler une méthode
var response = await client.InvokeAsync("system.listMethods");

// Récupérer le résultat
if (response.IsSuccess)
{
    var methods = response.Value!.AsArray;
    foreach (var method in methods)
    {
        Console.WriteLine(method.AsString);
    }
}
```

### Avec paramètres

```csharp
// Appel avec paramètres simples
var result = await client.InvokeAsync("math.add", new object?[] { 5, 3 });
Console.WriteLine($"5 + 3 = {result.Value!.AsInteger}");

// Ou utiliser la méthode générique pour obtenir directement le type
int sum = await client.InvokeAsync<int>("math.add", new object?[] { 5, 3 });
Console.WriteLine($"5 + 3 = {sum}");
```

### Utilisation du builder

```csharp
var client = XmlRpcClientBuilder.Create()
    .WithServerUrl("https://api.example.com/xmlrpc")
    .WithTimeout(TimeSpan.FromSeconds(30))
    .WithUserAgent("MyApp/1.0")
    .WithBasicAuth("username", "password")
    .WithHeader("X-API-Key", "your-api-key")
    .Build();
```

### Interface typée (Proxy)

```csharp
using XmlRpc.Client;
using XmlRpc.Proxy;

// Définir l'interface du service
[XmlRpcService]
public interface IBlogService
{
    [XmlRpcMethod]
    Task<int> NewPost(string title, string content, bool publish);

    [XmlRpcMethod("metaWeblog.getRecentPosts")]
    Task<BlogPost[]> GetRecentPosts(string blogId, int count);

    [XmlRpcMethod]
    Task<bool> DeletePost(int postId);
}

public class BlogPost
{
    [XmlRpcMember("postid")]
    public int Id { get; set; }

    [XmlRpcMember("title")]
    public string? Title { get; set; }

    [XmlRpcMember("description")]
    public string? Content { get; set; }

    [XmlRpcMember("dateCreated")]
    public DateTime DateCreated { get; set; }
}

// Utiliser le proxy
var client = new XmlRpcClient("https://blog.example.com/xmlrpc");
var blog = client.CreateProxy<IBlogService>();

// Appels typés
int postId = await blog.NewPost("Mon titre", "Mon contenu", true);
var posts = await blog.GetRecentPosts("blog1", 10);
```

## Types XML-RPC supportés

| Type XML-RPC       | Type .NET                         | Notes                      |
|--------------------|-----------------------------------|----------------------------|
| `int`, `i4`        | `int`                             | Entier 32 bits             |
| `i8`               | `long`                            | Entier 64 bits (extension) |
| `boolean`          | `bool`                            | Valeur booléenne           |
| `string`           | `string`                          | Chaîne de caractères       |
| `double`           | `double`                          | Nombre à virgule flottante |
| `dateTime.iso8601` | `DateTime`                        | Date et heure              |
| `base64`           | `byte[]`                          | Données binaires           |
| `struct`           | `Dictionary<string, XmlRpcValue>` | Dictionnaire clé-valeur    |
| `array`            | `XmlRpcValue[]`                   | Tableau de valeurs         |
| `nil`              | `null`                            | Valeur nulle (extension)   |

## XmlRpcValue

La classe `XmlRpcValue` est le cœur de la bibliothèque, offrant une représentation type-safe des valeurs XML-RPC.

### Création de valeurs

```csharp
// Méthodes factory
var intValue = XmlRpcValue.FromInt(42);
var stringValue = XmlRpcValue.FromString("hello");
var boolValue = XmlRpcValue.FromBoolean(true);
var doubleValue = XmlRpcValue.FromDouble(3.14);
var dateValue = XmlRpcValue.FromDateTime(DateTime.Now);
var bytesValue = XmlRpcValue.FromBase64(new byte[] { 1, 2, 3 });

// Struct
var structValue = XmlRpcValue.FromStruct(new Dictionary<string, XmlRpcValue>
{
    ["name"] = "John",
    ["age"] = 30,
    ["active"] = true
});

// Array
var arrayValue = XmlRpcValue.FromArray(new[]
{
    XmlRpcValue.FromInt(1),
    XmlRpcValue.FromInt(2),
    XmlRpcValue.FromInt(3)
});

// Conversion automatique depuis un objet .NET
var autoValue = XmlRpcValue.FromObject(new { Name = "Test", Value = 123 });
```

### Conversion de valeurs

```csharp
XmlRpcValue value = GetXmlRpcValue();

// Accès direct (lance une exception si le type ne correspond pas)
int i = value.AsInteger;
string s = value.AsString;
bool b = value.AsBoolean;
double d = value.AsDouble;
DateTime dt = value.AsDateTime;
byte[] bytes = value.AsBase64;
var dict = value.AsStruct;
var arr = value.AsArray;

// Accès sécurisé avec TryGet
if (value.TryGetInteger(out var intResult))
{
    Console.WriteLine($"Integer: {intResult}");
}

// Conversion vers un type .NET
var myObject = value.ToObject<MyClass>();
```

### Opérateurs implicites

```csharp
// Conversion implicite depuis les types .NET
XmlRpcValue v1 = 42;          // int -> XmlRpcValue
XmlRpcValue v2 = "hello";     // string -> XmlRpcValue
XmlRpcValue v3 = true;        // bool -> XmlRpcValue
XmlRpcValue v4 = 3.14;        // double -> XmlRpcValue
XmlRpcValue v5 = DateTime.Now; // DateTime -> XmlRpcValue

// Conversion explicite vers les types .NET
int i = (int)v1;
string s = (string)v2;
bool b = (bool)v3;
```

## Gestion des erreurs

### XmlRpcFaultException

```csharp
try
{
    var result = await client.InvokeAsync("some.method");
}
catch (XmlRpcFaultException ex)
{
    Console.WriteLine($"Fault Code: {ex.FaultCode}");
    Console.WriteLine($"Fault String: {ex.FaultString}");
}
```

### XmlRpcHttpException

```csharp
try
{
    var result = await client.InvokeAsync("some.method");
}
catch (XmlRpcHttpException ex)
{
    Console.WriteLine($"HTTP Status: {ex.StatusCode}");
    Console.WriteLine($"Response: {ex.ResponseContent}");
}
```

### Réponse avec fault

```csharp
var response = await client.InvokeAsync("some.method");

if (response.IsFault)
{
    Console.WriteLine($"Error: {response.Fault!.FaultString}");
}
else
{
    Console.WriteLine($"Result: {response.Value}");
}
```

## Événements

```csharp
var client = new XmlRpcClient("https://api.example.com/xmlrpc");

// Avant l'envoi de la requête
client.RequestSending += (sender, args) =>
{
    Console.WriteLine($"Calling: {args.Request.MethodName}");
    Console.WriteLine($"XML: {args.XmlContent}");
};

// Après réception de la réponse
client.ResponseReceived += (sender, args) =>
{
    Console.WriteLine($"Duration: {args.Duration.TotalMilliseconds}ms");
    Console.WriteLine($"Response: {args.XmlContent}");
};

// En cas d'erreur
client.ErrorOccurred += (sender, args) =>
{
    Console.WriteLine($"Error: {args.Exception.Message}");
};
```

## Configuration avancée

### Proxy HTTP

```csharp
var client = XmlRpcClientBuilder.Create()
    .WithServerUrl("https://api.example.com/xmlrpc")
    .WithProxy(new WebProxy("http://proxy.company.com:8080"))
    .Build();
```

### Authentification

```csharp
// Authentification basique
var client = XmlRpcClientBuilder.Create()
    .WithServerUrl("https://api.example.com/xmlrpc")
    .WithBasicAuth("username", "password")
    .Build();

// Ou avec credentials Windows
var client = XmlRpcClientBuilder.Create()
    .WithServerUrl("https://api.example.com/xmlrpc")
    .WithCredentials(CredentialCache.DefaultCredentials)
    .Build();
```

### Configuration personnalisée du HttpClient

```csharp
var client = XmlRpcClientBuilder.Create()
    .WithServerUrl("https://api.example.com/xmlrpc")
    .ConfigureHttpClient(http =>
    {
        http.DefaultRequestHeaders.Add("X-Custom-Header", "value");
        http.MaxResponseContentBufferSize = 1024 * 1024;
    })
    .ConfigureHandler(handler =>
    {
        handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
    })
    .Build();
```

## Sérialisation

La bibliothèque inclut un sérialiseur XML-RPC complet :

```csharp
using XmlRpc.Serialization;

var serializer = new XmlRpcSerializer
{
    UseExtendedTypes = true,  // Support i8, nil
    Indent = true             // Indenter le XML
};

// Sérialiser une requête
var request = new XmlRpcRequest("test.method", 42, "hello");
string xml = serializer.SerializeRequest(request);

// Désérialiser une réponse
var response = serializer.DeserializeResponse(xmlResponse);
```

## Exemples concrets

### WordPress XML-RPC API

```csharp
[XmlRpcService]
public interface IWordPressApi
{
    [XmlRpcMethod("wp.getPosts")]
    Task<WordPressPost[]> GetPosts(
        [XmlRpcParameter("blog_id")] string blogId,
        [XmlRpcParameter("username")] string username,
        [XmlRpcParameter("password")] string password,
        [XmlRpcParameter("filter")] Dictionary<string, object>? filter = null);

    [XmlRpcMethod("wp.newPost")]
    Task<string> NewPost(
        [XmlRpcParameter("blog_id")] string blogId,
        [XmlRpcParameter("username")] string username,
        [XmlRpcParameter("password")] string password,
        [XmlRpcParameter("post")] Dictionary<string, object> post);
}

// Utilisation
var client = new XmlRpcClient("https://myblog.com/xmlrpc.php");
var wp = client.CreateProxy<IWordPressApi>();

var posts = await wp.GetPosts("1", "admin", "password", new Dictionary<string, object>
{
    ["post_type"] = "post",
    ["post_status"] = "publish",
    ["numberposts"] = 10
});
```

### MetaWeblog API

```csharp
[XmlRpcService]
public interface IMetaWeblogApi
{
    [XmlRpcMethod("metaWeblog.getRecentPosts")]
    Task<MetaWeblogPost[]> GetRecentPosts(
        string blogId,
        string username,
        string password,
        int numberOfPosts);

    [XmlRpcMethod("metaWeblog.newPost")]
    Task<string> NewPost(
        string blogId,
        string username,
        string password,
        MetaWeblogPost post,
        bool publish);
}

public class MetaWeblogPost
{
    [XmlRpcMember("title")]
    public string? Title { get; set; }

    [XmlRpcMember("description")]
    public string? Description { get; set; }

    [XmlRpcMember("categories")]
    public string[]? Categories { get; set; }

    [XmlRpcMember("dateCreated")]
    public DateTime DateCreated { get; set; }
}
```