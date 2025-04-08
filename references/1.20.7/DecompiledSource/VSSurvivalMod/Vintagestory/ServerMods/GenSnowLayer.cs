using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenSnowLayer : ModStdWorldGen
{
	private ICoreServerAPI api;

	private Random rnd;

	private int worldheight;

	private IWorldGenBlockAccessor blockAccessor;

	private BlockLayerConfig blockLayerConfig;

	private int transSize;

	private int maxTemp;

	private int minTemp;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.0;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.InitWorldGenerator(initWorldGen, "standard");
			api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.NeighbourSunLightFlood, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
		}
	}

	private void initWorldGen()
	{
		LoadGlobalConfig(api);
		IAsset asset = api.Assets.Get("worldgen/blocklayers.json");
		blockLayerConfig = asset.ToObject<BlockLayerConfig>();
		blockLayerConfig.SnowLayer.BlockId = api.WorldManager.GetBlockId(blockLayerConfig.SnowLayer.BlockCode);
		rnd = new Random(api.WorldManager.Seed);
		worldheight = api.WorldManager.MapSizeY;
		transSize = blockLayerConfig.SnowLayer.TransitionSize;
		maxTemp = blockLayerConfig.SnowLayer.MaxTemp;
		minTemp = maxTemp - transSize;
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		blockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: true);
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		blockAccessor.BeginColumn();
		IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
		ushort[] heightMap = chunks[0].MapChunk.RainHeightMap;
		int regionChunkSize = api.WorldManager.RegionSize / 32;
		int rlX = chunkX % regionChunkSize;
		int rlZ = chunkZ % regionChunkSize;
		float facC = (float)climateMap.InnerSize / (float)regionChunkSize;
		int climateUpLeft = climateMap.GetUnpaddedInt((int)((float)rlX * facC), (int)((float)rlZ * facC));
		int climateUpRight = climateMap.GetUnpaddedInt((int)((float)rlX * facC + facC), (int)((float)rlZ * facC));
		int climateBotLeft = climateMap.GetUnpaddedInt((int)((float)rlX * facC), (int)((float)rlZ * facC + facC));
		int climateBotRight = climateMap.GetUnpaddedInt((int)((float)rlX * facC + facC), (int)((float)rlZ * facC + facC));
		for (int x = 0; x < 32; x++)
		{
			for (int z = 0; z < 32; z++)
			{
				int posY = heightMap[z * 32 + x];
				float temp = Climate.GetScaledAdjustedTemperatureFloat((GameMath.BiLerpRgbColor((float)x / 32f, (float)z / 32f, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight) >> 16) & 0xFF, posY - TerraGenConfig.seaLevel);
				int prevY = posY;
				if (PlaceSnowLayer(x, prevY, z, chunks, temp))
				{
					heightMap[z * 32 + x]++;
				}
			}
		}
	}

	private bool PlaceSnowLayer(int lx, int posY, int lz, IServerChunk[] chunks, float temp)
	{
		float transDistance = temp - (float)minTemp;
		if (temp > (float)maxTemp)
		{
			return false;
		}
		if ((double)transDistance > rnd.NextDouble() * (double)transSize)
		{
			return false;
		}
		while (posY < worldheight - 1 && chunks[(posY + 1) / 32].Data.GetBlockIdUnsafe((32 * ((posY + 1) % 32) + lz) * 32 + lx) != 0)
		{
			posY++;
		}
		if (posY >= worldheight - 1)
		{
			return false;
		}
		int index3d = (32 * (posY % 32) + lz) * 32 + lx;
		IServerChunk chunk = chunks[posY / 32];
		int blockId = chunk.Data.GetFluid(index3d);
		if (blockId == 0)
		{
			blockId = chunk.Data.GetBlockIdUnsafe(index3d);
		}
		if (api.World.Blocks[blockId].SideSolid[BlockFacing.UP.Index])
		{
			chunks[(posY + 1) / 32].Data[(32 * ((posY + 1) % 32) + lz) * 32 + lx] = blockLayerConfig.SnowLayer.BlockId;
			return true;
		}
		return false;
	}
}
