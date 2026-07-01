using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using EllipticBit.SDLang.VisualStudio;
using EllipticBit.SDLang.VisualStudio.LanguageServer;

using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;

using Task = System.Threading.Tasks.Task;

namespace EllipticBit.SDLang.VisualStudio;

/// <summary>
/// The SDLang extension package. Its sole responsibility is to proffer the brokered
/// <see cref="ISdlLanguageService"/> so out-of-MEF consumers — for example, the Visual D plug-in providing
/// <c>dub.sdl</c> support — can request SDLang validation, completion, and hover over JSON-RPC.
/// </summary>
/// <remarks>
/// The package loads in the background as soon as Visual Studio finishes its shell initialization and proffers
/// the service against the same <see cref="SdlLanguageEngine"/> that powers the in-proc editor (resolved from the
/// MEF <see cref="SdlLanguageEngineProvider"/>), guaranteeing identical results across both surfaces.
/// </remarks>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageGuidString)]
[ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
// Registers the brokered-service moniker in the pkgdef. IBrokeredServiceContainer.Proffer(...) requires the
// moniker to be registered first and otherwise throws InvalidOperationException (surfaced as
// "SetSite failed for package [SdlLanguagePackage]", HRESULT 0x80131509). Audience is Process because the only
// consumers are other in-proc extensions in the same devenv process (for example Visual D's dub.sdl support).
[ProvideBrokeredService(SdlBrokeredServices.LanguageServiceMonikerName, SdlBrokeredServices.LanguageServiceMonikerVersion, Audience = ServiceAudience.Process)]
// Registers the packaged TextMate grammar so Visual Studio actually loads it. The VSIX ships Grammars\* as
// content, but VS's TextMate engine only discovers grammars from folders registered under TextMate\Repositories
// (plus the GrammarMapping/ContentTypeMapping keys) in the pkgdef. Without this, .sdl files open with no
// highlighting even though the grammar is on disk. The content-type name must match the "sdlang" content type
// (which derives from "code++", the base type VS's TextMate classifiers target) and the scope must match the
// grammar's scopeName ("source.sdl").
[ProvideTextMateGrammar(
	"SDLang",
	"Grammars",
	SdlContentTypes.TextMateScopeName,
	SdlContentTypes.ContentTypeName,
	"sdlang.language-configuration.json")]
public sealed class SdlLanguagePackage : AsyncPackage
{
	/// <summary>The package GUID, also referenced by the generated pkgdef.</summary>
	public const string PackageGuidString = "f3a6c8e2-5d41-4b9a-8c70-2e6b9d4a1f58";

	/// <inheritdoc />
	protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
	{
		await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(false);

		SdlLanguageEngine engine = await ResolveEngineAsync(cancellationToken).ConfigureAwait(false);

		IBrokeredServiceContainer container = await this.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>()
			.ConfigureAwait(false);

		container.Proffer(
			SdlBrokeredServices.LanguageService,
			(moniker, options, serviceBroker, ct)
				=> new ValueTask<object?>(new SdlLanguageService(engine)));
	}

	private async Task<SdlLanguageEngine> ResolveEngineAsync(CancellationToken cancellationToken)
	{
		IComponentModel componentModel = await this.GetServiceAsync<SComponentModel, IComponentModel>()
			.ConfigureAwait(false);
		SdlLanguageEngineProvider provider = componentModel.GetService<SdlLanguageEngineProvider>();
		return provider.Engine;
	}
}
