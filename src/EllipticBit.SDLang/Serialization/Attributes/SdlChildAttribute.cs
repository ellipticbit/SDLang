namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Maps a property or field to a nested <em>child tag</em>. Complex (non-scalar) members default to this role.
/// For collection members, each element is emitted as a repeated child tag sharing <see cref="Name"/>. See also
/// <see cref="SdlValueAttribute"/> and <see cref="SdlAttributeAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class SdlChildAttribute : Attribute
{
	/// <summary>Initializes a new instance using the member's name as the child tag name.</summary>
	public SdlChildAttribute()
	{
	}

	/// <summary>Initializes a new instance with an explicit child tag name.</summary>
	public SdlChildAttribute(string name)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		Name = name;
	}

	/// <summary>Gets the explicit child tag name, or <see langword="null"/> to use the member name.</summary>
	public string? Name { get; }

	/// <summary>Gets or sets the optional SDL namespace of the child tag.</summary>
	public string? Namespace { get; set; }
}
