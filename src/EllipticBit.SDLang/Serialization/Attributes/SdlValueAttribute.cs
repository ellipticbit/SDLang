namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Maps a property or field to one of a tag's positional <em>values</em> (the literals that follow the tag name).
/// This is one of the three SDL structural roles a member can take; see also <see cref="SdlAttributeAttribute"/>
/// and <see cref="SdlChildAttribute"/>. When a type declares multiple value members, <see cref="Order"/>
/// determines their positional order.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class SdlValueAttribute : Attribute
{
	/// <summary>Initializes a new instance, optionally fixing the positional order of the value.</summary>
	public SdlValueAttribute(int order = 0) => Order = order;

	/// <summary>Gets the zero-based positional order of this value among the tag's values.</summary>
	public int Order { get; }
}
