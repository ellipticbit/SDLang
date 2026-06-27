namespace EllipticBit.SDLang.Tests;

[TestClass]
public sealed class ErrorRecoveryTests
{
	private static readonly SdlReaderOptions Permissive = new() { ErrorRecovery = true };

	[TestMethod]
	public void DefaultOptions_AreStrict_AndThrowOnFirstError()
	{
		// Security default: without explicitly opting in, malformed input must be rejected, not coerced.
		Assert.IsFalse(new SdlReaderOptions().ErrorRecovery);
		Assert.ThrowsException<SdlReaderException>(() => SdlDocument.Parse("alpha \"ok\"\n@bad\nbeta \"ok\"\n"));
	}

	[TestMethod]
	public void DiagnosticOverload_WithoutOptIn_StillThrows()
	{
		// Asking for diagnostics is not the opt-in; SdlReaderOptions.ErrorRecovery is. Strict still throws.
		Assert.ThrowsException<SdlReaderException>(
			() => SdlDocument.Parse("alpha \"ok\"\n@bad\n", out _));
	}

	[TestMethod]
	public void Permissive_WellFormedDocument_HasNoDiagnostics()
	{
		SdlDocument document = SdlDocument.Parse(
			"alpha \"ok\"\nbeta 42\n", out IReadOnlyList<SdlDiagnostic> diagnostics, Permissive);

		Assert.AreEqual(0, diagnostics.Count);
		Assert.AreEqual(2, document.Tags.Count);
		Assert.AreEqual("alpha", document.Tags[0].Name);
		Assert.AreEqual("ok", document.Tags[0].Value!.AsString());
	}

	[TestMethod]
	public void Permissive_CollectsMultipleDiagnostics_AndContinuesParsing()
	{
		SdlDocument document = SdlDocument.Parse(
			"alpha \"ok\"\n@bad1\ngamma \"ok\"\n@bad2\nomega \"ok\"\n",
			out IReadOnlyList<SdlDiagnostic> diagnostics,
			Permissive);

		// Both bad lines are reported, and all three valid tags survive recovery.
		Assert.AreEqual(2, diagnostics.Count);
		Assert.AreEqual(3, document.Tags.Count);
		Assert.AreEqual("alpha", document.Tags[0].Name);
		Assert.AreEqual("gamma", document.Tags[1].Name);
		Assert.AreEqual("omega", document.Tags[2].Name);
	}

	[TestMethod]
	public void Permissive_ReportsLineAndSeverity()
	{
		SdlDocument document = SdlDocument.Parse(
			"alpha \"ok\"\n@bad1\ngamma \"ok\"\n", out IReadOnlyList<SdlDiagnostic> diagnostics, Permissive);

		Assert.AreEqual(1, diagnostics.Count);
		Assert.AreEqual(SdlDiagnosticSeverity.Error, diagnostics[0].Severity);
		Assert.AreEqual(2, diagnostics[0].LineNumber);
		Assert.IsTrue(
			diagnostics[0].Message.Contains("Unexpected character", StringComparison.Ordinal),
			diagnostics[0].Message);
		Assert.AreEqual(2, document.Tags.Count);
	}

	[TestMethod]
	public void Permissive_DiagnosticMessage_OmitsLocationSuffix()
	{
		SdlDocument.Parse("a 1\n@bad\n", out IReadOnlyList<SdlDiagnostic> diagnostics, Permissive);

		// The clean message lives on Message; line/position are separate properties (no doubled "(Line ...)").
		Assert.AreEqual(1, diagnostics.Count);
		Assert.IsFalse(diagnostics[0].Message.Contains("Line ", StringComparison.Ordinal), diagnostics[0].Message);
	}

	[TestMethod]
	public void Permissive_RecoversInsideChildBlock()
	{
		SdlDocument document = SdlDocument.Parse(
			"root {\n\tgood \"v\"\n\t@bad\n\talso \"v\"\n}\ntrailer \"ok\"\n",
			out IReadOnlyList<SdlDiagnostic> diagnostics,
			Permissive);

		Assert.AreEqual(1, diagnostics.Count);
		Assert.AreEqual(2, document.Tags.Count);
		Assert.AreEqual("root", document.Tags[0].Name);
		Assert.AreEqual("trailer", document.Tags[1].Name);

		Tag root = document.Tags[0];
		Assert.AreEqual(2, root.Children.Count);
		Assert.AreEqual("good", root.Children[0].Name);
		Assert.AreEqual("also", root.Children[1].Name);
	}

	[TestMethod]
	public void Permissive_RecoversFromStrayCloseBrace()
	{
		SdlDocument document = SdlDocument.Parse(
			"alpha \"ok\"\nbeta }\ngamma \"ok\"\n", out IReadOnlyList<SdlDiagnostic> diagnostics, Permissive);

		Assert.AreEqual(1, diagnostics.Count);
		Assert.IsTrue(document.Tags.Count >= 2);
		Assert.AreEqual("alpha", document.Tags[0].Name);
		Assert.AreEqual("gamma", document.Tags[^1].Name);
	}
}
