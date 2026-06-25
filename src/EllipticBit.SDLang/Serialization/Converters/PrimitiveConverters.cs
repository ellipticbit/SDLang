using System.Globalization;
using System.Text;

namespace EllipticBit.SDLang.Serialization.Converters;

/// <summary>Converts <see cref="string"/> values.</summary>
internal sealed class StringConverter : SdlConverter<string?>
{
	public override string? Read(SdlValue value, SdlSerializerOptions options)
		=> value.IsNull ? null : value.AsString();

	public override SdlValue Write(string? value, SdlSerializerOptions options)
		=> SdlValue.Create(value);
}

/// <summary>Converts <see cref="bool"/> values.</summary>
internal sealed class BooleanConverter : SdlConverter<bool>
{
	public override bool Read(SdlValue value, SdlSerializerOptions options) => value.AsBoolean();

	public override SdlValue Write(bool value, SdlSerializerOptions options) => SdlValue.Create(value);
}

/// <summary>Converts <see cref="char"/> values.</summary>
internal sealed class CharConverter : SdlConverter<char>
{
	public override char Read(SdlValue value, SdlSerializerOptions options)
	{
		string s = value.AsString();
		return s.Length > 0 ? s[0] : '\0';
	}

	public override SdlValue Write(char value, SdlSerializerOptions options) => SdlValue.Create(value);
}

/// <summary>Converts <see cref="Rune"/> values.</summary>
internal sealed class RuneConverter : SdlConverter<Rune>
{
	public override Rune Read(SdlValue value, SdlSerializerOptions options)
	{
		if (value.Value is Rune rune)
		{
			return rune;
		}

		string s = value.AsString();
		return s.Length == 0 ? new Rune('\0') : Rune.GetRuneAt(s, 0);
	}

	public override SdlValue Write(Rune value, SdlSerializerOptions options) => SdlValue.Create(value);
}

internal sealed class SByteConverter : SdlConverter<sbyte>
{
	public override sbyte Read(SdlValue value, SdlSerializerOptions options) => (sbyte)value.AsInt32();

	public override SdlValue Write(sbyte value, SdlSerializerOptions options) => SdlValue.Create((int)value);
}

internal sealed class ByteConverter : SdlConverter<byte>
{
	public override byte Read(SdlValue value, SdlSerializerOptions options) => (byte)value.AsInt32();

	public override SdlValue Write(byte value, SdlSerializerOptions options) => SdlValue.Create((int)value);
}

internal sealed class Int16Converter : SdlConverter<short>
{
	public override short Read(SdlValue value, SdlSerializerOptions options) => (short)value.AsInt32();

	public override SdlValue Write(short value, SdlSerializerOptions options) => SdlValue.Create((int)value);
}

internal sealed class UInt16Converter : SdlConverter<ushort>
{
	public override ushort Read(SdlValue value, SdlSerializerOptions options) => (ushort)value.AsInt32();

	public override SdlValue Write(ushort value, SdlSerializerOptions options) => SdlValue.Create((int)value);
}

internal sealed class Int32Converter : SdlConverter<int>
{
	public override int Read(SdlValue value, SdlSerializerOptions options) => value.AsInt32();

	public override SdlValue Write(int value, SdlSerializerOptions options) => SdlValue.Create(value);
}

internal sealed class UInt32Converter : SdlConverter<uint>
{
	public override uint Read(SdlValue value, SdlSerializerOptions options) => (uint)value.AsInt64();

	public override SdlValue Write(uint value, SdlSerializerOptions options) => SdlValue.Create((long)value);
}

internal sealed class Int64Converter : SdlConverter<long>
{
	public override long Read(SdlValue value, SdlSerializerOptions options) => value.AsInt64();

	public override SdlValue Write(long value, SdlSerializerOptions options) => SdlValue.Create(value);
}

internal sealed class UInt64Converter : SdlConverter<ulong>
{
	public override ulong Read(SdlValue value, SdlSerializerOptions options)
		=> value.Value is null ? 0UL : Convert.ToUInt64(value.Value, CultureInfo.InvariantCulture);

