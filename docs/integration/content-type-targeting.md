# Content-Type Targeting

Visual Studio identifies SDLang documents through a **content type** named `sdlang`. Any editor component you
export through MEF can target that content type to participate in the SDLang editing experience.

## The `sdlang` content type

The extension defines the content type and maps the SDLang file extensions to it:

```csharp
[Export]
[Name("sdlang")]              // SdlContentTypes.ContentTypeName
[BaseDefinition("code")]
internal static ContentTypeDefinition SdlContentTypeDefinition;

[Export]
[FileExtension(".sdl")]        // SdlContentTypes.FileExtension
[ContentType("sdlang")]
internal static FileExtensionToContentTypeDefinition SdlFileExtensionDefinition;

[Export]
[FileExtension(".sdlang")]     // SdlContentTypes.AlternateFileExtension
[ContentType("sdlang")]
internal static FileExtensionToContentTypeDefinition SdlAlternateFileExtensionDefinition;
```

Because the content type is based on `code`, SDLang buffers automatically participate in standard editor
features (selection, outlining, bracket matching) and the TextMate grammar registered for the `source.sdl`
scope.

## Targeting the content type from your extension

Use the `[ContentType("sdlang")]` attribute on any MEF editor export. Prefer the constant
`SdlContentTypes.ContentTypeName` so you stay in sync with the extension.

### Example: a custom tagger

```csharp
using System.ComponentModel.Composition;
using EllipticBit.SDLang.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

[Export(typeof(ITaggerProvider))]
[ContentType(SdlContentTypes.ContentTypeName)]
[TagType(typeof(IClassificationTag))]
internal sealed class MySdlTaggerProvider : ITaggerProvider
{
	public ITagger<T>? CreateTagger<T>(ITextBuffer buffer) where T : ITag
	{
		// ...
	}
}
```

### Components that commonly target a content type

- `ITaggerProvider` / `IViewTaggerProvider` — classification, error, and structure tags.
- `IWpfTextViewCreationListener` — react when an SDLang view opens.
- `ICompletionSourceProvider` / `IAsyncCompletionSourceProvider` — completion.
- `IClassifierProvider` — additional classification.
- `IBraceCompletionSessionProvider`, `ISuggestedActionsSourceProvider`, and more.

## Targeting specific files

The content type applies to every `.sdl`/`.sdlang` buffer. If your component should only act on a specific file
(for example, `dub.sdl`), filter inside your component by inspecting the document file name, or — for semantic
validation — use the file-name scoping built into the validator metadata. See
[MEF Semantic Validators](mef-semantic-validators.md#scoping-to-specific-files).

## Related

- [MEF Semantic Validators](mef-semantic-validators.md) — the highest-level extension point.
- [Getting Started](getting-started.md) — reference the contracts assembly.
