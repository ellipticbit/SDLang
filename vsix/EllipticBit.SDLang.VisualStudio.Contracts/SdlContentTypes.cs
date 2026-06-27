namespace EllipticBit.SDLang.VisualStudio;

/// <summary>
/// Well-known names used to target the SDLang language in Visual Studio. Use these constants instead of
/// hard-coding literals so MEF exports, content-type definitions, and brokered-service consumers stay in sync.
/// </summary>
/// <remarks>
/// The editor identifies SDLang documents through the content type named <see cref="ContentTypeName"/>. The
/// extension projects map the <c>.sdl</c> and <c>.sdlang</c> file extensions to that content type, and the
/// in-proc language server applies the same TextMate grammar and language configuration to every buffer of
/// that content type. External extensions (for example, a Visual D <c>dub.sdl</c> integration) can reuse these
/// values to participate in the same experience.
/// </remarks>
public static class SdlContentTypes
{
	/// <summary>
	/// The Visual Studio content-type name for SDLang documents: <c>sdlang</c>. This is the value used in
	/// <c>[ContentType("sdlang")]</c> export attributes and in <see cref="ISdlSemanticValidatorMetadata.ContentTypes"/>.
	/// </summary>
	public const string ContentTypeName = "sdlang";

	/// <summary>
	/// The TextMate scope name applied to SDLang documents: <c>source.sdl</c>. Grammars and themes key off this scope.
	/// </summary>
	public const string TextMateScopeName = "source.sdl";

	/// <summary>The canonical primary file extension for SDLang documents, including the leading dot: <c>.sdl</c>.</summary>
	public const string FileExtension = ".sdl";

	/// <summary>The alternate, explicit file extension for SDLang documents, including the leading dot: <c>.sdlang</c>.</summary>
	public const string AlternateFileExtension = ".sdlang";

	/// <summary>
	/// All file extensions associated with the <see cref="ContentTypeName"/> content type, each including the
	/// leading dot. The order is stable: primary extension first.
	/// </summary>
	public static IReadOnlyList<string> FileExtensions { get; } = new[] { FileExtension, AlternateFileExtension };
}
