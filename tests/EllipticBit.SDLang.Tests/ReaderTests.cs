using System.Text;

namespace EllipticBit.SDLang.Tests;

[TestClass]
public sealed class ReaderTests
{
	private static List<(SdlTokenType Token, SdlValueKind Kind, object? Value)> Tokenize(string sdl)
	{
		List<(SdlTokenType, SdlValueKind, object?)> tokens = [];
		byte[] bytes = Encoding.UTF8.GetBytes(sdl);
		Utf8SdlReader reader = new(bytes);
		while (reader.Read())
		{
			object? value = reader.TokenType == SdlTokenType.Value ? reader.GetValue() : null;
			tokens.Add((reader.TokenType, reader.ValueKind, value));
		}

		return tokens;
	}

	[TestMethod]
	public void Reads_Identifier_And_String()
	{
		List<(SdlTokenType Token, SdlValueKind Kind, object? Value)> tokens = Tokenize("greeting \"Hello\"\n");

		Assert.AreEqual(SdlTokenType.Identifier, tokens[0].Token);
		Assert.AreEqual(SdlTokenType.Value, tokens[1].Token);
		Assert.AreEqual(SdlValueKind.String, tokens[1].Kind);
		Assert.AreEqual("Hello", tokens[1].Value);
	}

	[TestMethod]
	public void Reads_Int32()
	{
		byte[] bytes = "n 42\n"u8.ToArray();
		Utf8SdlReader reader = new(bytes);
		reader.Read();
		reader.Read();
		Assert.AreEqual(SdlValueKind.Int32, reader.ValueKind);
		Assert.AreEqual(42, reader.GetInt32());
	}

	[TestMethod]
	public void Reads_Int64_Suffix()
	{
		byte[] bytes = "n 9000000000L\n"u8.ToArray();
		Utf8SdlReader reader = new(bytes);
		reader.Read();
		reader.Read();
		Assert.AreEqual(SdlValueKind.Int64, reader.ValueKind);
		Assert.AreEqual(9000000000L, reader.GetInt64());
	}

	[TestMethod]
	public void Reads_Single_And_Double()
	{
		byte[] bytes = "n 3.14f 2.718\n"u8.ToArray();
		Utf8SdlReader reader = new(bytes);
		reader.Read();
		reader.Read();
		Assert.AreEqual(SdlValueKind.Single, reader.ValueKind);
		Assert.AreEqual(3.14f, reader.GetSingle(), 0.0001f);
		reader.Read();
		Assert.AreEqual(SdlValueKind.Double, reader.ValueKind);
		Assert.AreEqual(2.718, reader.GetDouble(), 0.0001);
	}

	[TestMethod]
	public void Reads_Decimal_Suffix()
	{
		byte[] bytes = "n 19.99BD\n"u8.ToArray();
		Utf8SdlReader reader = new(bytes);
		reader.Read();
		reader.Read();
		Assert.AreEqual(SdlValueKind.Decimal, reader.ValueKind);
		Assert.AreEqual(19.99m, reader.GetDecimal());
	}

	[TestMethod]
	public void Reads_Boolean_Keywords()
	{
		byte[] bytes = "n true false on off\n"u8.ToArray();
		Utf8SdlReader reader = new(bytes);
		reader.Read();
		bool[] expected = [true, false, true, false];
		foreach (bool e in expected)
		{
			reader.Read();
			Assert.AreEqual(SdlValueKind.Boolean, reader.ValueKind);
			Assert.AreEqual(e, reader.GetBoolean());
		}
	}

	[TestMethod]
	public void Reads_Null_Keyword()
	{
		byte[] bytes = "n null\n"u8.ToArray();
		Utf8SdlReader reader = new(bytes);
		reader.Read();
		reader.Read();
		Assert.AreEqual(SdlValueKind.Null, reader.ValueKind);
	}

	[TestMethod]
	public void Reads_Char_Literal()
	{
		byte[] bytes = "n 'x'\n"u8.ToArray();
		Utf8SdlReader reader = new(bytes);
		reader.Read();
		reader.Read();
		Assert.AreEqual(SdlValueKind.Char, reader.ValueKind);
		Assert.AreEqual(new Rune('x'), reader.GetRune());
	}

