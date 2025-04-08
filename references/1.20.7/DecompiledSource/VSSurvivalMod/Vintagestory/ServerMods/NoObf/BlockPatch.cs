using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods.NoObf;

[JsonObject(MemberSerialization.OptIn)]
public class BlockPatch
{
	[JsonProperty]
	public AssetLocation[] blockCodes;

	[JsonProperty]
	public float Chance = 0.05f;

	[JsonProperty]
	public int MinTemp = -30;

	[JsonProperty]
	public int MaxTemp = 40;

	[JsonProperty]
	public float MinRain;

	[JsonProperty]
	public float MaxRain = 1f;

	[JsonProperty]
	public float MinForest;

	[JsonProperty]
	public float MaxForest = 1f;

	[JsonProperty]
	public float MinShrub;

	[JsonProperty]
	public float MaxShrub = 1f;

	[JsonProperty]
	public float MinFertility;

	[JsonProperty]
	public float MaxFertility = 1f;

	[JsonProperty]
	public float MinY = -0.3f;

	[JsonProperty]
	public float MaxY = 1f;

	[JsonProperty]
	public EnumBlockPatchPlacement Placement = EnumBlockPatchPlacement.OnSurface;

	[JsonProperty]
	public EnumTreeType TreeType;

	[JsonProperty]
	public NatFloat OffsetX = NatFloat.createGauss(0f, 5f);

	[JsonProperty]
	public NatFloat OffsetZ = NatFloat.createGauss(0f, 5f);

	[JsonProperty]
	public NatFloat BlockCodeIndex;

	[JsonProperty]
	public NatFloat Quantity = NatFloat.createGauss(7f, 7f);

	[JsonProperty]
	public string MapCode;

	[JsonProperty]
	public string[] RandomMapCodePool;

	[JsonProperty]
	public int MinWaterDepth;

	[JsonProperty]
	public float MinWaterDepthP;

	[JsonProperty]
	public int MaxWaterDepth;

	[JsonProperty]
	public float MaxWaterDepthP;

	[JsonProperty]
	public int MaxHeightDifferential = 8;

	[JsonProperty]
	public bool PostPass;

	[JsonProperty]
	public bool PrePass;

	[JsonProperty]
	public BlockPatchAttributes Attributes;

	public Block[] Blocks;

	public Dictionary<int, Block[]> BlocksByRockType;

	private BlockPos pos = new BlockPos();

	private BlockPos tempPos = new BlockPos();

	public void Init(ICoreServerAPI api, RockStrataConfig rockstrata, LCGRandom rnd, int i)
	{
		List<Block> blocks = new List<Block>();
		for (int j = 0; j < blockCodes.Length; j++)
		{
			AssetLocation code = blockCodes[j];
			if (code.Path.Contains("{rocktype}"))
			{
				if (BlocksByRockType == null)
				{
					BlocksByRockType = new Dictionary<int, Block[]>();
				}
				for (int k = 0; k < rockstrata.Variants.Length; k++)
				{
					string rocktype = rockstrata.Variants[k].BlockCode.Path.Split('-')[1];
					AssetLocation rocktypedCode = code.CopyWithPath(code.Path.Replace("{rocktype}", rocktype));
					Block rockBlock = api.World.GetBlock(rockstrata.Variants[k].BlockCode);
					if (rockBlock != null)
					{
						Block block = api.World.GetBlock(rocktypedCode);
						BlocksByRockType[rockBlock.BlockId] = new Block[1] { block };
					}
				}
				continue;
			}
			Block block2 = api.World.GetBlock(code);
			if (block2 != null)
			{
				blocks.Add(block2);
			}
			else if (code.Path.Contains('*'))
			{
				Block[] searchBlocks = api.World.SearchBlocks(code);
				if (searchBlocks != null)
				{
					blocks.AddRange(searchBlocks);
					continue;
				}
				api.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve block with code {1}. Will ignore.", i, code);
			}
			else
			{
				api.World.Logger.Warning("Block patch Nr. {0}: Unable to resolve block with code {1}. Will ignore.", i, code);
			}
		}
		Blocks = blocks.ToArray();
		if (BlockCodeIndex == null)
		{
			BlockCodeIndex = NatFloat.createUniform(0f, Blocks.Length);
		}
		if (RandomMapCodePool != null)
		{
			int index = rnd.NextInt(RandomMapCodePool.Length);
			MapCode = RandomMapCodePool[index];
		}
		if (Attributes != null)
		{
			Attributes.Init(api, i);
		}
		if (MinWaterDepth == 0 && MinWaterDepthP != 0f)
		{
			MinWaterDepth = (int)((float)api.World.SeaLevel * Math.Clamp(MinWaterDepthP, 0f, 1f));
		}
		if (MaxWaterDepth == 0 && MaxWaterDepthP != 0f)
		{
			MaxWaterDepth = (int)((float)api.World.SeaLevel * Math.Clamp(MaxWaterDepthP, 0f, 1f));
		}
	}

