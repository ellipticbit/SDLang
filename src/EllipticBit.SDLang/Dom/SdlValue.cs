using System.Text;

namespace EllipticBit.SDLang;

/// <summary>
/// A single, typed SDLang literal value (the payload of a tag value or an attribute). Roughly analogous to a
/// scalar <c>System.Text.Json.Nodes.JsonValue</c>. Instances are mutable: assign <see cref="Value"/> through the
/// strongly typed factory methods or the typed <c>Set*</c> helpers and the <see cref="Kind"/> updates to match.
/// </summary>
public sealed class SdlValue : IEquatable<SdlValue>
{
	private object? _value;

	/// <summary>Initializes a new <see cref="SdlValue"/> representing the SDL <c>null</c> literal.</summary>
	public SdlValue()
	{
		_value = null;
		Kind = SdlValueKind.Null;
	}

	private SdlValue(object? value, SdlValueKind kind)
	{
		_value = value;
		Kind = kind;
	}

	/// <summary>Gets the literal kind of this value.</summary>
	public SdlValueKind Kind { get; private set; }

	/// <summary>Gets the boxed CLR payload. Use the typed accessors for conversion-checked reads.</summary>
	public object? Value => _value;

	/// <summary>Gets a value indicating whether this value is the SDL <c>null</c> literal.</summary>
	public bool IsNull => Kind == SdlValueKind.Null;

	/// <summary>Creates a value representing the SDL <c>null</c> literal.</summary>
	public static SdlValue Null() => new(null, SdlValueKind.Null);

	/// <summary>Creates a string value.</summary>
	public static SdlValue Create(string? value)
		=> value is null ? Null() : new SdlValue(value, SdlValueKind.String);

	/// <summary>Creates a single-character value.</summary>
	public static SdlValue Create(char value) => new(new Rune(value), SdlValueKind.Char);

	/// <summary>Creates a single-character value from a <see cref="Rune"/>.</summary>
	public static SdlValue Create(Rune value) => new(value, SdlValueKind.Char);

	/// <summary>Creates a boolean value.</summary>
	public static SdlValue Create(bool value) => new(value, SdlValueKind.Boolean);

	/// <summary>Creates a 32-bit integer value.</summary>
	public static SdlValue Create(int value) => new(value, SdlValueKind.Int32);

	/// <summary>Creates a 64-bit integer value.</summary>
	public static SdlValue Create(long value) => new(value, SdlValueKind.Int64);

	/// <summary>Creates a single-precision floating point value.</summary>
	public static SdlValue Create(float value) => new(value, SdlValueKind.Single);

	/// <summary>Creates a double-precision floating point value.</summary>
	public static SdlValue Create(double value) => new(value, SdlValueKind.Double);

	/// <summary>Creates a decimal value.</summary>
	public static SdlValue Create(decimal value) => new(value, SdlValueKind.Decimal);

	/// <summary>Creates a calendar date value.</summary>
	public static SdlValue Create(DateOnly value) => new(value, SdlValueKind.Date);

	/// <summary>Creates a local date/time value.</summary>
	public static SdlValue Create(DateTime value) => new(value, SdlValueKind.DateTime);

	/// <summary>Creates a zoned date/time value.</summary>
	public static SdlValue Create(DateTimeOffset value) => new(value, SdlValueKind.DateTimeOffset);

	/// <summary>Creates a duration value.</summary>
	public static SdlValue Create(TimeSpan value) => new(value, SdlValueKind.TimeSpan);

	/// <summary>Creates a binary value. The supplied bytes are copied.</summary>
	public static SdlValue Create(ReadOnlySpan<byte> value) => new(value.ToArray(), SdlValueKind.Binary);

	/// <summary>Creates a binary value, taking ownership of the supplied array.</summary>
	public static SdlValue CreateBinary(byte[] value)
	{
		ArgumentNullException.ThrowIfNull(value);
		return new SdlValue(value, SdlValueKind.Binary);
	}

	/// <summary>
	/// Wraps an arbitrary CLR object as an <see cref="SdlValue"/>, inferring the <see cref="SdlValueKind"/>.
	/// Throws <see cref="SdlException"/> for unsupported types.
	/// </summary>
	public static SdlValue FromObject(object? value) => value switch
	{
		null => Null(),
		string s => Create(s),
		bool b => Create(b),
		char c => Create(c),
		Rune r => Create(r),
		sbyte v => Create((int)v),
		byte v => Create((int)v),
		short v => Create((int)v),
		ushort v => Create((int)v),
		int v => Create(v),
		uint v => Create((long)v),
		long v => Create(v),
		float v => Create(v),
		double v => Create(v),
		decimal v => Create(v),
		DateOnly v => Create(v),
		DateTime v => Create(v),
		DateTimeOffset v => Create(v),
		TimeSpan v => Create(v),
		byte[] v => CreateBinary(v),
		SdlValue v => v,
		_ => throw new SdlException($"Type '{value.GetType()}' cannot be represented as an SDL value."),
	};

