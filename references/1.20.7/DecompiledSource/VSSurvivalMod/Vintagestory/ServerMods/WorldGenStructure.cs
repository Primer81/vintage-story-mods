using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods;

public class WorldGenStructure : WorldGenStructureBase
{
	[JsonProperty]
	public string Group;

	[JsonProperty]
	public int MinGroupDistance;

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
	public AssetLocation[] ReplaceWithBlocklayers;

	[JsonProperty]
	public bool PostPass;

	[JsonProperty]
	public bool SuppressTrees;

	[JsonProperty]
	public bool SuppressWaterfalls;

	internal BlockSchematicStructure[][] schematicDatas;

	internal int[] replacewithblocklayersBlockids = new int[0];

	internal HashSet<int> insideblockids = new HashSet<int>();

	internal Dictionary<int, Dictionary<int, int>> resolvedRockTypeRemaps;

	private TryGenerateHandler[] Generators;

	private LCGRandom rand;

	private int unscaledMinRain;

	private int unscaledMaxRain;

	private int unscaledMinTemp;

	private int unscaledMaxTemp;

	private GenStructures genStructuresSys;

	private BlockPos tmpPos = new BlockPos();

	public Cuboidi LastPlacedSchematicLocation = new Cuboidi();

	public BlockSchematicStructure LastPlacedSchematic;

	private int climateUpLeft;

	private int climateUpRight;

	private int climateBotLeft;

	private int climateBotRight;

	private BlockPos utestPos = new BlockPos();

	private static Cuboidi tmpLoc = new Cuboidi();

	public WorldGenStructure()
	{
		Generators = new TryGenerateHandler[4] { TryGenerateRuinAtSurface, TryGenerateAtSurface, TryGenerateUnderwater, TryGenerateUnderground };
	}

	public void Init(ICoreServerAPI api, BlockLayerConfig config, RockStrataConfig rockstrata, WorldGenStructuresConfig structureConfig, LCGRandom rand)
	{
		this.rand = rand;
		genStructuresSys = api.ModLoader.GetModSystem<GenStructures>();
		unscaledMinRain = (int)(MinRain * 255f);
		unscaledMaxRain = (int)(MaxRain * 255f);
		unscaledMinTemp = Climate.DescaleTemperature(MinTemp);
		unscaledMaxTemp = Climate.DescaleTemperature(MaxTemp);
		schematicDatas = LoadSchematicsWithRotations<BlockSchematicStructure>(api, this, config, structureConfig, structureConfig.SchematicYOffsets);
		if (ReplaceWithBlocklayers != null)
		{
			replacewithblocklayersBlockids = new int[ReplaceWithBlocklayers.Length];
			for (int j = 0; j < replacewithblocklayersBlockids.Length; j++)
			{
				Block block2 = api.World.GetBlock(ReplaceWithBlocklayers[j]);
				if (block2 == null)
				{
					throw new Exception($"Schematic with code {Code} has replace block layer {ReplaceWithBlocklayers[j]} defined, but no such block found!");
				}
				replacewithblocklayersBlockids[j] = block2.Id;
			}
		}
		if (InsideBlockCodes != null)
		{
			for (int i = 0; i < InsideBlockCodes.Length; i++)
			{
				Block block = api.World.GetBlock(InsideBlockCodes[i]);
				if (block == null)
				{
					throw new Exception($"Schematic with code {Code} has inside block {InsideBlockCodes[i]} defined, but no such block found!");
				}
				insideblockids.Add(block.Id);
			}
		}
		if (RockTypeRemapGroup != null)
		{
			resolvedRockTypeRemaps = structureConfig.resolvedRocktypeRemapGroups[RockTypeRemapGroup];
		}
		if (RockTypeRemaps == null)
		{
			return;
		}
		if (resolvedRockTypeRemaps != null)
		{
			Dictionary<int, Dictionary<int, int>> ownRemaps = WorldGenStructuresConfigBase.ResolveRockTypeRemaps(RockTypeRemaps, rockstrata, api);
			foreach (KeyValuePair<int, Dictionary<int, int>> val in resolvedRockTypeRemaps)
			{
				ownRemaps[val.Key] = val.Value;
			}
			resolvedRockTypeRemaps = ownRemaps;
		}
		else
		{
			resolvedRockTypeRemaps = WorldGenStructuresConfigBase.ResolveRockTypeRemaps(RockTypeRemaps, rockstrata, api);
		}
	}

