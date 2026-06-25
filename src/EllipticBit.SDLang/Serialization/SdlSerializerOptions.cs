using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using EllipticBit.SDLang.Serialization.Metadata;

namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Configures the behavior of <see cref="SdlSerializer"/>. Mirrors <c>System.Text.Json.JsonSerializerOptions</c>:
/// it carries the converter registry, naming policy, formatting, and the various handling modes, and becomes
/// effectively read-only the first time it is used for (de)serialization.
/// </summary>
public sealed class SdlSerializerOptions
{
	private readonly ConverterCollection _converters;
	private readonly ConcurrentDictionary<Type, object> _metadataCache = new();

	private SdlNamingPolicy? _namingPolicy;
	private bool _writeIndented;
	private int _maxDepth = 64;
	private SdlIgnoreCondition _defaultIgnoreCondition = SdlIgnoreCondition.Never;
	private SdlNumberHandling _numberHandling = SdlNumberHandling.Strict;
	private SdlUnmappedMemberHandling _unmappedMemberHandling = SdlUnmappedMemberHandling.Skip;
	private SdlCommentHandling _readCommentHandling = SdlCommentHandling.Skip;
	private SdlReferenceHandler? _referenceHandler;
	private bool _includeFields;
	private bool _propertyNameCaseInsensitive = true;
	private SdlMemberRole _defaultScalarRole = SdlMemberRole.Attribute;
	private ISdlTypeInfoResolver _typeInfoResolver = DefaultSdlTypeInfoResolver.Instance;
	private readonly ConcurrentDictionary<Type, SdlTypeInfo> _typeInfoCache = new();

	/// <summary>Gets a cached, default-configured, read-only options instance.</summary>
	public static SdlSerializerOptions Default { get; } = CreateLocked();

	/// <summary>Initializes a new, mutable options instance with library defaults.</summary>
	public SdlSerializerOptions() => _converters = new ConverterCollection(this);

	/// <summary>Initializes a new, mutable options instance copying the settings of <paramref name="other"/>.</summary>
	public SdlSerializerOptions(SdlSerializerOptions other)
	{
		ArgumentNullException.ThrowIfNull(other);
		_converters = new ConverterCollection(this);
		_namingPolicy = other._namingPolicy;
		_writeIndented = other._writeIndented;
		_maxDepth = other._maxDepth;
		_defaultIgnoreCondition = other._defaultIgnoreCondition;
		_numberHandling = other._numberHandling;
		_unmappedMemberHandling = other._unmappedMemberHandling;
		_readCommentHandling = other._readCommentHandling;
		_referenceHandler = other._referenceHandler;
		_includeFields = other._includeFields;
		_propertyNameCaseInsensitive = other._propertyNameCaseInsensitive;
		_defaultScalarRole = other._defaultScalarRole;
		_typeInfoResolver = other._typeInfoResolver;
		foreach (SdlConverter converter in other._converters)
		{
			_converters.Add(converter);
		}
	}

	/// <summary>Gets a value indicating whether this instance has been locked against further mutation.</summary>
	public bool IsReadOnly { get; private set; }

	/// <summary>Gets the mutable list of user-registered converters, searched before the built-in converters.</summary>
	public IList<SdlConverter> Converters => _converters;

	/// <summary>Gets or sets the policy used to translate CLR member names to SDL names. Defaults to none (verbatim names).</summary>
	public SdlNamingPolicy? PropertyNamingPolicy
	{
		get => _namingPolicy;
		set => Set(ref _namingPolicy, value);
	}

	/// <summary>Gets or sets a value indicating whether serialized output is pretty-printed. Mirrors <c>WriteIndented</c>.</summary>
	public bool WriteIndented
	{
		get => _writeIndented;
		set => Set(ref _writeIndented, value);
	}

