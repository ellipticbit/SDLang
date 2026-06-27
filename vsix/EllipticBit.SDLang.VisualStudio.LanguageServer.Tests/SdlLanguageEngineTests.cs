using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using EllipticBit.SDLang;
using EllipticBit.SDLang.VisualStudio;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EllipticBit.SDLang.VisualStudio.LanguageServer.Tests;

/// <summary>
/// Tests for the in-proc language engine: syntactic diagnostic production and translation into editor
/// (0-based, UTF-16) coordinates, UTF-8 byte-to-character position mapping (including multi-byte and
/// surrogate-pair text), MEF-style semantic validator aggregation, file-name scoping, validator isolation,
/// and the brokered-service adapter.
/// </summary>
[TestClass]
public sealed class SdlLanguageEngineTests
{
	private static SdlLanguageEngine CreateEngine(params Lazy<ISdlSemanticValidator, ISdlSemanticValidatorMetadata>[] validators)
		=> new(validators);

	// ---- Syntactic diagnostics -------------------------------------------------------------------------

	[TestMethod]
	public async Task ValidateAsync_ValidDocument_ReturnsNoDiagnostics()
	{
		SdlLanguageEngine engine = CreateEngine();

		IReadOnlyList<SdlValidationDiagnostic> diagnostics =
			await engine.ValidateAsync("alpha \"ok\"\nbeta 42\n", documentUri: null, CancellationToken.None);

		Assert.AreEqual(0, diagnostics.Count);
	}

	[TestMethod]
	public async Task ValidateAsync_InvalidDocument_ReportsParserDiagnostic_InZeroBasedCoordinates()
	{
		SdlLanguageEngine engine = CreateEngine();

		// '@bad1' on the SECOND line (1-based line 2) is invalid; the editor diagnostic must be 0-based line 1.
		IReadOnlyList<SdlValidationDiagnostic> diagnostics =
			await engine.ValidateAsync("alpha \"ok\"\n@bad1\ngamma \"ok\"\n", documentUri: null, CancellationToken.None);

		Assert.AreEqual(1, diagnostics.Count);
		Assert.AreEqual(SdlValidationSeverity.Error, diagnostics[0].Severity);
		Assert.AreEqual(SdlDiagnosticTranslator.ParserSource, diagnostics[0].Source);
		Assert.AreEqual(1, diagnostics[0].Span.StartLine);
	}

	[TestMethod]
	public async Task ValidateAsync_NullText_Throws()
	{
		SdlLanguageEngine engine = CreateEngine();

		await Assert.ThrowsExceptionAsync<ArgumentNullException>(
			() => engine.ValidateAsync(null!, documentUri: null, CancellationToken.None));
	}

	// ---- Semantic validator aggregation ----------------------------------------------------------------

	[TestMethod]
	public async Task ValidateAsync_AppendsValidatorDiagnostics_AfterSyntactic()
	{
		SdlValidationDiagnostic semantic = new(
			"semantic problem",
			SdlValidationSeverity.Warning,
			SdlTextSpan.AtPosition(0, 0),
			code: "DUB001",
			source: "dub");
		FakeValidator validator = new(diagnostics: new[] { semantic });
		SdlLanguageEngine engine = CreateEngine(SdlLanguageEngine.CreateRegistration(validator));

		// One syntactic error ('@bad') plus the validator's one semantic diagnostic.
		IReadOnlyList<SdlValidationDiagnostic> diagnostics =
			await engine.ValidateAsync("alpha \"ok\"\n@bad\n", documentUri: null, CancellationToken.None);

		Assert.AreEqual(2, diagnostics.Count);
		Assert.AreEqual(SdlDiagnosticTranslator.ParserSource, diagnostics[0].Source);
		Assert.AreEqual("dub", diagnostics[1].Source);
		Assert.AreEqual(1, validator.ValidateCalls);
	}

