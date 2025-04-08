using System;
using Newtonsoft.Json.Linq;

namespace Vintagestory.API.Datastructures;

internal class JsonObject_ReadOnly : JsonObject
{
	public override JToken Token
	{
		get
		{
			return base.Token;
		}
		set
		{
			throw new Exception("Modifying a JsonObject once it has become read-only is not allowed, sorry.  Mods should DeepClone the JsonObject first");
		}
	}

	public JsonObject_ReadOnly(JsonObject original)
		: base(original, unused: false)
	{
	}

	public override void FillPlaceHolder(string key, string value)
	{
		throw new Exception("Modifying a JsonObject once it has become read-only is not allowed, sorry.  Mods should DeepClone the JsonObject first");
	}
}
