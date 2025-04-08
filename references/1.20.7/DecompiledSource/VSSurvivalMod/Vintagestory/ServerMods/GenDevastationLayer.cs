using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Vintagestory.ServerMods;

public class GenDevastationLayer : ModStdWorldGen
{
	private ICoreServerAPI api;

	private StoryStructureLocation devastationLocation;

	public IWorldGenBlockAccessor worldgenBlockAccessor;

	public SimplexNoise distDistort;

	public NormalizedSimplexNoise devastationDensity;

	private byte[] noisemap;

	private int cellnoiseWidth;

	private int cellnoiseHeight;

	private const float fullHeightDist = 0.3f;

	private const float flatHeightDist = 0.4f;

	public static int[] DevastationBlockIds;

	private int growthBlockId;

	private int dim2Size;

	private const int Dim2HeightOffset = 9;

	private BlockPos tmpPos = new BlockPos();

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override double ExecuteOrder()
	{
		return 0.399;
	}

	public override void Start(ICoreAPI api)
	{
		api.Network.RegisterChannel("devastation").RegisterMessageType<DevaLocation>();
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		api.Event.InitWorldGenerator(InitWorldGen, "standard");
		api.Event.PlayerJoin += Event_PlayerJoin;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.ChunkColumnGeneration(OnChunkColumnGeneration, EnumWorldGenPass.Terrain, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
		}
		distDistort = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 20980);
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		if (devastationLocation != null)
		{
			api.Network.GetChannel("devastation").SendPacket(new DevaLocation
			{
				Pos = devastationLocation.CenterPos,
				Radius = devastationLocation.GenerationRadius
			}, byPlayer);
		}
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		worldgenBlockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: false);
	}

	private void InitWorldGen()
	{
		LoadGlobalConfig(api);
		distDistort = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 20980);
		devastationDensity = new NormalizedSimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.04, 0.08, 0.16, 0.3076923076923077 }, api.World.SeaLevel + 20981);
		modSys.storyStructureInstances.TryGetValue("devastationarea", out devastationLocation);
		if (devastationLocation != null)
		{
			Timeswitch ts = api.ModLoader.GetModSystem<Timeswitch>();
			ts.SetPos(devastationLocation.CenterPos);
			dim2Size = ts.SetupDim2TowerGeneration(devastationLocation, modSys);
		}
		ModSystemDevastationEffects modSystem = api.ModLoader.GetModSystem<ModSystemDevastationEffects>();
		modSystem.DevaLocationPresent = devastationLocation?.CenterPos.ToVec3d();
		modSystem.DevaLocationPast = devastationLocation?.CenterPos.Copy().SetDimension(2).ToVec3d();
		modSystem.EffectRadius = devastationLocation?.GenerationRadius ?? 0;
		BitmapRef bmp = BitmapCreateFromPng(api.Assets.TryGet("worldgen/devastationcracks.png"));
		int[] pixels = bmp.Pixels;
		noisemap = new byte[pixels.Length];
		for (int i = 0; i < pixels.Length; i++)
		{
			noisemap[i] = (byte)((uint)pixels[i] & 0xFFu);
		}
		cellnoiseWidth = bmp.Width;
		cellnoiseHeight = bmp.Height;
		DevastationBlockIds = new int[11]
		{
			GetBlockId("devastatedsoil-0"),
			GetBlockId("devastatedsoil-1"),
			GetBlockId("devastatedsoil-2"),
			GetBlockId("devastatedsoil-3"),
			GetBlockId("devastatedsoil-4"),
			GetBlockId("devastatedsoil-5"),
			GetBlockId("devastatedsoil-6"),
			GetBlockId("devastatedsoil-7"),
			GetBlockId("devastatedsoil-8"),
			GetBlockId("devastatedsoil-9"),
			GetBlockId("devastatedsoil-10")
		};
		growthBlockId = GetBlockId("devastationgrowth-normal");
		api.ModLoader.GetModSystem<GenStructures>().OnPreventSchematicPlaceAt += OnPreventSchematicPlaceAt;
	}

	private int GetBlockId(string code)
	{
		return api.World.GetBlock(new AssetLocation(code))?.BlockId ?? GlobalConfig.defaultRockId;
	}

	public BitmapRef BitmapCreateFromPng(IAsset asset)
	{
		return new BitmapExternal(new MemoryStream(asset.Data));
	}

	private void OnChunkColumnGeneration(IChunkColumnGenerateRequest request)
	{
		if (devastationLocation == null)
		{
			return;
		}
		BlockPos cpos = devastationLocation.CenterPos;
		int devastationRadius = devastationLocation.GenerationRadius;
		int rposx = request.ChunkX * 32 + 16;
		int rposz = request.ChunkZ * 32 + 16;
		if ((double)cpos.HorDistanceSqTo(rposx, rposz) >= (double)((devastationRadius + 100) * (devastationRadius + 100)))
		{
			return;
		}
		Random rnd = api.World.Rand;
		IServerChunk[] chunks = request.Chunks;
		IMapChunk mapchunk = chunks[0].MapChunk;
		if (ShouldGenerateDim2Terrain(request.ChunkX, request.ChunkZ))
		{
			api.WorldManager.CreateChunkColumnForDimension(request.ChunkX, request.ChunkZ, 2);
			GenerateDim2ChunkColumn(request.ChunkX, request.ChunkZ, mapchunk.WorldGenTerrainHeightMap);
		}
		float noiseMax = (float)DevastationBlockIds.Length - 1.01f;
		float noiseSub = DevastationBlockIds.Length;
		float noiseMul = (float)DevastationBlockIds.Length * 2f;
		for (int dx = 0; dx < 32; dx++)
		{
			for (int dz = 0; dz < 32; dz++)
			{
				int x = request.ChunkX * 32 + dx;
				int z = request.ChunkZ * 32 + dz;
				double density = GameMath.Clamp(devastationDensity.Noise(x, z) * (double)noiseMul - (double)noiseSub, 0.0, noiseMax);
				double extraDist = distDistort.Noise(x, z);
				double distance = (double)cpos.HorDistanceSqTo(x, z) / (double)(devastationRadius * devastationRadius);
				double distrel = distance + extraDist / 30.0;
				if (distrel > 1.0)
				{
					continue;
				}
				double heightModMapped = GameMath.Map(GameMath.Clamp(distance + extraDist / 1000.0, 0.30000001192092896, 0.4000000059604645), 0.30000001192092896, 0.4000000059604645, 0.0, 1.0);
				double offset = GameMath.Clamp((1.0 - heightModMapped) * 10.0, 0.0, 10.0);
				double offset2 = GameMath.Clamp((0.6000000238418579 - distrel) * 20.0, 0.0, 10.0);
				offset = GameMath.Max(offset, offset2 * GameMath.Clamp(heightModMapped + 0.2, 0.0, 0.8));
				int index2d = dz * 32 + dx;
				int wgenheight = mapchunk.WorldGenTerrainHeightMap[index2d];
				int nmapx = x - cpos.X + cellnoiseWidth / 2;
				int nmapz = z - cpos.Z + cellnoiseHeight / 2;
				int dy = 0;
				if (nmapx >= 0 && nmapz >= 0 && nmapx < cellnoiseWidth && nmapz < cellnoiseHeight)
				{
					dy = noisemap[nmapz * cellnoiseWidth + nmapx];
				}
				int height = (int)Math.Round(offset - (double)((float)dy / 30f));
				for (int j = height - 10; j <= height; j++)
				{
					int chunkY3 = (wgenheight + j) / 32;
					int lY3 = (wgenheight + j) % 32;
					int index3d3 = (32 * lY3 + dz) * 32 + dx;
					chunks[chunkY3].Data.SetBlockUnsafe(index3d3, DevastationBlockIds[(int)Math.Round(density)]);
					chunks[chunkY3].Data.SetFluid(index3d3, 0);
				}
				if (height < 0)
				{
					for (int i = height; i <= 0; i++)
					{
						int chunkY2 = (wgenheight + i) / 32;
						int lY2 = (wgenheight + i) % 32;
						int index3d2 = (32 * lY2 + dz) * 32 + dx;
						chunks[chunkY2].Data.SetBlockUnsafe(index3d2, 0);
						chunks[chunkY2].Data.SetFluid(index3d2, 0);
					}
				}
				ushort newWgenHeigt = (ushort)(wgenheight + height);
				mapchunk.WorldGenTerrainHeightMap[index2d] = newWgenHeigt;
				ushort rainHeight = Math.Max(newWgenHeigt, mapchunk.RainHeightMap[index2d]);
				mapchunk.RainHeightMap[index2d] = rainHeight;
				if (rnd.NextDouble() - 0.1 < density && rainHeight == newWgenHeigt)
				{
					int chunkY = (wgenheight + height + 1) / 32;
					int lY = (wgenheight + height + 1) % 32;
					int index3d = (32 * lY + dz) * 32 + dx;
					chunks[chunkY].Data.SetBlockUnsafe(index3d, growthBlockId);
				}
			}
		}
		api.ModLoader.GetModSystem<Timeswitch>().AttemptGeneration(worldgenBlockAccessor);
	}

	private bool OnPreventSchematicPlaceAt(IBlockAccessor blockAccessor, BlockPos pos, Cuboidi schematicLocation, string locationCode)
	{
		if (locationCode == "devastationarea" && !HasDevastationSoil(blockAccessor, pos, schematicLocation.SizeX, schematicLocation.SizeZ))
		{
			return true;
		}
		return false;
	}

	private bool HasDevastationSoil(IBlockAccessor blockAccessor, BlockPos startPos, int wdt, int len)
	{
		tmpPos.Set(startPos.X, startPos.Y + 1, startPos.Z);
		int height = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Y = height;
		if (!DevastationBlockIds.Contains(blockAccessor.GetBlockId(tmpPos)))
		{
			return false;
		}
		tmpPos.Set(startPos.X + wdt, startPos.Y + 1, startPos.Z);
		height = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Y = height;
		if (!DevastationBlockIds.Contains(blockAccessor.GetBlockId(tmpPos)))
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y + 1, startPos.Z + len);
		height = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Y = height;
		if (!DevastationBlockIds.Contains(blockAccessor.GetBlockId(tmpPos)))
		{
			return false;
		}
		tmpPos.Set(startPos.X + wdt, startPos.Y + 1, startPos.Z + len);
		height = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Y = height;
		if (!DevastationBlockIds.Contains(blockAccessor.GetBlockId(tmpPos)))
		{
			return false;
		}
		tmpPos.Set(startPos.X + wdt / 2, startPos.Y + 1, startPos.Z + len / 2);
		height = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Y = height;
		if (!DevastationBlockIds.Contains(blockAccessor.GetBlockId(tmpPos)))
		{
			return false;
		}
		return true;
	}

	public override void Dispose()
	{
		DevastationBlockIds = null;
	}

	private bool ShouldGenerateDim2Terrain(int cx, int cz)
	{
		int radius = dim2Size;
		int baseCx = devastationLocation.CenterPos.X / 32;
		int baseCz = devastationLocation.CenterPos.Z / 32;
		int num = Math.Abs(cx - baseCx);
		int dz = Math.Abs(cz - baseCz);
		if (num + dz == 0)
		{
			devastationLocation.DidGenerateAdditional = false;
		}
		if (num <= radius)
		{
			return dz <= radius;
		}
		return false;
	}

	private void GenerateDim2ChunkColumn(int cx, int cz, ushort[] heightmap)
	{
		int rockId = GlobalConfig.defaultRockId;
		int soilId = GetBlockId("soil-medium-none");
		int topsoilId = GetBlockId("soil-medium-normal");
		int grassId1 = GetBlockId("tallgrass-medium-free");
		int grassId2 = GetBlockId("tallgrass-tall-free");
		int miny = api.World.BlockAccessor.MapSizeY - 1;
		int yTop = 0;
		for (int i = 0; i < heightmap.Length; i++)
		{
			int height = heightmap[i] + 9;
			if (height < miny)
			{
				miny = height;
			}
			if (height > yTop)
			{
				yTop = height;
			}
		}
		int cy = 2048;
		IWorldChunk chunk = api.World.BlockAccessor.GetChunk(cx, cy, cz);
		if (chunk == null)
		{
			return;
		}
		chunk.Unpack();
		IChunkBlocks chunkBlockData = chunk.Data;
		chunkBlockData.SetBlockBulk(0, 32, 32, GlobalConfig.mantleBlockId);
		int yBase;
		for (yBase = 1; yBase < miny - 3; yBase++)
		{
			if (yBase % 32 == 0)
			{
				cy++;
				chunk = api.World.BlockAccessor.GetChunk(cx, cy, cz);
				if (chunk == null)
				{
					break;
				}
				chunkBlockData = chunk.Data;
			}
			chunkBlockData.SetBlockBulk(yBase % 32 * 32 * 32, 32, 32, rockId);
		}
		yTop++;
		for (int posY = yBase; posY <= yTop; posY++)
		{
			if (posY % 32 == 0)
			{
				cy++;
				chunk = api.World.BlockAccessor.GetChunk(cx, cy, cz);
				if (chunk == null)
				{
					break;
				}
				chunkBlockData = chunk.Data;
			}
			for (int lZ = 0; lZ < 32; lZ++)
			{
				for (int lX = 0; lX < 32; lX++)
				{
					int terrainY = heightmap[lZ * 32 + lX] + 9;
					int lY = posY % 32;
					if (posY < terrainY - 2)
					{
						chunkBlockData[ChunkIndex3D(lX, lY, lZ)] = rockId;
					}
					else if (posY < terrainY)
					{
						chunkBlockData[ChunkIndex3D(lX, lY, lZ)] = soilId;
					}
					else if (posY == terrainY)
					{
						chunkBlockData[ChunkIndex3D(lX, lY, lZ)] = topsoilId;
					}
					else if (posY == terrainY + 1)
					{
						int rand = GameMath.oaatHash(lX + cx * 32, lZ + cz * 32);
						if (rand % 21 < 3)
						{
							chunkBlockData[ChunkIndex3D(lX, lY, lZ)] = ((rand % 21 == 0) ? grassId2 : grassId1);
						}
					}
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ChunkIndex3D(int lx, int ly, int lz)
	{
		return (ly * 32 + lz) * 32 + lx;
	}
}
