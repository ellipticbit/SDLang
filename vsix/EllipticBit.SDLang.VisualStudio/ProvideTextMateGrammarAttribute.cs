using System.IO;

using Microsoft.VisualStudio.Shell;

namespace EllipticBit.SDLang.VisualStudio;

/// <summary>
/// Registers a TextMate grammar with Visual Studio so buffers of the associated content type receive syntax
/// highlighting, bracket/quote auto-close, comment toggling, and indentation from the packaged grammar and
/// language-configuration files.
/// </summary>
/// <remarks>
/// <para>
/// Packaging the <c>Grammars\*</c> files into the VSIX is not enough on its own: Visual Studio's TextMate engine
/// only discovers grammars from folders registered under three well-known pkgdef keys. Without these entries the
/// grammar is present on disk but never loaded, which is why <c>.sdl</c> files opened from disk showed no
/// highlighting. This attribute emits the same three keys that shipping Visual Studio extensions (for example the
/// REST and ASP.NET Razor editors) use:
/// </para>
/// <list type="bullet">
///   <item>
///     <description><c>TextMate\Repositories</c> — points a repository name at the folder that is scanned
///     (recursively) for grammar files such as <c>sdlang.tmLanguage.json</c>.</description>
///   </item>
///   <item>
///     <description><c>TextMate\LanguageConfiguration\GrammarMapping</c> — keyed by the grammar's TextMate scope
///     name (<c>source.sdl</c>); binds the language-configuration file to the grammar.</description>
///   </item>
///   <item>
///     <description><c>TextMate\LanguageConfiguration\ContentTypeMapping</c> — keyed by the editor content-type
///     name (<c>sdlang</c>); binds the language-configuration file to the content type.</description>
///   </item>
/// </list>
/// <para>
/// The attribute is processed at build time by CreatePkgDef, which reflects over the package assembly and writes
/// the resulting entries into the generated pkgdef. <see cref="RegistrationContext.ComponentPath"/> resolves to the
/// deployed extension folder and is tokenized to <c>$PackageFolder$</c> in the emitted pkgdef, so the registered
/// paths remain correct wherever Visual Studio installs the VSIX.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
internal sealed class ProvideTextMateGrammarAttribute : RegistrationAttribute
{
	private const string RepositoriesKey = @"TextMate\Repositories";
	private const string GrammarMappingKey = @"TextMate\LanguageConfiguration\GrammarMapping";
	private const string ContentTypeMappingKey = @"TextMate\LanguageConfiguration\ContentTypeMapping";

	/// <summary>Initializes the attribute with the grammar's registration details.</summary>
	/// <param name="repositoryName">The repository name to register; identifies the grammar folder to VS.</param>
	/// <param name="grammarFolder">The VSIX-relative folder that contains the grammar and configuration files.</param>
	/// <param name="scopeName">The TextMate scope name of the grammar (for example <c>source.sdl</c>).</param>
	/// <param name="contentTypeName">The editor content-type name (for example <c>sdlang</c>).</param>
	/// <param name="languageConfigurationFileName">The language-configuration file name within the grammar folder.</param>
	public ProvideTextMateGrammarAttribute(
		string repositoryName,
		string grammarFolder,
		string scopeName,
		string contentTypeName,
		string languageConfigurationFileName)
	{
		RepositoryName = repositoryName ?? throw new ArgumentNullException(nameof(repositoryName));
		GrammarFolder = grammarFolder ?? throw new ArgumentNullException(nameof(grammarFolder));
		ScopeName = scopeName ?? throw new ArgumentNullException(nameof(scopeName));
		ContentTypeName = contentTypeName ?? throw new ArgumentNullException(nameof(contentTypeName));
		LanguageConfigurationFileName = languageConfigurationFileName
			?? throw new ArgumentNullException(nameof(languageConfigurationFileName));
	}

	/// <summary>The repository name registered under <c>TextMate\Repositories</c>.</summary>
	public string RepositoryName { get; }

	/// <summary>The VSIX-relative folder that contains the grammar and language-configuration files.</summary>
	public string GrammarFolder { get; }

	/// <summary>The TextMate scope name used as the <c>GrammarMapping</c> key.</summary>
	public string ScopeName { get; }

	/// <summary>The editor content-type name used as the <c>ContentTypeMapping</c> key.</summary>
	public string ContentTypeName { get; }

	/// <summary>The language-configuration file name within <see cref="GrammarFolder"/>.</summary>
	public string LanguageConfigurationFileName { get; }

	/// <inheritdoc />
	public override void Register(RegistrationContext context)
	{
		if (context is null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		string grammarFolderPath = Path.Combine(context.ComponentPath, GrammarFolder);
		string languageConfigurationPath = Path.Combine(grammarFolderPath, LanguageConfigurationFileName);

		using (Key key = context.CreateKey(RepositoriesKey))
		{
			key.SetValue(RepositoryName, grammarFolderPath);
		}

		using (Key key = context.CreateKey(GrammarMappingKey))
		{
			key.SetValue(ScopeName, languageConfigurationPath);
		}

		using (Key key = context.CreateKey(ContentTypeMappingKey))
		{
			key.SetValue(ContentTypeName, languageConfigurationPath);
		}
	}

	/// <inheritdoc />
	public override void Unregister(RegistrationContext context)
	{
		if (context is null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		context.RemoveValue(RepositoriesKey, RepositoryName);
		context.RemoveValue(GrammarMappingKey, ScopeName);
		context.RemoveValue(ContentTypeMappingKey, ContentTypeName);
	}
}
