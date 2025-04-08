using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common.Collectible.Block;

namespace Vintagestory.ServerMods;

public abstract class WorldGenStructureBase
{
	[JsonProperty]
	public string Code;

	[JsonProperty]
	public string Name;

	[JsonProperty]
	public AssetLocation[] Schematics;

	[JsonProperty]
	public EnumStructurePlacement Placement;

	[JsonProperty]
	public NatFloat Depth;

	[JsonProperty]
	public bool BuildProtected;

	[JsonProperty]
	public string BuildProtectionDesc;

	[JsonProperty]
	public string BuildProtectionName;

	[JsonProperty]
	public string RockTypeRemapGroup;

	[JsonProperty]
	public Dictionary<AssetLocation, AssetLocation> RockTypeRemaps;

	[JsonProperty]
	public AssetLocation[] InsideBlockCodes;

	[JsonProperty]
	public EnumOrigin Origin;

	[JsonProperty]
	public int? OffsetY;

	[JsonProperty]
	public int MaxYDiff = 3;

	[JsonProperty]
	public int? StoryLocationMaxAmount;

	[JsonProperty]
	public int MinSpawnDistance;

	[JsonProperty]
	public int MaxBelowSealevel = 20;

	public const uint PosBitMask = 1023u;

	protected T[][] LoadSchematicsWithRotations<T>(ICoreAPI api, WorldGenStructureBase struc, BlockLayerConfig config, WorldGenStructuresConfig structureConfig, Dictionary<string, int> schematicYOffsets, string pathPrefix = "schematics/", bool isDungeon = false) where T : BlockSchematicStructure
	{
		List<T[]> schematics = new List<T[]>();
		for (int i = 0; i < struc.Schematics.Length; i++)
		{
			AssetLocation schematicLoc = Schematics[i];
			IAsset[] assets = ((!struc.Schematics[i].Path.EndsWith('*')) ? new IAsset[1] { api.Assets.Get(schematicLoc.Clone().WithPathPrefixOnce("worldgen/" + pathPrefix).WithPathAppendixOnce(".json")) } : api.Assets.GetManyInCategory("worldgen", pathPrefix + schematicLoc.Path.Substring(0, schematicLoc.Path.Length - 1), schematicLoc.Domain).ToArray());
			foreach (IAsset asset in assets)
			{
				int offsety = getOffsetY(schematicYOffsets, struc.OffsetY, asset);
				T[] sch = LoadSchematic<T>(api, asset, config, structureConfig, struc, offsety, isDungeon);
				if (sch != null)
				{
					schematics.Add(sch);
				}
			}
		}
		return schematics.ToArray();
	}

	public static int getOffsetY(Dictionary<string, int> schematicYOffsets, int? defaultOffsetY, IAsset asset)
	{
		string assloc = asset.Location.PathOmittingPrefixAndSuffix("worldgen/schematics/", ".json");
		int offsety = 0;
		if ((schematicYOffsets == null || !schematicYOffsets.TryGetValue(assloc, out offsety)) && defaultOffsetY.HasValue)
		{
			offsety = defaultOffsetY.Value;
		}
		return offsety;
	}

	public static T[] LoadSchematic<T>(ICoreAPI api, IAsset asset, BlockLayerConfig config, WorldGenStructuresConfig structureConfig, WorldGenStructureBase? struc, int offsety, bool isDungeon = false) where T : BlockSchematicStructure
	{
		string cacheKey = asset.Location.ToShortString() + "~" + offsety;
		if (structureConfig != null && structureConfig.LoadedSchematicsCache.TryGetValue(cacheKey, out var cached) && cached is T[] result)
		{
			return result;
		}
		T schematic = asset.ToObject<T>();
		schematic.Remap();
		if (isDungeon)
		{
			InitDungeonData(api, schematic);
		}
		if (schematic == null)
		{
			api.World.Logger.Warning("Could not load schematic {0}", asset.Location);
			if (structureConfig != null)
			{
				structureConfig.LoadedSchematicsCache[cacheKey] = null;
			}
			return null;
		}
		schematic.OffsetY = offsety;
		schematic.FromFileName = asset.Name;
		schematic.MaxYDiff = struc?.MaxYDiff ?? 3;
		schematic.MaxBelowSealevel = struc?.MaxBelowSealevel ?? 3;
		schematic.StoryLocationMaxAmount = struc?.StoryLocationMaxAmount;
		T[] rotations = new T[4] { schematic, null, null, null };
		for (int i = 0; i < 4; i++)
		{
			if (i > 0)
			{
				T unrotated = rotations[0];
				rotations[i] = unrotated.ClonePacked() as T;
				if (isDungeon)
				{
					List<BlockPosFacing> pathways = (rotations[i].PathwayBlocksUnpacked = new List<BlockPosFacing>());
					List<BlockPosFacing> pathwaysSource = unrotated.PathwayBlocksUnpacked;
					for (int index = 0; index < pathwaysSource.Count; index++)
					{
						BlockPosFacing path = pathwaysSource[index];
						BlockPos rotatedPos = unrotated.GetRotatedPos(EnumOrigin.BottomCenter, i * 90, path.Position.X, path.Position.Y, path.Position.Z);
						pathways.Add(new BlockPosFacing(rotatedPos, path.Facing.GetHorizontalRotated(i * 90), path.Constraints));
					}
				}
			}
			rotations[i].blockLayerConfig = config;
		}
		if (structureConfig != null)
		{
			Dictionary<string, BlockSchematicStructure[]> loadedSchematicsCache = structureConfig.LoadedSchematicsCache;
			BlockSchematicStructure[] value = rotations;
			loadedSchematicsCache[cacheKey] = value;
		}
		return rotations;
	}

