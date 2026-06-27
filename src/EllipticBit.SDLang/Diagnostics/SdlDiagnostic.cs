namespace EllipticBit.SDLang;

/// <summary>
/// A single problem reported while parsing or analyzing an SDLang document.
/// </summary>
/// <remarks>
/// Diagnostics are produced by the parser only when <see cref="SdlReaderOptions.ErrorRecovery"/> is explicitly
/// enabled (see the diagnostic-returning <c>SdlDocument.Parse</c> overloads), and by external semantic analyzers
/// that contribute additional validation. Positions mirror <see cref="SdlReaderException"/>: <see cref="LineNumber"/>
/// and <see cref="LinePosition"/> are 1-based, while <see cref="BytePosition"/> is a 0-based offset into the UTF-8
/// source. <see cref="Length"/> is measured in UTF-8 bytes and may be <c>0</c> when an exact span is unknown.
/// </remarks>
public sealed class SdlDiagnostic
{
	/// <summary>Initializes a new instance of the <see cref="SdlDiagnostic"/> class.</summary>
	/// <param name="message">A human-readable description of the problem.</param>
	/// <param name="severity">The severity of the problem.</param>
	/// <param name="lineNumber">The 1-based line number where the problem starts.</param>
	/// <param name="linePosition">The 1-based character position within the line where the problem starts.</param>
	/// <param name="bytePosition">The 0-based byte offset into the UTF-8 source where the problem starts.</param>
	/// <param name="length">The length, in UTF-8 bytes, of the offending span; <c>0</c> when not known.</param>
	/// <param name="code">An optional stable diagnostic identifier (for example, <c>SDL0001</c>).</param>
	/// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
	public SdlDiagnostic(string message, SdlDiagnosticSeverity severity, long lineNumber, long linePosition, long bytePosition, int length = 0, string? code = null)
	{
		ArgumentNullException.ThrowIfNull(message);
		Message = message;
		Severity = severity;
		LineNumber = lineNumber;
		LinePosition = linePosition;
		BytePosition = bytePosition;
		Length = length;
		Code = code;
	}

	/// <summary>Gets a human-readable description of the problem.</summary>
	public string Message { get; }

	/// <summary>Gets the severity of the problem.</summary>
	public SdlDiagnosticSeverity Severity { get; }

	/// <summary>Gets the 1-based line number where the problem starts.</summary>
	public long LineNumber { get; }

	/// <summary>Gets the 1-based character position within the line where the problem starts.</summary>
	public long LinePosition { get; }

	/// <summary>Gets the 0-based byte offset into the UTF-8 source where the problem starts.</summary>
	public long BytePosition { get; }

	/// <summary>Gets the length, in UTF-8 bytes, of the offending span, or <c>0</c> when the exact span is unknown.</summary>
	public int Length { get; }

	/// <summary>Gets an optional stable diagnostic identifier (for example, <c>SDL0001</c>), or <see langword="null"/>.</summary>
	public string? Code { get; }

	/// <summary>Returns a string of the form <c>"Severity: message (Line L, Position P)."</c>.</summary>
	public override string ToString()
		=> $"{Severity}: {Message} (Line {LineNumber}, Position {LinePosition}).";
}
