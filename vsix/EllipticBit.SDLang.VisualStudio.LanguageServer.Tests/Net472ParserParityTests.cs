using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using EllipticBit.SDLang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EllipticBit.SDLang.VisualStudio.LanguageServer.Tests;

/// <summary>
/// Runtime validation that the net472 shared-source build of the parser (EllipticBit.SDLang.Analysis) behaves
/// identically to the net10.0 library. These tests do not target the language server directly; instead they
/// exercise every hand-written net472 Compat shim through the public parser/DOM/writer surface so that a faulty
/// shim is caught here rather than inside Visual Studio.
///
/// Shim coverage map:
///   Rune (ASCII + multi-byte DecodeFromUtf8/EncodeToUtf8) ....... char-literal round trips
///   SearchValues&lt;byte&gt;.IndexOfAny ........................... quoted-string scanning
///   ArrayBufferWriter&lt;byte&gt; (WrittenSpan/WrittenMemory) ..... ToSdlString / WriteToAsync
///   HashCode.Combine + HashCode.AddBytes ....................... SdlValue.GetHashCode for scalar + binary
///   RuntimeHelpers.GetSubArray (array range indexing) .......... base64 binary decoding
///   ReadOnlySpan&lt;byte&gt;.TrimStart(byte) ...................... date-time literal parsing
///   ArgumentNullException/ArgumentException guards ............. null-argument validation
/// </summary>
[TestClass]
public sealed class Net472ParserParityTests
{
	[TestMethod]
	public void AsciiCharLiteral_RoundTrips_ThroughRuneShim()
	{
		SdlDocument document = SdlDocument.Parse("c 'x'\n");

		Assert.AreEqual(SdlValueKind.Char, document.Tags[0].Value!.Kind);
		Assert.AreEqual("x", document.Tags[0].Value!.AsString());

		string sdl = document.ToSdlString();
		SdlDocument reparsed = SdlDocument.Parse(sdl);
		Assert.AreEqual(SdlValueKind.Char, reparsed.Tags[0].Value!.Kind);
		Assert.AreEqual("x", reparsed.Tags[0].Value!.AsString());
	}

	[TestMethod]
	public void MultiByteCharLiteral_RoundTrips_ThroughRuneShim()
	{
		// U+1F600 is a 4-byte UTF-8 sequence: exercises Rune.DecodeFromUtf8 + EncodeToUtf8 surrogate handling.
		SdlDocument document = SdlDocument.Parse("c '\U0001F600'\n");
		Assert.AreEqual(SdlValueKind.Char, document.Tags[0].Value!.Kind);
		Assert.AreEqual("\U0001F600", document.Tags[0].Value!.AsString());

		string sdl = document.ToSdlString();
		SdlDocument reparsed = SdlDocument.Parse(sdl);
		Assert.AreEqual("\U0001F600", reparsed.Tags[0].Value!.AsString());
	}

	[TestMethod]
	public void QuotedString_WithEscapes_RoundTrips_ThroughSearchValuesShim()
	{
		// The quoted-string scanner uses SearchValues<byte>.IndexOfAny to find quote/backslash/line-break stops.
		SdlDocument document = SdlDocument.Parse("greeting \"Hello,\\tthere\\n\\\"world\\\"\"\n");

		Assert.AreEqual("Hello,\tthere\n\"world\"", document.Tags[0].Value!.AsString());
	}

