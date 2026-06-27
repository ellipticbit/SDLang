using System.Collections.Generic;

using EllipticBit.SDLang.VisualStudio;

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;

namespace EllipticBit.SDLang.VisualStudio;

/// <summary>
/// An Error List <see cref="ITableDataSource"/> for a single SDLang buffer. It exposes one factory whose snapshot
/// is rebuilt whenever the buffer's <see cref="SdlBufferValidator"/> reports new diagnostics, so SDLang problems
/// appear in (and navigate from) the standard Error List.
/// </summary>
internal sealed class SdlErrorListSource : ITableDataSource
{
	internal static readonly string[] Columns =
	{
		StandardTableColumnDefinitions.ErrorSeverity,
		StandardTableColumnDefinitions.ErrorCode,
		StandardTableColumnDefinitions.Text,
		StandardTableColumnDefinitions.DocumentName,
		StandardTableColumnDefinitions.Line,
		StandardTableColumnDefinitions.Column,
		StandardTableColumnDefinitions.BuildTool,
	};

	private readonly SdlBufferValidator _validator;
	private readonly SdlErrorTableFactory _factory;
	private readonly object _gate = new();
	private readonly List<ITableDataSink> _sinks = new();

	private SdlErrorListSource(ITextBuffer buffer, SdlBufferValidator validator)
	{
		_validator = validator;
		_factory = new SdlErrorTableFactory();
		Identifier = "EllipticBit.SDLang.ErrorSource." + buffer.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);

		UpdateSnapshot(validator.Current);
		_validator.DiagnosticsChanged += OnDiagnosticsChanged;
	}

	/// <inheritdoc />
	public string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;

	/// <inheritdoc />
	public string Identifier { get; }

	/// <inheritdoc />
	public string DisplayName => "SDLang";

	/// <summary>
	/// Gets the Error List source attached to <paramref name="buffer"/>, registering it with the supplied table
	/// manager exactly once.
	/// </summary>
	public static SdlErrorListSource GetOrCreate(ITextBuffer buffer, SdlBufferValidator validator, ITableManager tableManager)
	{
		return buffer.Properties.GetOrCreateSingletonProperty(
			typeof(SdlErrorListSource),
			() =>
			{
				SdlErrorListSource source = new(buffer, validator);
				tableManager.AddSource(source, Columns);
				return source;
			});
	}

	/// <inheritdoc />
	public IDisposable Subscribe(ITableDataSink sink)
	{
		lock (_gate)
		{
			_sinks.Add(sink);
		}

		sink.AddFactory(_factory, removeAllFactories: false);
		return new Unsubscriber(this, sink);
	}

	private void OnDiagnosticsChanged(object sender, SdlDiagnosticResult result)
	{
		UpdateSnapshot(result);

		ITableDataSink[] current;
		lock (_gate)
		{
			current = _sinks.ToArray();
		}

		foreach (ITableDataSink sink in current)
		{
			sink.FactorySnapshotChanged(_factory);
		}
	}

	private void UpdateSnapshot(SdlDiagnosticResult result)
	{
		string? path = TryGetPath(result.Snapshot.TextBuffer);
		_factory.Update(new SdlErrorTableSnapshot(_factory.NextVersion(), path, result));
	}

	private static string? TryGetPath(ITextBuffer buffer)
		=> buffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document) ? document?.FilePath : null;

	private void Remove(ITableDataSink sink)
	{
		lock (_gate)
		{
			_sinks.Remove(sink);
		}
	}

	private sealed class Unsubscriber : IDisposable
	{
		private readonly SdlErrorListSource _source;
		private readonly ITableDataSink _sink;

		public Unsubscriber(SdlErrorListSource source, ITableDataSink sink)
		{
			_source = source;
			_sink = sink;
		}

		public void Dispose() => _source.Remove(_sink);
	}
}

/// <summary>The single factory owned by an <see cref="SdlErrorListSource"/>; it always serves the latest snapshot.</summary>
internal sealed class SdlErrorTableFactory : TableEntriesSnapshotFactoryBase
{
	private readonly object _gate = new();
	private int _version;
	private SdlErrorTableSnapshot _snapshot = SdlErrorTableSnapshot.Empty;