	[TestMethod]
	public async Task ValidateAsync_FaultyValidator_DoesNotSuppressBuiltInDiagnostics()
	{
		SdlLanguageEngine engine = CreateEngine(SdlLanguageEngine.CreateRegistration(new ThrowingValidator()));

		IReadOnlyList<SdlValidationDiagnostic> diagnostics =
			await engine.ValidateAsync("alpha \"ok\"\n@bad\n", documentUri: null, CancellationToken.None);

		// The validator throws, but the parser's diagnostic must still survive.
		Assert.AreEqual(1, diagnostics.Count);
		Assert.AreEqual(SdlDiagnosticTranslator.ParserSource, diagnostics[0].Source);
	}

	[TestMethod]
	public async Task ValidateAsync_FileNameScopedValidator_OnlyRunsForMatchingFile()
	{
		SdlValidationDiagnostic semantic = new("dub", SdlValidationSeverity.Information, SdlTextSpan.AtPosition(0, 0));
		FakeValidator validator = new(diagnostics: new[] { semantic });
		SdlLanguageEngine engine = CreateEngine(
			SdlLanguageEngine.CreateRegistration(validator, fileNames: new[] { "dub.sdl" }));

		IReadOnlyList<SdlValidationDiagnostic> matched =
			await engine.ValidateAsync("a 1\n", documentUri: "dub.sdl", CancellationToken.None);
		IReadOnlyList<SdlValidationDiagnostic> unmatched =
			await engine.ValidateAsync("a 1\n", documentUri: "app.sdl", CancellationToken.None);

		Assert.AreEqual(1, matched.Count, "validator should run for dub.sdl");
		Assert.AreEqual(0, unmatched.Count, "validator should not run for app.sdl");
		Assert.AreEqual(1, validator.ValidateCalls);
	}

	[TestMethod]
	public async Task GetCompletionsAsync_MergesAcrossValidators()
	{
		FakeValidator first = new(completions: new[] { new SdlCompletionItem("name", SdlCompletionKind.Attribute) });
		FakeValidator second = new(completions: new[] { new SdlCompletionItem("version", SdlCompletionKind.Attribute) });
		SdlLanguageEngine engine = CreateEngine(
			SdlLanguageEngine.CreateRegistration(first),
			SdlLanguageEngine.CreateRegistration(second));

		IReadOnlyList<SdlCompletionItem> items =
			await engine.GetCompletionsAsync(new SdlRequestContext("a 1\n", 0, 0), CancellationToken.None);

		Assert.AreEqual(2, items.Count);
	}

	[TestMethod]
	public async Task GetHoverAsync_ReturnsFirstNonNull_InRegistrationOrder()
	{
		FakeValidator noHover = new();
		FakeValidator withHover = new(hover: new SdlHover("docs", SdlTextSpan.AtPosition(0, 0)));
		SdlLanguageEngine engine = CreateEngine(
			SdlLanguageEngine.CreateRegistration(noHover),
			SdlLanguageEngine.CreateRegistration(withHover));

		SdlHover? hover = await engine.GetHoverAsync(new SdlRequestContext("a 1\n", 0, 0), CancellationToken.None);

		Assert.IsNotNull(hover);
		Assert.AreEqual("docs", hover!.Content);
	}

	// ---- Position mapping ------------------------------------------------------------------------------

	[TestMethod]
	public void PositionMap_MapsMultiByteUtf8_ByUtf16CharacterNotByte()
	{
		// 'π' (U+03C0) is two UTF-8 bytes but one UTF-16 char. The byte after it (offset 2) is character 1.
		SdlPositionMap map = new("π = 3\n");

		(int line, int character) = map.GetPosition(2);

		Assert.AreEqual(0, line);
		Assert.AreEqual(1, character);
	}

	[TestMethod]
	public void PositionMap_MapsSurrogatePairEmoji_ToTwoUtf16Characters()
	{
		// '😀' (U+1F600) is four UTF-8 bytes and two UTF-16 chars; the byte after it (offset 4) is character 2.
		SdlPositionMap map = new("😀x\n");

		(int line, int character) = map.GetPosition(4);

		Assert.AreEqual(0, line);
		Assert.AreEqual(2, character);
	}

	[TestMethod]
	public void PositionMap_MapsSecondLineStart_AfterMultiByteFirstLine()
	{
		// 'π' then newline; the start of the second line ('x') is line 1, character 0.
		SdlPositionMap map = new("π\nx\n");

		(int line, int character) = map.GetPosition(3);

		Assert.AreEqual(1, line);
		Assert.AreEqual(0, character);
	}

