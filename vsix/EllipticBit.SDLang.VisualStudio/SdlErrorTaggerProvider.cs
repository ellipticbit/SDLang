using System.Collections.Generic;
using System.ComponentModel.Composition;

using EllipticBit.SDLang.VisualStudio;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace EllipticBit.SDLang.VisualStudio;

/// <summary>
/// Produces editor error tags (squiggles) for SDLang buffers from the diagnostics computed by the shared
/// <see cref="SdlBufferValidator"/>.
/// </summary>
[Export(typeof(ITaggerProvider))]
[ContentType(SdlContentTypes.ContentTypeName)]
[TagType(typeof(IErrorTag))]
internal sealed class SdlErrorTaggerProvider : ITaggerProvider
{
	private readonly SdlLanguageEngineProvider _engineProvider;

	/// <summary>Initializes the provider with the shared engine host.</summary>
	/// <param name="engineProvider">The MEF-shared language engine provider.</param>
	[ImportingConstructor]
	public SdlErrorTaggerProvider(SdlLanguageEngineProvider engineProvider)
	{
		_engineProvider = engineProvider ?? throw new ArgumentNullException(nameof(engineProvider));
	}

	/// <inheritdoc />
	public ITagger<T>? CreateTagger<T>(ITextBuffer buffer)
		where T : ITag
	{
		if (buffer is null)
		{
			return null;
		}

		SdlBufferValidator validator = SdlBufferValidator.GetOrCreate(buffer, _engineProvider.Engine);
		return buffer.Properties.GetOrCreateSingletonProperty(
			typeof(SdlErrorTagger),
			() => new SdlErrorTagger(validator)) as ITagger<T>;
	}
}

/// <summary>
/// An <see cref="ITagger{IErrorTag}"/> that maps SDLang diagnostics onto error tags, choosing the squiggle color
/// (error vs. warning vs. suggestion) from each diagnostic's severity and using its message as the tooltip.
/// </summary>
internal sealed class SdlErrorTagger : ITagger<IErrorTag>
{
	private readonly SdlBufferValidator _validator;

	public SdlErrorTagger(SdlBufferValidator validator)
	{
		_validator = validator;
		_validator.DiagnosticsChanged += OnDiagnosticsChanged;
	}

	/// <inheritdoc />
	public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

	/// <inheritdoc />
	public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
	{
		if (spans.Count == 0)
		{
			yield break;
		}

		SdlDiagnosticResult result = _validator.Current;
		ITextSnapshot requested = spans[0].Snapshot;

		foreach (SdlEditorDiagnostic diagnostic in result.Diagnostics)
		{
			SnapshotSpan span = diagnostic.Span;
			if (span.Snapshot != requested)
			{
				span = span.TranslateTo(requested, SpanTrackingMode.EdgeInclusive);
			}

			if (!spans.IntersectsWith(span))
			{
				continue;
			}

			string errorType = GetErrorType(diagnostic.Diagnostic.Severity);
			yield return new TagSpan<IErrorTag>(span, new ErrorTag(errorType, diagnostic.Diagnostic.Message));
		}
	}

	private void OnDiagnosticsChanged(object sender, SdlDiagnosticResult result)
	{
		ITextSnapshot snapshot = result.Snapshot;
		TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
	}

	private static string GetErrorType(SdlValidationSeverity severity) => severity switch
	{
		SdlValidationSeverity.Error => PredefinedErrorTypeNames.SyntaxError,
		SdlValidationSeverity.Warning => PredefinedErrorTypeNames.Warning,
		SdlValidationSeverity.Information => PredefinedErrorTypeNames.HintedSuggestion,
		SdlValidationSeverity.Hint => PredefinedErrorTypeNames.HintedSuggestion,
		_ => PredefinedErrorTypeNames.SyntaxError,
	};
}
