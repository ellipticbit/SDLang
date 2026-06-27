// net472 compatibility shim for the shared-source EllipticBit.SDLang.Analysis assembly.
//
// The C# array-slicing syntax `array[start..end]` is lowered by the compiler into a call to
// System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray<T>(T[], Range). That method exists only on
// .NET Core 3.0+, so on net472 the compiler reports CS0656 ("missing compiler required member"). The
// shared SDLang parser uses `decoded[..written]` when trimming a decoded Base64 buffer.
//
// RuntimeHelpers already EXISTS in the net472 mscorlib, so this partial-style augmentation works via the
// well-defined "source wins" rule (CS0436): when a type is declared in both source and a referenced
// assembly, the compiler binds member lookups to the source declaration for this compilation. We therefore
// re-declare RuntimeHelpers with ONLY the single GetSubArray member the lowering needs. The core sources make
// no other direct RuntimeHelpers calls (verified), so shadowing is safe and self-contained to this assembly.

namespace System.Runtime.CompilerServices;

/// <summary>
/// net472 source augmentation of <c>System.Runtime.CompilerServices.RuntimeHelpers</c> that supplies the
/// <see cref="GetSubArray{T}(T[], Range)"/> member required by the compiler to lower array range-indexing
/// (<c>array[a..b]</c>). Only that member is declared; all other RuntimeHelpers usage continues to bind to
/// the genuine BCL type at runtime.
/// </summary>
internal static class RuntimeHelpers
{
	/// <summary>Returns a new array containing the elements of <paramref name="array"/> within <paramref name="range"/>.</summary>
	public static T[] GetSubArray<T>(T[] array, Range range)
	{
		if (array is null)
		{
			throw new ArgumentNullException(nameof(array));
		}

		(int offset, int length) = range.GetOffsetAndLength(array.Length);
		if (length == 0)
		{
			return Array.Empty<T>();
		}

		T[] result = new T[length];
		Array.Copy(array, offset, result, 0, length);
		return result;
	}
}