	public void Generate(IBlockAccessor blockAccessor, IRandom rnd, int posX, int posY, int posZ, int firstBlockId, bool isStoryPatch)
	{
		float quantity = Quantity.nextFloat(1f, rnd) + 1f;
		Block[] blocks = getBlocks(firstBlockId);
		if (blocks.Length == 0)
		{
			return;
		}
		ModStdWorldGen modSys = null;
		if (blockAccessor is IWorldGenBlockAccessor wgba)
		{
			modSys = wgba.WorldgenWorldAccessor.Api.ModLoader.GetModSystem<GenVegetationAndPatches>();
		}
		while (quantity-- > 0f && (!(quantity < 1f) || !(rnd.NextFloat() > quantity)))
		{
			pos.X = posX + (int)OffsetX.nextFloat(1f, rnd);
			pos.Z = posZ + (int)OffsetZ.nextFloat(1f, rnd);
			if (modSys != null && !isStoryPatch && modSys.GetIntersectingStructure(pos, ModStdWorldGen.SkipPatchesgHashCode) != null)
			{
				continue;
			}
			int index = GameMath.Mod((int)BlockCodeIndex.nextFloat(1f, rnd), blocks.Length);
			IServerChunk chunk = (IServerChunk)blockAccessor.GetChunk(pos.X / 32, 0, pos.Z / 32);
			if (chunk == null)
			{
				break;
			}
			int lx = GameMath.Mod(pos.X, 32);
			int lz = GameMath.Mod(pos.Z, 32);
			if (Placement == EnumBlockPatchPlacement.Underground)
			{
				pos.Y = rnd.NextInt(Math.Max(1, chunk.MapChunk.WorldGenTerrainHeightMap[lz * 32 + lx] - 1));
			}
			else
			{
				pos.Y = chunk.MapChunk.RainHeightMap[lz * 32 + lx] + 1;
				if (Math.Abs(pos.Y - posY) > MaxHeightDifferential || pos.Y >= blockAccessor.MapSizeY - 1)
				{
					continue;
				}
				if (Placement == EnumBlockPatchPlacement.UnderWater || Placement == EnumBlockPatchPlacement.UnderSeaWater)
				{
					tempPos.Set(pos.X, pos.Y - 2, pos.Z);
					if (!blockAccessor.GetBlock(tempPos, 2).IsLiquid())
					{
						continue;
					}
					tempPos.Y = pos.Y - GameMath.Max(1, MinWaterDepth);
					Block downBlock = blockAccessor.GetBlock(tempPos, 2);
					if ((Placement == EnumBlockPatchPlacement.UnderWater && downBlock.LiquidCode != "water") || (Placement == EnumBlockPatchPlacement.UnderSeaWater && downBlock.LiquidCode != "saltwater"))
					{
						continue;
					}
					if (MaxWaterDepth > 0)
					{
						tempPos.Set(pos.X, pos.Y - (MaxWaterDepth + 1), pos.Z);
						downBlock = blockAccessor.GetBlock(tempPos, 2);
						if ((Placement == EnumBlockPatchPlacement.UnderWater && downBlock.LiquidCode == "water") || (Placement == EnumBlockPatchPlacement.UnderSeaWater && downBlock.LiquidCode == "saltwater"))
						{
							continue;
						}
					}
				}
			}
			if (Placement == EnumBlockPatchPlacement.UnderWater || Placement == EnumBlockPatchPlacement.UnderSeaWater)
			{
				blocks[index].TryPlaceBlockForWorldGenUnderwater(blockAccessor, pos, BlockFacing.UP, rnd, MinWaterDepth, MaxWaterDepth, Attributes);
			}
			else
			{
				blocks[index].TryPlaceBlockForWorldGen(blockAccessor, pos, BlockFacing.UP, rnd, Attributes);
			}
		}
	}

	private Block[] getBlocks(int firstBlockId)
	{
		if (BlocksByRockType == null || !BlocksByRockType.TryGetValue(firstBlockId, out var blocks))
		{
			return Blocks;
		}
		return blocks;
	}
}
