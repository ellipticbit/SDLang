// net472 compatibility shim for the shared-source EllipticBit.SDLang.Analysis assembly.
//
// System.Text.Rune was introduced in .NET Core 3.0 and has no official down-level NuGet backport.
// The Polyfill package (referenced by this project) supplies Index/Range/init/required/guard helpers
// but intentionally does NOT provide Rune, so we supply the minimal-but-correct surface that the
// linked core sources actually use:
//   - new Rune(char) / new Rune(int)
//   - Value, ToString()
//   - EncodeToUtf8(Span<byte>) / TryEncodeToUtf8(Span<byte>, out int)
//   - static DecodeFromUtf8(ReadOnlySpan<byte>, out Rune, out int) : OperationStatus
//   - static GetRuneAt(string, int), ReplacementChar (used by serialization paths)
//
// This type is declared in the System.Text namespace and is PUBLIC because it appears in the public
// signatures of SdlValue, Utf8SdlWriter, and Utf8SdlReader. It is compiled only into the net472
// analysis assembly; the original net10.0 library uses the real BCL type.
//
// IMPORTANT: This file must be excluded automatically on any non-net472 build of code that links it.
// Because it lives under /vsix/EllipticBit.SDLang.Analysis (which is net472-only), no guard is needed.

using System.Buffers;

namespace System.Text;

