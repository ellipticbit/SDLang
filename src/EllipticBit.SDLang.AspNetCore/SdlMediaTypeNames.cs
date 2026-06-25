namespace EllipticBit.SDLang.AspNetCore;

/// <summary>
/// The media (MIME) type names and file extension used by the SDLang ASP.NET Core integration when SDLang is
/// negotiated as an HTTP request or response document language.
/// </summary>
public static class SdlMediaTypeNames
{
	/// <summary>The primary SDLang media type, <c>application/sdlang</c>.</summary>
	public const string Application = "application/sdlang";

	/// <summary>The text-oriented SDLang media type, <c>text/sdlang</c>.</summary>
	public const string Text = "text/sdlang";

	/// <summary>The conventional file extension for SDLang documents (without a leading dot), used for format mapping.</summary>
	public const string FileExtension = "sdl";
}
