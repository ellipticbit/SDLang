namespace EllipticBit.SDLang;

/// <summary>
/// A single named attribute on a <see cref="Tag"/> (an SDL <c>name=value</c> or <c>namespace:name=value</c>
/// pair). This is the DOM entry type and is unrelated to the serialization marker attributes. Instances are mutable.
/// </summary>
public sealed class SdlAttribute
{
	private string _name;
	private SdlValue _value;

	/// <summary>Initializes a new attribute with the supplied name, value, and optional namespace.</summary>
	public SdlAttribute(string name, SdlValue value, string? ns = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentNullException.ThrowIfNull(value);
		_name = name;
		_value = value;
		Namespace = ns;
	}

	/// <summary>Gets or sets the optional namespace prefix of the attribute.</summary>
	public string? Namespace { get; set; }

	/// <summary>Gets or sets the attribute name. Cannot be null or empty.</summary>
	public string Name
	{
		get => _name;
		set
		{
			ArgumentException.ThrowIfNullOrEmpty(value);
			_name = value;
		}
	}

	/// <summary>Gets or sets the attribute value. Cannot be null (use <see cref="SdlValue.Null()"/> for the SDL null literal).</summary>
	public SdlValue Value
	{
		get => _value;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			_value = value;
		}
	}

	/// <inheritdoc />
	public override string ToString()
		=> Namespace is null ? $"{Name}={Value}" : $"{Namespace}:{Name}={Value}";
}
