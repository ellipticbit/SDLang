# EllipticBit.SDLang

A fast [SDLang](https://sdlang.org/) (Simple Declarative Language) parser, mutable DOM, and object
serializer for .NET, designed to feel like `System.Text.Json`.

SDLang is a concise, human-friendly document language similar to XML or JSON. EllipticBit.SDLang lets you:

- **Parse and emit** SDLang documents through a high-performance, span-based UTF-8 reader and writer.
- **Work with a mutable DOM** (`SdlDocument`, `Tag`, `SdlValue`) — the SDLang analog of `JsonDocument`/`JsonElement`.
- **Serialize and deserialize** .NET object graphs with attributes and options that mirror `System.Text.Json`.

![pipeline status](https://gitlab.com/EllipticBit/sdlang/badges/master/pipeline.svg)

> Targets **.NET 10** (`net10.0`). AOT support is out of scope for this project at this time.

> **Deprecation notice:** The previous release, [`EllipticBit.SDLang` 1.0.1](https://www.nuget.org/packages/EllipticBit.SDLang/1.0.1),
> is deprecated and is superseded by this package. Please upgrade to the current version.

---

## Installation

```shell
dotnet add package EllipticBit.SDLang
```

For Microsoft.Extensions.DependencyInjection integration:

```shell
dotnet add package EllipticBit.SDLang.DependencyInjection
```

For ASP.NET Core (MVC and Minimal API) integration:

```shell
dotnet add package EllipticBit.SDLang.AspNetCore
```

---

## SDLang in 30 seconds

```sdl
// A tag has a name, optional values, optional attributes, and optional children.
greeting "Hello World"

person "Alice" age=42 active=true {
	address "1 Main St" city="Springfield"
}

matrix {
	row 1 2 3
	row 4 5 6
}
```

- **Values** are positional literals (`"Alice"`, `1`, `true`).
- **Attributes** are `name=value` pairs.
- **Children** live inside `{ }` blocks.
- Tags may be **namespaced** (`ns:config`).

---

## Quick start: parse and query the DOM

```csharp
using EllipticBit.SDLang;

SdlDocument document = SdlDocument.Parse("""
	person "Alice" age=42 active=true {
		address "1 Main St" city="Springfield"
	}
	""");

Tag person = document.Tags[0];

string name = person.Value!.AsString();          // "Alice"
int age = person.Attributes["age"].AsInt32();    // 42
bool active = person.Attributes["active"].AsBoolean(); // true

Tag address = person.Children[0];
string city = address.Attributes["city"].AsString(); // "Springfield"
```

### Build and emit a document

```csharp
using EllipticBit.SDLang;

SdlDocument document = new();
Tag server = document.AddTag("server");
server.SetAttribute("host", "localhost");
server.SetAttribute("port", 8080);
server.AddChild("endpoint").AddValue("/api");

// Compact output...
string sdl = document.ToSdlString();

// ...or pretty-printed with indentation.
string indented = document.ToSdlString(new SdlWriterOptions { Indented = true });
```

---

## Object serialization

Serialization mirrors `System.Text.Json`: call the static `SdlSerializer` facade with your type.

```csharp
using EllipticBit.SDLang.Serialization;

public sealed class Server
{
	public string Host { get; set; } = "localhost";
	public int Port { get; set; }
	public bool Enabled { get; set; }
}

var server = new Server { Host = "db.local", Port = 5432, Enabled = true };

string sdl = SdlSerializer.Serialize(server);
Server? roundTripped = SdlSerializer.Deserialize<Server>(sdl);
```

### Controlling the shape with attributes

By default, scalar members become **attributes**. Use attributes to map members onto SDLang's value,
attribute, and child roles:

```csharp
using EllipticBit.SDLang.Serialization;

public sealed class Person
{
	[SdlValue(0)]                       // positional tag value
	public string Name { get; set; } = "";

	[SdlAttribute]                      // name=value attribute
	public int Age { get; set; }

	[SdlChild]                          // nested child tag
	public Address? Home { get; set; }

	[SdlIgnore(Condition = SdlIgnoreCondition.WhenWritingNull)]
	public string? Nickname { get; set; }
}
```

Other attributes include `[SdlPropertyName]`, `[SdlPropertyOrder]`, `[SdlNamespace]`, `[SdlConverter]`,
`[SdlConstructor]`, `[SdlInclude]`, and `[SdlExtensionData]`.

### Async and UTF-8 APIs

```csharp
using EllipticBit.SDLang.Serialization;

// Stream-based async serialization.
await SdlSerializer.SerializeAsync(stream, server);
stream.Position = 0;
Server? result = await SdlSerializer.DeserializeAsync<Server>(stream);

// Stay on UTF-8 bytes to avoid UTF-16 conversions.
byte[] utf8 = SdlSerializer.SerializeToUtf8Bytes(server);
Server? fromBytes = SdlSerializer.Deserialize<Server>(utf8);
```

### Options (parity with `JsonSerializerOptions`)

```csharp
using EllipticBit.SDLang.Serialization;

var options = new SdlSerializerOptions
{
	WriteIndented = true,
	PropertyNamingPolicy = SdlNamingPolicy.CamelCase,
	PropertyNameCaseInsensitive = true,
	DefaultIgnoreCondition = SdlIgnoreCondition.WhenWritingNull,
	ReferenceHandler = SdlReferenceHandler.Preserve, // round-trips shared/cyclic graphs via $id/$ref
};

string sdl = SdlSerializer.Serialize(server, options);
```

Supported options include `WriteIndented`, `MaxDepth`, `PropertyNamingPolicy`, `PropertyNameCaseInsensitive`,
`DefaultIgnoreCondition`, `NumberHandling`, `UnmappedMemberHandling`, `ReadCommentHandling`, `ReferenceHandler`,
`IncludeFields`, and custom `Converters`. Options become read-only on first use, just like `System.Text.Json`.

---

## Dependency Injection

The `EllipticBit.SDLang.DependencyInjection` package wires `SdlSerializerOptions` into the options pattern.

```csharp
using EllipticBit.SDLang.DependencyInjection;
using EllipticBit.SDLang.Serialization;
using Microsoft.Extensions.DependencyInjection;

services.AddSdlSerializerOptions(options =>
{
	options.WriteIndented = true;
	options.PropertyNamingPolicy = SdlNamingPolicy.CamelCase;
});

// Register a custom converter (instance or type, optionally named).
services.AddSdlConverter<TemperatureConverter>();
services.AddSdlSerializerOptions("compact", options => options.WriteIndented = false);
```

Resolve the configured options anywhere via constructor injection:

```csharp
public sealed class ConfigWriter(SdlSerializerOptions options)
{
	public string Write(Server server) => SdlSerializer.Serialize(server, options);
}
```

Named options are available through `IOptionsMonitor<SdlSerializerOptions>` / `IOptionsSnapshot<SdlSerializerOptions>`.

---

## ASP.NET Core

The `EllipticBit.SDLang.AspNetCore` package lets you use SDLang as an HTTP request and response document language —
the SDLang counterpart to the built-in JSON and XML support — for both MVC controllers and Minimal APIs.

Content is negotiated through these media types, and the `.sdl` URL/format suffix maps to `application/sdlang`:

| Media type           | Purpose                          |
| -------------------- | -------------------------------- |
| `application/sdlang` | Primary SDLang media type        |
| `text/sdlang`        | Text-oriented SDLang media type  |

### MVC controllers

Register the SDLang formatters on your MVC builder. The optional delegate configures the shared
`SdlSerializerOptions` used to read and write payloads (it flows through `AddSdlSerializerOptions`):

```csharp
using EllipticBit.SDLang.Serialization;
using Microsoft.Extensions.DependencyInjection;

builder.Services
	.AddControllers()
	.AddSdlFormatters(options =>
	{
		options.WriteIndented = true;
		options.PropertyNamingPolicy = SdlNamingPolicy.CamelCase;
	});
```

Controllers then accept and return your types as usual; clients select SDLang via the `Accept` and
`Content-Type` headers (or a `.sdl` format suffix):

```csharp
[ApiController]
[Route("api/servers")]
public sealed class ServersController : ControllerBase
{
	[HttpPost("echo")]
	public Server Echo([FromBody] Server server) => server;

	[HttpGet]
	public Server Get() => new() { Host = "localhost", Port = 8080, Enabled = true };
}
```

A malformed SDLang request body is reported as a model-state error, producing an HTTP 400 response.

### Minimal APIs

Bind an SDLang request body by declaring a parameter of type `Sdl<T>`, and write SDLang responses with
`Results.Extensions.Sdl(...)`:

```csharp
using EllipticBit.SDLang.AspNetCore.Http;

builder.Services.AddSdlFormatters(); // optional; registers SdlSerializerOptions for the resolver

app.MapPost("/servers/echo", (Sdl<Server> body) => Results.Extensions.Sdl(body.Value));
app.MapGet("/servers", () => Results.Extensions.Sdl(new Server { Host = "localhost", Port = 8080 }));
```

A malformed body bound through `Sdl<T>` surfaces as a `BadHttpRequestException` (HTTP 400).

Prefer to work with the request/response directly? Use the `HttpRequest`/`HttpResponse` helpers:

```csharp
using EllipticBit.SDLang.AspNetCore.Http;

app.MapPost("/servers/raw", async (HttpRequest request, HttpResponse response) =>
{
	Server? server = await request.ReadFromSdlAsync<Server>();
	await response.WriteAsSdlAsync(server);
});
```

---

## Custom converters

Derive from `SdlConverter<T>` and register it via options or the `[SdlConverter]` attribute:

```csharp
using EllipticBit.SDLang;
using EllipticBit.SDLang.Serialization;

public sealed class TemperatureConverter : SdlConverter<Temperature>
{
	public override Temperature Read(SdlValue value, SdlSerializerOptions options)
		=> new(value.AsDouble());

	public override SdlValue Write(Temperature value, SdlSerializerOptions options)
		=> SdlValue.Create(value.Celsius);
}

[SdlConverter(typeof(TemperatureConverter))]
public sealed class Temperature(double celsius)
{
	public double Celsius { get; } = celsius;
}
```

---

## Security and performance

SDLang documents frequently come from untrusted sources, so the parser is hardened against malformed input:

- **UTF-8 only** end to end, avoiding UTF-16 conversions where possible (`ReadOnlySpan<byte>`/`IBufferWriter<byte>`).
- Strict UTF-8 validation rejects malformed byte sequences with a located `SdlReaderException`.
- Configurable `MaxDepth` guards against deeply nested "depth bomb" inputs.
- Unterminated strings/comments, invalid base64, and numeric overflow raise typed exceptions with line/column info.
- Full Unicode support, including emoji (😀) and Kanji (漢字) in identifiers, values, and string literals.

---

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

> **LLM contributions:** Any contribution generated with the assistance of a Large Language Model **must** include
> the prompt(s) used to generate it in the [`PROMPTS.txt`](PROMPTS.txt) file at the repository root.

---

## License

Licensed under the [Boost Software License 1.0](LICENSE) (BSL-1.0).
