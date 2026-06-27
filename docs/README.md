# SDLang for Visual Studio — Integration Guide

The **SDLang Language Support** extension turns SDLang (`.sdl`, `.sdlang`) into a first-class language in
Visual Studio 2022 (17.x) and Visual Studio 2026 (18.x). Out of the box it provides:

- **Syntax highlighting** via a TextMate grammar (`source.sdl`).
- **Language configuration** — bracket/quote auto-close, comment toggling, indentation.
- **Live diagnostics** — error squiggles and Error List entries from a lightweight, in-proc parser.
- **Extensibility** — three integration surfaces so other extensions can add semantic understanding for
  their own SDLang documents.

This wiki documents the public integration endpoints. All of them are defined in the
`EllipticBit.SDLang.VisualStudio.Contracts` assembly, which is safe to reference from your own extension.

## Integration surfaces

| Surface | Use it when | Start here |
| --- | --- | --- |
| **MEF semantic validators** | You ship a Visual Studio extension (VSIX) and want to add semantic validation, completions, or hover for SDLang documents. | [MEF Semantic Validators](integration/mef-semantic-validators.md) |
| **Content-type targeting** | You want your own editor components (taggers, classifiers, adornments) to light up on SDLang buffers. | [Content-Type Targeting](integration/content-type-targeting.md) |
| **Brokered service** | You are a legacy or out-of-MEF consumer (for example, the Visual D plug-in for `dub.sdl`) and want SDLang validation over JSON-RPC. | [Brokered Service](integration/brokered-service.md) |

## Supporting concepts

- [Getting Started](integration/getting-started.md) — install, reference the contracts, and verify the experience.
- [Diagnostics Model](integration/diagnostics-model.md) — how positions, severities, and spans are represented.
- [Permissive Parsing](integration/permissive-parsing.md) — how the editor parses invalid documents to produce diagnostics, and how that differs from the strict, secure default of the core library.

## Architecture at a glance

```
EllipticBit.SDLang (net10.0, core parser/DOM/serializer)
		│  shared source (net472)
		▼
EllipticBit.SDLang.Analysis ── parses text, emits SdlDiagnostic
		▼
EllipticBit.SDLang.VisualStudio.LanguageServer ── SdlLanguageEngine
		│   • runs permissive parse
		│   • aggregates ISdlSemanticValidator (MEF)
		│   • maps byte offsets → 0-based line/character
		├────────────► editor: squiggles + Error List
		└────────────► brokered ISdlLanguageService (JSON-RPC)

EllipticBit.SDLang.VisualStudio.Contracts ── the public API you reference
```

The core library stays strict and secure by default; the editor opts into permissive parsing only for tooling.
See [Permissive Parsing](integration/permissive-parsing.md) for why that separation matters.
