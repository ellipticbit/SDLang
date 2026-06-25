using System.Net;
using System.Text;
using EllipticBit.SDLang.AspNetCore;
using EllipticBit.SDLang.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EllipticBit.SDLang.AspNetCore.Tests;

[TestClass]
public sealed class SdlMinimalApiTests
{
	[TestMethod]
	public async Task Post_Binds_Sdl_Body_And_RoundTrips()
	{
		await using SdlTestApp app = await SdlTestApp.StartAsync();
		Server expected = new() { Host = "minimal.local", Port = 9000, Enabled = false };
		string sdl = SdlSerializer.Serialize(expected);

		using HttpRequestMessage request = new(HttpMethod.Post, "/minimal/echo")
		{
			Content = new StringContent(sdl, Encoding.UTF8, SdlMediaTypeNames.Application),
		};

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
	public async Task Get_Writes_Sdl_Result()
	{
		await using SdlTestApp app = await SdlTestApp.StartAsync();

		using HttpResponseMessage response = await app.Client.GetAsync("/minimal/server");

		response.EnsureSuccessStatusCode();
		Assert.AreEqual(SdlMediaTypeNames.Application, response.Content.Headers.ContentType?.MediaType);

		Server? server = SdlSerializer.Deserialize<Server>(await response.Content.ReadAsStringAsync());
		Assert.IsNotNull(server);
		Assert.AreEqual("minimal", server!.Host);
	}

	[TestMethod]
	public async Task Post_Malformed_Sdl_Body_Yields_BadRequest()
	{
		await using SdlTestApp app = await SdlTestApp.StartAsync();

		using HttpRequestMessage request = new(HttpMethod.Post, "/minimal/echo")
		{
			Content = new StringContent("server \"unterminated", Encoding.UTF8, SdlMediaTypeNames.Application),
		};

		try
		{
			using HttpResponseMessage response = await app.Client.SendAsync(request);
			Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		}
		catch (BadHttpRequestException ex)
		{
			Assert.AreEqual(StatusCodes.Status400BadRequest, ex.StatusCode);
		}
	}
}
