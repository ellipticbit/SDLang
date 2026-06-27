// net472 compatibility shims for the shared-source EllipticBit.SDLang.Analysis assembly.
//
// ArgumentNullException.ThrowIfNull and ArgumentException.ThrowIfNullOrEmpty are STATIC methods that were
// added to existing sealed BCL exception types in .NET 6/7. They cannot be supplied as extension methods,
// and Polyfill exposes the unrelated `Guard.NotNull` API instead. To let the unmodified core sources keep
// calling `ArgumentNullException.ThrowIfNull(...)` / `ArgumentException.ThrowIfNullOrEmpty(...)`, the project
// file aliases those simple names to the polyfill types below via <Using ... Alias="..."> items:
//
//   <Using Include="EllipticBit.SDLang.Compat.ArgumentNullExceptionPolyfill" Alias="ArgumentNullException" />
//   <Using Include="EllipticBit.SDLang.Compat.ArgumentExceptionPolyfill"     Alias="ArgumentException" />
//
// Each polyfill DERIVES from the corresponding real exception (so `catch`, `is`, and <exception cref="...">
// semantics are preserved) and forwards the common constructors (so any `new ArgumentException(...)` in this
// compilation still works). The static helpers throw the REAL BCL exceptions for byte-for-byte fidelity.
//
// The base types and thrown instances are written with the global:: prefix so they bind to the genuine BCL
// types rather than recursively to these aliases.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EllipticBit.SDLang.Compat;

/// <summary>
/// net472 stand-in that augments <see cref="global::System.ArgumentNullException"/> with the modern static
/// <c>ThrowIfNull</c> guard. Aliased to the simple name <c>ArgumentNullException</c> for the linked core sources.
/// </summary>
public class ArgumentNullExceptionPolyfill : global::System.ArgumentNullException
{
	/// <summary>Initializes a new instance with a system-supplied message.</summary>
	public ArgumentNullExceptionPolyfill()
	{
	}

	/// <summary>Initializes a new instance for the specified null parameter.</summary>
	public ArgumentNullExceptionPolyfill(string? paramName)
		: base(paramName)
	{
	}

	/// <summary>Initializes a new instance with the specified parameter name and message.</summary>
	public ArgumentNullExceptionPolyfill(string? paramName, string? message)
		: base(paramName, message)
	{
	}

	/// <summary>Throws a <see cref="global::System.ArgumentNullException"/> when <paramref name="argument"/> is <see langword="null"/>.</summary>
	public static void ThrowIfNull(
		[NotNull] object? argument,
		[CallerArgumentExpression("argument")] string? paramName = null)
	{
		if (argument is null)
		{
			throw new global::System.ArgumentNullException(paramName);
		}
	}
}

/// <summary>
/// net472 stand-in that augments <see cref="global::System.ArgumentException"/> with the modern static
/// <c>ThrowIfNullOrEmpty</c> guard. Aliased to the simple name <c>ArgumentException</c> for the linked core sources.
/// </summary>
public class ArgumentExceptionPolyfill : global::System.ArgumentException
{
	/// <summary>Initializes a new instance with a system-supplied message.</summary>
	public ArgumentExceptionPolyfill()
	{
	}

	/// <summary>Initializes a new instance with the specified message.</summary>
	public ArgumentExceptionPolyfill(string? message)
		: base(message)
	{
	}

	/// <summary>Initializes a new instance with the specified message and parameter name.</summary>
	public ArgumentExceptionPolyfill(string? message, string? paramName)
		: base(message, paramName)
	{
	}

	/// <summary>
	/// Throws <see cref="global::System.ArgumentNullException"/> when <paramref name="argument"/> is
	/// <see langword="null"/>, or <see cref="global::System.ArgumentException"/> when it is empty.
	/// </summary>
	public static void ThrowIfNullOrEmpty(
		[NotNull] string? argument,
		[CallerArgumentExpression("argument")] string? paramName = null)
	{
		if (argument is null)
		{
			throw new global::System.ArgumentNullException(paramName);
		}

		if (argument.Length == 0)
		{
			throw new global::System.ArgumentException("The value cannot be an empty string.", paramName);
		}
	}
}
