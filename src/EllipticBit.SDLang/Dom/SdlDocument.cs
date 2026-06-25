using System.Buffers;
using System.Text;
using EllipticBit.SDLang.Internal;

namespace EllipticBit.SDLang;

/// <summary>
/// The root of an SDLang document object model. An SDL document is a sequence of top-level tags; this type holds
/// them in document order and provides the parse/serialize entry points. It is the SDL analog of
/// <c>System.Text.Json.JsonDocument</c>, but fully mutable and without the need for disposal.
/// </summary>
public sealed class SdlDocument
{
	private readonly Tag _root;

	/// <summary>Initializes a new, empty document.</summary>
	public SdlDocument() => _root = new Tag("\u0000root");

	/// <summary>Gets the ordered collection of top-level tags in the document.</summary>
	public SdlTagCollection Tags => _root.Children;

	/// <summary>Adds a new top-level tag with the given name and optional namespace.</summary>
	public Tag AddTag(string? name, string? ns = null) => _root.Children.Add(name, ns);

	/// <summary>Returns the first top-level tag with the given name, or <see langword="null"/>.</summary>
	public Tag? Tag(string name, string? ns = null) => _root.Children.FirstOrDefault(name, ns);

	/// <summary>Parses an SDLang document from a UTF-8 byte span.</summary>
	public static SdlDocument Parse(ReadOnlySpan<byte> utf8Sdl, SdlReaderOptions? options = null)
	{
		SdlDocument document = new();
		Utf8SdlReader reader = new(utf8Sdl, options);
		SdlDomBuilder.Build(ref reader, document._root);
		return document;
	}

	/// <summary>Parses an SDLang document from a <see cref="string"/>.</summary>
	public static SdlDocument Parse(string sdl, SdlReaderOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(sdl);
		int max = Encoding.UTF8.GetMaxByteCount(sdl.Length);
		byte[] rented = ArrayPool<byte>.Shared.Rent(max);
		try
		{
			int written = Encoding.UTF8.GetBytes(sdl, rented);
			return Parse(rented.AsSpan(0, written), options);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(rented);
		}
	}

	/// <summary>Asynchronously parses an SDLang document from a stream of UTF-8 bytes.</summary>
	public static async Task<SdlDocument> ParseAsync(Stream utf8Stream, SdlReaderOptions? options = null, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(utf8Stream);
		using MemoryStream buffer = new();
		await utf8Stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
		return buffer.TryGetBuffer(out ArraySegment<byte> segment)
			? Parse(segment.AsSpan(), options)
			: Parse(buffer.ToArray(), options);
	}

	/// <summary>Writes the document to the supplied buffer writer as UTF-8 SDLang.</summary>
	public void WriteTo(IBufferWriter<byte> output, SdlWriterOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(output);
		Utf8SdlWriter writer = new(output, options);
		foreach (Tag tag in _root.Children)
		{
			tag.WriteTo(writer);
		}
	}

	/// <summary>Serializes the document to UTF-8 SDLang bytes.</summary>
	public byte[] ToUtf8Bytes(SdlWriterOptions? options = null)
	{
		ArrayBufferWriter<byte> buffer = new();
		WriteTo(buffer, options);
		return buffer.WrittenSpan.ToArray();
	}

	/// <summary>Serializes the document to an SDLang <see cref="string"/>.</summary>
	public string ToSdlString(SdlWriterOptions? options = null)
	{
		ArrayBufferWriter<byte> buffer = new();
		WriteTo(buffer, options);
		return Encoding.UTF8.GetString(buffer.WrittenSpan);
	}

	/// <summary>Asynchronously serializes the document to a stream as UTF-8 SDLang.</summary>
	public async Task WriteToAsync(Stream utf8Stream, SdlWriterOptions? options = null, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(utf8Stream);
		ArrayBufferWriter<byte> buffer = new();
		WriteTo(buffer, options);
		await utf8Stream.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public override string ToString() => ToSdlString();
}
