using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace EllipticBit.SDLang.Serialization.Metadata;

/// <summary>
/// The default reflection-based <see cref="ISdlTypeInfoResolver"/>. It inspects a CLR type and produces an
/// <see cref="SdlTypeInfo"/> contract: classifying it as scalar, enumerable, dictionary, or object; discovering
/// serializable members and their SDL roles; choosing a constructor; and locating extension data. Mirrors
/// <c>System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver</c>.
/// </summary>
public sealed class DefaultSdlTypeInfoResolver : ISdlTypeInfoResolver
{
	private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	/// <summary>Gets the shared resolver instance.</summary>
	public static DefaultSdlTypeInfoResolver Instance { get; } = new();

	/// <inheritdoc />
	public SdlTypeInfo? GetTypeInfo(Type type, SdlSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(type);
		ArgumentNullException.ThrowIfNull(options);

		SdlTypeInfo info = new(type);

		if (SdlConverterRegistry.TryGetValueConverter(type, options, out SdlConverter scalarConverter))
		{
			info.Kind = SdlTypeKind.Scalar;
			info.ScalarConverter = scalarConverter;
			return info;
		}

		if (TryGetDictionary(type, out Type? valueType))
		{
			info.Kind = SdlTypeKind.Dictionary;
			info.DictionaryValueType = valueType;
			info.CreateDictionary = CreateDictionaryFactory(type, valueType!);
			return info;
		}

		if (TryGetEnumerable(type, out Type? elementType))
		{
			info.Kind = SdlTypeKind.Enumerable;
			info.ElementType = elementType;
			info.CreateList = CreateListFactory(type, elementType!);
			return info;
		}

		BuildObject(type, options, info);
		return info;
	}

	private static void BuildObject(Type type, SdlSerializerOptions options, SdlTypeInfo info)
	{
		info.Kind = SdlTypeKind.Object;
		info.TagName = ResolveTagName(type, options);
		info.Namespace = type.GetCustomAttribute<SdlNamespaceAttribute>()?.Namespace;

		ConfigureConstruction(type, options, info);

		List<SdlPropertyInfo> properties = [];
		HashSet<string> seen = new(StringComparer.Ordinal);
		int discoveryOrder = 0;

		foreach (PropertyInfo property in type.GetProperties(InstanceMembers))
		{
			if (property.GetIndexParameters().Length != 0 || !seen.Add(property.Name))
			{
				continue;
			}

			if (TryCreateProperty(property, property.PropertyType, options, ref discoveryOrder, out SdlPropertyInfo? info2))
			{
				AssignProperty(info, properties, info2!);
			}
		}

		if (options.IncludeFields || HasIncludedFields(type))
		{
			foreach (FieldInfo field in type.GetFields(InstanceMembers))
			{
				if (field.IsInitOnly && !field.IsLiteral && field.Name.EndsWith(">k__BackingField", StringComparison.Ordinal))
				{
					continue;
				}

				if (!seen.Add(field.Name))
				{
					continue;
				}

				if (TryCreateField(field, options, ref discoveryOrder, out SdlPropertyInfo? info2))
				{
					AssignProperty(info, properties, info2!);
				}
			}
		}

		properties.Sort(static (a, b) => a.Order != b.Order
			? a.Order.CompareTo(b.Order)
			: RoleRank(a.Role).CompareTo(RoleRank(b.Role)));

		info.Properties = properties;
	}

	private static void AssignProperty(SdlTypeInfo info, List<SdlPropertyInfo> properties, SdlPropertyInfo property)
	{
		if (property.Member.GetCustomAttribute<SdlExtensionDataAttribute>() is not null)
		{
			info.ExtensionData = property;
			return;
		}

		properties.Add(property);
	}

	private static bool TryCreateProperty(PropertyInfo property, Type propertyType, SdlSerializerOptions options, ref int discoveryOrder, out SdlPropertyInfo? info)
	{
		info = null;
		bool include = property.GetCustomAttribute<SdlIncludeAttribute>() is not null;
		bool hasPublicGetter = property.GetMethod is { IsPublic: true };
		bool hasPublicSetter = property.SetMethod is { IsPublic: true };

		if (!include && !hasPublicGetter && !hasPublicSetter)
		{
			return false;
		}

		Func<object, object?>? getter = property.CanRead ? property.GetValue : null;
		Action<object, object?>? setter = property.CanWrite ? property.SetValue : null;

		info = CreateMember(property, propertyType, options, getter, setter, ref discoveryOrder);
		return info is not null;
	}

