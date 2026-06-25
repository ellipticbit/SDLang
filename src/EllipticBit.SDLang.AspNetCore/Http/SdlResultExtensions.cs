using EllipticBit.SDLang.Serialization;
using Microsoft.AspNetCore.Http;

namespace EllipticBit.SDLang.AspNetCore.Http;

/// <summary>
/// Extension methods on <see cref="IResultExtensions"/> that produce SDLang results for Minimal API endpoints,
/// surfaced as <c>Results.Extensions.Sdl(...)</c>.
/// </summary>
public static class SdlResultExtensions
{
	/// <summary>
	/// Creates an <see cref="IResult"/> that writes <paramref name="value"/> to the response as an SDLang document.
	/// </summary>
	public static IResult Sdl<TValue>(this IResultExtensions resultExtensions, TValue? value, SdlSerializerOptions? options = null, int? statusCode = null)
	{
		ArgumentNullException.ThrowIfNull(resultExtensions);
		return new SdlResult<TValue>(value, options, statusCode);
	}
}
