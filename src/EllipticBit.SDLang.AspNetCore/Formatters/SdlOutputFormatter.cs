using System.Text;
using EllipticBit.SDLang.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace EllipticBit.SDLang.AspNetCore.Formatters;

/// <summary>
/// A <see cref="TextOutputFormatter"/> that writes action results as SDLang documents when a client negotiates
/// <c>application/sdlang</c> or <c>text/sdlang</c>, mirroring the role of the built-in JSON and XML output formatters.
/// </summary>
public sealed class SdlOutputFormatter : TextOutputFormatter
{
	private readonly SdlSerializerOptions _options;

	/// <summary>Initializes a new instance of the <see cref="SdlOutputFormatter"/> class.</summary>
	/// <param name="options">The serializer options used to write SDLang payloads.</param>
	public SdlOutputFormatter(SdlSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		_options = options;

		SupportedMediaTypes.Add(SdlMediaTypeNames.Application);
		SupportedMediaTypes.Add(SdlMediaTypeNames.Text);
		SupportedEncodings.Add(SdlEncodings.Utf8);
		SupportedEncodings.Add(SdlEncodings.Utf16);
	}

	/// <inheritdoc />
	public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(selectedEncoding);

		HttpContext httpContext = context.HttpContext;
		CancellationToken cancellationToken = httpContext.RequestAborted;
		object? value = context.Object;
		Type objectType = context.ObjectType ?? value?.GetType() ?? typeof(object);

		if (selectedEncoding.CodePage == Encoding.UTF8.CodePage)
		{
			await SdlSerializer.SerializeAsync(httpContext.Response.Body, value, objectType, _options, cancellationToken).ConfigureAwait(false);
			return;
		}

		string sdl = SdlSerializer.Serialize(value, objectType, _options);
		await using TextWriter writer = context.WriterFactory(httpContext.Response.Body, selectedEncoding);
		await writer.WriteAsync(sdl.AsMemory(), cancellationToken).ConfigureAwait(false);
		await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
	}
}
