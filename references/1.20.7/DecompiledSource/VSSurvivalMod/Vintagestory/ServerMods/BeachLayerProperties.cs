using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

[JsonObject(MemberSerialization.OptIn)]
public class BeachLayerProperties
{
	[JsonProperty]
	public float Strength;

	[JsonProperty]
	public AssetLocation BlockCode;

	public Dictionary<int, int> BlockIdMapping;

	public int BlockId;

	public void ResolveBlockIds(ICoreServerAPI api, RockStrataConfig rockstrata)
	{
		if (BlockCode != null && BlockCode.Path.Length > 0)
		{
			if (BlockCode.Path.Contains("{rocktype}"))
			{
				BlockIdMapping = new Dictionary<int, int>();
				for (int i = 0; i < rockstrata.Variants.Length; i++)
				{
					if (!rockstrata.Variants[i].IsDeposit)
					{
						string rocktype = rockstrata.Variants[i].BlockCode.Path.Split('-')[1];
						Block rockBlock = api.World.GetBlock(rockstrata.Variants[i].BlockCode);
						Block rocktypedBlock = api.World.GetBlock(BlockCode.CopyWithPath(BlockCode.Path.Replace("{rocktype}", rocktype)));
						if (rockBlock != null && rocktypedBlock != null)
						{
							BlockIdMapping[rockBlock.BlockId] = rocktypedBlock.BlockId;
						}
					}
				}
			}
			else
			{
				BlockId = api.WorldManager.GetBlockId(BlockCode);
			}
		}
		else
		{
			BlockCode = null;
		}
	}
}
