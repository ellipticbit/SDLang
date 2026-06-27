namespace EllipticBit.SDLang.VisualStudio;

/// <summary>
/// The semantic classification of an <see cref="SdlCompletionItem"/>, used to pick an appropriate glyph and
/// sort order in the completion list. Values mirror the most relevant Language Server Protocol completion kinds.
/// </summary>
public enum SdlCompletionKind
{
	/// <summary>An SDLang tag name.</summary>
	Tag = 0,

	/// <summary>An attribute name.</summary>
	Attribute = 1,

	/// <summary>A literal value (for example, an enumeration member or boolean).</summary>
	Value = 2,

	/// <summary>A namespace prefix.</summary>
	Namespace = 3,

	/// <summary>A code snippet or template.</summary>
	Snippet = 4,

	/// <summary>A keyword.</summary>
	Keyword = 5,
}

/// <summary>
/// A single completion suggestion contributed by an <see cref="ISdlSemanticValidator"/> in response to a
/// completion request at a caret position.
/// </summary>
public sealed class SdlCompletionItem
{
	/// <summary>Initializes a new instance of the <see cref="SdlCompletionItem"/> class.</summary>
	/// <param name="insertText">The text inserted into the buffer when the item is committed.</param>
	/// <param name="kind">The semantic classification of the item.</param>
	/// <param name="displayText">The label shown in the completion list; defaults to <paramref name="insertText"/> when <see langword="null"/>.</param>
	/// <param name="description">Optional tooltip text describing the item.</param>
	/// <exception cref="ArgumentNullException"><paramref name="insertText"/> is <see langword="null"/>.</exception>
	public SdlCompletionItem(string insertText, SdlCompletionKind kind, string? displayText = null, string? description = null)
	{
		InsertText = insertText ?? throw new ArgumentNullException(nameof(insertText));
		Kind = kind;
		DisplayText = displayText ?? insertText;
		Description = description;
	}

	/// <summary>Gets the text inserted into the buffer when the item is committed.</summary>
	public string InsertText { get; }

	/// <summary>Gets the semantic classification of the item.</summary>
	public SdlCompletionKind Kind { get; }

	/// <summary>Gets the label shown in the completion list.</summary>
	public string DisplayText { get; }

	/// <summary>Gets optional tooltip text describing the item, or <see langword="null"/>.</summary>
	public string? Description { get; }
}

/// <summary>
/// The content shown when the user hovers over a span of an SDLang document, contributed by an
/// <see cref="ISdlSemanticValidator"/>.
/// </summary>
public sealed class SdlHover
{
	/// <summary>Initializes a new instance of the <see cref="SdlHover"/> class.</summary>
	/// <param name="content">The text (plain or Markdown) to display.</param>
	/// <param name="span">The range of text the hover applies to.</param>
	/// <param name="isMarkdown"><see langword="true"/> when <paramref name="content"/> is Markdown; otherwise plain text.</param>
	/// <exception cref="ArgumentNullException"><paramref name="content"/> is <see langword="null"/>.</exception>
	public SdlHover(string content, SdlTextSpan span, bool isMarkdown = false)
	{
		Content = content ?? throw new ArgumentNullException(nameof(content));
		Span = span;
		IsMarkdown = isMarkdown;
	}

	/// <summary>Gets the text (plain or Markdown) to display.</summary>
	public string Content { get; }

	/// <summary>Gets the range of text the hover applies to.</summary>
	public SdlTextSpan Span { get; }

	/// <summary>Gets a value indicating whether <see cref="Content"/> is Markdown rather than plain text.</summary>
	public bool IsMarkdown { get; }
}

/// <summary>
/// Describes a request for semantic information at a specific caret position within a document, passed to
/// <see cref="ISdlSemanticValidator.GetCompletionsAsync"/> and <see cref="ISdlSemanticValidator.GetHoverAsync"/>.
/// </summary>
public sealed class SdlRequestContext
{
	/// <summary>Initializes a new instance of the <see cref="SdlRequestContext"/> class.</summary>
	/// <param name="text">The full current text of the document.</param>
	/// <param name="line">The zero-based caret line.</param>
	/// <param name="character">The zero-based caret character.</param>
	/// <param name="documentUri">An optional URI or file path identifying the document.</param>
	/// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
	public SdlRequestContext(string text, int line, int character, string? documentUri = null)
	{
		Text = text ?? throw new ArgumentNullException(nameof(text));
		Line = line;
		Character = character;
		DocumentUri = documentUri;
	}

	/// <summary>Gets the full current text of the document.</summary>
	public string Text { get; }

	/// <summary>Gets the zero-based caret line.</summary>
	public int Line { get; }

	/// <summary>Gets the zero-based caret character.</summary>
	public int Character { get; }

	/// <summary>Gets an optional URI or file path identifying the document, or <see langword="null"/>.</summary>
	public string? DocumentUri { get; }
}
