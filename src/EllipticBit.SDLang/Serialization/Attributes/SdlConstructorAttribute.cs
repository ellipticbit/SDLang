namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Marks the constructor the deserializer should use to instantiate a type. Equivalent to
/// <c>System.Text.Json.Serialization.JsonConstructorAttribute</c>. Constructor parameter names are matched to SDL
/// members case-insensitively (after applying any <see cref="SdlPropertyNameAttribute"/>).
/// </summary>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
public sealed class SdlConstructorAttribute : Attribute;
