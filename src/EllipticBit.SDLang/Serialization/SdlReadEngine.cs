using System.Collections;
using EllipticBit.SDLang.Serialization.Metadata;

namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Materializes CLR object graphs from the <see cref="Tag"/> DOM according to the metadata contracts and options.
/// One engine instance is created per top-level <c>Deserialize</c> call so it can hold reference-resolution state.
/// </summary>
internal sealed class SdlReadEngine
{
	private readonly SdlSerializerOptions _options;
	private readonly SdlReferenceHandling _referenceHandling;
	private readonly SdlReferenceResolver? _resolver;

	internal SdlReadEngine(SdlSerializerOptions options)
	{
		_options = options;
		_referenceHandling = options.ReferenceHandler?.Handling ?? SdlReferenceHandling.None;
		_resolver = options.ReferenceHandler?.CreateResolver();
	}

	/// <summary>Reads the root value of type <paramref name="type"/> from the document's top-level tags.</summary>
	internal object? ReadRoot(IReadOnlyList<Tag> tags, Type type)
	{
		SdlTypeInfo info = _options.GetTypeInfo(type);

		if (info.Kind == SdlTypeKind.Enumerable)
		{
			return ReadEnumerableFromTags(tags, info);
		}

		if (tags.Count == 0)
		{
			return null;
		}

		return ReadTag(tags[0], type, info);
	}

	private object? ReadTag(Tag tag, Type type, SdlTypeInfo info) => info.Kind switch
	{
		SdlTypeKind.Scalar => ReadScalar(tag.Value ?? SdlValue.Null(), type, info.ScalarConverter),
		SdlTypeKind.Dictionary => ReadDictionary(tag, info),
		SdlTypeKind.Enumerable => ReadEnumerableFromChildren(tag, info),
		_ => ReadObject(tag, info),
	};

	private object? ReadObject(Tag tag, SdlTypeInfo info)
	{
		if (_referenceHandling == SdlReferenceHandling.Preserve && _resolver is not null)
		{
			if (tag.Attributes.TryGetValue("$ref", out SdlValue? refValue))
			{
				return _resolver.ResolveReference(refValue.AsString());
			}
		}

		return info.ParameterizedConstructor is not null
			? ReadImmutable(tag, info)
			: ReadMutable(tag, info);
	}

	private object ReadMutable(Tag tag, SdlTypeInfo info)
	{
		if (info.CreateInstance is null)
		{
			throw new SdlException($"Type '{info.Type}' has no usable constructor for deserialization.");
		}

		object instance = info.CreateInstance();
		RegisterReference(tag, instance);

		int valueIndex = 0;
		Dictionary<string, SdlPropertyInfo> attributeMap = BuildAttributeMap(info);
		HashSet<SdlAttribute>? consumed = info.ExtensionData is not null ? [] : null;

		foreach (SdlPropertyInfo property in info.Properties)
		{
			switch (property.Role)
			{
				case SdlMemberRole.Value:
					if (valueIndex < tag.Values.Count && property.CanWrite)
					{
						property.SetValue(instance, ReadScalar(tag.Values[valueIndex], property.PropertyType, property.Converter));
					}

					valueIndex++;
					break;

				case SdlMemberRole.Attribute:
					ApplyAttribute(tag, property, instance, consumed);
					break;

				default:
					ApplyChild(tag, property, instance);
					break;
			}
		}

		ApplyExtensionData(tag, info, instance, attributeMap, consumed);
		return instance;
	}

	private object ReadImmutable(Tag tag, SdlTypeInfo info)
	{
		SdlParameterizedConstructor ctor = info.ParameterizedConstructor!;
		object?[] args = new object?[ctor.Parameters.Count];

		Dictionary<string, SdlPropertyInfo> byName = new(StringComparer.OrdinalIgnoreCase);
		foreach (SdlPropertyInfo property in info.Properties)
		{
			byName.TryAdd(property.Name, property);
		}

		int valueIndex = 0;
		List<SdlPropertyInfo> valueProperties = [];
		foreach (SdlPropertyInfo property in info.Properties)
		{
			if (property.Role == SdlMemberRole.Value)
			{
				valueProperties.Add(property);
			}
		}

