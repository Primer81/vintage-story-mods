using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenPonds : ModStdWorldGen
{
	private ICoreServerAPI api;

	private LCGRandom rand;

	private int mapheight;

	private IWorldGenBlockAccessor blockAccessor;

	private readonly QueueOfInt searchPositionsDeltas = new QueueOfInt();

	private readonly QueueOfInt pondPositions = new QueueOfInt();

	private int searchSize;

	private int mapOffset;

	private int minBoundary;

	private int maxBoundary;

	private int climateUpLeft;

	private int climateUpRight;

	private int climateBotLeft;

	private int climateBotRight;

	private int[] didCheckPosition;

	private int iteration;

	private LakeBedLayerProperties lakebedLayerConfig;

	public override double ExecuteOrder()
	{
		return 0.4;
	}

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.InitWorldGenerator(initWorldGen, "standard");
			api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.TerrainFeatures, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
		}
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		blockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: true);
	}

	public void initWorldGen()
	{
		LoadGlobalConfig(api);
		rand = new LCGRandom(api.WorldManager.Seed - 12);
		searchSize = 96;
		mapOffset = 32;
		minBoundary = -31;
		maxBoundary = 63;
		mapheight = api.WorldManager.MapSizeY;
		didCheckPosition = new int[searchSize * searchSize];
		BlockLayerConfig blockLayerConfig = BlockLayerConfig.GetInstance(api);
		lakebedLayerConfig = blockLayerConfig.LakeBedLayer;
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		if (GetIntersectingStructure(chunkX * 32 + 16, chunkZ * 32 + 16, ModStdWorldGen.SkipPondgHashCode) != null)
		{
			return;
		}
		blockAccessor.BeginColumn();
		LCGRandom rand = this.rand;
		rand.InitPositionSeed(chunkX, chunkZ);
		int maxHeight = mapheight - 1;
		ushort[] heightmap = chunks[0].MapChunk.RainHeightMap;
		IMapChunk mc = chunks[0].MapChunk;
		IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
		int regionChunkSize = api.WorldManager.RegionSize / 32;
		float fac = (float)climateMap.InnerSize / (float)regionChunkSize;
		int rlX = chunkX % regionChunkSize;
		int rlZ = chunkZ % regionChunkSize;
		climateUpLeft = climateMap.GetUnpaddedInt((int)((float)rlX * fac), (int)((float)rlZ * fac));
		climateUpRight = climateMap.GetUnpaddedInt((int)((float)rlX * fac + fac), (int)((float)rlZ * fac));
		climateBotLeft = climateMap.GetUnpaddedInt((int)((float)rlX * fac), (int)((float)rlZ * fac + fac));
		climateBotRight = climateMap.GetUnpaddedInt((int)((float)rlX * fac + fac), (int)((float)rlZ * fac + fac));
		int num = GameMath.BiLerpRgbColor(0.5f, 0.5f, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
		int rain = (num >> 8) & 0xFF;
		int temp = (num >> 16) & 0xFF;
		float num2 = Math.Max(0f, (float)(4 * (rain - 10)) / 255f);
		float sealeveltemp = Climate.GetScaledAdjustedTemperatureFloat(temp, 0);
		float maxTries = (num2 - Math.Max(0f, 5f - sealeveltemp)) * 10f;
		while (maxTries-- > 0f && (!(maxTries < 1f) || !(rand.NextFloat() > maxTries)))
		{
			int dx = rand.NextInt(32);
			int dz = rand.NextInt(32);
			int pondYPos = heightmap[dz * 32 + dx] + 1;
			if (pondYPos <= 0 || pondYPos >= maxHeight)
			{
				return;
			}
			TryPlacePondAt(dx, pondYPos, dz, chunkX, chunkZ);
		}
		int iMaxTries = 600;
		while (iMaxTries-- > 0)
		{
			int dx = rand.NextInt(32);
			int dz = rand.NextInt(32);
			int pondYPos = (int)(rand.NextFloat() * (float)(mc.WorldGenTerrainHeightMap[dz * 32 + dx] - 1));
			if (pondYPos <= 0 || pondYPos >= maxHeight)
			{
				break;
			}
			int chunkY = pondYPos / 32;
			int dy = pondYPos % 32;
			int blockID = chunks[chunkY].Data.GetBlockIdUnsafe((dy * 32 + dz) * 32 + dx);
			while (blockID == 0 && pondYPos > 20)
			{
				pondYPos--;
				chunkY = pondYPos / 32;
				dy = pondYPos % 32;
				blockID = chunks[chunkY].Data.GetBlockIdUnsafe((dy * 32 + dz) * 32 + dx);
				if (blockID != 0)
				{
					TryPlacePondAt(dx, pondYPos, dz, chunkX, chunkZ);
				}
			}
		}
	}

	public void TryPlacePondAt(int dx, int pondYPos, int dz, int chunkX, int chunkZ, int depth = 0)
	{
		int mapOffset = this.mapOffset;
		int searchSize = this.searchSize;
		int minBoundary = this.minBoundary;
		int maxBoundary = this.maxBoundary;
		int waterID = GlobalConfig.waterBlockId;
		searchPositionsDeltas.Clear();
		pondPositions.Clear();
		int basePosX = chunkX * 32;
		int basePosZ = chunkZ * 32;
		Vec2i tmp = new Vec2i();
		int arrayIndex = (dz + mapOffset) * searchSize + dx + mapOffset;
		searchPositionsDeltas.Enqueue(arrayIndex);
		pondPositions.Enqueue(arrayIndex);
		int iteration = ++this.iteration;
		didCheckPosition[arrayIndex] = iteration;
		BlockPos tmpPos = new BlockPos();
		while (searchPositionsDeltas.Count > 0)
		{
			int num = searchPositionsDeltas.Dequeue();
			int px2 = num % searchSize - mapOffset;
			int pz = num / searchSize - mapOffset;
			BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
			foreach (BlockFacing facing in hORIZONTALS)
			{
				Vec3i facingNormal = facing.Normali;
				int ndx = px2 + facingNormal.X;
				int ndz = pz + facingNormal.Z;
				arrayIndex = (ndz + mapOffset) * searchSize + ndx + mapOffset;
				if (didCheckPosition[arrayIndex] != iteration)
				{
					didCheckPosition[arrayIndex] = iteration;
					tmp.Set(basePosX + ndx, basePosZ + ndz);
					tmpPos.Set(tmp.X, pondYPos - 1, tmp.Y);
					Block belowBlock = blockAccessor.GetBlock(tmpPos);
					if (ndx <= minBoundary || ndz <= minBoundary || ndx >= maxBoundary || ndz >= maxBoundary || (!((double)belowBlock.GetLiquidBarrierHeightOnSide(BlockFacing.UP, tmpPos) >= 1.0) && belowBlock.BlockId != waterID))
					{
						pondPositions.Clear();
						searchPositionsDeltas.Clear();
						return;
					}
					tmpPos.Set(tmp.X, pondYPos, tmp.Y);
					if ((double)blockAccessor.GetBlock(tmpPos).GetLiquidBarrierHeightOnSide(facing.Opposite, tmpPos) < 0.9)
					{
						searchPositionsDeltas.Enqueue(arrayIndex);
						pondPositions.Enqueue(arrayIndex);
					}
				}
			}
		}
		int prevChunkX = -1;
		int prevChunkZ = -1;
		int regionChunkSize = api.WorldManager.RegionSize / 32;
		IMapChunk mapchunk = null;
		IServerChunk chunk = null;
		IServerChunk chunkOneBlockBelow = null;
		int ly = GameMath.Mod(pondYPos, 32);
		bool extraPondDepth = rand.NextFloat() > 0.5f;
		bool withSeabed = extraPondDepth || pondPositions.Count > 16;
		while (pondPositions.Count > 0)
		{
			int num2 = pondPositions.Dequeue();
			int px = num2 % searchSize - mapOffset + basePosX;
			int num3 = num2 / searchSize - mapOffset + basePosZ;
			int curChunkX = px / 32;
			int curChunkZ = num3 / 32;
			int lx = GameMath.Mod(px, 32);
			int lz = GameMath.Mod(num3, 32);
			if (curChunkX != prevChunkX || curChunkZ != prevChunkZ)
			{
				chunk = (IServerChunk)blockAccessor.GetChunk(curChunkX, pondYPos / 32, curChunkZ);
				if (chunk == null)
				{
					chunk = api.WorldManager.GetChunk(curChunkX, pondYPos / 32, curChunkZ);
				}
				chunk.Unpack();
				if (ly == 0)
				{
					chunkOneBlockBelow = (IServerChunk)blockAccessor.GetChunk(curChunkX, (pondYPos - 1) / 32, curChunkZ);
					if (chunkOneBlockBelow == null)
					{
						return;
					}
					chunkOneBlockBelow.Unpack();
				}
				else
				{
					chunkOneBlockBelow = chunk;
				}
				mapchunk = chunk.MapChunk;
				IntDataMap2D climateMap = mapchunk.MapRegion.ClimateMap;
				float fac = (float)climateMap.InnerSize / (float)regionChunkSize;
				int rlX = curChunkX % regionChunkSize;
				int rlZ = curChunkZ % regionChunkSize;
				climateUpLeft = climateMap.GetUnpaddedInt((int)((float)rlX * fac), (int)((float)rlZ * fac));
				climateUpRight = climateMap.GetUnpaddedInt((int)((float)rlX * fac + fac), (int)((float)rlZ * fac));
				climateBotLeft = climateMap.GetUnpaddedInt((int)((float)rlX * fac), (int)((float)rlZ * fac + fac));
				climateBotRight = climateMap.GetUnpaddedInt((int)((float)rlX * fac + fac), (int)((float)rlZ * fac + fac));
				prevChunkX = curChunkX;
				prevChunkZ = curChunkZ;
				chunkOneBlockBelow.MarkModified();
				chunk.MarkModified();
			}
			if (mapchunk.RainHeightMap[lz * 32 + lx] < pondYPos)
			{
				mapchunk.RainHeightMap[lz * 32 + lx] = (ushort)pondYPos;
			}
			int climate = GameMath.BiLerpRgbColor((float)lx / 32f, (float)lz / 32f, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
			float temp = Climate.GetScaledAdjustedTemperatureFloat((climate >> 16) & 0xFF, pondYPos - TerraGenConfig.seaLevel);
			int index3d = (ly * 32 + lz) * 32 + lx;
			Block existing = api.World.GetBlock(chunk.Data.GetBlockId(index3d, 1));
			if (existing.BlockMaterial == EnumBlockMaterial.Plant)
			{
				chunk.Data.SetBlockAir(index3d);
				if (existing.EntityClass != null)
				{
					tmpPos.Set(curChunkX * 32 + lx, pondYPos, curChunkZ * 32 + lz);
					chunk.RemoveBlockEntity(tmpPos);
				}
			}
			chunk.Data.SetFluid(index3d, (temp < -5f) ? GlobalConfig.lakeIceBlockId : waterID);
			if (!withSeabed)
			{
				continue;
			}
			int index = ((ly == 0) ? ((992 + lz) * 32 + lx) : (((ly - 1) * 32 + lz) * 32 + lx));
			if (api.World.Blocks[chunkOneBlockBelow.Data.GetFluid(index)].IsLiquid())
			{
				continue;
			}
			float rainRel = (float)Climate.GetRainFall((climate >> 8) & 0xFF, pondYPos) / 255f;
			int rockBlockId = mapchunk.TopRockIdMap[lz * 32 + lx];
			if (rockBlockId != 0)
			{
				int lakebedId = lakebedLayerConfig.GetSuitable(temp, rainRel, (float)pondYPos / (float)mapheight, rand, rockBlockId);
				if (lakebedId != 0)
				{
					chunkOneBlockBelow.Data[index] = lakebedId;
				}
			}
		}
		if (extraPondDepth)
		{
			TryPlacePondAt(dx, pondYPos + 1, dz, chunkX, chunkZ, depth + 1);
		}
	}
}
