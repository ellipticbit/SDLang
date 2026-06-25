namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Associates a custom <see cref="SdlConverter"/> with a type or member. Mirrors
/// <c>System.Text.Json.Serialization.JsonConverterAttribute</c>. The referenced type must derive from
/// <see cref="SdlConverter"/> (typically <see cref="SdlConverter{T}"/>) and expose a public parameterless constructor.
/// </summary>
[AttributeUsage(
	AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum
	| AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
	AllowMultiple = false)]
public sealed class SdlConverterAttribute : Attribute
{
	/// <summary>Initializes a new instance referencing the converter type to use.</summary>
	public SdlConverterAttribute(Type converterType)
	{
		ArgumentNullException.ThrowIfNull(converterType);
		ConverterType = converterType;
	}

	/// <summary>Gets the type of the converter to use.</summary>
	public Type ConverterType { get; }

	/// <summary>Creates the converter instance described by this attribute.</summary>
	public SdlConverter CreateConverter()
	{
		if (!typeof(SdlConverter).IsAssignableFrom(ConverterType))
		{
			throw new SdlException($"Type '{ConverterType}' does not derive from {nameof(SdlConverter)}.");
		}

		return (SdlConverter)(Activator.CreateInstance(ConverterType)
			?? throw new SdlException($"Unable to create converter '{ConverterType}'."));
	}
}
