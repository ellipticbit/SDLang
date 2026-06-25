using System.Text;

namespace EllipticBit.SDLang.Serialization;

/// <summary>
/// Determines how CLR member names are translated to SDL names. Mirrors
/// <c>System.Text.Json.JsonNamingPolicy</c>. Built-in policies are exposed as static properties.
/// </summary>
public abstract class SdlNamingPolicy
{
	/// <summary>Gets a policy that converts names to <c>camelCase</c>.</summary>
	public static SdlNamingPolicy CamelCase { get; } = new CamelCasePolicy();

	/// <summary>Gets a policy that converts names to <c>PascalCase</c>.</summary>
	public static SdlNamingPolicy PascalCase { get; } = new PascalCasePolicy();

	/// <summary>Gets a policy that converts names to <c>snake_case</c>.</summary>
	public static SdlNamingPolicy SnakeCaseLower { get; } = new SnakeCasePolicy(upper: false);

	/// <summary>Gets a policy that converts names to <c>SNAKE_CASE</c>.</summary>
	public static SdlNamingPolicy SnakeCaseUpper { get; } = new SnakeCasePolicy(upper: true);

	/// <summary>Gets a policy that converts names to <c>kebab-case</c>.</summary>
	public static SdlNamingPolicy KebabCaseLower { get; } = new KebabCasePolicy(upper: false);

	/// <summary>Gets a policy that converts names to <c>KEBAB-CASE</c>.</summary>
	public static SdlNamingPolicy KebabCaseUpper { get; } = new KebabCasePolicy(upper: true);

	/// <summary>Converts the supplied member name to the SDL name.</summary>
	public abstract string ConvertName(string name);

	private sealed class CamelCasePolicy : SdlNamingPolicy
	{
		public override string ConvertName(string name)
		{
			if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
			{
				return name;
			}

			return string.Create(name.Length, name, static (span, source) =>
			{
				source.CopyTo(span);
				span[0] = char.ToLowerInvariant(span[0]);
				for (int i = 1; i < span.Length && char.IsUpper(span[i]) && (i + 1 == span.Length || char.IsUpper(span[i + 1])); i++)
				{
					span[i] = char.ToLowerInvariant(span[i]);
				}
			});
		}
	}

	private sealed class PascalCasePolicy : SdlNamingPolicy
	{
		public override string ConvertName(string name)
		{
			if (string.IsNullOrEmpty(name) || char.IsUpper(name[0]))
			{
				return name;
			}

			return string.Create(name.Length, name, static (span, source) =>
			{
				source.CopyTo(span);
				span[0] = char.ToUpperInvariant(span[0]);
			});
		}
	}

	private sealed class SnakeCasePolicy(bool upper) : SdlNamingPolicy
	{
		public override string ConvertName(string name) => SeparatorPolicy.Convert(name, '_', upper);
	}

	private sealed class KebabCasePolicy(bool upper) : SdlNamingPolicy
	{
		public override string ConvertName(string name) => SeparatorPolicy.Convert(name, '-', upper);
	}

	private static class SeparatorPolicy
	{
		public static string Convert(string name, char separator, bool upper)
		{
			if (string.IsNullOrEmpty(name))
			{
				return name;
			}

			StringBuilder sb = new(name.Length + 8);
			for (int i = 0; i < name.Length; i++)
			{
				char c = name[i];
				if (char.IsUpper(c))
				{
					if (i > 0 && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
					{
						sb.Append(separator);
					}

					sb.Append(upper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
				}
				else
				{
					sb.Append(upper ? char.ToUpperInvariant(c) : c);
				}
			}

			return sb.ToString();
		}
	}
}
