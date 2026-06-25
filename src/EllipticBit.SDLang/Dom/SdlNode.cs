namespace EllipticBit.SDLang;

/// <summary>
/// The abstract base for nodes in the SDLang DOM. Currently <see cref="Tag"/> is the only node kind; the base
/// exists to mirror the <c>System.Text.Json.Nodes.JsonNode</c> hierarchy and to carry the parent back-pointer
/// used when mutating the tree.
/// </summary>
public abstract class SdlNode
{
	/// <summary>Gets the parent tag of this node, or <see langword="null"/> if it is a root.</summary>
	public Tag? Parent { get; internal set; }

	/// <summary>Writes this node and its descendants to the supplied writer.</summary>
	public abstract void WriteTo(Utf8SdlWriter writer);
}
