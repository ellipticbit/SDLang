using EllipticBit.SDLang.Serialization;
using Microsoft.AspNetCore.Http;

namespace EllipticBit.SDLang.AspNetCore.Http;

/// <summary>
/// Resolves the <see cref="SdlSerializerOptions"/> to use for an HTTP operation: the explicitly supplied options, the
/// options registered in dependency injection (for example via <c>AddSdlSerializerOptions</c>), or the library default.
/// </summary>
internal static class SdlOptionsResolver
{
	public static SdlSerializerOptions Resolve(HttpContext context, SdlSerializerOptions? options)
		=> options
			?? context.RequestServices?.GetService(typeof(SdlSerializerOptions)) as SdlSerializerOptions
			?? SdlSerializerOptions.Default;
}
