namespace EllipticBit.SDLang.Tests;

[TestClass]
public sealed class DomTests
{
	[TestMethod]
	public void Parses_Attributes_Into_Tag()
	{
		SdlDocument document = SdlDocument.Parse(SampleDocuments.TagWithAttributes);
		Tag person = document.Tags[0];

		Assert.AreEqual("person", person.Name);
		Assert.AreEqual("Alice", person.Attributes["name"].AsString());
		Assert.AreEqual(42, person.Attributes["age"].AsInt32());
		Assert.IsTrue(person.Attributes["active"].AsBoolean());
	}

	[TestMethod]
	public void Parses_NestedChildren()
	{
		SdlDocument document = SdlDocument.Parse(SampleDocuments.NestedChildren);
		Tag matrix = document.Tags[0];

		Assert.AreEqual("matrix", matrix.Name);
		Assert.AreEqual(2, matrix.Children.Count);
		Assert.AreEqual(3, matrix.Children[0].Values.Count);
		Assert.AreEqual(1, matrix.Children[0].Values[0].AsInt32());
		Assert.AreEqual(6, matrix.Children[1].Values[2].AsInt32());
	}

	[TestMethod]
	public void Parses_Namespaces()
	{
		SdlDocument document = SdlDocument.Parse(SampleDocuments.Namespaced);
		Tag config = document.Tags[0];

		Assert.AreEqual("ns", config.Namespace);
		Assert.AreEqual("config", config.Name);
		Assert.AreEqual("ns", config.Children[0].Namespace);
	}

	[TestMethod]
	public void Parses_MultipleTopLevel()
	{
		SdlDocument document = SdlDocument.Parse(SampleDocuments.MultipleTopLevel);

		Assert.AreEqual(3, document.Tags.Count);
		Assert.AreEqual("first", document.Tags[0].Name);
		Assert.AreEqual("third", document.Tags[2].Name);
	}

	[TestMethod]
	public void Mutates_Tag_And_Serializes()
	{
		SdlDocument document = new();
		Tag tag = document.AddTag("server");
		tag.SetAttribute("host", "localhost");
		tag.SetAttribute("port", 8080);
		Tag child = tag.AddChild("endpoint");
		child.AddValue("/api");

		string sdl = document.ToSdlString();
		SdlDocument reparsed = SdlDocument.Parse(sdl);

		Assert.AreEqual("localhost", reparsed.Tags[0].Attributes["host"].AsString());
		Assert.AreEqual(8080, reparsed.Tags[0].Attributes["port"].AsInt32());
		Assert.AreEqual("/api", reparsed.Tags[0].Children[0].Value!.AsString());
	}

	[TestMethod]
	public void Child_BackPointer_Maintained()
	{
		SdlDocument document = new();
		Tag parent = document.AddTag("parent");
		Tag child = parent.AddChild("child");

		Assert.AreSame(parent, child.Parent);

		parent.Children.Remove(child);
		Assert.IsNull(child.Parent);
	}

	[TestMethod]
	public void RoundTrips_AllValueKinds()
	{
		SdlDocument document = SdlDocument.Parse(SampleDocuments.AllValueKinds);
		Tag values = document.Tags[0];

		Assert.AreEqual(SdlValueKind.String, values.Values[0].Kind);
		Assert.AreEqual(SdlValueKind.Char, values.Values[1].Kind);
		Assert.AreEqual(SdlValueKind.Int32, values.Values[2].Kind);
		Assert.AreEqual(SdlValueKind.Int64, values.Values[3].Kind);
		Assert.AreEqual(SdlValueKind.Single, values.Values[4].Kind);
		Assert.AreEqual(SdlValueKind.Double, values.Values[5].Kind);
		Assert.AreEqual(SdlValueKind.Decimal, values.Values[6].Kind);
		Assert.AreEqual(SdlValueKind.Boolean, values.Values[7].Kind);
		Assert.AreEqual(SdlValueKind.Null, values.Values[8].Kind);
		Assert.AreEqual(SdlValueKind.Date, values.Values[9].Kind);
		Assert.AreEqual(SdlValueKind.DateTime, values.Values[10].Kind);
		Assert.AreEqual(SdlValueKind.Binary, values.Values[11].Kind);

		string sdl = document.ToSdlString();
		SdlDocument reparsed = SdlDocument.Parse(sdl);
		Assert.AreEqual(12, reparsed.Tags[0].Values.Count);
	}

	[TestMethod]
	public void WriteIndented_Produces_Braces_And_Indentation()
	{
		SdlDocument document = SdlDocument.Parse(SampleDocuments.NestedChildren);
		string indented = document.ToSdlString(new SdlWriterOptions { Indented = true });

		Assert.IsTrue(indented.Contains("{\n", StringComparison.Ordinal) || indented.Contains("{\r\n", StringComparison.Ordinal), indented);
		Assert.IsTrue(indented.Contains("  row", StringComparison.Ordinal), indented);
	}
}
