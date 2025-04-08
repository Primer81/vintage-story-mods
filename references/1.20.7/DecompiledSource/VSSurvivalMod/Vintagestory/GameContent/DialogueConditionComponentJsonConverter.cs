using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class DialogueConditionComponentJsonConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(ConditionElement);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		JsonObject jobj = new JsonObject(JToken.ReadFrom(reader));
		bool invert = !jobj["isvalue"].Exists && jobj["isnotvalue"].Exists;
		return new ConditionElement
		{
			Variable = jobj["variable"].AsString(),
			IsValue = (invert ? jobj["isnotvalue"].AsString() : jobj["isvalue"].AsString()),
			Invert = invert
		};
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
	}
}
