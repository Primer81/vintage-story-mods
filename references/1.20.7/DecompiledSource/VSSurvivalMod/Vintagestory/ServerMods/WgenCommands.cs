using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class WgenCommands : ModSystem
{
	private ICoreServerAPI api;

	private TreeGeneratorsUtil treeGenerators;

	private int _regionSize;

	private long _seed = 1239123912L;

	private int _chunksize;

	private WorldGenStructuresConfig _scfg;

	private int _regionChunkSize;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		treeGenerators = new TreeGeneratorsUtil(api);
		api.Event.SaveGameLoaded += OnGameWorldLoaded;
		if (this.api.Server.CurrentRunPhase == EnumServerRunPhase.RunGame)
		{
			OnGameWorldLoaded();
		}
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.InitWorldGenerator(InitWorldGen, "standard");
		}
		CreateCommands();
		this.api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, delegate
		{
			CommandArgumentParsers parsers = api.ChatCommands.Parsers;
			string[] array = api.World.TreeGenerators.Keys.Select((AssetLocation a) => a.Path).ToArray();
			api.ChatCommands.GetOrCreate("wgen").BeginSubCommand("tree").WithDescription("Generate a tree in front of the player")
				.RequiresPlayer()
				.WithArgs(parsers.WordRange("treeWorldPropertyCode", array), parsers.OptionalFloat("size", 1f), parsers.OptionalFloat("aheadoffset"))
				.HandleWith(OnCmdTree)
				.EndSubCommand()
				.BeginSubCommand("treelineup")
				.WithDescription("treelineup")
				.RequiresPlayer()
				.WithArgs(parsers.Word("treeWorldPropertyCode", array))
				.HandleWith(OnCmdTreelineup)
				.EndSubCommand();
		});
	}

	private void InitWorldGen()
	{
		_chunksize = 32;
		_regionChunkSize = api.WorldManager.RegionSize / _chunksize;
		IAsset asset = api.Assets.Get("worldgen/structures.json");
		_scfg = asset.ToObject<WorldGenStructuresConfig>();
		_scfg.Init(api);
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("wgen").BeginSubCommand("structures").BeginSubCommand("spawn")
			.RequiresPlayer()
			.WithDescription("Spawn a structure from structure.json like during worldgen. Target position will be the selected block or your position. See /dev list <num> command to get the correct index.")
			.WithArgs(parsers.Int("structure_index"), parsers.OptionalInt("schematic_index"), parsers.OptionalIntRange("rotation_index", 0, 3))
			.HandleWith(OnStructuresSpawn)
			.EndSubCommand()
			.BeginSubCommand("list")
			.WithDescription("List structures with their indices for the /dev structure spawn command")
			.WithArgs(parsers.OptionalInt("structure_num"))
			.HandleWith(OnStructuresList)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("resolve-meta")
			.WithDescription("Toggle resolve meta blocks mode during Worldgen. Turn it off to spawn structures as they are. For example, in this mode, instead of traders, their meta spawners will spawn")
			.WithAlias("rm")
			.WithArgs(parsers.OptionalBool("on/off"))
			.HandleWith(handleToggleImpresWgen)
			.EndSubCommand();
	}

	private void OnGameWorldLoaded()
	{
		_regionSize = api.WorldManager.RegionSize;
	}

	private void CreateCommands()
	{
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("wgen").WithDescription("World generator tools").RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("decopass")
			.WithDescription("Toggle DoDecorationPass on/off")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalBool("DoDecorationPass"))
			.HandleWith(OnCmdDecopass)
			.EndSubCommand()
			.BeginSubCommand("autogen")
			.WithDescription("Toggle AutoGenerateChunks on/off")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalBool("AutoGenerateChunks"))
			.HandleWith(OnCmdAutogen)
			.EndSubCommand()
			.BeginSubCommand("gt")
			.WithDescription("Toggle GenerateVegetation on/off")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalBool("GenerateVegetation"))
			.HandleWith(OnCmdGt)
			.EndSubCommand()
			.BeginSubCommand("regenk")
			.WithDescription("Regenerate chunks around the player. Keeps the mapregion and so will not regenerate structures use /wgen regen if you want to also regenerate the structures")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 0, 50), parsers.OptionalWord("landform"))
			.HandleWith(OnCmdRegenk)
			.EndSubCommand()
			.BeginSubCommand("regen")
			.WithDescription("Regenerate chunks around the player also regenerating the region. Keeps unaffected structures outside of the range and copy them to the new region")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 0, 50), parsers.OptionalWord("landform"))
			.HandleWith(OnCmdRegen)
			.EndSubCommand()
			.BeginSubCommand("regenr")
			.WithDescription("Regenerate chunks around the player with random seed")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 0, 50), parsers.OptionalWord("landform"))
			.HandleWith(OnCmdRegenr)
			.EndSubCommand()
			.BeginSubCommand("regenc")
			.WithDescription("Regenerate chunks around world center")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 0, 50), parsers.OptionalWord("landform"))
			.HandleWith(OnCmdRegenc)
			.EndSubCommand()
			.BeginSubCommand("regenrc")
			.WithDescription("Regenerate chunks around world center with random seed")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 0, 50), parsers.OptionalWord("landform"))
			.HandleWith(OnCmdRegenrc)
			.EndSubCommand()
			.BeginSubCommand("pregen")
			.WithDescription("Pregenerate chunks around the player or around world center when executed from console.")
			.WithArgs(parsers.OptionalInt("chunk_range", 2))
			.HandleWith(OnCmdPregen)
			.EndSubCommand()
			.BeginSubCommand("delrock")
			.WithDescription("Delete all rocks in specified chunk range around the player. Good for testing ore generation.")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 1, 50))
			.HandleWith(OnCmdDelrock)
			.EndSubCommand()
			.BeginSubCommand("delrockc")
			.WithDescription("Delete all rocks in specified chunk range around the world center. Good for testing ore generation.")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 1, 50))
			.HandleWith(OnCmdDelrockc)
			.EndSubCommand()
			.BeginSubCommand("del")
			.WithDescription("Delete chunks around the player")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 1, 50), parsers.OptionalWord("landform"))
			.HandleWith(OnCmdDel)
			.EndSubCommand()
			.BeginSubCommand("delr")
			.WithDescription("Delete chunks around the player and the map regions. This will allow that changed terrain can generate for example at story locations.")
			.RequiresPlayer()
			.WithArgs(parsers.IntRange("chunk_range", 1, 50))
			.HandleWith(OnCmdDelr)
			.EndSubCommand()
			.BeginSubCommand("delrange")
			.WithDescription("Delete a range of chunks. Start and end positions are in chunk coordinates. See CTRL + F3")
			.RequiresPlayer()
			.WithArgs(parsers.Int("x_start"), parsers.Int("z_start"), parsers.Int("x_end"), parsers.Int("z_end"))
			.HandleWith(OnCmdDelrange)
			.EndSubCommand()
			.BeginSubCommand("treemap")
			.WithDescription("treemap")
			.HandleWith(OnCmdTreemap)
			.EndSubCommand()
			.BeginSubCommand("testmap")
			.WithDescription("Generate a large noise map, to test noise generation")
			.WithPreCondition(DisallowHosted)
			.BeginSubCommand("climate")
			.WithDescription("Print a climate testmap")
			.HandleWith(OnCmdClimate)
			.EndSubCommand()
			.BeginSubCommand("geoact")
			.WithDescription("Print a geoact testmap")
			.WithArgs(parsers.OptionalInt("size", 512))
			.HandleWith(OnCmdGeoact)
			.EndSubCommand()
			.BeginSubCommand("climater")
			.WithDescription("Print a geoact testmap")
			.HandleWith(OnCmdClimater)
			.EndSubCommand()
			.BeginSubCommand("forest")
			.WithDescription("Print a forest testmap")
			.HandleWith(OnCmdForest)
			.EndSubCommand()
			.BeginSubCommand("upheavel")
			.WithDescription("Print a upheavel testmap")
			.WithArgs(parsers.OptionalInt("size", 512))
			.HandleWith(OnCmdUpheavel)
			.EndSubCommand()
			.BeginSubCommand("ocean")
			.WithDescription("Print a ocean testmap")
			.WithArgs(parsers.OptionalInt("size", 512))
			.HandleWith(OnCmdOcean)
			.EndSubCommand()
			.BeginSubCommand("ore")
			.WithDescription("Print a ore testmap")
			.WithArgs(parsers.OptionalFloat("scaleMul", 1f), parsers.OptionalFloat("contrast", 1f), parsers.OptionalFloat("sub"))
			.HandleWith(OnCmdOre)
			.EndSubCommand()
			.BeginSubCommand("oretopdistort")
			.WithDescription("Print a oretopdistort testmap")
			.HandleWith(OnCmdOretopdistort)
			.EndSubCommand()
			.BeginSubCommand("wind")
			.WithDescription("Print a wind testmap")
			.HandleWith(OnCmdWind)
			.EndSubCommand()
			.BeginSubCommand("gprov")
			.WithDescription("Print a gprov testmap")
			.HandleWith(OnCmdGprov)
			.EndSubCommand()
			.BeginSubCommand("landform")
			.WithDescription("Print a landform testmap")
			.HandleWith(OnCmdLandform)
			.EndSubCommand()
			.BeginSubCommand("rockstrata")
			.WithDescription("Print a rockstrata testmap")
			.HandleWith(OnCmdRockstrata)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("genmap")
			.WithDescription("Generate a large noise map around the players current location")
			.WithPreCondition(DisallowHosted)
			.BeginSubCommand("climate")
			.WithDescription("Generate a climate map")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalFloat("GeologicActivityStrength", 1f))
			.HandleWith(OnCmdGenmapClimate)
			.EndSubCommand()
			.BeginSubCommand("forest")
			.WithDescription("Generate a forest map")
			.RequiresPlayer()
			.HandleWith(OnCmdGenmapForest)
			.EndSubCommand()
			.BeginSubCommand("upheavel")
			.WithDescription("Generate a upheavel map")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalInt("size", 512))
			.HandleWith(OnCmdGenmapUpheavel)
			.EndSubCommand()
			.BeginSubCommand("mushroom")
			.WithDescription("Generate a mushroom map")
			.RequiresPlayer()
			.HandleWith(OnCmdGenmapMushroom)
			.EndSubCommand()
			.BeginSubCommand("ore")
			.WithDescription("Generate a ore map")
			.RequiresPlayer()
			.HandleWith(OnCmdGenmapOre)
			.EndSubCommand()
			.BeginSubCommand("gprov")
			.WithDescription("Generate a gprov map")
			.RequiresPlayer()
			.HandleWith(OnCmdGenmapGprov)
			.EndSubCommand()
			.BeginSubCommand("landform")
			.WithDescription("Generate a landform map")
			.RequiresPlayer()
			.HandleWith(OnCmdGenmapLandform)
			.EndSubCommand()
			.BeginSubCommand("ocean")
			.WithDescription("Generate a ocean map")
			.RequiresPlayer()
			.HandleWith(OnCmdGenmapOcean)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("stitchclimate")
			.WithDescription("Print a 3x3 stitched climate map")
			.RequiresPlayer()
			.HandleWith(OnCmdStitch)
			.EndSubCommand()
			.BeginSubCommand("region")
			.WithDescription("Extract already generated noise map data from the current region")
			.RequiresPlayer()
			.WithArgs(parsers.WordRange("sub_command", "climate", "ore", "forest", "upheavel", "ocean", "oretopdistort", "patches", "rockstrata", "gprov", "gprovi", "landform", "landformi"), parsers.OptionalBool("dolerp"), parsers.OptionalWord("orename"))
			.HandleWith(OnCmdRegion)
			.EndSubCommand()
			.BeginSubCommand("regions")
			.BeginSubCommand("ore")
			.WithDescription("Print a region ore map")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalInt("radius", 1), parsers.OptionalWord("orename"))
			.HandleWith(OnCmdRegionsOre)
			.EndSubCommand()
			.BeginSubCommand("upheavel")
			.WithDescription("Print a region upheavel map")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalInt("radius", 1))
			.HandleWith(OnCmdRegionsUpheavel)
			.EndSubCommand()
			.BeginSubCommand("climate")
			.WithDescription("Print a region climate map")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalInt("radius", 1))
			.HandleWith(OnCmdRegionsClimate)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("pos")
			.WithDescription("Print info for the current position")
			.RequiresPlayer()
			.WithArgs(parsers.WordRange("sub_command", "ymax", "coords", "latitude", "structures", "height", "cavedistort", "gprov", "rockstrata", "landform", "climate"))
			.HandleWith(OnCmdPos)
			.EndSubCommand()
			.BeginSubCommand("testnoise")
			.WithDescription("Testnoise command")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalInt("octaves", 1))
			.HandleWith(OnCmdTestnoise)
			.EndSubCommand()
			.BeginSubCommand("testvillage")
			.WithDescription("Testvillage command")
			.RequiresPlayer()
			.HandleWith(OnCmdTestVillage)
			.EndSubCommand();
	}

	private TextCommandResult handleToggleImpresWgen(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Meta block replacing and Item resolving for worldgen currently " + (GenStructures.ReplaceMetaBlocks ? "on" : "off"));
		}
		bool doReplace = (bool)args[0];
		GenStructures.ReplaceMetaBlocks = doReplace;
		return TextCommandResult.Success("Meta block replacing and Item resolving for worldgen now " + (doReplace ? "on" : "off"));
	}

	private TextCommandResult OnCmdTestVillage(TextCommandCallingArgs args)
	{
		if (api.Server.Config.HostedMode)
		{
			return TextCommandResult.Success(Lang.Get("Can't access this feature, server is in hosted mode"));
		}
		api.Assets.Reload(AssetCategory.worldgen);
		GenStructures ms = api.ModLoader.GetModSystem<GenStructures>();
		ms.initWorldGen();
		Vec3d pos = args.Caller.Pos;
		int chunkx = (int)pos.X / 32;
		int chunkz = (int)pos.Z / 32;
		IMapRegion mr = api.World.BlockAccessor.GetMapRegion((int)pos.X / _regionSize, (int)pos.Z / _regionSize);
		for (int i = 0; i < 50; i++)
		{
			int len = ms.vcfg.VillageTypes.Length;
			WorldGenVillage struc = ms.vcfg.VillageTypes[api.World.Rand.Next(len)];
			if (ms.GenVillage(api.World.BlockAccessor, mr, struc, chunkx, chunkz))
			{
				return TextCommandResult.Success($"Generated after {i + 1} tries");
			}
		}
		return TextCommandResult.Error("Unable to generate, likely not flat enough here.");
	}

	private TextCommandResult DisallowHosted(TextCommandCallingArgs args)
	{
		if (api.Server.Config.HostedMode)
		{
			return TextCommandResult.Error(Lang.Get("Can't access this feature, server is in hosted mode"));
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdRegion(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		BlockPos pos = player.Entity.Pos.AsBlockPos;
		IServerChunk serverchunk = api.WorldManager.GetChunk(pos);
		if (serverchunk == null)
		{
			return TextCommandResult.Success("Can't check here, beyond chunk boundaries!");
		}
		IMapRegion mapRegion = serverchunk.MapChunk.MapRegion;
		int regionX = pos.X / _regionSize;
		int regionZ = pos.Z / _regionSize;
		string subargtype = args[0] as string;
		bool dolerp = (bool)args[1];
		NoiseBase.Debug = true;
		switch (subargtype)
		{
		case "climate":
			DrawMapRegion(DebugDrawMode.RGB, args.Caller, mapRegion.ClimateMap, "climate", dolerp, regionX, regionZ, TerraGenConfig.climateMapScale);
			break;
		case "ore":
		{
			string type = (args.Parsers[2].IsMissing ? "limonite" : (args[2] as string));
			if (!mapRegion.OreMaps.ContainsKey(type))
			{
				player.SendMessage(args.Caller.FromChatGroupId, "Mapregion does not contain an ore map for ore " + type, EnumChatType.CommandError);
			}
			DrawMapRegion(DebugDrawMode.RGB, args.Caller, mapRegion.OreMaps[type], "ore-" + type, dolerp, regionX, regionZ, TerraGenConfig.oreMapScale);
			break;
		}
		case "forest":
			DrawMapRegion(DebugDrawMode.FirstByteGrayscale, args.Caller, mapRegion.ForestMap, "forest", dolerp, regionX, regionZ, TerraGenConfig.forestMapScale);
			break;
		case "upheavel":
			DrawMapRegion(DebugDrawMode.FirstByteGrayscale, args.Caller, mapRegion.UpheavelMap, "upheavel", dolerp, regionX, regionZ, TerraGenConfig.geoUpheavelMapScale);
			break;
		case "ocean":
			DrawMapRegion(DebugDrawMode.FirstByteGrayscale, args.Caller, mapRegion.OceanMap, "ocean", dolerp, regionX, regionZ, TerraGenConfig.oceanMapScale);
			break;
		case "oretopdistort":
			DrawMapRegion(DebugDrawMode.FirstByteGrayscale, args.Caller, mapRegion.OreMapVerticalDistortTop, "oretopdistort", dolerp, regionX, regionZ, TerraGenConfig.depositVerticalDistortScale);
			break;
		case "patches":
			foreach (KeyValuePair<string, IntDataMap2D> val in mapRegion.BlockPatchMaps)
			{
				DrawMapRegion(DebugDrawMode.FirstByteGrayscale, args.Caller, val.Value, val.Key, dolerp, regionX, regionZ, TerraGenConfig.forestMapScale);
			}
			player.SendMessage(args.Caller.FromChatGroupId, "Patch maps generated", EnumChatType.CommandSuccess);
			break;
		case "rockstrata":
		{
			for (int k = 0; k < mapRegion.RockStrata.Length; k++)
			{
				DrawMapRegion(DebugDrawMode.FirstByteGrayscale, args.Caller, mapRegion.RockStrata[k], "rockstrata" + k, dolerp, regionX, regionZ, TerraGenConfig.rockStrataScale);
			}
			break;
		}
		case "gprov":
			DrawMapRegion(DebugDrawMode.ProvinceRGB, args.Caller, mapRegion.GeologicProvinceMap, "province", dolerp, regionX, regionZ, TerraGenConfig.geoProvMapScale);
			break;
		case "gprovi":
		{
			int[] data2 = mapRegion.GeologicProvinceMap.Data;
			int noiseSizeGeoProv = mapRegion.GeologicProvinceMap.InnerSize;
			int outSize2 = (noiseSizeGeoProv + TerraGenConfig.geoProvMapPadding - 1) * TerraGenConfig.geoProvMapScale;
			GeologicProvinceVariant[] provincesByIndex = NoiseGeoProvince.provinces.Variants;
			LerpedWeightedIndex2DMap map2 = new LerpedWeightedIndex2DMap(data2, noiseSizeGeoProv + 2 * TerraGenConfig.geoProvMapPadding, 2, mapRegion.GeologicProvinceMap.TopLeftPadding, mapRegion.GeologicProvinceMap.BottomRightPadding);
			int[] outColors2 = new int[outSize2 * outSize2];
			for (int x2 = 0; x2 < outSize2; x2++)
			{
				for (int z2 = 0; z2 < outSize2; z2++)
				{
					WeightedIndex[] indices2 = map2[(float)x2 / (float)TerraGenConfig.geoProvMapScale, (float)z2 / (float)TerraGenConfig.geoProvMapScale];
					for (int j = 0; j < indices2.Length; j++)
					{
						indices2[j].Index = provincesByIndex[indices2[j].Index].ColorInt;
					}
					map2.Split(indices2, out var colors2, out var weights2);
					outColors2[z2 * outSize2 + x2] = ColorUtil.ColorAverage(colors2, weights2);
				}
			}
			NoiseBase.DebugDrawBitmap(DebugDrawMode.ProvinceRGB, outColors2, outSize2, outSize2, "geoprovince-lerped-" + regionX + "-" + regionZ);
			player.SendMessage(args.Caller.FromChatGroupId, "done", EnumChatType.CommandSuccess);
			break;
		}
		case "landform":
			DrawMapRegion(DebugDrawMode.LandformRGB, args.Caller, mapRegion.LandformMap, "landform", dolerp, regionX, regionZ, TerraGenConfig.landformMapScale);
			break;
		case "landformi":
		{
			int[] data = mapRegion.LandformMap.Data;
			int outSize = (mapRegion.LandformMap.InnerSize + TerraGenConfig.landformMapPadding - 1) * TerraGenConfig.landformMapScale;
			LandformVariant[] landformsByIndex = NoiseLandforms.landforms.LandFormsByIndex;
			LerpedWeightedIndex2DMap map = new LerpedWeightedIndex2DMap(data, mapRegion.LandformMap.Size, 1, mapRegion.LandformMap.TopLeftPadding, mapRegion.LandformMap.BottomRightPadding);
			int[] outColors = new int[outSize * outSize];
			for (int x = 0; x < outSize; x++)
			{
				for (int z = 0; z < outSize; z++)
				{
					WeightedIndex[] indices = map[(float)x / (float)TerraGenConfig.landformMapScale, (float)z / (float)TerraGenConfig.landformMapScale];
					for (int i = 0; i < indices.Length; i++)
					{
						indices[i].Index = landformsByIndex[indices[i].Index].ColorInt;
					}
					map.Split(indices, out var colors, out var weights);
					outColors[z * outSize + x] = ColorUtil.ColorAverage(colors, weights);
				}
			}
			NoiseBase.DebugDrawBitmap(DebugDrawMode.LandformRGB, outColors, outSize, outSize, "landform-lerped-" + regionX + "-" + regionZ);
			player.SendMessage(args.Caller.FromChatGroupId, "Landform map done", EnumChatType.CommandSuccess);
			break;
		}
		}
		NoiseBase.Debug = false;
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdDecopass(TextCommandCallingArgs args)
	{
		TerraGenConfig.DoDecorationPass = (bool)args[0];
		return TextCommandResult.Success("Decopass now " + (TerraGenConfig.DoDecorationPass ? "on" : "off"));
	}

	private TextCommandResult OnCmdAutogen(TextCommandCallingArgs args)
	{
		api.WorldManager.AutoGenerateChunks = (bool)args[0];
		return TextCommandResult.Success("Autogen now " + (api.WorldManager.AutoGenerateChunks ? "on" : "off"));
	}

	private TextCommandResult OnCmdGt(TextCommandCallingArgs args)
	{
		TerraGenConfig.GenerateVegetation = (bool)args[0];
		return TextCommandResult.Success("Generate trees now " + (TerraGenConfig.GenerateVegetation ? "on" : "off"));
	}

	private TextCommandResult OnCmdRegenk(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		string landform = args[1] as string;
		return RegenChunks(args.Caller, range, landform, aroundPlayer: true);
	}

	private TextCommandResult OnCmdRegen(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		string landform = args[1] as string;
		return RegenChunks(args.Caller, range, landform, aroundPlayer: true, randomSeed: false, deleteRegion: true);
	}

	private TextCommandResult OnCmdRegenr(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		string landform = args[1] as string;
		return RegenChunks(args.Caller, range, landform, aroundPlayer: true, randomSeed: true, deleteRegion: true);
	}

	private TextCommandResult OnCmdRegenc(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		string landform = args[1] as string;
		return RegenChunks(args.Caller, range, landform, aroundPlayer: false, randomSeed: false, deleteRegion: true);
	}

	private TextCommandResult OnCmdRegenrc(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		string landform = args[1] as string;
		return RegenChunks(args.Caller, range, landform, aroundPlayer: false, randomSeed: true, deleteRegion: true);
	}

	private TextCommandResult OnCmdPregen(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		return PregenerateChunksAroundPlayer(args.Caller, range);
	}

	private TextCommandResult OnCmdDelrock(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		DelRock(args.Caller, range, aroundPlayer: true);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdDelrockc(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		DelRock(args.Caller, range);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdDel(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		string landform = args[1] as string;
		return Regen(args.Caller, range, onlydelete: true, landform, aroundPlayer: true);
	}

	private TextCommandResult OnCmdDelr(TextCommandCallingArgs args)
	{
		int range = (int)args[0];
		return Regen(args.Caller, range, onlydelete: true, null, aroundPlayer: true, deleteRegion: true);
	}

	private TextCommandResult OnCmdDelrange(TextCommandCallingArgs args)
	{
		int xs = (int)args[0];
		int zs = (int)args[1];
		int xe = (int)args[2];
		int ze = (int)args[3];
		return DelChunkRange(new Vec2i(xs, zs), new Vec2i(xe, ze));
	}

	private TextCommandResult OnCmdTree(TextCommandCallingArgs args)
	{
		string asset = args[0] as string;
		float size = (float)args[1];
		float aheadoffset = (float)args[2];
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		return TestTree(player, asset, size, aheadoffset);
	}

	private TextCommandResult OnCmdTreelineup(TextCommandCallingArgs args)
	{
		string asset = args[0] as string;
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		return TreeLineup(player, asset);
	}

	private TextCommandResult OnCmdGenmapClimate(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		BlockPos asBlockPos = args.Caller.Entity.ServerPos.XYZ.AsBlockPos;
		int noiseSizeClimate = api.WorldManager.RegionSize / TerraGenConfig.climateMapScale;
		int regionX = asBlockPos.X / api.WorldManager.RegionSize;
		int num = asBlockPos.Z / api.WorldManager.RegionSize;
		int startX = regionX * noiseSizeClimate - 256;
		int startZ = num * noiseSizeClimate - 256;
		GenMaps modSystem = api.ModLoader.GetModSystem<GenMaps>();
		modSystem.initWorldGen();
		MapLayerBase climateGen = modSystem.climateGen;
		if (!args.Parsers[0].IsMissing)
		{
			float fac = (float)args[0];
			(((climateGen as MapLayerPerlinWobble).parent as MapLayerClimate).noiseMap as NoiseClimateRealistic).GeologicActivityStrength = fac;
			climateGen.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, startX, startZ, "climatemap-" + fac);
			NoiseBase.Debug = false;
			return TextCommandResult.Success("Geo activity map generated");
		}
		climateGen.DebugDrawBitmap(DebugDrawMode.RGB, startX, startZ, "climatemap");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Climate map generated");
	}

	private TextCommandResult OnCmdGenmapForest(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		GenMaps modSystem = api.ModLoader.GetModSystem<GenMaps>();
		modSystem.initWorldGen();
		MapLayerBase forestGen = modSystem.forestGen;
		BlockPos asBlockPos = args.Caller.Entity.ServerPos.XYZ.AsBlockPos;
		int regionX = asBlockPos.X / api.WorldManager.RegionSize;
		int num = asBlockPos.Z / api.WorldManager.RegionSize;
		int noiseSizeForest = api.WorldManager.RegionSize / TerraGenConfig.forestMapScale;
		int startX = regionX * noiseSizeForest - 256;
		int startZ = num * noiseSizeForest - 256;
		forestGen.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, startX, startZ, "forestmap");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Forest map generated");
	}

	private TextCommandResult OnCmdGenmapUpheavel(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		GenMaps genmapsSys = api.ModLoader.GetModSystem<GenMaps>();
		genmapsSys.initWorldGen();
		BlockPos asBlockPos = args.Caller.Entity.ServerPos.XYZ.AsBlockPos;
		int regionX = asBlockPos.X / api.WorldManager.RegionSize;
		int num = asBlockPos.Z / api.WorldManager.RegionSize;
		MapLayerBase upheavelGen = genmapsSys.upheavelGen;
		int noiseSizeUpheavel = api.WorldManager.RegionSize / TerraGenConfig.geoUpheavelMapScale;
		int startX = regionX * noiseSizeUpheavel - 256;
		int startZ = num * noiseSizeUpheavel - 256;
		int size = (int)args[0];
		upheavelGen.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, startX, startZ, size, "upheavelmap");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Upheavel map generated");
	}

	private TextCommandResult OnCmdGenmapMushroom(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		api.ModLoader.GetModSystem<GenMaps>().initWorldGen();
		BlockPos asBlockPos = args.Caller.Entity.ServerPos.XYZ.AsBlockPos;
		int regionX = asBlockPos.X / api.WorldManager.RegionSize;
		int num = asBlockPos.Z / api.WorldManager.RegionSize;
		int noiseSizeForest = api.WorldManager.RegionSize / TerraGenConfig.forestMapScale;
		int startX = regionX * noiseSizeForest - 256;
		int startZ = num * noiseSizeForest - 256;
		new MapLayerWobbled(api.World.Seed + 112897, 2, 0.9f, TerraGenConfig.forestMapScale, 4000f, -3000).DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, startX, startZ, "mushroom");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Mushroom maps generated");
	}

	private TextCommandResult OnCmdGenmapOre(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		NoiseBase.Debug = false;
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdGenmapGprov(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		int noiseSizeGeoProv = api.WorldManager.RegionSize / TerraGenConfig.geoProvMapScale;
		GenMaps modSystem = api.ModLoader.GetModSystem<GenMaps>();
		modSystem.initWorldGen();
		MapLayerBase geologicprovinceGen = modSystem.geologicprovinceGen;
		BlockPos asBlockPos = args.Caller.Entity.ServerPos.XYZ.AsBlockPos;
		int regionX = asBlockPos.X / api.WorldManager.RegionSize;
		int num = asBlockPos.Z / api.WorldManager.RegionSize;
		int startX = regionX * noiseSizeGeoProv - 256;
		int startZ = num * noiseSizeGeoProv - 256;
		geologicprovinceGen.DebugDrawBitmap(DebugDrawMode.ProvinceRGB, startX, startZ, "gprovmap");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Province map generated");
	}

	private TextCommandResult OnCmdGenmapLandform(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		int noiseSizeLandform = api.WorldManager.RegionSize / TerraGenConfig.landformMapScale;
		GenMaps modSystem = api.ModLoader.GetModSystem<GenMaps>();
		modSystem.initWorldGen();
		MapLayerBase landformsGen = modSystem.landformsGen;
		BlockPos asBlockPos = args.Caller.Entity.ServerPos.XYZ.AsBlockPos;
		int regionX = asBlockPos.X / api.WorldManager.RegionSize;
		int num = asBlockPos.Z / api.WorldManager.RegionSize;
		int startX = regionX * noiseSizeLandform - 256;
		int startZ = num * noiseSizeLandform - 256;
		landformsGen.DebugDrawBitmap(DebugDrawMode.LandformRGB, startX, startZ, "landformmap");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Landforms map generated");
	}

	private TextCommandResult OnCmdGenmapOcean(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		GenMaps modSystem = api.ModLoader.GetModSystem<GenMaps>();
		modSystem.initWorldGen();
		MapLayerBase oceanGen = modSystem.oceanGen;
		BlockPos asBlockPos = args.Caller.Entity.ServerPos.XYZ.AsBlockPos;
		int regionX = asBlockPos.X / api.WorldManager.RegionSize;
		int num = asBlockPos.Z / api.WorldManager.RegionSize;
		int noiseSizeOcean = api.WorldManager.RegionSize / TerraGenConfig.oceanMapScale;
		int startX = regionX * noiseSizeOcean - 256;
		int startZ = num * noiseSizeOcean - 256;
		oceanGen.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, startX, startZ, "oceanmap");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Ocean map generated");
	}

	private TextCommandResult OnCmdStitch(TextCommandCallingArgs args)
	{
		BlockPos pos = args.Caller.Entity.Pos.AsBlockPos;
		IServerChunk serverchunk = api.WorldManager.GetChunk(pos);
		if (serverchunk == null)
		{
			return TextCommandResult.Success("Can't check here, beyond chunk boundaries!");
		}
		IMapRegion mapRegion = serverchunk.MapChunk.MapRegion;
		int regionX = pos.X / _regionSize;
		int regionZ = pos.Z / _regionSize;
		MapLayerBase climateGen = api.ModLoader.GetModSystem<GenMaps>().climateGen;
		NoiseBase.Debug = true;
		int size = mapRegion.ClimateMap.InnerSize;
		int stitchSize = size * 3;
		int[] stitchedMap = new int[stitchSize * stitchSize];
		for (int dx = -1; dx <= 1; dx++)
		{
			for (int dz = -1; dz <= 1; dz++)
			{
				IntDataMap2D map = OnMapRegionGen(regionX + dx, regionZ + dz, climateGen);
				for (int px = 0; px < size; px++)
				{
					for (int py = 0; py < size; py++)
					{
						int col = map.GetUnpaddedInt(px, py);
						int y = (dz + 1) * size + py;
						int x = (dx + 1) * size + px;
						stitchedMap[y * stitchSize + x] = col;
					}
				}
			}
		}
		NoiseBase.DebugDrawBitmap(DebugDrawMode.RGB, stitchedMap, stitchSize, "climated-3x3-stitch");
		NoiseBase.Debug = false;
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdRegionsOre(TextCommandCallingArgs args)
	{
		BlockPos pos = args.Caller.Entity.Pos.AsBlockPos;
		IServerChunk serverchunk = api.WorldManager.GetChunk(pos);
		if (serverchunk == null)
		{
			return TextCommandResult.Success("Can't check here, beyond chunk boundaries!");
		}
		IMapRegion mapRegion = serverchunk.MapChunk.MapRegion;
		int regionX = pos.X / _regionSize;
		int regionZ = pos.Z / _regionSize;
		int radius = (int)args[0];
		NoiseBase.Debug = false;
		string type = (args.Parsers[1].IsMissing ? "limonite" : (args[1] as string));
		if (!mapRegion.OreMaps.ContainsKey(type))
		{
			return TextCommandResult.Success("Mapregion does not contain an ore map for ore " + type);
		}
		int oreMapSize = mapRegion.OreMaps[type].InnerSize;
		int len = (2 * radius + 1) * oreMapSize;
		int[] outPixels = new int[len * len];
		GenDeposits depsys = api.ModLoader.GetModSystem<GenDeposits>();
		api.ModLoader.GetModSystem<GenDeposits>().initWorldGen();
		for (int dx = -radius; dx <= radius; dx++)
		{
			for (int dz = -radius; dz <= radius; dz++)
			{
				mapRegion = api.World.BlockAccessor.GetMapRegion(regionX + dx, regionZ + dz);
				if (mapRegion == null)
				{
					continue;
				}
				mapRegion.OreMaps.Clear();
				depsys.OnMapRegionGen(mapRegion, regionX + dx, regionZ + dz);
				if (!mapRegion.OreMaps.ContainsKey(type))
				{
					return TextCommandResult.Success("Mapregion does not contain an ore map for ore " + type);
				}
				IntDataMap2D map = mapRegion.OreMaps[type];
				int baseX = (dx + radius) * oreMapSize;
				int baseZ = (dz + radius) * oreMapSize;
				for (int px = 0; px < map.InnerSize; px++)
				{
					for (int pz = 0; pz < map.InnerSize; pz++)
					{
						int pixel = map.GetUnpaddedInt(px, pz);
						outPixels[(pz + baseZ) * len + px + baseX] = pixel;
					}
				}
			}
		}
		NoiseBase.Debug = true;
		NoiseBase.DebugDrawBitmap(DebugDrawMode.RGB, outPixels, len, "ore-" + type + "around-" + regionX + "-" + regionZ);
		NoiseBase.Debug = false;
		return TextCommandResult.Success(type + " ore map generated.");
	}

	private TextCommandResult OnCmdRegionsClimate(TextCommandCallingArgs args)
	{
		BlockPos pos = args.Caller.Entity.Pos.AsBlockPos;
		IServerChunk serverchunk = api.WorldManager.GetChunk(pos);
		if (serverchunk == null)
		{
			return TextCommandResult.Success("Can't check here, beyond chunk boundaries!");
		}
		IMapRegion mapRegion = serverchunk.MapChunk.MapRegion;
		int regionX = pos.X / _regionSize;
		int regionZ = pos.Z / _regionSize;
		int radius = (int)args[0];
		NoiseBase.Debug = false;
		int oreMapSize = mapRegion.ClimateMap.InnerSize;
		int len = (2 * radius + 1) * oreMapSize;
		int[] outPixels = new int[len * len];
		for (int dx = -radius; dx <= radius; dx++)
		{
			for (int dz = -radius; dz <= radius; dz++)
			{
				mapRegion = api.World.BlockAccessor.GetMapRegion(regionX + dx, regionZ + dz);
				if (mapRegion == null)
				{
					continue;
				}
				IntDataMap2D map = mapRegion.ClimateMap;
				int baseX = (dx + radius) * oreMapSize;
				int baseZ = (dz + radius) * oreMapSize;
				for (int px = 0; px < map.InnerSize; px++)
				{
					for (int pz = 0; pz < map.InnerSize; pz++)
					{
						int pixel = map.GetUnpaddedInt(px, pz);
						outPixels[(pz + baseZ) * len + px + baseX] = pixel;
					}
				}
			}
		}
		NoiseBase.Debug = true;
		NoiseBase.DebugDrawBitmap(DebugDrawMode.RGB, outPixels, len, "climates-" + regionX + "-" + regionZ + "-" + radius);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("climate map generated.");
	}

	private TextCommandResult OnCmdRegionsUpheavel(TextCommandCallingArgs args)
	{
		BlockPos pos = args.Caller.Entity.Pos.AsBlockPos;
		IServerChunk serverchunk = api.WorldManager.GetChunk(pos);
		if (serverchunk == null)
		{
			return TextCommandResult.Success("Can't check here, beyond chunk boundaries!");
		}
		IMapRegion mapRegion = serverchunk.MapChunk.MapRegion;
		int regionX = pos.X / _regionSize;
		int regionZ = pos.Z / _regionSize;
		int radius = (int)args[0];
		NoiseBase.Debug = false;
		int oreMapSize = mapRegion.UpheavelMap.InnerSize;
		int len = (2 * radius + 1) * oreMapSize;
		int[] outPixels = new int[len * len];
		for (int dx = -radius; dx <= radius; dx++)
		{
			for (int dz = -radius; dz <= radius; dz++)
			{
				mapRegion = api.World.BlockAccessor.GetMapRegion(regionX + dx, regionZ + dz);
				if (mapRegion == null)
				{
					continue;
				}
				IntDataMap2D map = mapRegion.UpheavelMap;
				int baseX = (dx + radius) * oreMapSize;
				int baseZ = (dz + radius) * oreMapSize;
				for (int px = 0; px < map.InnerSize; px++)
				{
					for (int pz = 0; pz < map.InnerSize; pz++)
					{
						int pixel = map.GetUnpaddedInt(px, pz);
						outPixels[(pz + baseZ) * len + px + baseX] = pixel;
					}
				}
			}
		}
		NoiseBase.Debug = true;
		NoiseBase.DebugDrawBitmap(DebugDrawMode.RGB, outPixels, len, "upheavels-" + regionX + "-" + regionZ + "-" + radius);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("upheavel map generated.");
	}

	private TextCommandResult OnCmdPos(TextCommandCallingArgs args)
	{
		int chunkSize = 32;
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		BlockPos pos = args.Caller.Entity.Pos.AsBlockPos;
		IServerChunk serverchunk = api.WorldManager.GetChunk(pos);
		if (serverchunk == null)
		{
			return TextCommandResult.Success("Can't check here, beyond chunk boundaries!");
		}
		IMapRegion mapRegion = serverchunk.MapChunk.MapRegion;
		IMapChunk mapchunk = serverchunk.MapChunk;
		int regionChunkSize = api.WorldManager.RegionSize / chunkSize;
		int lx = pos.X % chunkSize;
		int lz = pos.Z % chunkSize;
		int chunkX = pos.X / chunkSize;
		int chunkZ = pos.Z / chunkSize;
		int regionX = pos.X / _regionSize;
		int regionZ = pos.Z / _regionSize;
		switch (args[0] as string)
		{
		case "ymax":
			return TextCommandResult.Success($"YMax: {serverchunk.MapChunk.YMax}");
		case "coords":
			return TextCommandResult.Success($"Chunk X/Z: {chunkX}/{chunkZ}, Region X/Z: {regionX},{regionZ}");
		case "latitude":
		{
			double? lat = api.World.Calendar.OnGetLatitude(pos.Z);
			return TextCommandResult.Success(string.Format("Latitude: {0:0.##}°, {1}", lat * 90.0, (lat < 0.0) ? "Southern Hemisphere" : "Northern Hemisphere"));
		}
		case "structures":
		{
			bool found = false;
			api.World.BlockAccessor.WalkStructures(pos, delegate(GeneratedStructure struc)
			{
				found = true;
				player.SendMessage(args.Caller.FromChatGroupId, "Structure with code " + struc.Code + " at this position", EnumChatType.CommandSuccess);
			});
			if (!found)
			{
				return TextCommandResult.Success("No structures at this position");
			}
			break;
		}
		case "height":
		{
			string str = $"Rain y={serverchunk.MapChunk.RainHeightMap[lz * chunkSize + lx]}, Worldgen terrain y={serverchunk.MapChunk.WorldGenTerrainHeightMap[lz * chunkSize + lx]}";
			player.SendMessage(args.Caller.FromChatGroupId, str, EnumChatType.CommandSuccess);
			break;
		}
		case "cavedistort":
		{
			SKBitmap bmp = new SKBitmap(chunkSize, chunkSize);
			for (int x = 0; x < chunkSize; x++)
			{
				for (int z = 0; z < chunkSize; z++)
				{
					byte color = mapchunk.CaveHeightDistort[z * chunkSize + x];
					bmp.SetPixel(x, z, new SKColor((byte)((uint)(color >> 16) & 0xFFu), (byte)((uint)(color >> 8) & 0xFFu), (byte)(color & 0xFFu)));
				}
			}
			bmp.Save("cavedistort" + chunkX + "-" + chunkZ + ".png");
			player.SendMessage(args.Caller.FromChatGroupId, "saved bitmap cavedistort" + chunkX + "-" + chunkZ + ".png", EnumChatType.CommandSuccess);
			break;
		}
		case "gprov":
		{
			int noiseSizeGeoProv2 = mapRegion.GeologicProvinceMap.InnerSize;
			float posXInRegion3 = ((float)pos.X / (float)_regionSize - (float)regionX) * (float)noiseSizeGeoProv2;
			float posZInRegion3 = ((float)pos.Z / (float)_regionSize - (float)regionZ) * (float)noiseSizeGeoProv2;
			GeologicProvinceVariant[] provincesByIndex = NoiseGeoProvince.provinces.Variants;
			WeightedIndex[] array3 = new LerpedWeightedIndex2DMap(mapRegion.GeologicProvinceMap.Data, mapRegion.GeologicProvinceMap.Size, TerraGenConfig.geoProvSmoothingRadius, mapRegion.GeologicProvinceMap.TopLeftPadding, mapRegion.GeologicProvinceMap.BottomRightPadding)[posXInRegion3, posZInRegion3];
			string text3 = "";
			WeightedIndex[] array2 = array3;
			for (int k = 0; k < array2.Length; k++)
			{
				WeightedIndex windex2 = array2[k];
				if (text3.Length > 0)
				{
					text3 += ", ";
				}
				text3 = text3 + (100f * windex2.Weight).ToString("#.#") + "% " + provincesByIndex[windex2.Index].Code;
			}
			player.SendMessage(args.Caller.FromChatGroupId, text3, EnumChatType.CommandSuccess);
			break;
		}
		case "rockstrata":
		{
			GenRockStrataNew rockstratagen = api.ModLoader.GetModSystem<GenRockStrataNew>();
			int noiseSizeGeoProv = mapRegion.GeologicProvinceMap.InnerSize;
			float posXInRegion2 = ((float)pos.X / (float)_regionSize - (float)(pos.X / _regionSize)) * (float)noiseSizeGeoProv;
			float posZInRegion2 = ((float)pos.Z / (float)_regionSize - (float)(pos.Z / _regionSize)) * (float)noiseSizeGeoProv;
			_ = NoiseGeoProvince.provinces.Variants;
			WeightedIndex[] indices = new LerpedWeightedIndex2DMap(mapRegion.GeologicProvinceMap.Data, mapRegion.GeologicProvinceMap.Size, TerraGenConfig.geoProvSmoothingRadius, mapRegion.GeologicProvinceMap.TopLeftPadding, mapRegion.GeologicProvinceMap.BottomRightPadding)[posXInRegion2, posZInRegion2];
			float[] rockGroupMaxThickness = new float[4];
			rockGroupMaxThickness[0] = (rockGroupMaxThickness[1] = (rockGroupMaxThickness[2] = (rockGroupMaxThickness[3] = 0f)));
			int rdx = chunkX % regionChunkSize;
			int rdz = chunkZ % regionChunkSize;
			float step = 0f;
			float distx = (float)rockstratagen.distort2dx.Noise(pos.X, pos.Z);
			float distz = (float)rockstratagen.distort2dz.Noise(pos.X, pos.Z);
			for (int i = 0; i < indices.Length; i++)
			{
				float w = indices[i].Weight;
				GeologicProvinceVariant var = NoiseGeoProvince.provinces.Variants[indices[i].Index];
				rockGroupMaxThickness[0] += var.RockStrataIndexed[0].ScaledMaxThickness * w;
				rockGroupMaxThickness[1] += var.RockStrataIndexed[1].ScaledMaxThickness * w;
				rockGroupMaxThickness[2] += var.RockStrataIndexed[2].ScaledMaxThickness * w;
				rockGroupMaxThickness[3] += var.RockStrataIndexed[3].ScaledMaxThickness * w;
			}
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Sedimentary max thickness: " + rockGroupMaxThickness[0]);
			sb.AppendLine("Metamorphic max thickness: " + rockGroupMaxThickness[1]);
			sb.AppendLine("Igneous max thickness: " + rockGroupMaxThickness[2]);
			sb.AppendLine("Volcanic max thickness: " + rockGroupMaxThickness[3]);
			sb.AppendLine("========");
			for (int id = 0; id < rockstratagen.strata.Variants.Length; id++)
			{
				IntDataMap2D rockMap = mapchunk.MapRegion.RockStrata[id];
				step = (float)rockMap.InnerSize / (float)regionChunkSize;
				GameMath.Clamp((distx + distz) / 30f, 0.9f, 1.1f);
				sb.AppendLine(rockstratagen.strata.Variants[id].BlockCode.ToShortString() + " max thickness: " + rockMap.GetIntLerpedCorrectly((float)rdx * step + step * ((float)lx + distx) / (float)chunkSize, (float)rdz * step + step * ((float)lz + distz) / (float)chunkSize));
			}
			sb.AppendLine("======");
			int terrainMapheightAt = api.World.BlockAccessor.GetTerrainMapheightAt(pos);
			int ylower = 1;
			int yupper = terrainMapheightAt;
			int rockStrataId = -1;
			float strataThickness = 0f;
			RockStratum stratum = null;
			OrderedDictionary<int, int> stratathicknesses = new OrderedDictionary<int, int>();
			while (ylower <= yupper)
			{
				if ((strataThickness -= 1f) <= 0f)
				{
					rockStrataId++;
					if (rockStrataId >= rockstratagen.strata.Variants.Length)
					{
						break;
					}
					stratum = rockstratagen.strata.Variants[rockStrataId];
					IntDataMap2D rockMap = mapchunk.MapRegion.RockStrata[rockStrataId];
					step = (float)rockMap.InnerSize / (float)regionChunkSize;
					int grp = (int)stratum.RockGroup;
					float dist = 1f + GameMath.Clamp((distx + distz) / 30f, 0.9f, 1.1f);
					strataThickness = Math.Min(rockGroupMaxThickness[grp] * dist, rockMap.GetIntLerpedCorrectly((float)rdx * step + step * ((float)lx + distx) / (float)chunkSize, (float)rdz * step + step * ((float)lz + distz) / (float)chunkSize));
					strataThickness -= ((stratum.RockGroup == EnumRockGroup.Sedimentary) ? ((float)Math.Max(0, yupper - TerraGenConfig.seaLevel) * 0.5f) : 0f);
					if (strataThickness < 2f)
					{
						strataThickness = -1f;
						continue;
					}
				}
				if (!stratathicknesses.ContainsKey(stratum.BlockId))
				{
					stratathicknesses[stratum.BlockId] = 0;
				}
				stratathicknesses[stratum.BlockId]++;
				if (stratum.GenDir == EnumStratumGenDir.BottomUp)
				{
					ylower++;
				}
				else
				{
					yupper--;
				}
			}
			foreach (KeyValuePair<int, int> val in stratathicknesses)
			{
				sb.AppendLine(api.World.Blocks[val.Key].Code.ToShortString() + " : " + val.Value + " blocks");
			}
			player.SendMessage(args.Caller.FromChatGroupId, sb.ToString(), EnumChatType.CommandSuccess);
			break;
		}
		case "landform":
		{
			int noiseSizeLandform = mapRegion.LandformMap.InnerSize;
			float posXInRegion = ((float)pos.X / (float)_regionSize - (float)(pos.X / _regionSize)) * (float)noiseSizeLandform;
			float posZInRegion = ((float)pos.Z / (float)_regionSize - (float)(pos.Z / _regionSize)) * (float)noiseSizeLandform;
			LandformVariant[] landforms = NoiseLandforms.landforms.LandFormsByIndex;
			IntDataMap2D intmap = mapRegion.LandformMap;
			WeightedIndex[] array = new LerpedWeightedIndex2DMap(intmap.Data, mapRegion.LandformMap.Size, TerraGenConfig.landFormSmoothingRadius, intmap.TopLeftPadding, intmap.BottomRightPadding)[posXInRegion, posZInRegion];
			string text2 = "";
			WeightedIndex[] array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				WeightedIndex windex = array2[j];
				if (text2.Length > 0)
				{
					text2 += ", ";
				}
				text2 = text2 + (100f * windex.Weight).ToString("#.#") + "% " + landforms[windex.Index].Code.ToShortString();
			}
			player.SendMessage(args.Caller.FromChatGroupId, text2, EnumChatType.CommandSuccess);
			break;
		}
		case "climate":
		{
			ClimateCondition climate = api.World.BlockAccessor.GetClimateAt(pos);
			string text = string.Format("Temperature: {0}°C, Year avg: {1}°C, Avg. Rainfall: {2}%, Geologic Activity: {3}%, Fertility: {4}%, Forest: {5}%, Shrub: {6}%, Sealevel dist: {7}%, Season: {8}, Hemisphere: {9}", climate.Temperature.ToString("0.#"), climate.WorldGenTemperature.ToString("0.#"), (int)(climate.WorldgenRainfall * 100f), (int)(climate.GeologicActivity * 100f), (int)(climate.Fertility * 100f), (int)(climate.ForestDensity * 100f), (int)(climate.ShrubDensity * 100f), (int)(100f * (float)pos.Y / 255f), api.World.Calendar.GetSeason(pos), api.World.Calendar.GetHemisphere(pos));
			player.SendMessage(args.Caller.FromChatGroupId, text, EnumChatType.CommandSuccess);
			break;
		}
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdTestnoise(TextCommandCallingArgs args)
	{
		bool use3d = false;
		int octaves = (int)args[0];
		long seed = new Random().Next();
		NormalizedSimplexNoise noise = NormalizedSimplexNoise.FromDefaultOctaves(octaves, 5.0, 0.7, seed);
		int size = 800;
		SKBitmap bitmap = new SKBitmap(size, size);
		int underflows = 0;
		int overflows = 0;
		float min = 1f;
		float max = 0f;
		for (int x = 0; x < size; x++)
		{
			for (int y = 0; y < size; y++)
			{
				double value = (use3d ? noise.Noise((double)x / (double)size, 0.0, (double)y / (double)size) : noise.Noise((double)x / (double)size, (double)y / (double)size));
				if (value < 0.0)
				{
					underflows++;
					value = 0.0;
				}
				if (value > 1.0)
				{
					overflows++;
					value = 1.0;
				}
				min = Math.Min((float)value, min);
				max = Math.Max((float)value, max);
				byte light = (byte)(value * 255.0);
				bitmap.SetPixel(x, y, new SKColor(light, light, light, byte.MaxValue));
			}
		}
		bitmap.Save("noise.png");
		string msg = (use3d ? "3D" : "2D") + " Noise (" + octaves + " Octaves) saved to noise.png. Overflows: " + overflows + ", Underflows: " + underflows;
		msg = msg + "\nNoise min = " + min.ToString("0.##") + ", max= " + max.ToString("0.##");
		return TextCommandResult.Success(msg);
	}

	private TextCommandResult OnCmdClimate(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		NoiseClimatePatchy noiseClimate = new NoiseClimatePatchy(_seed);
		GenMaps.GetClimateMapGen(_seed, noiseClimate);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Patchy climate map generated");
	}

	private TextCommandResult OnCmdGeoact(TextCommandCallingArgs args)
	{
		int polarEquatorDistance = api.WorldManager.SaveGame.WorldConfiguration.GetString("polarEquatorDistance", "50000").ToInt(50000);
		int size = (int)args[0];
		int spawnMinTemp = 6;
		int spawnMaxTemp = 14;
		NoiseBase.Debug = true;
		NoiseClimateRealistic noiseClimate = new NoiseClimateRealistic(_seed, api.World.BlockAccessor.MapSizeZ / TerraGenConfig.climateMapScale / TerraGenConfig.climateMapSubScale, polarEquatorDistance, spawnMinTemp, spawnMaxTemp);
		MapLayerBase climate = GenMaps.GetClimateMapGen(_seed, noiseClimate);
		NoiseBase.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, climate.GenLayer(0, 0, 128, 2048), 128, size, "geoactivity");
		return TextCommandResult.Success("Geologic activity map generated");
	}

	private TextCommandResult OnCmdClimater(TextCommandCallingArgs args)
	{
		ITreeAttribute worldConfiguration = api.WorldManager.SaveGame.WorldConfiguration;
		int polarEquatorDistance = worldConfiguration.GetString("polarEquatorDistance", "50000").ToInt(50000);
		int spawnMinTemp = 6;
		int spawnMaxTemp = 14;
		switch (worldConfiguration.GetString("worldClimate", "realistic"))
		{
		case "hot":
			spawnMinTemp = 28;
			spawnMaxTemp = 32;
			break;
		case "warm":
			spawnMinTemp = 19;
			spawnMaxTemp = 23;
			break;
		case "cool":
			spawnMinTemp = -5;
			spawnMaxTemp = 1;
			break;
		case "icy":
			spawnMinTemp = -15;
			spawnMaxTemp = -10;
			break;
		}
		NoiseBase.Debug = true;
		NoiseClimateRealistic noiseClimate = new NoiseClimateRealistic(_seed, api.World.BlockAccessor.MapSizeZ / TerraGenConfig.climateMapScale / TerraGenConfig.climateMapSubScale, polarEquatorDistance, spawnMinTemp, spawnMaxTemp);
		MapLayerBase climate = GenMaps.GetClimateMapGen(_seed, noiseClimate);
		NoiseBase.DebugDrawBitmap(DebugDrawMode.RGB, climate.GenLayer(0, 0, 128, 2048), 128, 2048, "realisticlimate");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Realistic climate map generated");
	}

	private TextCommandResult OnCmdForest(TextCommandCallingArgs args)
	{
		NoiseClimatePatchy noiseClimate = new NoiseClimatePatchy(_seed);
		MapLayerBase climate = GenMaps.GetClimateMapGen(_seed, noiseClimate);
		MapLayerBase forestMapGen = GenMaps.GetForestMapGen(_seed + 1, TerraGenConfig.forestMapScale);
		IntDataMap2D climateMap = new IntDataMap2D
		{
			Data = climate.GenLayer(0, 0, 512, 512),
			Size = 512
		};
		forestMapGen.SetInputMap(climateMap, new IntDataMap2D
		{
			Size = 512
		});
		NoiseBase.Debug = true;
		forestMapGen.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, 0, 0, "Forest 1 - Forest");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Forest map generated");
	}

	private TextCommandResult OnCmdUpheavel(TextCommandCallingArgs args)
	{
		int size = (int)args[0];
		MapLayerBase geoUpheavelMapGen = GenMaps.GetGeoUpheavelMapGen(_seed + 873, TerraGenConfig.geoUpheavelMapScale);
		NoiseBase.Debug = true;
		geoUpheavelMapGen.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, 0, 0, size, "Geoupheavel 1");
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Geo upheavel map generated");
	}

	private TextCommandResult OnCmdOcean(TextCommandCallingArgs args)
	{
		ITreeAttribute worldConfiguration = api.WorldManager.SaveGame.WorldConfiguration;
		int size = (int)args[0];
		float landcover = worldConfiguration.GetString("landcover", "1").ToFloat(1f);
		float oceanscale = worldConfiguration.GetString("oceanscale", "1").ToFloat(1f);
		int chunkSize = 32;
		List<XZ> list = api.ModLoader.GetModSystem<GenMaps>().requireLandAt;
		int startX = 0;
		int startZ = 0;
		if (args.Caller.Player != null)
		{
			startX = (int)args.Caller.Player.Entity.Pos.X / chunkSize;
			startZ = (int)args.Caller.Player.Entity.Pos.Z / chunkSize;
		}
		bool requiresSpawnOffset = GameVersion.IsLowerVersionThan(api.WorldManager.SaveGame.CreatedGameVersion, "1.20.0-pre.14");
		MapLayerBase oceanMapGen = GenMaps.GetOceanMapGen(_seed + 1873, landcover, TerraGenConfig.oceanMapScale, oceanscale, list, requiresSpawnOffset);
		NoiseBase.Debug = true;
		oceanMapGen.DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, startX, startZ, size, "Ocean 1-" + startX + "-" + startZ);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Ocean map generated");
	}

	private TextCommandResult OnCmdOre(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		NoiseOre noiseOre = new NoiseOre(_seed);
		float scaleMul = (float)args[0];
		float contrast = (float)args[1];
		float sub = (float)args[2];
		GenMaps.GetOreMap(_seed, noiseOre, scaleMul, contrast, sub);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("ore map generated");
	}

	private TextCommandResult OnCmdOretopdistort(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		GenMaps.GetDepositVerticalDistort(_seed);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Ore top distort map generated");
	}

	private TextCommandResult OnCmdWind(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		GenMaps.GetDebugWindMap(_seed);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Wind map generated");
	}

	private TextCommandResult OnCmdGprov(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		GenMaps.GetGeologicProvinceMapGen(_seed, api);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Province map generated");
	}

	private TextCommandResult OnCmdLandform(TextCommandCallingArgs args)
	{
		ITreeAttribute worldConfiguration = api.WorldManager.SaveGame.WorldConfiguration;
		NoiseBase.Debug = true;
		NoiseClimatePatchy noiseClimate = new NoiseClimatePatchy(_seed);
		float landformScale = worldConfiguration.GetString("landformScale", "1").ToFloat(1f);
		GenMaps.GetLandformMapGen(_seed + 1, noiseClimate, api, landformScale);
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Landforms map generated");
	}

	private TextCommandResult OnCmdRockstrata(TextCommandCallingArgs args)
	{
		NoiseBase.Debug = true;
		GenRockStrataNew mod = api.ModLoader.GetModSystem<GenRockStrataNew>();
		for (int i = 0; i < mod.strataNoises.Length; i++)
		{
			mod.strataNoises[i].DebugDrawBitmap(DebugDrawMode.FirstByteGrayscale, 0, 0, "Rockstrata-" + mod.strata.Variants[i].BlockCode.ToShortString().Replace(":", "-"));
		}
		NoiseBase.Debug = false;
		return TextCommandResult.Success("Rockstrata maps generated");
	}

	private TextCommandResult OnCmdTreemap(TextCommandCallingArgs args)
	{
		int chs = 3;
		byte[] pixels = new byte[131072 * chs];
		int w = 256;
		for (int x = 0; x < 256; x++)
		{
			for (int y = 0; y < 256; y++)
			{
				pixels[(y * w + x) * chs] = byte.MaxValue;
				pixels[(y * w + x) * chs + 1] = byte.MaxValue;
				pixels[(y * w + x) * chs + 2] = byte.MaxValue;
			}
		}
		WgenTreeSupplier wgenTreeSupplier = new WgenTreeSupplier(api);
		wgenTreeSupplier.LoadTrees();
		TreeVariant[] treeGens = wgenTreeSupplier.treeGenProps.TreeGens;
		Random rnd = new Random(123);
		int[] colors = new int[treeGens.Length];
		for (int i = 0; i < colors.Length; i++)
		{
			colors[i] = rnd.Next() | int.MinValue;
		}
		ImageSurface obj = (ImageSurface)ImageSurface.CreateForImage(pixels, Format.Rgb24, 256, 512);
		Context ctx = new Context(obj);
		obj.WriteToPng("treecoveragemap.png");
		ctx.Dispose();
		obj.Dispose();
		return TextCommandResult.Success("treecoveragemap.png created.");
	}

	private TextCommandResult DelChunkRange(Vec2i start, Vec2i end)
	{
		for (int x = start.X; x <= end.X; x++)
		{
			for (int z = start.Y; z <= end.Y; z++)
			{
				api.WorldManager.DeleteChunkColumn(x, z);
			}
		}
		return TextCommandResult.Success("Ok, chunk deletions enqueued, might take a while to process. Run command without args to see queue size");
	}

	private void DelRock(Caller caller, int rad, bool aroundPlayer = false)
	{
		IServerPlayer player = caller.Player as IServerPlayer;
		player.SendMessage(caller.FromChatGroupId, "Deleting rock, this may take a while...", EnumChatType.CommandError);
		int chunkMidX = api.WorldManager.MapSizeX / 32 / 2;
		int chunkMidZ = api.WorldManager.MapSizeZ / 32 / 2;
		if (aroundPlayer)
		{
			chunkMidX = (int)player.Entity.Pos.X / 32;
			chunkMidZ = (int)player.Entity.Pos.Z / 32;
		}
		List<Vec2i> coords = new List<Vec2i>();
		for (int x = -rad; x <= rad; x++)
		{
			for (int z = -rad; z <= rad; z++)
			{
				coords.Add(new Vec2i(chunkMidX + x, chunkMidZ + z));
			}
		}
		int chunksize = 32;
		IList<Block> blocks = api.World.Blocks;
		foreach (Vec2i coord in coords)
		{
			for (int cy = 0; cy < api.WorldManager.MapSizeY / 32; cy++)
			{
				IServerChunk chunk = api.WorldManager.GetChunk(coord.X, cy, coord.Y);
				if (chunk == null)
				{
					continue;
				}
				chunk.Unpack();
				for (int i = 0; i < chunk.Data.Length; i++)
				{
					Block block = blocks[chunk.Data[i]];
					if (block.BlockMaterial == EnumBlockMaterial.Stone || block.BlockMaterial == EnumBlockMaterial.Liquid || block.BlockMaterial == EnumBlockMaterial.Soil)
					{
						chunk.Data[i] = 0;
					}
				}
				chunk.MarkModified();
			}
			api.WorldManager.FullRelight(new BlockPos(coord.X * chunksize, 0, coord.Y * chunksize), new BlockPos(coord.X * chunksize, api.WorldManager.MapSizeY, coord.Y * chunksize));
		}
		player.CurrentChunkSentRadius = 0;
	}

	private TextCommandResult PregenerateChunksAroundPlayer(Caller caller, int range)
	{
		int chunkMidX;
		int chunkMidZ;
		if (caller.Type == EnumCallerType.Console)
		{
			chunkMidX = api.WorldManager.MapSizeX / 32 / 2;
			chunkMidZ = api.WorldManager.MapSizeX / 32 / 2;
		}
		else
		{
			IServerPlayer obj = caller.Player as IServerPlayer;
			chunkMidX = (int)obj.Entity.Pos.X / 32;
			chunkMidZ = (int)obj.Entity.Pos.Z / 32;
		}
		List<Vec2i> coords = new List<Vec2i>();
		for (int x = -range; x <= range; x++)
		{
			for (int z = -range; z <= range; z++)
			{
				coords.Add(new Vec2i(chunkMidX + x, chunkMidZ + z));
			}
		}
		LoadColumnsSlow(caller, coords, 0);
		return TextCommandResult.Success("Type /debug chunk queue to see current generating queue size");
	}

	private void LoadColumnsSlow(Caller caller, List<Vec2i> coords, int startIndex)
	{
		int qadded = 0;
		IServerPlayer player = caller.Player as IServerPlayer;
		if (api.WorldManager.CurrentGeneratingChunkCount < 10)
		{
			int batchSize = 200;
			for (int i = startIndex; i < coords.Count; i++)
			{
				qadded++;
				startIndex++;
				Vec2i coord = coords[i];
				api.WorldManager.LoadChunkColumn(coord.X, coord.Y);
				if (qadded > batchSize)
				{
					break;
				}
			}
			if (caller.Type == EnumCallerType.Console)
			{
				api.Logger.Notification("Ok, added {0} columns, {1} left to add, waiting until these are done.", qadded, coords.Count - startIndex);
			}
			else
			{
				player.SendMessage(caller.FromChatGroupId, $"Ok, added {qadded} columns, {coords.Count - startIndex} left to add, waiting until these are done.", EnumChatType.CommandSuccess);
			}
		}
		if (startIndex < coords.Count)
		{
			api.World.RegisterCallback(delegate
			{
				LoadColumnsSlow(caller, coords, startIndex);
			}, 1000);
		}
		else if (caller.Type == EnumCallerType.Console)
		{
			api.Logger.Notification("Ok, {0} columns, generated!", coords.Count);
		}
		else
		{
			player.SendMessage(caller.FromChatGroupId, $"Ok, {coords.Count} columns, generated!", EnumChatType.CommandSuccess);
		}
	}

	private TextCommandResult RegenChunks(Caller caller, int range, string landform = null, bool aroundPlayer = false, bool randomSeed = false, bool deleteRegion = false)
	{
		int seedDiff = 0;
		IServerPlayer player = caller.Player as IServerPlayer;
		if (randomSeed)
		{
			seedDiff = api.World.Rand.Next(100000);
			player.SendMessage(GlobalConstants.CurrentChatGroup, "Using random seed diff " + seedDiff, EnumChatType.Notification);
		}
		player.SendMessage(GlobalConstants.CurrentChatGroup, "Waiting for chunk thread to pause...", EnumChatType.Notification);
		TextCommandResult msg;
		if (api.Server.PauseThread("chunkdbthread"))
		{
			api.Assets.Reload(new AssetLocation("worldgen/"));
			api.ModLoader.GetModSystem<ModJsonPatchLoader>().ApplyPatches("worldgen/");
			NoiseLandforms.LoadLandforms(api);
			api.Event.TriggerInitWorldGen();
			msg = Regen(caller, range, onlydelete: false, landform, aroundPlayer, deleteRegion);
		}
		else
		{
			msg = TextCommandResult.Success("Unable to regenerate chunks. Was not able to pause the chunk gen thread");
		}
		api.Server.ResumeThread("chunkdbthread");
		return msg;
	}

	private TextCommandResult Regen(Caller caller, int rad, bool onlydelete, string landforms, bool aroundPlayer = false, bool deleteRegion = false)
	{
		int chunkMidX = api.WorldManager.MapSizeX / 32 / 2;
		int chunkMidZ = api.WorldManager.MapSizeZ / 32 / 2;
		IServerPlayer player = caller.Player as IServerPlayer;
		if (aroundPlayer)
		{
			chunkMidX = (int)player.Entity.Pos.X / 32;
			chunkMidZ = (int)player.Entity.Pos.Z / 32;
		}
		List<Vec2i> coords = new List<Vec2i>();
		HashSet<Vec2i> regCoords = new HashSet<Vec2i>();
		int regionChunkSize = api.WorldManager.RegionSize / 32;
		for (int x = -rad; x <= rad; x++)
		{
			for (int z = -rad; z <= rad; z++)
			{
				coords.Add(new Vec2i(chunkMidX + x, chunkMidZ + z));
				regCoords.Add(new Vec2i((chunkMidX + x) / regionChunkSize, (chunkMidZ + z) / regionChunkSize));
			}
		}
		GenStoryStructures modSys = api.ModLoader.GetModSystem<GenStoryStructures>();
		TreeAttribute tree = null;
		if (deleteRegion && !onlydelete)
		{
			Dictionary<long, List<GeneratedStructure>> regionStructures = new Dictionary<long, List<GeneratedStructure>>();
			int chunkSize = 32;
			foreach (Vec2i coord5 in coords)
			{
				long regionIndex2 = api.WorldManager.MapRegionIndex2D(coord5.X / regionChunkSize, coord5.Y / regionChunkSize);
				IMapRegion mapRegion2 = api.WorldManager.GetMapRegion(regionIndex2);
				if (mapRegion2 == null || mapRegion2.GeneratedStructures.Count <= 0)
				{
					continue;
				}
				regionStructures.TryAdd(regionIndex2, mapRegion2.GeneratedStructures);
				List<GeneratedStructure> structures2 = mapRegion2.GeneratedStructures.Where((GeneratedStructure s) => coord5.X == s.Location.X1 / chunkSize && coord5.Y == s.Location.Z1 / chunkSize).ToList();
				foreach (GeneratedStructure structure2 in structures2)
				{
					StoryStructureLocation location2 = modSys.GetStoryStructureAt(structure2.Location.X1, structure2.Location.Z1);
					if (location2 != null && modSys.storyStructureInstances.TryGetValue(location2.Code, out var structureInstance2) && structure2.Group != null)
					{
						Dictionary<string, int> schematicsSpawned = structureInstance2.SchematicsSpawned;
						if (schematicsSpawned != null && schematicsSpawned.TryGetValue(structure2.Group, out var spawned2))
						{
							structureInstance2.SchematicsSpawned[structure2.Group] = Math.Max(0, spawned2 - 1);
						}
					}
				}
				regionStructures[regionIndex2].RemoveAll((GeneratedStructure s) => structures2.Contains(s));
			}
			tree = new TreeAttribute();
			tree.SetBytes("GeneratedStructures", SerializerUtil.Serialize(regionStructures));
		}
		foreach (Vec2i coord4 in coords)
		{
			api.WorldManager.DeleteChunkColumn(coord4.X, coord4.Y);
		}
		if (deleteRegion)
		{
			foreach (Vec2i coord3 in regCoords)
			{
				api.WorldManager.DeleteMapRegion(coord3.X, coord3.Y);
			}
		}
		if (!onlydelete)
		{
			if (landforms != null)
			{
				if (tree == null)
				{
					tree = new TreeAttribute();
				}
				LandformVariant[] list = NoiseLandforms.landforms.LandFormsByIndex;
				int index = -1;
				for (int i = 0; i < list.Length; i++)
				{
					if (list[i].Code.Path.Equals(landforms))
					{
						index = i;
						break;
					}
				}
				if (index < 0)
				{
					return TextCommandResult.Success("No such landform exists");
				}
				tree.SetInt("forceLandform", index);
			}
			int leftToLoad = coords.Count;
			bool sent = false;
			api.WorldManager.SendChunks = false;
			foreach (Vec2i coord in coords)
			{
				api.WorldManager.LoadChunkColumnPriority(coord.X, coord.Y, new ChunkLoadOptions
				{
					OnLoaded = delegate
					{
						leftToLoad--;
						if (leftToLoad <= 0 && !sent)
						{
							modSys.FinalizeRegeneration(chunkMidX, chunkMidZ);
							sent = true;
							player.SendMessage(caller.FromChatGroupId, "Regen complete", EnumChatType.CommandSuccess);
							player.CurrentChunkSentRadius = 0;
							api.WorldManager.SendChunks = true;
							foreach (Vec2i current in coords)
							{
								for (int j = 0; j < api.WorldManager.MapSizeY / 32; j++)
								{
									api.WorldManager.BroadcastChunk(current.X, j, current.Y);
								}
							}
						}
					},
					ChunkGenParams = tree
				});
			}
		}
		else if (!deleteRegion)
		{
			foreach (Vec2i coord2 in coords)
			{
				long regionIndex = api.WorldManager.MapRegionIndex2D(coord2.X / regionChunkSize, coord2.Y / regionChunkSize);
				IMapRegion mapRegion = api.WorldManager.GetMapRegion(regionIndex);
				if (mapRegion == null || mapRegion.GeneratedStructures.Count <= 0)
				{
					continue;
				}
				List<GeneratedStructure> generatedStructures = mapRegion.GeneratedStructures;
				List<GeneratedStructure> structures = generatedStructures.Where((GeneratedStructure s) => coord2.X == s.Location.X1 / 32 && coord2.Y == s.Location.Z1 / 32).ToList();
				foreach (GeneratedStructure structure in structures)
				{
					StoryStructureLocation location = modSys.GetStoryStructureAt(structure.Location.X1, structure.Location.Z1);
					if (location != null && modSys.storyStructureInstances.TryGetValue(location.Code, out var structureInstance) && structure.Group != null)
					{
						Dictionary<string, int> schematicsSpawned2 = structureInstance.SchematicsSpawned;
						if (schematicsSpawned2 != null && schematicsSpawned2.TryGetValue(structure.Group, out var spawned))
						{
							structureInstance.SchematicsSpawned[structure.Group] = Math.Max(0, spawned - 1);
						}
					}
				}
				generatedStructures.RemoveAll((GeneratedStructure s) => structures.Contains(s));
			}
		}
		int diam = 2 * rad + 1;
		if (onlydelete)
		{
			return TextCommandResult.Success("Deleted " + diam + "x" + diam + " columns" + (deleteRegion ? " and regions" : ""));
		}
		return TextCommandResult.Success("Reloaded landforms and regenerating " + diam + "x" + diam + " columns" + (deleteRegion ? " and regions" : ""));
	}

	private TextCommandResult TestTree(IServerPlayer player, string asset, float size, float aheadoffset)
	{
		AssetLocation loc = new AssetLocation(asset);
		BlockPos pos = player.Entity.Pos.HorizontalAheadCopy(aheadoffset).AsBlockPos;
		IBlockAccessor blockAccessor = api.World.GetBlockAccessorBulkUpdate(synchronize: true, relight: true);
		while (blockAccessor.GetBlockId(pos) == 0 && pos.Y > 1)
		{
			pos.Down();
		}
		treeGenerators.ReloadTreeGenerators();
		if (treeGenerators.GetGenerator(loc) == null)
		{
			return TextCommandResult.Success("Cannot generate this tree, no such generator found");
		}
		treeGenerators.RunGenerator(loc, blockAccessor, pos, new TreeGenParams
		{
			size = size,
			skipForestFloor = true
		});
		blockAccessor.Commit();
		return TextCommandResult.Success(string.Concat(loc, " size ", size.ToString(), " generated."));
	}

	private TextCommandResult TreeLineup(IServerPlayer player, string asset)
	{
		BlockPos center = player.Entity.Pos.HorizontalAheadCopy(25.0).AsBlockPos;
		IBlockAccessor blockAccessor = api.World.GetBlockAccessorBulkUpdate(synchronize: true, relight: true, debug: true);
		AssetLocation loc = new AssetLocation(asset);
		int size = 12;
		for (int dx = -2 * size; dx < 2 * size; dx++)
		{
			for (int dz = -size; dz < size; dz++)
			{
				for (int dy = 0; dy < 2 * size; dy++)
				{
					blockAccessor.SetBlock(0, center.AddCopy(dx, dy, dz));
				}
			}
		}
		TreeGenParams pa = new TreeGenParams
		{
			size = 1f
		};
		treeGenerators.ReloadTreeGenerators();
		treeGenerators.RunGenerator(loc, blockAccessor, center.AddCopy(0, -1, 0), pa);
		treeGenerators.RunGenerator(loc, blockAccessor, center.AddCopy(-9, -1, 0), pa);
		treeGenerators.RunGenerator(loc, blockAccessor, center.AddCopy(9, -1, 0), pa);
		blockAccessor.Commit();
		return TextCommandResult.Success();
	}

	private IntDataMap2D OnMapRegionGen(int regionX, int regionZ, MapLayerBase climateGen)
	{
		int pad = 2;
		int noiseSizeClimate = api.WorldManager.RegionSize / TerraGenConfig.climateMapScale;
		IntDataMap2D obj = new IntDataMap2D
		{
			Data = climateGen.GenLayer(regionX * noiseSizeClimate - pad, regionZ * noiseSizeClimate - pad, noiseSizeClimate + 2 * pad, noiseSizeClimate + 2 * pad),
			Size = noiseSizeClimate + 2 * pad
		};
		obj.TopLeftPadding = (obj.BottomRightPadding = pad);
		return obj;
	}

	private void DrawMapRegion(DebugDrawMode mode, Caller caller, IntDataMap2D map, string prefix, bool lerp, int regionX, int regionZ, int scale)
	{
		IServerPlayer player = caller.Player as IServerPlayer;
		if (lerp)
		{
			int[] lerped = GameMath.BiLerpColorMap(map, scale);
			NoiseBase.DebugDrawBitmap(mode, lerped, map.InnerSize * scale, prefix + "-" + regionX + "-" + regionZ + "-l");
			player.SendMessage(caller.FromChatGroupId, "Lerped " + prefix + " map generated.", EnumChatType.CommandSuccess);
		}
		else
		{
			NoiseBase.DebugDrawBitmap(mode, map.Data, map.Size, prefix + "-" + regionX + "-" + regionZ);
			player.SendMessage(caller.FromChatGroupId, "Original " + prefix + " map generated.", EnumChatType.CommandSuccess);
		}
	}

	private TextCommandResult OnStructuresList(TextCommandCallingArgs args)
	{
		StringBuilder sb = new StringBuilder();
		if (args.Parsers[0].IsMissing)
		{
			for (int i = 0; i < _scfg.Structures.Length; i++)
			{
				WorldGenStructure structure = _scfg.Structures[i];
				StringBuilder stringBuilder = sb;
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(27, 4, stringBuilder);
				handler.AppendFormatted(i);
				handler.AppendLiteral(": Name: ");
				handler.AppendFormatted(structure.Name);
				handler.AppendLiteral(" - Code: ");
				handler.AppendFormatted(structure.Code);
				handler.AppendLiteral(" - Group: ");
				handler.AppendFormatted(structure.Group);
				stringBuilder2.AppendLine(ref handler);
				stringBuilder = sb;
				StringBuilder stringBuilder3 = stringBuilder;
				handler = new StringBuilder.AppendInterpolatedStringHandler(28, 2, stringBuilder);
				handler.AppendLiteral("     YOff: ");
				handler.AppendFormatted(structure.OffsetY);
				handler.AppendLiteral(" - MinGroupDist: ");
				handler.AppendFormatted(structure.MinGroupDistance);
				stringBuilder3.AppendLine(ref handler);
			}
			return TextCommandResult.Success(sb.ToString());
		}
		int structureNum = (int)args[0];
		if (structureNum < 0 || structureNum >= _scfg.Structures.Length)
		{
			return TextCommandResult.Success($"structureNum is out of range: 0-{_scfg.Structures.Length - 1}");
		}
		WorldGenStructure structures = _scfg.Structures[structureNum];
		for (int j = 0; j < structures.schematicDatas.Length; j++)
		{
			BlockSchematicStructure[] schematic = structures.schematicDatas[j];
			StringBuilder stringBuilder = sb;
			StringBuilder stringBuilder4 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(8, 2, stringBuilder);
			handler.AppendFormatted(j);
			handler.AppendLiteral(": File: ");
			handler.AppendFormatted(schematic[0].FromFileName);
			stringBuilder4.AppendLine(ref handler);
		}
		return TextCommandResult.Success(sb.ToString());
	}

	private TextCommandResult OnStructuresSpawn(TextCommandCallingArgs args)
	{
		int structureNum = (int)args[0];
		int schematicNum = (int)args[1];
		int schematicRot = (int)args[2];
		if (structureNum < 0 || structureNum >= _scfg.Structures.Length)
		{
			return TextCommandResult.Success($"structureNum is out of range: 0-{_scfg.Structures.Length - 1}");
		}
		WorldGenStructure struc = _scfg.Structures[structureNum];
		if (schematicNum < 0 || schematicNum >= struc.schematicDatas.Length)
		{
			return TextCommandResult.Success($"schematicNum is out of range: 0-{struc.schematicDatas.Length - 1}");
		}
		BlockPos pos = args.Caller.Player.CurrentBlockSelection?.Position.AddCopy(0, struc.OffsetY.GetValueOrDefault(), 0) ?? args.Caller.Pos.AsBlockPos.AddCopy(0, struc.OffsetY.GetValueOrDefault(), 0);
		BlockSchematicStructure schematic = struc.schematicDatas[schematicNum][schematicRot];
		int chunkX = pos.X / _chunksize;
		int chunkZ = pos.Z / _chunksize;
		int chunkY = pos.Y / _chunksize;
		switch (struc.Placement)
		{
		case EnumStructurePlacement.SurfaceRuin:
		case EnumStructurePlacement.Surface:
		{
			IntDataMap2D climateMap = api.WorldManager.GetChunk(chunkX, chunkY, chunkZ).MapChunk.MapRegion.ClimateMap;
			int rlX = chunkX % _regionChunkSize;
			int rlZ = chunkZ % _regionChunkSize;
			float facC = (float)climateMap.InnerSize / (float)_regionChunkSize;
			int climateUpLeft = climateMap.GetUnpaddedInt((int)((float)rlX * facC), (int)((float)rlZ * facC));
			int climateUpRight = climateMap.GetUnpaddedInt((int)((float)rlX * facC + facC), (int)((float)rlZ * facC));
			int climateBotLeft = climateMap.GetUnpaddedInt((int)((float)rlX * facC), (int)((float)rlZ * facC + facC));
			int climateBotRight = climateMap.GetUnpaddedInt((int)((float)rlX * facC + facC), (int)((float)rlZ * facC + facC));
			schematic.PlaceRespectingBlockLayers(api.World.BlockAccessor, api.World, pos, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight, struc.resolvedRockTypeRemaps, struc.replacewithblocklayersBlockids, GenStructures.ReplaceMetaBlocks);
			break;
		}
		case EnumStructurePlacement.Underground:
			if (struc.resolvedRockTypeRemaps != null)
			{
				schematic.PlaceReplacingBlocks(api.World.BlockAccessor, api.World, pos, schematic.ReplaceMode, struc.resolvedRockTypeRemaps, null, GenStructures.ReplaceMetaBlocks);
			}
			else
			{
				schematic.Place(api.World.BlockAccessor, api.World, pos, GenStructures.ReplaceMetaBlocks);
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case EnumStructurePlacement.Underwater:
			break;
		}
		return TextCommandResult.Success($"placing structure: {struc.Name} :: {schematic.FromFileName} placement: {struc.Placement}");
	}
}
