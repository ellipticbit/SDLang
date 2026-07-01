# SDLang (Simple Declarative Language) for Visual Studio

A comprehensive language extension for [SDLang](https://sdlang.org/) (Simple Declarative Language) in Visual Studio. This extension provides first-class support for reading, writing, and understanding `.sdl` files directly within your IDE.

SDLang is an elegant, human-friendly declarative language that is conceptually similar to XML or JSON, but with a much cleaner syntax focused on developer ergonomics.

## Features

This extension brings modern language features to SDLang files within Visual Studio, making it easier than ever to work with SDLang documents:

### Syntax Highlighting
Full semantic coloring for all SDLang constructs, helping you easily distinguish between tags, attributes, values, namespaces, and blocks.

### Live Error and Syntax Validation
Powered by a dedicated Language Server and the lightning-fast `.NET` parser (`EllipticBit.SDLang`), the extension provides real-time validation of your `.sdl` files. 
- Diagnostic squiggles on syntax errors directly in the editor.
- detailed error entries in the Visual Studio Error List window.
- Protection against unterminated strings/comments, malformed values, and invalid byte sequences.

### Seamless Integration
The extension associates `.sdl` fles with an enhanced language editor experience automatically. It integrates cleanly with Visual Studio's TextMate grammar support and standard IDE features.

## What is SDLang?

A tag in SDLang has a name, optional values, optional attributes, and optional children. See for yourself:

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

## Ecosystem Support

This Visual Studio extension is built to accompany the [EllipticBit.SDLang](https://www.nuget.org/packages/EllipticBit.SDLang/) NuGet package, an ultra-fast, zero-allocation SDLang parser, mutable DOM, and JSON-like object serializer for modern .NET.

Working side-by-side with the package, this extension brings the same level of capability and type-safety check guarantees to your IDE's editing surface.

## Feedback and Contributions

If you find a bug or have a feature request, please head over to the [GitHub Repository](https://github.com/ellipticbit/SDLang) to submit an issue or open a pull request. Contributions are welcome!

---

**License:** Boost Software License 1.0 (BSL-1.0)
