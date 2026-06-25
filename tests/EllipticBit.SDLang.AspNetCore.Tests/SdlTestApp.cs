using EllipticBit.SDLang.AspNetCore.Http;
using EllipticBit.SDLang.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EllipticBit.SDLang.AspNetCore.Tests;

/// <summary>
/// Builds an in-memory ASP.NET Core host (via <see cref="TestServer"/>) that registers the SDLang formatters, a test
/// controller, and Minimal API endpoints, exposing an <see cref="HttpClient"/> for end-to-end assertions.
/// </summary>
internal sealed class SdlTestApp : IAsyncDisposable
{
	private readonly WebApplication _app;

	private SdlTestApp(WebApplication app)
	{
		_app = app;
		Client = app.GetTestClient();
	}

	public HttpClient Client { get; }

	public static async Task<SdlTestApp> StartAsync(Action<SdlSerializerOptions>? configure = null)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();

		builder.Services
			.AddControllers()
			.AddSdlFormatters(configure ?? (_ => { }))
			.AddApplicationPart(typeof(ServersController).Assembly);

		WebApplication app = builder.Build();

		app.MapControllers();
		app.MapPost("/minimal/echo", (Sdl<Server> body) => Results.Extensions.Sdl(body.Value));
		app.MapGet("/minimal/server", () => Results.Extensions.Sdl(new Server { Host = "minimal", Port = 1234, Enabled = true }));

		await app.StartAsync();
		return new SdlTestApp(app);
	}

	public async ValueTask DisposeAsync()
	{
		Client.Dispose();
		await _app.DisposeAsync();
	}
}
