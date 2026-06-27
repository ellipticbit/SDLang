using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using EllipticBit.SDLang;
using EllipticBit.SDLang.VisualStudio;

namespace EllipticBit.SDLang.VisualStudio.LanguageServer;

/// <summary>
/// The in-proc SDLang language engine. It produces editor diagnostics by running the shared-source parser in
/// error-recovery mode and merging the results with any contributed <see cref="ISdlSemanticValidator"/> exports,
/// and it serves completions and hover by delegating to those same validators. Both the editor layer and the
/// brokered <see cref="ISdlLanguageService"/> adapter call this engine, so behavior is identical across surfaces.
/// </summary>
/// <remarks>
/// <para>
/// The engine is composed by MEF through its <see cref="ImportingConstructorAttribute"/>, which imports every
/// exported validator together with its <see cref="ISdlSemanticValidatorMetadata"/>. A validator is invoked for a
/// document only when its metadata declares the SDLang content type and, when file names are specified, includes
/// the document's file name (derived from the document URI).
/// </para>
/// <para>
/// Validators are isolated: an exception thrown by one validator is swallowed (so a faulty third-party extension
/// cannot suppress the built-in parser diagnostics), but <see cref="OperationCanceledException"/> is always
/// allowed to propagate so the editor can abandon superseded requests promptly.
/// </para>
/// </remarks>
public sealed class SdlLanguageEngine
{
	private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

	private readonly IReadOnlyList<Lazy<ISdlSemanticValidator, ISdlSemanticValidatorMetadata>> _validators;

	/// <summary>Initializes a new <see cref="SdlLanguageEngine"/> with the supplied validator registrations.</summary>
	/// <param name="validators">
	/// The semantic validators and their metadata. In Visual Studio this is satisfied by MEF; in tests or manual
	/// hosting it can be any collection, including the results of <see cref="CreateRegistration"/>.
	/// </param>
	/// <exception cref="ArgumentNullException"><paramref name="validators"/> is <see langword="null"/>.</exception>
	[ImportingConstructor]
	public SdlLanguageEngine(
		[ImportMany] IEnumerable<Lazy<ISdlSemanticValidator, ISdlSemanticValidatorMetadata>> validators)
	{
		if (validators is null) throw new ArgumentNullException(nameof(validators));
		_validators = [.. validators];
	}

	/// <summary>
	/// Creates a validator registration without a MEF container, for unit tests or manual composition. The
	/// content types default to the SDLang content type when not specified.
	/// </summary>
	/// <param name="validator">The validator instance to register.</param>
	/// <param name="contentTypes">The content types the validator applies to; defaults to the SDLang content type.</param>
	/// <param name="fileNames">The optional file names the validator is scoped to, or <see langword="null"/> for all files.</param>
	/// <returns>A lazy registration consumable by the <see cref="SdlLanguageEngine(IEnumerable{Lazy{ISdlSemanticValidator, ISdlSemanticValidatorMetadata}})"/> constructor.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="validator"/> is <see langword="null"/>.</exception>
	public static Lazy<ISdlSemanticValidator, ISdlSemanticValidatorMetadata> CreateRegistration(
		ISdlSemanticValidator validator,
		IReadOnlyList<string>? contentTypes = null,
		IReadOnlyList<string>? fileNames = null)
	{
		if (validator is null) throw new ArgumentNullException(nameof(validator));
		SdlSemanticValidatorMetadata metadata = new(contentTypes ?? new[] { SdlContentTypes.ContentTypeName }, fileNames);
		return new Lazy<ISdlSemanticValidator, ISdlSemanticValidatorMetadata>(() => validator, metadata);
	}