	private static void InitDungeonData(ICoreAPI api, BlockSchematicStructure schematic)
	{
		bool hasX = false;
		bool hasZ = false;
		bool hasXO = false;
		bool hasZO = false;
		int pathwayBlockId = schematic.BlockCodes.First((KeyValuePair<int, AssetLocation> s) => s.Value.Path.Equals("meta-connector")).Key;
		schematic.PathwayBlocksUnpacked = new List<BlockPosFacing>();
		List<int> listIndex = new List<int>();
		for (int m = 0; m < schematic.Indices.Count; m++)
		{
			uint index2 = schematic.Indices[m];
			int dx4 = (int)(index2 & 0x3FF);
			int dy4 = (int)((index2 >> 20) & 0x3FF);
			int dz4 = (int)((index2 >> 10) & 0x3FF);
			if (dx4 == 0 && schematic.BlockIds[m] == pathwayBlockId)
			{
				hasX = true;
				string constraint4 = ExtractDungeonPathConstraint(schematic, index2);
				schematic.PathwayBlocksUnpacked.Add(new BlockPosFacing(new BlockPos(dx4, dy4, dz4), BlockFacing.WEST, constraint4));
				listIndex.Add(m);
			}
			if (dz4 == 0 && dx4 != 0 && schematic.BlockIds[m] == pathwayBlockId)
			{
				hasZ = true;
				string constraint3 = ExtractDungeonPathConstraint(schematic, index2);
				schematic.PathwayBlocksUnpacked.Add(new BlockPosFacing(new BlockPos(dx4, dy4, dz4), BlockFacing.NORTH, constraint3));
				listIndex.Add(m);
			}
			if (dx4 == schematic.SizeX - 1 && schematic.BlockIds[m] == pathwayBlockId)
			{
				hasXO = true;
				string constraint2 = ExtractDungeonPathConstraint(schematic, index2);
				schematic.PathwayBlocksUnpacked.Add(new BlockPosFacing(new BlockPos(dx4, dy4, dz4), BlockFacing.EAST, constraint2));
				listIndex.Add(m);
			}
			if (dz4 == schematic.SizeZ - 1 && dx4 != schematic.SizeX - 1 && schematic.BlockIds[m] == pathwayBlockId)
			{
				hasZO = true;
				string constraint = ExtractDungeonPathConstraint(schematic, index2);
				schematic.PathwayBlocksUnpacked.Add(new BlockPosFacing(new BlockPos(dx4, dy4, dz4), BlockFacing.SOUTH, constraint));
				listIndex.Add(m);
			}
		}
		listIndex.Reverse();
		foreach (int l in listIndex)
		{
			schematic.Indices.RemoveAt(l);
			schematic.BlockIds.RemoveAt(l);
		}
		if (hasXO)
		{
			schematic.SizeX--;
		}
		if (hasZO)
		{
			schematic.SizeZ--;
		}
		if (hasX)
		{
			schematic.SizeX--;
		}
		if (hasZ)
		{
			schematic.SizeZ--;
		}
		for (int k = 0; k < schematic.Indices.Count; k++)
		{
			if (hasX || hasZ)
			{
				uint num = schematic.Indices[k];
				int dx3 = (int)(num & 0x3FF);
				int dy3 = (int)((num >> 20) & 0x3FF);
				int dz3 = (int)((num >> 10) & 0x3FF);
				if (hasX)
				{
					dx3--;
				}
				if (hasZ)
				{
					dz3--;
				}
				schematic.Indices[k] = (uint)((dy3 << 20) | (dz3 << 10) | dx3);
			}
		}
		for (int j = 0; j < schematic.DecorIndices.Count; j++)
		{
			if (hasX || hasZ)
			{
				uint num2 = schematic.DecorIndices[j];
				int dx2 = (int)(num2 & 0x3FF);
				int dy2 = (int)((num2 >> 20) & 0x3FF);
				int dz2 = (int)((num2 >> 10) & 0x3FF);
				if (hasX)
				{
					dx2--;
				}
				if (hasZ)
				{
					dz2--;
				}
				schematic.DecorIndices[j] = (uint)((dy2 << 20) | (dz2 << 10) | dx2);
			}
		}
		Dictionary<uint, string> tmpBlockEntities = new Dictionary<uint, string>();
		foreach (var (index, data) in schematic.BlockEntities)
		{
			if (hasX || hasZ)
			{
				int dx = (int)(index & 0x3FF);
				int dy = (int)((index >> 20) & 0x3FF);
				int dz = (int)((index >> 10) & 0x3FF);
				if (hasX)
				{
					dx--;
				}
				if (hasZ)
				{
					dz--;
				}
				tmpBlockEntities[(uint)((dy << 20) | (dz << 10) | dx)] = data;
			}
		}
		schematic.BlockEntities = tmpBlockEntities;
		schematic.EntitiesUnpacked.Clear();
		foreach (string entity3 in schematic.Entities)
		{
			using MemoryStream input = new MemoryStream(Ascii85.Decode(entity3));
			BinaryReader reader = new BinaryReader(input);
			string className = reader.ReadString();
			Entity entity2 = api.ClassRegistry.CreateEntity(className);
			entity2.FromBytes(reader, isSync: false);
			if (hasX)
			{
				entity2.ServerPos.X--;
				entity2.Pos.X--;
				entity2.PositionBeforeFalling.X -= 1.0;
			}
			if (hasZ)
			{
				entity2.ServerPos.Z--;
				entity2.Pos.Z--;
				entity2.PositionBeforeFalling.Z -= 1.0;
			}
			schematic.EntitiesUnpacked.Add(entity2);
		}
		schematic.Entities.Clear();
		if (schematic.EntitiesUnpacked.Count > 0)
		{
			using FastMemoryStream ms = new FastMemoryStream();
			foreach (Entity entity in schematic.EntitiesUnpacked)
			{
				ms.Reset();
				BinaryWriter writer = new BinaryWriter(ms);
				writer.Write(api.ClassRegistry.GetEntityClassName(entity.GetType()));
				entity.ToBytes(writer, forClient: false);
				schematic.Entities.Add(Ascii85.Encode(ms.ToArray()));
			}
		}
		for (int i = 0; i < schematic.PathwayBlocksUnpacked.Count; i++)
		{
			BlockPosFacing path = schematic.PathwayBlocksUnpacked[i];
			int posx = 0;
			int posz = 0;
			if (hasX && path.Position.X > 0)
			{
				posx--;
			}
			if (hasZ && path.Position.Z > 0)
			{
				posz--;
			}
			if (hasXO && path.Position.X >= schematic.SizeX)
			{
				posx--;
			}
			if (hasZO && path.Position.Z >= schematic.SizeZ)
			{
				posz--;
			}
			path.Position.X += posx;
			path.Position.Z += posz;
		}
	}