	[TestMethod]
	public void Reads_Date()
	{
		byte[] bytes = "n 2015/12/06\n"u8.ToArray();
		Utf8SdlReader reader = new(bytes);
		reader.Read();
		reader.Read();
		Assert.AreEqual(SdlValueKind.Date, reader.ValueKind);
		Assert.AreEqual(new DateOnly(2015, 12, 6), reader.GetDateOnly());
	}

	[TestMethod]
	public void Reads_DateTime()
	{
		byte[] bytes = "n 2015/12/06 12:30:00\n"u8.ToArray();
		Utf8SdlReader reader = new(bytes);
		reader.Read();
		reader.Read();
		Assert.AreEqual(SdlValueKind.DateTime, reader.ValueKind);
		Assert.AreEqual(new DateTime(2015, 12, 6, 12, 30, 0), reader.GetDateTime());
	}

	[TestMethod]
	public void Reads_Binary_Base64()
	{
		byte[] bytes = "n [aGVsbG8=]\n"u8.ToArray();
		Utf8SdlReader reader = new(bytes);
		reader.Read();
		reader.Read();
		Assert.AreEqual(SdlValueKind.Binary, reader.ValueKind);
		CollectionAssert.AreEqual("hello"u8.ToArray(), reader.GetBytes());
	}

	[TestMethod]
	public void Reads_RawString_Backtick()
	{
		byte[] bytes = "n `C:\\path\\file`\n"u8.ToArray();
		Utf8SdlReader reader = new(bytes);
		reader.Read();
		reader.Read();
		Assert.AreEqual(SdlValueKind.String, reader.ValueKind);
		Assert.AreEqual("C:\\path\\file", reader.GetString());
	}

	[TestMethod]
	public void Reads_Namespace()
	{
		byte[] bytes = "ns:config\n"u8.ToArray();
		Utf8SdlReader reader = new(bytes);
		reader.Read();
		Assert.AreEqual(SdlTokenType.Identifier, reader.TokenType);
		Assert.AreEqual("ns", reader.GetNamespace());
		Assert.AreEqual("config", reader.GetName());
	}

	[TestMethod]
	public void Skips_Comments()
	{
		List<(SdlTokenType Token, SdlValueKind Kind, object? Value)> tokens = Tokenize(SampleDocuments.WithComments);

		int values = tokens.Count(t => t.Token == SdlTokenType.Value);
		Assert.AreEqual(2, values);
	}

	[TestMethod]
	public void Handles_LineContinuation()
	{
		byte[] bytes = "n 1 \\\n  2\n"u8.ToArray();
		Utf8SdlReader reader = new(bytes);
		reader.Read();
		reader.Read();
		Assert.AreEqual(1, reader.GetInt32());
		reader.Read();
		Assert.AreEqual(2, reader.GetInt32());
	}

	[TestMethod]
	public void Reports_OpenAndCloseBrace()
	{
		List<(SdlTokenType Token, SdlValueKind Kind, object? Value)> tokens = Tokenize("a {\n b\n}\n");

		Assert.IsTrue(tokens.Any(t => t.Token == SdlTokenType.OpenBrace));
		Assert.IsTrue(tokens.Any(t => t.Token == SdlTokenType.CloseBrace));
	}

	[TestMethod]
	public void Throws_On_Unmatched_CloseBrace()
	{
		Assert.ThrowsException<SdlReaderException>(() =>
		{
			byte[] bytes = "a }\n"u8.ToArray();
			Utf8SdlReader reader = new(bytes);
			while (reader.Read())
			{
			}
		});
	}

	[TestMethod]
	public void Throws_On_Exceeding_MaxDepth()
	{
		Assert.ThrowsException<SdlReaderException>(() =>
		{
			SdlReaderOptions options = new() { MaxDepth = 2 };
			byte[] bytes = "a { b { c { d 1 } } }\n"u8.ToArray();
			Utf8SdlReader reader = new(bytes, options);
			while (reader.Read())
			{
			}
		});
	}
}
