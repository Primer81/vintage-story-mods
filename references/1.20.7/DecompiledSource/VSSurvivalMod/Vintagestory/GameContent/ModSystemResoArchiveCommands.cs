using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemResoArchiveCommands : ModSystem
{
	private ICoreServerAPI sapi;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("dev").WithDesc("Gamedev tools").RequiresPrivilege(Privilege.controlserver)
			.BeginSub("setmbname")
			.WithDesc("Set the name of a microblock")
			.WithArgs(parsers.WorldPosition("micro block position"), parsers.All("name"))
			.HandleWith(onSetMicroBlockName)
			.EndSub()
			.BeginSub("setlorecode")
			.WithDesc("Set the lore code of a bookshelf with lore")
			.WithArgs(parsers.OptionalWord("lorecode"))
			.HandleWith(onSetLoreCode)
			.EndSub()
			.BeginSub("settranslateable")
			.WithDesc("Set the looked at block as translatable, supported by sign blocks")
			.WithArgs(parsers.Bool("on or off"))
			.HandleWith(onSetTranslateable)
			.EndSub()
			.BeginSub("lampncfg")
			.WithDesc("Set the network code of a light source")
			.WithArgs(parsers.OptionalWord("networkcode"))
			.HandleWith(onLampNodeConfig)
			.EndSub()
			.BeginSub("pumponcmd")
			.WithDesc("Set command to run when the pump is on")
			.WithArgs(parsers.OptionalAll("command"))
			.HandleWith((TextCommandCallingArgs args) => onSetCmd(args, on: true))
			.EndSub()
			.BeginSub("pumpoffcmd")
			.WithDesc("Set command to run when the pump is off")
			.WithArgs(parsers.OptionalAll("command"))
			.HandleWith((TextCommandCallingArgs args) => onSetCmd(args, on: false))
			.EndSub()
			.BeginSub("musictrigger")
			.WithDesc("Configure the music trigger meta block")
			.WithArgs(parsers.WorldPosition("music trigger block"))
			.RequiresPlayer()
			.BeginSub("addarea")
			.WithDesc("Add trigger area")
			.WithArgs(parsers.Int("minX"), parsers.Int("minY"), parsers.Int("minZ"), parsers.Int("maxX"), parsers.Int("maxY"), parsers.Int("maxZ"))
			.HandleWith(onAddMusicTriggerArea)
			.EndSub()
			.BeginSub("modarea")
			.WithDesc("Modify trigger area")
			.WithArgs(parsers.Int("index"), parsers.Int("minX"), parsers.Int("minY"), parsers.Int("minZ"), parsers.Int("maxX"), parsers.Int("maxY"), parsers.Int("maxZ"))
			.HandleWith(onModMusicTriggerArea)
			.EndSub()
			.BeginSub("removearea")
			.WithDesc("Remove trigger area by index")
			.WithArgs(parsers.Int("index"))
			.HandleWith(onRemoveMusicTriggerArea)
			.EndSub()
			.BeginSub("hidearea")
			.WithDesc("Hide trigger preview")
			.HandleWith(onMusicTriggerHideArea)
			.EndSub()
			.BeginSub("showarea")
			.WithDesc("Show trigger preview")
			.HandleWith(onMusicTriggerShowArea)
			.EndSub()
			.BeginSub("settrack")
			.WithDesc("Set track file")
			.WithArgs(parsers.Word("track file location"))
			.HandleWith(onMusicTriggerSetTrack)
			.EndSub()
			.EndSub()
			.BeginSub("chiselfix")
			.WithDesc("Fixes game: chiseled blocks")
			.HandleWith(onChiselFix)
			.EndSub()
			.BeginSub("chiselsearchreplace")
			.WithDesc("Search&Replace chiseled block names in (worldedit) marked area. Wildcard (but no spaces) are allowed in the serach part")
			.WithArgs(parsers.Word("search"), parsers.All("replace"))
			.HandleWith((TextCommandCallingArgs args) => onSearchReplace(args, dryrun: false))
			.EndSub()
			.BeginSub("chiselsearchreplacedry")
			.WithDesc("Search&Replace chiseled block names in (worldedit) marked area. Dry run, will only print the finds.")
			.WithArgs(parsers.Word("search"))
			.HandleWith((TextCommandCallingArgs args) => onSearchReplace(args, dryrun: true))
			.EndSub()
			.Validate();
	}

	private TextCommandResult onChiselFix(TextCommandCallingArgs args)
	{
		BlockPos start = sapi.ObjectCache.Get("weStartMarker-" + args.Caller.Player.PlayerUID) as BlockPos;
		BlockPos end = sapi.ObjectCache.Get("weEndMarker-" + args.Caller.Player.PlayerUID) as BlockPos;
		if (start == null || end == null)
		{
			return TextCommandResult.Error("No area marked");
		}
		HashSet<string> foundtypes = new HashSet<string>();
		int foundtotal = 0;
		sapi.World.BlockAccessor.WalkBlocks(start, end, delegate(Block block, int x, int y, int z)
		{
			if (block is BlockChisel)
			{
				BlockEntityChisel blockEntity = sapi.World.BlockAccessor.GetBlockEntity<BlockEntityChisel>(new BlockPos(x, y, z));
				if (WildcardUtil.Match("game:*", blockEntity.BlockName))
				{
					foundtotal++;
					foundtypes.Add(blockEntity.BlockName);
					blockEntity.BlockName = sapi.World.Blocks[blockEntity.BlockIds[0]].GetPlacedBlockName(sapi.World, new BlockPos(x, y, z));
					blockEntity.MarkDirty(redrawOnClient: true);
				}
			}
		});
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "{0} chiseled block names replaced.", foundtotal));
	}

	private TextCommandResult onSearchReplace(TextCommandCallingArgs args, bool dryrun)
	{
		BlockPos start = sapi.ObjectCache.Get("weStartMarker-" + args.Caller.Player.PlayerUID) as BlockPos;
		BlockPos end = sapi.ObjectCache.Get("weEndMarker-" + args.Caller.Player.PlayerUID) as BlockPos;
		if (start == null || end == null)
		{
			return TextCommandResult.Error("No area marked");
		}
		string search = (string)args[0];
		string replace = (dryrun ? "" : ((string)args[1]));
		HashSet<string> foundtypes = new HashSet<string>();
		int foundtotal = 0;
		sapi.World.BlockAccessor.WalkBlocks(start, end, delegate(Block block, int x, int y, int z)
		{
			if (block is BlockChisel)
			{
				BlockEntityChisel blockEntity = sapi.World.BlockAccessor.GetBlockEntity<BlockEntityChisel>(new BlockPos(x, y, z));
				if (WildcardUtil.Match(search, blockEntity.BlockName))
				{
					foundtotal++;
					foundtypes.Add(blockEntity.BlockName);
					if (!dryrun)
					{
						blockEntity.BlockName = replace;
						blockEntity.MarkDirty(redrawOnClient: true);
					}
				}
			}
		});
		if (dryrun)
		{
			if (foundtypes.Count < 20)
			{
				sapi.Logger.Notification(string.Join(", ", foundtypes));
				return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "{0} chiseled blocks match your search. Names: {1}", foundtotal, string.Join(", ", foundtypes)));
			}
			sapi.Logger.Notification(string.Join(", ", foundtypes));
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "{0} chiseled blocks match your search. Names logged to server-main.log.", foundtotal));
		}
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "{0} chiseled block names replaced.", foundtotal));
	}

	private TextCommandResult onSetMicroBlockName(TextCommandCallingArgs args)
	{
		BlockPos pos = (args[0] as Vec3d).AsBlockPos;
		string name = args[1] as string;
		BlockEntityMicroBlock be = sapi.World.BlockAccessor.GetBlockEntity<BlockEntityMicroBlock>(pos);
		if (be == null)
		{
			return TextCommandResult.Error("Target block is not a microblock");
		}
		be.BlockName = name;
		be.MarkDirty(redrawOnClient: true);
		return TextCommandResult.Success("Microblock name set.");
	}

	private TextCommandResult onMusicTriggerSetTrack(TextCommandCallingArgs args)
	{
		BlockPos pos = (args[0] as Vec3d).AsBlockPos;
		BlockEntityMusicTrigger bec = sapi.World.BlockAccessor.GetBlockEntity<BlockEntityMusicTrigger>(pos);
		if (bec == null)
		{
			return TextCommandResult.Error("Selected block is not a music trigger");
		}
		bec.musicTrackLocation = new AssetLocation(args[1] as string);
		bec.MarkDirty(redrawOnClient: true);
		return TextCommandResult.Success("Ok, music track set");
	}

	private TextCommandResult onMusicTriggerHideArea(TextCommandCallingArgs args)
	{
		sapi.World.HighlightBlocks(args.Caller.Player, 1292, new List<BlockPos>(), EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cube);
		musicAreaPreview(null, args, show: false);
		return TextCommandResult.Success("Ok, preview area removed");
	}

	private TextCommandResult onMusicTriggerShowArea(TextCommandCallingArgs args)
	{
		BlockPos pos = (args[0] as Vec3d).AsBlockPos;
		BlockEntityMusicTrigger bec = sapi.World.BlockAccessor.GetBlockEntity<BlockEntityMusicTrigger>(pos);
		if (bec == null)
		{
			return TextCommandResult.Error("Selected block is not a music trigger");
		}
		musicAreaPreview(bec, args, show: true);
		return TextCommandResult.Success("Ok, preview area on");
	}

	private TextCommandResult onModMusicTriggerArea(TextCommandCallingArgs args)
	{
		BlockPos pos = (args[0] as Vec3d).AsBlockPos;
		BlockEntityMusicTrigger bec = sapi.World.BlockAccessor.GetBlockEntity<BlockEntityMusicTrigger>(pos);
		if (bec == null)
		{
			return TextCommandResult.Error("Selected block is not a music trigger");
		}
		int index = (int)args[1];
		if (bec.areas.Length <= index)
		{
			return TextCommandResult.Error("No such area at this index");
		}
		Cuboidi area = new Cuboidi((int)args[2], (int)args[3], (int)args[4], (int)args[5], (int)args[6], (int)args[7]);
		bec.areas[index] = area;
		bec.MarkDirty(redrawOnClient: true);
		musicAreaPreview(bec, args, show: true);
		return TextCommandResult.Success("Ok, area modified");
	}

	private TextCommandResult onAddMusicTriggerArea(TextCommandCallingArgs args)
	{
		BlockPos pos = (args[0] as Vec3d).AsBlockPos;
		if (args.Caller.Player != null && pos == null)
		{
			return TextCommandResult.Error("Need to look at a block");
		}
		BlockEntityMusicTrigger bec = sapi.World.BlockAccessor.GetBlockEntity<BlockEntityMusicTrigger>(pos);
		if (bec == null)
		{
			return TextCommandResult.Error("Selected block is not a music trigger");
		}
		Cuboidi area = new Cuboidi((int)args[1], (int)args[2], (int)args[3], (int)args[4], (int)args[5], (int)args[6]);
		bec.areas = (bec.areas ?? new Cuboidi[0]).Append(area);
		bec.MarkDirty(redrawOnClient: true);
		musicAreaPreview(bec, args, show: true);
		return TextCommandResult.Success("Ok, area added");
	}

	private TextCommandResult onRemoveMusicTriggerArea(TextCommandCallingArgs args)
	{
		BlockPos pos = (args[0] as Vec3d).AsBlockPos;
		if (args.Caller.Player != null && pos == null)
		{
			return TextCommandResult.Error("Need to look at a block");
		}
		BlockEntityMusicTrigger bec = sapi.World.BlockAccessor.GetBlockEntity<BlockEntityMusicTrigger>(pos);
		if (bec == null)
		{
			return TextCommandResult.Error("Selected block is not a music trigger");
		}
		List<Cuboidi> list = new List<Cuboidi>(bec.areas);
		int index = (int)args[1];
		if (index >= list.Count)
		{
			return TextCommandResult.Error("Supplied index is out of range. Maybe list is empty?");
		}
		list.RemoveAt(index);
		bec.areas = list.ToArray();
		bec.MarkDirty(redrawOnClient: true);
		musicAreaPreview(bec, args, show: true);
		return TextCommandResult.Success("Ok, area removed");
	}

	private void musicAreaPreview(BlockEntityMusicTrigger bec, TextCommandCallingArgs args, bool show)
	{
		List<BlockPos> minmax = new List<BlockPos>();
		if (show)
		{
			if (bec.areas == null)
			{
				return;
			}
			Cuboidi[] areas = bec.areas;
			foreach (Cuboidi area in areas)
			{
				minmax.Add(bec.Pos.AddCopy(area.MinX, area.MinY, area.MinZ));
				minmax.Add(bec.Pos.AddCopy(area.MaxX, area.MaxY, area.MaxZ));
			}
		}
		sapi.World.HighlightBlocks(args.Caller.Player, 1292, minmax, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes);
	}

	private TextCommandResult onSetTranslateable(TextCommandCallingArgs args)
	{
		BlockPos pos = ((args.Caller.Player == null) ? args.Caller.Pos.AsBlockPos : args.Caller.Player?.CurrentBlockSelection?.Position);
		if (args.Caller.Player != null && pos == null)
		{
			return TextCommandResult.Error("Need to look at a block");
		}
		if (!(sapi.World.BlockAccessor.GetBlockEntity(pos) is BlockEntitySign bec))
		{
			return TextCommandResult.Error("Selected block is not sign block");
		}
		bool on = (bool)args[0];
		bec.Translateable = on;
		return TextCommandResult.Success("Translatable " + (on ? "enabled" : "disabled"));
	}

	private TextCommandResult onSetLoreCode(TextCommandCallingArgs args)
	{
		BlockPos pos = ((args.Caller.Player == null) ? args.Caller.Pos.AsBlockPos : args.Caller.Player?.CurrentBlockSelection?.Position);
		if (args.Caller.Player != null && pos == null)
		{
			return TextCommandResult.Error("Need to look at a block");
		}
		BEBehaviorClutterBookshelfWithLore bec = sapi.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorClutterBookshelfWithLore>();
		if (bec == null)
		{
			return TextCommandResult.Error("Selected block is not a bookshelf with lore");
		}
		bec.LoreCode = args[0] as string;
		bec.Blockentity.MarkDirty(redrawOnClient: true);
		return TextCommandResult.Success("Lore code set");
	}

	private TextCommandResult onSetCmd(TextCommandCallingArgs args, bool on)
	{
		BlockPos pos = ((args.Caller.Player == null) ? args.Caller.Pos.AsBlockPos : args.Caller.Player?.CurrentBlockSelection?.Position);
		if (args.Caller.Player != null && pos == null)
		{
			return TextCommandResult.Error("Need to look at a block");
		}
		BEBehaviorJonasHydraulicPump bec = sapi.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorJonasHydraulicPump>();
		if (bec == null)
		{
			return TextCommandResult.Error("Selected block is not a command block");
		}
		string cmds = args[0] as string;
		if (on)
		{
			bec.oncommands = cmds;
		}
		else
		{
			bec.offcommands = cmds;
		}
		bec.Blockentity.MarkDirty(redrawOnClient: true);
		if (cmds == null || cmds.Length == 0)
		{
			return TextCommandResult.Success((on ? "On" : "Off") + " Command cleared.");
		}
		return TextCommandResult.Success((on ? "On" : "Off") + " Command " + cmds.Replace("{", "{{").Replace("}", "}}") + " set.");
	}

	private TextCommandResult onLampNodeConfig(TextCommandCallingArgs args)
	{
		BlockPos pos = ((args.Caller.Player == null) ? args.Caller.Pos.AsBlockPos : args.Caller.Player?.CurrentBlockSelection?.Position);
		if (args.Caller.Player != null && pos == null)
		{
			return TextCommandResult.Error("Need to look at a block");
		}
		INetworkedLight beh = sapi.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<INetworkedLight>();
		if (beh == null)
		{
			return TextCommandResult.Error("Selected block is not a lamp node");
		}
		beh.setNetwork(args[0] as string);
		return TextCommandResult.Success("Network " + args[0]?.ToString() + " set.");
	}
}
