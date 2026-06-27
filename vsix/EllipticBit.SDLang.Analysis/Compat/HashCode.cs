// net472 compatibility shim for the shared-source EllipticBit.SDLang.Analysis assembly.
//
// System.HashCode was introduced in .NET Core 2.1 and is absent from the net472 BCL, so a source-defined
// version is picked up by the compiler (the same mechanism that makes the Index/Range polyfills work).
// We deliberately do NOT reference Microsoft.Bcl.HashCode because its netstandard2.0 build lacks the
// AddBytes(ReadOnlySpan<byte>) member that SdlValue.GetHashCode() relies on for binary values.
//
// The contract of GetHashCode only requires that equal inputs yield equal hashes within a single process,
// so this shim uses a simple, well-distributed combiner rather than reproducing the BCL's randomized xxHash.

using System.Collections.Generic;

namespace System;

/// <summary>
/// A net472 stand-in for <c>System.HashCode</c> that combines field hashes into a single value. Implements the
/// members consumed by the SDLang DOM (<see cref="Add{T}(T)"/>, <see cref="AddBytes"/>, <see cref="ToHashCode"/>,
/// and the <see cref="Combine{T1}(T1)"/> family).
/// </summary>
public struct HashCode
{
	private const int InitialHash = 17;
	private const int Multiplier = 31;

	private int _hash;
	private bool _initialized;

	private void EnsureInitialized()
	{
		if (!_initialized)
		{
			_hash = InitialHash;
			_initialized = true;
		}
	}

	private void AddHash(int value)
	{
		EnsureInitialized();
		unchecked
		{
			_hash = (_hash * Multiplier) + value;
		}
	}

	/// <summary>Adds a value's hash code to the running hash.</summary>
	public void Add<T>(T value) => AddHash(value?.GetHashCode() ?? 0);

	/// <summary>Adds a value's hash code (using the supplied comparer) to the running hash.</summary>
	public void Add<T>(T value, IEqualityComparer<T>? comparer)
		=> AddHash(value is null ? 0 : comparer?.GetHashCode(value) ?? value.GetHashCode());

	/// <summary>Adds a span of bytes to the running hash.</summary>
	public void AddBytes(ReadOnlySpan<byte> value)
	{
		EnsureInitialized();
		unchecked
		{
			foreach (byte b in value)
			{
				_hash = (_hash * Multiplier) + b;
			}
		}
	}

	/// <summary>Returns the combined hash code computed so far.</summary>
	public int ToHashCode()
	{
		EnsureInitialized();
		return _hash;
	}

	/// <summary>Combines the hash codes of one value.</summary>
	public static int Combine<T1>(T1 value1)
	{
		HashCode hc = default;
		hc.Add(value1);
		return hc.ToHashCode();
	}

	/// <summary>Combines the hash codes of two values.</summary>
	public static int Combine<T1, T2>(T1 value1, T2 value2)
	{
		HashCode hc = default;
		hc.Add(value1);
		hc.Add(value2);
		return hc.ToHashCode();
	}

	/// <summary>Combines the hash codes of three values.</summary>
	public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
	{
		HashCode hc = default;
		hc.Add(value1);
		hc.Add(value2);
		hc.Add(value3);
		return hc.ToHashCode();
	}

	/// <summary>Combines the hash codes of four values.</summary>
	public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
	{
		HashCode hc = default;
		hc.Add(value1);
		hc.Add(value2);
		hc.Add(value3);
		hc.Add(value4);
		return hc.ToHashCode();
	}

	/// <summary>Combines the hash codes of five values.</summary>
	public static int Combine<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
	{
		HashCode hc = default;
		hc.Add(value1);
		hc.Add(value2);
		hc.Add(value3);
		hc.Add(value4);
		hc.Add(value5);
		return hc.ToHashCode();
	}

	/// <summary>Combines the hash codes of six values.</summary>
	public static int Combine<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
	{
		HashCode hc = default;
		hc.Add(value1);
		hc.Add(value2);
		hc.Add(value3);
		hc.Add(value4);
		hc.Add(value5);
		hc.Add(value6);
		return hc.ToHashCode();
	}

	/// <summary>Combines the hash codes of seven values.</summary>
	public static int Combine<T1, T2, T3, T4, T5, T6, T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)
	{
		HashCode hc = default;
		hc.Add(value1);
		hc.Add(value2);
		hc.Add(value3);
		hc.Add(value4);
		hc.Add(value5);
		hc.Add(value6);
		hc.Add(value7);
		return hc.ToHashCode();
	}

	/// <summary>Combines the hash codes of eight values.</summary>
	public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)
	{
		HashCode hc = default;
		hc.Add(value1);
		hc.Add(value2);
		hc.Add(value3);
		hc.Add(value4);
		hc.Add(value5);
		hc.Add(value6);
		hc.Add(value7);
		hc.Add(value8);
		return hc.ToHashCode();
	}
}