	private static bool TryCreateField(FieldInfo field, SdlSerializerOptions options, ref int discoveryOrder, out SdlPropertyInfo? info)
	{
		info = null;
		bool include = field.GetCustomAttribute<SdlIncludeAttribute>() is not null;
		if (!include && !field.IsPublic)
		{
			return false;
		}

		info = CreateMember(field, field.FieldType, options, field.GetValue, field.SetValue, ref discoveryOrder);
		return info is not null;
	}

	private static SdlPropertyInfo? CreateMember(
		MemberInfo member,
		Type memberType,
		SdlSerializerOptions options,
		Func<object, object?>? getter,
		Action<object, object?>? setter,
		ref int discoveryOrder)
	{
		SdlIgnoreAttribute? ignore = member.GetCustomAttribute<SdlIgnoreAttribute>();
		if (ignore is { Condition: SdlIgnoreCondition.Always })
		{
			return null;
		}

		string name = ResolveMemberName(member, options);
		bool isScalar = SdlConverterRegistry.TryGetValueConverter(memberType, options, out SdlConverter converter);
		SdlConverter? memberConverter = SdlConverterRegistry.FromAttribute(member, options) ?? (isScalar ? converter : null);

		(SdlMemberRole role, string? ns, string roleName) = ResolveRole(member, memberType, isScalar, options, name);

		int order = member.GetCustomAttribute<SdlPropertyOrderAttribute>()?.Order ?? discoveryOrder++;
		SdlIgnoreCondition condition = ignore?.Condition ?? options.DefaultIgnoreCondition;

		return new SdlPropertyInfo(roleName, ns, memberType, role, order, condition, isScalar, memberConverter, getter, setter, member);
	}

	private static (SdlMemberRole Role, string? Namespace, string Name) ResolveRole(
		MemberInfo member, Type memberType, bool isScalar, SdlSerializerOptions options, string defaultName)
	{
		if (member.GetCustomAttribute<SdlValueAttribute>() is not null)
		{
			return (SdlMemberRole.Value, null, defaultName);
		}

		SdlAttributeAttribute? attr = member.GetCustomAttribute<SdlAttributeAttribute>();
		if (attr is not null)
		{
			return (SdlMemberRole.Attribute, attr.Namespace ?? MemberNamespace(member), attr.Name ?? defaultName);
		}

		SdlChildAttribute? child = member.GetCustomAttribute<SdlChildAttribute>();
		if (child is not null)
		{
			return (SdlMemberRole.Child, child.Namespace ?? MemberNamespace(member), child.Name ?? defaultName);
		}

		// No structural attribute: scalars default to the configured scalar role; complex members become children.
		if (!isScalar)
		{
			return (SdlMemberRole.Child, MemberNamespace(member), defaultName);
		}

		return (options.DefaultScalarRole, MemberNamespace(member), defaultName);
	}

	private static string? MemberNamespace(MemberInfo member)
		=> member.GetCustomAttribute<SdlNamespaceAttribute>()?.Namespace;

	private static void ConfigureConstruction(Type type, SdlSerializerOptions options, SdlTypeInfo info)
	{
		if (type.IsValueType)
		{
			info.CreateInstance = () => Activator.CreateInstance(type)!;
		}

		ConstructorInfo? chosen = SelectConstructor(type);
		if (chosen is null)
		{
			return;
		}

		ParameterInfo[] parameters = chosen.GetParameters();
		if (parameters.Length == 0)
		{
			info.CreateInstance = () => chosen.Invoke(null)!;
			return;
		}

		List<SdlParameterInfo> mapped = new(parameters.Length);
		foreach (ParameterInfo parameter in parameters)
		{
			string name = parameter.GetCustomAttribute<SdlPropertyNameAttribute>()?.Name
				?? options.ConvertName(parameter.Name ?? $"arg{parameter.Position}");
			mapped.Add(new SdlParameterInfo(name, parameter.ParameterType, parameter.Position, parameter.DefaultValue, parameter.HasDefaultValue));
		}

		info.ParameterizedConstructor = new SdlParameterizedConstructor(args => chosen.Invoke(args)!, mapped);
	}

