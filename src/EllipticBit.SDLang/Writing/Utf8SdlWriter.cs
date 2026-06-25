using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Text;
using EllipticBit.SDLang.Internal;

namespace EllipticBit.SDLang;

/// <summary>
/// A forward-only writer that emits UTF-8 encoded SDLang to an <see cref="IBufferWriter{T}"/>. Mirrors the role
/// of <c>System.Text.Json.Utf8JsonWriter</c>. The writer tracks structural state (indentation and value
/// separators) so callers emit tags, values, attributes, and child blocks without managing whitespace.
/// </summary>
public sealed class Utf8SdlWriter
{
	private readonly IBufferWriter<byte> _output;
	private readonly SdlWriterOptions _options;

	private int _indent;
	private bool _pendingSpace;
	private bool _suppressSeparator;

	/// <summary>Initializes a new <see cref="Utf8SdlWriter"/> targeting the supplied buffer writer.</summary>
	public Utf8SdlWriter(IBufferWriter<byte> output, SdlWriterOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(output);
		_output = output;
		_options = options ?? SdlWriterOptions.Default;
	}

	/// <summary>Begins a tag, writing its indentation and optional <c>namespace:name</c>.</summary>
	/// <param name="name">The tag name, or <see langword="null"/> for an anonymous tag.</param>
	/// <param name="ns">The optional namespace.</param>
	public void BeginTag(string? name, string? ns = null)
	{
		WriteIndent();
		_pendingSpace = false;
		_suppressSeparator = false;

		if (name is not null)
		{
			WriteName(ns, name);
			_pendingSpace = true;
		}
	}

	/// <summary>Writes an attribute name and the <c>=</c> separator. Follow with a single value method.</summary>
	public void WriteAttributeName(string name, string? ns = null)
	{
		ArgumentNullException.ThrowIfNull(name);
		StartToken();
		WriteName(ns, name);
		WriteByte(SdlText.Equal);
		_pendingSpace = true;
		_suppressSeparator = true;
	}

	/// <summary>Opens a child block (<c>{</c>) and increases the indent level.</summary>
	public void BeginChildren()
	{
		if (_pendingSpace)
		{
			WriteByte(SdlText.Space);
		}

		WriteByte(SdlText.OpenBrace);
		WriteNewLine();
		_indent++;
		_pendingSpace = false;
		_suppressSeparator = false;
	}

	/// <summary>Closes a child block (<c>}</c>) and decreases the indent level.</summary>
	public void EndChildren()
	{
		_indent--;
		WriteIndent();
		WriteByte(SdlText.CloseBrace);
		_pendingSpace = false;
		_suppressSeparator = false;
	}

	/// <summary>Ends the current tag, writing a line break.</summary>
	public void EndTag()
	{
		WriteNewLine();
		_pendingSpace = false;
		_suppressSeparator = false;
	}

	/// <summary>Writes the SDL <c>null</c> literal.</summary>
	public void WriteNullValue()
	{
		StartToken();
		WriteRaw("null"u8);
		_pendingSpace = true;
	}

	/// <summary>Writes a boolean value (<c>true</c>/<c>false</c>).</summary>
	public void WriteBooleanValue(bool value)
	{
		StartToken();
		WriteRaw(value ? "true"u8 : "false"u8);
		_pendingSpace = true;
	}

	/// <summary>Writes a quoted, escaped string value.</summary>
	public void WriteStringValue(string value)
	{
		ArgumentNullException.ThrowIfNull(value);
		StartToken();
		WriteByte(SdlText.Quote);
		WriteEscaped(value);
		WriteByte(SdlText.Quote);
		_pendingSpace = true;
	}

	/// <summary>Writes a single character value (<c>'x'</c>).</summary>
	public void WriteCharValue(Rune value)
	{
		StartToken();
		WriteByte(SdlText.Apostrophe);
		switch (value.Value)
		{
			case '\n':
				WriteRaw("\\n"u8);
				break;
			case '\r':
				WriteRaw("\\r"u8);
				break;
			case '\t':
				WriteRaw("\\t"u8);
				break;
			case '\\':
				WriteRaw("\\\\"u8);
				break;
			case '\'':
				WriteRaw("\\'"u8);
				break;
			default:
				Span<byte> tmp = stackalloc byte[4];
				int n = value.EncodeToUtf8(tmp);
				WriteRaw(tmp[..n]);
				break;
		}

		WriteByte(SdlText.Apostrophe);
		_pendingSpace = true;
	}

	/// <summary>Writes a 32-bit integer value.</summary>
	public void WriteInt32Value(int value)
	{
		StartToken();
		Span<byte> dst = _output.GetSpan(16);
		Utf8Formatter.TryFormat(value, dst, out int written);
		_output.Advance(written);
		_pendingSpace = true;
	}

	/// <summary>Writes a 64-bit integer value with the <c>L</c> suffix.</summary>
	public void WriteInt64Value(long value)
	{
		StartToken();
		Span<byte> dst = _output.GetSpan(24);
		Utf8Formatter.TryFormat(value, dst, out int written);
		dst[written] = (byte)'L';
		_output.Advance(written + 1);
		_pendingSpace = true;
	}