	/// <summary>Gets the value as a string, formatting non-string kinds.</summary>
	public string AsString() => Kind switch
	{
		SdlValueKind.Null => string.Empty,
		SdlValueKind.String => (string)_value!,
		SdlValueKind.Char => ((Rune)_value!).ToString(),
		_ => _value!.ToString() ?? string.Empty,
	};

	/// <summary>Gets the value as a boolean, or throws if the kind is not <see cref="SdlValueKind.Boolean"/>.</summary>
	public bool AsBoolean() => Kind == SdlValueKind.Boolean
		? (bool)_value!
		: throw KindError(SdlValueKind.Boolean);

	/// <summary>Gets the value as a 32-bit integer, converting from any numeric kind.</summary>
	public int AsInt32() => Convert.ToInt32(RequireNumeric());

	/// <summary>Gets the value as a 64-bit integer, converting from any numeric kind.</summary>
	public long AsInt64() => Convert.ToInt64(RequireNumeric());

	/// <summary>Gets the value as a double, converting from any numeric kind.</summary>
	public double AsDouble() => Convert.ToDouble(RequireNumeric());

	/// <summary>Gets the value as a decimal, converting from any numeric kind.</summary>
	public decimal AsDecimal() => Convert.ToDecimal(RequireNumeric());

	/// <summary>Gets the value as a byte array, or throws if the kind is not <see cref="SdlValueKind.Binary"/>.</summary>
	public byte[] AsBytes() => Kind == SdlValueKind.Binary
		? (byte[])_value!
		: throw KindError(SdlValueKind.Binary);

	/// <summary>Writes this value to the supplied writer using the kind-appropriate emit method.</summary>
	public void WriteTo(Utf8SdlWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);
		switch (Kind)
		{
			case SdlValueKind.Null:
				writer.WriteNullValue();
				break;
			case SdlValueKind.String:
				writer.WriteStringValue((string)_value!);
				break;
			case SdlValueKind.Char:
				writer.WriteCharValue((Rune)_value!);
				break;
			case SdlValueKind.Boolean:
				writer.WriteBooleanValue((bool)_value!);
				break;
			case SdlValueKind.Int32:
				writer.WriteInt32Value((int)_value!);
				break;
			case SdlValueKind.Int64:
				writer.WriteInt64Value((long)_value!);
				break;
			case SdlValueKind.Single:
				writer.WriteSingleValue((float)_value!);
				break;
			case SdlValueKind.Double:
				writer.WriteDoubleValue((double)_value!);
				break;
			case SdlValueKind.Decimal:
				writer.WriteDecimalValue((decimal)_value!);
				break;
			case SdlValueKind.Date:
				writer.WriteDateValue((DateOnly)_value!);
				break;
			case SdlValueKind.DateTime:
				writer.WriteDateTimeValue((DateTime)_value!);
				break;
			case SdlValueKind.DateTimeOffset:
				writer.WriteDateTimeOffsetValue((DateTimeOffset)_value!);
				break;
			case SdlValueKind.TimeSpan:
				writer.WriteTimeSpanValue((TimeSpan)_value!);
				break;
			case SdlValueKind.Binary:
				writer.WriteBinaryValue((byte[])_value!);
				break;
		}
	}

	/// <inheritdoc />
	public bool Equals(SdlValue? other)
	{
		if (other is null)
		{
			return false;
		}

		if (Kind != other.Kind)
		{
			return false;
		}

		if (Kind == SdlValueKind.Binary)
		{
			return ((byte[])_value!).AsSpan().SequenceEqual((byte[])other._value!);
		}

		return Equals(_value, other._value);
	}

	/// <inheritdoc />
	public override bool Equals(object? obj) => Equals(obj as SdlValue);

	/// <inheritdoc />
	public override int GetHashCode()
	{
		if (Kind == SdlValueKind.Binary)
		{
			HashCode hc = default;
			hc.AddBytes((byte[])_value!);
			return hc.ToHashCode();
		}

		return HashCode.Combine(Kind, _value);
	}

	/// <inheritdoc />
	public override string ToString() => AsString();

	private object RequireNumeric()
	{
		if (_value is null || Kind is SdlValueKind.Null or SdlValueKind.String or SdlValueKind.Char
			or SdlValueKind.Boolean or SdlValueKind.Binary or SdlValueKind.Date or SdlValueKind.DateTime
			or SdlValueKind.DateTimeOffset or SdlValueKind.TimeSpan)
		{
			throw new SdlException($"SDL value of kind '{Kind}' is not numeric.");
		}

		return _value;
	}

	private SdlException KindError(SdlValueKind expected)
		=> new($"SDL value is of kind '{Kind}', not '{expected}'.");
}