	private static string ExtractDungeonPathConstraint(BlockSchematicStructure schematic, uint index)
	{
		string beData = schematic.BlockEntities[index];
		string value = (schematic.DecodeBlockEntityData(beData)["constraints"] as StringAttribute).value;
		schematic.BlockEntities.Remove(index);
		return value;
	}

	public T[] LoadSchematics<T>(ICoreAPI api, AssetLocation[] locs, BlockLayerConfig config, string pathPrefix = "schematics/") where T : BlockSchematicStructure
	{
		List<T> schematics = new List<T>();
		for (int i = 0; i < locs.Length; i++)
		{
			string error = "";
			AssetLocation schematicLoc = Schematics[i];
			IAsset[] assets = ((!locs[i].Path.EndsWith('*')) ? new IAsset[1] { api.Assets.Get(schematicLoc.Clone().WithPathPrefixOnce("worldgen/" + pathPrefix).WithPathAppendixOnce(".json")) } : api.Assets.GetManyInCategory("worldgen", pathPrefix + schematicLoc.Path.Substring(0, schematicLoc.Path.Length - 1), schematicLoc.Domain).ToArray());
			foreach (IAsset asset in assets)
			{
				T schematic = asset.ToObject<T>();
				if (schematic == null)
				{
					api.World.Logger.Warning("Could not load {0}: {1}", Schematics[i], error);
				}
				else
				{
					schematic.FromFileName = asset.Name;
					schematics.Add(schematic);
				}
			}
		}
		return schematics.ToArray();
	}
}
