namespace EllipticBit.SDLang;

/// <summary>
/// Controls how the reader treats SDLang comments (<c>//</c>, <c>#</c>, <c>--</c>, and <c>/* */</c>).
/// </summary>
public enum SdlCommentHandling : byte
{
	/// <summary>Comments are silently skipped (the default).</summary>
	Skip = 0,

	/// <summary>Encountering a comment raises an <see cref="SdlReaderException"/>.</summary>
	Disallow = 1,
}
