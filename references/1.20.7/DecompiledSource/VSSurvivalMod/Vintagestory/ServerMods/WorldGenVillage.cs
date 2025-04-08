using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class WorldGenVillage
{
	[JsonProperty]
	public string Code;

	[JsonProperty]
	public string Name;

	[JsonProperty]
	public string Group;

	[JsonProperty]
	public int MinGroupDistance;

	[JsonProperty]
	public VillageSchematic[] Schematics;

	[JsonProperty]
	public float Chance = 0.05f;

	[JsonProperty]
	public NatFloat QuantityStructures = NatFloat.createGauss(7f, 7f);

	[JsonProperty]
	public AssetLocation[] ReplaceWithBlocklayers;

	[JsonProperty]
	public bool BuildProtected;

	[JsonProperty]
	public bool PostPass;

	[JsonProperty]
	public string BuildProtectionDesc;

	[JsonProperty]
	public string BuildProtectionName;

	[JsonProperty]
	public Dictionary<AssetLocation, AssetLocation> RockTypeRemaps;

	[JsonProperty]
	public string RockTypeRemapGroup;

	[JsonProperty]
	public int MaxYDiff = 3;

	internal int[] replaceblockids = new int[0];

	internal Dictionary<int, Dictionary<int, int>> resolvedRockTypeRemaps;

	private LCGRandom rand;

	public void Init(ICoreServerAPI api, BlockLayerConfig blockLayerConfig, WorldGenStructuresConfig structureConfig, Dictionary<string, Dictionary<int, Dictionary<int, int>>> resolvedRocktypeRemapGroups, Dictionary<string, int> schematicYOffsets, int? defaultOffsetY, RockStrataConfig rockstrata, LCGRandom rand)
	{
		this.rand = rand;
		for (int j = 0; j < Schematics.Length; j++)
		{
			List<BlockSchematicStructure> schematics = new List<BlockSchematicStructure>();
			VillageSchematic schem = Schematics[j];
			IAsset[] assets = ((!schem.Path.EndsWith('*')) ? new IAsset[1] { api.Assets.Get("worldgen/schematics/" + Schematics[j].Path + ".json") } : api.Assets.GetManyInCategory("worldgen", "schematics/" + schem.Path.Substring(0, schem.Path.Length - 1)).ToArray());
			for (int k = 0; k < assets.Length; k++)
			{
				int offsety = WorldGenStructureBase.getOffsetY(schematicYOffsets, defaultOffsetY, assets[k]);
				BlockSchematicStructure[] sch = WorldGenStructureBase.LoadSchematic<BlockSchematicStructure>(api, assets[k], blockLayerConfig, structureConfig, null, offsety);
				if (sch != null)
				{
					schematics.AddRange(sch);
				}
			}
			schem.Structures = schematics.ToArray();
			if (schem.Structures.Length == 0)
			{
				throw new Exception($"villages.json, village with code {Code} has a schematic definition at index {j} that resolves into zero schematics. Please fix or remove this entry");
			}
		}
		if (ReplaceWithBlocklayers != null)
		{
			replaceblockids = new int[ReplaceWithBlocklayers.Length];
			for (int i = 0; i < replaceblockids.Length; i++)
			{
				Block block = api.World.GetBlock(ReplaceWithBlocklayers[i]);
				if (block == null)
				{
					throw new Exception($"Schematic with code {Code} has replace block layer {ReplaceWithBlocklayers[i]} defined, but no such block found!");
				}
				replaceblockids[i] = (ushort)block.Id;
			}
		}
		if (RockTypeRemapGroup != null)
		{
			resolvedRockTypeRemaps = resolvedRocktypeRemapGroups[RockTypeRemapGroup];
		}
		if (RockTypeRemaps != null)
		{
			resolvedRockTypeRemaps = WorldGenStructuresConfigBase.ResolveRockTypeRemaps(RockTypeRemaps, rockstrata, api);
		}
	}

	public bool TryGenerate(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos pos, int climateUpLeft, int climateUpRight, int climateBotLeft, int climateBotRight, DidGenerate didGenerateStructure, BlockPos spawnPos)
	{
		if (!WorldGenStructure.SatisfiesMinDistance(pos, worldForCollectibleResolve, MinGroupDistance, Group))
		{
			return false;
		}
		rand.InitPositionSeed(pos.X, pos.Z);
		float cnt = QuantityStructures.nextFloat(1f, rand);
		int minQuantity = (int)cnt;
		BlockPos botCenterPos = pos.Copy();
		Cuboidi location = new Cuboidi();
		List<GeneratableStructure> generatables = new List<GeneratableStructure>();
		List<VillageSchematic> mustGenerate = new List<VillageSchematic>();
		List<VillageSchematic> canGenerate = new List<VillageSchematic>();
		for (int j = 0; j < Schematics.Length; j++)
		{
			VillageSchematic schem2 = Schematics[j];
			schem2.NowQuantity = 0;
			if (schem2.MinQuantity > 0)
			{
				for (int k = 0; k < schem2.MinQuantity; k++)
				{
					mustGenerate.Add(schem2);
				}
			}
			if (schem2.MaxQuantity > schem2.MinQuantity)
			{
				canGenerate.Add(schem2);
			}
		}
		while (cnt-- > 0f && (!(cnt < 1f) || !(rand.NextFloat() > cnt)))
		{
			int tries = 30;
			int dr = 0;
			double totalWeight = getTotalWeight(canGenerate);
			while (tries-- > 0)
			{
				int r = Math.Min(16 + dr++ / 2, 24);
				botCenterPos.Set(pos);
				botCenterPos.Add(rand.NextInt(2 * r) - r, 0, rand.NextInt(2 * r) - r);
				botCenterPos.Y = blockAccessor.GetTerrainMapheightAt(botCenterPos);
				if (botCenterPos.Y == 0)
				{
					continue;
				}
				VillageSchematic schem = null;
				bool genRequired = mustGenerate.Count > 0;
				if (genRequired)
				{
					schem = mustGenerate[mustGenerate.Count - 1];
				}
				else
				{
					double rndVal = rand.NextDouble() * totalWeight;
					int i = 0;
					while (rndVal > 0.0)
					{
						schem = canGenerate[i++];
						if (schem.ShouldGenerate)
						{
							rndVal -= schem.Weight;
						}
					}
				}
				if (!BlockSchematicStructure.SatisfiesMinSpawnDistance(schem.MinSpawnDistance, pos, spawnPos))
				{
					if (genRequired)
					{
						break;
					}
					continue;
				}
				int randomIndex = rand.NextInt(schem.Structures.Length);
				BlockSchematicStructure struc = schem.Structures[randomIndex];
				location.Set(botCenterPos.X - struc.SizeX / 2, botCenterPos.Y, botCenterPos.Z - struc.SizeZ / 2, botCenterPos.X + (int)Math.Ceiling((float)struc.SizeX / 2f), botCenterPos.Y + struc.SizeY, botCenterPos.Z + (int)Math.Ceiling((float)struc.SizeZ / 2f));
				bool intersect = false;
				for (int l = 0; l < generatables.Count; l++)
				{
					if (location.IntersectsOrTouches(generatables[l].Location))
					{
						intersect = true;
						break;
					}
				}
				if (intersect)
				{
					continue;
				}
				struc.Unpack(worldForCollectibleResolve.Api, randomIndex % 4);
				if (CanGenerateStructureAt(struc, blockAccessor, location))
				{
					if (genRequired)
					{
						mustGenerate.RemoveAt(mustGenerate.Count - 1);
					}
					schem.NowQuantity++;
					generatables.Add(new GeneratableStructure
					{
						Structure = struc,
						StartPos = location.Start.AsBlockPos,
						Location = location.Clone()
					});
					tries = 0;
				}
			}
		}
		if (generatables.Count >= minQuantity && mustGenerate.Count == 0)
		{
			foreach (GeneratableStructure val in generatables)
			{
				val.Structure.PlaceRespectingBlockLayers(blockAccessor, worldForCollectibleResolve, val.StartPos, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight, resolvedRockTypeRemaps, replaceblockids, GenStructures.ReplaceMetaBlocks);
				didGenerateStructure(val.Location, val.Structure);
			}
			return true;
		}
		return false;
	}

	private double getTotalWeight(List<VillageSchematic> canGenerate)
	{
		double weight = 0.0;
		for (int i = 0; i < canGenerate.Count; i++)
		{
			VillageSchematic schem = canGenerate[i];
			if (schem.ShouldGenerate)
			{
				weight += schem.Weight;
			}
		}
		return weight;
	}

	protected bool CanGenerateStructureAt(BlockSchematicStructure schematic, IBlockAccessor ba, Cuboidi location)
	{
		BlockPos centerPos = new BlockPos(location.CenterX, location.Y1 + schematic.OffsetY, location.CenterZ);
		BlockPos tmpPos = new BlockPos();
		int topLeftY = ba.GetTerrainMapheightAt(tmpPos.Set(location.X1, 0, location.Z1));
		int topRightY = ba.GetTerrainMapheightAt(tmpPos.Set(location.X2, 0, location.Z1));
		int botLeftY = ba.GetTerrainMapheightAt(tmpPos.Set(location.X1, 0, location.Z2));
		int botRightY = ba.GetTerrainMapheightAt(tmpPos.Set(location.X2, 0, location.Z2));
		int centerY = location.Y1;
		int highestY = GameMath.Max(centerY, topLeftY, topRightY, botLeftY, botRightY);
		int lowestY = GameMath.Min(centerY, topLeftY, topRightY, botLeftY, botRightY);
		if (highestY - lowestY > 2)
		{
			return false;
		}
		location.Y1 = lowestY + schematic.OffsetY + 1;
		location.Y2 = location.Y1 + schematic.SizeY;
		if (!testUndergroundCheckPositions(ba, location.Start.AsBlockPos, schematic.UndergroundCheckPositions))
		{
			return false;
		}
		tmpPos.Set(location.X1, centerPos.Y - 1, location.Z1);
		if (ba.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(tmpPos.Up(), 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(tmpPos.Up(), 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(location.X2, centerPos.Y - 1, location.Z1);
		if (ba.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(tmpPos.Up(), 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(tmpPos.Up(), 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(location.X1, centerPos.Y - 1, location.Z2);
		if (ba.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(tmpPos.Up(), 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(tmpPos.Up(), 2).IsLiquid())
		{
			return false;
		}
		tmpPos.Set(location.X2, centerPos.Y - 1, location.Z2);
		if (ba.GetBlock(tmpPos, 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(tmpPos.Up(), 2).IsLiquid())
		{
			return false;
		}
		if (ba.GetBlock(tmpPos.Up(), 2).IsLiquid())
		{
			return false;
		}
		if (overlapsExistingStructure(ba, location))
		{
			return false;
		}
		return true;
	}

	protected bool testUndergroundCheckPositions(IBlockAccessor blockAccessor, BlockPos pos, BlockPos[] testPositionsDelta)
	{
		int posX = pos.X;
		int posY = pos.Y;
		int posZ = pos.Z;
		foreach (BlockPos deltapos in testPositionsDelta)
		{
			pos.Set(posX + deltapos.X, posY + deltapos.Y, posZ + deltapos.Z);
			EnumBlockMaterial material = blockAccessor.GetBlock(pos, 1).BlockMaterial;
			if (material != EnumBlockMaterial.Stone && material != EnumBlockMaterial.Soil)
			{
				return false;
			}
		}
		return true;
	}

	protected bool overlapsExistingStructure(IBlockAccessor ba, Cuboidi cuboid)
	{
		int regsize = ba.RegionSize;
		IMapRegion mapregion = ba.GetMapRegion(cuboid.CenterX / regsize, cuboid.CenterZ / regsize);
		if (mapregion == null)
		{
			return false;
		}
		foreach (GeneratedStructure generatedStructure in mapregion.GeneratedStructures)
		{
			if (generatedStructure.Location.Intersects(cuboid))
			{
				return true;
			}
		}
		return false;
	}
}
