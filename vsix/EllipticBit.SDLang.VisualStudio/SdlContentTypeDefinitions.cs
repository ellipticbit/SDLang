using System.ComponentModel.Composition;

using EllipticBit.SDLang.VisualStudio;

using Microsoft.VisualStudio.Utilities;

namespace EllipticBit.SDLang.VisualStudio;

/// <summary>
/// MEF definitions that introduce the SDLang content type to the Visual Studio editor and associate the
/// <c>.sdl</c> and <c>.sdlang</c> file extensions with it.
/// </summary>
/// <remarks>
/// The content-type name is <see cref="SdlContentTypes.ContentTypeName"/> (<c>sdlang</c>) and derives from the
/// editor's <c>code</c> base content type so SDLang buffers participate in standard editor features (selection,
/// outlining, bracket matching, and the TextMate grammar registered for <see cref="SdlContentTypes.TextMateScopeName"/>).
/// These fields are discovered by MEF; they are never referenced directly from code, which is why they are
/// assigned <see langword="null"/> with the warning suppressed.
/// </remarks>
internal static class SdlContentTypeDefinitions
{
#pragma warning disable CS0649 // Fields are populated by the MEF composition engine, not by code.
#pragma warning disable IDE0044 // MEF export fields cannot be readonly.

	/// <summary>Defines the <c>sdlang</c> content type, based on the editor's <c>code</c> content type.</summary>
	[Export]
	[Name(SdlContentTypes.ContentTypeName)]
	[BaseDefinition("code")]
	internal static ContentTypeDefinition? SdlContentTypeDefinition;

	/// <summary>Maps the primary <c>.sdl</c> extension to the <c>sdlang</c> content type.</summary>
	[Export]
	[FileExtension(SdlContentTypes.FileExtension)]
	[ContentType(SdlContentTypes.ContentTypeName)]
	internal static FileExtensionToContentTypeDefinition? SdlFileExtensionDefinition;

	/// <summary>Maps the alternate <c>.sdlang</c> extension to the <c>sdlang</c> content type.</summary>
	[Export]
	[FileExtension(SdlContentTypes.AlternateFileExtension)]
	[ContentType(SdlContentTypes.ContentTypeName)]
	internal static FileExtensionToContentTypeDefinition? SdlAlternateFileExtensionDefinition;

#pragma warning restore IDE0044
#pragma warning restore CS0649
}
