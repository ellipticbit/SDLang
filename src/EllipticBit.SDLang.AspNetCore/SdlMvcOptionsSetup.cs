using EllipticBit.SDLang.AspNetCore.Formatters;
using EllipticBit.SDLang.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EllipticBit.SDLang.AspNetCore;

/// <summary>
/// Configures <see cref="MvcOptions"/> to support SDLang content negotiation by registering the SDLang input and
/// output formatters and mapping the <c>sdl</c> format/extension to the SDLang media type.
/// </summary>
internal sealed class SdlMvcOptionsSetup : IConfigureOptions<MvcOptions>
{
	private readonly SdlSerializerOptions _serializerOptions;

	public SdlMvcOptionsSetup(SdlSerializerOptions serializerOptions)
	{
		ArgumentNullException.ThrowIfNull(serializerOptions);
		_serializerOptions = serializerOptions;
	}

	public void Configure(MvcOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		options.InputFormatters.Add(new SdlInputFormatter(_serializerOptions));
		options.OutputFormatters.Add(new SdlOutputFormatter(_serializerOptions));

		options.FormatterMappings.SetMediaTypeMappingForFormat(SdlMediaTypeNames.FileExtension, SdlMediaTypeNames.Application);
	}
}
