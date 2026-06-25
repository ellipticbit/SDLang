using EllipticBit.SDLang.Serialization;
using Microsoft.AspNetCore.Http;

namespace EllipticBit.SDLang.AspNetCore.Http;

/// <summary>
/// An <see cref="IResult"/> that writes <typeparamref name="TValue"/> to the response as an SDLang document with the
/// <c>application/sdlang</c> content type. Created through <c>Results.Extensions.Sdl(...)</c>.
/// </summary>
public sealed class SdlResult<TValue> : IResult, IStatusCodeHttpResult, IContentTypeHttpResult, IValueHttpResult, IValueHttpResult<TValue>
{
	private readonly SdlSerializerOptions? _options;

	internal SdlResult(TValue? value, SdlSerializerOptions? options, int? statusCode)
	{
		Value = value;
		_options = options;
		StatusCode = statusCode;
	}

	/// <summary>Gets the value serialized to the response body.</summary>
	public TValue? Value { get; }

	object? IValueHttpResult.Value => Value;

	/// <summary>Gets the optional HTTP status code applied before the body is written.</summary>
	public int? StatusCode { get; }

	/// <summary>Gets the content type written to the response.</summary>
	public string? ContentType => $"{SdlMediaTypeNames.Application}; charset=utf-8";

	/// <inheritdoc />
	public async Task ExecuteAsync(HttpContext httpContext)
	{
		ArgumentNullException.ThrowIfNull(httpContext);

		SdlSerializerOptions effective = SdlOptionsResolver.Resolve(httpContext, _options);
		if (StatusCode is not null)
		{
			httpContext.Response.StatusCode = StatusCode.Value;
		}

		httpContext.Response.ContentType = ContentType;
		await SdlSerializer.SerializeAsync(httpContext.Response.Body, Value, typeof(TValue), effective, httpContext.RequestAborted).ConfigureAwait(false);
	}
}
