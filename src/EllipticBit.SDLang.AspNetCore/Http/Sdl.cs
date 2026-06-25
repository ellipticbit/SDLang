using EllipticBit.SDLang.Serialization;
using Microsoft.AspNetCore.Http;

namespace EllipticBit.SDLang.AspNetCore.Http;

/// <summary>
/// A Minimal API request-body binder that deserializes an SDLang request body into <typeparamref name="TValue"/>.
/// Declare a handler parameter of type <see cref="Sdl{TValue}"/> to bind an SDLang body, then read <see cref="Value"/>.
/// A malformed body surfaces as a <see cref="BadHttpRequestException"/> (HTTP 400).
/// </summary>
public sealed class Sdl<TValue>
{
	/// <summary>Initializes a new instance of the <see cref="Sdl{TValue}"/> class.</summary>
	public Sdl(TValue? value) => Value = value;

	/// <summary>Gets the deserialized request body value.</summary>
	public TValue? Value { get; }

	/// <summary>Binds an SDLang request body to <see cref="Sdl{TValue}"/> for Minimal API endpoints.</summary>
	public static async ValueTask<Sdl<TValue>?> BindAsync(HttpContext context)
	{
		ArgumentNullException.ThrowIfNull(context);

		SdlSerializerOptions options = SdlOptionsResolver.Resolve(context, null);
		try
		{
			object? model = await SdlSerializer.DeserializeAsync(context.Request.Body, typeof(TValue), options, context.RequestAborted).ConfigureAwait(false);
			return new Sdl<TValue>((TValue?)model);
		}
		catch (SdlReaderException ex)
		{
			throw new BadHttpRequestException(ex.Message, StatusCodes.Status400BadRequest);
		}
	}
}
