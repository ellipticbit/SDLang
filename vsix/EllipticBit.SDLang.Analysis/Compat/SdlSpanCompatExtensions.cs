// net472 compatibility shim for the shared-source EllipticBit.SDLang.Analysis assembly.
//
// MemoryExtensions in the net472 System.Memory package provides TrimStart only for ReadOnlySpan<char>.
// The shared SDLang parser calls ReadOnlySpan<byte>.TrimStart(byte) when splitting a date/time token, so we
// add the byte-receiver overload here. It is byte-specific (rather than generic) to avoid overload-ambiguity
// with the existing char-based BCL extension.

namespace System;

/// <summary>net472-only span helpers that backfill the <see cref="byte"/> overloads missing from System.Memory.</summary>
public static class SdlSpanCompatExtensions
{
	/// <summary>Removes all leading occurrences of <paramref name="trimValue"/> from <paramref name="span"/>.</summary>
	public static ReadOnlySpan<byte> TrimStart(this ReadOnlySpan<byte> span, byte trimValue)
	{
		int start = 0;
		while (start < span.Length && span[start] == trimValue)
		{
			start++;
		}

		return span.Slice(start);
	}

	/// <summary>Removes all trailing occurrences of <paramref name="trimValue"/> from <paramref name="span"/>.</summary>
	public static ReadOnlySpan<byte> TrimEnd(this ReadOnlySpan<byte> span, byte trimValue)
	{
		int end = span.Length - 1;
		while (end >= 0 && span[end] == trimValue)
		{
			end--;
		}

		return span.Slice(0, end + 1);
	}
}
