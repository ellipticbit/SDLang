# MEF Semantic Validators

`ISdlSemanticValidator` is the highest-level SDLang extension point. Implement and export it to add
domain-specific **diagnostics**, **completions**, and **hover** on top of the built-in syntactic parsing. The
in-proc language engine discovers every exported validator, runs the ones whose metadata matches the document,
and merges their results with the parser's own diagnostics.

## The interface

```csharp
public interface ISdlSemanticValidator
{
	Task<IReadOnlyList<SdlValidationDiagnostic>> ValidateAsync(
		string text, string? documentUri, CancellationToken cancellationToken);

	Task<IReadOnlyList<SdlCompletionItem>> GetCompletionsAsync(
		SdlRequestContext context, CancellationToken cancellationToken);

	Task<SdlHover?> GetHoverAsync(
		SdlRequestContext context, CancellationToken cancellationToken);
}
```

- Return an **empty collection** (never `null`) from `ValidateAsync`/`GetCompletionsAsync` when you have nothing
  to contribute. Return `null` from `GetHoverAsync` when there is nothing to show.
- Implementations must be **thread-safe** and should honor the `CancellationToken` — the editor cancels
  superseded requests as the user types.

## Exporting a validator

Export the interface and decorate it with the metadata attributes that scope it to SDLang:

```csharp
using System.ComponentModel.Composition;
using EllipticBit.SDLang.VisualStudio;

[Export(typeof(ISdlSemanticValidator))]
[SdlContentType(SdlContentTypes.ContentTypeName)]   // required: scopes to "sdlang"
internal sealed class MySemanticValidator : ISdlSemanticValidator
{
	public Task<IReadOnlyList<SdlValidationDiagnostic>> ValidateAsync(
		string text, string? documentUri, CancellationToken cancellationToken)
	{
		var diagnostics = new List<SdlValidationDiagnostic>();

		// Example: flag a deprecated tag.
		// (Use the SDLang DOM or your own parsing to locate spans.)
		// diagnostics.Add(new SdlValidationDiagnostic(
		//     "The 'legacy' tag is deprecated.",
		//     SdlValidationSeverity.Warning,
		//     new SdlTextSpan(line, startChar, line, endChar),
		//     code: "MY0001",
		//     source: "my-extension"));

		return Task.FromResult<IReadOnlyList<SdlValidationDiagnostic>>(diagnostics);
	}

	public Task<IReadOnlyList<SdlCompletionItem>> GetCompletionsAsync(
		SdlRequestContext context, CancellationToken cancellationToken)
		=> Task.FromResult<IReadOnlyList<SdlCompletionItem>>(Array.Empty<SdlCompletionItem>());

	public Task<SdlHover?> GetHoverAsync(
		SdlRequestContext context, CancellationToken cancellationToken)
		=> Task.FromResult<SdlHover?>(null);
}
```

## Scoping to specific files

Add `[SdlFileName(...)]` to run your validator only for matching documents — ideal for tools like Visual D that
care about `dub.sdl` specifically:

```csharp
[Export(typeof(ISdlSemanticValidator))]
[SdlContentType(SdlContentTypes.ContentTypeName)]
[SdlFileName("dub.sdl")]
internal sealed class DubValidator : ISdlSemanticValidator { /* ... */ }
```

The engine derives the file name from the document URI passed to `ValidateAsync` (and from
`SdlRequestContext.DocumentUri` for completion/hover). When one or more file names are declared, the validator
runs **only** for those files. When no file name is declared, it runs for every SDLang document.

## How results are merged

- **Diagnostics** — the parser's syntactic diagnostics come first, followed by each matching validator's
  diagnostics, in registration order.
- **Completions** — items from all matching validators are concatenated.
- **Hover** — the first matching validator to return a non-`null` hover wins (registration order).

## Isolation

A validator that throws does **not** break the experience: the engine swallows the exception (so the built-in
parser diagnostics still appear) and continues with the next validator. Cancellation is always propagated, so
return promptly when the token is signaled.

## Related

- [Diagnostics Model](diagnostics-model.md) — span and severity semantics.
- [Brokered Service](brokered-service.md) — the same capabilities for non-MEF consumers.
