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

	/// <summary>
	/// Gets or sets a value indicating whether the parser recovers from errors instead of throwing on the first
	/// one. When <see langword="true"/>, the parser records each problem as an <see cref="SdlDiagnostic"/>,
	/// resynchronizes at the next statement boundary, and produces a best-effort document object model; the
	/// collected diagnostics are surfaced through the diagnostic-returning <c>SdlDocument.Parse</c> overloads.
	/// Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This option is <b>opt-in for security reasons</b>. The strict default (throw on the first error) ensures
	/// malformed or hostile input is rejected rather than silently coerced into a partial document. Error recovery
	/// is intended for tooling scenarios — editors, language servers, and linters — that must keep analyzing a
	/// document after the first mistake. <b>Do not enable it on trusted/production ingestion paths</b> where a
	/// best-effort parse of invalid input could mask data-integrity problems.
	/// </para>
	/// </remarks>
	public bool ErrorRecovery { get; set; }
}
