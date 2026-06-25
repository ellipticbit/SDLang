using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace EllipticBit.SDLang;

/// <summary>
/// An ordered collection of <see cref="SdlAttribute"/> entries on a <see cref="Tag"/>, with convenience lookup
/// by name. Order is preserved for round-tripping; duplicate names are permitted but the lookup helpers operate
/// on the first match.
/// </summary>
public sealed class SdlAttributeCollection : Collection<SdlAttribute>
{
	/// <summary>Gets the value of the first attribute with the given name, or throws if none exists.</summary>
	public SdlValue this[string name]
	{
		get => TryGetValue(name, out SdlValue? value)
			? value
			: throw new KeyNotFoundException($"No attribute named '{name}' exists.");
		set => Set(name, value);
	}

	/// <summary>Adds an attribute with the supplied name, value, and optional namespace.</summary>
	public SdlAttribute Add(string name, SdlValue value, string? ns = null)
	{
		SdlAttribute attribute = new(name, value, ns);
		Add(attribute);
		return attribute;
	}

	/// <summary>Adds an attribute whose value is inferred from a CLR object.</summary>
	public SdlAttribute Add(string name, object? value, string? ns = null)
		=> Add(name, SdlValue.FromObject(value), ns);

	/// <summary>Sets (or adds) the first attribute with the given name to the supplied value.</summary>
	public void Set(string name, SdlValue value, string? ns = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentNullException.ThrowIfNull(value);

		for (int i = 0; i < Count; i++)
		{
			if (NameMatches(this[i], name, ns))
			{
				this[i].Value = value;
				return;
			}
		}

		Add(new SdlAttribute(name, value, ns));
	}

	/// <summary>Determines whether an attribute with the given name exists.</summary>
	public bool Contains(string name, string? ns = null)
		=> IndexOf(name, ns) >= 0;

	/// <summary>Gets the value of the first attribute with the given name.</summary>
	public bool TryGetValue(string name, [NotNullWhen(true)] out SdlValue? value, string? ns = null)
	{
		int index = IndexOf(name, ns);
		if (index < 0)
		{
			value = null;
			return false;
		}

		value = this[index].Value;
		return true;
	}

	/// <summary>Removes the first attribute with the given name. Returns <see langword="true"/> if one was removed.</summary>
	public bool Remove(string name, string? ns = null)
	{
		int index = IndexOf(name, ns);
		if (index < 0)
		{
			return false;
		}

		RemoveAt(index);
		return true;
	}

	private int IndexOf(string name, string? ns)
	{
		for (int i = 0; i < Count; i++)
		{
			if (NameMatches(this[i], name, ns))
			{
				return i;
			}
		}

		return -1;
	}

	private static bool NameMatches(SdlAttribute attribute, string name, string? ns)
		=> string.Equals(attribute.Name, name, StringComparison.Ordinal)
			&& (ns is null || string.Equals(attribute.Namespace, ns, StringComparison.Ordinal));

	/// <inheritdoc />
	protected override void InsertItem(int index, SdlAttribute item)
	{
		ArgumentNullException.ThrowIfNull(item);
		base.InsertItem(index, item);
	}

	/// <inheritdoc />
	protected override void SetItem(int index, SdlAttribute item)
	{
		ArgumentNullException.ThrowIfNull(item);
		base.SetItem(index, item);
	}
}
