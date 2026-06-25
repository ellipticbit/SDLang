namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Specifies the relative order in which a member is serialized. Lower values are written first; members without
/// an explicit order use <c>0</c> and retain their reflection order as a tie-breaker. Mirrors
/// <c>System.Text.Json.Serialization.JsonPropertyOrderAttribute</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class SdlPropertyOrderAttribute : Attribute
{
	/// <summary>Initializes a new instance with the supplied order.</summary>
	public SdlPropertyOrderAttribute(int order) => Order = order;

	/// <summary>Gets the serialization order of the member.</summary>
	public int Order { get; }
}
