namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Specifies the SDL name used for a property or field when serializing and deserializing. Equivalent in spirit to
/// <c>System.Text.Json.Serialization.JsonPropertyNameAttribute</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class SdlPropertyNameAttribute : Attribute
{
	/// <summary>Initializes a new instance with the SDL name to use.</summary>
	public SdlPropertyNameAttribute(string name)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		Name = name;
	}

	/// <summary>Gets the SDL name to use for the member.</summary>
	public string Name { get; }
}
