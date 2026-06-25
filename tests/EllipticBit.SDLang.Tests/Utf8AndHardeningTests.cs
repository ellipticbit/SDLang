using System.Text;
using EllipticBit.SDLang.Serialization;

namespace EllipticBit.SDLang.Tests;

[TestClass]
public sealed class Utf8AndHardeningTests
{
	[TestMethod]
	public void Emoji_In_StringValue_RoundTrips()
	{
		SdlDocument document = SdlDocument.Parse("greeting \"Hello \U0001F600 World\"\n");
		Assert.AreEqual("Hello \U0001F600 World", document.Tags[0].Value!.AsString());

		string sdl = document.ToSdlString();
		SdlDocument reparsed = SdlDocument.Parse(sdl);
		Assert.AreEqual("Hello \U0001F600 World", reparsed.Tags[0].Value!.AsString());
	}

	[TestMethod]
	public void Kanji_In_Identifier_And_Value()
	{
		SdlDocument document = SdlDocument.Parse("\u6F22\u5B57 \"\u65E5\u672C\u8A9E\"\n");

		Assert.AreEqual("\u6F22\u5B57", document.Tags[0].Name);
		Assert.AreEqual("\u65E5\u672C\u8A9E", document.Tags[0].Value!.AsString());
	}

	[TestMethod]
	public void Emoji_In_Identifier_IsAllowed()
	{
		SdlDocument document = SdlDocument.Parse("\U0001F600 1\n");
		Assert.AreEqual("\U0001F600", document.Tags[0].Name);
		Assert.AreEqual(1, document.Tags[0].Value!.AsInt32());
	}

	[TestMethod]
	public void Emoji_Char_Literal_RoundTrips()
	{
		SdlDocument document = SdlDocument.Parse("c '\U0001F600'\n");
		Assert.AreEqual(SdlValueKind.Char, document.Tags[0].Value!.Kind);

		string sdl = document.ToSdlString();
		SdlDocument reparsed = SdlDocument.Parse(sdl);
		Assert.AreEqual(SdlValueKind.Char, reparsed.Tags[0].Value!.Kind);
	}

	[TestMethod]
	public void Serializer_Preserves_Unicode()
	{
		Server server = new() { Host = "\u6F22\u5B57.\U0001F600.example", Port = 1, Enabled = true };
		string sdl = SdlSerializer.Serialize(server);
		Server? result = SdlSerializer.Deserialize<Server>(sdl);

		Assert.IsNotNull(result);
		Assert.AreEqual("\u6F22\u5B57.\U0001F600.example", result.Host);
	}

	[TestMethod]
	public void Malformed_Utf8_IsRejected()
	{
		byte[] invalid = [(byte)'n', (byte)' ', (byte)'"', 0xFF, 0xFE, (byte)'"', (byte)'\n'];

		Assert.ThrowsException<SdlReaderException>(() =>
		{
			Utf8SdlReader reader = new(invalid);
			while (reader.Read())
			{
				if (reader.TokenType == SdlTokenType.Value && reader.ValueKind == SdlValueKind.String)
				{
					_ = reader.GetString();
				}
			}
		});
	}

	[TestMethod]
	public void DepthBomb_IsRejected()
	{
		StringBuilder sb = new();
		for (int i = 0; i < 200; i++)
		{
			sb.Append("a { ");
		}

		Assert.ThrowsException<SdlReaderException>(() => SdlDocument.Parse(sb.ToString()));
	}

	[TestMethod]
	public void Custom_MaxDepth_IsEnforced()
	{
		SdlReaderOptions options = new() { MaxDepth = 3 };
		Assert.ThrowsException<SdlReaderException>(() =>
			SdlDocument.Parse("a { b { c { d { e 1 } } } }\n", options));
	}

	[TestMethod]
	public void Bad_Base64_IsRejected()
	{
		Assert.ThrowsException<SdlReaderException>(() => SdlDocument.Parse("data [@@not-base64@@]\n"));
	}

	[TestMethod]
	public void Unterminated_QuotedString_IsRejected()
	{
		Assert.ThrowsException<SdlReaderException>(() => SdlDocument.Parse("n \"unterminated\n"));
	}

	[TestMethod]
	public void Unterminated_BlockComment_IsRejected()
	{
		Assert.ThrowsException<SdlReaderException>(() => SdlDocument.Parse("n 1 /* never closed\n"));
	}

	[TestMethod]
	public void Integer_Overflow_IsRejected()
	{
		string tooLarge = new('9', 40);
		Assert.ThrowsException<SdlReaderException>(() => SdlDocument.Parse($"n {tooLarge}\n"));
	}

	[TestMethod]
	public void Exception_Carries_LocationInformation()
	{
		try
		{
			SdlDocument.Parse("first 1\nsecond }\n");
			Assert.Fail("Expected SdlReaderException.");
		}
		catch (SdlReaderException ex)
		{
			Assert.IsTrue(ex.LineNumber >= 1, $"LineNumber was {ex.LineNumber}");
			Assert.IsTrue(ex.Message.Contains("Line", StringComparison.Ordinal), ex.Message);
		}
	}

	[TestMethod]
	public void Unmatched_CloseBrace_IsRejected()
	{
		Assert.ThrowsException<SdlReaderException>(() => SdlDocument.Parse("a 1 }\n"));
	}
}
