namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Maps a property or field to a tag <em>attribute</em> (an SDL <c>name=value</c> pair). This is the default
/// structural role for scalar members that are not otherwise annotated. See also <see cref="SdlValueAttribute"/>
/// and <see cref="SdlChildAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class SdlAttributeAttribute : Attribute
{
	/// <summary>Initializes a new instance using the member's name as the attribute name.</summary>
	public SdlAttributeAttribute()
	{
	}

	/// <summary>Initializes a new instance with an explicit attribute name.</summary>
	public SdlAttributeAttribute(string name)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		Name = name;
	}

	/// <summary>Gets the explicit attribute name, or <see langword="null"/> to use the member name.</summary>
	public string? Name { get; }

	/// <summary>Gets or sets the optional SDL namespace of the attribute.</summary>
	public string? Namespace { get; set; }
}
