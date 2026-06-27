# Brokered Service

For consumers that cannot (or do not want to) participate in MEF — including legacy in-proc extensions such as
the **Visual D** plug-in providing `dub.sdl` support — the extension proffers a **brokered language service**.
It exposes the same SDLang validation, completion, and hover capabilities as the editor, over JSON-RPC, backed
by the same engine.

## The service descriptor

The descriptor and interface live in `EllipticBit.SDLang.VisualStudio.Contracts`:

```csharp
public static class SdlBrokeredServices
{
	public const string LanguageServiceMonikerName = "EllipticBit.SDLang.LanguageService";
	public const string LanguageServiceMonikerVersion = "1.0";

	public static ServiceRpcDescriptor LanguageService { get; }  // ServiceJsonRpcDescriptor
}

public interface ISdlLanguageService
{
	Task<IReadOnlyList<SdlValidationDiagnostic>> ValidateAsync(
		string text, string? documentUri, CancellationToken cancellationToken);

	Task<IReadOnlyList<SdlCompletionItem>> GetCompletionsAsync(
		SdlRequestContext context, CancellationToken cancellationToken);

	Task<SdlHover?> GetHoverAsync(
		SdlRequestContext context, CancellationToken cancellationToken);
}
```

The moniker version is incremented only on **breaking** changes to `ISdlLanguageService`.

## Consuming the service

Acquire an `IServiceBroker` (for example, from `SVsBrokeredServiceContainer` or your package's service container)
and request a proxy using the descriptor:

```csharp
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using EllipticBit.SDLang.VisualStudio;

IBrokeredServiceContainer container =
	await GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
IServiceBroker serviceBroker = container.GetFullAccessServiceBroker();

ISdlLanguageService? sdl = await serviceBroker.GetProxyAsync<ISdlLanguageService>(
	SdlBrokeredServices.LanguageService, cancellationToken);

try
{
	if (sdl is not null)
	{
		IReadOnlyList<SdlValidationDiagnostic> diagnostics =
			await sdl.ValidateAsync(documentText, "dub.sdl", cancellationToken);
		// surface diagnostics in your own UI / error source...
	}
}
finally
{
	(sdl as IDisposable)?.Dispose();   // dispose the proxy when finished
}
```

## Why use the brokered service?

- **Out-of-MEF hosts.** Your code runs somewhere MEF composition is not available or not desired.
- **Process/feature isolation.** You want a stable, versioned RPC contract rather than a direct assembly
  reference to editor internals.
- **Identical results.** The service delegates to the same `SdlLanguageEngine` that powers the editor, so
  diagnostics, completions, and hover match exactly what the user sees.

## Data crosses the wire as contract types only

Only the serializable types in the contracts assembly (`SdlValidationDiagnostic`, `SdlTextSpan`,
`SdlCompletionItem`, `SdlHover`, `SdlRequestContext`) are exchanged. No editor or parser types leak across the
JSON-RPC boundary, which keeps the surface stable across Visual Studio and SDLang versions.

## Related

- [MEF Semantic Validators](mef-semantic-validators.md) — the in-proc equivalent for VSIX extensions.
- [Diagnostics Model](diagnostics-model.md) — the shape of the data returned.
