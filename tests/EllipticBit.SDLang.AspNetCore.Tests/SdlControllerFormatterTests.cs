using System.Net;
using System.Text;
using EllipticBit.SDLang.AspNetCore;
using EllipticBit.SDLang.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EllipticBit.SDLang.AspNetCore.Tests;

[TestClass]
public sealed class SdlControllerFormatterTests
{
	[TestMethod]
	public async Task Post_Sdl_RoundTrips_Through_Controller()
	{
		await using SdlTestApp app = await SdlTestApp.StartAsync();
		Server expected = new() { Host = "db.local", Port = 5432, Enabled = true };
		string sdl = SdlSerializer.Serialize(expected);

		using HttpRequestMessage request = new(HttpMethod.Post, "/api/servers/echo")
		{
			Content = new StringContent(sdl, Encoding.UTF8, SdlMediaTypeNames.Application),
		};
		request.Headers.Accept.ParseAdd(SdlMediaTypeNames.Application);

		using HttpResponseMessage response = await app.Client.SendAsync(request);

		response.EnsureSuccessStatusCode();
		Assert.AreEqual(SdlMediaTypeNames.Application, response.Content.Headers.ContentType?.MediaType);

		Server? actual = SdlSerializer.Deserialize<Server>(await response.Content.ReadAsStringAsync());
		Assert.IsNotNull(actual);
		Assert.AreEqual(expected.Host, actual!.Host);
		Assert.AreEqual(expected.Port, actual.Port);
		Assert.AreEqual(expected.Enabled, actual.Enabled);
	}

	[TestMethod]
	public async Task Get_Negotiates_Sdl_Via_Accept_Header()
	{
		await using SdlTestApp app = await SdlTestApp.StartAsync();

		using HttpRequestMessage request = new(HttpMethod.Get, "/api/servers");
		request.Headers.Accept.ParseAdd(SdlMediaTypeNames.Application);

		using HttpResponseMessage response = await app.Client.SendAsync(request);

		response.EnsureSuccessStatusCode();
		Assert.AreEqual(SdlMediaTypeNames.Application, response.Content.Headers.ContentType?.MediaType);

		Server? server = SdlSerializer.Deserialize<Server>(await response.Content.ReadAsStringAsync());
		Assert.IsNotNull(server);
		Assert.AreEqual("controller", server!.Host);
	}

	[TestMethod]
	public async Task Get_Maps_Sdl_File_Extension_Format()
	{
		await using SdlTestApp app = await SdlTestApp.StartAsync();

		using HttpResponseMessage response = await app.Client.GetAsync("/api/servers/formatted/sdl");

		response.EnsureSuccessStatusCode();
		Assert.AreEqual(SdlMediaTypeNames.Application, response.Content.Headers.ContentType?.MediaType);

		Server? server = SdlSerializer.Deserialize<Server>(await response.Content.ReadAsStringAsync());
		Assert.IsNotNull(server);
		Assert.AreEqual("formatted", server!.Host);
	}

	[TestMethod]
	public async Task Post_Malformed_Sdl_Returns_BadRequest()
	{
		await using SdlTestApp app = await SdlTestApp.StartAsync();

		using HttpRequestMessage request = new(HttpMethod.Post, "/api/servers/echo")
		{
			Content = new StringContent("server \"unterminated", Encoding.UTF8, SdlMediaTypeNames.Application),
		};

		using HttpResponseMessage response = await app.Client.SendAsync(request);

		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
	}
}
