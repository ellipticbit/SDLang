namespace EllipticBit.SDLang.Internal;

/// <summary>
/// Builds a mutable <see cref="Tag"/> tree from a <see cref="Utf8SdlReader"/> token stream. The grammar handled is
/// <c>tag ::= [ns ':'] name? value* (name '=' value)* ('{' tag* '}')?</c> with tags separated by line breaks or
/// semicolons. The builder maintains a single invariant across its mutually recursive methods: on return, the
/// reader is positioned on the terminator token (a line break, a closing brace, or end-of-document).
/// </summary>
internal static class SdlDomBuilder
{
	internal static void Build(ref Utf8SdlReader reader, Tag root)
	{
		if (!reader.Read())
		{
			return;
		}

		ParseTagSequence(ref reader, root, topLevel: true);
	}

	private static void ParseTagSequence(ref Utf8SdlReader reader, Tag parent, bool topLevel)
	{
		while (true)
		{
			switch (reader.TokenType)
			{
				case SdlTokenType.LineBreak:
					if (!reader.Read())
					{
						if (!topLevel)
						{
							throw Error(ref reader, "Unterminated child block; expected '}'");
						}

						return;
					}

					continue;

				case SdlTokenType.CloseBrace:
					if (topLevel)
					{
						throw Error(ref reader, "Unexpected '}' at the top level");
					}

					return;

				case SdlTokenType.EndOfDocument:
					if (!topLevel)
					{
						throw Error(ref reader, "Unterminated child block; expected '}'");
					}

					return;

				default:
					Tag tag = new();
					ParseTag(ref reader, tag);
					parent.Children.Add(tag);
					continue;
			}
		}
	}

	private static void ParseTag(ref Utf8SdlReader reader, Tag tag)
	{
		bool first = true;

		while (true)
		{
			switch (reader.TokenType)
			{
				case SdlTokenType.Identifier:
					string ns = reader.GetNamespace();
					string name = reader.GetName();
					bool more = reader.Read();

					if (more && reader.TokenType == SdlTokenType.Equals)
					{
						if (!reader.Read() || reader.TokenType != SdlTokenType.Value)
						{
							throw Error(ref reader, $"Expected a value after '{name}='");
						}

						tag.Attributes.Add(name, BuildValue(ref reader), ns.Length == 0 ? null : ns);
						first = false;

						if (!reader.Read())
						{
							return;
						}

						continue;
					}

					if (!first)
					{
						throw Error(ref reader, $"Unexpected identifier '{name}'");
					}

					tag.Namespace = ns.Length == 0 ? null : ns;
					tag.Name = name;
					first = false;

					if (!more)
					{
						return;
					}

					continue;

				case SdlTokenType.Value:
					tag.Values.Add(BuildValue(ref reader));
					first = false;
					if (!reader.Read())
					{
						return;
					}

					continue;

				case SdlTokenType.OpenBrace:
					if (!reader.Read())
					{
						throw Error(ref reader, "Unterminated child block; expected '}'");
					}

					ParseTagSequence(ref reader, tag, topLevel: false);
					reader.Read();
					return;

				default:
					return;
			}
		}
	}

	private static SdlValue BuildValue(ref Utf8SdlReader reader) => reader.ValueKind switch
	{
		SdlValueKind.Null => SdlValue.Null(),
		SdlValueKind.String => SdlValue.Create(reader.GetString()),
		SdlValueKind.Char => SdlValue.Create(reader.GetRune()),
		SdlValueKind.Boolean => SdlValue.Create(reader.GetBoolean()),
		SdlValueKind.Int32 => SdlValue.Create(reader.GetInt32()),
		SdlValueKind.Int64 => SdlValue.Create(reader.GetInt64()),
		SdlValueKind.Single => SdlValue.Create(reader.GetSingle()),
		SdlValueKind.Double => SdlValue.Create(reader.GetDouble()),
		SdlValueKind.Decimal => SdlValue.Create(reader.GetDecimal()),
		SdlValueKind.Date => SdlValue.Create(reader.GetDateOnly()),
		SdlValueKind.DateTime => SdlValue.Create(reader.GetDateTime()),
		SdlValueKind.DateTimeOffset => SdlValue.Create(reader.GetDateTimeOffset()),
		SdlValueKind.TimeSpan => SdlValue.Create(reader.GetTimeSpan()),
		SdlValueKind.Binary => SdlValue.CreateBinary(reader.GetBytes()),
		_ => SdlValue.Null(),
	};

	private static SdlReaderException Error(ref Utf8SdlReader reader, string message)
		=> new(message, reader.CurrentLine, 0, 0);
}
