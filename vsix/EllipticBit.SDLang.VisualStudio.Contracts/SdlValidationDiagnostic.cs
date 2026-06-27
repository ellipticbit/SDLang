namespace EllipticBit.SDLang.VisualStudio;

/// <summary>
/// The severity of a semantic <see cref="SdlValidationDiagnostic"/> contributed by an
/// <see cref="ISdlSemanticValidator"/>. Values align with the Language Server Protocol's
/// <c>DiagnosticSeverity</c> so they map cleanly onto editor squiggles and Error List entries.
/// </summary>
public enum SdlValidationSeverity
{
	/// <summary>A problem that should block the document from being considered valid. Rendered as a red squiggle.</summary>
	Error = 1,

	/// <summary>A likely problem that does not necessarily block use. Rendered as a green/orange squiggle.</summary>
	Warning = 2,

	/// <summary>Informational guidance that is not a problem. Rendered as a subtle indicator.</summary>
	Information = 3,

	/// <summary>A gentle hint, typically surfaced only on hover or as a faded indicator.</summary>
	Hint = 4,
}

/// <summary>
/// A zero-based, line/character text range within an SDLang document, expressed independently of any specific
/// editor type so the contracts assembly stays free of editor dependencies.
/// </summary>
/// <remarks>
/// Positions are zero-based to match the Language Server Protocol: the first line is line <c>0</c> and the first
/// character on a line is character <c>0</c>. <see cref="EndCharacter"/> is exclusive. A zero-length span (where
/// start equals end) denotes a caret position rather than a selection.
/// </remarks>
public readonly struct SdlTextSpan : IEquatable<SdlTextSpan>
{
	/// <summary>Initializes a new <see cref="SdlTextSpan"/>.</summary>
	/// <param name="startLine">The zero-based line on which the span starts.</param>
	/// <param name="startCharacter">The zero-based character at which the span starts.</param>
	/// <param name="endLine">The zero-based line on which the span ends.</param>
	/// <param name="endCharacter">The zero-based, exclusive character at which the span ends.</param>
	public SdlTextSpan(int startLine, int startCharacter, int endLine, int endCharacter)
	{
		StartLine = startLine;
		StartCharacter = startCharacter;
		EndLine = endLine;
		EndCharacter = endCharacter;
	}

	/// <summary>Gets the zero-based line on which the span starts.</summary>
	public int StartLine { get; }

	/// <summary>Gets the zero-based character at which the span starts.</summary>
	public int StartCharacter { get; }

	/// <summary>Gets the zero-based line on which the span ends.</summary>
	public int EndLine { get; }

	/// <summary>Gets the zero-based, exclusive character at which the span ends.</summary>
	public int EndCharacter { get; }

	/// <summary>Creates a zero-length span (a caret position) at the supplied line and character.</summary>
	/// <param name="line">The zero-based line.</param>
	/// <param name="character">The zero-based character.</param>
	/// <returns>A zero-length <see cref="SdlTextSpan"/> at the given position.</returns>
	public static SdlTextSpan AtPosition(int line, int character) => new(line, character, line, character);

	/// <inheritdoc />
	public bool Equals(SdlTextSpan other)
		=> StartLine == other.StartLine
		&& StartCharacter == other.StartCharacter
		&& EndLine == other.EndLine
		&& EndCharacter == other.EndCharacter;

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is SdlTextSpan other && Equals(other);

	/// <inheritdoc />
	public override int GetHashCode()
	{
		unchecked
		{
			int hash = 17;
			hash = (hash * 31) + StartLine;
			hash = (hash * 31) + StartCharacter;
			hash = (hash * 31) + EndLine;
			hash = (hash * 31) + EndCharacter;
			return hash;
		}
	}

	/// <summary>Determines whether two spans are equal.</summary>
	public static bool operator ==(SdlTextSpan left, SdlTextSpan right) => left.Equals(right);

	/// <summary>Determines whether two spans are not equal.</summary>
	public static bool operator !=(SdlTextSpan left, SdlTextSpan right) => !left.Equals(right);

	/// <summary>Returns a string of the form <c>(startLine,startCharacter)-(endLine,endCharacter)</c>.</summary>
	public override string ToString() => $"({StartLine},{StartCharacter})-({EndLine},{EndCharacter})";
}

/// <summary>
/// A single semantic problem reported by an <see cref="ISdlSemanticValidator"/>. The in-proc language server
/// converts these into editor squiggles and Error List entries.
/// </summary>
public sealed class SdlValidationDiagnostic
{
	/// <summary>Initializes a new instance of the <see cref="SdlValidationDiagnostic"/> class.</summary>
	/// <param name="message">A human-readable description of the problem.</param>
	/// <param name="severity">The severity of the problem.</param>
	/// <param name="span">The range of text the problem applies to.</param>
	/// <param name="code">An optional stable identifier for the rule that produced the diagnostic.</param>
	/// <param name="source">An optional display name identifying the validator that produced the diagnostic.</param>
	/// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
	public SdlValidationDiagnostic(string message, SdlValidationSeverity severity, SdlTextSpan span, string? code = null, string? source = null)
	{
		Message = message ?? throw new ArgumentNullException(nameof(message));
		Severity = severity;
		Span = span;
		Code = code;
		Source = source;
	}

	/// <summary>Gets a human-readable description of the problem.</summary>
	public string Message { get; }

	/// <summary>Gets the severity of the problem.</summary>
	public SdlValidationSeverity Severity { get; }

	/// <summary>Gets the range of text the problem applies to.</summary>
	public SdlTextSpan Span { get; }

	/// <summary>Gets an optional stable identifier for the rule that produced the diagnostic, or <see langword="null"/>.</summary>
	public string? Code { get; }

	/// <summary>Gets an optional display name identifying the validator that produced the diagnostic, or <see langword="null"/>.</summary>
	public string? Source { get; }
}
