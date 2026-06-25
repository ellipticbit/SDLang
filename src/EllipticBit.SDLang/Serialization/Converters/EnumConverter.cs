using System.Globalization;

namespace EllipticBit.SDLang.Serialization.Converters;

/// <summary>Produces converters for any <see cref="Enum"/> type, reading/writing by member name.</summary>
internal sealed class EnumConverterFactory : SdlConverterFactory
{
	public override bool CanConvert(Type typeToConvert)
	{
		ArgumentNullException.ThrowIfNull(typeToConvert);
		return typeToConvert.IsEnum;
	}

	public override SdlConverter CreateConverter(Type typeToConvert, SdlSerializerOptions options)
	{
		Type converterType = typeof(EnumConverter<>).MakeGenericType(typeToConvert);
		return (SdlConverter)Activator.CreateInstance(converterType)!;
	}
}

/// <summary>Converts a specific enum type. Strings are matched by name; numbers fall back to the underlying value.</summary>
internal sealed class EnumConverter<T> : SdlConverter<T>
	where T : struct, Enum
{
	public override T Read(SdlValue value, SdlSerializerOptions options)
	{
		if (value.Kind == SdlValueKind.String)
		{
			string name = value.AsString();
			if (Enum.TryParse(name, ignoreCase: true, out T parsed))
			{
				return parsed;
			}

			throw new SdlException($"'{name}' is not a valid {typeof(T).Name} value.");
		}

		return (T)Enum.ToObject(typeof(T), value.AsInt64());
	}

	public override SdlValue Write(T value, SdlSerializerOptions options)
		=> SdlValue.Create(value.ToString());
}
