namespace EllipticBit.SDLang.Serialization.Metadata;

/// <summary>
/// Resolves <see cref="SdlTypeInfo"/> contracts for CLR types. Mirrors
/// <c>System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver</c>, allowing custom metadata providers (for
/// example a future source generator) to replace the default reflection resolver.
/// </summary>
public interface ISdlTypeInfoResolver
{
	/// <summary>Returns the metadata for <paramref name="type"/>, or <see langword="null"/> if unsupported.</summary>
	SdlTypeInfo? GetTypeInfo(Type type, SdlSerializerOptions options);
}
