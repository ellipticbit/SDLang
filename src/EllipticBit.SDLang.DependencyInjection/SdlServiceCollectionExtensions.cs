using EllipticBit.SDLang.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EllipticBit.SDLang.DependencyInjection;

/// <summary>
/// Extension methods for registering and configuring <see cref="SdlSerializerOptions"/> in a
/// <see cref="IServiceCollection"/>, including named-options support and converter registration. Mirrors the
/// ergonomics of configuring <c>JsonSerializerOptions</c> through the options pattern.
/// </summary>
public static class SdlServiceCollectionExtensions
{
	/// <summary>
	/// Registers the default (unnamed) <see cref="SdlSerializerOptions"/> and applies the supplied configuration.
	/// The configured options are resolvable directly as <see cref="SdlSerializerOptions"/> and via
	/// <see cref="IOptions{TOptions}"/>.
	/// </summary>
	public static IServiceCollection AddSdlSerializerOptions(
		this IServiceCollection services,
		Action<SdlSerializerOptions>? configure = null)
		=> services.AddSdlSerializerOptions(Options.DefaultName, configure);

	/// <summary>
	/// Registers a named <see cref="SdlSerializerOptions"/> and applies the supplied configuration. Named options are
	/// resolved through <see cref="IOptionsMonitor{TOptions}"/> or <see cref="IOptionsSnapshot{TOptions}"/>.
	/// </summary>
	public static IServiceCollection AddSdlSerializerOptions(
		this IServiceCollection services,
		string name,
		Action<SdlSerializerOptions>? configure)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(name);

		services.AddOptions();

		if (configure is not null)
		{
			services.Configure(name, configure);
		}

		services.TryAddSdlSerializerOptionsResolver();
		return services;
	}

	/// <summary>Registers a converter instance that is applied to every <see cref="SdlSerializerOptions"/> configured here.</summary>
	public static IServiceCollection AddSdlConverter(this IServiceCollection services, SdlConverter converter)
		=> services.AddSdlConverter(Options.DefaultName, converter);

	/// <summary>Registers a converter instance applied to the named <see cref="SdlSerializerOptions"/>.</summary>
	public static IServiceCollection AddSdlConverter(this IServiceCollection services, string name, SdlConverter converter)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(converter);

		services.AddOptions();
		services.Configure<SdlSerializerOptions>(name, options => options.Converters.Add(converter));
		services.TryAddSdlSerializerOptionsResolver();
		return services;
	}

	/// <summary>Registers a converter type, constructed via the service provider, applied to the default options.</summary>
	public static IServiceCollection AddSdlConverter<TConverter>(this IServiceCollection services)
		where TConverter : SdlConverter
		=> services.AddSdlConverter<TConverter>(Options.DefaultName);

	/// <summary>Registers a converter type, constructed via the service provider, applied to the named options.</summary>
	public static IServiceCollection AddSdlConverter<TConverter>(this IServiceCollection services, string name)
		where TConverter : SdlConverter
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(name);

		services.AddOptions();
		services.AddOptions<SdlSerializerOptions>(name).Configure<IServiceProvider>((options, provider) =>
		{
			TConverter converter = ActivatorUtilities.CreateInstance<TConverter>(provider);
			options.Converters.Add(converter);
		});
		services.TryAddSdlSerializerOptionsResolver();
		return services;
	}

	/// <summary>
	/// Ensures the configured (default) <see cref="SdlSerializerOptions"/> instance is resolvable for direct
	/// constructor injection, without requiring a configuration delegate. This is the registration that higher-level
	/// integrations (for example, ASP.NET Core formatters) build on. Safe to call multiple times.
	/// </summary>
	public static IServiceCollection TryAddSdlSerializerOptions(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.AddOptions();
		services.TryAddSdlSerializerOptionsResolver();
		return services;
	}

	private static void TryAddSdlSerializerOptionsResolver(this IServiceCollection services)
	{
		for (int i = 0; i < services.Count; i++)
		{
			if (services[i].ServiceType == typeof(SdlSerializerOptions))
			{
				return;
			}
		}

		services.AddSingleton(static provider =>
			provider.GetRequiredService<IOptions<SdlSerializerOptions>>().Value);
	}
}
