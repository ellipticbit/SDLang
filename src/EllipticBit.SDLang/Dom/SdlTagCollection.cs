using System.Collections.ObjectModel;

namespace EllipticBit.SDLang;

/// <summary>
/// An ordered collection of child <see cref="Tag"/> nodes. Maintains the <see cref="SdlNode.Parent"/> back-pointer
/// of each child so the DOM stays internally consistent as it is mutated.
/// </summary>
public sealed class SdlTagCollection : Collection<Tag>
{
	private readonly Tag _owner;

	internal SdlTagCollection(Tag owner) => _owner = owner;

	/// <summary>Adds a new anonymous or named child tag and returns it.</summary>
	public Tag Add(string? name, string? ns = null)
	{
		Tag tag = new(name, ns);
		Add(tag);
		return tag;
	}

	/// <summary>Returns the first child tag with the given name, or <see langword="null"/> if none exists.</summary>
	public Tag? FirstOrDefault(string name, string? ns = null)
	{
		foreach (Tag tag in this)
		{
			if (tag.NameMatches(name, ns))
			{
				return tag;
			}
		}

		return null;
	}

	/// <summary>Returns all child tags with the given name in document order.</summary>
	public IEnumerable<Tag> Where(string name, string? ns = null)
	{
		foreach (Tag tag in this)
		{
			if (tag.NameMatches(name, ns))
			{
				yield return tag;
			}
		}
	}

	/// <inheritdoc />
	protected override void InsertItem(int index, Tag item)
	{
		ArgumentNullException.ThrowIfNull(item);
		Detach(item);
		item.Parent = _owner;
		base.InsertItem(index, item);
	}

	/// <inheritdoc />
	protected override void SetItem(int index, Tag item)
	{
		ArgumentNullException.ThrowIfNull(item);
		Tag existing = this[index];
		if (!ReferenceEquals(existing, item))
		{
			existing.Parent = null;
			Detach(item);
			item.Parent = _owner;
		}

		base.SetItem(index, item);
	}

	/// <inheritdoc />
	protected override void RemoveItem(int index)
	{
		this[index].Parent = null;
		base.RemoveItem(index);
	}

	/// <inheritdoc />
	protected override void ClearItems()
	{
		foreach (Tag tag in this)
		{
			tag.Parent = null;
		}

		base.ClearItems();
	}

	private static void Detach(Tag item)
		=> item.Parent?.Children.Remove(item);
}
