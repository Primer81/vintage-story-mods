using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

public class JsonAttributesConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(JsonObject);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		return new JsonObject(JToken.ReadFrom(reader));
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		(value as JsonObject).Token.WriteTo(writer);
	}
}
