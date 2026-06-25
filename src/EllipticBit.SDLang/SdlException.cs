namespace EllipticBit.SDLang;

/// <summary>
/// The base type for all exceptions thrown by the EllipticBit.SDLang library.
/// </summary>
public class SdlException : Exception
{
	/// <summary>Initializes a new instance of the <see cref="SdlException"/> class.</summary>
	public SdlException()
	{
	}

	/// <summary>Initializes a new instance of the <see cref="SdlException"/> class with a message.</summary>
	public SdlException(string message)
		: base(message)
	{
	}

	/// <summary>Initializes a new instance of the <see cref="SdlException"/> class with a message and inner exception.</summary>
	public SdlException(string message, Exception? innerException)
		: base(message, innerException)
	{
	}
}
