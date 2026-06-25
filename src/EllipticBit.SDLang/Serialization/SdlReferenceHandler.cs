namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Tracks object references during (de)serialization so cyclic and shared object graphs can be preserved or
/// safely ignored. Mirrors <c>System.Text.Json.Serialization.ReferenceHandler</c>. Use the static
/// <see cref="Preserve"/> or <see cref="IgnoreCycles"/> handlers, or derive a custom handler.
/// </summary>
public abstract class SdlReferenceHandler
{
	/// <summary>
	/// Gets a handler that preserves reference identity using <c>$id</c> and <c>$ref</c> attributes, allowing
	/// shared and circular references to round-trip.
	/// </summary>
	public static SdlReferenceHandler Preserve { get; } = new PreserveReferenceHandler();

	/// <summary>Gets a handler that writes <c>null</c> in place of references that would create a cycle.</summary>
	public static SdlReferenceHandler IgnoreCycles { get; } = new IgnoreCyclesReferenceHandler();

	/// <summary>Gets the reference-resolution strategy this handler represents.</summary>
	public abstract SdlReferenceHandling Handling { get; }

	/// <summary>Creates a fresh <see cref="SdlReferenceResolver"/> for a single serialization or deserialization pass.</summary>
	public abstract SdlReferenceResolver CreateResolver();

	private sealed class PreserveReferenceHandler : SdlReferenceHandler
	{
		public override SdlReferenceHandling Handling => SdlReferenceHandling.Preserve;

		public override SdlReferenceResolver CreateResolver() => new SdlReferenceResolver();
	}

	private sealed class IgnoreCyclesReferenceHandler : SdlReferenceHandler
	{
		public override SdlReferenceHandling Handling => SdlReferenceHandling.IgnoreCycles;

		public override SdlReferenceResolver CreateResolver() => new SdlReferenceResolver();
	}
}

/// <summary>Identifies the reference-resolution strategy of an <see cref="SdlReferenceHandler"/>.</summary>
public enum SdlReferenceHandling
{
	/// <summary>References are not tracked; cyclic graphs cause an exception (the default).</summary>
	None = 0,

	/// <summary>Reference identity is preserved using <c>$id</c>/<c>$ref</c>.</summary>
	Preserve,

	/// <summary>Cyclic references are replaced with <c>null</c>.</summary>
	IgnoreCycles,
}
