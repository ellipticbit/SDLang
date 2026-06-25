using EllipticBit.SDLang.Serialization;

namespace EllipticBit.SDLang.Tests;

internal sealed class Server
{
	public string Host { get; set; } = "localhost";

	public int Port { get; set; }

	public bool Enabled { get; set; }
}

internal sealed class Person
{
	[SdlValue(0)]
	public string Name { get; set; } = string.Empty;

	[SdlAttribute]
	public int Age { get; set; }

	[SdlChild]
	public Address? Home { get; set; }
}

internal sealed class Address
{
	[SdlValue(0)]
	public string Street { get; set; } = string.Empty;

	[SdlAttribute]
	public string City { get; set; } = string.Empty;
}

internal sealed record Point(int X, int Y);

internal sealed class WithIgnore
{
	public string Kept { get; set; } = string.Empty;

	[SdlIgnore]
	public string Skipped { get; set; } = "secret";

	[SdlIgnore(Condition = SdlIgnoreCondition.WhenWritingNull)]
	public string? Optional { get; set; }
}

internal sealed class WithOrder
{
	[SdlPropertyOrder(2)]
	public int Second { get; set; }

	[SdlPropertyOrder(1)]
	public int First { get; set; }
}

internal sealed class NamingModel
{
	public string FirstName { get; set; } = string.Empty;

	public int ItemCount { get; set; }
}

internal sealed class WithExtensionData
{
	[SdlAttribute]
	public string Known { get; set; } = string.Empty;

	[SdlExtensionData]
	public Dictionary<string, object>? Extra { get; set; }
}

internal enum Color
{
	Red,
	Green,
	Blue,
}

internal sealed class WithEnum
{
	[SdlAttribute]
	public Color Color { get; set; }
}

internal sealed class Catalog
{
	[SdlChild("item")]
	public List<string> Items { get; set; } = [];
}

internal sealed class Node
{
	[SdlAttribute]
	public string Name { get; set; } = string.Empty;

	[SdlChild]
	public Node? Next { get; set; }
}

internal sealed class Temperature
{
	public Temperature(double celsius) => Celsius = celsius;

	public double Celsius { get; }
}

[SdlConverter(typeof(TemperatureConverter))]
internal sealed class ConvertedTemperature
{
	public ConvertedTemperature(double celsius) => Celsius = celsius;

	public double Celsius { get; }
}

internal sealed class TemperatureConverter : SdlConverter<ConvertedTemperature?>
{
	public override ConvertedTemperature? Read(SdlValue value, SdlSerializerOptions options)
		=> value.IsNull ? null : new ConvertedTemperature(value.AsDouble());

	public override SdlValue Write(ConvertedTemperature? value, SdlSerializerOptions options)
		=> value is null ? SdlValue.Null() : SdlValue.Create(value.Celsius);
}
