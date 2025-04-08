using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

[JsonObject(MemberSerialization.OptIn)]
public class LakeBedBlockCodeByMin
{
	[JsonProperty]
	public float MinTemp = -30f;

	[JsonProperty]
	public float MinRain;

	[JsonProperty]
	public float MaxRain = 1f;

	[JsonProperty]
	public float MinY;

	[JsonProperty]
	public float MaxY = 1f;

	[JsonProperty]
	public AssetLocation BlockCode;

	private int transSize = 3;

	public Dictionary<int, int> BlockIdMapping;

	public int BlockId;

	public bool Suitable(float temp, float rain, float yRel, LCGRandom rnd)
	{
		return Suitable(temp, rain, yRel, rnd.NextFloat());
	}

	public bool Suitable(float temp, float rain, float yRel, float rnd)
	{
		float transDistance = MinTemp - temp + (float)transSize;
		if (rain >= MinRain && rain <= MaxRain && MinY <= yRel && MaxY >= yRel)
		{
			return transDistance <= rnd * (float)transSize;
		}
		return false;
	}

	public int GetBlockForMotherRock(int rockBlockid)
	{
		int resultId = BlockId;
		BlockIdMapping?.TryGetValue(rockBlockid, out resultId);
		return resultId;
	}

	public void Init(ICoreServerAPI api, RockStrataConfig rockstrata, Random rnd)
	{
		ResolveBlockIds(api, rockstrata);
	}

	private void ResolveBlockIds(ICoreServerAPI api, RockStrataConfig rockstrata)
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
						Block typedBlock = api.World.GetBlock(BlockCode.CopyWithPath(BlockCode.Path.Replace("{rocktype}", rocktype)));
						if (rockBlock != null && typedBlock != null)
						{
							BlockIdMapping[rockBlock.BlockId] = typedBlock.BlockId;
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
