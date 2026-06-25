using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using EllipticBit.SDLang.Serialization.Converters;

namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Resolves <see cref="SdlConverter"/> instances for CLR types. Mirrors the converter-resolution logic inside
/// <c>System.Text.Json.JsonSerializerOptions</c>: user converters and <c>[SdlConverter]</c> annotations take
/// precedence, followed by the built-in scalar converters and the enum/nullable factories. The registry also
/// answers whether a given type is a <em>scalar</em> (maps to a single SDL literal) versus a complex type that the
/// reflection serializer handles structurally.
/// </summary>
internal static class SdlConverterRegistry
{
	private static readonly Dictionary<Type, SdlConverter> BuiltIn = CreateBuiltIns();
	private static readonly SdlConverterFactory[] Factories =
	[
		new EnumConverterFactory(),
		new NullableConverterFactory(),
	];

	private static readonly ConcurrentDictionary<(SdlSerializerOptions Options, Type Type), SdlConverter?> Cache = new();

	/// <summary>Determines whether <paramref name="type"/> maps onto a single SDL literal value.</summary>
	internal static bool IsScalar(Type type, SdlSerializerOptions options)
		=> TryGetValueConverter(type, options, out _);

	/// <summary>Gets the value converter for <paramref name="type"/>, throwing if none applies.</summary>
	internal static SdlConverter GetValueConverter(Type type, SdlSerializerOptions options)
		=> TryGetValueConverter(type, options, out SdlConverter? converter)
			? converter
			: throw new SdlException($"No SDL value converter is registered for type '{type}'.");

	/// <summary>Attempts to resolve a value converter for <paramref name="type"/>.</summary>
	internal static bool TryGetValueConverter(Type type, SdlSerializerOptions options, out SdlConverter converter)
	{
		SdlConverter? resolved = Cache.GetOrAdd((options, type), static key => Resolve(key.Type, key.Options));
		converter = resolved!;
		return resolved is not null;
	}

	/// <summary>Resolves the converter declared by a <c>[SdlConverter]</c> attribute on a member, if any.</summary>
	internal static SdlConverter? FromAttribute(MemberInfo member, SdlSerializerOptions options)
	{
		SdlConverterAttribute? attribute = member.GetCustomAttribute<SdlConverterAttribute>(inherit: false);
		return attribute is null ? null : Materialize(attribute.CreateConverter(), member is Type t ? t : ((member as PropertyInfo)?.PropertyType ?? typeof(object)), options);
	}

	private static SdlConverter? Resolve(Type type, SdlSerializerOptions options)
	{
		SdlConverter? user = options.FindUserConverter(type);
		if (user is not null)
		{
			return Materialize(user, type, options);
		}

		SdlConverterAttribute? typeAttribute = type.GetCustomAttribute<SdlConverterAttribute>(inherit: false);
		if (typeAttribute is not null)
		{
			return Materialize(typeAttribute.CreateConverter(), type, options);
		}

		if (BuiltIn.TryGetValue(type, out SdlConverter? builtIn))
		{
			return builtIn;
		}

		foreach (SdlConverterFactory factory in Factories)
		{
			if (factory.CanConvert(type))
			{
				return factory.CreateConverter(type, options);
			}
		}

		return null;
	}

	private static SdlConverter Materialize(SdlConverter converter, Type type, SdlSerializerOptions options)
		=> converter is SdlConverterFactory factory ? factory.CreateConverter(type, options) : converter;

	private static Dictionary<Type, SdlConverter> CreateBuiltIns()
	{
		SdlConverter[] converters =
		[
			new StringConverter(),
			new BooleanConverter(),
			new CharConverter(),
			new RuneConverter(),
			new SByteConverter(),
			new ByteConverter(),
			new Int16Converter(),
			new UInt16Converter(),
			new Int32Converter(),
			new UInt32Converter(),
			new Int64Converter(),
			new UInt64Converter(),
			new SingleConverter(),
			new DoubleConverter(),
			new DecimalConverter(),
			new DateOnlyConverter(),
			new DateTimeConverter(),
			new DateTimeOffsetConverter(),
			new TimeSpanConverter(),
			new ByteArrayConverter(),
			new GuidConverter(),
			new UriConverter(),
			new VersionConverter(),
		];

		Dictionary<Type, SdlConverter> map = new(converters.Length);
		foreach (SdlConverter converter in converters)
		{
			map[converter.Type] = converter;
		}

		return map;
	}
}
