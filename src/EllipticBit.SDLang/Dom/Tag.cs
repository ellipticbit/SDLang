using System.Buffers;
using System.Text;

namespace EllipticBit.SDLang;

/// <summary>
/// The core SDLang DOM node: a tag with an optional namespace and name, an ordered list of literal values, a set
/// of named attributes, and an ordered list of child tags. This is the SDL analog of
/// <c>System.Text.Json.JsonElement</c>, but fully mutable. An anonymous tag (one whose <see cref="Name"/> is
/// <see langword="null"/>) is rendered by SDLang as a <c>content</c> tag.
/// </summary>
public sealed class Tag : SdlNode
{
	private string? _name;

	/// <summary>Initializes a new tag with the supplied name and optional namespace.</summary>
	public Tag(string? name = null, string? ns = null)
	{
		_name = name;
		Namespace = ns;
		Values = new SdlValueCollection();
		Attributes = new SdlAttributeCollection();
		Children = new SdlTagCollection(this);
	}

	/// <summary>Gets or sets the optional namespace prefix of the tag.</summary>
	public string? Namespace { get; set; }

	/// <summary>
	/// Gets or sets the tag name. A <see langword="null"/> name denotes an anonymous tag, which SDLang treats as a
	/// tag named <c>content</c>.
	/// </summary>
	public string? Name
	{
		get => _name;
		set => _name = value;
	}

	/// <summary>Gets a value indicating whether this tag is anonymous (has no explicit name).</summary>
	public bool IsAnonymous => _name is null;

	/// <summary>Gets the ordered collection of literal values attached directly to this tag.</summary>
	public SdlValueCollection Values { get; }

	/// <summary>Gets the collection of named attributes on this tag.</summary>
	public SdlAttributeCollection Attributes { get; }

	/// <summary>Gets the ordered collection of child tags.</summary>
	public SdlTagCollection Children { get; }

	/// <summary>Gets the first value of this tag, or <see langword="null"/> if it has none.</summary>
	public SdlValue? Value => Values.Count > 0 ? Values[0] : null;

	/// <summary>Adds a literal value (inferred from a CLR object) and returns this tag for chaining.</summary>
	public Tag AddValue(object? value)
	{
		Values.Add(SdlValue.FromObject(value));
		return this;
	}

	/// <summary>Adds a strongly typed literal value and returns this tag for chaining.</summary>
	public Tag AddValue(SdlValue value)
	{
		Values.Add(value);
		return this;
	}

	/// <summary>Sets or adds an attribute and returns this tag for chaining.</summary>
	public Tag SetAttribute(string name, object? value, string? ns = null)
	{
		Attributes.Set(name, SdlValue.FromObject(value), ns);
		return this;
	}

	/// <summary>Adds a child tag and returns the new child.</summary>
	public Tag AddChild(string? name, string? ns = null) => Children.Add(name, ns);

	/// <summary>Returns the first child with the given name, or <see langword="null"/>.</summary>
	public Tag? Child(string name, string? ns = null) => Children.FirstOrDefault(name, ns);

	/// <summary>Returns all children with the given name in document order.</summary>
	public IEnumerable<Tag> ChildrenNamed(string name, string? ns = null) => Children.Where(name, ns);

	/// <summary>Gets the value of the named attribute, or throws if it does not exist.</summary>
	public SdlValue Attribute(string name, string? ns = null) => Attributes[name];

	internal bool NameMatches(string name, string? ns)
		=> string.Equals(_name ?? "content", name, StringComparison.Ordinal)
			&& (ns is null || string.Equals(Namespace, ns, StringComparison.Ordinal));

	/// <summary>Writes this tag (and its descendants) to the supplied writer.</summary>
	public override void WriteTo(Utf8SdlWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);

		writer.BeginTag(_name, Namespace);

		foreach (SdlValue value in Values)
		{
			value.WriteTo(writer);
		}

		foreach (SdlAttribute attribute in Attributes)
		{
			writer.WriteAttributeName(attribute.Name, attribute.Namespace);
			attribute.Value.WriteTo(writer);
		}

		if (Children.Count > 0)
		{
			writer.BeginChildren();
			foreach (Tag child in Children)
			{
				child.WriteTo(writer);
			}

			writer.EndChildren();
		}

		writer.EndTag();
	}

	/// <summary>Serializes this tag to an SDLang <see cref="string"/>.</summary>
	public string ToSdlString(SdlWriterOptions? options = null)
	{
		ArrayBufferWriter<byte> buffer = new();
		Utf8SdlWriter writer = new(buffer, options);
		WriteTo(writer);
		return Encoding.UTF8.GetString(buffer.WrittenSpan);
	}

	/// <inheritdoc />
	public override string ToString() => _name ?? "content";
}
