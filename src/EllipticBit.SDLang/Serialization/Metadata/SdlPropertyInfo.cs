using System.Reflection;

namespace EllipticBit.SDLang.Serialization.Metadata;

/// <summary>
/// Describes a single serializable member (property or field) of a type: its SDL name, structural role, ordering,
/// converter, ignore condition, and compiled get/set accessors. Mirrors the role of
/// <c>System.Text.Json.Serialization.Metadata.JsonPropertyInfo</c>.
/// </summary>
public sealed class SdlPropertyInfo
{
	private readonly Func<object, object?>? _getter;
	private readonly Action<object, object?>? _setter;

	internal SdlPropertyInfo(
		string name,
		string? @namespace,
		Type propertyType,
		SdlMemberRole role,
		int order,
		SdlIgnoreCondition ignoreCondition,
		bool isScalar,
		SdlConverter? converter,
		Func<object, object?>? getter,
		Action<object, object?>? setter,
		MemberInfo member)
	{
		Name = name;
		Namespace = @namespace;
		PropertyType = propertyType;
		Role = role;
		Order = order;
		IgnoreCondition = ignoreCondition;
		IsScalar = isScalar;
		Converter = converter;
		_getter = getter;
		_setter = setter;
		Member = member;
	}

	/// <summary>Gets the SDL name of the member after applying naming policy and annotations.</summary>
	public string Name { get; }

	/// <summary>Gets the optional SDL namespace for the member's attribute or child tag.</summary>
	public string? Namespace { get; }

	/// <summary>Gets the CLR type of the member.</summary>
	public Type PropertyType { get; }

	/// <summary>Gets the SDL structural role (value, attribute, or child tag) of the member.</summary>
	public SdlMemberRole Role { get; }

	/// <summary>Gets the relative serialization order of the member.</summary>
	public int Order { get; }

	/// <summary>Gets the condition under which the member is skipped while writing.</summary>
	public SdlIgnoreCondition IgnoreCondition { get; }

	/// <summary>Gets a value indicating whether the member maps to a single SDL literal value.</summary>
	public bool IsScalar { get; }

	/// <summary>Gets the value converter for scalar members, or <see langword="null"/> for complex members.</summary>
	public SdlConverter? Converter { get; }

	/// <summary>Gets the reflection member backing this property.</summary>
	public MemberInfo Member { get; }

	/// <summary>Gets a value indicating whether the member can be read.</summary>
	public bool CanRead => _getter is not null;

	/// <summary>Gets a value indicating whether the member can be written.</summary>
	public bool CanWrite => _setter is not null;

	/// <summary>Reads the member value from the supplied instance.</summary>
	public object? GetValue(object instance)
	{
		ArgumentNullException.ThrowIfNull(instance);
		return _getter is null
			? throw new SdlException($"Member '{Name}' cannot be read.")
			: _getter(instance);
	}

	/// <summary>Writes the member value to the supplied instance.</summary>
	public void SetValue(object instance, object? value)
	{
		ArgumentNullException.ThrowIfNull(instance);
		_setter?.Invoke(instance, value);
	}
}
