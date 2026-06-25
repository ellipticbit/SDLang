namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Determines what happens when SDL input contains a member that does not map to any CLR member. Mirrors
/// <c>System.Text.Json.Serialization.JsonUnmappedMemberHandling</c>.
/// </summary>
public enum SdlUnmappedMemberHandling
{
	/// <summary>Unmapped members are silently skipped.</summary>
	Skip = 0,

	/// <summary>An <see cref="SdlException"/> is thrown when an unmapped member is encountered.</summary>
	Disallow,
}