	internal bool TryGenerate(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos startPos, int climateUpLeft, int climateUpRight, int climateBotLeft, int climateBotRight, string locationCode)
	{
		this.climateUpLeft = climateUpLeft;
		this.climateUpRight = climateUpRight;
		this.climateBotLeft = climateBotLeft;
		this.climateBotRight = climateBotRight;
		int num = GameMath.BiLerpRgbColor((float)(startPos.X % 32) / 32f, (float)(startPos.Z % 32) / 32f, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
		int rain = Climate.GetRainFall((num >> 8) & 0xFF, startPos.Y);
		int unscaledtemp = Climate.DescaleTemperature(Climate.GetScaledAdjustedTemperature((num >> 16) & 0xFF, startPos.Y - TerraGenConfig.seaLevel));
		if (rain < unscaledMinRain || rain > unscaledMaxRain || unscaledtemp < unscaledMinTemp || unscaledtemp > unscaledMaxTemp)
		{
			return false;
		}
		if (unscaledtemp < 20 && startPos.Y > worldForCollectibleResolve.SeaLevel + 15)
		{
			return false;
		}
		rand.InitPositionSeed(startPos.X, startPos.Z);
		bool generated = Generators[(int)Placement](blockAccessor, worldForCollectibleResolve, startPos, locationCode);
		if (generated && Placement == EnumStructurePlacement.SurfaceRuin)
		{
			float rainValMoss = Math.Max(0f, (float)(rain - 50) / 255f);
			float tempValMoss = Math.Max(0f, (float)(unscaledtemp - 50) / 255f);
			float mossGrowthChance = 1.5f * rainValMoss * tempValMoss + 1f * rainValMoss * GameMath.Clamp((tempValMoss + 0.33f) / 1.33f, 0f, 1f);
			int mossTries = (int)(10f * mossGrowthChance * GameMath.Sqrt(LastPlacedSchematicLocation.SizeXYZ));
			int sizex = LastPlacedSchematic.SizeX;
			int sizey = LastPlacedSchematic.SizeY;
			int sizez = LastPlacedSchematic.SizeZ;
			BlockPos tmpPos = new BlockPos(startPos.dimension);
			Block mossDecor = blockAccessor.GetBlock(new AssetLocation("attachingplant-spottymoss"));
			while (mossTries-- > 0)
			{
				int dx = rand.NextInt(sizex);
				int dy = rand.NextInt(sizey);
				int dz = rand.NextInt(sizez);
				tmpPos.Set(startPos.X + dx, startPos.Y + dy, startPos.Z + dz);
				Block block = blockAccessor.GetBlock(tmpPos);
				if (block.BlockMaterial != EnumBlockMaterial.Stone)
				{
					continue;
				}
				for (int i = 0; i < 6; i++)
				{
					BlockFacing face = BlockFacing.ALLFACES[i];
					if (block.SideSolid[i] && !blockAccessor.GetBlockOnSide(tmpPos, face).SideSolid[face.Opposite.Index])
					{
						blockAccessor.SetDecor(mossDecor, tmpPos, face);
						break;
					}
				}
			}
		}
		return generated;
	}

	private int FindClearEntranceRotation(IBlockAccessor blockAccessor, BlockSchematicStructure[] schematics, BlockPos pos)
	{
		BlockSchematicStructure schematic = schematics[0];
		int entranceRot = GameMath.Clamp(schematics[0].EntranceRotation / 90, 0, 3);
		int minX = pos.X - 2;
		int maxX = pos.X + schematic.SizeX + 2;
		int minZ = pos.Z - 2;
		int maxZ = pos.Z + schematic.SizeZ + 2;
		int weightedHeightW = 1;
		int weightedHeightE = 1;
		int weightedHeightN = 1;
		int weightedHeightS = 1;
		int x = minX;
		switch (entranceRot)
		{
		case 1:
		case 3:
		{
			for (int z = minZ; z <= maxZ; z++)
			{
				int h6 = blockAccessor.GetMapChunk(x / 32, z / 32).WorldGenTerrainHeightMap[z % 32 * 32 + x % 32];
				weightedHeightW += h6;
			}
			x = maxX;
			for (int z = minZ; z <= maxZ; z++)
			{
				int h5 = blockAccessor.GetMapChunk(x / 32, z / 32).WorldGenTerrainHeightMap[z % 32 * 32 + x % 32];
				weightedHeightE += h5;
			}
			break;
		}
		case 0:
		case 2:
		{
			int z = minZ;
			for (x = minX; x <= maxX; x++)
			{
				int h8 = blockAccessor.GetMapChunk(x / 32, z / 32).WorldGenTerrainHeightMap[z % 32 * 32 + x % 32];
				weightedHeightN += h8;
			}
			z = maxZ;
			for (x = minX; x <= maxX; x++)
			{
				int h7 = blockAccessor.GetMapChunk(x / 32, z / 32).WorldGenTerrainHeightMap[z % 32 * 32 + x % 32];
				weightedHeightS += h7;
			}
			break;
		}
		}
		schematic = schematics[1];
		int entranceRot2 = GameMath.Clamp(schematic.EntranceRotation / 90, 0, 3);
		minX = pos.X - 2;
		maxX = pos.X + schematic.SizeX + 2;
		minZ = pos.Z - 2;
		maxZ = pos.Z + schematic.SizeZ + 2;
		int weightedHeightW2 = 1;
		int weightedHeightE2 = 1;
		int weightedHeightN2 = 1;
		int weightedHeightS2 = 1;
		switch (entranceRot2)
		{
		case 1:
		case 3:
		{
			for (int z = minZ; z <= maxZ; z++)
			{
				int h2 = blockAccessor.GetMapChunk(x / 32, z / 32).WorldGenTerrainHeightMap[z % 32 * 32 + x % 32];
				weightedHeightW2 += h2;
			}
			x = maxX;
			for (int z = minZ; z <= maxZ; z++)
			{
				int h = blockAccessor.GetMapChunk(x / 32, z / 32).WorldGenTerrainHeightMap[z % 32 * 32 + x % 32];
				weightedHeightE2 += h;
			}
			break;
		}
		case 0:
		case 2:
		{
			int z = minZ;
			for (x = minX; x <= maxX; x++)
			{
				int h4 = blockAccessor.GetMapChunk(x / 32, z / 32).WorldGenTerrainHeightMap[z % 32 * 32 + x % 32];
				weightedHeightN2 += h4;
			}
			z = maxZ;
			for (x = minX; x <= maxX; x++)
			{
				int h3 = blockAccessor.GetMapChunk(x / 32, z / 32).WorldGenTerrainHeightMap[z % 32 * 32 + x % 32];
				weightedHeightS2 += h3;
			}
			break;
		}
		}
		int lowSide = ((entranceRot == 1 || entranceRot == 3) ? ((weightedHeightE < weightedHeightW) ? ((weightedHeightN2 >= weightedHeightS2) ? ((weightedHeightE < weightedHeightS2) ? 1 : 2) : ((weightedHeightE < weightedHeightN2) ? 1 : 0)) : ((weightedHeightN2 >= weightedHeightS2) ? ((weightedHeightW < weightedHeightS2) ? 3 : 2) : ((weightedHeightW < weightedHeightN2) ? 3 : 0))) : ((weightedHeightN < weightedHeightS) ? ((weightedHeightE2 >= weightedHeightW2) ? ((weightedHeightN >= weightedHeightW2) ? 3 : 0) : ((weightedHeightN >= weightedHeightE2) ? 1 : 0)) : ((weightedHeightE2 >= weightedHeightW2) ? ((weightedHeightS >= weightedHeightW2) ? 3 : 0) : ((weightedHeightS >= weightedHeightE2) ? 1 : 2))));
		return (4 + lowSide - entranceRot) % 4;
	}

	internal bool TryGenerateRuinAtSurface(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos startPos, string locationCode)
	{
		if (schematicDatas.Length == 0)
		{
			return false;
		}
		int num = rand.NextInt(schematicDatas.Length);
		int orient = rand.NextInt(4);
		BlockSchematicStructure schematic = schematicDatas[num][orient];
		schematic.Unpack(worldForCollectibleResolve.Api, orient);
		startPos = startPos.AddCopy(0, schematic.OffsetY, 0);
		if (schematic.EntranceRotation != -1)
		{
			orient = FindClearEntranceRotation(blockAccessor, schematicDatas[num], startPos);
			schematic = schematicDatas[num][orient];
			schematic.Unpack(worldForCollectibleResolve.Api, orient);
		}
		int wdthalf = (int)Math.Ceiling((float)schematic.SizeX / 2f);
		int lenhalf = (int)Math.Ceiling((float)schematic.SizeZ / 2f);
		int wdt = schematic.SizeX;
		int len = schematic.SizeZ;
		tmpPos.Set(startPos.X + wdthalf, 0, startPos.Z + lenhalf);
		int centerY = blockAccessor.GetTerrainMapheightAt(startPos);
		if (centerY < worldForCollectibleResolve.SeaLevel - MaxBelowSealevel)
		{
			return false;
		}
		tmpPos.Set(startPos.X, 0, startPos.Z);
		int topLeftY = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Set(startPos.X + wdt, 0, startPos.Z);
		int topRightY = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Set(startPos.X, 0, startPos.Z + len);
		int botLeftY = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Set(startPos.X + wdt, 0, startPos.Z + len);
		int botRightY = blockAccessor.GetTerrainMapheightAt(tmpPos);
		int maxY = GameMath.Max(centerY, topLeftY, topRightY, botLeftY, botRightY);
		int minY = GameMath.Min(centerY, topLeftY, topRightY, botLeftY, botRightY);
		if (schematic.SizeX >= 30)
		{
			int size3 = (int)((double)schematic.SizeX * 0.15 + 8.0);
			for (int j = size3; j < schematic.SizeX; j += size3)
			{
				tmpPos.Set(startPos.X + j, 0, startPos.Z);
				int topSide = blockAccessor.GetTerrainMapheightAt(tmpPos);
				tmpPos.Set(startPos.X + j, 0, startPos.Z + len);
				int botSide = blockAccessor.GetTerrainMapheightAt(tmpPos);
				tmpPos.Set(startPos.X + j, 0, startPos.Z + len / 2);
				int centerSide3 = blockAccessor.GetTerrainMapheightAt(tmpPos);
				maxY = GameMath.Max(maxY, topSide, botSide, centerSide3);
				minY = GameMath.Min(minY, topSide, botSide, centerSide3);
			}
		}
		else if (schematic.SizeX >= 15)
		{
			int size4 = schematic.SizeX / 2;
			tmpPos.Set(startPos.X + size4, 0, startPos.Z);
			int topSide2 = blockAccessor.GetTerrainMapheightAt(tmpPos);
			tmpPos.Set(startPos.X + size4, 0, startPos.Z + len);
			int botSide2 = blockAccessor.GetTerrainMapheightAt(tmpPos);
			tmpPos.Set(startPos.X + size4, 0, startPos.Z + len / 2);
			int centerSide4 = blockAccessor.GetTerrainMapheightAt(tmpPos);
			maxY = GameMath.Max(maxY, topSide2, botSide2, centerSide4);
			minY = GameMath.Min(minY, topSide2, botSide2, centerSide4);
		}
		if (schematic.SizeZ >= 30)
		{
			int size = (int)((double)schematic.SizeZ * 0.15 + 8.0);
			for (int i = size; i < schematic.SizeZ; i += size)
			{
				tmpPos.Set(startPos.X + wdt, 0, startPos.Z + i);
				int rightSide = blockAccessor.GetTerrainMapheightAt(tmpPos);
				tmpPos.Set(startPos.X, 0, startPos.Z + i);
				int leftSide = blockAccessor.GetTerrainMapheightAt(tmpPos);
				tmpPos.Set(startPos.X + wdt / 2, 0, startPos.Z + i);
				int centerSide = blockAccessor.GetTerrainMapheightAt(tmpPos);
				maxY = GameMath.Max(maxY, rightSide, leftSide, centerSide);
				minY = GameMath.Min(minY, rightSide, leftSide, centerSide);
			}
		}
		else if (schematic.SizeZ >= 15)
		{
			int size2 = schematic.SizeZ / 2;
			tmpPos.Set(startPos.X + wdt, 0, startPos.Z + size2);
			int rightSide2 = blockAccessor.GetTerrainMapheightAt(tmpPos);
			tmpPos.Set(startPos.X, 0, startPos.Z + size2);
			int leftSide2 = blockAccessor.GetTerrainMapheightAt(tmpPos);
			tmpPos.Set(startPos.X + wdt / 2, 0, startPos.Z + size2);
			int centerSide2 = blockAccessor.GetTerrainMapheightAt(tmpPos);
			maxY = GameMath.Max(maxY, rightSide2, leftSide2, centerSide2);
			minY = GameMath.Min(minY, rightSide2, leftSide2, centerSide2);
		}
		if (Math.Abs(maxY - minY) > schematic.MaxYDiff)
		{
			return false;
		}
		startPos.Y = minY + schematic.OffsetY;
		tmpPos.Set(startPos.X, startPos.Y + 1, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + wdt, startPos.Y + 1, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y + 1, startPos.Z + len);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + wdt, startPos.Y + 1, startPos.Z + len);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		startPos.Y++;
		if (!TestAboveGroundCheckPositions(blockAccessor, startPos, schematic.AbovegroundCheckPositions))
		{
			return false;
		}
		if (!SatisfiesMinDistance(startPos, worldForCollectibleResolve))
		{
			return false;
		}
		if (WouldOverlapAt(blockAccessor, startPos, schematic, locationCode))
		{
			return false;
		}
		LastPlacedSchematicLocation.Set(startPos.X, startPos.Y, startPos.Z, startPos.X + schematic.SizeX, startPos.Y + schematic.SizeY, startPos.Z + schematic.SizeZ);
		LastPlacedSchematic = schematic;
		schematic.PlaceRespectingBlockLayers(blockAccessor, worldForCollectibleResolve, startPos, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight, resolvedRockTypeRemaps, replacewithblocklayersBlockids, GenStructures.ReplaceMetaBlocks);
		return true;
	}

