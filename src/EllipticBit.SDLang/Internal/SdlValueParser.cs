using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;

namespace EllipticBit.SDLang.Internal;

/// <summary>
/// Pure UTF-8 parsing routines for SDLang literals. Every method is allocation-light and operates on byte
/// spans. Failures are reported via <c>bool</c> return values so the caller can raise a positioned exception.
/// </summary>
internal static class SdlValueParser
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ReadOnlySpan<byte> TrimSuffix(ReadOnlySpan<byte> token, int count)
		=> token[..^count];

	internal static bool TryParseInt32(ReadOnlySpan<byte> token, out int value)
		=> Utf8Parser.TryParse(token, out value, out int consumed) && consumed == token.Length;

	internal static bool TryParseInt64(ReadOnlySpan<byte> token, out long value)
	{
		if (token.Length > 0 && (token[^1] | 0x20) == (byte)'l')
		{
			token = TrimSuffix(token, 1);
		}

		return Utf8Parser.TryParse(token, out value, out int consumed) && consumed == token.Length;
	}

	internal static bool TryParseSingle(ReadOnlySpan<byte> token, out float value)
	{
		if (token.Length > 0 && (token[^1] | 0x20) == (byte)'f')
		{
			token = TrimSuffix(token, 1);
		}

		return Utf8Parser.TryParse(token, out value, out int consumed) && consumed == token.Length;
	}

	internal static bool TryParseDouble(ReadOnlySpan<byte> token, out double value)
	{
		if (token.Length > 0 && (token[^1] | 0x20) == (byte)'d')
		{
			token = TrimSuffix(token, 1);
		}

		return Utf8Parser.TryParse(token, out value, out int consumed) && consumed == token.Length;
	}

	internal static bool TryParseDecimal(ReadOnlySpan<byte> token, out decimal value)
	{
		if (token.Length >= 2 && (token[^1] | 0x20) == (byte)'d' && (token[^2] | 0x20) == (byte)'b')
		{
			token = TrimSuffix(token, 2);
		}

		return Utf8Parser.TryParse(token, out value, out int consumed) && consumed == token.Length;
	}

	/// <summary>Determines the numeric <see cref="SdlValueKind"/> implied by a token's suffix and shape.</summary>
	internal static SdlValueKind ClassifyNumber(ReadOnlySpan<byte> token)
	{
		ReadOnlySpan<byte> body = token;
		if (body.Length > 0 && (body[0] == SdlText.Dash || body[0] == SdlText.Plus))
		{
			body = body[1..];
		}

		if (body.IsEmpty)
		{
			return SdlValueKind.Int32;
		}

		byte last = (byte)(body[^1] | 0x20);
		switch (last)
		{
			case (byte)'l':
				return SdlValueKind.Int64;
			case (byte)'f':
				return SdlValueKind.Single;
			case (byte)'d':
				return body.Length >= 2 && (body[^2] | 0x20) == (byte)'b'
					? SdlValueKind.Decimal
					: SdlValueKind.Double;
		}

		if (body.IndexOf(SdlText.Dot) >= 0 || body.IndexOfAny((byte)'e', (byte)'E') >= 0)
		{
			return SdlValueKind.Double;
		}

		if (TryParseInt32(token, out _))
		{
			return SdlValueKind.Int32;
		}

		return TryParseInt64(token, out _) ? SdlValueKind.Int64 : SdlValueKind.Decimal;
	}

	private static bool TryParseUInt(ReadOnlySpan<byte> span, out int value)
	{
		value = 0;
		if (span.IsEmpty || span.Length > 9)
		{
			return false;
		}

		int acc = 0;
		foreach (byte b in span)
		{
			if (!SdlText.IsDigit(b))
			{
				return false;
			}

			acc = (acc * 10) + (b - '0');
		}

		value = acc;
		return true;
	}

	internal static bool TryParseDate(ReadOnlySpan<byte> token, out DateOnly value)
	{
		value = default;
		int first = token.IndexOf(SdlText.Slash);
		if (first <= 0)
		{
			return false;
		}

		int second = token[(first + 1)..].IndexOf(SdlText.Slash);
		if (second <= 0)
		{
			return false;
		}

		second += first + 1;

		if (!TryParseUInt(token[..first], out int year)
			|| !TryParseUInt(token[(first + 1)..second], out int month)
			|| !TryParseUInt(token[(second + 1)..], out int day))
		{
			return false;
		}

		try
		{
			value = new DateOnly(year, month, day);
			return true;
		}
		catch (ArgumentOutOfRangeException)
		{
			return false;
		}
	}

	internal static bool TryParseDateTime(ReadOnlySpan<byte> token, out DateTime dateTime, out DateTimeOffset dateTimeOffset, out bool hasOffset)
	{
		dateTime = default;
		dateTimeOffset = default;
		hasOffset = false;

		int space = token.IndexOf(SdlText.Space);
		if (space < 0)
		{
			return false;
		}

		ReadOnlySpan<byte> datePart = token[..space];
		ReadOnlySpan<byte> timePart = token[(space + 1)..].TrimStart(SdlText.Space);

		if (!TryParseDate(datePart, out DateOnly date))
		{
			return false;
		}

		if (!TryParseClock(timePart, out int hour, out int minute, out int second, out long fractionTicks, out ReadOnlySpan<byte> zone))
		{
			return false;
		}

		if (hour > 23 || minute > 59 || second > 59)
		{
			return false;
		}

		TimeOnly time;
		try
		{
			time = new TimeOnly(hour, minute, second).Add(TimeSpan.FromTicks(fractionTicks));
		}
		catch (ArgumentOutOfRangeException)
		{
			return false;
		}

		if (zone.IsEmpty)
		{
			dateTime = date.ToDateTime(time, DateTimeKind.Unspecified);
			return true;
		}

		DateTime local = date.ToDateTime(time, DateTimeKind.Unspecified);
		if (!TryParseZone(zone, local, out TimeSpan offset))
		{
			return false;
		}

		dateTimeOffset = new DateTimeOffset(local, offset);
		hasOffset = true;
		return true;
	}

	private static bool TryParseClock(ReadOnlySpan<byte> span, out int hour, out int minute, out int second, out long fractionTicks, out ReadOnlySpan<byte> zone)
	{
		hour = minute = second = 0;
		fractionTicks = 0;
		zone = default;

		int pos = 0;
		if (!ReadUIntUntil(span, ref pos, SdlText.Colon, out hour))
		{
			return false;
		}

		if (pos >= span.Length || span[pos] != SdlText.Colon)
		{
			return false;
		}

		pos++;
		if (!ReadTwoOrMoreDigits(span, ref pos, out minute))
		{
			return false;
		}

		if (pos < span.Length && span[pos] == SdlText.Colon)
		{
			pos++;
			if (!ReadTwoOrMoreDigits(span, ref pos, out second))
			{
				return false;
			}

			if (pos < span.Length && span[pos] == SdlText.Dot)
			{
				pos++;
				if (!ReadFraction(span, ref pos, out fractionTicks))
				{
					return false;
				}
			}
		}

		zone = span[pos..];
		return true;
	}

	private static bool ReadUIntUntil(ReadOnlySpan<byte> span, ref int pos, byte terminator, out int value)
	{
		int start = pos;
		while (pos < span.Length && SdlText.IsDigit(span[pos]))
		{
			pos++;
		}

		return TryParseUInt(span[start..pos], out value) && pos < span.Length && span[pos] == terminator;
	}

	private static bool ReadTwoOrMoreDigits(ReadOnlySpan<byte> span, ref int pos, out int value)
	{
		int start = pos;
		while (pos < span.Length && SdlText.IsDigit(span[pos]))
		{
			pos++;
		}

		return TryParseUInt(span[start..pos], out value);
	}

	private static bool ReadFraction(ReadOnlySpan<byte> span, ref int pos, out long fractionTicks)
	{
		fractionTicks = 0;
		int start = pos;
		while (pos < span.Length && SdlText.IsDigit(span[pos]))
		{
			pos++;
		}

		ReadOnlySpan<byte> digits = span[start..pos];
		if (digits.IsEmpty)
		{
			return false;
		}

		long ticks = 0;
		for (int i = 0; i < 7; i++)
		{
			ticks *= 10;
			if (i < digits.Length)
			{
				ticks += digits[i] - '0';
			}
		}

		fractionTicks = ticks;
		return true;
	}

	private static bool TryParseZone(ReadOnlySpan<byte> zone, DateTime local, out TimeSpan offset)
	{
		offset = TimeSpan.Zero;
		if (zone.SequenceEqual("UTC"u8) || zone.SequenceEqual("Z"u8) || zone.SequenceEqual("GMT"u8))
		{
			return true;
		}

		if (zone.Length >= 3 && (zone[0] | 0x20) == (byte)'g' && (zone[1] | 0x20) == (byte)'m' && (zone[2] | 0x20) == (byte)'t')
		{
			return TryParseSignedOffset(zone[3..], out offset);
		}

		if (zone[0] == SdlText.Plus || zone[0] == SdlText.Dash)
		{
			return TryParseSignedOffset(zone, out offset);
		}

		try
		{
			string id = SdlText.StrictUtf8.GetString(zone);
			TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(id);
			offset = tz.GetUtcOffset(local);
			return true;
		}
		catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException or System.Text.DecoderFallbackException)
		{
			return false;
		}
	}

	private static bool TryParseSignedOffset(ReadOnlySpan<byte> span, out TimeSpan offset)
	{
		offset = TimeSpan.Zero;
		if (span.IsEmpty)
		{
			return true;
		}

		bool negative = span[0] == SdlText.Dash;
		if (span[0] == SdlText.Plus || span[0] == SdlText.Dash)
		{
			span = span[1..];
		}

		int colon = span.IndexOf(SdlText.Colon);
		ReadOnlySpan<byte> hoursSpan = colon >= 0 ? span[..colon] : span;
		ReadOnlySpan<byte> minutesSpan = colon >= 0 ? span[(colon + 1)..] : default;

		if (colon < 0 && span.Length == 4)
		{
			hoursSpan = span[..2];
			minutesSpan = span[2..];
		}

		if (!TryParseUInt(hoursSpan, out int hours))
		{
			return false;
		}

		int minutes = 0;
		if (!minutesSpan.IsEmpty && !TryParseUInt(minutesSpan, out minutes))
		{
			return false;
		}

		if (hours > 18 || minutes > 59)
		{
			return false;
		}

		offset = new TimeSpan(hours, minutes, 0);
		if (negative)
		{
			offset = -offset;
		}

		return true;
	}

	internal static bool TryParseTimeSpan(ReadOnlySpan<byte> token, out TimeSpan value)
	{
		value = default;
		bool negative = false;
		if (!token.IsEmpty && token[0] == SdlText.Dash)
		{
			negative = true;
			token = token[1..];
		}

		long days = 0;
		int firstColon = token.IndexOf(SdlText.Colon);
		if (firstColon < 0)
		{
			return false;
		}

		ReadOnlySpan<byte> firstField = token[..firstColon];
		if (!firstField.IsEmpty && (firstField[^1] | 0x20) == (byte)'d')
		{
			if (!TryParseUInt(firstField[..^1], out int d))
			{
				return false;
			}

			days = d;
			token = token[(firstColon + 1)..];
			firstColon = token.IndexOf(SdlText.Colon);
			if (firstColon < 0)
			{
				return false;
			}
		}

		int secondColon = token[(firstColon + 1)..].IndexOf(SdlText.Colon);
		if (secondColon < 0)
		{
			return false;
		}

		secondColon += firstColon + 1;

		if (!TryParseUInt(token[..firstColon], out int hours)
			|| !TryParseUInt(token[(firstColon + 1)..secondColon], out int minutes))
		{
			return false;
		}

		ReadOnlySpan<byte> secondsField = token[(secondColon + 1)..];
		long fractionTicks = 0;
		int dot = secondsField.IndexOf(SdlText.Dot);
		if (dot >= 0)
		{
			int p = dot + 1;
			if (!ReadFraction(secondsField, ref p, out fractionTicks) || p != secondsField.Length)
			{
				return false;
			}

			secondsField = secondsField[..dot];
		}

		if (!TryParseUInt(secondsField, out int seconds))
		{
			return false;
		}

		long ticks = (days * TimeSpan.TicksPerDay)
			+ (hours * TimeSpan.TicksPerHour)
			+ (minutes * TimeSpan.TicksPerMinute)
			+ (seconds * TimeSpan.TicksPerSecond)
			+ fractionTicks;

		value = new TimeSpan(negative ? -ticks : ticks);
		return true;
	}

	internal static bool TryDecodeBase64(ReadOnlySpan<byte> inner, out byte[] value)
	{
		value = [];
		byte[] rented = ArrayPool<byte>.Shared.Rent(inner.Length + 4);
		try
		{
			int len = 0;
			foreach (byte b in inner)
			{
				if (SdlText.IsInlineWhitespace(b) || SdlText.IsLineBreak(b))
				{
					continue;
				}

				rented[len++] = b;
			}

			int padding = len % 4;
			if (padding != 0)
			{
				for (int i = padding; i < 4; i++)
				{
					rented[len++] = (byte)'=';
				}
			}

			Span<byte> compact = rented.AsSpan(0, len);
			int maxDecoded = Base64.GetMaxDecodedFromUtf8Length(len);
			byte[] decoded = new byte[maxDecoded];
			OperationStatus status = Base64.DecodeFromUtf8(compact, decoded, out _, out int written);
			if (status != OperationStatus.Done)
			{
				return false;
			}

			value = written == decoded.Length ? decoded : decoded[..written];
			return true;
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(rented);
		}
	}
}
