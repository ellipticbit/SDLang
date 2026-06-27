namespace EllipticBit.SDLang;

/// <summary>
/// The exception thrown when SDLang text is malformed or violates a configured hardening limit.
/// </summary>
public sealed class SdlReaderException : SdlException
{
	/// <summary>Gets the 1-based line number where the error occurred.</summary>
	public long LineNumber { get; }

	/// <summary>Gets the 1-based character position within the line where the error occurred.</summary>
	public long LinePosition { get; }

	/// <summary>Gets the 0-based byte offset within the document where the error occurred.</summary>
	public long BytePosition { get; }

	/// <summary>Gets the original message without the appended "(Line, Position)" location suffix.</summary>
	internal string RawMessage { get; }

	/// <summary>Initializes a new instance of the <see cref="SdlReaderException"/> class.</summary>
	public SdlReaderException(string message, long lineNumber, long linePosition, long bytePosition)
		: base(BuildMessage(message, lineNumber, linePosition))
	{
		RawMessage = message;
		LineNumber = lineNumber;
		LinePosition = linePosition;
		BytePosition = bytePosition;
	}

	private static string BuildMessage(string message, long lineNumber, long linePosition)
		=> $"{message} (Line {lineNumber}, Position {linePosition}).";
}
