using EllipticBit.SDLang.AspNetCore;
using EllipticBit.SDLang.DependencyInjection;
using EllipticBit.SDLang.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods that add the SDLang input/output formatters to an MVC pipeline, enabling SDLang as an HTTP
/// request and response document language for controllers (alongside JSON and XML).
/// </summary>
public static class SdlMvcBuilderExtensions
{
	/// <summary>
	/// Adds the SDLang formatters to the MVC pipeline and, optionally, configures the shared
	/// <see cref="SdlSerializerOptions"/> used to read and write SDLang payloads.
	/// </summary>
	public static IMvcBuilder AddSdlFormatters(this IMvcBuilder builder, Action<SdlSerializerOptions>? configure = null)
	{
		ArgumentNullException.ThrowIfNull(builder);
		AddSdlFormattersCore(builder.Services, configure);
		return builder;
	}

	/// <summary>
	/// Adds the SDLang formatters to the MVC Core pipeline and, optionally, configures the shared
	/// <see cref="SdlSerializerOptions"/> used to read and write SDLang payloads.
	/// </summary>
	public static IMvcCoreBuilder AddSdlFormatters(this IMvcCoreBuilder builder, Action<SdlSerializerOptions>? configure = null)
	{
		ArgumentNullException.ThrowIfNull(builder);
		AddSdlFormattersCore(builder.Services, configure);
		return builder;
	}

	private static void AddSdlFormattersCore(IServiceCollection services, Action<SdlSerializerOptions>? configure)
	{
		if (configure is not null)
		{
			services.AddSdlSerializerOptions(configure);
		}
		else
		{
			services.TryAddSdlSerializerOptions();
		}

		services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, SdlMvcOptionsSetup>());
	}
}
