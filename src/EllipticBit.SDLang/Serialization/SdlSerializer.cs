using System.Buffers;
using System.Text;

namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// The primary entry point for converting between CLR objects and SDLang text, mirroring the role of
/// <c>System.Text.Json.JsonSerializer</c>. All overloads accept an optional <see cref="SdlSerializerOptions"/>;
/// when omitted, <see cref="SdlSerializerOptions.Default"/> is used. Serialization always flows through the mutable
/// <see cref="SdlDocument"/> DOM so the same structural rules apply to objects and hand-built trees alike.
/// </summary>
public static class SdlSerializer
{
	/// <summary>Serializes <paramref name="value"/> to an SDLang <see cref="string"/>.</summary>
	public static string Serialize<T>(T value, SdlSerializerOptions? options = null)
		=> Serialize(value, typeof(T), options);

	/// <summary>Serializes <paramref name="value"/> of the supplied runtime type to an SDLang <see cref="string"/>.</summary>
	public static string Serialize(object? value, Type inputType, SdlSerializerOptions? options = null)
	{
		SdlDocument document = SerializeToDocument(value, inputType, options);
		return document.ToSdlString(EffectiveOptions(options).GetWriterOptions());
	}

	/// <summary>Serializes <paramref name="value"/> to UTF-8 encoded SDLang bytes.</summary>
	public static byte[] SerializeToUtf8Bytes<T>(T value, SdlSerializerOptions? options = null)
	{
		SdlDocument document = SerializeToDocument(value, typeof(T), options);
		return document.ToUtf8Bytes(EffectiveOptions(options).GetWriterOptions());
	}

	/// <summary>Serializes <paramref name="value"/> into the supplied UTF-8 buffer writer.</summary>
	public static void Serialize<T>(IBufferWriter<byte> utf8Output, T value, SdlSerializerOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(utf8Output);
		SdlDocument document = SerializeToDocument(value, typeof(T), options);
		document.WriteTo(utf8Output, EffectiveOptions(options).GetWriterOptions());
	}

	/// <summary>Serializes <paramref name="value"/> to a <see cref="SdlDocument"/> DOM.</summary>
	public static SdlDocument SerializeToDocument<T>(T value, SdlSerializerOptions? options = null)
		=> SerializeToDocument(value, typeof(T), options);

	/// <summary>Serializes <paramref name="value"/> of a runtime type to a <see cref="SdlDocument"/> DOM.</summary>
	public static SdlDocument SerializeToDocument(object? value, Type inputType, SdlSerializerOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(inputType);
		SdlSerializerOptions effective = EffectiveOptions(options);
		SdlWriteEngine engine = new(effective);
		SdlDocument document = new();
		engine.WriteRoot(value, value?.GetType() ?? inputType, document.Tags);
		return document;
	}

	/// <summary>Asynchronously serializes <paramref name="value"/> to a UTF-8 stream.</summary>
	public static async Task SerializeAsync<T>(Stream utf8Stream, T value, SdlSerializerOptions? options = null, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(utf8Stream);
		SdlDocument document = SerializeToDocument(value, typeof(T), options);
		await document.WriteToAsync(utf8Stream, EffectiveOptions(options).GetWriterOptions(), cancellationToken).ConfigureAwait(false);
	}

	/// <summary>Asynchronously serializes <paramref name="value"/> of the supplied runtime type to a UTF-8 stream.</summary>
	public static async Task SerializeAsync(Stream utf8Stream, object? value, Type inputType, SdlSerializerOptions? options = null, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(utf8Stream);
		SdlDocument document = SerializeToDocument(value, inputType, options);
		await document.WriteToAsync(utf8Stream, EffectiveOptions(options).GetWriterOptions(), cancellationToken).ConfigureAwait(false);
	}

	/// <summary>Deserializes an SDLang <see cref="string"/> into a <typeparamref name="T"/>.</summary>
	public static T? Deserialize<T>(string sdl, SdlSerializerOptions? options = null)
		=> (T?)Deserialize(sdl, typeof(T), options);

	/// <summary>Deserializes an SDLang <see cref="string"/> into the supplied <paramref name="returnType"/>.</summary>
	public static object? Deserialize(string sdl, Type returnType, SdlSerializerOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(sdl);
		SdlSerializerOptions effective = EffectiveOptions(options);
		SdlDocument document = SdlDocument.Parse(sdl, effective.GetReaderOptions());
		return DeserializeDocument(document, returnType, effective);
	}

	/// <summary>Deserializes UTF-8 encoded SDLang bytes into a <typeparamref name="T"/>.</summary>
	public static T? Deserialize<T>(ReadOnlySpan<byte> utf8Sdl, SdlSerializerOptions? options = null)
		=> (T?)Deserialize(utf8Sdl, typeof(T), options);

	/// <summary>Deserializes UTF-8 encoded SDLang bytes into the supplied <paramref name="returnType"/>.</summary>
	public static object? Deserialize(ReadOnlySpan<byte> utf8Sdl, Type returnType, SdlSerializerOptions? options = null)
	{
		SdlSerializerOptions effective = EffectiveOptions(options);
		SdlDocument document = SdlDocument.Parse(utf8Sdl, effective.GetReaderOptions());
		return DeserializeDocument(document, returnType, effective);
	}

	/// <summary>Deserializes a parsed <see cref="SdlDocument"/> into a <typeparamref name="T"/>.</summary>
	public static T? Deserialize<T>(SdlDocument document, SdlSerializerOptions? options = null)
		=> (T?)DeserializeDocument(document, typeof(T), EffectiveOptions(options));

	/// <summary>Asynchronously deserializes a UTF-8 stream into a <typeparamref name="T"/>.</summary>
	public static async Task<T?> DeserializeAsync<T>(Stream utf8Stream, SdlSerializerOptions? options = null, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(utf8Stream);
		SdlSerializerOptions effective = EffectiveOptions(options);
		SdlDocument document = await SdlDocument.ParseAsync(utf8Stream, effective.GetReaderOptions(), cancellationToken).ConfigureAwait(false);
		return (T?)DeserializeDocument(document, typeof(T), effective);
	}

	/// <summary>Asynchronously deserializes a UTF-8 stream into the supplied <paramref name="returnType"/>.</summary>
	public static async Task<object?> DeserializeAsync(Stream utf8Stream, Type returnType, SdlSerializerOptions? options = null, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(utf8Stream);
		SdlSerializerOptions effective = EffectiveOptions(options);
		SdlDocument document = await SdlDocument.ParseAsync(utf8Stream, effective.GetReaderOptions(), cancellationToken).ConfigureAwait(false);
		return DeserializeDocument(document, returnType, effective);
	}

	private static object? DeserializeDocument(SdlDocument document, Type returnType, SdlSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(document);
		ArgumentNullException.ThrowIfNull(returnType);

		if (returnType == typeof(SdlDocument))
		{
			return document;
		}

		SdlReadEngine engine = new(options);
		return engine.ReadRoot([.. document.Tags], returnType);
	}

	private static SdlSerializerOptions EffectiveOptions(SdlSerializerOptions? options)
	{
		SdlSerializerOptions effective = options ?? SdlSerializerOptions.Default;
		effective.MakeReadOnly();
		return effective;
	}
}
