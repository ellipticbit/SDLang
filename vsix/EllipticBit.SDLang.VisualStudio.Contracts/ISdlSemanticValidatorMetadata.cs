using System.ComponentModel;
using System.ComponentModel.Composition;

namespace EllipticBit.SDLang.VisualStudio;

/// <summary>
/// The MEF metadata that targets an <see cref="ISdlSemanticValidator"/> export at specific documents. The
/// language server imports validators alongside this metadata view and only invokes a validator when the
/// document matches its declared content types and, when specified, its file names.
/// </summary>
/// <remarks>
/// Populate this metadata by applying <see cref="SdlContentTypeAttribute"/> (one or more) and, optionally,
/// <see cref="SdlFileNameAttribute"/> (one or more) to the export. When no file names are specified, the
/// validator applies to every document of the declared content types.
/// </remarks>
public interface ISdlSemanticValidatorMetadata
{
	/// <summary>
	/// Gets the content-type names the validator applies to (for example, <see cref="SdlContentTypes.ContentTypeName"/>).
	/// </summary>
	IReadOnlyList<string> ContentTypes { get; }

	/// <summary>
	/// Gets the specific file names (case-insensitive, for example <c>dub.sdl</c>) the validator is scoped to, or an
	/// empty list to apply to all documents of the declared <see cref="ContentTypes"/>.
	/// </summary>
	[DefaultValue(null)]
	IReadOnlyList<string>? FileNames { get; }
}

/// <summary>
/// Targets an <see cref="ISdlSemanticValidator"/> export at a Visual Studio content type. Apply once per content
/// type the validator supports; at least one is required.
/// </summary>
[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class SdlContentTypeAttribute : Attribute
{
	/// <summary>Initializes a new instance of the <see cref="SdlContentTypeAttribute"/> class.</summary>
	/// <param name="contentType">The content-type name to target, typically <see cref="SdlContentTypes.ContentTypeName"/>.</param>
	/// <exception cref="ArgumentNullException"><paramref name="contentType"/> is <see langword="null"/>.</exception>
	public SdlContentTypeAttribute(string contentType)
	{
		ContentTypes = new[] { contentType ?? throw new ArgumentNullException(nameof(contentType)) };
	}

	/// <summary>Gets the single-element content-type collection contributed to <see cref="ISdlSemanticValidatorMetadata.ContentTypes"/>.</summary>
	public IReadOnlyList<string> ContentTypes { get; }
}

/// <summary>
/// Optionally scopes an <see cref="ISdlSemanticValidator"/> export to specific file names (for example,
/// <c>dub.sdl</c>). Apply once per file name. When omitted, the validator applies to all documents of its
/// declared content types.
/// </summary>
[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class SdlFileNameAttribute : Attribute
{
	/// <summary>Initializes a new instance of the <see cref="SdlFileNameAttribute"/> class.</summary>
	/// <param name="fileName">The file name to scope the validator to, compared case-insensitively (for example, <c>dub.sdl</c>).</param>
	/// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <see langword="null"/>.</exception>
	public SdlFileNameAttribute(string fileName)
	{
		FileNames = new[] { fileName ?? throw new ArgumentNullException(nameof(fileName)) };
	}

	/// <summary>Gets the single-element file-name collection contributed to <see cref="ISdlSemanticValidatorMetadata.FileNames"/>.</summary>
	public IReadOnlyList<string> FileNames { get; }
}
