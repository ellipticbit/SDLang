namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Forces a non-public property or field to participate in serialization. Analogous to
/// <c>System.Text.Json.Serialization.JsonIncludeAttribute</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class SdlIncludeAttribute : Attribute;
