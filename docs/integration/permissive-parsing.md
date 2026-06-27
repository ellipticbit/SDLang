# Permissive Parsing

The SDLang editor experience depends on parsing documents that are **incomplete or invalid** — that is the
normal state of a file while you are typing. This page explains how the extension parses such documents to
produce diagnostics, and why that behavior is deliberately separate from the strict defaults of the core
`EllipticBit.SDLang` library.

## Two parsing modes, one parser

The core library is **strict and secure by default**. Its reader rejects malformed input by throwing
`SdlReaderException` at the first violation, which is the correct behavior for applications deserializing
untrusted SDLang: fail fast, surface nothing partial.

That is the wrong behavior for an editor, where a half-typed document must still yield as many useful squiggles
as possible. So the parser also supports an **opt-in permissive mode** that:

- continues past recoverable errors instead of throwing, and
- collects each problem as a structured diagnostic (message, severity, position) rather than an exception.

The same parser powers both modes — there is no second, divergent implementation to keep in sync.

## Where permissive parsing is enabled

Only the tooling layer opts in. The language engine
(`EllipticBit.SDLang.VisualStudio.LanguageServer.SdlLanguageEngine`) requests error recovery and the
diagnostic-returning parse path, then maps the results into the editor's coordinate system:

```
text ──► permissive parse (error recovery on)
	 ──► IReadOnlyList<SdlDiagnostic>        (1-based line/col, UTF-8 byte offsets)
	 ──► map to SdlValidationDiagnostic      (0-based line/char, UTF-16 — see Diagnostics Model)
	 ──► merge with ISdlSemanticValidator results
```

Applications that reference the core library directly are **unaffected**: they keep the strict, throwing default
unless they explicitly enable recovery.

## What counts as recoverable

The permissive parser aims to keep going wherever a sensible recovery point exists, for example:

- an unexpected character at the start of a statement,
- an unterminated quoted or backtick string,
- a malformed number, date, time, or duration literal,
- an attribute without a value, or a value without an attribute name,
- an unbalanced block (`{` without a matching `}`).

Each becomes a diagnostic anchored to the offending span, and parsing resumes at the next statement or line so a
single mistake does not cascade into noise across the rest of the file.

## Performance and cancellation

Validation runs **off the UI thread** and is **debounced** (a short delay after the last keystroke) so rapid
typing does not trigger redundant passes. Every pass observes a `CancellationToken`; when you keep typing, the
in-flight pass is cancelled and a new one is scheduled. Validators you contribute should honor the same token so
the whole pipeline stays responsive.

## Why the separation matters

- **Security.** Untrusted-input scenarios keep the strict, fail-fast parser. Permissive recovery never becomes
  the default for deserialization.
- **Fidelity.** The editor and the brokered service share the exact same engine and recovery rules, so what a
  user sees in squiggles matches what an automated consumer receives over JSON-RPC.
- **Maintainability.** One parser, two entry points — recovery is a mode, not a fork.

## Related

- [Diagnostics Model](diagnostics-model.md) — the coordinate translation referenced above.
- [MEF Semantic Validators](mef-semantic-validators.md) — add semantic diagnostics on top of syntactic ones.