	private static ConstructorInfo? SelectConstructor(Type type)
	{
		ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		ConstructorInfo? annotated = Array.Find(constructors, static c => c.GetCustomAttribute<SdlConstructorAttribute>() is not null);
		if (annotated is not null)
		{
			return annotated;
		}

		ConstructorInfo? parameterless = Array.Find(constructors, static c => c.GetParameters().Length == 0 && c.IsPublic);
		if (parameterless is not null)
		{
			return parameterless;
		}

		ConstructorInfo[] publicCtors = Array.FindAll(constructors, static c => c.IsPublic);
		if (publicCtors.Length == 1)
		{
			return publicCtors[0];
		}

		return null;
	}

	private static string ResolveTagName(Type type, SdlSerializerOptions options)
	{
		string raw = type.Name;
		int tick = raw.IndexOf('`', StringComparison.Ordinal);
		if (tick >= 0)
		{
			raw = raw[..tick];
		}

		return options.ConvertName(raw);
	}

	private static string ResolveMemberName(MemberInfo member, SdlSerializerOptions options)
	{
		SdlPropertyNameAttribute? nameAttribute = member.GetCustomAttribute<SdlPropertyNameAttribute>();
		return nameAttribute is not null ? nameAttribute.Name : options.ConvertName(member.Name);
	}

	private static bool HasIncludedFields(Type type)
	{
		foreach (FieldInfo field in type.GetFields(InstanceMembers))
		{
			if (field.GetCustomAttribute<SdlIncludeAttribute>() is not null)
			{
				return true;
			}
		}

		return false;
	}

	private static bool TryGetDictionary(Type type, out Type? valueType)
	{
		valueType = null;
		foreach (Type iface in EnumerateInterfaces(type))
		{
			if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
			{
				Type[] args = iface.GetGenericArguments();
				if (args[0] == typeof(string))
				{
					valueType = args[1];
					return true;
				}
			}
		}

		return false;
	}

	private static bool TryGetEnumerable(Type type, out Type? elementType)
	{
		elementType = null;
		if (type == typeof(string))
		{
			return false;
		}

		if (type.IsArray)
		{
			elementType = type.GetElementType();
			return true;
		}

		foreach (Type iface in EnumerateInterfaces(type))
		{
			if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
			{
				elementType = iface.GetGenericArguments()[0];
				return true;
			}
		}

		return false;
	}

	private static IEnumerable<Type> EnumerateInterfaces(Type type)
	{
		if (type.IsInterface)
		{
			yield return type;
		}

		foreach (Type iface in type.GetInterfaces())
		{
			yield return iface;
		}
	}

	private static Func<IList>? CreateListFactory(Type type, Type elementType)
	{
		if (type.IsArray)
		{
			Type listType = typeof(List<>).MakeGenericType(elementType);
			return () => (IList)Activator.CreateInstance(listType)!;
		}

		if (type.IsInterface)
		{
			Type listType = typeof(List<>).MakeGenericType(elementType);
			return () => (IList)Activator.CreateInstance(listType)!;
		}

		if (typeof(IList).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is not null)
		{
			return () => (IList)Activator.CreateInstance(type)!;
		}

		Type fallback = typeof(List<>).MakeGenericType(elementType);
		return () => (IList)Activator.CreateInstance(fallback)!;
	}

	private static Func<IDictionary>? CreateDictionaryFactory(Type type, Type valueType)
	{
		if (type.IsInterface)
		{
			Type dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType);
			return () => (IDictionary)Activator.CreateInstance(dictType)!;
		}

		if (typeof(IDictionary).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is not null)
		{
			return () => (IDictionary)Activator.CreateInstance(type)!;
		}

		Type fallback = typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType);
		return () => (IDictionary)Activator.CreateInstance(fallback)!;
	}

	private static int RoleRank(SdlMemberRole role) => role switch
	{
		SdlMemberRole.Value => 0,
		SdlMemberRole.Attribute => 1,
		_ => 2,
	};
}
