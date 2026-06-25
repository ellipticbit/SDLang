using System.Buffers;
using System.Text;

namespace EllipticBit.SDLang.Tests;

[TestClass]
public sealed class WriterTests
{
	private static string Write(Action<Utf8SdlWriter> write, SdlWriterOptions? options = null)
	{
		ArrayBufferWriter<byte> buffer = new();
		Utf8SdlWriter writer = new(buffer, options);
		write(writer);
		return Encoding.UTF8.GetString(buffer.WrittenSpan);
	}

	[TestMethod]
	public void Writes_SimpleTag_WithStringValue()
	{
		string result = Write(w =>
		{
			w.BeginTag("greeting");
			w.WriteStringValue("Hello");
			w.EndTag();
		});

		Assert.AreEqual("greeting \"Hello\"", result.TrimEnd());
	}

	[TestMethod]
	public void Writes_Attributes()
	{
		string result = Write(w =>
		{
			w.BeginTag("person");
			w.WriteAttributeName("name");
			w.WriteStringValue("Alice");
			w.WriteAttributeName("age");
			w.WriteInt32Value(42);
			w.EndTag();
		});

		Assert.AreEqual("person name=\"Alice\" age=42", result.TrimEnd());
	}

	[TestMethod]
	public void Writes_Numeric_Suffixes()
	{
		string result = Write(w =>
		{
			w.BeginTag("n");
			w.WriteInt64Value(9000000000L);
			w.WriteSingleValue(3.14f);
			w.WriteDecimalValue(19.99m);
			w.EndTag();
		});

		Assert.IsTrue(result.Contains("9000000000L", StringComparison.Ordinal), result);
		Assert.IsTrue(result.Contains('f', StringComparison.Ordinal), result);
		Assert.IsTrue(result.Contains("BD", StringComparison.Ordinal), result);
	}

	[TestMethod]
	public void Writes_Binary_AsBase64()
	{
		string result = Write(w =>
		{
			w.BeginTag("data");
			w.WriteBinaryValue("hello"u8);
			w.EndTag();
		});

		Assert.IsTrue(result.Contains("[aGVsbG8=]", StringComparison.Ordinal), result);
	}

	[TestMethod]
	public void Writes_Null()
	{
		string result = Write(w =>
		{
			w.BeginTag("n");
			w.WriteNullValue();
			w.EndTag();
		});

		Assert.AreEqual("n null", result.TrimEnd());
	}

	[TestMethod]
	public void Escapes_QuotedString()
	{
		string result = Write(w =>
		{
			w.BeginTag("n");
			w.WriteStringValue("line1\nline2\t\"quoted\"");
			w.EndTag();
		});

		Assert.IsTrue(result.Contains("\\n", StringComparison.Ordinal), result);
		Assert.IsTrue(result.Contains("\\t", StringComparison.Ordinal), result);
		Assert.IsTrue(result.Contains("\\\"", StringComparison.Ordinal), result);
	}

	[TestMethod]
	public void Writes_Indented_Children()
	{
		SdlWriterOptions options = new() { Indented = true };
		string result = Write(w =>
		{
			w.BeginTag("parent");
			w.BeginChildren();
			w.BeginTag("child");
			w.WriteInt32Value(1);
			w.EndTag();
			w.EndChildren();
			w.EndTag();
		}, options);

		Assert.IsTrue(result.Contains('{', StringComparison.Ordinal), result);
		Assert.IsTrue(result.Contains('}', StringComparison.Ordinal), result);
		Assert.IsTrue(result.Contains("  child", StringComparison.Ordinal), result);
	}
}
