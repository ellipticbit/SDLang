using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace EllipticBit.SDLang.Internal;

/// <summary>
/// Low-level UTF-8 byte classification helpers and shared <see cref="SearchValues{T}"/> sets used by
/// the SDLang scanner. All routines operate directly on bytes to avoid UTF-16 conversions.
/// </summary>
internal static class SdlText
{
    /// <summary>
    /// A UTF-8 codec that throws on malformed input rather than emitting U+FFFD replacement characters.
    /// Used whenever untrusted bytes are decoded to a <see cref="string"/> so invalid input is rejected.
    /// </summary>
    internal static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	internal const byte Tab = (byte)'\t';
	internal const byte Space = (byte)' ';
	internal const byte Cr = (byte)'\r';
	internal const byte Lf = (byte)'\n';
	internal const byte Backslash = (byte)'\\';
	internal const byte Quote = (byte)'"';
	internal const byte Backtick = (byte)'`';
	internal const byte Apostrophe = (byte)'\'';
	internal const byte OpenBrace = (byte)'{';
	internal const byte CloseBrace = (byte)'}';
	internal const byte OpenBracket = (byte)'[';
	internal const byte CloseBracket = (byte)']';
	internal const byte Equal = (byte)'=';
	internal const byte Colon = (byte)':';
	internal const byte Semicolon = (byte)';';
	internal const byte Slash = (byte)'/';
	internal const byte Hash = (byte)'#';
	internal const byte Dash = (byte)'-';
	internal const byte Plus = (byte)'+';
	internal const byte Dot = (byte)'.';
	internal const byte Star = (byte)'*';

	/// <summary>Bytes that are skippable inline whitespace (space and tab) but not line breaks.</summary>
	internal static readonly SearchValues<byte> InlineWhitespace = SearchValues.Create([Space, Tab]);

	/// <summary>Bytes that terminate the scan of a quoted-string body (a quote, a backslash, or a line break).</summary>
	internal static readonly SearchValues<byte> QuotedStringStops = SearchValues.Create([Quote, Backslash, Cr, Lf]);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsInlineWhitespace(byte b) => b == Space || b == Tab;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsLineBreak(byte b) => b == Lf || b == Cr;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsDigit(byte b) => (uint)(b - (byte)'0') <= 9u;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsAsciiLetter(byte b) => (uint)((b | 0x20) - (byte)'a') <= (uint)('z' - 'a');

	/// <summary>
	/// True when <paramref name="b"/> may start a tag, namespace, or attribute identifier. ASCII letters,
	/// underscore, the reference-metadata marker '$', and any UTF-8 lead/continuation byte (so non-ASCII
	/// identifiers such as Kanji are allowed).
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsIdentifierStart(byte b) => IsAsciiLetter(b) || b == (byte)'_' || b == (byte)'$' || b >= 0x80;

	/// <summary>True when <paramref name="b"/> may continue an identifier.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsIdentifierPart(byte b)
		=> IsIdentifierStart(b) || IsDigit(b) || b == Dash || b == Dot;
}
