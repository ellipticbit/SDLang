using System.Text;
using EllipticBit.SDLang.Serialization;

namespace EllipticBit.SDLang.Tests;

[TestClass]
public sealed class SerializerTests
{
	[TestMethod]
	public void Serialize_Poco_ProducesAttributes()
	{
		Server server = new() { Host = "example.com", Port = 8080, Enabled = true };
		string sdl = SdlSerializer.Serialize(server);

		Assert.IsTrue(sdl.Contains("Host=\"example.com\"", StringComparison.Ordinal), sdl);
		Assert.IsTrue(sdl.Contains("Port=8080", StringComparison.Ordinal), sdl);
		Assert.IsTrue(sdl.Contains("Enabled=true", StringComparison.Ordinal), sdl);
	}

	[TestMethod]
	public void RoundTrip_Poco()
	{
		Server server = new() { Host = "db.local", Port = 5432, Enabled = false };
		string sdl = SdlSerializer.Serialize(server);
		Server? result = SdlSerializer.Deserialize<Server>(sdl);

		Assert.IsNotNull(result);
		Assert.AreEqual("db.local", result.Host);
		Assert.AreEqual(5432, result.Port);
		Assert.IsFalse(result.Enabled);
	}

	[TestMethod]
	public void Serialize_ValueAndChild_Roles()
	{
		Person person = new()
		{
			Name = "Alice",
			Age = 30,
			Home = new Address { Street = "1 Main St", City = "Springfield" },
		};

		string sdl = SdlSerializer.Serialize(person);
		Person? result = SdlSerializer.Deserialize<Person>(sdl);

		Assert.IsNotNull(result);
		Assert.AreEqual("Alice", result.Name);
		Assert.AreEqual(30, result.Age);
		Assert.IsNotNull(result.Home);
		Assert.AreEqual("1 Main St", result.Home.Street);
		Assert.AreEqual("Springfield", result.Home.City);
	}

	[TestMethod]
	public void RoundTrip_Record_WithParameterizedConstructor()
	{
		Point point = new(3, 7);
		string sdl = SdlSerializer.Serialize(point);
		Point? result = SdlSerializer.Deserialize<Point>(sdl);

		Assert.IsNotNull(result);
		Assert.AreEqual(3, result.X);
		Assert.AreEqual(7, result.Y);
	}

	[TestMethod]
	public void Ignore_AlwaysAndWhenWritingNull()
	{
		WithIgnore model = new() { Kept = "yes", Optional = null };
		string sdl = SdlSerializer.Serialize(model);

		Assert.IsTrue(sdl.Contains("kept", StringComparison.OrdinalIgnoreCase), sdl);
		Assert.IsFalse(sdl.Contains("secret", StringComparison.Ordinal), sdl);
		Assert.IsFalse(sdl.Contains("optional", StringComparison.OrdinalIgnoreCase), sdl);
	}

	[TestMethod]
	public void PropertyOrder_IsRespected()
	{
		WithOrder model = new() { First = 1, Second = 2 };
		string sdl = SdlSerializer.Serialize(model);

		int firstIndex = sdl.IndexOf("first", StringComparison.OrdinalIgnoreCase);
		int secondIndex = sdl.IndexOf("second", StringComparison.OrdinalIgnoreCase);
		Assert.IsTrue(firstIndex >= 0 && secondIndex >= 0 && firstIndex < secondIndex, sdl);
	}

	[TestMethod]
	public void NamingPolicy_CamelCase()
	{
		SdlSerializerOptions options = new() { PropertyNamingPolicy = SdlNamingPolicy.CamelCase };
		NamingModel model = new() { FirstName = "Bob", ItemCount = 5 };
		string sdl = SdlSerializer.Serialize(model, options);

		Assert.IsTrue(sdl.Contains("firstName", StringComparison.Ordinal), sdl);
		Assert.IsTrue(sdl.Contains("itemCount", StringComparison.Ordinal), sdl);
	}

	[TestMethod]
	public void NamingPolicy_SnakeCase()
	{
		SdlSerializerOptions options = new() { PropertyNamingPolicy = SdlNamingPolicy.SnakeCaseLower };
		NamingModel model = new() { FirstName = "Bob", ItemCount = 5 };
		string sdl = SdlSerializer.Serialize(model, options);

		Assert.IsTrue(sdl.Contains("first_name", StringComparison.Ordinal), sdl);
		Assert.IsTrue(sdl.Contains("item_count", StringComparison.Ordinal), sdl);
	}

	[TestMethod]
	public void CaseInsensitive_Deserialization()
	{
		SdlSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
		Server? result = SdlSerializer.Deserialize<Server>("Server HOST=\"x\" PORT=1\n", options);

		Assert.IsNotNull(result);
		Assert.AreEqual("x", result.Host);
		Assert.AreEqual(1, result.Port);
	}

	[TestMethod]
	public void WriteIndented_Option_AffectsOutput()
	{
		SdlSerializerOptions options = new() { WriteIndented = true };
		Person person = new() { Name = "A", Age = 1, Home = new Address { Street = "s", City = "c" } };
		string sdl = SdlSerializer.Serialize(person, options);

		Assert.IsTrue(sdl.Contains('{', StringComparison.Ordinal), sdl);
	}

