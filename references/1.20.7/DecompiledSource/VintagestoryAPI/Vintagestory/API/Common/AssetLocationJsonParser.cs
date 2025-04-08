using System;
using Newtonsoft.Json;

namespace Vintagestory.API.Common;

public class AssetLocationJsonParser : JsonConverter
{
	private string domain;

	public AssetLocationJsonParser(string domain)
	{
		this.domain = domain;
	}

	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(AssetLocation);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (reader.Value is string)
		{
			return AssetLocation.Create(reader.Value as string, domain);
		}
		return null;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}