	[TestMethod]
	public void Binary_Base64_Decodes_ThroughGetSubArrayShim()
	{
		// Base64 decoding trims the result array via `decoded[..written]` (RuntimeHelpers.GetSubArray).
		SdlDocument document = SdlDocument.Parse("data [aGVsbG8=]\n");

		Assert.AreEqual(SdlValueKind.Binary, document.Tags[0].Value!.Kind);
		CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("hello"), document.Tags[0].Value!.AsBytes());
	}

	[TestMethod]
	public void DateTime_WithTime_Parses_ThroughTrimStartShim()
	{
		// Date/time parsing splits on the space and calls ReadOnlySpan<byte>.TrimStart(Space).
		SdlDocument document = SdlDocument.Parse("ts 2015/12/06 12:30:00\n");

		Assert.AreEqual(SdlValueKind.DateTime, document.Tags[0].Value!.Kind);
	}

	[TestMethod]
	public void Serialization_RoundTrips_ThroughArrayBufferWriterShim()
	{
		SdlDocument document = new();
		Tag server = document.AddTag("server");
		server.SetAttribute("host", "localhost");
		server.SetAttribute("port", 8080);
		server.AddChild("endpoint").AddValue("/api");

		// ToSdlString -> ArrayBufferWriter<byte>.WrittenSpan
		string sdl = document.ToSdlString();
		SdlDocument reparsed = SdlDocument.Parse(sdl);

		Assert.AreEqual("localhost", reparsed.Tags[0].Attributes["host"].AsString());
		Assert.AreEqual(8080, reparsed.Tags[0].Attributes["port"].AsInt32());
		Assert.AreEqual("/api", reparsed.Tags[0].Children[0].Value!.AsString());
	}

	[TestMethod]
	public async Task WriteToAsync_Streams_ThroughBufferWriterAndValueTaskShims()
	{
		SdlDocument document = new();
		document.AddTag("note").AddValue("hello");

		using MemoryStream stream = new();
		// WriteToAsync uses ArrayBufferWriter<byte>.WrittenMemory + Stream.WriteAsync(ReadOnlyMemory<byte>).
		await document.WriteToAsync(stream).ConfigureAwait(false);

		string written = Encoding.UTF8.GetString(stream.ToArray());
		Assert.IsTrue(written.Contains("note"), written);
		Assert.IsTrue(written.Contains("hello"), written);
	}

	[TestMethod]
	public void ScalarAndBinaryHashCodes_AreStable_ThroughHashCodeShim()
	{
		// Scalar values exercise HashCode.Combine; binary values exercise HashCode.AddBytes.
		SdlValue scalarA = SdlValue.Create(42);
		SdlValue scalarB = SdlValue.Create(42);
		Assert.AreEqual(scalarA.GetHashCode(), scalarB.GetHashCode());

		SdlValue binaryA = SdlValue.Create(new byte[] { 1, 2, 3, 4 });
		SdlValue binaryB = SdlValue.Create(new byte[] { 1, 2, 3, 4 });
		Assert.AreEqual(binaryA.GetHashCode(), binaryB.GetHashCode());
	}

	[TestMethod]
	public void PermissiveParsing_CollectsDiagnostics_OnNet472()
	{
		// Validates the opt-in recovery path runs end-to-end on the shared-source net472 build.
		SdlReaderOptions options = new() { ErrorRecovery = true };
		SdlDocument document = SdlDocument.Parse("good 1\nbad ]\nalsogood 2\n", out IReadOnlyList<SdlDiagnostic> diagnostics, options);

		Assert.IsTrue(diagnostics.Count >= 1, "Expected at least one recovered diagnostic.");
		Assert.AreEqual(SdlDiagnosticSeverity.Error, diagnostics[0].Severity);
		Assert.IsTrue(document.Tags.Count >= 2, "Recovery should still surface the well-formed tags.");
	}

	[TestMethod]
	public void StrictParsing_IsDefault_OnNet472()
	{
		Assert.ThrowsException<SdlReaderException>(() => SdlDocument.Parse("bad ]\n"));
	}

	[TestMethod]
	public void ArgumentGuards_AreEnforced_ThroughExceptionPolyfills()
	{
		// ArgumentNullException.ThrowIfNull is supplied by the Compat polyfill aliased in the csproj.
		Assert.ThrowsException<ArgumentNullException>(() => SdlDocument.Parse((string)null!));

		// ArgumentException.ThrowIfNullOrEmpty is reached via SetAttribute with an empty name.
		SdlDocument document = new();
		Tag tag = document.AddTag("t");
		Assert.ThrowsException<ArgumentException>(() => tag.SetAttribute(string.Empty, "value"));
	}
}
