// net472 compatibility shim for the shared-source EllipticBit.SDLang.Analysis assembly.
//
// System.Buffers.ArrayBufferWriter<T> was introduced in .NET Core 3.0. The referenced System.Memory
// package provides IBufferWriter<T>, Span<T>, Memory<T>, and the AsSpan/AsMemory array extensions, but
// NOT the concrete ArrayBufferWriter<T> sink. The SDLang DOM serialization helpers (SdlDocument.ToUtf8Bytes,
// SdlDocument.ToSdlString, Tag.ToSdlString, WriteToAsync) all `new ArrayBufferWriter<byte>()` and then read
// `WrittenSpan` / `WrittenMemory`, so we provide a faithful, growable implementation here.

namespace System.Buffers;

/// <summary>
/// A net472 stand-in for <c>System.Buffers.ArrayBufferWriter&lt;T&gt;</c>: a heap-backed
/// <see cref="IBufferWriter{T}"/> that grows its internal array on demand. Semantics mirror the BCL type,
/// including the members consumed by the SDLang writer (<see cref="WrittenSpan"/>, <see cref="WrittenMemory"/>).
/// </summary>
/// <typeparam name="T">The element type stored by the writer.</typeparam>
public sealed class ArrayBufferWriter<T> : IBufferWriter<T>
{
	private const int DefaultInitialBufferSize = 256;

	private T[] _buffer;
	private int _index;

	/// <summary>Initializes a writer with no initial capacity; the backing array grows on first write.</summary>
	public ArrayBufferWriter()
	{
		_buffer = Array.Empty<T>();
		_index = 0;
	}

	/// <summary>Initializes a writer with the requested initial capacity.</summary>
	/// <exception cref="ArgumentException"><paramref name="initialCapacity"/> is not positive.</exception>
	public ArrayBufferWriter(int initialCapacity)
	{
		if (initialCapacity <= 0)
		{
			throw new ArgumentException("Initial capacity must be greater than zero.", nameof(initialCapacity));
		}

		_buffer = new T[initialCapacity];
		_index = 0;
	}

	/// <summary>Gets the data written so far as a <see cref="ReadOnlyMemory{T}"/>.</summary>
	public ReadOnlyMemory<T> WrittenMemory => _buffer.AsMemory(0, _index);

	/// <summary>Gets the data written so far as a <see cref="ReadOnlySpan{T}"/>.</summary>
	public ReadOnlySpan<T> WrittenSpan => _buffer.AsSpan(0, _index);

	/// <summary>Gets the number of elements written.</summary>
	public int WrittenCount => _index;

	/// <summary>Gets the total capacity of the backing array.</summary>
	public int Capacity => _buffer.Length;

	/// <summary>Gets the amount of free capacity remaining in the backing array.</summary>
	public int FreeCapacity => _buffer.Length - _index;

	/// <summary>Resets the writer, zeroing the written region so it can be reused.</summary>
	public void Clear()
	{
		_buffer.AsSpan(0, _index).Clear();
		_index = 0;
	}

	/// <summary>Marks <paramref name="count"/> elements (previously obtained via GetSpan/GetMemory) as written.</summary>
	public void Advance(int count)
	{
		if (count < 0)
		{
			throw new ArgumentException("Count must not be negative.", nameof(count));
		}

		if (_index > _buffer.Length - count)
		{
			throw new InvalidOperationException("Cannot advance past the end of the buffer.");
		}

		_index += count;
	}

	/// <summary>Returns writable memory of at least <paramref name="sizeHint"/> elements, growing if necessary.</summary>
	public Memory<T> GetMemory(int sizeHint = 0)
	{
		CheckAndResizeBuffer(sizeHint);
		return _buffer.AsMemory(_index);
	}

	/// <summary>Returns a writable span of at least <paramref name="sizeHint"/> elements, growing if necessary.</summary>
	public Span<T> GetSpan(int sizeHint = 0)
	{
		CheckAndResizeBuffer(sizeHint);
		return _buffer.AsSpan(_index);
	}

	private void CheckAndResizeBuffer(int sizeHint)
	{
		if (sizeHint < 0)
		{
			throw new ArgumentException("Size hint must not be negative.", nameof(sizeHint));
		}

		if (sizeHint == 0)
		{
			sizeHint = 1;
		}

		if (sizeHint <= FreeCapacity)
		{
			return;
		}

		int currentLength = _buffer.Length;
		int growBy = Math.Max(sizeHint, currentLength);
		if (currentLength == 0)
		{
			growBy = Math.Max(growBy, DefaultInitialBufferSize);
		}

		int newSize = currentLength + growBy;
		if ((uint)newSize > int.MaxValue)
		{
			newSize = currentLength + sizeHint;
			if ((uint)newSize > int.MaxValue)
			{
				throw new OutOfMemoryException();
			}
		}

		Array.Resize(ref _buffer, newSize);
	}
}
