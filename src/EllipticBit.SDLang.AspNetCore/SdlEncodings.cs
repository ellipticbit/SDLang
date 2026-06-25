using System.Text;

namespace EllipticBit.SDLang.AspNetCore;

/// <summary>
/// Shared text encodings used by the SDLang formatters and Minimal API helpers. SDLang is UTF-8 first; UTF-16 is
/// also offered so text-oriented clients can negotiate a charset. Non-UTF-8 payloads are transcoded to UTF-8 before
/// they reach the UTF-8-only core reader/writer.
/// </summary>
internal static class SdlEncodings
{
	/// <summary>UTF-8 without a byte-order mark, the canonical SDLang encoding.</summary>
	public static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	/// <summary>UTF-16 little-endian, accepted for interop with text-oriented clients.</summary>
	public static readonly UnicodeEncoding Utf16 = new(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);
}
