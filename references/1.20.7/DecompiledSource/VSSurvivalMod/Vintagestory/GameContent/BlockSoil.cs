using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class BlockSoil : BlockWithGrassOverlay
{
	protected List<AssetLocation> tallGrassCodes = new List<AssetLocation>();

	protected string[] growthStages = new string[4] { "none", "verysparse", "sparse", "normal" };

	protected string[] tallGrassGrowthStages = new string[6] { "veryshort", "short", "mediumshort", "medium", "tall", "verytall" };

	protected int growthLightLevel;

	protected string growthBlockLayer;

	protected float tallGrassGrowthChance;

	protected BlockLayerConfig blocklayerconfig;

	protected const int chunksize = 32;

	protected float growthChanceOnTick = 0.16f;

	public bool growOnlyWhereRainfallExposed;

	private GenBlockLayers genBlockLayers;

	private const int FullyGrownStage = 3;

	protected int currentStage;

	protected virtual int MaxStage => 3;

	private int GrowthStage(string stage)
	{
		return stage switch
		{
			"normal" => 3, 
			"sparse" => 2, 
			"verysparse" => 1, 
			_ => 0, 
		};
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		growthLightLevel = ((Attributes?["growthLightLevel"] != null) ? Attributes["growthLightLevel"].AsInt(7) : 7);
		growthBlockLayer = Attributes?["growthBlockLayer"]?.AsString("l1soilwithgrass");
		tallGrassGrowthChance = ((Attributes?["tallGrassGrowthChance"] != null) ? Attributes["tallGrassGrowthChance"].AsFloat(0.3f) : 0.3f);
		growthChanceOnTick = ((Attributes?["growthChanceOnTick"] != null) ? Attributes["growthChanceOnTick"].AsFloat(0.33f) : 0.33f);
		growOnlyWhereRainfallExposed = Attributes?["growOnlyWhereRainfallExposed"] != null && Attributes["growOnlyWhereRainfallExposed"].AsBool();
		tallGrassCodes.Add(new AssetLocation("tallgrass-veryshort-free"));
		tallGrassCodes.Add(new AssetLocation("tallgrass-short-free"));
		tallGrassCodes.Add(new AssetLocation("tallgrass-mediumshort-free"));
		tallGrassCodes.Add(new AssetLocation("tallgrass-medium-free"));
		tallGrassCodes.Add(new AssetLocation("tallgrass-tall-free"));
		tallGrassCodes.Add(new AssetLocation("tallgrass-verytall-free"));
		if (api.Side == EnumAppSide.Server)
		{
			(api as ICoreServerAPI).Event.ServerRunPhase(EnumServerRunPhase.RunGame, delegate
			{
				genBlockLayers = api.ModLoader.GetModSystem<GenBlockLayers>();
				blocklayerconfig = genBlockLayers.blockLayerConfig;
			});
		}
		currentStage = GrowthStage(Variant["grasscoverage"]);
	}

	public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
	{
		base.OnServerGameTick(world, pos, extra);
		GrassTick tick = extra as GrassTick;
		world.BlockAccessor.ExchangeBlock(tick.Grass.BlockId, pos);
		BlockPos upPos = pos.UpCopy();
		if (tick.TallGrass != null && world.BlockAccessor.GetBlock(upPos).BlockId == 0)
		{
			world.BlockAccessor.SetBlock(tick.TallGrass.BlockId, upPos);
		}
	}

	public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
	{
		extra = null;
		if (offThreadRandom.NextDouble() > (double)growthChanceOnTick)
		{
			return false;
		}
		if (growOnlyWhereRainfallExposed && world.BlockAccessor.GetRainMapHeightAt(pos) > pos.Y + 1)
		{
			return false;
		}
		bool isGrowing = false;
		Block grass = null;
		BlockPos upPos = pos.UpCopy();
		bool lowLightLevel = world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.MaxLight) < growthLightLevel && (world.BlockAccessor.GetLightLevel(upPos, EnumLightLevelType.MaxLight) < growthLightLevel || world.BlockAccessor.GetBlock(upPos).SideSolid[BlockFacing.DOWN.Index]);
		bool smothering = isSmotheringBlock(world, upPos);
		int overheatingAmount = 0;
		world.BlockAccessor.WalkBlocks(pos.AddCopy(-3, 0, -3), pos.AddCopy(3, 1, 3), delegate(Block block, int x, int y, int z)
		{
			if (block.Attributes != null)
			{
				overheatingAmount = Math.Max(overheatingAmount, block.Attributes["killPlantRadius"].AsInt() - Math.Max(0, (int)pos.DistanceTo(x, y, z) - 1));
			}
		});
		bool die = (overheatingAmount >= 1 && currentStage == 3) || (overheatingAmount >= 2 && currentStage == 2) || (overheatingAmount >= 3 && currentStage == 1);
		if ((lowLightLevel || smothering || die) && currentStage > 0)
		{
			grass = tryGetBlockForDying(world);
		}
		else if (overheatingAmount <= 0 && !smothering && !lowLightLevel && currentStage < MaxStage)
		{
			isGrowing = true;
			grass = tryGetBlockForGrowing(world, pos);
		}
		if (grass != null)
		{
			extra = new GrassTick
			{
				Grass = grass,
				TallGrass = (isGrowing ? getTallGrassBlock(world, upPos, offThreadRandom) : null)
			};
		}
		return extra != null;
	}

	protected bool isSmotheringBlock(IWorldAccessor world, BlockPos pos)
	{
		Block block = world.BlockAccessor.GetBlock(pos, 2);
		if (block is BlockLakeIce || block.LiquidLevel > 1)
		{
			return true;
		}
		block = world.BlockAccessor.GetBlock(pos);
		if (!block.SideSolid[BlockFacing.DOWN.Index] || !block.SideOpaque[BlockFacing.DOWN.Index])
		{
			return block is BlockLava;
		}
		return true;
	}

	protected Block tryGetBlockForGrowing(IWorldAccessor world, BlockPos pos)
	{
		ClimateCondition conds = GetClimateAt(world.BlockAccessor, pos);
		int targetStage;
		if (currentStage != MaxStage && (targetStage = getClimateSuitedGrowthStage(world, pos, conds)) != currentStage)
		{
			int nextStage = GameMath.Clamp(targetStage, currentStage - 1, currentStage + 1);
			return world.GetBlock(CodeWithParts(growthStages[nextStage]));
		}
		return null;
	}

	private ClimateCondition GetClimateAt(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (genBlockLayers == null)
		{
			return blockAccessor.GetClimateAt(pos, EnumGetClimateMode.WorldGenValues);
		}
		double rndX;
		double rndZ;
		int rndY = genBlockLayers.RandomlyAdjustPosition(pos, out rndX, out rndZ);
		int distx = (int)Math.Round(rndX, 0);
		int distz = (int)Math.Round(rndZ, 0);
		pos.Add(distx, rndY, distz);
		ClimateCondition climateAt = blockAccessor.GetClimateAt(pos, EnumGetClimateMode.WorldGenValues);
		pos.Add(-distx, -rndY, -distz);
		return climateAt;
	}

	protected Block tryGetBlockForDying(IWorldAccessor world)
	{
		int nextStage = Math.Max(currentStage - 1, 0);
		if (nextStage != currentStage)
		{
			return world.GetBlock(CodeWithParts(growthStages[nextStage]));
		}
		return null;
	}

	protected Block getTallGrassBlock(IWorldAccessor world, BlockPos abovePos, Random offthreadRandom)
	{
		if (offthreadRandom.NextDouble() > (double)tallGrassGrowthChance)
		{
			return null;
		}
		Block block = world.BlockAccessor.GetBlock(abovePos);
		int nextTallgrassStage = Math.Min(((block.FirstCodePart() == "tallgrass") ? Array.IndexOf(tallGrassGrowthStages, block.Variant["tallgrass"]) : 0) + 1 + offthreadRandom.Next(3), tallGrassGrowthStages.Length - 1);
		return world.GetBlock(tallGrassCodes[nextTallgrassStage]);
	}

	protected bool canGrassGrowHere(IWorldAccessor world, BlockPos pos)
	{
		if (currentStage != 3 && world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.MaxLight) >= growthLightLevel && !world.BlockAccessor.IsSideSolid(pos.X, pos.Y + 1, pos.Z, BlockFacing.DOWN))
		{
			return getClimateSuitedGrowthStage(world, pos, GetClimateAt(world.BlockAccessor, pos)) != currentStage;
		}
		return false;
	}

	protected int getClimateSuitedGrowthStage(IWorldAccessor world, BlockPos pos, ClimateCondition climate)
	{
		if (climate == null)
		{
			return currentStage;
		}
		IMapChunk mapchunk = world.BlockAccessor.GetMapChunkAtBlockPos(pos);
		if (mapchunk == null)
		{
			return 0;
		}
		int mapheight = ((ICoreServerAPI)world.Api).WorldManager.MapSizeY;
		float transitionSize = blocklayerconfig.blockLayerTransitionSize;
		int topblockid = mapchunk.TopRockIdMap[pos.Z % 32 * 32 + pos.X % 32];
		double posRand = (double)GameMath.MurmurHash3(pos.X, 1, pos.Z) / 2147483647.0;
		posRand = (posRand + 1.0) * (double)transitionSize;
		int posY = pos.Y + (int)(genBlockLayers.distort2dx.Noise(-pos.X, -pos.Z) / 4.0);
		for (int i = 0; i < blocklayerconfig.Blocklayers.Length; i++)
		{
			BlockLayer bl = blocklayerconfig.Blocklayers[i];
			float num = bl.CalcTrfDistance(climate.Temperature, climate.WorldgenRainfall, climate.Fertility);
			float yDist = bl.CalcYDistance(posY, mapheight);
			if ((double)(num + yDist) <= posRand)
			{
				int blockId = bl.GetBlockId(posRand, climate.Temperature, climate.WorldgenRainfall, climate.Fertility, topblockid, pos, mapheight);
				if (world.Blocks[blockId] is BlockSoil blockSoil)
				{
					return blockSoil.currentStage;
				}
			}
		}
		return 0;
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		return base.GetColor(capi, pos);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (facing == BlockFacing.UP && Variant["grasscoverage"] != "none")
		{
			return capi.World.ApplyColorMapOnRgba(ClimateColorMap, SeasonColorMap, capi.BlockTextureAtlas.GetRandomColor(Textures["specialSecondTexture"].Baked.TextureSubId, rndIndex), pos.X, pos.Y, pos.Z);
		}
		return base.GetRandomColor(capi, pos, facing, rndIndex);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		if (!Variant.ContainsKey("fertility"))
		{
			return;
		}
		string fertility = inSlot.Itemstack.Block.Variant["fertility"];
		Block farmland = world.GetBlock(new AssetLocation("farmland-dry-" + fertility));
		if (farmland != null)
		{
			int fert_value = farmland.Fertility;
			if (fert_value > 0)
			{
				dsc.Append(Lang.Get("Fertility when tilled:") + " " + fert_value + "%\n");
			}
		}
	}
}
