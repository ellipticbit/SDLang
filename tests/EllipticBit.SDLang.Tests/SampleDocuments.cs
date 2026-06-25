namespace EllipticBit.SDLang.Tests;

/// <summary>
/// Reusable SDLang sample documents that exercise the breadth of the language: tags, namespaces, values of every
/// literal type, attributes, nested children, comments, line continuations, and non-ASCII identifiers/content
/// (emoji and Kanji) per the project requirements.
/// </summary>
internal static class SampleDocuments
{
	internal const string SimpleTag = "greeting \"Hello, World\"\n";

	internal const string TagWithAttributes =
		"person name=\"Alice\" age=42 active=true\n";

	internal const string NestedChildren =
		"matrix {\n" +
		"  row 1 2 3\n" +
		"  row 4 5 6\n" +
		"}\n";

	internal const string Namespaced =
		"ns:config ns:enabled=true {\n" +
		"  ns:item \"value\"\n" +
		"}\n";

	internal const string AllValueKinds =
		"values \\\n" +
		"  \"text\" \\\n" +
		"  'c' \\\n" +
		"  42 \\\n" +
		"  9000000000L \\\n" +
		"  3.14f \\\n" +
		"  2.718 \\\n" +
		"  19.99BD \\\n" +
		"  true \\\n" +
		"  null \\\n" +
		"  2015/12/06 \\\n" +
		"  2015/12/06 12:30:00 \\\n" +
		"  [aGVsbG8=]\n";

	internal const string WithComments =
		"// line comment\n" +
		"tag1 1 # hash comment\n" +
		"/* block\n" +
		"   comment */\n" +
		"tag2 2 -- dashes\n";

	internal const string Unicode =
		"emoji \"Hello \U0001F600 World\"\n" +
		"\u6F22\u5B57 \"\u65E5\u672C\u8A9E\"\n";

	internal const string MultipleTopLevel =
		"first 1\n" +
		"second 2\n" +
		"third 3\n";
}
