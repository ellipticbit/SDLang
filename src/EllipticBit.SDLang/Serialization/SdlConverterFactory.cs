namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Produces converters for an open family of types (for example all enums, or all <see cref="Nullable{T}"/>
/// types) rather than a single closed type. Mirrors
/// <c>System.Text.Json.Serialization.JsonConverterFactory</c>.
/// </summary>
public abstract class SdlConverterFactory : SdlConverter
{
	/// <inheritdoc />
	public override Type Type => typeof(object);

	/// <summary>Creates a converter capable of handling <paramref name="typeToConvert"/>.</summary>
	public abstract SdlConverter CreateConverter(Type typeToConvert, SdlSerializerOptions options);
}
