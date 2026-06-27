using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using EllipticBit.SDLang.VisualStudio;
using EllipticBit.SDLang.VisualStudio.LanguageServer;

using Microsoft.VisualStudio.Text;

namespace EllipticBit.SDLang.VisualStudio;

/// <summary>
/// A single, per-buffer diagnostic computed result: the diagnostics and the exact snapshot they were produced
/// from, so consumers can translate spans without risking version drift.
/// </summary>
internal sealed class SdlDiagnosticResult
{
	public SdlDiagnosticResult(ITextSnapshot snapshot, IReadOnlyList<SdlEditorDiagnostic> diagnostics)
	{
		Snapshot = snapshot;
		Diagnostics = diagnostics;
	}

	/// <summary>Gets the snapshot the diagnostics were computed against.</summary>
	public ITextSnapshot Snapshot { get; }

	/// <summary>Gets the diagnostics, each already resolved to a <see cref="SnapshotSpan"/> on <see cref="Snapshot"/>.</summary>
	public IReadOnlyList<SdlEditorDiagnostic> Diagnostics { get; }
}

/// <summary>A validation diagnostic resolved onto a concrete editor span.</summary>
internal sealed class SdlEditorDiagnostic
{
	public SdlEditorDiagnostic(SdlValidationDiagnostic diagnostic, SnapshotSpan span)
	{
		Diagnostic = diagnostic;
		Span = span;
	}

	/// <summary>Gets the underlying contract diagnostic (message, severity, code, source).</summary>
	public SdlValidationDiagnostic Diagnostic { get; }

	/// <summary>Gets the resolved span within the buffer snapshot.</summary>
	public SnapshotSpan Span { get; }
}

/// <summary>
/// Owns validation for a single SDLang text buffer. It debounces buffer changes, runs the shared
/// <see cref="SdlLanguageEngine"/> off the UI thread, resolves each diagnostic's 0-based line/character span onto
/// the buffer snapshot, and raises <see cref="DiagnosticsChanged"/> so the squiggle tagger and the Error List
/// source can refresh from one shared result.
/// </summary>
internal sealed class SdlBufferValidator
{
	private const int DebounceMilliseconds = 350;

	private readonly ITextBuffer _buffer;
	private readonly SdlLanguageEngine _engine;
	private readonly object _gate = new();

	private CancellationTokenSource? _pending;
	private SdlDiagnosticResult _current;

	private SdlBufferValidator(ITextBuffer buffer, SdlLanguageEngine engine)
	{
		_buffer = buffer;
		_engine = engine;
		_current = new SdlDiagnosticResult(buffer.CurrentSnapshot, Array.Empty<SdlEditorDiagnostic>());
		_buffer.ChangedLowPriority += OnBufferChanged;
		ScheduleValidation();
	}

	/// <summary>Raised after a validation pass completes with a new diagnostic result.</summary>
	public event EventHandler<SdlDiagnosticResult>? DiagnosticsChanged;

	/// <summary>Gets the most recent diagnostic result for the buffer.</summary>
	public SdlDiagnosticResult Current
	{
		get
		{
			lock (_gate)
			{
				return _current;
			}
		}
	}

	/// <summary>
	/// Gets the validator attached to <paramref name="buffer"/>, creating it on first use. One validator instance
	/// is shared by every editor component working over the same buffer.
	/// </summary>
	public static SdlBufferValidator GetOrCreate(ITextBuffer buffer, SdlLanguageEngine engine)
	{
		if (buffer is null) throw new ArgumentNullException(nameof(buffer));
		if (engine is null) throw new ArgumentNullException(nameof(engine));

		return buffer.Properties.GetOrCreateSingletonProperty(
			typeof(SdlBufferValidator),
			() => new SdlBufferValidator(buffer, engine));
	}

	private void OnBufferChanged(object sender, TextContentChangedEventArgs e) => ScheduleValidation();

	private void ScheduleValidation()
	{
		CancellationTokenSource cts = new();
		CancellationToken token;
		lock (_gate)
		{
			_pending?.Cancel();
			_pending?.Dispose();
			_pending = cts;
			token = cts.Token;
		}

		_ = ValidateAfterDelayAsync(token);
	}

	private async Task ValidateAfterDelayAsync(CancellationToken token)
	{
		try
		{
			await Task.Delay(DebounceMilliseconds, token).ConfigureAwait(false);

			ITextSnapshot snapshot = _buffer.CurrentSnapshot;
			string text = snapshot.GetText();
			string? documentUri = TryGetDocumentPath();

			IReadOnlyList<SdlValidationDiagnostic> diagnostics =
				await _engine.ValidateAsync(text, documentUri, token).ConfigureAwait(false);

			if (token.IsCancellationRequested)
			{
				return;
			}

			SdlDiagnosticResult result = new(snapshot, Resolve(diagnostics, snapshot));
			lock (_gate)
			{
				_current = result;
			}

			DiagnosticsChanged?.Invoke(this, result);
		}
		catch (OperationCanceledException)
		{
			// A newer change superseded this pass; ignore.
		}
	}

	private static IReadOnlyList<SdlEditorDiagnostic> Resolve(IReadOnlyList<SdlValidationDiagnostic> diagnostics, ITextSnapshot snapshot)
	{
		if (diagnostics.Count == 0)
		{
			return Array.Empty<SdlEditorDiagnostic>();
		}

		List<SdlEditorDiagnostic> resolved = new(diagnostics.Count);
		foreach (SdlValidationDiagnostic diagnostic in diagnostics)
		{
			if (TryResolveSpan(diagnostic.Span, snapshot, out SnapshotSpan span))
			{
				resolved.Add(new SdlEditorDiagnostic(diagnostic, span));
			}
		}

		return resolved;
	}

	private static bool TryResolveSpan(SdlTextSpan span, ITextSnapshot snapshot, out SnapshotSpan result)
	{
		result = default;
		int lineCount = snapshot.LineCount;
		if (span.StartLine < 0 || span.StartLine >= lineCount)
		{
			return false;
		}

		ITextSnapshotLine startLine = snapshot.GetLineFromLineNumber(span.StartLine);
		int start = startLine.Start.Position + Math.Min(Math.Max(span.StartCharacter, 0), startLine.Length);

		int endLineNumber = Math.Min(Math.Max(span.EndLine, span.StartLine), lineCount - 1);
		ITextSnapshotLine endLine = snapshot.GetLineFromLineNumber(endLineNumber);
		int end = endLine.Start.Position + Math.Min(Math.Max(span.EndCharacter, 0), endLine.Length);

		if (end < start)
		{
			end = start;
		}

		// Ensure a visible squiggle for zero-length (caret) diagnostics by extending one character when possible.
		if (end == start)
		{
			if (end < snapshot.Length && end < endLine.End.Position)
			{
				end++;
			}
			else if (start > startLine.Start.Position)
			{
				start--;
			}
		}

		result = new SnapshotSpan(snapshot, Span.FromBounds(start, end));
		return true;
	}

	private string? TryGetDocumentPath()
	{
		if (_buffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document))
		{
			return document?.FilePath;
		}

		return null;
	}
}
