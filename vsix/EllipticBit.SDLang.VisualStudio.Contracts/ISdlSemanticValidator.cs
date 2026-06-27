using System.Threading;
using System.Threading.Tasks;

namespace EllipticBit.SDLang.VisualStudio;

/// <summary>
/// A MEF extension point that contributes semantic validation, completions, and hover information for SDLang
/// documents, layered on top of the built-in syntactic parsing.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface and export it so the in-proc SDLang language server discovers and invokes it.
/// The built-in server always performs syntactic (parser) validation; semantic validators add domain-specific
/// rules on top. For example, a Visual D <c>dub.sdl</c> integration can validate that dependency versions exist.
/// </para>
/// <para>
/// Export with <see cref="System.ComponentModel.Composition.ExportAttribute"/> together with the metadata
/// attributes consumed through <see cref="ISdlSemanticValidatorMetadata"/>:
/// </para>
/// <code>
/// [Export(typeof(ISdlSemanticValidator))]
/// [SdlContentType(SdlContentTypes.ContentTypeName)]
/// [SdlFileName("dub.sdl")]
/// internal sealed class DubValidator : ISdlSemanticValidator { /* ... */ }
/// </code>
/// <para>
/// Implementations must be thread-safe and should honor the supplied <see cref="CancellationToken"/>, returning
/// promptly when cancellation is requested (the editor cancels superseded requests as the user types).
/// </para>
/// </remarks>
public interface ISdlSemanticValidator
{
	/// <summary>
	/// Validates the supplied document text and returns any semantic diagnostics. Called on the full document
	/// text whenever the buffer changes and validation is (re)scheduled.
	/// </summary>
	/// <param name="text">The full current text of the document.</param>
	/// <param name="documentUri">An optional URI or file path identifying the document, or <see langword="null"/>.</param>
	/// <param name="cancellationToken">A token that is signaled when the request is superseded or canceled.</param>
	/// <returns>
	/// A task that yields the diagnostics for the document. Return an empty collection (not <see langword="null"/>)
	/// when there are no problems.
	/// </returns>
	Task<IReadOnlyList<SdlValidationDiagnostic>> ValidateAsync(string text, string? documentUri, CancellationToken cancellationToken);

	/// <summary>
	/// Returns completion suggestions for the caret position described by <paramref name="context"/>.
	/// </summary>
	/// <param name="context">The document text and caret position for which completions are requested.</param>
	/// <param name="cancellationToken">A token that is signaled when the request is superseded or canceled.</param>
	/// <returns>
	/// A task that yields the completion items to merge into the list. Return an empty collection when this
	/// validator has no suggestions for the position.
	/// </returns>
	Task<IReadOnlyList<SdlCompletionItem>> GetCompletionsAsync(SdlRequestContext context, CancellationToken cancellationToken);

	/// <summary>
	/// Returns hover content for the caret position described by <paramref name="context"/>.
	/// </summary>
	/// <param name="context">The document text and caret position for which hover content is requested.</param>
	/// <param name="cancellationToken">A token that is signaled when the request is superseded or canceled.</param>
	/// <returns>
	/// A task that yields the hover content, or <see langword="null"/> when this validator has nothing to show
	/// at the position.
	/// </returns>
	Task<SdlHover?> GetHoverAsync(SdlRequestContext context, CancellationToken cancellationToken);
}
