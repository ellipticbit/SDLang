namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Controls when a property or field is ignored during serialization. Mirrors
/// <c>System.Text.Json.Serialization.JsonIgnoreAttribute</c> and its <c>Condition</c> semantics.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class SdlIgnoreAttribute : Attribute
{
	/// <summary>Gets or sets the condition under which the member is ignored. Defaults to <see cref="SdlIgnoreCondition.Always"/>.</summary>
	public SdlIgnoreCondition Condition { get; set; } = SdlIgnoreCondition.Always;
}

/// <summary>Specifies when a member is excluded from serialization.</summary>
public enum SdlIgnoreCondition
{
	/// <summary>The member is always serialized.</summary>
	Never = 0,

	/// <summary>The member is always ignored.</summary>
	Always,

	/// <summary>The member is ignored when its value is <see langword="null"/>.</summary>
	WhenWritingNull,

	/// <summary>The member is ignored when its value equals the type's default.</summary>
	WhenWritingDefault,
}
