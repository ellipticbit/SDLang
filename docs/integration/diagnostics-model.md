# Diagnostics Model

All integration surfaces share one diagnostics model, defined in `EllipticBit.SDLang.VisualStudio.Contracts`.
Understanding its coordinate system and severities lets your validators place squiggles exactly where you intend.

## `SdlTextSpan` — zero-based, line/character

```csharp
public readonly struct SdlTextSpan
{
	public SdlTextSpan(int startLine, int startCharacter, int endLine, int endCharacter);
	public int StartLine { get; }
	public int StartCharacter { get; }
	public int EndLine { get; }
	public int EndCharacter { get; }
	public static SdlTextSpan AtPosition(int line, int character);
}
```

- Positions are **zero-based**, matching the Language Server Protocol: the first line is `0`, and the first
  character on a line is `0`.
- `EndCharacter` is **exclusive**.
- A **zero-length** span (start equals end) denotes a caret position; the editor widens it to a single character
  so the squiggle remains visible.
- Characters are counted in **UTF-16 code units** (the unit Visual Studio uses), so multi-byte and
  surrogate-pair text map correctly.

> **Note on the core library.** The core `EllipticBit.SDLang` parser reports positions as **1-based** line/column
> with **UTF-8 byte** offsets. The language engine translates those into the zero-based, UTF-16 `SdlTextSpan`
> used here, so as a validator author you only ever deal with editor coordinates.

## `SdlValidationSeverity`

```csharp
public enum SdlValidationSeverity
{
	Error = 1,        // red squiggle, Error List "Error"
	Warning = 2,      // green/orange squiggle, Error List "Warning"
	Information = 3,  // subtle indicator, Error List "Message"
	Hint = 4,         // gentle hint
}
```

The numeric values match the LSP `DiagnosticSeverity` enumeration, so they map cleanly onto editor squiggles and
Error List categories without remapping.

## `SdlValidationDiagnostic`

```csharp
public sealed class SdlValidationDiagnostic
{
	public SdlValidationDiagnostic(
		string message,
		SdlValidationSeverity severity,
		SdlTextSpan span,
		string? code = null,
		string? source = null);

	public string Message { get; }
	public SdlValidationSeverity Severity { get; }
	public SdlTextSpan Span { get; }
	public string? Code { get; }    // stable rule id, e.g. "DUB001"
	public string? Source { get; }  // display name of the producing validator
}
```

- **`Message`** is shown as the squiggle tooltip and the Error List text.
- **`Code`** appears in the Error List "Code" column; use a stable identifier per rule.
- **`Source`** identifies the producing validator in the Error List. Built-in parser diagnostics use the source
  `"sdlang"`.

## Completions and hover

```csharp
public sealed class SdlCompletionItem
{
	public SdlCompletionItem(string insertText, SdlCompletionKind kind,
		string? displayText = null, string? description = null);
}

public enum SdlCompletionKind { Tag, Attribute, Value, Namespace, Snippet, Keyword }

public sealed class SdlHover
{
	public SdlHover(string content, SdlTextSpan span, bool isMarkdown = false);
}

public sealed class SdlRequestContext
{
	public SdlRequestContext(string text, int line, int character, string? documentUri = null);
}
```

`SdlRequestContext` carries the full document text plus the zero-based caret `Line`/`Character` for which
completions or hover are requested, and an optional `DocumentUri` used for file-name scoping.

## Related

- [MEF Semantic Validators](mef-semantic-validators.md) — produce diagnostics from a VSIX.
- [Permissive Parsing](permissive-parsing.md) — how syntactic diagnostics are produced.
