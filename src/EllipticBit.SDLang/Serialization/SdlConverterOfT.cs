namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Converts a single CLR value of type <typeparamref name="T"/> to and from an <see cref="SdlValue"/> (a scalar
/// SDL literal). This is the type users derive from to customize how a scalar maps onto SDL. Complex object graphs
/// are handled by the serializer itself via reflection; a value converter is only responsible for producing or
/// consuming a single literal.
/// </summary>
/// <typeparam name="T">The CLR type handled by this converter.</typeparam>
public abstract class SdlConverter<T> : SdlConverter
{
	/// <inheritdoc />
	public override Type Type => typeof(T);

	/// <inheritdoc />
	public override bool CanConvert(Type typeToConvert)
	{
		ArgumentNullException.ThrowIfNull(typeToConvert);
		return typeToConvert == typeof(T);
	}

	/// <summary>Reads a <typeparamref name="T"/> from the supplied SDL literal.</summary>
	public abstract T Read(SdlValue value, SdlSerializerOptions options);

	/// <summary>Writes a <typeparamref name="T"/> as an SDL literal.</summary>
	public abstract SdlValue Write(T value, SdlSerializerOptions options);

	internal sealed override object? ReadValueAsObject(SdlValue value, SdlSerializerOptions options)
		=> Read(value, options);

	internal sealed override SdlValue WriteValueAsObject(object? value, SdlSerializerOptions options)
		=> Write((T)value!, options);
}