	/// <summary>Writes a single-precision float value with the <c>f</c> suffix.</summary>
	public void WriteSingleValue(float value)
	{
		StartToken();
		Span<char> chars = stackalloc char[32];
		value.TryFormat(chars, out int cw, "R", CultureInfo.InvariantCulture);
		WriteAscii(chars[..cw]);
		WriteByte((byte)'f');
		_pendingSpace = true;
	}

	/// <summary>Writes a double-precision float value, ensuring it round-trips as a double.</summary>
	public void WriteDoubleValue(double value)
	{
		StartToken();
		Span<char> chars = stackalloc char[32];
		value.TryFormat(chars, out int cw, "R", CultureInfo.InvariantCulture);
		ReadOnlySpan<char> span = chars[..cw];
		WriteAscii(span);
		if (span.IndexOfAny('.', 'e') < 0 && span.IndexOf('E') < 0)
		{
			WriteByte((byte)'d');
		}

		_pendingSpace = true;
	}

	/// <summary>Writes a decimal value with the <c>BD</c> suffix.</summary>
	public void WriteDecimalValue(decimal value)
	{
		StartToken();
		Span<char> chars = stackalloc char[40];
		value.TryFormat(chars, out int cw, default, CultureInfo.InvariantCulture);
		WriteAscii(chars[..cw]);
		WriteRaw("BD"u8);
		_pendingSpace = true;
	}

	/// <summary>Writes a calendar date value (<c>yyyy/MM/dd</c>).</summary>
	public void WriteDateValue(DateOnly value)
	{
		StartToken();
		Span<char> b = stackalloc char[10];
		int p = WritePadded(b, 0, value.Year, 4);
		b[p++] = '/';
		p = WritePadded(b, p, value.Month, 2);
		b[p++] = '/';
		p = WritePadded(b, p, value.Day, 2);
		WriteAscii(b[..p]);
		_pendingSpace = true;
	}

	/// <summary>Writes a local date/time value (no zone).</summary>
	public void WriteDateTimeValue(DateTime value)
	{
		StartToken();
		Span<char> b = stackalloc char[40];
		int p = WriteDateTimeCore(b, value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Ticks % TimeSpan.TicksPerSecond);
		WriteAscii(b[..p]);
		_pendingSpace = true;
	}

	/// <summary>Writes a zoned date/time value (<c>...-UTC</c> or <c>...-GMT±hh:mm</c>).</summary>
	public void WriteDateTimeOffsetValue(DateTimeOffset value)
	{
		StartToken();
		Span<char> b = stackalloc char[48];
		int p = WriteDateTimeCore(b, value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Ticks % TimeSpan.TicksPerSecond);

		TimeSpan offset = value.Offset;
		if (offset == TimeSpan.Zero)
		{
			b[p++] = '-';
			"UTC".AsSpan().CopyTo(b[p..]);
			p += 3;
		}
		else
		{
			b[p++] = '-';
			"GMT".AsSpan().CopyTo(b[p..]);
			p += 3;
			b[p++] = offset < TimeSpan.Zero ? '-' : '+';
			TimeSpan abs = offset.Duration();
			p = WritePadded(b, p, abs.Hours, 2);
			b[p++] = ':';
			p = WritePadded(b, p, abs.Minutes, 2);
		}

		WriteAscii(b[..p]);
		_pendingSpace = true;
	}

	/// <summary>Writes a duration value (<c>[Nd:]hh:mm:ss[.fff]</c>).</summary>
	public void WriteTimeSpanValue(TimeSpan value)
	{
		StartToken();
		Span<char> b = stackalloc char[40];
		int p = 0;

		if (value < TimeSpan.Zero)
		{
			b[p++] = '-';
			value = value.Negate();
		}

		if (value.Days > 0)
		{
			p = WriteVar(b, p, value.Days);
			b[p++] = 'd';
			b[p++] = ':';
		}

		p = WritePadded(b, p, value.Hours, 2);
		b[p++] = ':';
		p = WritePadded(b, p, value.Minutes, 2);
		b[p++] = ':';
		p = WritePadded(b, p, value.Seconds, 2);

		long fraction = value.Ticks % TimeSpan.TicksPerSecond;
		if (fraction > 0)
		{
			b[p++] = '.';
			p = WriteFraction(b, p, fraction);
		}

		WriteAscii(b[..p]);
		_pendingSpace = true;
	}

	/// <summary>Writes a base64-encoded binary value (<c>[...]</c>).</summary>
	public void WriteBinaryValue(ReadOnlySpan<byte> value)
	{
		StartToken();
		WriteByte(SdlText.OpenBracket);
		int max = Base64.GetMaxEncodedToUtf8Length(value.Length);
		Span<byte> dst = _output.GetSpan(max);
		Base64.EncodeToUtf8(value, dst, out _, out int written);
		_output.Advance(written);
		WriteByte(SdlText.CloseBracket);
		_pendingSpace = true;
	}

