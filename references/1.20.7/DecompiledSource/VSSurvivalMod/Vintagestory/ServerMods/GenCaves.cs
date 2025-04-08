using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenCaves : GenPartial
{
	internal LCGRandom caveRand;

	private IWorldGenBlockAccessor worldgenBlockAccessor;

	private NormalizedSimplexNoise basaltNoise;

	private NormalizedSimplexNoise heightvarNoise;

	private int regionsize;

	protected override int chunkRange => 5;

	public override double ExecuteOrder()
	{
		return 0.3;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.ChunkColumnGeneration(GenChunkColumn, EnumWorldGenPass.Terrain, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
			api.ChatCommands.GetOrCreate("dev").BeginSubCommand("gencaves").WithDescription("Cave generator test tool. Deletes all chunks in the area and generates inverse caves around the world middle")
				.RequiresPrivilege(Privilege.controlserver)
				.HandleWith(CmdCaveGenTest)
				.EndSubCommand();
			if (TerraGenConfig.DoDecorationPass)
			{
				api.Event.MapChunkGeneration(OnMapChunkGen, "standard");
				api.Event.MapChunkGeneration(OnMapChunkGen, "superflat");
				api.Event.InitWorldGenerator(initWorldGen, "superflat");
			}
		}
	}

	public override void initWorldGen()
	{
		base.initWorldGen();
		caveRand = new LCGRandom(api.WorldManager.Seed + 123128);
		basaltNoise = NormalizedSimplexNoise.FromDefaultOctaves(2, 0.2857142984867096, 0.8999999761581421, api.World.Seed + 12);
		heightvarNoise = NormalizedSimplexNoise.FromDefaultOctaves(3, 0.05000000074505806, 0.8999999761581421, api.World.Seed + 12);
		regionsize = api.World.BlockAccessor.RegionSize;
	}

	private void OnMapChunkGen(IMapChunk mapChunk, int chunkX, int chunkZ)
	{
		mapChunk.CaveHeightDistort = new byte[1024];
		for (int dx = 0; dx < 32; dx++)
		{
			for (int dz = 0; dz < 32; dz++)
			{
				double val = heightvarNoise.Noise(32 * chunkX + dx, 32 * chunkZ + dz) - 0.5;
				val = ((val > 0.0) ? Math.Max(0.0, val - 0.07) : Math.Min(0.0, val + 0.07));
				mapChunk.CaveHeightDistort[dz * 32 + dx] = (byte)(128.0 * val + 127.0);
			}
		}
	}

	private TextCommandResult CmdCaveGenTest(TextCommandCallingArgs args)
	{
		caveRand = new LCGRandom(api.WorldManager.Seed + 123128);
		initWorldGen();
		airBlockId = api.World.GetBlock(new AssetLocation("rock-granite")).BlockId;
		int baseChunkX = api.World.BlockAccessor.MapSizeX / 2 / 32;
		int baseChunkZ = api.World.BlockAccessor.MapSizeZ / 2 / 32;
		for (int dx2 = -5; dx2 <= 5; dx2++)
		{
			for (int dz2 = -5; dz2 <= 5; dz2++)
			{
				int chunkX2 = baseChunkX + dx2;
				int chunkZ2 = baseChunkZ + dz2;
				IServerChunk[] chunks2 = GetChunkColumn(chunkX2, chunkZ2);
				for (int i = 0; i < chunks2.Length; i++)
				{
					if (chunks2[i] == null)
					{
						return TextCommandResult.Success("Cannot generate 10x10 area of caves, chunks are not loaded that far yet.");
					}
				}
				OnMapChunkGen(chunks2[0].MapChunk, chunkX2, chunkZ2);
			}
		}
		for (int dx = -5; dx <= 5; dx++)
		{
			for (int dz = -5; dz <= 5; dz++)
			{
				int chunkX = baseChunkX + dx;
				int chunkZ = baseChunkZ + dz;
				IServerChunk[] chunks = GetChunkColumn(chunkX, chunkZ);
				ClearChunkColumn(chunks);
				for (int gdx = -chunkRange; gdx <= chunkRange; gdx++)
				{
					for (int gdz = -chunkRange; gdz <= chunkRange; gdz++)
					{
						chunkRand.InitPositionSeed(chunkX + gdx, chunkZ + gdz);
						GeneratePartial(chunks, chunkX, chunkZ, gdx, gdz);
					}
				}
				MarkDirty(chunkX, chunkZ, chunks);
			}
		}
		airBlockId = 0;
		return TextCommandResult.Success("Generated and chunks force resend flags set");
	}

	private IServerChunk[] GetChunkColumn(int chunkX, int chunkZ)
	{
		int size = api.World.BlockAccessor.MapSizeY / 32;
		IServerChunk[] chunks = new IServerChunk[size];
		for (int chunkY = 0; chunkY < size; chunkY++)
		{
			chunks[chunkY] = api.WorldManager.GetChunk(chunkX, chunkY, chunkZ);
		}
		return chunks;
	}

	private void MarkDirty(int chunkX, int chunkZ, IServerChunk[] chunks)
	{
		for (int chunkY = 0; chunkY < chunks.Length; chunkY++)
		{
			chunks[chunkY].MarkModified();
			api.WorldManager.BroadcastChunk(chunkX, chunkY, chunkZ);
		}
	}

	private bool ClearChunkColumn(IServerChunk[] chunks)
	{
		foreach (IServerChunk chunk in chunks)
		{
			if (chunk == null)
			{
				return false;
			}
			chunk.Unpack();
			chunk.Data.ClearBlocks();
			chunk.MarkModified();
		}
		return true;
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		worldgenBlockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: false);
	}

	public override void GeneratePartial(IServerChunk[] chunks, int chunkX, int chunkZ, int cdx, int cdz)
	{
		if (GetIntersectingStructure(chunkX * 32 + 16, chunkZ * 32 + 16, ModStdWorldGen.SkipCavesgHashCode) != null)
		{
			return;
		}
		worldgenBlockAccessor.BeginColumn();
		LCGRandom chunkRand = base.chunkRand;
		int quantityCaves = (((double)chunkRand.NextInt(100) < TerraGenConfig.CavesPerChunkColumn * 100.0) ? 1 : 0);
		int rndSize = 1024 * (worldheight - 20);
		while (quantityCaves-- > 0)
		{
			int rnd = chunkRand.NextInt(rndSize);
			int posX = cdx * 32 + rnd % 32;
			rnd /= 32;
			int posZ = cdz * 32 + rnd % 32;
			rnd /= 32;
			int posY = rnd + 8;
			float horAngle = chunkRand.NextFloat() * ((float)Math.PI * 2f);
			float vertAngle = (chunkRand.NextFloat() - 0.5f) * 0.25f;
			float horizontalSize = chunkRand.NextFloat() * 2f + chunkRand.NextFloat();
			float verticalSize = 0.75f + chunkRand.NextFloat() * 0.4f;
			rnd = chunkRand.NextInt(500000000);
			if (rnd % 100 < 4)
			{
				horizontalSize = chunkRand.NextFloat() * 2f + chunkRand.NextFloat() + chunkRand.NextFloat();
				verticalSize = 0.25f + chunkRand.NextFloat() * 0.2f;
			}
			else if (rnd % 100 == 4)
			{
				horizontalSize = 0.75f + chunkRand.NextFloat();
				verticalSize = chunkRand.NextFloat() * 2f + chunkRand.NextFloat();
			}
			rnd /= 100;
			bool extraBranchy = posY < TerraGenConfig.seaLevel / 2 && rnd % 50 == 0;
			rnd /= 50;
			int rnd2 = rnd % 1000;
			rnd /= 1000;
			bool largeNearLavaLayer = rnd2 % 10 < 3;
			float curviness = ((rnd == 0) ? 0.035f : ((rnd2 < 30) ? 0.5f : 0.1f));
			int maxIterations = chunkRange * 32 - 16;
			maxIterations -= chunkRand.NextInt(maxIterations / 4);
			caveRand.SetWorldSeed(chunkRand.NextInt(10000000));
			caveRand.InitPositionSeed(chunkX + cdx, chunkZ + cdz);
			CarveTunnel(chunks, chunkX, chunkZ, posX, posY, posZ, horAngle, vertAngle, horizontalSize, verticalSize, 0, maxIterations, 0, extraBranchy, curviness, largeNearLavaLayer);
		}
	}

	private void CarveTunnel(IServerChunk[] chunks, int chunkX, int chunkZ, double posX, double posY, double posZ, float horAngle, float vertAngle, float horizontalSize, float verticalSize, int currentIteration, int maxIterations, int branchLevel, bool extraBranchy = false, float curviness = 0.1f, bool largeNearLavaLayer = false)
	{
		LCGRandom caveRand = this.caveRand;
		ushort[] terrainheightmap = chunks[0].MapChunk.WorldGenTerrainHeightMap;
		ushort[] rainheightmap = chunks[0].MapChunk.RainHeightMap;
		float horAngleChange = 0f;
		float vertAngleChange = 0f;
		float horRadiusGain = 0f;
		float horRadiusLoss = 0f;
		float horRadiusGainAccum = 0f;
		float horRadiusLossAccum = 0f;
		float verHeightGain = 0f;
		float verHeightLoss = 0f;
		float verHeightGainAccum = 0f;
		float verHeightLossAccum = 0f;
		float sizeChangeSpeedAccum = 0.15f;
		float sizeChangeSpeedGain = 0f;
		int branchRand = (branchLevel + 1) * (extraBranchy ? 12 : 25);
		while (currentIteration++ < maxIterations)
		{
			float relPos = (float)currentIteration / (float)maxIterations;
			float horRadius = 1.5f + GameMath.FastSin(relPos * (float)Math.PI) * horizontalSize + horRadiusGainAccum;
			horRadius = Math.Min(horRadius, Math.Max(1f, horRadius - horRadiusLossAccum));
			float vertRadius = 1.5f + GameMath.FastSin(relPos * (float)Math.PI) * (verticalSize + horRadiusLossAccum / 4f) + verHeightGainAccum;
			vertRadius = Math.Min(vertRadius, Math.Max(0.6f, vertRadius - verHeightLossAccum));
			float advanceHor = GameMath.FastCos(vertAngle);
			float advanceVer = GameMath.FastSin(vertAngle);
			if (largeNearLavaLayer)
			{
				float factor = 1f + Math.Max(0f, 1f - (float)Math.Abs(posY - 12.0) / 10f);
				horRadius *= factor;
				vertRadius *= factor;
			}
			if (vertRadius < 1f)
			{
				vertAngle *= 0.1f;
			}
			posX += (double)(GameMath.FastCos(horAngle) * advanceHor);
			posY += (double)GameMath.Clamp(advanceVer, 0f - vertRadius, vertRadius);
			posZ += (double)(GameMath.FastSin(horAngle) * advanceHor);
			vertAngle *= 0.8f;
			int rrnd = caveRand.NextInt(800000);
			if (rrnd / 10000 == 0)
			{
				sizeChangeSpeedGain = caveRand.NextFloat() * caveRand.NextFloat() / 2f;
			}
			bool genHotSpring = false;
			int rnd = rrnd % 10000;
			if ((rnd -= 30) <= 0)
			{
				horAngle = caveRand.NextFloat() * ((float)Math.PI * 2f);
			}
			else if ((rnd -= 76) <= 0)
			{
				horAngle += caveRand.NextFloat() * (float)Math.PI - (float)Math.PI / 2f;
			}
			else if ((rnd -= 60) <= 0)
			{
				horRadiusGain = caveRand.NextFloat() * caveRand.NextFloat() * 3.5f;
			}
			else if ((rnd -= 60) <= 0)
			{
				horRadiusLoss = caveRand.NextFloat() * caveRand.NextFloat() * 10f;
			}
			else if ((rnd -= 50) <= 0)
			{
				if (posY < (double)(TerraGenConfig.seaLevel - 10))
				{
					verHeightLoss = caveRand.NextFloat() * caveRand.NextFloat() * 12f;
					horRadiusGain = Math.Max(horRadiusGain, caveRand.NextFloat() * caveRand.NextFloat() * 3f);
				}
			}
			else if ((rnd -= 9) <= 0)
			{
				if (posY < (double)(TerraGenConfig.seaLevel - 20))
				{
					horRadiusGain = 1f + caveRand.NextFloat() * caveRand.NextFloat() * 5f;
				}
			}
			else if ((rnd -= 9) <= 0)
			{
				verHeightGain = 2f + caveRand.NextFloat() * caveRand.NextFloat() * 7f;
			}
			else if ((rnd -= 100) <= 0 && posY < 19.0)
			{
				verHeightGain = 2f + caveRand.NextFloat() * caveRand.NextFloat() * 5f;
				horRadiusGain = 4f + caveRand.NextFloat() * caveRand.NextFloat() * 9f;
			}
			if (posY > -5.0 && posY < 16.0 && horRadius > 4f && vertRadius > 2f)
			{
				genHotSpring = true;
			}
			sizeChangeSpeedAccum = Math.Max(0.1f, sizeChangeSpeedAccum + sizeChangeSpeedGain * 0.05f);
			sizeChangeSpeedGain -= 0.02f;
			horRadiusGainAccum = Math.Max(0f, horRadiusGainAccum + horRadiusGain * sizeChangeSpeedAccum);
			horRadiusGain -= 0.45f;
			horRadiusLossAccum = Math.Max(0f, horRadiusLossAccum + horRadiusLoss * sizeChangeSpeedAccum);
			horRadiusLoss -= 0.4f;
			verHeightGainAccum = Math.Max(0f, verHeightGainAccum + verHeightGain * sizeChangeSpeedAccum);
			verHeightGain -= 0.45f;
			verHeightLossAccum = Math.Max(0f, verHeightLossAccum + verHeightLoss * sizeChangeSpeedAccum);
			verHeightLoss -= 0.4f;
			horAngle += curviness * horAngleChange;
			vertAngle += curviness * vertAngleChange;
			vertAngleChange = 0.9f * vertAngleChange + caveRand.NextFloatMinusToPlusOne() * caveRand.NextFloat() * 3f;
			horAngleChange = 0.9f * horAngleChange + caveRand.NextFloatMinusToPlusOne() * caveRand.NextFloat();
			if (rrnd % 140 == 0)
			{
				horAngleChange *= caveRand.NextFloat() * 6f;
			}
			int brand = branchRand + 2 * Math.Max(0, (int)posY - (TerraGenConfig.seaLevel - 20));
			if (branchLevel < 3 && (vertRadius > 1f || horRadius > 1f) && caveRand.NextInt(brand) == 0)
			{
				CarveTunnel(chunks, chunkX, chunkZ, posX, posY + (double)(verHeightGainAccum / 2f), posZ, horAngle + (caveRand.NextFloat() + caveRand.NextFloat() - 1f) + (float)Math.PI, vertAngle + (caveRand.NextFloat() - 0.5f) * (caveRand.NextFloat() - 0.5f), horizontalSize, verticalSize + verHeightGainAccum, currentIteration, maxIterations - (int)((double)caveRand.NextFloat() * 0.5 * (double)maxIterations), branchLevel + 1);
			}
			if (branchLevel < 1 && horRadius > 3f && posY > 60.0 && caveRand.NextInt(60) == 0)
			{
				CarveShaft(chunks, chunkX, chunkZ, posX, posY + (double)(verHeightGainAccum / 2f), posZ, horAngle + (caveRand.NextFloat() + caveRand.NextFloat() - 1f) + (float)Math.PI, -1.6707964f + 0.2f * caveRand.NextFloat(), Math.Min(3.5f, horRadius - 1f), verticalSize + verHeightGainAccum, currentIteration, maxIterations - (int)((double)caveRand.NextFloat() * 0.5 * (double)maxIterations) + (int)(posY / 5.0 * (double)(0.5f + 0.5f * caveRand.NextFloat())), branchLevel);
				branchLevel++;
			}
			if ((!(horRadius >= 2f) || rrnd % 5 != 0) && !(posX <= (double)((0f - horRadius) * 2f)) && !(posX >= (double)(32f + horRadius * 2f)) && !(posZ <= (double)((0f - horRadius) * 2f)) && !(posZ >= (double)(32f + horRadius * 2f)))
			{
				SetBlocks(chunks, horRadius, vertRadius + verHeightGainAccum, posX, posY + (double)(verHeightGainAccum / 2f), posZ, terrainheightmap, rainheightmap, chunkX, chunkZ, genHotSpring);
			}
		}
	}

	private void CarveShaft(IServerChunk[] chunks, int chunkX, int chunkZ, double posX, double posY, double posZ, float horAngle, float vertAngle, float horizontalSize, float verticalSize, int caveCurrentIteration, int maxIterations, int branchLevel)
	{
		float vertAngleChange = 0f;
		ushort[] terrainheightmap = chunks[0].MapChunk.WorldGenTerrainHeightMap;
		ushort[] rainheightmap = chunks[0].MapChunk.RainHeightMap;
		int currentIteration = 0;
		while (currentIteration++ < maxIterations)
		{
			float relPos = (float)currentIteration / (float)maxIterations;
			float horRadius = horizontalSize * (1f - relPos * 0.33f);
			float vertRadius = horRadius * verticalSize;
			float advanceHor = GameMath.FastCos(vertAngle);
			float advanceVer = GameMath.FastSin(vertAngle);
			if (vertRadius < 1f)
			{
				vertAngle *= 0.1f;
			}
			posX += (double)(GameMath.FastCos(horAngle) * advanceHor);
			posY += (double)GameMath.Clamp(advanceVer, 0f - vertRadius, vertRadius);
			posZ += (double)(GameMath.FastSin(horAngle) * advanceHor);
			vertAngle += 0.1f * vertAngleChange;
			vertAngleChange = 0.9f * vertAngleChange + (caveRand.NextFloat() - caveRand.NextFloat()) * caveRand.NextFloat() / 3f;
			if (maxIterations - currentIteration < 10)
			{
				int num = 3 + caveRand.NextInt(4);
				for (int i = 0; i < num; i++)
				{
					CarveTunnel(chunks, chunkX, chunkZ, posX, posY, posZ, caveRand.NextFloat() * ((float)Math.PI * 2f), (caveRand.NextFloat() - 0.5f) * 0.25f, horizontalSize + 1f, verticalSize, caveCurrentIteration, maxIterations, 1);
				}
				break;
			}
			if ((caveRand.NextInt(5) != 0 || !(horRadius >= 2f)) && !(posX <= (double)((0f - horRadius) * 2f)) && !(posX >= (double)(32f + horRadius * 2f)) && !(posZ <= (double)((0f - horRadius) * 2f)) && !(posZ >= (double)(32f + horRadius * 2f)))
			{
				SetBlocks(chunks, horRadius, vertRadius, posX, posY, posZ, terrainheightmap, rainheightmap, chunkX, chunkZ, genHotSpring: false);
			}
		}
	}

	private bool SetBlocks(IServerChunk[] chunks, float horRadius, float vertRadius, double centerX, double centerY, double centerZ, ushort[] terrainheightmap, ushort[] rainheightmap, int chunkX, int chunkZ, bool genHotSpring)
	{
		IMapChunk mapchunk = chunks[0].MapChunk;
		horRadius += 1f;
		vertRadius += 2f;
		int num = (int)GameMath.Clamp(centerX - (double)horRadius, 0.0, 31.0);
		int maxdx = (int)GameMath.Clamp(centerX + (double)horRadius + 1.0, 0.0, 31.0);
		int mindy = (int)GameMath.Clamp(centerY - (double)(vertRadius * 0.7f), 1.0, worldheight - 1);
		int maxdy = (int)GameMath.Clamp(centerY + (double)vertRadius + 1.0, 1.0, worldheight - 1);
		int mindz = (int)GameMath.Clamp(centerZ - (double)horRadius, 0.0, 31.0);
		int maxdz = (int)GameMath.Clamp(centerZ + (double)horRadius + 1.0, 0.0, 31.0);
		double hRadiusSq = horRadius * horRadius;
		double vRadiusSq = vertRadius * vertRadius;
		double distortStrength = GameMath.Clamp((double)vertRadius / 4.0, 0.0, 0.1);
		for (int lx2 = num; lx2 <= maxdx; lx2++)
		{
			double xdistRel = ((double)lx2 - centerX) * ((double)lx2 - centerX) / hRadiusSq;
			for (int lz2 = mindz; lz2 <= maxdz; lz2++)
			{
				double zdistRel = ((double)lz2 - centerZ) * ((double)lz2 - centerZ) / hRadiusSq;
				double heightrnd2 = (double)(mapchunk.CaveHeightDistort[lz2 * 32 + lx2] - 127) * distortStrength;
				for (int y2 = mindy; y2 <= maxdy + 10; y2++)
				{
					double num2 = (double)y2 - centerY;
					double heightOffFac2 = ((num2 > 0.0) ? (heightrnd2 * heightrnd2) : 0.0);
					double ydistRel = num2 * num2 / (vRadiusSq + heightOffFac2);
					if (!(xdistRel + ydistRel + zdistRel > 1.0) && y2 <= worldheight - 1)
					{
						int ly = y2 % 32;
						if (api.World.Blocks[chunks[y2 / 32].Data.GetFluid((ly * 32 + lz2) * 32 + lx2)].LiquidCode != null)
						{
							return false;
						}
					}
				}
			}
		}
		horRadius -= 1f;
		vertRadius -= 2f;
		int num3 = (int)GameMath.Clamp(centerX - (double)horRadius, 0.0, 31.0);
		maxdx = (int)GameMath.Clamp(centerX + (double)horRadius + 1.0, 0.0, 31.0);
		mindz = (int)GameMath.Clamp(centerZ - (double)horRadius, 0.0, 31.0);
		maxdz = (int)GameMath.Clamp(centerZ + (double)horRadius + 1.0, 0.0, 31.0);
		mindy = (int)GameMath.Clamp(centerY - (double)(vertRadius * 0.7f), 1.0, worldheight - 1);
		maxdy = (int)GameMath.Clamp(centerY + (double)vertRadius + 1.0, 1.0, worldheight - 1);
		hRadiusSq = horRadius * horRadius;
		vRadiusSq = vertRadius * vertRadius;
		int geoActivity = getGeologicActivity(chunkX * 32 + (int)centerX, chunkZ * 32 + (int)centerZ);
		genHotSpring = genHotSpring && geoActivity > 128;
		if (genHotSpring && centerX >= 0.0 && centerX < 32.0 && centerZ >= 0.0 && centerZ < 32.0)
		{
			Dictionary<Vec3i, HotSpringGenData> data = mapchunk.GetModdata<Dictionary<Vec3i, HotSpringGenData>>("hotspringlocations");
			if (data == null)
			{
				data = new Dictionary<Vec3i, HotSpringGenData>();
			}
			data[new Vec3i((int)centerX, (int)centerY, (int)centerZ)] = new HotSpringGenData
			{
				horRadius = horRadius
			};
			mapchunk.SetModdata("hotspringlocations", data);
		}
		int yLavaStart = geoActivity * 16 / 128;
		for (int lx = num3; lx <= maxdx; lx++)
		{
			double xdistRel = ((double)lx - centerX) * ((double)lx - centerX) / hRadiusSq;
			for (int lz = mindz; lz <= maxdz; lz++)
			{
				double zdistRel = ((double)lz - centerZ) * ((double)lz - centerZ) / hRadiusSq;
				double heightrnd = (double)(mapchunk.CaveHeightDistort[lz * 32 + lx] - 127) * distortStrength;
				int surfaceY = terrainheightmap[lz * 32 + lx];
				for (int y = maxdy + 10; y >= mindy; y--)
				{
					double num4 = (double)y - centerY;
					double heightOffFac = ((num4 > 0.0) ? (heightrnd * heightrnd * Math.Min(1.0, (double)Math.Abs(y - surfaceY) / 10.0)) : 0.0);
					double ydistRel = num4 * num4 / (vRadiusSq + heightOffFac);
					if (y <= worldheight - 1 && !(xdistRel + ydistRel + zdistRel > 1.0))
					{
						if (terrainheightmap[lz * 32 + lx] == y)
						{
							terrainheightmap[lz * 32 + lx] = (ushort)(y - 1);
							rainheightmap[lz * 32 + lx]--;
						}
						IChunkBlocks chunkBlockData = chunks[y / 32].Data;
						int index3d = (y % 32 * 32 + lz) * 32 + lx;
						if (y == 11)
						{
							if (basaltNoise.Noise(chunkX * 32 + lx, chunkZ * 32 + lz) > 0.65)
							{
								chunkBlockData[index3d] = GlobalConfig.basaltBlockId;
								terrainheightmap[lz * 32 + lx] = Math.Max(terrainheightmap[lz * 32 + lx], (ushort)11);
								rainheightmap[lz * 32 + lx] = Math.Max(rainheightmap[lz * 32 + lx], (ushort)11);
							}
							else
							{
								chunkBlockData[index3d] = 0;
								if (y > yLavaStart)
								{
									chunkBlockData[index3d] = GlobalConfig.basaltBlockId;
								}
								else
								{
									chunkBlockData.SetFluid(index3d, GlobalConfig.lavaBlockId);
								}
								if (y <= yLavaStart)
								{
									worldgenBlockAccessor.ScheduleBlockLightUpdate(new BlockPos(chunkX * 32 + lx, y, chunkZ * 32 + lz), airBlockId, GlobalConfig.lavaBlockId);
								}
							}
						}
						else if (y < 12)
						{
							chunkBlockData[index3d] = 0;
							if (y > yLavaStart)
							{
								chunkBlockData[index3d] = GlobalConfig.basaltBlockId;
							}
							else
							{
								chunkBlockData.SetFluid(index3d, GlobalConfig.lavaBlockId);
							}
						}
						else
						{
							chunkBlockData.SetBlockAir(index3d);
						}
					}
				}
			}
		}
		return true;
	}

	private int getGeologicActivity(int posx, int posz)
	{
		IntDataMap2D climateMap = worldgenBlockAccessor.GetMapRegion(posx / regionsize, posz / regionsize)?.ClimateMap;
		if (climateMap == null)
		{
			return 0;
		}
		int regionChunkSize = regionsize / 32;
		float fac = (float)climateMap.InnerSize / (float)regionChunkSize;
		int rlX = posx / 32 % regionChunkSize;
		int rlZ = posz / 32 % regionChunkSize;
		return climateMap.GetUnpaddedInt((int)((float)rlX * fac), (int)((float)rlZ * fac)) & 0xFF;
	}
}