	/// <summary>Gets or sets the maximum nesting depth honored while reading and writing. Defaults to <c>64</c>.</summary>
	public int MaxDepth
	{
		get => _maxDepth;
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "MaxDepth must be non-negative.");
			}

			Set(ref _maxDepth, value == 0 ? 64 : value);
		}
	}

	/// <summary>Gets or sets the global condition under which members are ignored when writing.</summary>
	public SdlIgnoreCondition DefaultIgnoreCondition
	{
		get => _defaultIgnoreCondition;
		set => Set(ref _defaultIgnoreCondition, value);
	}

	/// <summary>Gets or sets how numbers are read. Defaults to <see cref="SdlNumberHandling.Strict"/>.</summary>
	public SdlNumberHandling NumberHandling
	{
		get => _numberHandling;
		set => Set(ref _numberHandling, value);
	}

	/// <summary>Gets or sets how unmapped members are treated. Defaults to <see cref="SdlUnmappedMemberHandling.Skip"/>.</summary>
	public SdlUnmappedMemberHandling UnmappedMemberHandling
	{
		get => _unmappedMemberHandling;
		set => Set(ref _unmappedMemberHandling, value);
	}

	/// <summary>Gets or sets how comments are handled while reading. Defaults to <see cref="SdlCommentHandling.Skip"/>.</summary>
	public SdlCommentHandling ReadCommentHandling
	{
		get => _readCommentHandling;
		set => Set(ref _readCommentHandling, value);
	}

	/// <summary>Gets or sets the reference handler used to preserve or ignore object cycles. Defaults to none.</summary>
	public SdlReferenceHandler? ReferenceHandler
	{
		get => _referenceHandler;
		set => Set(ref _referenceHandler, value);
	}

	/// <summary>Gets or sets a value indicating whether public fields are (de)serialized in addition to properties.</summary>
	public bool IncludeFields
	{
		get => _includeFields;
		set => Set(ref _includeFields, value);
	}

	/// <summary>Gets or sets a value indicating whether member name matching is case-insensitive. Defaults to <see langword="true"/>.</summary>
	public bool PropertyNameCaseInsensitive
	{
		get => _propertyNameCaseInsensitive;
		set => Set(ref _propertyNameCaseInsensitive, value);
	}

	/// <summary>
	/// Gets or sets the default SDL structural role for scalar members that carry no explicit
	/// <see cref="SdlValueAttribute"/>, <see cref="SdlAttributeAttribute"/>, or <see cref="SdlChildAttribute"/>.
	/// Defaults to <see cref="SdlMemberRole.Attribute"/>.
	/// </summary>
	public SdlMemberRole DefaultScalarRole
	{
		get => _defaultScalarRole;
		set => Set(ref _defaultScalarRole, value);
	}

	/// <summary>Gets or sets the metadata resolver. Defaults to <see cref="DefaultSdlTypeInfoResolver.Instance"/>.</summary>
	public ISdlTypeInfoResolver TypeInfoResolver
	{
		get => _typeInfoResolver;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			Set(ref _typeInfoResolver, value);
		}
	}

	/// <summary>Locks the options against further mutation. Idempotent. Called automatically on first use.</summary>
	public void MakeReadOnly() => IsReadOnly = true;

	/// <summary>Resolves and caches the <see cref="SdlTypeInfo"/> contract for <paramref name="type"/>.</summary>
	public SdlTypeInfo GetTypeInfo(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);
		MakeReadOnly();
		return _typeInfoCache.GetOrAdd(type, static (t, self) =>
			self._typeInfoResolver.GetTypeInfo(t, self)
				?? throw new SdlException($"No SDL metadata could be resolved for type '{t}'."),
			this);
	}

	/// <summary>Translates a member name through the configured <see cref="PropertyNamingPolicy"/>.</summary>
	public string ConvertName(string name)
		=> _namingPolicy is null ? name : _namingPolicy.ConvertName(name);

	/// <summary>Returns the writer options implied by this configuration.</summary>
	public SdlWriterOptions GetWriterOptions() => new() { Indented = _writeIndented };

	/// <summary>Returns the reader options implied by this configuration.</summary>
	public SdlReaderOptions GetReaderOptions() => new() { MaxDepth = _maxDepth, CommentHandling = _readCommentHandling };

	internal ConcurrentDictionary<Type, object> MetadataCache => _metadataCache;

	internal SdlConverter? FindUserConverter(Type type)
	{
		foreach (SdlConverter converter in _converters)
		{
			if (converter.CanConvert(type))
			{
				return converter;
			}
		}

		return null;
	}

	internal void EnsureMutable()
	{
		if (IsReadOnly)
		{
			throw new InvalidOperationException("SdlSerializerOptions instance is read-only because it has already been used for (de)serialization.");
		}
	}

	private void Set<T>(ref T field, T value)
	{
		EnsureMutable();
		field = value;
	}

	private static SdlSerializerOptions CreateLocked()
	{
		SdlSerializerOptions options = new();
		options.MakeReadOnly();
		return options;
	}

	private sealed class ConverterCollection(SdlSerializerOptions owner) : Collection<SdlConverter>
	{
		protected override void InsertItem(int index, SdlConverter item)
		{
			ArgumentNullException.ThrowIfNull(item);
			owner.EnsureMutable();
			base.InsertItem(index, item);
		}

		protected override void SetItem(int index, SdlConverter item)
		{
			ArgumentNullException.ThrowIfNull(item);
			owner.EnsureMutable();
			base.SetItem(index, item);
		}

		protected override void RemoveItem(int index)
		{
			owner.EnsureMutable();
			base.RemoveItem(index);
		}

		protected override void ClearItems()
		{
			owner.EnsureMutable();
			base.ClearItems();
		}
	}
}

/// <summary>Identifies the SDL structural role a CLR member maps to.</summary>
public enum SdlMemberRole
{
	/// <summary>The member is a tag attribute (<c>name=value</c>).</summary>
	Attribute = 0,

	/// <summary>The member is a positional tag value.</summary>
	Value,

	/// <summary>The member is a nested child tag.</summary>
	Child,
}
