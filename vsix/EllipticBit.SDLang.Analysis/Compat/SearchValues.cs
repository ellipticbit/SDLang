// net472 compatibility shim for the shared-source EllipticBit.SDLang.Analysis assembly.
//
// System.Buffers.SearchValues<T> and the MemoryExtensions.IndexOfAny(ReadOnlySpan<T>, SearchValues<T>)
// overloads were introduced in .NET 8 and have no down-level NuGet backport. The referenced
// System.Memory package supplies Span<T>/ReadOnlySpan<T> and the classic IndexOfAny overloads on
// net472, but NOT the SearchValues-based ones. We provide the minimal surface the linked core sources use:
//   - SearchValues.Create(ReadOnlySpan<byte>) : SearchValues<byte>      (SdlText.cs field initializers)
//   - ReadOnlySpan<byte>.IndexOfAny(SearchValues<byte>)                 (Utf8SdlReader quoted-string scan)
//   - SearchValues<byte>.Contains(byte)
//
// These live in System.Buffers so the existing `using System.Buffers;` in the linked files binds them.
// Compiled only into the net472 analysis assembly; the original net10.0 library uses the real BCL types.

namespace System.Buffers;

/// <summary>
/// A net472 stand-in for <c>System.Buffers.SearchValues&lt;T&gt;</c>. Holds an immutable, precomputed set of
/// values for fast membership testing. Only the <see cref="byte"/> specialization used by the SDLang scanner is
/// implemented; lookups use a 256-entry bitmap for O(1) <see cref="Contains"/>.
/// </summary>
/// <typeparam name="T">The element type. Constrained to mirror the BCL declaration.</typeparam>
public sealed class SearchValues<T>
	where T : IEquatable<T>?
{
	// Only the byte specialization is exercised by the linked sources; the bitmap is sized for that.
	private readonly bool[] _byteLookup;

	internal SearchValues(bool[] byteLookup) => _byteLookup = byteLookup;

	/// <summary>Returns <see langword="true"/> when <paramref name="value"/> is in the precomputed set.</summary>
	public bool Contains(T value) => value is byte b && _byteLookup[b];

	internal bool ContainsByte(byte value) => _byteLookup[value];
}

/// <summary>
/// A net472 stand-in for the static <c>System.Buffers.SearchValues</c> factory. Provides the byte-span overload
/// consumed by the SDLang scanner.
/// </summary>
public static class SearchValues
{
	/// <summary>Builds an optimized <see cref="SearchValues{T}"/> set from the supplied bytes.</summary>
	public static SearchValues<byte> Create(ReadOnlySpan<byte> values)
	{
		bool[] lookup = new bool[256];
		foreach (byte value in values)
		{
			lookup[value] = true;
		}

		return new SearchValues<byte>(lookup);
	}
}

/// <summary>
/// net472-only span extensions that mirror the .NET 8 <c>MemoryExtensions</c> overloads accepting
/// <see cref="SearchValues{T}"/>. Resolved at the call sites via their existing <c>using System.Buffers;</c>.
/// </summary>
public static class SearchValuesSpanExtensions
{
	/// <summary>Returns the index of the first byte in <paramref name="span"/> present in <paramref name="values"/>, or -1.</summary>
	public static int IndexOfAny(this ReadOnlySpan<byte> span, SearchValues<byte> values)
	{
		for (int i = 0; i < span.Length; i++)
		{
			if (values.ContainsByte(span[i]))
			{
				return i;
			}
		}

		return -1;
	}

	/// <summary>Returns the index of the first byte in <paramref name="span"/> present in <paramref name="values"/>, or -1.</summary>
	public static int IndexOfAny(this Span<byte> span, SearchValues<byte> values)
		=> IndexOfAny((ReadOnlySpan<byte>)span, values);
}
