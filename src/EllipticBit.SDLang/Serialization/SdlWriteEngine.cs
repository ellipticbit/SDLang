using System.Collections;
using EllipticBit.SDLang.Serialization.Metadata;

namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Converts CLR object graphs into the mutable <see cref="Tag"/> DOM according to the metadata contracts and
/// options. One engine instance is created per top-level <c>Serialize</c> call so it can hold the reference and
/// cycle-tracking state for that operation.
/// </summary>
internal sealed class SdlWriteEngine
{
	private readonly SdlSerializerOptions _options;
	private readonly SdlReferenceHandling _referenceHandling;
	private readonly SdlReferenceResolver? _resolver;
	private readonly HashSet<object> _stack = new(ReferenceEqualityComparer.Instance);

	internal SdlWriteEngine(SdlSerializerOptions options)
	{
		_options = options;
		_referenceHandling = options.ReferenceHandler?.Handling ?? SdlReferenceHandling.None;
		_resolver = options.ReferenceHandler?.CreateResolver();
	}

	/// <summary>Writes the root value as one or more top-level tags appended to <paramref name="tags"/>.</summary>
	internal void WriteRoot(object? value, Type type, ICollection<Tag> tags)
	{
		SdlTypeInfo info = _options.GetTypeInfo(type);

		if (info.Kind == SdlTypeKind.Enumerable && value is IEnumerable enumerable)
		{
			Type elementType = info.ElementType ?? typeof(object);
			foreach (object? element in enumerable)
			{
				tags.Add(WriteElementTag(element, elementType, RootElementName(elementType), null));
			}

			return;
		}

		tags.Add(WriteValueTag(value, type, info.TagName ?? "content", info.Namespace, info));
	}

	private Tag WriteValueTag(object? value, Type type, string name, string? ns, SdlTypeInfo info)
	{
		Tag tag = new(name, ns);

		switch (info.Kind)
		{
			case SdlTypeKind.Scalar:
				tag.Values.Add(WriteScalar(value, type, info.ScalarConverter));
				break;
			case SdlTypeKind.Dictionary:
				if (value is IDictionary dictionary)
				{
					PopulateDictionary(tag, dictionary, info.DictionaryValueType ?? typeof(object));
				}

				break;
			case SdlTypeKind.Enumerable:
				if (value is IEnumerable seq)
				{
					Type elementType = info.ElementType ?? typeof(object);
					foreach (object? element in seq)
					{
						tag.Children.Add(WriteElementTag(element, elementType, "item", null));
					}
				}

				break;
			default:
				if (value is not null)
				{
					PopulateObject(tag, value, info);
				}

				break;
		}

		return tag;
	}

	private void PopulateObject(Tag tag, object value, SdlTypeInfo info)
	{
		if (_referenceHandling == SdlReferenceHandling.Preserve && _resolver is not null && !info.Type.IsValueType)
		{
			string id = _resolver.GetReference(value, out bool exists);
			if (exists)
			{
				tag.Attributes.Add("$ref", SdlValue.Create(id));
				return;
			}

			tag.Attributes.Add("$id", SdlValue.Create(id));
		}
		else if (!info.Type.IsValueType && !_stack.Add(value))
		{
			if (_referenceHandling == SdlReferenceHandling.IgnoreCycles)
			{
				tag.Values.Add(SdlValue.Null());
				return;
			}

			throw new SdlException($"A possible object cycle was detected for type '{info.Type}'. Configure ReferenceHandler to handle cycles.");
		}

		try
		{
			foreach (SdlPropertyInfo property in info.Properties)
			{
				if (property.CanRead)
				{
					WriteMember(tag, property, value);
				}
			}
		}
		finally
		{
			if (_referenceHandling != SdlReferenceHandling.Preserve && !info.Type.IsValueType)
			{
				_stack.Remove(value);
			}
		}
	}