	/// <summary>
	/// Parses and validates the supplied SDLang text, returning the built-in syntactic diagnostics followed by any
	/// diagnostics contributed by matching semantic validators, all in editor (0-based) coordinates.
	/// </summary>
	/// <param name="text">The full document text.</param>
	/// <param name="documentUri">An optional URI or path used to scope file-name-specific validators, or <see langword="null"/>.</param>
	/// <param name="cancellationToken">A token observed for cancellation.</param>
	/// <returns>The merged diagnostics; empty when the document is valid and no validator reports a problem.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
	public async Task<IReadOnlyList<SdlValidationDiagnostic>> ValidateAsync(string text, string? documentUri, CancellationToken cancellationToken)
	{
		if (text is null) throw new ArgumentNullException(nameof(text));
		cancellationToken.ThrowIfCancellationRequested();

		byte[] utf8 = Utf8.GetBytes(text);
		SdlPositionMap map = new(utf8);

		SdlReaderOptions options = new() { ErrorRecovery = true };
		SdlDocument.Parse(utf8, out IReadOnlyList<SdlDiagnostic> syntactic, options);

		List<SdlValidationDiagnostic> results = [.. SdlDiagnosticTranslator.Translate(syntactic, map)];

		string? fileName = GetFileName(documentUri);
		foreach (Lazy<ISdlSemanticValidator, ISdlSemanticValidatorMetadata> registration in GetMatching(fileName))
		{
			cancellationToken.ThrowIfCancellationRequested();
			IReadOnlyList<SdlValidationDiagnostic>? contributed;
			try
			{
				contributed = await registration.Value.ValidateAsync(text, documentUri, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				continue;
			}

			if (contributed is { Count: > 0 })
			{
				results.AddRange(contributed);
			}
		}

		return results;
	}

	/// <summary>Returns the merged completion suggestions from all matching semantic validators.</summary>
	/// <param name="context">The document text and caret position for which completions are requested.</param>
	/// <param name="cancellationToken">A token observed for cancellation.</param>
	/// <returns>The merged completion items; empty when no validator contributes any.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
	public async Task<IReadOnlyList<SdlCompletionItem>> GetCompletionsAsync(SdlRequestContext context, CancellationToken cancellationToken)
	{
		if (context is null) throw new ArgumentNullException(nameof(context));
		cancellationToken.ThrowIfCancellationRequested();

		List<SdlCompletionItem> results = [];
		string? fileName = GetFileName(context.DocumentUri);
		foreach (Lazy<ISdlSemanticValidator, ISdlSemanticValidatorMetadata> registration in GetMatching(fileName))
		{
			cancellationToken.ThrowIfCancellationRequested();
			IReadOnlyList<SdlCompletionItem>? items;
			try
			{
				items = await registration.Value.GetCompletionsAsync(context, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				continue;
			}

			if (items is { Count: > 0 })
			{
				results.AddRange(items);
			}
		}

		return results;
	}

	/// <summary>Returns hover content from the first matching validator that produces any, in registration order.</summary>
	/// <param name="context">The document text and caret position for which hover content is requested.</param>
	/// <param name="cancellationToken">A token observed for cancellation.</param>
	/// <returns>The first non-<see langword="null"/> hover content, or <see langword="null"/> when none is available.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
	public async Task<SdlHover?> GetHoverAsync(SdlRequestContext context, CancellationToken cancellationToken)
	{
		if (context is null) throw new ArgumentNullException(nameof(context));
		cancellationToken.ThrowIfCancellationRequested();

		string? fileName = GetFileName(context.DocumentUri);
		foreach (Lazy<ISdlSemanticValidator, ISdlSemanticValidatorMetadata> registration in GetMatching(fileName))
		{
			cancellationToken.ThrowIfCancellationRequested();
			SdlHover? hover;
			try
			{
				hover = await registration.Value.GetHoverAsync(context, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				continue;
			}

			if (hover is not null)
			{
				return hover;
			}
		}

		return null;
	}

	private IEnumerable<Lazy<ISdlSemanticValidator, ISdlSemanticValidatorMetadata>> GetMatching(string? fileName)
	{
		foreach (Lazy<ISdlSemanticValidator, ISdlSemanticValidatorMetadata> registration in _validators)
		{
			if (Matches(registration.Metadata, fileName))
			{
				yield return registration;
			}
		}
	}

	private static bool Matches(ISdlSemanticValidatorMetadata metadata, string? fileName)
	{
		if (metadata?.ContentTypes is null)
		{
			return false;
		}

		bool contentMatch = false;
		foreach (string contentType in metadata.ContentTypes)
		{
			if (string.Equals(contentType, SdlContentTypes.ContentTypeName, StringComparison.OrdinalIgnoreCase))
			{
				contentMatch = true;
				break;
			}
		}

		if (!contentMatch)
		{
			return false;
		}

		IReadOnlyList<string>? fileNames = metadata.FileNames;
		if (fileNames is null || fileNames.Count == 0)
		{
			return true;
		}

		if (fileName is null)
		{
			return false;
		}

		foreach (string candidate in fileNames)
		{
			if (string.Equals(candidate, fileName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static string? GetFileName(string? documentUri)
	{
		if (string.IsNullOrEmpty(documentUri))
		{
			return null;
		}

		string path = documentUri!;
		if (Uri.TryCreate(documentUri, UriKind.Absolute, out Uri? uri) && uri.IsFile)
		{
			path = uri.LocalPath;
		}

		try
		{
			string name = Path.GetFileName(path);
			return string.IsNullOrEmpty(name) ? null : name;
		}
		catch (ArgumentException)
		{
			return null;
		}
	}
}
