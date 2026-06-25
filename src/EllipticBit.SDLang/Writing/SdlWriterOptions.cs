namespace EllipticBit.SDLang;

/// <summary>
/// Configures how <see cref="Utf8SdlWriter"/> formats output.
/// </summary>
public sealed class SdlWriterOptions
{
	internal static readonly SdlWriterOptions Default = new();

	/// <summary>
	/// Gets or sets a value indicating whether output is pretty-printed with indentation. Mirrors
	/// <c>JsonSerializerOptions.WriteIndented</c>. When <see langword="false"/> (the default) lines carry no
	/// leading indentation. Tags are always newline-delimited so the output remains valid SDLang.
	/// </summary>
	public bool Indented { get; set; }

	/// <summary>Gets or sets the number of <see cref="IndentCharacter"/>s written per indent level. Defaults to <c>2</c>.</summary>
	public int IndentSize { get; set; } = 2;

	/// <summary>Gets or sets the character used for indentation. Must be a space or a tab. Defaults to a space.</summary>
	public char IndentCharacter { get; set; } = ' ';
}