/// <summary>
/// A net472 stand-in for <c>System.Text.Rune</c> that represents a Unicode scalar value. Implements only the
/// members consumed by the shared-source SDLang parser, DOM, and writer. The semantics mirror the BCL type:
/// surrogate code points are rejected and UTF-8 transcoding is validating.
/// </summary>
public readonly struct Rune : IEquatable<Rune>, IComparable<Rune>
{
	private const int ReplacementCodePoint = 0xFFFD;
	private const int MaxCodePoint = 0x10FFFF;

	private readonly int _value;

	/// <summary>Initializes a new <see cref="Rune"/> from a UTF-16 code unit that is not a surrogate.</summary>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="ch"/> is a surrogate.</exception>
	public Rune(char ch)
	{
		if (char.IsSurrogate(ch))
		{
			throw new ArgumentOutOfRangeException(nameof(ch), "A surrogate code unit is not a valid Unicode scalar value.");
		}

		_value = ch;
	}

	/// <summary>Initializes a new <see cref="Rune"/> from a Unicode scalar value.</summary>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is out of range or a surrogate.</exception>
	public Rune(int value)
	{
		if (!IsValidScalar(value))
		{
			throw new ArgumentOutOfRangeException(nameof(value), "The value is not a valid Unicode scalar value.");
		}

		_value = value;
	}

	private Rune(int value, bool _)
	{
		// Trusted constructor used internally after validation (e.g. successful UTF-8 decode).
		_value = value;
	}

	/// <summary>Gets the Unicode scalar value as an integer.</summary>
	public int Value => _value;

	/// <summary>Gets the number of UTF-8 bytes required to encode this scalar (1–4).</summary>
	public int Utf8SequenceLength => _value <= 0x7F ? 1 : _value <= 0x7FF ? 2 : _value <= 0xFFFF ? 3 : 4;

	/// <summary>Gets the Unicode replacement character (U+FFFD).</summary>
	public static Rune ReplacementChar => new(ReplacementCodePoint, false);

	/// <summary>Returns the UTF-16 string representation of this scalar value.</summary>
	public override string ToString() => char.ConvertFromUtf32(_value);

	/// <summary>Encodes this scalar to UTF-8 into <paramref name="destination"/>, returning the byte count.</summary>
	/// <exception cref="ArgumentException"><paramref name="destination"/> is too small.</exception>
	public int EncodeToUtf8(Span<byte> destination)
	{
		if (!TryEncodeToUtf8(destination, out int written))
		{
			throw new ArgumentException("Destination is too small to hold the encoded scalar value.", nameof(destination));
		}

		return written;
	}

	/// <summary>Attempts to encode this scalar to UTF-8, writing the byte count to <paramref name="bytesWritten"/>.</summary>
	public bool TryEncodeToUtf8(Span<byte> destination, out int bytesWritten)
	{
		int v = _value;
		if (v <= 0x7F)
		{
			if (destination.Length < 1)
			{
				bytesWritten = 0;
				return false;
			}

			destination[0] = (byte)v;
			bytesWritten = 1;
			return true;
		}

		if (v <= 0x7FF)
		{
			if (destination.Length < 2)
			{
				bytesWritten = 0;
				return false;
			}

			destination[0] = (byte)(0xC0 | (v >> 6));
			destination[1] = (byte)(0x80 | (v & 0x3F));
			bytesWritten = 2;
			return true;
		}

		if (v <= 0xFFFF)
		{
			if (destination.Length < 3)
			{
				bytesWritten = 0;
				return false;
			}

			destination[0] = (byte)(0xE0 | (v >> 12));
			destination[1] = (byte)(0x80 | ((v >> 6) & 0x3F));
			destination[2] = (byte)(0x80 | (v & 0x3F));
			bytesWritten = 3;
			return true;
		}

		if (destination.Length < 4)
		{
			bytesWritten = 0;
			return false;
		}

		destination[0] = (byte)(0xF0 | (v >> 18));
		destination[1] = (byte)(0x80 | ((v >> 12) & 0x3F));
		destination[2] = (byte)(0x80 | ((v >> 6) & 0x3F));
		destination[3] = (byte)(0x80 | (v & 0x3F));
		bytesWritten = 4;
		return true;
	}

	/// <summary>
	/// Decodes the first scalar value from a UTF-8 buffer, validating overlong forms, surrogates, and range,
	/// exactly as the BCL implementation does.
	/// </summary>
	public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> source, out Rune result, out int bytesConsumed)
	{
		if (source.IsEmpty)
		{
			result = ReplacementChar;
			bytesConsumed = 0;
			return OperationStatus.NeedMoreData;
		}

		byte b0 = source[0];
		if (b0 < 0x80)
		{
			result = new Rune(b0, false);
			bytesConsumed = 1;
			return OperationStatus.Done;
		}

		int length;
		int codePoint;
		byte firstContLow = 0x80;
		byte firstContHigh = 0xBF;

		if ((b0 & 0xE0) == 0xC0)
		{
			if (b0 < 0xC2)
			{
				// Overlong two-byte sequence (encodes an ASCII value).
				result = ReplacementChar;
				bytesConsumed = 1;
				return OperationStatus.InvalidData;
			}

			length = 2;
			codePoint = b0 & 0x1F;
		}
		else if ((b0 & 0xF0) == 0xE0)
		{
			length = 3;
			codePoint = b0 & 0x0F;
			if (b0 == 0xE0)
			{
				firstContLow = 0xA0; // reject overlong
			}
			else if (b0 == 0xED)
			{
				firstContHigh = 0x9F; // reject UTF-16 surrogates
			}
		}
		else if ((b0 & 0xF8) == 0xF0 && b0 <= 0xF4)
		{
			length = 4;
			codePoint = b0 & 0x07;
			if (b0 == 0xF0)
			{
				firstContLow = 0x90; // reject overlong
			}
			else if (b0 == 0xF4)
			{
				firstContHigh = 0x8F; // reject > U+10FFFF
			}
		}
		else
		{
			result = ReplacementChar;
			bytesConsumed = 1;
			return OperationStatus.InvalidData;
		}

		int available = Math.Min(length, source.Length);
		for (int i = 1; i < available; i++)
		{
			byte low = i == 1 ? firstContLow : (byte)0x80;
			byte high = i == 1 ? firstContHigh : (byte)0xBF;
			byte bi = source[i];
			if (bi < low || bi > high)
			{
				result = ReplacementChar;
				bytesConsumed = i;
				return OperationStatus.InvalidData;
			}

			codePoint = (codePoint << 6) | (bi & 0x3F);
		}

		if (source.Length < length)
		{
			result = ReplacementChar;
			bytesConsumed = source.Length;
			return OperationStatus.NeedMoreData;
		}

		result = new Rune(codePoint, false);
		bytesConsumed = length;
		return OperationStatus.Done;
	}

	/// <summary>Gets the <see cref="Rune"/> that begins at <paramref name="index"/> within <paramref name="input"/>.</summary>
	public static Rune GetRuneAt(string input, int index)
	{
		if (input is null)
		{
			throw new ArgumentNullException(nameof(input));
		}

		char ch = input[index];
		if (!char.IsSurrogate(ch))
		{
			return new Rune(ch);
		}

		if (char.IsHighSurrogate(ch) && index + 1 < input.Length && char.IsLowSurrogate(input[index + 1]))
		{
			return new Rune(char.ConvertToUtf32(ch, input[index + 1]));
		}

		throw new ArgumentException("The index points to an unpaired surrogate.", nameof(index));
	}

	private static bool IsValidScalar(int value)
		=> (uint)value <= MaxCodePoint && (value < 0xD800 || value > 0xDFFF);

	/// <inheritdoc />
	public bool Equals(Rune other) => _value == other._value;

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Rune other && Equals(other);

	/// <inheritdoc />
	public override int GetHashCode() => _value;

	/// <inheritdoc />
	public int CompareTo(Rune other) => _value.CompareTo(other._value);

	/// <summary>Determines whether two runes represent the same scalar value.</summary>
	public static bool operator ==(Rune left, Rune right) => left._value == right._value;

	/// <summary>Determines whether two runes represent different scalar values.</summary>
	public static bool operator !=(Rune left, Rune right) => left._value != right._value;
}
