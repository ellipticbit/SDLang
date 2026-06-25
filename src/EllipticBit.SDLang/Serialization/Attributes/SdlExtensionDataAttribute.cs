namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Marks a dictionary-typed member as the receptacle for SDL attributes that do not map to a declared member.
/// Mirrors <c>System.Text.Json.Serialization.JsonExtensionDataAttribute</c>. The member must be assignable to
/// <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> with a <see cref="string"/> key.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class SdlExtensionDataAttribute : Attribute;
