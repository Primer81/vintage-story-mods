using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class ModSystemSyncHarvestableDropsToClient : ModSystem
{
	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void AssetsFinalize(ICoreAPI api)
	{
		base.AssetsFinalize(api);
		foreach (EntityProperties etype in api.World.EntityTypes)
		{
			JsonObject[] behaviorsAsJsonObj = etype.Server.BehaviorsAsJsonObj;
			foreach (JsonObject bh in behaviorsAsJsonObj)
			{
				if (bh["code"].AsString() == "harvestable")
				{
					if (etype.Attributes == null)
					{
						etype.Attributes = new JsonObject(JToken.Parse("{}"));
					}
					etype.Attributes.Token["harvestableDrops"] = bh["drops"].Token;
				}
			}
		}
	}
}
