using EllipticBit.SDLang.VisualStudio;

namespace EllipticBit.SDLang.VisualStudio.LanguageServer;

/// <summary>
/// A concrete <see cref="ISdlSemanticValidatorMetadata"/> implementation used to build
/// <see cref="Lazy{T, TMetadata}"/> validator registrations outside of a MEF container — for example in unit
/// tests or when a host composes the <see cref="SdlLanguageEngine"/> manually. Inside Visual Studio, MEF
/// synthesizes an equivalent metadata view from the export attributes, so this type is not required there.
/// </summary>
public sealed class SdlSemanticValidatorMetadata : ISdlSemanticValidatorMetadata
{
	/// <summary>Initializes a new instance of the <see cref="SdlSemanticValidatorMetadata"/> class.</summary>
	/// <param name="contentTypes">The content types the validator applies to; must contain at least one entry.</param>
	/// <param name="fileNames">The optional file names the validator is scoped to, or <see langword="null"/> for all files.</param>
	/// <exception cref="ArgumentNullException"><paramref name="contentTypes"/> is <see langword="null"/>.</exception>
	public SdlSemanticValidatorMetadata(IReadOnlyList<string> contentTypes, IReadOnlyList<string>? fileNames = null)
	{
		ContentTypes = contentTypes ?? throw new ArgumentNullException(nameof(contentTypes));
		FileNames = fileNames;
	}

	/// <inheritdoc />
	public IReadOnlyList<string> ContentTypes { get; }

	/// <inheritdoc />
	public IReadOnlyList<string>? FileNames { get; }
}
