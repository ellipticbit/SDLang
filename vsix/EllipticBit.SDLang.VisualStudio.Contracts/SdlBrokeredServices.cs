using System.Threading;
using System.Threading.Tasks;

using Microsoft.ServiceHub.Framework;

namespace EllipticBit.SDLang.VisualStudio;

/// <summary>
/// Describes the SDLang brokered language service so that out-of-MEF consumers — including legacy in-proc
/// extensions such as the Visual D plug-in — can request SDLang parsing, validation, and IntelliSense for their
/// own documents (for example, <c>dub.sdl</c>) over JSON-RPC.
/// </summary>
/// <remarks>
/// Acquire the service from an <see cref="IServiceBroker"/> using <see cref="LanguageService"/>:
/// <code>
/// ISdlLanguageService? sdl = await serviceBroker.GetProxyAsync&lt;ISdlLanguageService&gt;(
///     SdlBrokeredServices.LanguageService, cancellationToken);
/// </code>
/// The extension proffers the service under the same descriptor. The moniker version is incremented only on
/// breaking changes to <see cref="ISdlLanguageService"/>.
/// </remarks>
public static class SdlBrokeredServices
{
	/// <summary>The unique moniker name of the SDLang brokered language service.</summary>
	public const string LanguageServiceMonikerName = "EllipticBit.SDLang.LanguageService";

	/// <summary>The current version of the SDLang brokered language service moniker.</summary>
	public const string LanguageServiceMonikerVersion = "1.0";

	/// <summary>
	/// Gets the descriptor used to proffer and consume <see cref="ISdlLanguageService"/>. The descriptor uses a
	/// UTF-8 message formatter and HTTP-like header delimiters for broad transport compatibility.
	/// </summary>
	public static ServiceRpcDescriptor LanguageService { get; } = new ServiceJsonRpcDescriptor(
		new ServiceMoniker(LanguageServiceMonikerName, new Version(LanguageServiceMonikerVersion)),
		ServiceJsonRpcDescriptor.Formatters.UTF8,
		ServiceJsonRpcDescriptor.MessageDelimiters.HttpLikeHeaders);
}

/// <summary>
/// The JSON-RPC surface of the SDLang brokered language service. Consumers pass document text and receive
/// SDLang diagnostics, completions, and hover content, reusing the same engine that powers the editor.
/// </summary>
/// <remarks>
/// All members are asynchronous and accept a <see cref="CancellationToken"/>. Because invocations cross a
/// JSON-RPC boundary, only the serializable contract types in this assembly are exchanged; no editor or parser
/// types leak across the wire. Implementations must be thread-safe.
/// </remarks>
public interface ISdlLanguageService
{
	/// <summary>
	/// Parses and validates the supplied SDLang text, returning both syntactic and semantic diagnostics.
	/// </summary>
	/// <param name="text">The full SDLang document text to validate.</param>
	/// <param name="documentUri">An optional URI or file path identifying the document, or <see langword="null"/>.</param>
	/// <param name="cancellationToken">A token to observe for cancellation.</param>
	/// <returns>A task that yields the diagnostics; empty when the document is valid.</returns>
	Task<IReadOnlyList<SdlValidationDiagnostic>> ValidateAsync(string text, string? documentUri, CancellationToken cancellationToken);

	/// <summary>
	/// Returns completion suggestions for the caret position described by <paramref name="context"/>.
	/// </summary>
	/// <param name="context">The document text and caret position for which completions are requested.</param>
	/// <param name="cancellationToken">A token to observe for cancellation.</param>
	/// <returns>A task that yields the completion items; empty when there are no suggestions.</returns>
	Task<IReadOnlyList<SdlCompletionItem>> GetCompletionsAsync(SdlRequestContext context, CancellationToken cancellationToken);

	/// <summary>
	/// Returns hover content for the caret position described by <paramref name="context"/>.
	/// </summary>
	/// <param name="context">The document text and caret position for which hover content is requested.</param>
	/// <param name="cancellationToken">A token to observe for cancellation.</param>
	/// <returns>A task that yields the hover content, or <see langword="null"/> when there is nothing to show.</returns>
	Task<SdlHover?> GetHoverAsync(SdlRequestContext context, CancellationToken cancellationToken);
}
