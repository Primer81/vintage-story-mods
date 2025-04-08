using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class GenStoryStructures : ModStdWorldGen
{
	private WorldGenStoryStructuresConfig scfg;

	private LCGRandom strucRand;

	private LCGRandom grassRand;

	public OrderedDictionary<string, StoryStructureLocation> storyStructureInstances = new OrderedDictionary<string, StoryStructureLocation>();

	public bool StoryStructureInstancesDirty;

	private IWorldGenBlockAccessor worldgenBlockAccessor;

	private ICoreServerAPI api;

	private bool genStoryStructures;

	public BlockLayerConfig blockLayerConfig;

	private Cuboidi tmpCuboid = new Cuboidi();

	private int mapheight;

	private ClampedSimplexNoise grassDensity;

	private ClampedSimplexNoise grassHeight;

	public SimplexNoise distort2dx;

	public SimplexNoise distort2dz;

	private bool FailedToGenerateLocation;

	private IServerNetworkChannel serverChannel;

	public override double ExecuteOrder()
	{
		return 0.2;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		base.StartServerSide(api);
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.PlayerJoin += OnPlayerJoined;
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.InitWorldGenerator(InitWorldGen, "standard");
			api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Vegetation, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
			api.Event.WorldgenHook(GenerateHookStructure, "standard", "genHookStructure");
		}
		api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, delegate
		{
			if (genStoryStructures)
			{
				api.ChatCommands.GetOrCreate("wgen").BeginSubCommand("story").BeginSubCommand("tp")
					.WithRootAlias("tpstoryloc")
					.WithDescription("Teleport to a story structure instance")
					.RequiresPrivilege(Privilege.controlserver)
					.RequiresPlayer()
					.WithArgs(api.ChatCommands.Parsers.WordRange("code", scfg.Structures.Select((WorldGenStoryStructure s) => s.Code).ToArray()))
					.HandleWith(OnTpStoryLoc)
					.EndSubCommand()
					.EndSubCommand();
			}
		});
		api.ChatCommands.GetOrCreate("wgen").BeginSubCommand("story").BeginSubCommand("setpos")
			.WithRootAlias("setstorystrucpos")
			.WithDescription("Set the location of a story structure")
			.RequiresPrivilege(Privilege.controlserver)
			.WithArgs(api.ChatCommands.Parsers.Word("code"), api.ChatCommands.Parsers.WorldPosition("position"), api.ChatCommands.Parsers.OptionalBool("confirm"))
			.HandleWith(OnSetStoryStructurePos)
			.EndSubCommand()
			.BeginSubCommand("removeschematiccount")
			.WithAlias("rmsc")
			.WithDescription("Remove the story structures schematic count, which allows on regen to spawn them again")
			.RequiresPrivilege(Privilege.controlserver)
			.WithArgs(api.ChatCommands.Parsers.Word("code"))
			.HandleWith(OnRemoveStorySchematics)
			.EndSubCommand()
			.BeginSubCommand("listmissing")
			.WithDescription("List story locations that failed to generate.")
			.RequiresPrivilege(Privilege.controlserver)
			.HandleWith(OnListMissingStructures)
			.EndSubCommand()
			.EndSubCommand();
		serverChannel = api.Network.RegisterChannel("StoryGenFailed");
		serverChannel.RegisterMessageType<StoryGenFailed>();
	}

	private TextCommandResult OnListMissingStructures(TextCommandCallingArgs args)
	{
		List<string> missingStructures = GetMissingStructures();
		if (missingStructures.Count > 0)
		{
			string missing = string.Join(",", missingStructures);
			if (args.Caller.Player is IServerPlayer player)
			{
				StoryGenFailed message = new StoryGenFailed
				{
					MissingStructures = missingStructures
				};
				serverChannel.SendPacket(message, player);
			}
			return TextCommandResult.Success("Missing story locations: " + missing);
		}
		return TextCommandResult.Success("No story locations are missing.");
	}

	private List<string> GetMissingStructures()
	{
		List<string> attemptedToGen = api.WorldManager.SaveGame.GetData<List<string>>("attemptedToGenerateStoryLocation");
		List<string> missingStructures = new List<string>();
		if (attemptedToGen != null)
		{
			foreach (string storyCode in attemptedToGen)
			{
				if (!storyStructureInstances.ContainsKey(storyCode))
				{
					missingStructures.Add(storyCode);
				}
			}
		}
		return missingStructures;
	}

	private void OnPlayerJoined(IServerPlayer byplayer)
	{
		if (FailedToGenerateLocation && byplayer.HasPrivilege(Privilege.controlserver))
		{
			StoryGenFailed message = new StoryGenFailed
			{
				MissingStructures = GetMissingStructures()
			};
			serverChannel.SendPacket(message, byplayer);
		}
	}

	private TextCommandResult OnRemoveStorySchematics(TextCommandCallingArgs args)
	{
		string code = (string)args[0];
		if (storyStructureInstances.TryGetValue(code, out var storyStructureLocation))
		{
			storyStructureLocation.SchematicsSpawned = null;
			StoryStructureInstancesDirty = true;
			return TextCommandResult.Success("Ok, removed the story structure locations " + code + " schematics counter.");
		}
		return TextCommandResult.Error("No such story structure exist in assets");
	}

	private TextCommandResult OnSetStoryStructurePos(TextCommandCallingArgs args)
	{
		WorldGenStoryStructure storyStruc = scfg.Structures.FirstOrDefault((WorldGenStoryStructure st) => st.Code == (string)args[0]);
		if (storyStruc == null)
		{
			return TextCommandResult.Error("No such story structure exist in assets");
		}
		if (!(bool)args[2])
		{
			double chunkRange = Math.Ceiling((float)storyStruc.LandformRadius / 32f) + 3.0;
			return TextCommandResult.Success($"Ok, will move the story structure location to this position. Make sure that at least {storyStruc.LandformRadius + 32} blocks around you are unoccupied.\n Add 'true' to the command to confirm.\n After this is done, you will have to regenerate chunks in this area, \ne.g. via <a href=\"chattype:///wgen delr {chunkRange}\">/wgen delr {chunkRange}</a> to delete {chunkRange * 32.0} blocks in all directions. They will then generate again as you move around.");
		}
		BlockPos pos = ((Vec3d)args[1]).AsBlockPos;
		pos.Y = 1;
		GenMaps genmaps = api.ModLoader.GetModSystem<GenMaps>();
		BlockSchematicPartial schem = storyStruc.schematicData;
		int minX = pos.X - schem.SizeX / 2;
		int minZ = pos.Z - schem.SizeZ / 2;
		Cuboidi cub = new Cuboidi(minX, pos.Y, minZ, minX + schem.SizeX - 1, pos.Y + schem.SizeY - 1, minZ + schem.SizeZ - 1);
		storyStructureInstances[storyStruc.Code] = new StoryStructureLocation
		{
			Code = storyStruc.Code,
			CenterPos = pos,
			Location = cub,
			LandformRadius = storyStruc.LandformRadius,
			GenerationRadius = storyStruc.GenerationRadius,
			SkipGenerationFlags = storyStruc.SkipGenerationFlags
		};
		if (storyStruc.RequireLandform != null)
		{
			genmaps.ForceLandformAt(new ForceLandform
			{
				CenterPos = pos,
				Radius = storyStruc.LandformRadius,
				LandformCode = storyStruc.RequireLandform
			});
		}
		if (storyStruc.ForceTemperature.HasValue || storyStruc.ForceRain.HasValue)
		{
			genmaps.ForceClimateAt(new ForceClimate
			{
				Radius = storyStructureInstances[storyStruc.Code].LandformRadius,
				CenterPos = storyStructureInstances[storyStruc.Code].CenterPos,
				Climate = (Climate.DescaleTemperature(((float?)storyStruc.ForceTemperature) ?? 0f) << 16) + (storyStruc.ForceRain.GetValueOrDefault() << 8)
			});
		}
		StoryStructureInstancesDirty = true;
		return TextCommandResult.Success("Ok, story structure location moved to this position. Regenerating chunks at the location should make it appear now.");
	}

	public void InitWorldGen()
	{
		genStoryStructures = api.World.Config.GetAsString("loreContent", "true").ToBool(defaultValue: true);
		if (genStoryStructures)
		{
			strucRand = new LCGRandom(api.WorldManager.Seed + 1095);
			IAsset asset = api.Assets.Get("worldgen/storystructures.json");
			scfg = asset.ToObject<WorldGenStoryStructuresConfig>();
			grassRand = new LCGRandom(api.WorldManager.Seed);
			grassDensity = new ClampedSimplexNoise(new double[1] { 4.0 }, new double[1] { 0.5 }, grassRand.NextInt());
			grassHeight = new ClampedSimplexNoise(new double[1] { 1.5 }, new double[1] { 0.5 }, grassRand.NextInt());
			distort2dx = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 20980);
			distort2dz = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 20981);
			mapheight = api.WorldManager.MapSizeY;
			blockLayerConfig = BlockLayerConfig.GetInstance(api);
			scfg.Init(api, blockLayerConfig.RockStrata, blockLayerConfig);
			double distScale = api.World.Config.GetDecimal("storyStructuresDistScaling", 1.0);
			WorldGenStoryStructure[] structures = scfg.Structures;
			foreach (WorldGenStoryStructure obj in structures)
			{
				obj.MinSpawnDistX = (int)((double)obj.MinSpawnDistX * distScale);
				obj.MinSpawnDistZ = (int)((double)obj.MinSpawnDistZ * distScale);
				obj.MaxSpawnDistX = (int)((double)obj.MaxSpawnDistX * distScale);
				obj.MaxSpawnDistZ = (int)((double)obj.MaxSpawnDistZ * distScale);
			}
			DetermineStoryStructures();
			strucRand.SetWorldSeed(api.WorldManager.Seed + 1095);
		}
	}

	private TextCommandResult OnTpStoryLoc(TextCommandCallingArgs args)
	{
		if (!(args[0] is string code))
		{
			return TextCommandResult.Success();
		}
		if (storyStructureInstances.TryGetValue(code, out var storystruc))
		{
			BlockPos pos = storystruc.CenterPos.Copy();
			pos.Y = (storystruc.Location.Y1 + storystruc.Location.Y2) / 2;
			args.Caller.Entity.TeleportTo(pos);
			return TextCommandResult.Success("Teleporting to " + code);
		}
		return TextCommandResult.Success("No such story structure, " + code);
	}

	public string GetStoryStructureCodeAt(int x, int z, int category)
	{
		if (storyStructureInstances == null)
		{
			return null;
		}
		foreach (KeyValuePair<string, StoryStructureLocation> storyStructureInstance in storyStructureInstances)
		{
			storyStructureInstance.Deconstruct(out var _, out var value);
			StoryStructureLocation loc = value;
			int checkRadius;
			bool hasCategory = loc.SkipGenerationFlags.TryGetValue(category, out checkRadius);
			if (loc.Location.Contains(x, z) && hasCategory)
			{
				return loc.Code;
			}
			if (checkRadius > 0 && loc.CenterPos.HorDistanceSqTo(x, z) < (float)(checkRadius * checkRadius))
			{
				return loc.Code;
			}
		}
		return null;
	}

	public StoryStructureLocation GetStoryStructureAt(int x, int z)
	{
		if (storyStructureInstances == null)
		{
			return null;
		}
		foreach (var (_, loc) in storyStructureInstances)
		{
			if (loc.Location.Contains(x, z))
			{
				return loc;
			}
			int checkRadius = loc.LandformRadius;
			if (checkRadius > 0 && loc.CenterPos.HorDistanceSqTo(x, z) < (float)(checkRadius * checkRadius))
			{
				return loc;
			}
		}
		return null;
	}

	public string GetStoryStructureCodeAt(Vec3d position, int skipCategory)
	{
		return GetStoryStructureCodeAt((int)position.X, (int)position.Z, skipCategory);
	}

	public string GetStoryStructureCodeAt(BlockPos position, int skipCategory)
	{
		return GetStoryStructureCodeAt(position.X, position.Z, skipCategory);
	}

	protected void DetermineStoryStructures()
	{
		List<string> data = api.WorldManager.SaveGame.GetData<List<string>>("attemptedToGenerateStoryLocation");
		if (data == null)
		{
			data = new List<string>();
		}
		int i = 0;
		WorldGenStoryStructure[] structures = scfg.Structures;
		foreach (WorldGenStoryStructure storyStructure in structures)
		{
			if (storyStructureInstances.TryGetValue(storyStructure.Code, out var storyStruc))
			{
				storyStruc.LandformRadius = storyStructure.LandformRadius;
				storyStruc.GenerationRadius = storyStructure.GenerationRadius;
				storyStruc.SkipGenerationFlags = storyStructure.SkipGenerationFlags;
				BlockSchematicPartial schem = storyStructure.schematicData;
				int minX = storyStruc.CenterPos.X - schem.SizeX / 2;
				int minZ = storyStruc.CenterPos.Z - schem.SizeZ / 2;
				Cuboidi cuboidi = new Cuboidi(minX, storyStruc.CenterPos.Y, minZ, minX + schem.SizeX, storyStruc.CenterPos.Y + schem.SizeY, minZ + schem.SizeZ);
				storyStruc.Location = cuboidi;
			}
			else if (!data.Contains(storyStructure.Code))
			{
				strucRand.SetWorldSeed(api.WorldManager.Seed + 1095 + i);
				TryAddStoryLocation(storyStructure);
				data.Add(storyStructure.Code);
			}
			i++;
		}
		StoryStructureInstancesDirty = true;
		api.WorldManager.SaveGame.StoreData("attemptedToGenerateStoryLocation", data);
		SetupForceLandform();
	}

	private void TryAddStoryLocation(WorldGenStoryStructure storyStructure)
	{
		BlockPos basePos = null;
		StoryStructureLocation dependentLocation = null;
		if (!string.IsNullOrEmpty(storyStructure.DependsOnStructure))
		{
			if (storyStructure.DependsOnStructure == "spawn")
			{
				basePos = new BlockPos(api.World.BlockAccessor.MapSizeX / 2, 0, api.World.BlockAccessor.MapSizeZ / 2, 0);
			}
			else if (storyStructureInstances.TryGetValue(storyStructure.DependsOnStructure, out dependentLocation))
			{
				basePos = dependentLocation.CenterPos.Copy();
			}
		}
		if (basePos == null)
		{
			FailedToGenerateLocation = true;
			api.Logger.Error("Could not find dependent structure " + storyStructure.DependsOnStructure + " to generate structure: " + storyStructure.Code + ". Make sure that the dependent structure is before this one in the list.");
			api.Logger.Error($"You will need to add them manually by running /wgen story setpos {storyStructure.DependsOnStructure} and /wgen story setpos {storyStructure.Code} at two different locations about at least 1000 blocks apart.");
			return;
		}
		int dirX = dependentLocation?.DirX ?? ((!((double)strucRand.NextFloat() > 0.5)) ? 1 : (-1));
		int dirZ = ((!((double)strucRand.NextFloat() > 0.5)) ? 1 : (-1));
		int distanceX = storyStructure.MinSpawnDistX + strucRand.NextInt(storyStructure.MaxSpawnDistX + 1 - storyStructure.MinSpawnDistX);
		int distanceZ = storyStructure.MinSpawnDistZ + strucRand.NextInt(storyStructure.MaxSpawnDistZ + 1 - storyStructure.MinSpawnDistZ);
		BlockSchematicPartial schem = storyStructure.schematicData;
		EnumStructurePlacement placement = storyStructure.Placement;
		bool flag = (uint)placement <= 1u;
		int locationHeight = ((!flag) ? 1 : (api.World.SeaLevel + schem.OffsetY));
		BlockPos pos = new BlockPos(basePos.X + distanceX * dirX, locationHeight, basePos.Z + distanceZ * dirZ, 0);
		int radius = Math.Max(storyStructure.LandformRadius, storyStructure.GenerationRadius);
		int posMinX = pos.X - radius;
		int posMaxX = pos.X + radius;
		int posMinZ = pos.Z - radius;
		int posMaxZ = pos.Z + radius;
		int mapRegionPosMinX = posMinX / api.WorldManager.RegionSize;
		int mapRegionPosMaxX = posMaxX / api.WorldManager.RegionSize;
		int mapRegionPosMinZ = posMinZ / api.WorldManager.RegionSize;
		int mapRegionPosMaxZ = posMaxZ / api.WorldManager.RegionSize;
		bool hasMapRegion = false;
		for (int x4 = mapRegionPosMinX; x4 <= mapRegionPosMaxX; x4++)
		{
			for (int z4 = mapRegionPosMinZ; z4 <= mapRegionPosMaxZ; z4++)
			{
				if (api.WorldManager.BlockingTestMapRegionExists(x4, z4))
				{
					hasMapRegion = true;
				}
			}
		}
		int mapChunkPosMinX = posMinX / 32;
		int mapChunkPosMaxX = posMaxX / 32;
		int mapChunkPosMinZ = posMinZ / 32;
		int mapChunkPosMaxZ = posMaxZ / 32;
		if (hasMapRegion)
		{
			for (int x3 = mapChunkPosMinX; x3 <= mapChunkPosMaxX; x3++)
			{
				for (int z3 = mapChunkPosMinZ; z3 <= mapChunkPosMaxZ; z3++)
				{
					if (!api.WorldManager.BlockingTestMapChunkExists(x3, z3))
					{
						continue;
					}
					IServerChunk[] array = api.WorldManager.BlockingLoadChunkColumn(x3, z3);
					foreach (IServerChunk chunk in array)
					{
						if (chunk.BlocksPlaced > 0 || chunk.BlocksRemoved > 0)
						{
							FailedToGenerateLocation = true;
							api.Logger.Error($"Map chunk in area of story location {storyStructure.Code} contains player edits, can not automatically add it your world. You can add it manually running /wgen story setpos {storyStructure.Code} at a location that seems suitable for you.");
							return;
						}
						chunk.Dispose();
					}
				}
			}
		}
		if (hasMapRegion)
		{
			for (int x2 = mapRegionPosMinX; x2 <= mapRegionPosMaxX; x2++)
			{
				for (int z2 = mapRegionPosMinZ; z2 <= mapRegionPosMaxZ; z2++)
				{
					api.WorldManager.DeleteMapRegion(x2, z2);
				}
			}
			for (int x = mapChunkPosMinX; x <= mapChunkPosMaxX; x++)
			{
				for (int z = mapChunkPosMinZ; z <= mapChunkPosMaxZ; z++)
				{
					api.WorldManager.DeleteChunkColumn(x, z);
				}
			}
		}
		int minX = pos.X - schem.SizeX / 2;
		int minZ = pos.Z - schem.SizeZ / 2;
		Cuboidi cuboidi = new Cuboidi(minX, pos.Y, minZ, minX + schem.SizeX, pos.Y + schem.SizeY, minZ + schem.SizeZ);
		storyStructureInstances[storyStructure.Code] = new StoryStructureLocation
		{
			Code = storyStructure.Code,
			CenterPos = pos,
			Location = cuboidi,
			LandformRadius = storyStructure.LandformRadius,
			GenerationRadius = storyStructure.GenerationRadius,
			DirX = dirX,
			SkipGenerationFlags = storyStructure.SkipGenerationFlags
		};
	}

	private void SetupForceLandform()
	{
		GenMaps genmaps = api.ModLoader.GetModSystem<GenMaps>();
		foreach (KeyValuePair<string, StoryStructureLocation> storyStructureInstance in storyStructureInstances)
		{
			storyStructureInstance.Deconstruct(out var key, out var value);
			string code = key;
			StoryStructureLocation location = value;
			WorldGenStoryStructure structureConfig = scfg.Structures.FirstOrDefault((WorldGenStoryStructure s) => s.Code == code);
			if (structureConfig == null)
			{
				api.Logger.Warning("Could not find config for story structure: " + code + ". Terrain will not be generated as it should at " + code);
				continue;
			}
			if (structureConfig.ForceTemperature.HasValue || structureConfig.ForceRain.HasValue)
			{
				genmaps.ForceClimateAt(new ForceClimate
				{
					Radius = location.LandformRadius,
					CenterPos = location.CenterPos,
					Climate = (Climate.DescaleTemperature(((float?)structureConfig.ForceTemperature) ?? 0f) << 16) + (structureConfig.ForceRain.GetValueOrDefault() << 8)
				});
			}
			if (structureConfig.RequireLandform != null)
			{
				genmaps.ForceLandformAt(new ForceLandform
				{
					Radius = location.LandformRadius,
					CenterPos = location.CenterPos,
					LandformCode = structureConfig.RequireLandform
				});
			}
		}
	}

	private void Event_GameWorldSave()
	{
		if (StoryStructureInstancesDirty)
		{
			api.WorldManager.SaveGame.StoreData("storystructurelocations", SerializerUtil.Serialize(storyStructureInstances));
			StoryStructureInstancesDirty = false;
		}
	}

	private void Event_SaveGameLoaded()
	{
		OrderedDictionary<string, StoryStructureLocation> strucs = api.WorldManager.SaveGame.GetData<OrderedDictionary<string, StoryStructureLocation>>("storystructurelocations");
		if (strucs != null)
		{
			storyStructureInstances = strucs;
		}
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		worldgenBlockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: false);
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		if (!genStoryStructures || storyStructureInstances == null)
		{
			return;
		}
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		tmpCuboid.Set(chunkX * 32, 0, chunkZ * 32, chunkX * 32 + 32, chunks.Length * 32, chunkZ * 32 + 32);
		worldgenBlockAccessor.BeginColumn();
		foreach (KeyValuePair<string, StoryStructureLocation> storyStructureInstance in storyStructureInstances)
		{
			storyStructureInstance.Deconstruct(out var key, out var value);
			string strucCode = key;
			StoryStructureLocation strucInst = value;
			Cuboidi strucloc = strucInst.Location;
			if (!strucloc.Intersects(tmpCuboid))
			{
				continue;
			}
			if (!strucInst.DidGenerate)
			{
				strucInst.DidGenerate = true;
				StoryStructureInstancesDirty = true;
			}
			BlockPos startPos = new BlockPos(strucloc.X1, strucloc.Y1, strucloc.Z1, 0);
			WorldGenStoryStructure structure = scfg.Structures.First((WorldGenStoryStructure s) => s.Code == strucCode);
			switch (structure.Placement)
			{
			case EnumStructurePlacement.SurfaceRuin:
			{
				int h;
				if (strucInst.WorldgenHeight >= 0)
				{
					h = strucInst.WorldgenHeight;
				}
				else
				{
					h = (strucInst.WorldgenHeight = chunks[0].MapChunk.WorldGenTerrainHeightMap[startPos.Z % 32 * 32 + startPos.X % 32]);
					strucloc.Y1 = h - structure.schematicData.SizeY + structure.schematicData.OffsetY;
					strucloc.Y2 = strucloc.Y1 + structure.schematicData.SizeY;
					StoryStructureInstancesDirty = true;
				}
				startPos.Y = h - structure.schematicData.SizeY + structure.schematicData.OffsetY;
				break;
			}
			case EnumStructurePlacement.Surface:
				strucloc.Y1 = api.World.SeaLevel + structure.schematicData.OffsetY;
				strucloc.Y2 = strucloc.Y1 + structure.schematicData.SizeY;
				StoryStructureInstancesDirty = true;
				startPos.Y = strucloc.Y1;
				break;
			}
			Block rockBlock = null;
			if (structure.resolvedRockTypeRemaps != null)
			{
				if (string.IsNullOrEmpty(strucInst.RockBlockCode))
				{
					strucRand.InitPositionSeed(chunkX, chunkZ);
					int lx = strucRand.NextInt(32);
					int lz = strucRand.NextInt(32);
					EnumStructurePlacement placement = structure.Placement;
					bool flag = (uint)placement <= 1u;
					int posY = ((!flag) ? (startPos.Y + structure.schematicData.SizeY / 2 + strucRand.NextInt(structure.schematicData.SizeY / 2)) : request.Chunks[0].MapChunk.WorldGenTerrainHeightMap[lz * 32 + lx]);
					int i = 0;
					while (rockBlock == null && i < 10)
					{
						Block block = worldgenBlockAccessor.GetBlock(chunkX * 32 + lx, posY, chunkZ * 32 + lz, 1);
						if (block.BlockMaterial == EnumBlockMaterial.Stone)
						{
							rockBlock = block;
							strucInst.RockBlockCode = block.Code.ToString();
							StoryStructureInstancesDirty = true;
						}
						placement = structure.Placement;
						flag = (uint)placement <= 1u;
						posY = ((!flag) ? (startPos.Y + structure.schematicData.SizeY / 2 + strucRand.NextInt(structure.schematicData.SizeY / 2)) : (posY - 1));
						i++;
					}
					if (string.IsNullOrEmpty(strucInst.RockBlockCode))
					{
						api.Logger.Warning("Could not find rock block code for " + strucInst.Code);
					}
				}
				else
				{
					rockBlock = worldgenBlockAccessor.GetBlock(new AssetLocation(strucInst.RockBlockCode));
				}
			}
			int blocksPlaced = structure.schematicData.PlacePartial(chunks, worldgenBlockAccessor, api.World, chunkX, chunkZ, startPos, EnumReplaceMode.ReplaceAll, structure.Placement, GenStructures.ReplaceMetaBlocks, GenStructures.ReplaceMetaBlocks, structure.resolvedRockTypeRemaps, structure.replacewithblocklayersBlockids, rockBlock);
			if (blocksPlaced > 0)
			{
				EnumStructurePlacement placement = structure.Placement;
				if ((uint)placement <= 1u)
				{
					UpdateHeightmap(request, worldgenBlockAccessor);
				}
				if (structure.GenerateGrass)
				{
					GenerateGrass(request);
				}
			}
			string code = structure.Code + ":" + structure.Schematics[0];
			IMapRegion region = chunks[0].MapChunk.MapRegion;
			if (region.GeneratedStructures.FirstOrDefault((GeneratedStructure struc) => struc.Code.Equals(code)) == null)
			{
				region.AddGeneratedStructure(new GeneratedStructure
				{
					Code = code,
					Group = structure.Group,
					Location = strucloc.Clone()
				});
			}
			if (blocksPlaced <= 0 || !structure.BuildProtected)
			{
				continue;
			}
			if (!structure.ExcludeSchematicSizeProtect)
			{
				LandClaim[] claims3 = api.World.Claims.Get(strucloc.Center.AsBlockPos);
				if (claims3 == null || claims3.Length == 0)
				{
					api.World.Claims.Add(new LandClaim
					{
						Areas = new List<Cuboidi> { strucloc },
						Description = structure.BuildProtectionDesc,
						ProtectionLevel = 10,
						LastKnownOwnerName = structure.BuildProtectionName,
						AllowUseEveryone = true
					});
				}
			}
			if (structure.ExtraLandClaimX > 0 && structure.ExtraLandClaimZ > 0)
			{
				Cuboidi struclocDeva = new Cuboidi(strucloc.Center.X - structure.ExtraLandClaimX, 0, strucloc.Center.Z - structure.ExtraLandClaimZ, strucloc.Center.X + structure.ExtraLandClaimX, api.WorldManager.MapSizeY, strucloc.Center.Z + structure.ExtraLandClaimZ);
				LandClaim[] claims2 = api.World.Claims.Get(struclocDeva.Center.AsBlockPos);
				if (claims2 == null || claims2.Length == 0)
				{
					api.World.Claims.Add(new LandClaim
					{
						Areas = new List<Cuboidi> { struclocDeva },
						Description = structure.BuildProtectionDesc,
						ProtectionLevel = 10,
						LastKnownOwnerName = structure.BuildProtectionName,
						AllowUseEveryone = true
					});
				}
			}
			if (structure.CustomLandClaims == null)
			{
				continue;
			}
			Cuboidi[] customLandClaims = structure.CustomLandClaims;
			for (int j = 0; j < customLandClaims.Length; j++)
			{
				Cuboidi cuboidi = customLandClaims[j].Clone();
				cuboidi.X1 += strucloc.X1;
				cuboidi.X2 += strucloc.X1;
				cuboidi.Y1 += strucloc.Y1;
				cuboidi.Y2 += strucloc.Y1;
				cuboidi.Z1 += strucloc.Z1;
				cuboidi.Z2 += strucloc.Z1;
				LandClaim[] claims = api.World.Claims.Get(cuboidi.Center.AsBlockPos);
				if (claims == null || claims.Length == 0)
				{
					api.World.Claims.Add(new LandClaim
					{
						Areas = new List<Cuboidi> { cuboidi },
						Description = structure.BuildProtectionDesc,
						ProtectionLevel = 10,
						LastKnownOwnerName = structure.BuildProtectionName,
						AllowUseEveryone = true
					});
				}
			}
		}
	}

	private void UpdateHeightmap(IChunkColumnGenerateRequest request, IWorldGenBlockAccessor worldGenBlockAccessor)
	{
		int updatedPositionsT = 0;
		int updatedPositionsR = 0;
		ushort[] rainHeightMap = request.Chunks[0].MapChunk.RainHeightMap;
		ushort[] terrainHeightMap = request.Chunks[0].MapChunk.WorldGenTerrainHeightMap;
		for (int i = 0; i < rainHeightMap.Length; i++)
		{
			rainHeightMap[i] = 0;
			terrainHeightMap[i] = 0;
		}
		int mapSizeY = worldgenBlockAccessor.MapSizeY;
		int mapSize2D = 1024;
		for (int x = 0; x < 32; x++)
		{
			for (int z = 0; z < 32; z++)
			{
				int mapIndex = z * 32 + x;
				bool rainSet = false;
				bool heightSet = false;
				for (int posY = mapSizeY - 1; posY >= 0; posY--)
				{
					int num = posY % 32;
					IServerChunk chunk = request.Chunks[posY / 32];
					int chunkIndex = (num * 32 + z) * 32 + x;
					int blockId = chunk.Data[chunkIndex];
					if (blockId != 0)
					{
						Block block = worldGenBlockAccessor.GetBlock(blockId);
						bool newRainPermeable = block.RainPermeable;
						bool num2 = block.SideSolid[BlockFacing.UP.Index];
						if (!newRainPermeable && !rainSet)
						{
							rainSet = true;
							rainHeightMap[mapIndex] = (ushort)posY;
							updatedPositionsR++;
						}
						if (num2 && !heightSet)
						{
							heightSet = true;
							terrainHeightMap[mapIndex] = (ushort)posY;
							updatedPositionsT++;
						}
						if (updatedPositionsR >= mapSize2D && updatedPositionsT >= mapSize2D)
						{
							return;
						}
					}
				}
			}
		}
	}

	private void GenerateGrass(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		grassRand.InitPositionSeed(chunkX, chunkZ);
		IntDataMap2D forestMap = chunks[0].MapChunk.MapRegion.ForestMap;
		IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
		ushort[] heightMap = chunks[0].MapChunk.RainHeightMap;
		int regionChunkSize = api.WorldManager.RegionSize / 32;
		int rdx = chunkX % regionChunkSize;
		int rdz = chunkZ % regionChunkSize;
		float climateStep = (float)climateMap.InnerSize / (float)regionChunkSize;
		float forestStep = (float)forestMap.InnerSize / (float)regionChunkSize;
		int forestUpLeft = forestMap.GetUnpaddedInt((int)((float)rdx * forestStep), (int)((float)rdz * forestStep));
		int forestUpRight = forestMap.GetUnpaddedInt((int)((float)rdx * forestStep + forestStep), (int)((float)rdz * forestStep));
		int forestBotLeft = forestMap.GetUnpaddedInt((int)((float)rdx * forestStep), (int)((float)rdz * forestStep + forestStep));
		int forestBotRight = forestMap.GetUnpaddedInt((int)((float)rdx * forestStep + forestStep), (int)((float)rdz * forestStep + forestStep));
		BlockPos herePos = new BlockPos();
		for (int x = 0; x < 32; x++)
		{
			for (int z = 0; z < 32; z++)
			{
				herePos.Set(chunkX * 32 + x, 1, chunkZ * 32 + z);
				double distx;
				double distz;
				int rnd = RandomlyAdjustPosition(herePos, out distx, out distz);
				int posY = heightMap[z * 32 + x];
				if (posY < mapheight)
				{
					int unpaddedColorLerped = climateMap.GetUnpaddedColorLerped((float)rdx * climateStep + climateStep * ((float)x + (float)distx) / 32f, (float)rdz * climateStep + climateStep * ((float)z + (float)distz) / 32f);
					int unscaledTemp = (unpaddedColorLerped >> 16) & 0xFF;
					float temp = Climate.GetScaledAdjustedTemperatureFloat(unscaledTemp, posY - TerraGenConfig.seaLevel + rnd);
					float tempRel = (float)Climate.GetAdjustedTemperature(unscaledTemp, posY - TerraGenConfig.seaLevel + rnd) / 255f;
					float rainRel = (float)Climate.GetRainFall((unpaddedColorLerped >> 8) & 0xFF, posY + rnd) / 255f;
					float forestRel = GameMath.BiLerp(forestUpLeft, forestUpRight, forestBotLeft, forestBotRight, (float)x / 32f, (float)z / 32f) / 255f;
					ushort num = chunks[0].MapChunk.WorldGenTerrainHeightMap[z * 32 + x];
					int chunkY = num / 32;
					int lY = num % 32;
					int index3d = (32 * lY + z) * 32 + x;
					int rockblockID = chunks[chunkY].Data.GetBlockIdUnsafe(index3d);
					if (api.World.Blocks[rockblockID].BlockMaterial == EnumBlockMaterial.Soil)
					{
						PlaceTallGrass(x, posY, z, chunks, rainRel, tempRel, temp, forestRel);
					}
				}
			}
		}
	}

	public int RandomlyAdjustPosition(BlockPos herePos, out double distx, out double distz)
	{
		distx = distort2dx.Noise(herePos.X, herePos.Z);
		distz = distort2dz.Noise(herePos.X, herePos.Z);
		return (int)(distx / 5.0);
	}

	private void PlaceTallGrass(int x, int posY, int z, IServerChunk[] chunks, float rainRel, float tempRel, float temp, float forestRel)
	{
		double num = (double)blockLayerConfig.Tallgrass.RndWeight * grassRand.NextDouble() + (double)blockLayerConfig.Tallgrass.PerlinWeight * grassDensity.Noise(x, z, -0.5);
		double extraGrass = Math.Max(0.0, (double)(rainRel * tempRel) - 0.25);
		if (num <= GameMath.Clamp((double)forestRel - extraGrass, 0.05, 0.99) || posY >= mapheight - 1 || posY < 1)
		{
			return;
		}
		int blockId = chunks[posY / 32].Data[(32 * (posY % 32) + z) * 32 + x];
		if (api.World.Blocks[blockId].Fertility <= grassRand.NextInt(100))
		{
			return;
		}
		double gheight = Math.Max(0.0, grassHeight.Noise(x, z) * (double)blockLayerConfig.Tallgrass.BlockCodeByMin.Length - 1.0);
		for (int i = (int)gheight + ((grassRand.NextDouble() < gheight) ? 1 : 0); i < blockLayerConfig.Tallgrass.BlockCodeByMin.Length; i++)
		{
			TallGrassBlockCodeByMin bcbymin = blockLayerConfig.Tallgrass.BlockCodeByMin[i];
			if (forestRel <= bcbymin.MaxForest && rainRel >= bcbymin.MinRain && temp >= (float)bcbymin.MinTemp)
			{
				chunks[(posY + 1) / 32].Data[(32 * ((posY + 1) % 32) + z) * 32 + x] = bcbymin.BlockId;
				break;
			}
		}
	}

	public void GenerateHookStructure(IBlockAccessor blockAccessor, BlockPos pos, string param)
	{
		AssetLocation code = new AssetLocation(param);
		api.Logger.VerboseDebug("Worldgen hook generation event fired, with code " + code);
		IMapChunk mapchunk = blockAccessor.GetMapChunkAtBlockPos(pos);
		IAsset assetMain = api.Assets.TryGet(code.WithPathPrefixOnce("worldgen/hookgeneratedstructures/").WithPathAppendixOnce(".json"));
		if (assetMain == null || mapchunk == null)
		{
			api.Logger.Error("Worldgen hook event failed: " + ((mapchunk == null) ? "bad coordinates" : string.Concat(code, "* not found")));
			return;
		}
		HookGeneratedStructure hookStruct = assetMain.ToObject<HookGeneratedStructure>();
		int mainsizeX = hookStruct.mainsizeX;
		int mainsizeZ = hookStruct.mainsizeZ;
		int minX = pos.X - mainsizeX / 2 - 2;
		int maxX = pos.X + mainsizeX / 2 + 2;
		int minZ = pos.Z - mainsizeZ / 2 - 2;
		int maxZ = pos.Z + mainsizeZ / 2 + 2;
		List<int> heights = new List<int>((maxX - minX + 1) * (maxZ - minZ + 1));
		int maxheight = 0;
		int minheight = int.MaxValue;
		int z;
		int x;
		for (x = minX; x <= maxX; x++)
		{
			for (z = minZ; z <= maxZ; z++)
			{
				mapchunk = blockAccessor.GetMapChunk(x / 32, z / 32);
				int h6 = mapchunk.WorldGenTerrainHeightMap[z % 32 * 32 + x % 32];
				heights.Add(h6);
				maxheight = Math.Max(maxheight, h6);
				minheight = Math.Min(minheight, h6);
			}
		}
		x = Math.Max(mainsizeX, mainsizeZ);
		minX = pos.X - x / 2;
		maxX = pos.X + x / 2;
		minZ = pos.Z - x / 2;
		maxZ = pos.Z + x / 2;
		int weightedHeightW = 1;
		int weightedHeightE = 1;
		int weightedHeightN = 1;
		int weightedHeightS = 1;
		x = minX - 2;
		for (z = minZ; z <= maxZ; z++)
		{
			mapchunk = blockAccessor.GetMapChunk(x / 32, z / 32);
			int h5 = mapchunk.WorldGenTerrainHeightMap[z % 32 * 32 + x % 32];
			weightedHeightW += h5;
		}
		x = maxX + 2;
		for (z = minZ; z <= maxZ; z++)
		{
			mapchunk = blockAccessor.GetMapChunk(x / 32, z / 32);
			int h4 = mapchunk.WorldGenTerrainHeightMap[z % 32 * 32 + x % 32];
			weightedHeightE += h4;
		}
		z = minZ - 2;
		for (x = minX; x <= maxX; x++)
		{
			mapchunk = blockAccessor.GetMapChunk(x / 32, z / 32);
			int h3 = mapchunk.WorldGenTerrainHeightMap[z % 32 * 32 + x % 32];
			weightedHeightN += h3;
		}
		z = maxZ + 2;
		for (x = minX; x <= maxX; x++)
		{
			mapchunk = blockAccessor.GetMapChunk(x / 32, z / 32);
			int h2 = mapchunk.WorldGenTerrainHeightMap[z % 32 * 32 + x % 32];
			weightedHeightS += h2;
		}
		if (hookStruct.mainElements.Length != 0)
		{
			pos = pos.AddCopy(hookStruct.offsetX, hookStruct.offsetY, hookStruct.offsetZ);
			Vec3i[] offsets = new Vec3i[hookStruct.mainElements.Length];
			BlockSchematicStructure[] structures = new BlockSchematicStructure[hookStruct.mainElements.Length];
			int[] counts = new int[hookStruct.mainElements.Length];
			int[] maxCounts = new int[hookStruct.mainElements.Length];
			int structuresLength = 0;
			PathAndOffset[] mainElements = hookStruct.mainElements;
			foreach (PathAndOffset el in mainElements)
			{
				IAsset asset = api.Assets.TryGet(new AssetLocation(code.Domain, "worldgen/" + el.path + ".json"));
				if (asset == null)
				{
					api.Logger.Notification("Worldgen hook event elements: path not found: " + el.path);
					continue;
				}
				BlockSchematicStructure structure = asset.ToObject<BlockSchematicStructure>();
				structure.Init(blockAccessor);
				structures[structuresLength] = structure;
				maxCounts[structuresLength] = ((el.maxCount == 0) ? 16384 : el.maxCount);
				offsets[structuresLength++] = new Vec3i(el.dx, el.dy, el.dz);
			}
			Random rand = api.World.Rand;
			List<int> indices = new List<int>();
			List<int> bestIndices = new List<int>();
			int bestDiff = int.MaxValue;
			heights.Sort();
			int n = Math.Min(5, heights.Count);
			int height = 0;
			for (int l = 0; l < n; l++)
			{
				height += heights[l];
			}
			height = height / n + hookStruct.endOffsetY;
			if (maxheight - minheight < 5 && height - minheight < 2)
			{
				height++;
			}
			if (height < api.World.SeaLevel)
			{
				height = api.World.SeaLevel;
			}
			height = Math.Min(height, api.World.BlockAccessor.MapSizeY - 11);
			for (int k = 0; k < 25; k++)
			{
				indices.Clear();
				for (int m = 0; m < counts.Length; m++)
				{
					counts[m] = 0;
				}
				int testHeight = pos.Y;
				while (testHeight < height)
				{
					int j = rand.Next(structuresLength);
					if (counts[j] >= maxCounts[j])
					{
						continue;
					}
					int h = structures[j].SizeY;
					if (testHeight + h > height)
					{
						if (testHeight + h - height > height - testHeight)
						{
							h = (height - testHeight) * 2;
						}
						else
						{
							indices.Add(j);
							counts[j]++;
						}
						int newDiff = testHeight + h - height;
						if (newDiff >= bestDiff)
						{
							break;
						}
						bestDiff = newDiff;
						bestIndices.Clear();
						foreach (int ix2 in indices)
						{
							bestIndices.Add(ix2);
						}
						if (bestDiff == 0)
						{
							k = 25;
						}
						break;
					}
					indices.Add(j);
					counts[j]++;
					testHeight += h;
				}
			}
			int posY = pos.Y;
			int entranceMinX = int.MaxValue;
			int entranceMinZ = int.MaxValue;
			int entranceMaxX = 0;
			int entranceMaxZ = 0;
			foreach (int ix in bestIndices)
			{
				BlockSchematicStructure struc = structures[ix];
				Vec3i offset = offsets[ix];
				BlockPos posPlace = pos.AddCopy(offset.X, offset.Y, offset.Z);
				struc.PlaceRespectingBlockLayers(blockAccessor, api.World, posPlace, 0, 0, 0, 0, null, new int[0], GenStructures.ReplaceMetaBlocks, replaceBlockEntities: true, suppressSoilIfAirBelow: false, displaceWater: true);
				pos.Y += struc.SizeY;
				entranceMinX = Math.Min(entranceMinX, posPlace.X);
				entranceMinZ = Math.Min(entranceMinZ, posPlace.Z);
				entranceMaxX = Math.Max(entranceMaxX, posPlace.X + struc.SizeX);
				entranceMaxZ = Math.Max(entranceMaxZ, posPlace.Z + struc.SizeY);
			}
			IMapRegion mapRegion = blockAccessor.GetMapRegion(pos.X / blockAccessor.RegionSize, pos.Z / blockAccessor.RegionSize);
			Cuboidi location = new Cuboidi(entranceMinX, posY, entranceMinZ, entranceMaxX, pos.Y, entranceMaxZ);
			mapRegion.AddGeneratedStructure(new GeneratedStructure
			{
				Code = param,
				Group = hookStruct.group,
				Location = location.Clone()
			});
			if (hookStruct.buildProtected)
			{
				api.World.Claims.Add(new LandClaim
				{
					Areas = new List<Cuboidi> { location },
					Description = hookStruct.buildProtectionDesc,
					ProtectionLevel = 10,
					LastKnownOwnerName = hookStruct.buildProtectionName,
					AllowUseEveryone = true
				});
			}
		}
		string topside = ((weightedHeightW < weightedHeightE) ? ((weightedHeightW >= weightedHeightN || weightedHeightW >= weightedHeightS) ? ((weightedHeightS < weightedHeightN) ? "s" : "n") : "w") : ((weightedHeightE >= weightedHeightN || weightedHeightE >= weightedHeightS) ? ((weightedHeightS < weightedHeightN) ? "s" : "n") : "e"));
		if (!hookStruct.endElements.TryGetValue(topside, out var endElement))
		{
			api.Logger.Notification("Worldgen hook event incomplete: no end structure for " + topside);
			return;
		}
		BlockSchematicStructure structTop = api.Assets.Get(new AssetLocation(code.Domain, endElement.path))?.ToObject<BlockSchematicStructure>();
		if (structTop == null)
		{
			api.Logger.Notification("Worldgen hook event incomplete: " + endElement.path + " not found");
			return;
		}
		int[] replaceblockids;
		if (hookStruct.ReplaceWithBlocklayers != null)
		{
			replaceblockids = new int[hookStruct.ReplaceWithBlocklayers.Length];
			for (int i = 0; i < replaceblockids.Length; i++)
			{
				Block block = api.World.GetBlock(hookStruct.ReplaceWithBlocklayers[i]);
				if (block == null)
				{
					api.Logger.Error($"Hook structure with code {code} has replace block layer {hookStruct.ReplaceWithBlocklayers[i]} defined, but no such block found!");
					return;
				}
				replaceblockids[i] = block.Id;
			}
		}
		else
		{
			replaceblockids = new int[0];
		}
		IMapRegion mapRegion2 = mapchunk.MapRegion;
		IntDataMap2D climateMap = mapRegion2.ClimateMap;
		int regionChunkSize = api.WorldManager.RegionSize / 32;
		int rlX = pos.X / 32 % regionChunkSize;
		int rlZ = pos.Z / 32 % regionChunkSize;
		float facC = (float)climateMap.InnerSize / (float)regionChunkSize;
		int climateUpLeft = climateMap.GetUnpaddedInt((int)((float)rlX * facC), (int)((float)rlZ * facC));
		int climateUpRight = climateMap.GetUnpaddedInt((int)((float)rlX * facC + facC), (int)((float)rlZ * facC));
		int climateBotLeft = climateMap.GetUnpaddedInt((int)((float)rlX * facC), (int)((float)rlZ * facC + facC));
		int climateBotRight = climateMap.GetUnpaddedInt((int)((float)rlX * facC + facC), (int)((float)rlZ * facC + facC));
		structTop.blockLayerConfig = blockLayerConfig;
		structTop.Init(blockAccessor);
		pos.Add(endElement.dx, endElement.dy, endElement.dz);
		structTop.PlaceRespectingBlockLayers(blockAccessor, api.World, pos, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight, null, replaceblockids, GenStructures.ReplaceMetaBlocks, replaceBlockEntities: true, suppressSoilIfAirBelow: true);
		Cuboidi locationEnd = new Cuboidi(pos.X, pos.Y, pos.Z, pos.X + structTop.SizeX, pos.Y + structTop.SizeY, pos.Z + structTop.SizeZ);
		mapRegion2.AddGeneratedStructure(new GeneratedStructure
		{
			Code = hookStruct.group,
			Group = hookStruct.group,
			Location = locationEnd.Clone()
		});
	}

	public void FinalizeRegeneration(int chunkMidX, int chunkMidZ)
	{
		api.ModLoader.GetModSystem<Timeswitch>().AttemptGeneration(worldgenBlockAccessor);
	}
}
