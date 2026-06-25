using Microsoft.AspNetCore.Mvc;

namespace EllipticBit.SDLang.AspNetCore.Tests;

/// <summary>A test controller that exercises the SDLang input and output formatters.</summary>
[ApiController]
[Route("api/servers")]
public sealed class ServersController : ControllerBase
{
	[HttpPost("echo")]
	public ActionResult<Server> Echo([FromBody] Server server) => server;

	[HttpGet]
	public ActionResult<Server> Get() => new Server { Host = "controller", Port = 8080, Enabled = true };

	[HttpGet("formatted/{format?}")]
	[FormatFilter]
	public ActionResult<Server> Formatted() => new Server { Host = "formatted", Port = 443, Enabled = false };
}
