namespace EllipticBit.SDLang.Tests;

[TestClass]
public sealed class SmokeTests
{
	[TestMethod]
	public void Parse_SimpleTag_ProducesOneTag()
	{
		SdlDocument document = SdlDocument.Parse(SampleDocuments.SimpleTag);

		Assert.AreEqual(1, document.Tags.Count);
		Assert.AreEqual("greeting", document.Tags[0].Name);
		Assert.AreEqual("Hello, World", document.Tags[0].Value!.AsString());
	}

	[TestMethod]
	public void RoundTrip_SimpleTag_PreservesValue()
	{
		SdlDocument document = SdlDocument.Parse(SampleDocuments.SimpleTag);
		string text = document.ToSdlString();

		SdlDocument reparsed = SdlDocument.Parse(text);
		Assert.AreEqual("Hello, World", reparsed.Tags[0].Value!.AsString());
	}
}