	private void WriteMember(Tag tag, SdlPropertyInfo property, object instance)
	{
		object? value = property.GetValue(instance);

		if (ShouldIgnore(property, value))
		{
			return;
		}

		switch (property.Role)
		{
			case SdlMemberRole.Value:
				tag.Values.Add(WriteScalar(value, property.PropertyType, property.Converter));
				break;

			case SdlMemberRole.Attribute:
				tag.Attributes.Add(property.Name, WriteScalar(value, property.PropertyType, property.Converter), property.Namespace);
				break;

			default:
				WriteChildMember(tag, property, value);
				break;
		}
	}

	private void WriteChildMember(Tag tag, SdlPropertyInfo property, object? value)
	{
		if (value is null)
		{
			Tag nullChild = tag.Children.Add(property.Name, property.Namespace);
			nullChild.Values.Add(SdlValue.Null());
			return;
		}

		if (property.IsScalar)
		{
			Tag scalarChild = tag.Children.Add(property.Name, property.Namespace);
			scalarChild.Values.Add(WriteScalar(value, property.PropertyType, property.Converter));
			return;
		}

		SdlTypeInfo memberInfo = _options.GetTypeInfo(property.PropertyType);

		if (memberInfo.Kind == SdlTypeKind.Enumerable && value is IEnumerable enumerable)
		{
			Type elementType = memberInfo.ElementType ?? typeof(object);
			foreach (object? element in enumerable)
			{
				tag.Children.Add(WriteElementTag(element, elementType, property.Name, property.Namespace));
			}

			return;
		}

		tag.Children.Add(WriteValueTag(value, property.PropertyType, property.Name, property.Namespace, memberInfo));
	}

	private Tag WriteElementTag(object? value, Type elementType, string name, string? ns)
	{
		Type runtimeType = value?.GetType() ?? elementType;
		SdlTypeInfo info = _options.GetTypeInfo(runtimeType);

		if (info.Kind == SdlTypeKind.Scalar)
		{
			Tag tag = new(name, ns);
			tag.Values.Add(WriteScalar(value, runtimeType, info.ScalarConverter));
			return tag;
		}

		return WriteValueTag(value, runtimeType, name, ns, info);
	}

	private void PopulateDictionary(Tag tag, IDictionary dictionary, Type valueType)
	{
		SdlTypeInfo valueInfo = _options.GetTypeInfo(valueType);
		foreach (DictionaryEntry entry in dictionary)
		{
			string key = entry.Key.ToString() ?? string.Empty;
			if (valueInfo.Kind == SdlTypeKind.Scalar)
			{
				tag.Attributes.Add(key, WriteScalar(entry.Value, valueType, valueInfo.ScalarConverter));
			}
			else
			{
				tag.Children.Add(WriteElementTag(entry.Value, valueType, key, null));
			}
		}
	}

	private SdlValue WriteScalar(object? value, Type type, SdlConverter? converter)
	{
		if (value is null)
		{
			return SdlValue.Null();
		}

		if (converter is null && (type == typeof(object) || type.IsAbstract || type.IsInterface))
		{
			return SdlValue.FromObject(value);
		}

		SdlConverter resolved = converter ?? SdlConverterRegistry.GetValueConverter(type, _options);
		return resolved.WriteValueAsObject(value, _options);
	}

	private bool ShouldIgnore(SdlPropertyInfo property, object? value) => property.IgnoreCondition switch
	{
		SdlIgnoreCondition.Always => true,
		SdlIgnoreCondition.WhenWritingNull => value is null,
		SdlIgnoreCondition.WhenWritingDefault => IsDefault(property.PropertyType, value),
		_ => false,
	};

	private static bool IsDefault(Type type, object? value)
	{
		if (value is null)
		{
			return true;
		}

		if (!type.IsValueType)
		{
			return false;
		}

		object? defaultValue = Activator.CreateInstance(type);
		return value.Equals(defaultValue);
	}

	private string RootElementName(Type elementType)
	{
		SdlTypeInfo info = _options.GetTypeInfo(elementType);
		return info.Kind == SdlTypeKind.Object ? info.TagName ?? "item" : "item";
	}
}
