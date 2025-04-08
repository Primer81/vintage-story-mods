using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Vintagestory.API.Common;

public static class JsonUtil
{
	public static void Populate<T>(this JToken value, T target) where T : class
	{
		using JsonReader sr = value.CreateReader();
		JsonSerializer.CreateDefault().Populate(sr, target);
	}

	/// <summary>
	/// Reads a Json object, and converts it to the designated type.
	/// </summary>
	/// <typeparam name="T">The designated type</typeparam>
	/// <param name="data">The json object.</param>
	/// <returns></returns>
	public static T FromBytes<T>(byte[] data)
	{
		using MemoryStream stream = new MemoryStream(data);
		using StreamReader sr = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
		return JsonConvert.DeserializeObject<T>(sr.ReadToEnd());
	}

	public static T FromString<T>(string data)
	{
		return JsonConvert.DeserializeObject<T>(data);
	}

	/// <summary>
	/// Converts the object to json.
	/// </summary>
	/// <typeparam name="T">The type to convert</typeparam>
	/// <param name="obj">The object to convert</param>
	/// <returns></returns>
	public static byte[] ToBytes<T>(T obj)
	{
		return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
	}

	public static string ToString<T>(T obj)
	{
		return JsonConvert.SerializeObject(obj);
	}

	public static string ToPrettyString<T>(T obj)
	{
		return JsonConvert.SerializeObject(obj, Formatting.Indented);
	}

	public static void PopulateObject(object toPopulate, string text, string domain, JsonSerializerSettings settings = null)
	{
		if (domain != "game")
		{
			if (settings == null)
			{
				settings = new JsonSerializerSettings();
			}
			settings.Converters.Add(new AssetLocationJsonParser(domain));
		}
		JsonConvert.PopulateObject(text, toPopulate, settings);
	}

	public static JsonSerializer CreateSerializerForDomain(string domain, JsonSerializerSettings settings = null)
	{
		if (domain != "game")
		{
			if (settings == null)
			{
				settings = new JsonSerializerSettings();
			}
			settings.Converters.Add(new AssetLocationJsonParser(domain));
		}
		return JsonSerializer.CreateDefault(settings);
	}

	public static void PopulateObject(object toPopulate, JToken token, JsonSerializer js)
	{
		using JsonReader reader = token.CreateReader();
		js.Populate(reader, toPopulate);
	}

	/// <summary>
	/// Converts a Json object to a typed object.
	/// </summary>
	/// <typeparam name="T">The type to convert.</typeparam>
	/// <param name="text">The text to deserialize</param>
	/// <param name="domain">The domain of the text.</param>
	/// <param name="settings">The settings of the deserializer. (default: Null)</param>
	/// <returns></returns>
	public static T ToObject<T>(string text, string domain, JsonSerializerSettings settings = null)
	{
		if (domain != "game")
		{
			if (settings == null)
			{
				settings = new JsonSerializerSettings();
			}
			settings.Converters.Add(new AssetLocationJsonParser(domain));
		}
		return JsonConvert.DeserializeObject<T>(text, settings);
	}

	/// <summary>
	/// Converts a Json token to a typed object.
	/// </summary>
	/// <typeparam name="T">The type to convert.</typeparam>
	/// <param name="token">The token to deserialize</param>
	/// <param name="domain">The domain of the text.</param>
	/// <param name="settings">The settings of the deserializer. (default: Null)</param>
	/// <returns></returns>
	public static T ToObject<T>(this JToken token, string domain, JsonSerializerSettings settings = null)
	{
		if (domain != "game")
		{
			if (settings == null)
			{
				settings = new JsonSerializerSettings();
			}
			settings.Converters.Add(new AssetLocationJsonParser(domain));
		}
		return token.ToObject<T>(JsonSerializer.Create(settings));
	}
}
