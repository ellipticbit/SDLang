using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using EllipticBit.SDLang.Internal;

namespace EllipticBit.SDLang;

/// <summary>
/// A high-performance, forward-only, allocation-light reader for UTF-8 encoded SDLang text. Operates directly
/// over a <see cref="ReadOnlySpan{T}"/> of bytes (the SDL native encoding) and mirrors the role of
/// <c>System.Text.Json.Utf8JsonReader</c>. The reader is hardened against malformed input: it validates UTF-8,
/// enforces a maximum nesting depth, and reports precise line/column information on failure.
/// </summary>
public ref struct Utf8SdlReader
{
	private readonly ReadOnlySpan<byte> _data;
	private readonly SdlReaderOptions _options;

	private int _pos;
	private int _line;
	private int _lineStart;
	private int _depth;

	// Identifier token spans.
	private int _nsStart;
	private int _nsLength;
	private int _nameStart;
	private int _nameLength;

	// Value token spans.
	private int _tokenStart;
	private int _tokenLength;
	private int _valueStart;
	private int _valueLength;
	private bool _stringEscaped;
	private bool _boolValue;

	/// <summary>Initializes a new <see cref="Utf8SdlReader"/> over the supplied UTF-8 bytes.</summary>
	public Utf8SdlReader(ReadOnlySpan<byte> utf8Sdl, SdlReaderOptions? options = null)
	{
		_data = utf8Sdl;
		_options = options ?? SdlReaderOptions.Default;
		_pos = 0;
		_line = 1;
		_lineStart = 0;
		_depth = 0;
		TokenType = SdlTokenType.None;
		ValueKind = SdlValueKind.Null;
	}

	/// <summary>Gets the type of the current token.</summary>
	public SdlTokenType TokenType { get; private set; }

	/// <summary>Gets the literal kind of the current token when <see cref="TokenType"/> is <see cref="SdlTokenType.Value"/>.</summary>
	public SdlValueKind ValueKind { get; private set; }

	/// <summary>Gets the current child-block nesting depth.</summary>
	public readonly int CurrentDepth => _depth;

	/// <summary>Gets the 1-based line number at the start of the current token.</summary>
	public readonly long CurrentLine => _line;

	/// <summary>Gets the raw UTF-8 bytes of the current value token (for diagnostics).</summary>
	public readonly ReadOnlySpan<byte> ValueSpan => _data.Slice(_tokenStart, _tokenLength);

	/// <summary>
	/// Advances to the next token. Returns <see langword="false"/> when the end of the document is reached.
	/// </summary>
	public bool Read()
	{
		SkipTrivia();

		if (_pos >= _data.Length)
		{
			TokenType = SdlTokenType.EndOfDocument;
			return false;
		}

		byte b = _data[_pos];

		if (b == SdlText.Semicolon || SdlText.IsLineBreak(b))
		{
			ConsumeSeparators();
			TokenType = SdlTokenType.LineBreak;
			return true;
		}

		switch (b)
		{
			case SdlText.OpenBrace:
				_pos++;
				if (++_depth > _options.MaxDepth)
				{
					throw Error($"Maximum nesting depth of {_options.MaxDepth} exceeded");
				}

				TokenType = SdlTokenType.OpenBrace;
				return true;

			case SdlText.CloseBrace:
				if (_depth == 0)
				{
					throw Error("Unexpected '}' without a matching '{'");
				}

				_pos++;
				_depth--;
				TokenType = SdlTokenType.CloseBrace;
				return true;

			case SdlText.Equal:
				_pos++;
				TokenType = SdlTokenType.Equals;
				return true;

			case SdlText.Quote:
				ScanQuotedString();
				return true;

			case SdlText.Backtick:
				ScanRawString();
				return true;

			case SdlText.Apostrophe:
				ScanCharLiteral();
				return true;

			case SdlText.OpenBracket:
				ScanBinary();
				return true;
		}

		if (b == SdlText.Dash || b == SdlText.Plus || SdlText.IsDigit(b))
		{
			ScanNumberDateOrTime();
			return true;
		}

		if (SdlText.IsIdentifierStart(b))
		{
			ScanIdentifierOrKeyword();
			return true;
		}

		throw Error($"Unexpected character '{(char)b}'");
	}

	/// <summary>Gets the namespace of the current <see cref="SdlTokenType.Identifier"/> token, or an empty string.</summary>
	public readonly string GetNamespace()
		=> _nsLength == 0 ? string.Empty : Decode(_data.Slice(_nsStart, _nsLength));

	/// <summary>Gets the local name of the current <see cref="SdlTokenType.Identifier"/> token.</summary>
	public readonly string GetName()
		=> Decode(_data.Slice(_nameStart, _nameLength));

	/// <summary>Gets the string value of the current token (valid for <see cref="SdlValueKind.String"/>).</summary>
	public readonly string GetString()
	{
		if (ValueKind == SdlValueKind.Char)
		{
			return GetRune().ToString();
		}

		EnsureKind(SdlValueKind.String);
		ReadOnlySpan<byte> inner = _data.Slice(_valueStart, _valueLength);
		return _stringEscaped ? Unescape(inner) : Decode(inner);
	}

	/// <summary>Gets the boolean value of the current token.</summary>
	public readonly bool GetBoolean()
	{
		EnsureKind(SdlValueKind.Boolean);
		return _boolValue;
	}

	/// <summary>Gets the <see cref="System.Text.Rune"/> value of the current char token.</summary>
	public readonly Rune GetRune()
	{
		EnsureKind(SdlValueKind.Char);
		ReadOnlySpan<byte> inner = _data.Slice(_valueStart, _valueLength);
		if (inner.Length > 0 && inner[0] == SdlText.Backslash)
		{
			return new Rune(UnescapeChar(inner));
		}

		if (Rune.DecodeFromUtf8(inner, out Rune rune, out int consumed) != OperationStatus.Done || consumed != inner.Length)
		{
			throw Error("Invalid character literal");
		}

		return rune;
	}

	/// <summary>Gets the 32-bit integer value of the current token.</summary>
	public readonly int GetInt32()
		=> SdlValueParser.TryParseInt32(NumericToken(), out int v) ? v : throw NumberError("Int32");

	/// <summary>Gets the 64-bit integer value of the current token.</summary>
	public readonly long GetInt64()
		=> SdlValueParser.TryParseInt64(NumericToken(), out long v) ? v : throw NumberError("Int64");

	/// <summary>Gets the single-precision floating point value of the current token.</summary>
	public readonly float GetSingle()
		=> SdlValueParser.TryParseSingle(NumericToken(), out float v) ? v : throw NumberError("Single");

	/// <summary>Gets the double-precision floating point value of the current token.</summary>
	public readonly double GetDouble()
		=> SdlValueParser.TryParseDouble(NumericToken(), out double v) ? v : throw NumberError("Double");

	/// <summary>Gets the decimal value of the current token.</summary>
	public readonly decimal GetDecimal()
		=> SdlValueParser.TryParseDecimal(NumericToken(), out decimal v) ? v : throw NumberError("Decimal");

	/// <summary>Gets the <see cref="DateOnly"/> value of the current token.</summary>
	public readonly DateOnly GetDateOnly()
	{
		EnsureKind(SdlValueKind.Date);
		return SdlValueParser.TryParseDate(ValueSpan, out DateOnly v) ? v : throw NumberError("Date");
	}

	/// <summary>Gets the <see cref="System.DateTime"/> value of the current token.</summary>
	public readonly DateTime GetDateTime()
	{
		EnsureKind(SdlValueKind.DateTime);
		return SdlValueParser.TryParseDateTime(ValueSpan, out DateTime v, out _, out _) ? v : throw NumberError("DateTime");
	}

	/// <summary>Gets the <see cref="System.DateTimeOffset"/> value of the current token.</summary>
	public readonly DateTimeOffset GetDateTimeOffset()
	{
		EnsureKind(SdlValueKind.DateTimeOffset);
		return SdlValueParser.TryParseDateTime(ValueSpan, out _, out DateTimeOffset v, out _) ? v : throw NumberError("DateTimeOffset");
	}

	/// <summary>Gets the <see cref="System.TimeSpan"/> value of the current token.</summary>
	public readonly TimeSpan GetTimeSpan()
	{
		EnsureKind(SdlValueKind.TimeSpan);
		return SdlValueParser.TryParseTimeSpan(ValueSpan, out TimeSpan v) ? v : throw NumberError("TimeSpan");
	}

	/// <summary>Gets the decoded bytes of the current binary token.</summary>
	public readonly byte[] GetBytes()
	{
		EnsureKind(SdlValueKind.Binary);
		return SdlValueParser.TryDecodeBase64(_data.Slice(_valueStart, _valueLength), out byte[] v)
			? v
			: throw Error("Invalid base64 binary literal");
	}

	/// <summary>Materializes the current value token as a boxed CLR object (used by the DOM builder).</summary>
	public readonly object? GetValue() => ValueKind switch
	{
		SdlValueKind.Null => null,
		SdlValueKind.String => GetString(),
		SdlValueKind.Char => GetRune(),
		SdlValueKind.Int32 => GetInt32(),
		SdlValueKind.Int64 => GetInt64(),
		SdlValueKind.Single => GetSingle(),
		SdlValueKind.Double => GetDouble(),
		SdlValueKind.Decimal => GetDecimal(),
		SdlValueKind.Boolean => GetBoolean(),
		SdlValueKind.Date => GetDateOnly(),
		SdlValueKind.DateTime => GetDateTime(),
		SdlValueKind.DateTimeOffset => GetDateTimeOffset(),
		SdlValueKind.TimeSpan => GetTimeSpan(),
		SdlValueKind.Binary => GetBytes(),
		_ => null,
	};

	private readonly ReadOnlySpan<byte> NumericToken() => ValueSpan;

	private void ScanIdentifierOrKeyword()
	{
		int start = _pos;
		while (_pos < _data.Length && SdlText.IsIdentifierPart(_data[_pos]))
		{
			_pos++;
		}

		int firstEnd = _pos;

		// Namespaced identifier: name ':' name (only when followed by another identifier start).
		if (_pos + 1 < _data.Length && _data[_pos] == SdlText.Colon && SdlText.IsIdentifierStart(_data[_pos + 1]))
		{
			_pos++; // consume ':'
			int localStart = _pos;
			while (_pos < _data.Length && SdlText.IsIdentifierPart(_data[_pos]))
			{
				_pos++;
			}

			_nsStart = start;
			_nsLength = firstEnd - start;
			_nameStart = localStart;
			_nameLength = _pos - localStart;
			TokenType = SdlTokenType.Identifier;
			return;
		}

		ReadOnlySpan<byte> word = _data[start..firstEnd];

		// An identifier immediately followed by '=' (ignoring inline whitespace) is an attribute name.
		int peek = firstEnd;
		while (peek < _data.Length && SdlText.IsInlineWhitespace(_data[peek]))
		{
			peek++;
		}

		bool isAssignment = peek < _data.Length && _data[peek] == SdlText.Equal;

		if (!isAssignment)
		{
			SdlKeyword keyword = SdlKeywords.Match(word);
			if (keyword != SdlKeyword.None)
			{
				SetKeywordValue(keyword, start, firstEnd);
				return;
			}
		}

		_nsStart = 0;
		_nsLength = 0;
		_nameStart = start;
		_nameLength = firstEnd - start;
		TokenType = SdlTokenType.Identifier;
	}

	private void SetKeywordValue(SdlKeyword keyword, int start, int end)
	{
		_tokenStart = start;
		_tokenLength = end - start;
		TokenType = SdlTokenType.Value;
		if (keyword == SdlKeyword.Null)
		{
			ValueKind = SdlValueKind.Null;
		}
		else
		{
			ValueKind = SdlValueKind.Boolean;
			_boolValue = keyword is SdlKeyword.True or SdlKeyword.On;
		}
	}

	private void ScanNumberDateOrTime()
	{
		int start = _pos;
		if (_data[_pos] is SdlText.Dash or SdlText.Plus)
		{
			_pos++;
		}

		while (_pos < _data.Length && IsNumberDateChar(_data[_pos]))
		{
			_pos++;
		}

		int firstEnd = _pos;
		ReadOnlySpan<byte> firstRun = _data[start..firstEnd];

		if (firstRun.IndexOf(SdlText.Slash) >= 0)
		{
			int peek = _pos;
			while (peek < _data.Length && SdlText.IsInlineWhitespace(_data[peek]))
			{
				peek++;
			}

			int candidateStart = peek;
			while (peek < _data.Length && IsNumberDateChar(_data[peek]))
			{
				peek++;
			}

			ReadOnlySpan<byte> candidate = _data[candidateStart..peek];

			// The run after a date is only a time component when it looks like one (contains ':' and no '/').
			// Otherwise it is a separate value (for example an adjacent date), so the current token is just a Date.
			if (candidate.Length > 0 && candidate.IndexOf(SdlText.Colon) >= 0 && candidate.IndexOf(SdlText.Slash) < 0)
			{
				_pos = peek;
				ValueKind = HasZone(candidate) ? SdlValueKind.DateTimeOffset : SdlValueKind.DateTime;
			}
			else
			{
				ValueKind = SdlValueKind.Date;
			}
		}
		else if (firstRun.IndexOf(SdlText.Colon) >= 0)
		{
			ValueKind = SdlValueKind.TimeSpan;
		}
		else
		{
			ValueKind = SdlValueParser.ClassifyNumber(firstRun);
		}

		_tokenStart = start;
		_tokenLength = _pos - start;
		TokenType = SdlTokenType.Value;
	}

	private static bool HasZone(ReadOnlySpan<byte> timeRun)
	{
		foreach (byte b in timeRun)
		{
			if (b == SdlText.Dash || b == SdlText.Plus || SdlText.IsAsciiLetter(b))
			{
				return true;
			}
		}

		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsNumberDateChar(byte b)
		=> SdlText.IsDigit(b) || SdlText.IsAsciiLetter(b)
			|| b == SdlText.Dot || b == SdlText.Colon || b == SdlText.Slash
			|| b == SdlText.Dash || b == SdlText.Plus;

	private void ScanQuotedString()
	{
		_pos++; // opening quote
		int contentStart = _pos;
		bool escaped = false;
		int i = contentStart;

		while (true)
		{
			int rel = _data[i..].IndexOfAny(SdlText.QuotedStringStops);
			if (rel < 0)
			{
				throw Error("Unterminated string literal");
			}

			int j = i + rel;
			byte c = _data[j];

			if (c == SdlText.Quote)
			{
				_valueStart = contentStart;
				_valueLength = j - contentStart;
				_pos = j + 1;
				break;
			}

			if (SdlText.IsLineBreak(c))
			{
				throw Error("Unescaped line break inside a quoted string; use '\\' to continue a line");
			}

			// Backslash escape.
			escaped = true;
			int k = j + 1;
			if (k >= _data.Length)
			{
				throw Error("Unterminated string literal");
			}

			byte next = _data[k];
			if (next == SdlText.Lf)
			{
				NewLine(k);
				i = k + 1;
			}
			else if (next == SdlText.Cr)
			{
				if (k + 1 < _data.Length && _data[k + 1] == SdlText.Lf)
				{
					NewLine(k + 1);
					i = k + 2;
				}
				else
				{
					NewLine(k);
					i = k + 1;
				}
			}
			else
			{
				i = k + 1;
			}
		}

		_stringEscaped = escaped;
		ValueKind = SdlValueKind.String;
		TokenType = SdlTokenType.Value;
	}

	private void ScanRawString()
	{
		_pos++; // opening backtick
		int contentStart = _pos;
		int rel = _data[_pos..].IndexOf(SdlText.Backtick);
		if (rel < 0)
		{
			throw Error("Unterminated raw string literal");
		}

		int end = _pos + rel;
		TrackNewlines(contentStart, end);
		_valueStart = contentStart;
		_valueLength = end - contentStart;
		_pos = end + 1;
		_stringEscaped = false;
		ValueKind = SdlValueKind.String;
		TokenType = SdlTokenType.Value;
	}

	private void ScanCharLiteral()
	{
		_pos++; // opening apostrophe
		int contentStart = _pos;
		if (_pos >= _data.Length)
		{
			throw Error("Unterminated character literal");
		}

		if (_data[_pos] == SdlText.Backslash)
		{
			_pos += 2; // backslash + escaped char
		}
		else
		{
			if (Rune.DecodeFromUtf8(_data[_pos..], out _, out int consumed) != OperationStatus.Done)
			{
				throw Error("Invalid character literal");
			}

			_pos += consumed;
		}

		if (_pos >= _data.Length || _data[_pos] != SdlText.Apostrophe)
		{
			throw Error("Unterminated character literal");
		}

		_valueStart = contentStart;
		_valueLength = _pos - contentStart;
		_pos++; // closing apostrophe
		ValueKind = SdlValueKind.Char;
		TokenType = SdlTokenType.Value;
	}

	private void ScanBinary()
	{
		_pos++; // opening bracket
		int contentStart = _pos;
		int rel = _data[_pos..].IndexOf(SdlText.CloseBracket);
		if (rel < 0)
		{
			throw Error("Unterminated binary literal");
		}

		int end = _pos + rel;
		TrackNewlines(contentStart, end);
		_valueStart = contentStart;
		_valueLength = end - contentStart;
		_pos = end + 1;
		ValueKind = SdlValueKind.Binary;
		TokenType = SdlTokenType.Value;
	}

	private void SkipTrivia()
	{
		while (_pos < _data.Length)
		{
			byte b = _data[_pos];
			if (b == SdlText.Space || b == SdlText.Tab)
			{
				_pos++;
				continue;
			}

			if (b == SdlText.Backslash)
			{
				int j = _pos + 1;
				while (j < _data.Length && SdlText.IsInlineWhitespace(_data[j]))
				{
					j++;
				}

				if (j < _data.Length && _data[j] == SdlText.Lf)
				{
					NewLine(j);
					_pos = j + 1;
					continue;
				}

				if (j < _data.Length && _data[j] == SdlText.Cr)
				{
					if (j + 1 < _data.Length && _data[j + 1] == SdlText.Lf)
					{
						NewLine(j + 1);
						_pos = j + 2;
					}
					else
					{
						NewLine(j);
						_pos = j + 1;
					}

					continue;
				}

				throw Error("Unexpected '\\'; a line continuation must be followed by a line break");
			}

			if (b == SdlText.Slash && _pos + 1 < _data.Length)
			{
				byte n = _data[_pos + 1];
				if (n == SdlText.Slash)
				{
					SkipLineComment();
					continue;
				}

				if (n == SdlText.Star)
				{
					SkipBlockComment();
					continue;
				}
			}

			if (b == SdlText.Hash)
			{
				SkipLineComment();
				continue;
			}

			if (b == SdlText.Dash && _pos + 1 < _data.Length && _data[_pos + 1] == SdlText.Dash)
			{
				SkipLineComment();
				continue;
			}

			return;
		}
	}

	private void SkipLineComment()
	{
		if (_options.CommentHandling == SdlCommentHandling.Disallow)
		{
			throw Error("Comments are not allowed");
		}

		while (_pos < _data.Length && !SdlText.IsLineBreak(_data[_pos]))
		{
			_pos++;
		}
	}

	private void SkipBlockComment()
	{
		if (_options.CommentHandling == SdlCommentHandling.Disallow)
		{
			throw Error("Comments are not allowed");
		}

		_pos += 2; // consume /*
		while (_pos + 1 < _data.Length)
		{
			byte b = _data[_pos];
			if (b == SdlText.Star && _data[_pos + 1] == SdlText.Slash)
			{
				_pos += 2;
				return;
			}

			if (b == SdlText.Lf)
			{
				NewLine(_pos);
			}

			_pos++;
		}

		throw Error("Unterminated block comment");
	}

	private void ConsumeSeparators()
	{
		while (_pos < _data.Length)
		{
			SkipTrivia();
			if (_pos >= _data.Length)
			{
				break;
			}

			byte b = _data[_pos];
			if (b == SdlText.Semicolon)
			{
				_pos++;
				continue;
			}

			if (b == SdlText.Lf)
			{
				NewLine(_pos);
				_pos++;
				continue;
			}

			if (b == SdlText.Cr)
			{
				if (_pos + 1 < _data.Length && _data[_pos + 1] == SdlText.Lf)
				{
					NewLine(_pos + 1);
					_pos += 2;
				}
				else
				{
					NewLine(_pos);
					_pos++;
				}

				continue;
			}

			break;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void NewLine(int lineFeedIndex)
	{
		_line++;
		_lineStart = lineFeedIndex + 1;
	}

	private void TrackNewlines(int from, int to)
	{
		for (int p = from; p < to; p++)
		{
			byte b = _data[p];
			if (b == SdlText.Lf)
			{
				NewLine(p);
			}
			else if (b == SdlText.Cr && (p + 1 >= to || _data[p + 1] != SdlText.Lf))
			{
				NewLine(p);
			}
		}
	}

	private readonly void EnsureKind(SdlValueKind expected)
	{
		if (TokenType != SdlTokenType.Value || ValueKind != expected)
		{
			throw Error($"The current token is not an SDL {expected} value");
		}
	}

	private readonly string Unescape(ReadOnlySpan<byte> inner)
	{
		byte[] rented = ArrayPool<byte>.Shared.Rent(inner.Length);
		try
		{
			int o = 0;
			int i = 0;
			while (i < inner.Length)
			{
				byte b = inner[i];
				if (b != SdlText.Backslash)
				{
					rented[o++] = b;
					i++;
					continue;
				}

				i++;
				if (i >= inner.Length)
				{
					break;
				}

				byte e = inner[i];
				switch (e)
				{
					case (byte)'n':
						rented[o++] = SdlText.Lf;
						break;
					case (byte)'t':
						rented[o++] = SdlText.Tab;
						break;
					case (byte)'r':
						rented[o++] = SdlText.Cr;
						break;
					case (byte)'b':
						rented[o++] = (byte)'\b';
						break;
					case (byte)'f':
						rented[o++] = (byte)'\f';
						break;
					case SdlText.Quote:
					case SdlText.Apostrophe:
					case SdlText.Backslash:
					case SdlText.Slash:
						rented[o++] = e;
						break;
					case SdlText.Lf:
						break; // line continuation
					case SdlText.Cr:
						if (i + 1 < inner.Length && inner[i + 1] == SdlText.Lf)
						{
							i++;
						}

						break;
					default:
						throw Error($"Invalid escape sequence '\\{(char)e}'");
				}

				i++;
			}

			return Decode(rented.AsSpan(0, o));
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(rented);
		}
	}

	private readonly char UnescapeChar(ReadOnlySpan<byte> inner)
	{
		if (inner.Length < 2)
		{
			throw Error("Invalid character escape");
		}

		return inner[1] switch
		{
			(byte)'n' => '\n',
			(byte)'t' => '\t',
			(byte)'r' => '\r',
			(byte)'b' => '\b',
			(byte)'f' => '\f',
			(byte)'0' => '\0',
			SdlText.Quote => '"',
			SdlText.Apostrophe => '\'',
			SdlText.Backslash => '\\',
			SdlText.Slash => '/',
			_ => throw Error($"Invalid character escape '\\{(char)inner[1]}'"),
		};
	}

	private readonly string Decode(ReadOnlySpan<byte> bytes)
	{
		try
		{
			return SdlText.StrictUtf8.GetString(bytes);
		}
		catch (DecoderFallbackException)
		{
			throw Error("Invalid UTF-8 byte sequence");
		}
	}

	private readonly SdlReaderException NumberError(string type)
		=> Error($"Value '{Decode(ValueSpan)}' is not a valid SDL {type}");

	private readonly SdlReaderException Error(string message)
		=> new(message, _line, _pos - _lineStart + 1, _pos);
}