		for (int i = 0; i < ctor.Parameters.Count; i++)
		{
			SdlParameterInfo parameter = ctor.Parameters[i];
			if (byName.TryGetValue(parameter.Name, out SdlPropertyInfo? property))
			{
				args[i] = ReadParameterValue(tag, property, ref valueIndex, valueProperties);
			}
			else
			{
				args[i] = parameter.HasDefaultValue ? parameter.DefaultValue : DefaultOf(parameter.ParameterType);
			}
		}

		object instance = ctor.Invoke(args);
		RegisterReference(tag, instance);
		return instance;
	}

	private object? ReadParameterValue(Tag tag, SdlPropertyInfo property, ref int valueIndex, List<SdlPropertyInfo> valueProperties)
	{
		switch (property.Role)
		{
			case SdlMemberRole.Value:
				int index = valueProperties.IndexOf(property);
				return index >= 0 && index < tag.Values.Count
					? ReadScalar(tag.Values[index], property.PropertyType, property.Converter)
					: DefaultOf(property.PropertyType);

			case SdlMemberRole.Attribute:
				return tag.Attributes.TryGetValue(property.Name, out SdlValue? attr, property.Namespace)
					? ReadScalar(attr, property.PropertyType, property.Converter)
					: DefaultOf(property.PropertyType);

			default:
				return ReadChildValue(tag, property);
		}
	}

	private void ApplyAttribute(Tag tag, SdlPropertyInfo property, object instance, HashSet<SdlAttribute>? consumed)
	{
		if (!property.CanWrite)
		{
			return;
		}

		foreach (SdlAttribute attribute in tag.Attributes)
		{
			if (string.Equals(attribute.Name, property.Name, StringComparison.OrdinalIgnoreCase)
				&& (property.Namespace is null || string.Equals(attribute.Namespace, property.Namespace, StringComparison.Ordinal)))
			{
				property.SetValue(instance, ReadScalar(attribute.Value, property.PropertyType, property.Converter));
				consumed?.Add(attribute);
				return;
			}
		}

		if (_options.UnmappedMemberHandling == SdlUnmappedMemberHandling.Disallow)
		{
			// Member exists in the contract but absent in the document; not an unmapped-member error.
		}
	}

	private void ApplyChild(Tag tag, SdlPropertyInfo property, object instance)
	{
		if (!property.CanWrite && !property.IsScalar)
		{
			// Read-only collection members could be populated in place, but we only support settable members here.
		}

		object? value = ReadChildValue(tag, property);
		if (property.CanWrite)
		{
			property.SetValue(instance, value);
		}
	}

	private object? ReadChildValue(Tag tag, SdlPropertyInfo property)
	{
		if (property.IsScalar)
		{
			Tag? scalarChild = tag.Child(property.Name, property.Namespace);
			return scalarChild?.Value is { } v
				? ReadScalar(v, property.PropertyType, property.Converter)
				: DefaultOf(property.PropertyType);
		}

		SdlTypeInfo memberInfo = _options.GetTypeInfo(property.PropertyType);

		if (memberInfo.Kind == SdlTypeKind.Enumerable)
		{
			List<Tag> matches = [.. tag.ChildrenNamed(property.Name, property.Namespace)];
			return matches.Count == 0 ? DefaultOf(property.PropertyType) : BuildEnumerable(matches, memberInfo);
		}

		Tag? child = tag.Child(property.Name, property.Namespace);
		return child is null ? DefaultOf(property.PropertyType) : ReadTag(child, property.PropertyType, memberInfo);
	}

	private object ReadDictionary(Tag tag, SdlTypeInfo info)
	{
		IDictionary dictionary = info.CreateDictionary?.Invoke()
			?? throw new SdlException($"Type '{info.Type}' cannot be constructed as a dictionary.");
		Type valueType = info.DictionaryValueType ?? typeof(object);
		SdlTypeInfo valueInfo = _options.GetTypeInfo(valueType);

