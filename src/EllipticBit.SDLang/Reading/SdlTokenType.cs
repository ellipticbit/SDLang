namespace EllipticBit.SDLang;

/// <summary>
/// Identifies the kind of token most recently read by <see cref="Utf8SdlReader"/>.
/// </summary>
public enum SdlTokenType : byte
{
	/// <summary>No token has been read yet.</summary>
	None = 0,

	/// <summary>A bare identifier: a tag name, namespace, or attribute name. See <see cref="Utf8SdlReader.GetName"/>.</summary>
	Identifier,

	/// <summary>A literal value. Inspect <see cref="Utf8SdlReader.ValueKind"/> and the typed <c>Get*</c> accessors.</summary>
	Value,

	/// <summary>The <c>=</c> that separates an attribute name from its value.</summary>
	Equals,

	/// <summary>The <c>{</c> that begins a child block.</summary>
	OpenBrace,

	/// <summary>The <c>}</c> that ends a child block.</summary>
	CloseBrace,

	/// <summary>One or more node separators (newline or <c>;</c>) collapsed into a single token.</summary>
	LineBreak,

	/// <summary>The end of the document has been reached.</summary>
	EndOfDocument,
}
