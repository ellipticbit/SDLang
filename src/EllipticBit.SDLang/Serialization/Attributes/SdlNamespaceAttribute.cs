namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Specifies the SDL namespace applied to a type's tag, or to an individual member. When placed on a type it sets
/// the default namespace for the tag emitted for that type; when placed on a member it overrides the namespace of
/// that member's attribute or child tag.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class SdlNamespaceAttribute : Attribute
{
	/// <summary>Initializes a new instance with the SDL namespace to apply.</summary>
	public SdlNamespaceAttribute(string @namespace)
	{
		ArgumentException.ThrowIfNullOrEmpty(@namespace);
		Namespace = @namespace;
	}

	/// <summary>Gets the SDL namespace.</summary>
	public string Namespace { get; }
}
