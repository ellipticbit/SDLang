using EllipticBit.SDLang.Serialization;
using Microsoft.AspNetCore.Http;

namespace EllipticBit.SDLang.AspNetCore.Http;

/// <summary>
/// Minimal API helpers for writing SDLang response bodies directly inside endpoint handlers.
/// </summary>
public static class SdlHttpResponseExtensions
{
	/// <summary>
	/// Serializes <paramref name="value"/> as an SDLang document, sets the <c>application/sdlang</c> content type, and
	/// writes it to the response body. Optionally sets the response status code first.
	/// </summary>
	public static async Task WriteAsSdlAsync<T>(this HttpResponse response, T value, SdlSerializerOptions? options = null, int? statusCode = null, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(response);

		SdlSerializerOptions effective = SdlOptionsResolver.Resolve(response.HttpContext, options);
		if (statusCode is not null)
		{
			response.StatusCode = statusCode.Value;
		}

		response.ContentType = $"{SdlMediaTypeNames.Application}; charset=utf-8";
		await SdlSerializer.SerializeAsync(response.Body, value, typeof(T), effective, cancellationToken).ConfigureAwait(false);
	}
}
