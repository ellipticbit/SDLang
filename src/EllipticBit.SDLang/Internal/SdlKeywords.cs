using System.Runtime.CompilerServices;

namespace EllipticBit.SDLang.Internal;

/// <summary>Identifies a reserved bare-word keyword in SDLang.</summary>
internal enum SdlKeyword : byte
{
	None = 0,
	True,
	False,
	On,
	Off,
	Null,
}

/// <summary>Recognizes SDLang's reserved value keywords (<c>true</c>, <c>false</c>, <c>on</c>, <c>off</c>, <c>null</c>) from UTF-8 bytes.</summary>
internal static class SdlKeywords
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static SdlKeyword Match(ReadOnlySpan<byte> word) => word.Length switch
	{
		2 => word.SequenceEqual("on"u8) ? SdlKeyword.On : SdlKeyword.None,
		3 => word.SequenceEqual("off"u8) ? SdlKeyword.Off : SdlKeyword.None,
		4 => Match4(word),
		5 => word.SequenceEqual("false"u8) ? SdlKeyword.False : SdlKeyword.None,
		_ => SdlKeyword.None,
	};

	private static SdlKeyword Match4(ReadOnlySpan<byte> word)
	{
		if (word.SequenceEqual("true"u8))
		{
			return SdlKeyword.True;
		}

		return word.SequenceEqual("null"u8) ? SdlKeyword.Null : SdlKeyword.None;
	}
}
