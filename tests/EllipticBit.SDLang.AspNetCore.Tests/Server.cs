namespace EllipticBit.SDLang.AspNetCore.Tests;

/// <summary>A simple model used across the SDLang ASP.NET Core integration tests.</summary>
public sealed class Server
{
	public string Host { get; set; } = "";

	public int Port { get; set; }

	public bool Enabled { get; set; }
}
