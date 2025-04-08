using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class BlockBituCoal : BlockOre
{
	private Block clay;

	private static RockStrataConfig rockStrata;

	private static LCGRandom rand;

	private const int chunksize = 32;

	private static int regionChunkSize;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		clay = api.World.BlockAccessor.GetBlock(new AssetLocation("rawclay-fire-none"));
		if (rockStrata == null && api is ICoreServerAPI sapi)
		{
			regionChunkSize = sapi.WorldManager.RegionSize / 32;
			rockStrata = BlockLayerConfig.GetInstance(sapi).RockStrata;
			rand = new LCGRandom(api.World.Seed);
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		rockStrata = null;
		clay = null;
		rand = null;
	}

	public float GetDepositDistortTop(BlockPos pos, int lx, int lz, IMapChunk heremapchunk)
	{
		int rdx = pos.X / 32 % regionChunkSize;
		int rdz = pos.Z / 32 % regionChunkSize;
		IMapRegion mapRegion = heremapchunk.MapRegion;
		float step = (float)heremapchunk.MapRegion.OreMapVerticalDistortTop.InnerSize / (float)regionChunkSize;
		return mapRegion.OreMapVerticalDistortTop.GetIntLerpedCorrectly((float)rdx * step + step * ((float)lx / 32f), (float)rdz * step + step * ((float)lz / 32f));
	}

	public float GetDepositDistortBot(BlockPos pos, int lx, int lz, IMapChunk heremapchunk)
	{
		int rdx = pos.X / 32 % regionChunkSize;
		int rdz = pos.Z / 32 % regionChunkSize;
		IMapRegion mapRegion = heremapchunk.MapRegion;
		float step = (float)heremapchunk.MapRegion.OreMapVerticalDistortBottom.InnerSize / (float)regionChunkSize;
		return mapRegion.OreMapVerticalDistortBottom.GetIntLerpedCorrectly((float)rdx * step + step * ((float)lx / 32f), (float)rdz * step + step * ((float)lz / 32f));
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRandom, BlockPatchAttributes attributes = null)
	{
		IMapChunk mapChunk = blockAccessor.GetMapChunk(pos.X / 32, pos.Z / 32);
		int posX = pos.X % 32;
		int posZ = pos.Z % 32;
		int extraDistX = (int)GetDepositDistortTop(pos, posX, posZ, mapChunk) / 7;
		int extraDistZ = (int)GetDepositDistortBot(pos, posX, posZ, mapChunk) / 7;
		rand.InitPositionSeed(pos.X / 100 + extraDistX, pos.Z / 100 + extraDistZ);
		BlockPos beloPos = pos.DownCopy();
		Block blockBelow = blockAccessor.GetBlock(beloPos);
		for (int i = 0; i < rockStrata.Variants.Length; i++)
		{
			if (rockStrata.Variants[i].RockGroup == EnumRockGroup.Sedimentary && rockStrata.Variants[i].BlockCode == blockBelow.Code)
			{
				if (rand.NextDouble() > 0.6)
				{
					blockAccessor.SetBlock(clay.BlockId, beloPos);
				}
				break;
			}
		}
		blockAccessor.SetBlock(BlockId, pos);
		return base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldgenRandom, attributes);
	}
}