	/// <summary>Returns the next monotonically increasing version number for a new snapshot.</summary>
	public int NextVersion() => System.Threading.Interlocked.Increment(ref _version);

	/// <summary>Atomically replaces the current snapshot.</summary>
	public void Update(SdlErrorTableSnapshot snapshot)
	{
		lock (_gate)
		{
			_snapshot = snapshot;
		}
	}

	/// <inheritdoc />
	public override int CurrentVersionNumber
	{
		get
		{
			lock (_gate)
			{
				return _snapshot.VersionNumber;
			}
		}
	}

	/// <inheritdoc />
	public override ITableEntriesSnapshot GetCurrentSnapshot()
	{
		lock (_gate)
		{
			return _snapshot;
		}
	}

	/// <inheritdoc />
	public override ITableEntriesSnapshot? GetSnapshot(int versionNumber)
	{
		lock (_gate)
		{
			return _snapshot.VersionNumber == versionNumber ? _snapshot : null;
		}
	}

	/// <inheritdoc />
	public override void Dispose()
	{
	}
}

/// <summary>
/// An immutable Error List snapshot built from one <see cref="SdlDiagnosticResult"/>. Each row exposes severity,
/// message, code, document name, and 1-based line/column for navigation.
/// </summary>
internal sealed class SdlErrorTableSnapshot : TableEntriesSnapshotBase
{
	internal static readonly SdlErrorTableSnapshot Empty = new();

	private readonly int _version;
	private readonly string? _documentPath;
	private readonly IReadOnlyList<SdlEditorDiagnostic> _diagnostics;
	private readonly ITextSnapshot? _snapshot;

	private SdlErrorTableSnapshot()
	{
		_version = 0;
		_documentPath = null;
		_diagnostics = Array.Empty<SdlEditorDiagnostic>();
		_snapshot = null;
	}

	public SdlErrorTableSnapshot(int version, string? documentPath, SdlDiagnosticResult result)
	{
		_version = version;
		_documentPath = documentPath;
		_diagnostics = result.Diagnostics;
		_snapshot = result.Snapshot;
	}

	/// <inheritdoc />
	public override int Count => _diagnostics.Count;

	/// <inheritdoc />
	public override int VersionNumber => _version;

	/// <inheritdoc />
	public override bool TryGetValue(int index, string keyName, out object? content)
	{
		content = null;
		if (index < 0 || index >= _diagnostics.Count)
		{
			return false;
		}

		SdlEditorDiagnostic diagnostic = _diagnostics[index];
		switch (keyName)
		{
			case StandardTableKeyNames.ErrorSeverity:
				content = ToErrorCategory(diagnostic.Diagnostic.Severity);
				return true;
			case StandardTableKeyNames.Text:
				content = diagnostic.Diagnostic.Message;
				return true;
			case StandardTableKeyNames.ErrorCode:
				content = diagnostic.Diagnostic.Code ?? string.Empty;
				return true;
			case StandardTableKeyNames.DocumentName:
				content = _documentPath ?? string.Empty;
				return _documentPath is not null;
			case StandardTableKeyNames.Line:
				content = GetLine(diagnostic);
				return true;
			case StandardTableKeyNames.Column:
				content = GetColumn(diagnostic);
				return true;
			case StandardTableKeyNames.BuildTool:
				content = diagnostic.Diagnostic.Source ?? "SDLang";
				return true;
			default:
				return false;
		}
	}

	private int GetLine(SdlEditorDiagnostic diagnostic)
		=> _snapshot is null ? 0 : _snapshot.GetLineNumberFromPosition(diagnostic.Span.Start.Position);

	private int GetColumn(SdlEditorDiagnostic diagnostic)
	{
		if (_snapshot is null)
		{
			return 0;
		}

		ITextSnapshotLine line = _snapshot.GetLineFromPosition(diagnostic.Span.Start.Position);
		return diagnostic.Span.Start.Position - line.Start.Position;
	}

	private static __VSERRORCATEGORY ToErrorCategory(SdlValidationSeverity severity) => severity switch
	{
		SdlValidationSeverity.Error => __VSERRORCATEGORY.EC_ERROR,
		SdlValidationSeverity.Warning => __VSERRORCATEGORY.EC_WARNING,
		_ => __VSERRORCATEGORY.EC_MESSAGE,
	};
}
