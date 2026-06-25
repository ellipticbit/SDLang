using System.Collections;
using System.Reflection;

namespace EllipticBit.SDLang.Serialization.Metadata;

/// <summary>
/// Describes how a CLR type maps to and from SDLang: its tag name/namespace, its constructor, the ordered set of
/// serializable members, the optional extension-data member, and (for collections) the element type. Mirrors the
/// role of <c>System.Text.Json.Serialization.Metadata.JsonTypeInfo</c>.
/// </summary>
public sealed class SdlTypeInfo
{
	internal SdlTypeInfo(Type type)
	{
		Type = type;
		Properties = [];
	}

	/// <summary>Gets the CLR type described by this metadata.</summary>
	public Type Type { get; }

	/// <summary>Gets or sets the default SDL tag name used when emitting an instance as a tag.</summary>
	public string? TagName { get; internal set; }

	/// <summary>Gets or sets the SDL namespace applied to the type's tag.</summary>
	public string? Namespace { get; internal set; }

	/// <summary>Gets the ordered list of serializable members.</summary>
	public IReadOnlyList<SdlPropertyInfo> Properties { get; internal set; }

	/// <summary>Gets the kind of CLR shape this type represents.</summary>
	public SdlTypeKind Kind { get; internal set; }

	/// <summary>Gets the element type for collection/enumerable types, or <see langword="null"/>.</summary>
	public Type? ElementType { get; internal set; }

	/// <summary>Gets the value type for dictionary types, or <see langword="null"/>.</summary>
	public Type? DictionaryValueType { get; internal set; }

	/// <summary>Gets the extension-data member, or <see langword="null"/> if none is declared.</summary>
	public SdlPropertyInfo? ExtensionData { get; internal set; }

	/// <summary>Gets the parameterless factory for the type, or <see langword="null"/> for parameterized construction.</summary>
	public Func<object>? CreateInstance { get; internal set; }

	/// <summary>Gets the parameterized factory metadata for immutable types, or <see langword="null"/>.</summary>
	public SdlParameterizedConstructor? ParameterizedConstructor { get; internal set; }

	/// <summary>Gets the converter for scalar types, or <see langword="null"/> for object/collection types.</summary>
	public SdlConverter? ScalarConverter { get; internal set; }

	/// <summary>Creates an empty collection/list instance for deserialization, or <see langword="null"/>.</summary>
	public Func<IList>? CreateList { get; internal set; }

	/// <summary>Creates an empty dictionary instance for deserialization, or <see langword="null"/>.</summary>
	public Func<IDictionary>? CreateDictionary { get; internal set; }
}

/// <summary>Classifies how a type is serialized.</summary>
public enum SdlTypeKind
{
	/// <summary>A scalar mapped to a single SDL literal.</summary>
	Scalar = 0,

	/// <summary>An object mapped to a tag with values/attributes/children.</summary>
	Object,

	/// <summary>An enumerable mapped to repeated child tags.</summary>
	Enumerable,

	/// <summary>A dictionary mapped to attributes or child tags.</summary>
	Dictionary,
}

/// <summary>Describes a constructor used to build immutable types from SDL members.</summary>
public sealed class SdlParameterizedConstructor
{
	internal SdlParameterizedConstructor(Func<object?[], object> invoke, IReadOnlyList<SdlParameterInfo> parameters)
	{
		Invoke = invoke;
		Parameters = parameters;
	}

	/// <summary>Gets the factory that invokes the constructor with positional arguments.</summary>
	public Func<object?[], object> Invoke { get; }

	/// <summary>Gets the ordered constructor parameters.</summary>
	public IReadOnlyList<SdlParameterInfo> Parameters { get; }
}

/// <summary>Describes a single constructor parameter and the SDL member it binds to.</summary>
public sealed class SdlParameterInfo
{
	internal SdlParameterInfo(string name, Type parameterType, int position, object? defaultValue, bool hasDefaultValue)
	{
		Name = name;
		ParameterType = parameterType;
		Position = position;
		DefaultValue = defaultValue;
		HasDefaultValue = hasDefaultValue;
	}

	/// <summary>Gets the SDL name the parameter binds to.</summary>
	public string Name { get; }

	/// <summary>Gets the CLR parameter type.</summary>
	public Type ParameterType { get; }

	/// <summary>Gets the positional index of the parameter.</summary>
	public int Position { get; }

	/// <summary>Gets the default value used when the SDL member is absent.</summary>
	public object? DefaultValue { get; }

	/// <summary>Gets a value indicating whether the parameter declares a default value.</summary>
	public bool HasDefaultValue { get; }
}
