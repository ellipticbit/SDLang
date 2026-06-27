using System.Text;

using EllipticBit.SDLang.VisualStudio;

namespace EllipticBit.SDLang.VisualStudio.LanguageServer;

/// <summary>
/// Translates 0-based absolute UTF-8 byte offsets (as produced by the core <c>SdlDiagnostic.BytePosition</c>) into
/// 0-based (line, character) positions suitable for the editor and the public <see cref="SdlTextSpan"/> model.
/// </summary>
/// <remarks>
/// The core parser reports positions as 1-based line/position with the position counted in UTF-8 <em>bytes</em>;
/// Visual Studio (and the Language Server Protocol) expect 0-based lines and characters counted in UTF-16 code
/// units. Counting by byte would mis-place squiggles on any line containing non-ASCII text, so this map ignores
/// the byte-based <c>LinePosition</c> entirely and recomputes the character from the absolute byte offset using
/// <see cref="Encoding.GetCharCount(byte[], int, int)"/>, which yields the exact UTF-16 code-unit count.
///
/// A line-start index is built once per document so each lookup is O(log n) over the number of lines.
/// </remarks>
public sealed class SdlPositionMap
{
	private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

	private readonly byte[] _data;
	private readonly int _length;

	// Byte offset at which each line starts. _lineStarts[0] is always 0; index == 0-based line number.
	private readonly int[] _lineStarts;

	/// <summary>Initializes a new <see cref="SdlPositionMap"/> over the supplied UTF-8 source bytes.</summary>
	/// <param name="utf8">The UTF-8 encoded document the diagnostics were produced from.</param>
	public SdlPositionMap(ReadOnlySpan<byte> utf8)
	{
		_data = utf8.ToArray();
		_length = _data.Length;
		_lineStarts = BuildLineStarts(_data, _length);
	}

	/// <summary>Initializes a new <see cref="SdlPositionMap"/> over the supplied document text.</summary>
	/// <param name="text">The document text; encoded to UTF-8 so offsets line up with the parser.</param>
	/// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
	public SdlPositionMap(string text)
		: this(Utf8.GetBytes(text ?? throw new ArgumentNullException(nameof(text))))
	{
	}

	/// <summary>Gets the number of lines in the document (always at least one).</summary>
	public int LineCount => _lineStarts.Length;

	/// <summary>Maps a 0-based absolute UTF-8 byte offset to a 0-based (line, character) position.</summary>
	/// <param name="byteOffset">The absolute byte offset; values outside the document are clamped to its bounds.</param>
	/// <returns>The 0-based line and 0-based UTF-16 character of the offset.</returns>
	public (int Line, int Character) GetPosition(long byteOffset)
	{
		int offset = Clamp(byteOffset);
		int line = FindLine(offset);
		int lineStart = _lineStarts[line];
		int character = offset > lineStart ? Utf8.GetCharCount(_data, lineStart, offset - lineStart) : 0;
		return (line, character);
	}

	/// <summary>
	/// Builds an <see cref="SdlTextSpan"/> from a starting byte offset and a byte length. A length of <c>0</c>
	/// yields a zero-length (caret) span at the start position.
	/// </summary>
	/// <param name="byteOffset">The 0-based absolute starting byte offset.</param>
	/// <param name="byteLength">The length of the span in UTF-8 bytes; negative values are treated as <c>0</c>.</param>
	/// <returns>The corresponding 0-based, UTF-16 character span.</returns>
	public SdlTextSpan GetSpan(long byteOffset, int byteLength)
	{
		(int startLine, int startCharacter) = GetPosition(byteOffset);
		if (byteLength <= 0)
		{
			return new SdlTextSpan(startLine, startCharacter, startLine, startCharacter);
		}

		(int endLine, int endCharacter) = GetPosition(byteOffset + byteLength);
		return new SdlTextSpan(startLine, startCharacter, endLine, endCharacter);
	}

	private int Clamp(long byteOffset)
	{
		if (byteOffset <= 0)
		{
			return 0;
		}

		return byteOffset >= _length ? _length : (int)byteOffset;
	}

	// Largest line index whose start byte offset is <= the supplied offset.
	private int FindLine(int offset)
	{
		int lo = 0;
		int hi = _lineStarts.Length - 1;
		int line = 0;
		while (lo <= hi)
		{
			int mid = lo + ((hi - lo) >> 1);
			if (_lineStarts[mid] <= offset)
			{
				line = mid;
				lo = mid + 1;
			}
			else
			{
				hi = mid - 1;
			}
		}

		return line;
	}

	// LF (0x0A) is a single byte that never appears inside a multi-byte UTF-8 sequence (continuation bytes are
	// 0x80-0xBF, lead bytes are >= 0xC2), so a byte-wise scan for it is correct. A trailing CR is left on the
	// previous line, matching how the editor counts characters.
	private static int[] BuildLineStarts(byte[] data, int length)
	{
		int count = 1;
		for (int i = 0; i < length; i++)
		{
			if (data[i] == (byte)'\n')
			{
				count++;
			}
		}

		int[] starts = new int[count];
		starts[0] = 0;
		int next = 1;
		for (int i = 0; i < length; i++)
		{
			if (data[i] == (byte)'\n')
			{
				starts[next++] = i + 1;
			}
		}

		return starts;
	}
}
