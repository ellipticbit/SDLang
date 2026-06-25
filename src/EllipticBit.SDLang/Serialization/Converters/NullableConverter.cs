namespace EllipticBit.SDLang.Serialization.Converters;

/// <summary>Produces converters for <see cref="Nullable{T}"/> by wrapping the converter for the underlying type.</summary>
internal sealed class NullableConverterFactory : SdlConverterFactory
{
	public override bool CanConvert(Type typeToConvert)
	{
		ArgumentNullException.ThrowIfNull(typeToConvert);
		return Nullable.GetUnderlyingType(typeToConvert) is not null;
	}

	public override SdlConverter CreateConverter(Type typeToConvert, SdlSerializerOptions options)
	{
		Type underlying = Nullable.GetUnderlyingType(typeToConvert)!;
		SdlConverter inner = SdlConverterRegistry.GetValueConverter(underlying, options);
		Type converterType = typeof(NullableConverter<>).MakeGenericType(underlying);
		return (SdlConverter)Activator.CreateInstance(converterType, inner)!;
	}
}

/// <summary>Converts <see cref="Nullable{T}"/>, mapping the SDL <c>null</c> literal to and from <see langword="null"/>.</summary>
internal sealed class NullableConverter<T> : SdlConverter<T?>
	where T : struct
{
	private readonly SdlConverter _inner;

	public NullableConverter(SdlConverter inner) => _inner = inner;

	public override T? Read(SdlValue value, SdlSerializerOptions options)
		=> value.IsNull ? null : (T)_inner.ReadValueAsObject(value, options)!;

	public override SdlValue Write(T? value, SdlSerializerOptions options)
		=> value is null ? SdlValue.Null() : _inner.WriteValueAsObject(value.Value, options);
}