		foreach (SdlAttribute attribute in tag.Attributes)
		{
			if (attribute.Name is "$id" or "$ref")
			{
				continue;
			}

			dictionary[attribute.Name] = ReadScalar(attribute.Value, valueType, valueInfo.ScalarConverter);
		}

		foreach (Tag child in tag.Children)
		{
			string key = child.Name ?? "content";
			dictionary[key] = ReadTag(child, valueType, valueInfo);
		}

		return dictionary;
	}

	private object ReadEnumerableFromChildren(Tag tag, SdlTypeInfo info)
		=> BuildEnumerable([.. tag.Children], info);

	private object ReadEnumerableFromTags(IReadOnlyList<Tag> tags, SdlTypeInfo info)
		=> BuildEnumerable(tags, info);

	private object BuildEnumerable(IReadOnlyList<Tag> elements, SdlTypeInfo info)
	{
		Type elementType = info.ElementType ?? typeof(object);
		SdlTypeInfo elementInfo = _options.GetTypeInfo(elementType);
		IList list = info.CreateList?.Invoke()
			?? throw new SdlException($"Type '{info.Type}' cannot be constructed as a list.");

		foreach (Tag element in elements)
		{
			object? value = elementInfo.Kind == SdlTypeKind.Scalar
				? ReadScalar(element.Value ?? SdlValue.Null(), elementType, elementInfo.ScalarConverter)
				: ReadTag(element, elementType, elementInfo);
			list.Add(value);
		}

		if (info.Type.IsArray)
		{
			Array array = Array.CreateInstance(elementType, list.Count);
			list.CopyTo(array, 0);
			return array;
		}

		return list;
	}

	private void ApplyExtensionData(Tag tag, SdlTypeInfo info, object instance, Dictionary<string, SdlPropertyInfo> attributeMap, HashSet<SdlAttribute>? consumed)
	{
		if (info.ExtensionData is not { } extension || !extension.CanWrite)
		{
			return;
		}

		SdlTypeInfo extInfo = _options.GetTypeInfo(extension.PropertyType);
		if (extInfo.CreateDictionary is null)
		{
			return;
		}

		IDictionary target = extInfo.CreateDictionary();
		Type valueType = extInfo.DictionaryValueType ?? typeof(object);
		SdlTypeInfo valueInfo = _options.GetTypeInfo(valueType);
		bool any = false;

		foreach (SdlAttribute attribute in tag.Attributes)
		{
			if (attribute.Name is "$id" or "$ref")
			{
				continue;
			}

			if ((consumed is null || !consumed.Contains(attribute)) && !attributeMap.ContainsKey(attribute.Name))
			{
				target[attribute.Name] = ReadScalar(attribute.Value, valueType, valueInfo.ScalarConverter);
				any = true;
			}
		}

		if (any)
		{
			extension.SetValue(instance, target);
		}
	}

	private static Dictionary<string, SdlPropertyInfo> BuildAttributeMap(SdlTypeInfo info)
	{
		Dictionary<string, SdlPropertyInfo> map = new(StringComparer.OrdinalIgnoreCase);
		foreach (SdlPropertyInfo property in info.Properties)
		{
			if (property.Role == SdlMemberRole.Attribute)
			{
				map.TryAdd(property.Name, property);
			}
		}

		return map;
	}

	private void RegisterReference(Tag tag, object instance)
	{
		if (_referenceHandling == SdlReferenceHandling.Preserve
			&& _resolver is not null
			&& tag.Attributes.TryGetValue("$id", out SdlValue? id))
		{
			_resolver.AddReference(id.AsString(), instance);
		}
	}

	private object? ReadScalar(SdlValue value, Type type, SdlConverter? converter)
	{
		if (value.IsNull && !type.IsValueType)
		{
			return null;
		}

		if (type == typeof(object))
		{
			return value.Value;
		}

		SdlConverter resolved = converter ?? SdlConverterRegistry.GetValueConverter(type, _options);
		return resolved.ReadValueAsObject(value, _options);
	}

	private static object? DefaultOf(Type type)
		=> type.IsValueType ? Activator.CreateInstance(type) : null;
}
