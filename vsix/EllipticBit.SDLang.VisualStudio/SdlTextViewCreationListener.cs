using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace EllipticBit.SDLang.VisualStudio;

/// <summary>
/// Connects each opened SDLang view to the diagnostics pipeline: it ensures the buffer's
/// <see cref="SdlBufferValidator"/> exists (which starts validation) and registers the buffer's
/// <see cref="SdlErrorListSource"/> with the Error List table manager so problems surface there.
/// </summary>
/// <remarks>
/// The squiggle tagger is created separately by the editor through <see cref="SdlErrorTaggerProvider"/>; this
/// listener guarantees the Error List feed is wired even when no tagger has been requested yet, and it gives the
/// validator an early start as soon as a document is shown.
/// </remarks>
[Export(typeof(IWpfTextViewCreationListener))]
[ContentType(SdlContentTypes.ContentTypeName)]
[TextViewRole(PredefinedTextViewRoles.Document)]
internal sealed class SdlTextViewCreationListener : IWpfTextViewCreationListener
{
	private readonly SdlLanguageEngineProvider _engineProvider;
	private readonly ITableManagerProvider _tableManagerProvider;

	/// <summary>Initializes the listener with the shared engine and the Error List table manager provider.</summary>
	/// <param name="engineProvider">The MEF-shared language engine provider.</param>
	/// <param name="tableManagerProvider">The provider used to obtain the Error List table manager.</param>
	[ImportingConstructor]
	public SdlTextViewCreationListener(
		SdlLanguageEngineProvider engineProvider,
		ITableManagerProvider tableManagerProvider)
	{
		_engineProvider = engineProvider ?? throw new ArgumentNullException(nameof(engineProvider));
		_tableManagerProvider = tableManagerProvider ?? throw new ArgumentNullException(nameof(tableManagerProvider));
	}

	/// <inheritdoc />
	public void TextViewCreated(IWpfTextView textView)
	{
		if (textView is null)
		{
			return;
		}

		SdlBufferValidator validator = SdlBufferValidator.GetOrCreate(textView.TextBuffer, _engineProvider.Engine);
		ITableManager tableManager = _tableManagerProvider.GetTableManager(StandardTables.ErrorsTable);
		SdlErrorListSource.GetOrCreate(textView.TextBuffer, validator, tableManager);
	}
}