	[TestMethod]
	public void ExtensionData_RoundTrips()
	{
		WithExtensionData? result = SdlSerializer.Deserialize<WithExtensionData>(
			"WithExtensionData known=\"k\" extra1=\"v1\" extra2=42\n");

		Assert.IsNotNull(result);
		Assert.AreEqual("k", result.Known);
		Assert.IsNotNull(result.Extra);
		Assert.IsTrue(result.Extra.ContainsKey("extra1"));
		Assert.IsTrue(result.Extra.ContainsKey("extra2"));
	}

	[TestMethod]
	public void Enum_SerializesByName()
	{
		WithEnum model = new() { Color = Color.Green };
		string sdl = SdlSerializer.Serialize(model);
		Assert.IsTrue(sdl.Contains("Green", StringComparison.Ordinal), sdl);

		WithEnum? result = SdlSerializer.Deserialize<WithEnum>(sdl);
		Assert.IsNotNull(result);
		Assert.AreEqual(Color.Green, result.Color);
	}

	[TestMethod]
	public void Collection_AsRepeatedChildTags()
	{
		Catalog catalog = new() { Items = ["a", "b", "c"] };
		string sdl = SdlSerializer.Serialize(catalog);

		Catalog? result = SdlSerializer.Deserialize<Catalog>(sdl);
		Assert.IsNotNull(result);
		CollectionAssert.AreEqual(new[] { "a", "b", "c" }, result.Items);
	}

	[TestMethod]
	public void TopLevel_List_RoundTrips()
	{
		List<int> numbers = [1, 2, 3, 4];
		string sdl = SdlSerializer.Serialize(numbers);

		List<int>? result = SdlSerializer.Deserialize<List<int>>(sdl);
		Assert.IsNotNull(result);
		CollectionAssert.AreEqual(numbers, result);
	}

	[TestMethod]
	public void Dictionary_RoundTrips()
	{
		Dictionary<string, int> map = new() { ["one"] = 1, ["two"] = 2 };
		string sdl = SdlSerializer.Serialize(map);

		Dictionary<string, int>? result = SdlSerializer.Deserialize<Dictionary<string, int>>(sdl);
		Assert.IsNotNull(result);
		Assert.AreEqual(1, result["one"]);
		Assert.AreEqual(2, result["two"]);
	}

	[TestMethod]
	public void ReferenceHandler_Preserve_RoundTripsSharedReference()
	{
		SdlSerializerOptions options = new() { ReferenceHandler = SdlReferenceHandler.Preserve };
		Node head = new() { Name = "head" };
		head.Next = head;

		string sdl = SdlSerializer.Serialize(head, options);
		Assert.IsTrue(sdl.Contains("$id", StringComparison.Ordinal), sdl);
		Assert.IsTrue(sdl.Contains("$ref", StringComparison.Ordinal), sdl);

		Node? result = SdlSerializer.Deserialize<Node>(sdl, options);
		Assert.IsNotNull(result);
		Assert.AreSame(result, result.Next);
	}

	[TestMethod]
	public void ReferenceHandler_IgnoreCycles_BreaksCycle()
	{
		SdlSerializerOptions options = new() { ReferenceHandler = SdlReferenceHandler.IgnoreCycles };
		Node head = new() { Name = "head" };
		head.Next = head;

		string sdl = SdlSerializer.Serialize(head, options);
		Assert.IsNotNull(sdl);
	}

	[TestMethod]
	public void Cycle_WithoutHandler_Throws()
	{
		Node head = new() { Name = "head" };
		head.Next = head;

		Assert.ThrowsException<SdlException>(() => SdlSerializer.Serialize(head));
	}

	[TestMethod]
	public void CustomConverter_ViaAttribute()
	{
		ConvertedTemperature temp = new(21.5);
		string sdl = SdlSerializer.Serialize(temp);

		ConvertedTemperature? result = SdlSerializer.Deserialize<ConvertedTemperature>(sdl);
		Assert.IsNotNull(result);
		Assert.AreEqual(21.5, result.Celsius, 0.001);
	}

	[TestMethod]
	public async Task Async_Serialize_And_Deserialize_Stream()
	{
		Server server = new() { Host = "async.host", Port = 9090, Enabled = true };

		using MemoryStream stream = new();
		await SdlSerializer.SerializeAsync(stream, server);
		stream.Position = 0;

		Server? result = await SdlSerializer.DeserializeAsync<Server>(stream);
		Assert.IsNotNull(result);
		Assert.AreEqual("async.host", result.Host);
		Assert.AreEqual(9090, result.Port);
	}

	[TestMethod]
	public void Serialize_To_Utf8Bytes()
	{
		Server server = new() { Host = "h", Port = 1, Enabled = false };
		byte[] bytes = SdlSerializer.SerializeToUtf8Bytes(server);

		Server? result = SdlSerializer.Deserialize<Server>(bytes);
		Assert.IsNotNull(result);
		Assert.AreEqual("h", result.Host);
	}

	[TestMethod]
	public void Options_BecomeReadOnly_AfterUse()
	{
		SdlSerializerOptions options = new() { WriteIndented = false };
		_ = SdlSerializer.Serialize(new Server(), options);

		Assert.IsTrue(options.IsReadOnly);
		Assert.ThrowsException<InvalidOperationException>(() => options.WriteIndented = true);
	}
}
