using System.Text;
using EllipticBit.SDLang.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace EllipticBit.SDLang.AspNetCore.Formatters;

/// <summary>
/// A <see cref="TextInputFormatter"/> that reads SDLang request bodies (negotiated as
/// <c>application/sdlang</c> or <c>text/sdlang</c>) into action parameters and model-bound types, mirroring the
/// role of the built-in JSON and XML input formatters.
/// </summary>
public sealed class SdlInputFormatter : TextInputFormatter
{
	private readonly SdlSerializerOptions _options;

	/// <summary>Initializes a new instance of the <see cref="SdlInputFormatter"/> class.</summary>
	/// <param name="options">The serializer options used to read SDLang payloads.</param>
	public SdlInputFormatter(SdlSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		_options = options;

		SupportedMediaTypes.Add(SdlMediaTypeNames.Application);
		SupportedMediaTypes.Add(SdlMediaTypeNames.Text);
		SupportedEncodings.Add(SdlEncodings.Utf8);
		SupportedEncodings.Add(SdlEncodings.Utf16);
	}

	/// <inheritdoc />
	public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(encoding);

		HttpContext httpContext = context.HttpContext;
		Stream body = httpContext.Request.Body;
		CancellationToken cancellationToken = httpContext.RequestAborted;

		try
		{
			object? model;
			if (encoding.CodePage == Encoding.UTF8.CodePage)
			{
				model = await SdlSerializer.DeserializeAsync(body, context.ModelType, _options, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				using StreamReader reader = new(body, encoding, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
				string sdl = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
				model = SdlSerializer.Deserialize(sdl, context.ModelType, _options);
			}

			return InputFormatterResult.Success(model);
		}
		catch (SdlReaderException ex)
		{
			context.ModelState.TryAddModelError(context.ModelName, ex.Message);
			return InputFormatterResult.Failure();
		}
	}
}
