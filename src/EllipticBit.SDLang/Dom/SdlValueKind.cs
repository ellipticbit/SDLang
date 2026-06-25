namespace EllipticBit.SDLang;

/// <summary>
/// Identifies the literal type carried by an <see cref="SdlValue"/> or reported by the
/// <see cref="Utf8SdlReader"/>. Mirrors the role of <c>System.Text.Json.JsonValueKind</c>.
/// </summary>
public enum SdlValueKind : byte
{
	/// <summary>The SDL <c>null</c> literal.</summary>
	Null = 0,

	/// <summary>A UTF-8 string (<c>"escaped"</c> or <c>`raw`</c>). Maps to <see cref="string"/>.</summary>
	String,

	/// <summary>A single Unicode character (<c>'x'</c>). Maps to <see cref="System.Text.Rune"/>.</summary>
	Char,

	/// <summary>A 32-bit signed integer (<c>10</c>). Maps to <see cref="int"/>.</summary>
	Int32,

	/// <summary>A 64-bit signed integer (<c>10L</c>). Maps to <see cref="long"/>.</summary>
	Int64,

	/// <summary>A 32-bit IEEE float (<c>10.5f</c>). Maps to <see cref="float"/>.</summary>
	Single,

	/// <summary>A 64-bit IEEE float (<c>10.5</c> / <c>10.5d</c>). Maps to <see cref="double"/>.</summary>
	Double,

	/// <summary>A 128-bit decimal (<c>10.123BD</c>). Maps to <see cref="decimal"/>.</summary>
	Decimal,

	/// <summary>A boolean (<c>true</c>/<c>false</c>/<c>on</c>/<c>off</c>). Maps to <see cref="bool"/>.</summary>
	Boolean,

	/// <summary>A calendar date (<c>2015/12/06</c>). Maps to <see cref="DateOnly"/>.</summary>
	Date,

	/// <summary>A date and time without a time zone. Maps to <see cref="System.DateTime"/>.</summary>
	DateTime,

	/// <summary>A date and time with a time zone offset. Maps to <see cref="System.DateTimeOffset"/>.</summary>
	DateTimeOffset,

	/// <summary>A duration (<c>2d:12:14:34.123</c>). Maps to <see cref="System.TimeSpan"/>.</summary>
	TimeSpan,

	/// <summary>Base64-encoded binary data (<c>[sdf789GSfsb2+3324sf2]</c>). Maps to <see cref="T:System.Byte[]"/>.</summary>
	Binary,
}
