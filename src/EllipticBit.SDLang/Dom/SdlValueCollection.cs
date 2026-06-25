using System.Collections.ObjectModel;

namespace EllipticBit.SDLang;

/// <summary>
/// An ordered collection of <see cref="SdlValue"/> instances belonging to a <see cref="Tag"/>. Rejects
/// <see langword="null"/> entries; assign <see cref="SdlValue.Null()"/> to represent the SDL <c>null</c> literal.
/// </summary>
public sealed class SdlValueCollection : Collection<SdlValue>
{
	/// <summary>Adds a CLR object as an inferred <see cref="SdlValue"/>.</summary>
	public void Add(object? value) => base.Add(SdlValue.FromObject(value));

	/// <inheritdoc />
	protected override void InsertItem(int index, SdlValue item)
	{
		ArgumentNullException.ThrowIfNull(item);
		base.InsertItem(index, item);
	}

	/// <inheritdoc />
	protected override void SetItem(int index, SdlValue item)
	{
		ArgumentNullException.ThrowIfNull(item);
		base.SetItem(index, item);
	}
}
