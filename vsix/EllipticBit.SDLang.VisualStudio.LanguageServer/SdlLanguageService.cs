using System.Threading;
using System.Threading.Tasks;

using EllipticBit.SDLang.VisualStudio;

namespace EllipticBit.SDLang.VisualStudio.LanguageServer;

/// <summary>
/// The brokered-service implementation of <see cref="ISdlLanguageService"/>. It is a thin adapter that forwards
/// every call to a shared <see cref="SdlLanguageEngine"/>, so consumers reaching SDLang over JSON-RPC (for
/// example, the Visual D <c>dub.sdl</c> plug-in via <c>IServiceBroker</c>) get exactly the same diagnostics,
/// completions, and hover content as the in-proc editor.
/// </summary>
/// <remarks>
/// The VSIX shell proffers this service under <see cref="SdlBrokeredServices.LanguageService"/>. Because only the
/// serializable contract types cross the JSON-RPC boundary, this adapter introduces no editor or parser types of
/// its own; all behavior lives in the engine.
/// </remarks>
public sealed class SdlLanguageService : ISdlLanguageService
{
	private readonly SdlLanguageEngine _engine;

	/// <summary>Initializes a new <see cref="SdlLanguageService"/> over the supplied engine.</summary>
	/// <param name="engine">The shared language engine that performs the actual work.</param>
	/// <exception cref="ArgumentNullException"><paramref name="engine"/> is <see langword="null"/>.</exception>
	public SdlLanguageService(SdlLanguageEngine engine)
	{
		_engine = engine ?? throw new ArgumentNullException(nameof(engine));
	}

	/// <inheritdoc />
	public Task<IReadOnlyList<SdlValidationDiagnostic>> ValidateAsync(string text, string? documentUri, CancellationToken cancellationToken)
		=> _engine.ValidateAsync(text, documentUri, cancellationToken);

	/// <inheritdoc />
	public Task<IReadOnlyList<SdlCompletionItem>> GetCompletionsAsync(SdlRequestContext context, CancellationToken cancellationToken)
		=> _engine.GetCompletionsAsync(context, cancellationToken);

	/// <inheritdoc />
	public Task<SdlHover?> GetHoverAsync(SdlRequestContext context, CancellationToken cancellationToken)
		=> _engine.GetHoverAsync(context, cancellationToken);
}
