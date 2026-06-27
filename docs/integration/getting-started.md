# Getting Started

This guide shows how to consume the SDLang Visual Studio integration endpoints from your own extension.

## 1. Install the extension

Install **SDLang Language Support** from the Visual Studio Marketplace, or build and deploy the
`EllipticBit.SDLang.VisualStudio` VSIX from source. Once installed, opening any `.sdl` or `.sdlang` file gives you
syntax highlighting, bracket/quote auto-close, comment toggling, and live error squiggles.

## 2. Reference the contracts

Every integration surface is defined in **`EllipticBit.SDLang.VisualStudio.Contracts`**. Add a package (or project)
reference to it from your extension:

```xml
<ItemGroup>
  <PackageReference Include="EllipticBit.SDLang.VisualStudio.Contracts" Version="1.0.0" />
</ItemGroup>
```

The contracts assembly is intentionally tiny and free of editor or parser dependencies, so it is safe to
reference from any extension without pulling in the full SDLang stack.

## 3. Choose an integration surface

- To add **semantic rules, completions, or hover** for SDLang documents, implement and export
  [`ISdlSemanticValidator`](mef-semantic-validators.md).
- To target **your own editor components** at SDLang buffers, use the
  [`sdlang` content type](content-type-targeting.md).
- To consume SDLang validation from a **non-MEF or legacy host**, acquire the
  [brokered `ISdlLanguageService`](brokered-service.md).

## 4. Use the shared constants

Always use the well-known constants from `SdlContentTypes` instead of hard-coding literals, so your code stays
in sync with the extension:

```csharp
using EllipticBit.SDLang.VisualStudio;

string contentType = SdlContentTypes.ContentTypeName;      // "sdlang"
string scope       = SdlContentTypes.TextMateScopeName;    // "source.sdl"
string primaryExt  = SdlContentTypes.FileExtension;        // ".sdl"
string altExt      = SdlContentTypes.AlternateFileExtension;// ".sdlang"
```

## 5. Verify

Open a `.sdl` file and introduce an error (for example, a line beginning with `@`). Within a moment you should
see a red squiggle and a matching entry in the **Error List**. If you have registered a semantic validator
scoped to the document, its diagnostics appear alongside the built-in parser diagnostics.

Next: [Diagnostics Model](diagnostics-model.md).
