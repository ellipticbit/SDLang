namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// The non-generic base type for all SDLang converters. Mirrors the role of
/// <c>System.Text.Json.Serialization.JsonConverter</c>. Derive from <see cref="SdlConverter{T}"/> for a value
/// converter, or from <see cref="SdlConverterFactory"/> to produce converters for a family of types. A converter
/// customizes how a CLR type is translated to and from an <see cref="SdlValue"/>.
/// </summary>
public abstract class SdlConverter
{
	private protected SdlConverter()
	{
	}

	/// <summary>Gets the CLR type this converter handles, used for registry lookups.</summary>
	public abstract Type Type { get; }

	/// <summary>Determines whether this converter can convert the supplied <paramref name="typeToConvert"/>.</summary>
	public virtual bool CanConvert(Type typeToConvert)
	{
		ArgumentNullException.ThrowIfNull(typeToConvert);
		return Type.IsAssignableFrom(typeToConvert);
	}

	/// <summary>Reads a CLR value from an <see cref="SdlValue"/> using the loosely typed (boxed) entry point.</summary>
	internal virtual object? ReadValueAsObject(SdlValue value, SdlSerializerOptions options)
		=> throw new NotSupportedException($"Converter '{GetType()}' is not a value converter.");

	/// <summary>Writes a CLR value to an <see cref="SdlValue"/> using the loosely typed (boxed) entry point.</summary>
	internal virtual SdlValue WriteValueAsObject(object? value, SdlSerializerOptions options)
		=> throw new NotSupportedException($"Converter '{GetType()}' is not a value converter.");
}
