using EllipticBit.SDLang;
using EllipticBit.SDLang.VisualStudio;

namespace EllipticBit.SDLang.VisualStudio.LanguageServer;

/// <summary>
/// Converts core parser <see cref="SdlDiagnostic"/> instances into the editor-facing
/// <see cref="SdlValidationDiagnostic"/> contract, mapping byte-based positions through an
/// <see cref="SdlPositionMap"/> and preserving severity and code.
/// </summary>
/// <remarks>
/// The two severity enumerations (<see cref="SdlDiagnosticSeverity"/> and <see cref="SdlValidationSeverity"/>)
/// are intentionally defined with the same numeric values (<c>Error = 1 … Hint = 4</c>, matching the Language
/// Server Protocol), so the mapping is a direct cast with a defensive fallback to
/// <see cref="SdlValidationSeverity.Error"/> for any unexpected value.
/// </remarks>
public static class SdlDiagnosticTranslator
{
	/// <summary>Identifies diagnostics produced by the built-in SDLang parser in <see cref="SdlValidationDiagnostic.Source"/>.</summary>
	public const string ParserSource = "sdlang";

	/// <summary>Maps a single core <see cref="SdlDiagnostic"/> to an editor-facing <see cref="SdlValidationDiagnostic"/>.</summary>
	/// <param name="diagnostic">The core diagnostic to translate.</param>
	/// <param name="positionMap">The position map for the document the diagnostic was produced from.</param>
	/// <returns>The translated diagnostic.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="diagnostic"/> or <paramref name="positionMap"/> is <see langword="null"/>.</exception>
	public static SdlValidationDiagnostic Translate(SdlDiagnostic diagnostic, SdlPositionMap positionMap)
	{
		if (diagnostic is null) throw new ArgumentNullException(nameof(diagnostic));
		if (positionMap is null) throw new ArgumentNullException(nameof(positionMap));

		SdlTextSpan span = positionMap.GetSpan(diagnostic.BytePosition, diagnostic.Length);
		return new SdlValidationDiagnostic(
			diagnostic.Message,
			MapSeverity(diagnostic.Severity),
			span,
			diagnostic.Code,
			ParserSource);
	}

	/// <summary>Maps a sequence of core diagnostics to editor-facing diagnostics in the same order.</summary>
	/// <param name="diagnostics">The core diagnostics to translate.</param>
	/// <param name="positionMap">The position map for the document the diagnostics were produced from.</param>
	/// <returns>The translated diagnostics, preserving input order.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="diagnostics"/> or <paramref name="positionMap"/> is <see langword="null"/>.</exception>
	public static IReadOnlyList<SdlValidationDiagnostic> Translate(IEnumerable<SdlDiagnostic> diagnostics, SdlPositionMap positionMap)
	{
		if (diagnostics is null) throw new ArgumentNullException(nameof(diagnostics));
		if (positionMap is null) throw new ArgumentNullException(nameof(positionMap));

		List<SdlValidationDiagnostic> results = [];
		foreach (SdlDiagnostic diagnostic in diagnostics)
		{
			results.Add(Translate(diagnostic, positionMap));
		}

		return results;
	}

	private static SdlValidationSeverity MapSeverity(SdlDiagnosticSeverity severity) => severity switch
	{
		SdlDiagnosticSeverity.Error => SdlValidationSeverity.Error,
		SdlDiagnosticSeverity.Warning => SdlValidationSeverity.Warning,
		SdlDiagnosticSeverity.Information => SdlValidationSeverity.Information,
		SdlDiagnosticSeverity.Hint => SdlValidationSeverity.Hint,
		_ => SdlValidationSeverity.Error,
	};
}
