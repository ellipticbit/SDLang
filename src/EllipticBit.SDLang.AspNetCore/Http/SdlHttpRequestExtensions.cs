using EllipticBit.SDLang.Serialization;
using Microsoft.AspNetCore.Http;

namespace EllipticBit.SDLang.AspNetCore.Http;

/// <summary>
/// Minimal API helpers for reading SDLang request bodies directly inside endpoint handlers. The request body is
/// assumed to be UTF-8 encoded SDLang, consistent with the rest of the library.
/// </summary>
public static class SdlHttpRequestExtensions
{
	/// <summary>Reads and deserializes the SDLang request body into <typeparamref name="T"/>.</summary>
	public static async Task<T?> ReadFromSdlAsync<T>(this HttpRequest request, SdlSerializerOptions? options = null, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);
		object? model = await request.ReadFromSdlAsync(typeof(T), options, cancellationToken).ConfigureAwait(false);
		return (T?)model;
	}

	/// <summary>Reads and deserializes the SDLang request body into the supplied <paramref name="returnType"/>.</summary>
	public static async Task<object?> ReadFromSdlAsync(this HttpRequest request, Type returnType, SdlSerializerOptions? options = null, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(returnType);

		SdlSerializerOptions effective = SdlOptionsResolver.Resolve(request.HttpContext, options);
		return await SdlSerializer.DeserializeAsync(request.Body, returnType, effective, cancellationToken).ConfigureAwait(false);
	}
}
