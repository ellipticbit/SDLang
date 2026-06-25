using System.Runtime.CompilerServices;

namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Maintains the bidirectional mapping between object instances and their string reference identifiers for a
/// single (de)serialization pass. Mirrors <c>System.Text.Json.Serialization.ReferenceResolver</c>.
/// </summary>
public sealed class SdlReferenceResolver
{
	private readonly Dictionary<string, object> _byId = new(StringComparer.Ordinal);
	private readonly Dictionary<object, string> _byObject = new(ReferenceEqualityComparer.Instance);
	private uint _next = 1;

	/// <summary>Resolves the object previously registered under <paramref name="referenceId"/>.</summary>
	public object ResolveReference(string referenceId)
		=> _byId.TryGetValue(referenceId, out object? value)
			? value
			: throw new SdlException($"Reference '{referenceId}' was used before it was defined.");

	/// <summary>Registers <paramref name="value"/> under the supplied identifier while reading.</summary>
	public void AddReference(string referenceId, object value)
	{
		ArgumentNullException.ThrowIfNull(value);
		if (!_byId.TryAdd(referenceId, value))
		{
			throw new SdlException($"Reference identifier '{referenceId}' is defined more than once.");
		}
	}

	/// <summary>
	/// Returns the identifier for <paramref name="value"/> while writing, assigning a new one if needed.
	/// <paramref name="alreadyExists"/> is <see langword="true"/> when the object had been seen before.
	/// </summary>
	public string GetReference(object value, out bool alreadyExists)
	{
		ArgumentNullException.ThrowIfNull(value);
		if (_byObject.TryGetValue(value, out string? existing))
		{
			alreadyExists = true;
			return existing;
		}

		string id = _next++.ToString(System.Globalization.CultureInfo.InvariantCulture);
		_byObject[value] = id;
		alreadyExists = false;
		return id;
	}

	/// <summary>Determines whether <paramref name="value"/> is currently being written (used for cycle detection).</summary>
	public bool Contains(object value) => _byObject.ContainsKey(value);
}
