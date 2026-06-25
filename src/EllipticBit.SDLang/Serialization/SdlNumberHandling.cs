namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Controls how numbers are read during deserialization. Mirrors
/// <c>System.Text.Json.Serialization.JsonNumberHandling</c>.
/// </summary>
[Flags]
public enum SdlNumberHandling
{
	/// <summary>Numbers are only read from SDL numeric literals.</summary>
	Strict = 0,

	/// <summary>Numbers may also be read from SDL string literals.</summary>
	AllowReadingFromString = 1,
}