	public override SdlValue Write(ulong value, SdlSerializerOptions options)
		=> value <= long.MaxValue ? SdlValue.Create((long)value) : SdlValue.Create((decimal)value);
}

internal sealed class SingleConverter : SdlConverter<float>
{
	public override float Read(SdlValue value, SdlSerializerOptions options) => (float)value.AsDouble();

	public override SdlValue Write(float value, SdlSerializerOptions options) => SdlValue.Create(value);
}

internal sealed class DoubleConverter : SdlConverter<double>
{
	public override double Read(SdlValue value, SdlSerializerOptions options) => value.AsDouble();

	public override SdlValue Write(double value, SdlSerializerOptions options) => SdlValue.Create(value);
}

internal sealed class DecimalConverter : SdlConverter<decimal>
{
	public override decimal Read(SdlValue value, SdlSerializerOptions options) => value.AsDecimal();

	public override SdlValue Write(decimal value, SdlSerializerOptions options) => SdlValue.Create(value);
}

internal sealed class DateOnlyConverter : SdlConverter<DateOnly>
{
	public override DateOnly Read(SdlValue value, SdlSerializerOptions options)
		=> value.Value is DateOnly d ? d : DateOnly.FromDateTime((DateTime)value.Value!);

	public override SdlValue Write(DateOnly value, SdlSerializerOptions options) => SdlValue.Create(value);
}

internal sealed class DateTimeConverter : SdlConverter<DateTime>
{
	public override DateTime Read(SdlValue value, SdlSerializerOptions options) => value.Value switch
	{
		DateTime dt => dt,
		DateTimeOffset dto => dto.DateTime,
		DateOnly d => d.ToDateTime(TimeOnly.MinValue),
		_ => throw new SdlException("Value is not a date/time."),
	};

	public override SdlValue Write(DateTime value, SdlSerializerOptions options) => SdlValue.Create(value);
}

internal sealed class DateTimeOffsetConverter : SdlConverter<DateTimeOffset>
{
	public override DateTimeOffset Read(SdlValue value, SdlSerializerOptions options) => value.Value switch
	{
		DateTimeOffset dto => dto,
		DateTime dt => new DateTimeOffset(dt),
		_ => throw new SdlException("Value is not a date/time."),
	};

	public override SdlValue Write(DateTimeOffset value, SdlSerializerOptions options) => SdlValue.Create(value);
}

internal sealed class TimeSpanConverter : SdlConverter<TimeSpan>
{
	public override TimeSpan Read(SdlValue value, SdlSerializerOptions options) => (TimeSpan)value.Value!;

	public override SdlValue Write(TimeSpan value, SdlSerializerOptions options) => SdlValue.Create(value);
}

internal sealed class ByteArrayConverter : SdlConverter<byte[]?>
{
	public override byte[]? Read(SdlValue value, SdlSerializerOptions options)
		=> value.IsNull ? null : value.AsBytes();

	public override SdlValue Write(byte[]? value, SdlSerializerOptions options)
		=> value is null ? SdlValue.Null() : SdlValue.CreateBinary(value);
}

internal sealed class GuidConverter : SdlConverter<Guid>
{
	public override Guid Read(SdlValue value, SdlSerializerOptions options) => Guid.Parse(value.AsString());

	public override SdlValue Write(Guid value, SdlSerializerOptions options)
		=> SdlValue.Create(value.ToString("D", CultureInfo.InvariantCulture));
}

internal sealed class UriConverter : SdlConverter<Uri?>
{
	public override Uri? Read(SdlValue value, SdlSerializerOptions options)
		=> value.IsNull ? null : new Uri(value.AsString(), UriKind.RelativeOrAbsolute);

	public override SdlValue Write(Uri? value, SdlSerializerOptions options)
		=> value is null ? SdlValue.Null() : SdlValue.Create(value.ToString());
}

internal sealed class VersionConverter : SdlConverter<Version?>
{
	public override Version? Read(SdlValue value, SdlSerializerOptions options)
		=> value.IsNull ? null : Version.Parse(value.AsString());

	public override SdlValue Write(Version? value, SdlSerializerOptions options)
		=> value is null ? SdlValue.Null() : SdlValue.Create(value.ToString());
}
