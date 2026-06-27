using System.Collections.Generic;
using System.ComponentModel.Composition;

using EllipticBit.SDLang.VisualStudio;
using EllipticBit.SDLang.VisualStudio.LanguageServer;

namespace EllipticBit.SDLang.VisualStudio;

/// <summary>
/// A MEF-shared host for the single in-proc <see cref="SdlLanguageEngine"/> instance used by the editor and the
/// brokered service. It imports every exported <see cref="ISdlSemanticValidator"/> (with metadata) and constructs
/// the engine once, so the entire Visual Studio session shares one validator composition.
/// </summary>
/// <remarks>
/// This provider lives in the VSIX assembly — the one declared as a MEF component in the manifest — so that the
/// <c>[ImportMany]</c> of validators is satisfied by the Visual Studio composition container. Other components
/// (the error tagger, the Error List source) import this provider; the <see cref="SdlLanguagePackage"/> obtains
/// it through the component model to proffer the brokered service over the same engine.
/// </remarks>
[Export(typeof(SdlLanguageEngineProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class SdlLanguageEngineProvider
{
	/// <summary>Initializes the provider and builds the shared engine from the imported validators.</summary>
	/// <param name="validators">The semantic validators contributed through MEF.</param>
	[ImportingConstructor]
	public SdlLanguageEngineProvider(
		[ImportMany] IEnumerable<Lazy<ISdlSemanticValidator, ISdlSemanticValidatorMetadata>> validators)
	{
		Engine = new SdlLanguageEngine(validators);
	}

	/// <summary>Gets the shared language engine.</summary>
	public SdlLanguageEngine Engine { get; }
}