	internal bool TryGenerateAtSurface(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos startPos, string locationCode)
	{
		int num = rand.NextInt(schematicDatas.Length);
		int orient = rand.NextInt(4);
		BlockSchematicStructure schematic = schematicDatas[num][orient];
		schematic.Unpack(worldForCollectibleResolve.Api, orient);
		startPos = startPos.AddCopy(0, schematic.OffsetY, 0);
		if (schematic.EntranceRotation != -1)
		{
			orient = FindClearEntranceRotation(blockAccessor, schematicDatas[num], startPos);
			schematic = schematicDatas[num][orient];
			schematic.Unpack(worldForCollectibleResolve.Api, orient);
		}
		int wdthalf = (int)Math.Ceiling((float)schematic.SizeX / 2f);
		int lenhalf = (int)Math.Ceiling((float)schematic.SizeZ / 2f);
		int wdt = schematic.SizeX;
		int len = schematic.SizeZ;
		tmpPos.Set(startPos.X + wdthalf, 0, startPos.Z + lenhalf);
		int centerY = blockAccessor.GetTerrainMapheightAt(tmpPos);
		if (centerY < worldForCollectibleResolve.SeaLevel - MaxBelowSealevel)
		{
			return false;
		}
		tmpPos.Set(startPos.X, 0, startPos.Z);
		int topLeftY = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Set(startPos.X + wdt, 0, startPos.Z);
		int topRightY = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Set(startPos.X, 0, startPos.Z + len);
		int botLeftY = blockAccessor.GetTerrainMapheightAt(tmpPos);
		tmpPos.Set(startPos.X + wdt, 0, startPos.Z + len);
		int botRightY = blockAccessor.GetTerrainMapheightAt(tmpPos);
		if (GameMath.Max(centerY, topLeftY, topRightY, botLeftY, botRightY) - GameMath.Min(centerY, topLeftY, topRightY, botLeftY, botRightY) != 0)
		{
			return false;
		}
		startPos.Y = centerY + 1 + schematic.OffsetY;
		tmpPos.Set(startPos.X + wdthalf, startPos.Y - 1, startPos.Z + lenhalf);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y - 1, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + wdt, startPos.Y - 1, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y - 1, startPos.Z + len);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + wdt, startPos.Y - 1, startPos.Z + len);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + wdt, startPos.Y, startPos.Z + len);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + wdt, startPos.Y, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y, startPos.Z + len);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + wdt, startPos.Y, startPos.Z + len);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y + 1, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + wdt, startPos.Y + 1, startPos.Z);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X, startPos.Y + 1, startPos.Z + len);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(startPos.X + wdt, startPos.Y + 1, startPos.Z + len);
		if (blockAccessor.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		if (!SatisfiesMinDistance(startPos, worldForCollectibleResolve))
		{
			return false;
		}
		if (WouldOverlapAt(blockAccessor, startPos, schematic, locationCode))
		{
			return false;
		}
		LastPlacedSchematicLocation.Set(startPos.X, startPos.Y, startPos.Z, startPos.X + schematic.SizeX, startPos.Y + schematic.SizeY, startPos.Z + schematic.SizeZ);
		LastPlacedSchematic = schematic;
		schematic.PlaceRespectingBlockLayers(blockAccessor, worldForCollectibleResolve, startPos, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight, resolvedRockTypeRemaps, replacewithblocklayersBlockids, GenStructures.ReplaceMetaBlocks);
		return true;
	}

	internal bool TryGenerateUnderwater(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos pos, string locationCode)
	{
		return false;
	}

	internal bool TryGenerateUnderground(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos pos, string locationCode)
	{
		int num = rand.NextInt(schematicDatas.Length);
		BlockSchematicStructure[] schematicStruc = schematicDatas[num];
		BlockPos targetPos = pos.Copy();
		schematicStruc[0].Unpack(worldForCollectibleResolve.Api, 0);
		if (schematicStruc[0].PathwayStarts.Length != 0)
		{
			return tryGenerateAttachedToCave(blockAccessor, worldForCollectibleResolve, schematicStruc, targetPos, locationCode);
		}
		int orient = rand.NextInt(4);
		BlockSchematicStructure schematic = schematicStruc[orient];
		schematic.Unpack(worldForCollectibleResolve.Api, orient);
		BlockPos placePos = schematic.AdjustStartPos(targetPos.Copy(), Origin);
		LastPlacedSchematicLocation.Set(placePos.X, placePos.Y, placePos.Z, placePos.X + schematic.SizeX, placePos.Y + schematic.SizeY, placePos.Z + schematic.SizeZ);
		LastPlacedSchematic = schematic;
		if (insideblockids.Count > 0 && !insideblockids.Contains(blockAccessor.GetBlock(targetPos).Id))
		{
			return false;
		}
		if (!TestUndergroundCheckPositions(blockAccessor, placePos, schematic.UndergroundCheckPositions))
		{
			return false;
		}
		if (!SatisfiesMinDistance(pos, worldForCollectibleResolve))
		{
			return false;
		}
		if (WouldOverlapAt(blockAccessor, pos, schematic, locationCode))
		{
			return false;
		}
		if (resolvedRockTypeRemaps != null)
		{
			Block rockBlock = null;
			int i = 0;
			while (rockBlock == null && i < 10)
			{
				Block block = blockAccessor.GetBlock(placePos.X + rand.NextInt(schematic.SizeX), placePos.Y + rand.NextInt(schematic.SizeY), placePos.Z + rand.NextInt(schematic.SizeZ), 1);
				if (block.BlockMaterial == EnumBlockMaterial.Stone)
				{
					rockBlock = block;
				}
				i++;
			}
			schematic.PlaceReplacingBlocks(blockAccessor, worldForCollectibleResolve, placePos, schematic.ReplaceMode, resolvedRockTypeRemaps, rockBlock?.Id, GenStructures.ReplaceMetaBlocks);
		}
		else
		{
			schematic.Place(blockAccessor, worldForCollectibleResolve, targetPos);
		}
		return true;
	}

	private bool tryGenerateAttachedToCave(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockSchematicStructure[] schematicStruc, BlockPos targetPos, string locationCode)
	{
		Block rockBlock = null;
		Block block = blockAccessor.GetBlock(targetPos);
		if (block.Id != 0)
		{
			return false;
		}
		bool found = false;
		for (int dy = 0; dy <= 4; dy++)
		{
			targetPos.Down();
			block = blockAccessor.GetBlock(targetPos);
			if (block.BlockMaterial == EnumBlockMaterial.Stone)
			{
				rockBlock = block;
				targetPos.Up();
				found = true;
				break;
			}
		}
		if (!found)
		{
			return false;
		}
		BlockSchematicStructure schematic = schematicStruc[0];
		schematic.Unpack(worldForCollectibleResolve.Api, 0);
		int pathwayNum = rand.NextInt(schematic.PathwayStarts.Length);
		int targetDistance = -1;
		BlockFacing targetFacing = null;
		BlockPos[] pathway = null;
		for (int targetOrientation = 0; targetOrientation < 4; targetOrientation++)
		{
			schematic = schematicStruc[targetOrientation];
			schematic.Unpack(worldForCollectibleResolve.Api, targetOrientation);
			pathway = schematic.PathwayOffsets[pathwayNum];
			targetFacing = schematic.PathwaySides[pathwayNum];
			targetDistance = CanPlacePathwayAt(blockAccessor, pathway, targetFacing, targetPos);
			if (targetDistance != -1)
			{
				break;
			}
		}
		if (targetDistance == -1)
		{
			return false;
		}
		BlockPos pathwayStart = schematic.PathwayStarts[pathwayNum];
		targetPos.Add(-pathwayStart.X - targetFacing.Normali.X * targetDistance, -pathwayStart.Y - targetFacing.Normali.Y * targetDistance + schematic.OffsetY, -pathwayStart.Z - targetFacing.Normali.Z * targetDistance);
		if (targetPos.Y <= 0)
		{
			return false;
		}
		if (!TestUndergroundCheckPositions(blockAccessor, targetPos, schematic.UndergroundCheckPositions))
		{
			return false;
		}
		if (WouldOverlapAt(blockAccessor, targetPos, schematic, locationCode))
		{
			return false;
		}
		LastPlacedSchematicLocation.Set(targetPos.X, targetPos.Y, targetPos.Z, targetPos.X + schematic.SizeX, targetPos.Y + schematic.SizeY, targetPos.Z + schematic.SizeZ);
		LastPlacedSchematic = schematic;
		if (resolvedRockTypeRemaps != null)
		{
			schematic.PlaceReplacingBlocks(blockAccessor, worldForCollectibleResolve, targetPos, schematic.ReplaceMode, resolvedRockTypeRemaps, rockBlock.Id, GenStructures.ReplaceMetaBlocks);
		}
		else
		{
			schematic.Place(blockAccessor, worldForCollectibleResolve, targetPos);
		}
		ushort blockId = 0;
		for (int i = 0; i < pathway.Length; i++)
		{
			for (int d = 0; d <= targetDistance; d++)
			{
				tmpPos.Set(targetPos.X + pathwayStart.X + pathway[i].X + (d + 1) * targetFacing.Normali.X, targetPos.Y + pathwayStart.Y + pathway[i].Y + (d + 1) * targetFacing.Normali.Y, targetPos.Z + pathwayStart.Z + pathway[i].Z + (d + 1) * targetFacing.Normali.Z);
				blockAccessor.SetBlock(blockId, tmpPos);
			}
		}
		return true;
	}

	private bool TestUndergroundCheckPositions(IBlockAccessor blockAccessor, BlockPos pos, BlockPos[] testPositionsDelta)
	{
		foreach (BlockPos deltapos in testPositionsDelta)
		{
			utestPos.Set(pos.X + deltapos.X, pos.Y + deltapos.Y, pos.Z + deltapos.Z);
			if (blockAccessor.GetBlock(utestPos).BlockMaterial != EnumBlockMaterial.Stone)
			{
				return false;
			}
		}
		return true;
	}

	private bool TestAboveGroundCheckPositions(IBlockAccessor blockAccessor, BlockPos pos, BlockPos[] testPositionsDelta)
	{
		foreach (BlockPos deltapos in testPositionsDelta)
		{
			utestPos.Set(pos.X + deltapos.X, pos.Y + deltapos.Y, pos.Z + deltapos.Z);
			int height = blockAccessor.GetTerrainMapheightAt(utestPos);
			if (utestPos.Y <= height)
			{
				return false;
			}
		}
		return true;
	}

	private int CanPlacePathwayAt(IBlockAccessor blockAccessor, BlockPos[] pathway, BlockFacing towardsFacing, BlockPos targetPos)
	{
		BlockPos tmpPos = new BlockPos();
		bool oppositeDir = rand.NextInt(2) > 0;
		for (int i = 3; i >= 1; i--)
		{
			int dist = (oppositeDir ? (3 - i) : i);
			int dx = dist * towardsFacing.Normali.X;
			int dz = dist * towardsFacing.Normali.Z;
			int quantityAir = 0;
			for (int j = 0; j < pathway.Length; j++)
			{
				tmpPos.Set(targetPos.X + pathway[j].X + dx, targetPos.Y + pathway[j].Y, targetPos.Z + pathway[j].Z + dz);
				Block block = blockAccessor.GetBlock(tmpPos);
				if (block.Id == 0)
				{
					quantityAir++;
				}
				else if (block.BlockMaterial != EnumBlockMaterial.Stone)
				{
					return -1;
				}
			}
			if (quantityAir > 0 && quantityAir < pathway.Length)
			{
				return dist;
			}
		}
		return -1;
	}

	private bool WouldOverlapAt(IBlockAccessor blockAccessor, BlockPos pos, BlockSchematicStructure schematic, string locationCode)
	{
		int regSize = blockAccessor.RegionSize;
		int mapRegionSizeX = blockAccessor.MapSizeX / regSize;
		int mapRegionSizeZ = blockAccessor.MapSizeZ / regSize;
		int num = GameMath.Clamp(pos.X / regSize, 0, mapRegionSizeX);
		int minrz = GameMath.Clamp(pos.Z / regSize, 0, mapRegionSizeZ);
		int maxrx = GameMath.Clamp((pos.X + schematic.SizeX) / regSize, 0, mapRegionSizeX);
		int maxrz = GameMath.Clamp((pos.Z + schematic.SizeZ) / regSize, 0, mapRegionSizeZ);
		tmpLoc.Set(pos.X, pos.Y, pos.Z, pos.X + schematic.SizeX, pos.Y + schematic.SizeY, pos.Z + schematic.SizeZ);
		for (int rx = num; rx <= maxrx; rx++)
		{
			for (int rz = minrz; rz <= maxrz; rz++)
			{
				IMapRegion mapregion = blockAccessor.GetMapRegion(rx, rz);
				if (mapregion == null)
				{
					continue;
				}
				foreach (GeneratedStructure generatedStructure in mapregion.GeneratedStructures)
				{
					if (generatedStructure.Location.Intersects(tmpLoc))
					{
						return true;
					}
				}
			}
		}
		if (genStructuresSys.WouldSchematicOverlapAt(blockAccessor, pos, tmpLoc, locationCode))
		{
			return true;
		}
		return false;
	}

	public bool SatisfiesMinDistance(BlockPos pos, IWorldAccessor world)
	{
		return SatisfiesMinDistance(pos, world, MinGroupDistance, Group);
	}

	public static bool SatisfiesMinDistance(BlockPos pos, IWorldAccessor world, int mingroupDistance, string group)
	{
		if (mingroupDistance < 1)
		{
			return true;
		}
		int regSize = world.BlockAccessor.RegionSize;
		int mapRegionSizeX = world.BlockAccessor.MapSizeX / regSize;
		int mapRegionSizeZ = world.BlockAccessor.MapSizeZ / regSize;
		int num = pos.X - mingroupDistance;
		int z1 = pos.Z - mingroupDistance;
		int x2 = pos.X + mingroupDistance;
		int z2 = pos.Z + mingroupDistance;
		long minDistSq = (long)mingroupDistance * (long)mingroupDistance;
		int num2 = GameMath.Clamp(num / regSize, 0, mapRegionSizeX);
		int minrz = GameMath.Clamp(z1 / regSize, 0, mapRegionSizeZ);
		int maxrx = GameMath.Clamp(x2 / regSize, 0, mapRegionSizeX);
		int maxrz = GameMath.Clamp(z2 / regSize, 0, mapRegionSizeZ);
		for (int rx = num2; rx <= maxrx; rx++)
		{
			for (int rz = minrz; rz <= maxrz; rz++)
			{
				IMapRegion mapregion = world.BlockAccessor.GetMapRegion(rx, rz);
				if (mapregion == null)
				{
					continue;
				}
				foreach (GeneratedStructure val in mapregion.GeneratedStructures)
				{
					if (val.Group == group && val.Location.Center.SquareDistanceTo(pos.X, pos.Y, pos.Z) < minDistSq)
					{
						return false;
					}
				}
			}
		}
		return true;
	}
}