	[TestMethod]
	public void PositionMap_GetSpan_ZeroLength_ProducesCaret()
	{
		SdlPositionMap map = new("abc\n");

		SdlTextSpan span = map.GetSpan(1, 0);

		Assert.AreEqual(new SdlTextSpan(0, 1, 0, 1), span);
	}

	// ---- Diagnostic translation ------------------------------------------------------------------------

	[TestMethod]
	public void DiagnosticTranslator_MapsSeverityCodeAndSpan()
	{
		SdlPositionMap map = new("abc\ndefg\n");
		// 'd' starts the second line at byte offset 4; a length-3 span covers "def".
		SdlDiagnostic core = new("msg", SdlDiagnosticSeverity.Warning, lineNumber: 2, linePosition: 1, bytePosition: 4, length: 3, code: "SDL0001");

		SdlValidationDiagnostic mapped = SdlDiagnosticTranslator.Translate(core, map);

		Assert.AreEqual("msg", mapped.Message);
		Assert.AreEqual(SdlValidationSeverity.Warning, mapped.Severity);
		Assert.AreEqual("SDL0001", mapped.Code);
		Assert.AreEqual(SdlDiagnosticTranslator.ParserSource, mapped.Source);
		Assert.AreEqual(new SdlTextSpan(1, 0, 1, 3), mapped.Span);
	}

	// ---- Brokered service adapter ----------------------------------------------------------------------

	[TestMethod]
	public async Task BrokeredService_DelegatesValidationToEngine()
	{
		SdlLanguageEngine engine = CreateEngine();
		ISdlLanguageService service = new SdlLanguageService(engine);

		IReadOnlyList<SdlValidationDiagnostic> diagnostics =
			await service.ValidateAsync("alpha \"ok\"\n@bad\n", documentUri: null, CancellationToken.None);

		Assert.AreEqual(1, diagnostics.Count);
		Assert.AreEqual(SdlDiagnosticTranslator.ParserSource, diagnostics[0].Source);
	}

	// ---- Fakes -----------------------------------------------------------------------------------------

	private sealed class FakeValidator : ISdlSemanticValidator
	{
		private readonly IReadOnlyList<SdlValidationDiagnostic> _diagnostics;
		private readonly IReadOnlyList<SdlCompletionItem> _completions;
		private readonly SdlHover? _hover;

		public FakeValidator(
			IReadOnlyList<SdlValidationDiagnostic>? diagnostics = null,
			IReadOnlyList<SdlCompletionItem>? completions = null,
			SdlHover? hover = null)
		{
			_diagnostics = diagnostics ?? Array.Empty<SdlValidationDiagnostic>();
			_completions = completions ?? Array.Empty<SdlCompletionItem>();
			_hover = hover;
		}

		public int ValidateCalls { get; private set; }

		public Task<IReadOnlyList<SdlValidationDiagnostic>> ValidateAsync(string text, string? documentUri, CancellationToken cancellationToken)
		{
			ValidateCalls++;
			return Task.FromResult(_diagnostics);
		}

		public Task<IReadOnlyList<SdlCompletionItem>> GetCompletionsAsync(SdlRequestContext context, CancellationToken cancellationToken)
			=> Task.FromResult(_completions);

		public Task<SdlHover?> GetHoverAsync(SdlRequestContext context, CancellationToken cancellationToken)
			=> Task.FromResult<SdlHover?>(_hover);
	}

	private sealed class ThrowingValidator : ISdlSemanticValidator
	{
		public Task<IReadOnlyList<SdlValidationDiagnostic>> ValidateAsync(string text, string? documentUri, CancellationToken cancellationToken)
			=> throw new InvalidOperationException("boom");

		public Task<IReadOnlyList<SdlCompletionItem>> GetCompletionsAsync(SdlRequestContext context, CancellationToken cancellationToken)
			=> throw new InvalidOperationException("boom");

		public Task<SdlHover?> GetHoverAsync(SdlRequestContext context, CancellationToken cancellationToken)
			=> throw new InvalidOperationException("boom");
	}
}