	private void WriteName(string? ns, string name)
	{
		if (!string.IsNullOrEmpty(ns))
		{
			WriteUtf8(ns);
			WriteByte(SdlText.Colon);
		}

		WriteUtf8(name);
	}

	private void StartToken()
	{
		if (_suppressSeparator)
		{
			_suppressSeparator = false;
			return;
		}

		if (_pendingSpace)
		{
			WriteByte(SdlText.Space);
		}
	}

	private void WriteIndent()
	{
		if (!_options.Indented || _indent == 0)
		{
			return;
		}

		int count = _indent * _options.IndentSize;
		Span<byte> dst = _output.GetSpan(count);
		dst[..count].Fill((byte)_options.IndentCharacter);
		_output.Advance(count);
	}

	private void WriteNewLine() => WriteByte(SdlText.Lf);

	private void WriteByte(byte b)
	{
		Span<byte> dst = _output.GetSpan(1);
		dst[0] = b;
		_output.Advance(1);
	}

	private void WriteRaw(ReadOnlySpan<byte> bytes)
	{
		Span<byte> dst = _output.GetSpan(bytes.Length);
		bytes.CopyTo(dst);
		_output.Advance(bytes.Length);
	}

	private void WriteAscii(ReadOnlySpan<char> chars)
	{
		Span<byte> dst = _output.GetSpan(chars.Length);
		for (int i = 0; i < chars.Length; i++)
		{
			dst[i] = (byte)chars[i];
		}

		_output.Advance(chars.Length);
	}

	private void WriteUtf8(string text)
	{
		int max = SdlText.StrictUtf8.GetMaxByteCount(text.Length);
		Span<byte> dst = _output.GetSpan(max);
		int written = SdlText.StrictUtf8.GetBytes(text, dst);
		_output.Advance(written);
	}

	private void WriteEscaped(string value)
	{
		int max = SdlText.StrictUtf8.GetMaxByteCount(value.Length);
		byte[] rented = ArrayPool<byte>.Shared.Rent(max);
		try
		{
			int n = SdlText.StrictUtf8.GetBytes(value, rented);
			ReadOnlySpan<byte> bytes = rented.AsSpan(0, n);
			int runStart = 0;
			for (int i = 0; i < bytes.Length; i++)
			{
				ReadOnlySpan<byte> escape = bytes[i] switch
				{
					SdlText.Quote => "\\\""u8,
					SdlText.Backslash => "\\\\"u8,
					SdlText.Lf => "\\n"u8,
					SdlText.Cr => "\\r"u8,
					SdlText.Tab => "\\t"u8,
					_ => default,
				};

				if (escape.IsEmpty)
				{
					continue;
				}

				if (i > runStart)
				{
					WriteRaw(bytes[runStart..i]);
				}

				WriteRaw(escape);
				runStart = i + 1;
			}

			if (runStart < bytes.Length)
			{
				WriteRaw(bytes[runStart..]);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(rented);
		}
	}

	private static int WritePadded(Span<char> dst, int pos, int value, int width)
	{
		for (int i = width - 1; i >= 0; i--)
		{
			dst[pos + i] = (char)('0' + (value % 10));
			value /= 10;
		}

		return pos + width;
	}

	private static int WriteVar(Span<char> dst, int pos, long value)
	{
		if (value == 0)
		{
			dst[pos++] = '0';
			return pos;
		}

		Span<char> tmp = stackalloc char[20];
		int n = 0;
		while (value > 0)
		{
			tmp[n++] = (char)('0' + (int)(value % 10));
			value /= 10;
		}

		for (int i = n - 1; i >= 0; i--)
		{
			dst[pos++] = tmp[i];
		}

		return pos;
	}

	private static int WriteFraction(Span<char> dst, int pos, long fractionTicks)
	{
		Span<char> digits = stackalloc char[7];
		for (int i = 6; i >= 0; i--)
		{
			digits[i] = (char)('0' + (int)(fractionTicks % 10));
			fractionTicks /= 10;
		}

		int len = 7;
		while (len > 1 && digits[len - 1] == '0')
		{
			len--;
		}

		digits[..len].CopyTo(dst[pos..]);
		return pos + len;
	}

	private static int WriteDateTimeCore(Span<char> b, int year, int month, int day, int hour, int minute, int second, long fraction)
	{
		int p = WritePadded(b, 0, year, 4);
		b[p++] = '/';
		p = WritePadded(b, p, month, 2);
		b[p++] = '/';
		p = WritePadded(b, p, day, 2);
		b[p++] = ' ';
		p = WritePadded(b, p, hour, 2);
		b[p++] = ':';
		p = WritePadded(b, p, minute, 2);
		b[p++] = ':';
		p = WritePadded(b, p, second, 2);

		if (fraction > 0)
		{
			b[p++] = '.';
			p = WriteFraction(b, p, fraction);
		}

		return p;
	}
}
