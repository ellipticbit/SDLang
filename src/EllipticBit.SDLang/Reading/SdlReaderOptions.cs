namespace EllipticBit.SDLang;

/// <summary>
/// Configures the behavior and hardening limits of <see cref="Utf8SdlReader"/>.
/// </summary>
public sealed class SdlReaderOptions
{
	internal static readonly SdlReaderOptions Default = new();

	/// <summary>
	/// Gets or sets the maximum nesting depth of child blocks. Documents that exceed this depth are rejected
	/// to guard against stack-exhaustion attacks. Defaults to <c>64</c>.
	/// </summary>
	public int MaxDepth { get; set; } = 64;

	/// <summary>Gets or sets how comments are handled. Defaults to <see cref="SdlCommentHandling.Skip"/>.</summary>
	public SdlCommentHandling CommentHandling { get; set; } = SdlCommentHandling.Skip;
}
