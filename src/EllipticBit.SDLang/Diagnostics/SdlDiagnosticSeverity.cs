namespace EllipticBit.SDLang;

/// <summary>
/// Describes the severity of an <see cref="SdlDiagnostic"/> produced during permissive (error-recovering) parsing
/// or by an external semantic analyzer. The numeric values intentionally match the Language Server Protocol
/// <c>DiagnosticSeverity</c> enumeration so diagnostics can be surfaced in editors without remapping.
/// </summary>
public enum SdlDiagnosticSeverity
{
	/// <summary>An error that typically prevents the document from being interpreted correctly.</summary>
	Error = 1,

	/// <summary>A warning about a construct that is legal but likely incorrect or discouraged.</summary>
	Warning = 2,

	/// <summary>Informational guidance that does not indicate a problem.</summary>
	Information = 3,

	/// <summary>A subtle hint, such as a refactoring opportunity, usually rendered unobtrusively.</summary>
	Hint = 4,
}
