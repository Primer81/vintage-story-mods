using System;
using System.Collections.Generic;
using VSCreativeMod;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods.WorldEdit;

public class SelectTool : ToolBase
{
	private IBlockAccessor blockAccess;

	private HashSet<Block> edgeBlocksCache = new HashSet<Block>();

	public virtual string Prefix => "std.select";

	public override bool ScrollEnabled => true;

	public bool MagicSelect
	{
		get
		{
			return workspace.IntValues[Prefix + "magicSelect"] > 0;
		}
		set
		{
			workspace.IntValues[Prefix + "magicSelect"] = (value ? 1 : 0);
		}
	}

	public string[] EdgeBlocks
	{
		get
		{
			return workspace.StringValues[Prefix + "magicEdgeBlocks"].Split(',');
		}
		private set
		{
			workspace.StringValues[Prefix + "magicEdgeBlocks"] = string.Join(",", value);
		}
	}

	public override Vec3i Size => new Vec3i(0, 0, 0);

	public SelectTool()
	{
	}

	public SelectTool(WorldEditWorkspace workspace, IBlockAccessorRevertable blockAccess)
		: base(workspace, blockAccess)
	{
		if (!workspace.IntValues.ContainsKey(Prefix + "magicSelect"))
		{
			MagicSelect = false;
		}
		if (!workspace.StringValues.ContainsKey(Prefix + "magicEdgeBlocks"))
		{
			SetEdgeBlocks(workspace.world, new string[3] { "air", "soil-*", "tallgrass-*" });
		}
		else
		{
			SetEdgeBlocks(workspace.world, EdgeBlocks);
		}
	}

	public void SetEdgeBlocks(IWorldAccessor world, string[] blockCodes)
	{
		EdgeBlocks = blockCodes;
		edgeBlocksCache = new HashSet<Block>();
		AssetLocation[] blockCodesA = new AssetLocation[blockCodes.Length];
		for (int j = 0; j < blockCodesA.Length; j++)
		{
			blockCodesA[j] = new AssetLocation(blockCodes[j]);
		}
		foreach (Block block in world.Blocks)
		{
			if (block?.Code == null)
			{
				continue;
			}
			for (int i = 0; i < blockCodesA.Length; i++)
			{
				if (block.WildCardMatch(blockCodesA[i]))
				{
					edgeBlocksCache.Add(block);
				}
			}
		}
	}

	public override bool OnWorldEditCommand(WorldEdit worldEdit, TextCommandCallingArgs callerArgs)
	{
		IServerPlayer player = (IServerPlayer)callerArgs.Caller.Player;
		CmdArgs args = callerArgs.RawArgs;
		switch (args.PopWord())
		{
		case "normalize":
		{
			BlockPos tmpEnd = workspace.EndMarker.Copy();
			BlockPos tmpStart = workspace.StartMarker.Copy();
			workspace.StartMarkerExact = new Vec3d(Math.Min(tmpStart.X, tmpEnd.X), Math.Min(tmpStart.InternalY, tmpEnd.InternalY), Math.Min(tmpStart.Z, tmpEnd.Z));
			workspace.StartMarkerExact.Add(0.5);
			workspace.EndMarkerExact = new Vec3d(Math.Max(tmpStart.X, tmpEnd.X), Math.Max(tmpStart.InternalY, tmpEnd.InternalY), Math.Max(tmpStart.Z, tmpEnd.Z));
			workspace.EndMarkerExact.Add(-0.5);
			workspace.UpdateSelection();
			if (args.PopWord() != "quiet")
			{
				WorldEdit.Good(player, "Start and End marker normalized");
			}
			return true;
		}
		case "magic":
			MagicSelect = args.PopBool(false).Value;
			WorldEdit.Good(player, "Magic select now " + (MagicSelect ? "on" : "off"));
			return true;
		case "edgeblocks":
			switch (args.PopWord("list"))
			{
			case "list":
				WorldEdit.Good(player, "Edge blocks: " + string.Join(", ", EdgeBlocks));
				break;
			case "add":
			{
				string blockcode = args.PopAll();
				if (matchesAnyBlock(worldEdit.sapi, blockcode))
				{
					EdgeBlocks = EdgeBlocks.Append(args.PopAll());
					WorldEdit.Good(player, "Ok, edge block '" + blockcode + "' added.");
					SetEdgeBlocks(worldEdit.sapi.World, EdgeBlocks);
				}
				else
				{
					WorldEdit.Good(player, "Error, block code/wildcard '" + blockcode + "' does not match any known blocks.");
				}
				break;
			}
			case "remove":
			{
				List<string> elems = new List<string>(EdgeBlocks);
				if (elems.Remove(args.PopAll()))
				{
					WorldEdit.Good(player, "Ok, edge block removed.");
					SetEdgeBlocks(worldEdit.sapi.World, elems.ToArray());
				}
				else
				{
					WorldEdit.Good(player, "No such edge block in list.");
				}
				break;
			}
			default:
				WorldEdit.Bad(player, "Invalid arg. Syntax: /we edgeblocks or /we edgeblocks [list|add|remove] [blockcode]");
				break;
			}
			return true;
		default:
			return false;
		}
	}

	private bool matchesAnyBlock(ICoreAPI api, string blockcode)
	{
		AssetLocation loc = new AssetLocation(blockcode);
		foreach (Block block in api.World.Blocks)
		{
			if (block.WildCardMatch(loc))
			{
				return true;
			}
		}
		return false;
	}

	public override void OnAttackStart(WorldEdit worldEdit, BlockSelection blockSelection)
	{
		if (blockSelection != null && !MagicSelect)
		{
			workspace.SetStartPos(blockSelection.Position.ToVec3d().Add(0.5));
		}
	}

	public override void OnInteractStart(WorldEdit worldEdit, BlockSelection blockSelection)
	{
		if (blockSelection != null)
		{
			blockAccess = worldEdit.sapi.World.BlockAccessor;
			if (MagicSelect)
			{
				Cuboidi sele = MagiSelect(blockSelection);
				workspace.SetStartPos(new Vec3d(sele.X1, sele.Y1, sele.Z1), update: false);
				workspace.SetEndPos(new Vec3d(sele.X2, sele.Y2, sele.Z2).Add(-0.5, -0.5, -0.5));
				workspace.revertableBlockAccess.StoreHistoryState(HistoryState.Empty());
			}
			else
			{
				workspace.SetEndPos(blockSelection.Position.ToVec3d().Add(0.5, 0.5, 0.5));
			}
		}
	}

	private Cuboidi MagiSelect(BlockSelection blockSelection)
	{
		BlockPos position = blockSelection.Position;
		Cuboidi selection = new Cuboidi(position, position.AddCopy(1, 1, 1));
		bool didGrowAny = true;
		int i = 0;
		while (didGrowAny && i < 600)
		{
			didGrowAny = TryGrowX(selection, positive: true) || TryGrowX(selection, positive: false) || TryGrowY(selection, positive: true) || TryGrowY(selection, positive: false) || TryGrowZ(selection, positive: true) || TryGrowZ(selection, positive: false);
			i++;
		}
		return selection;
	}

	private bool TryGrowX(Cuboidi selection, bool positive)
	{
		bool shouldInclude = false;
		int y = selection.Y1;
		while (!shouldInclude && y < selection.Y2)
		{
			int z = selection.Z1;
			while (!shouldInclude && z < selection.Z2)
			{
				int x = (positive ? selection.X2 : (selection.X1 - 1));
				Block block = blockAccess.GetBlock(x, y, z);
				shouldInclude |= !edgeBlocksCache.Contains(block);
				z++;
			}
			y++;
		}
		if (shouldInclude)
		{
			if (positive)
			{
				selection.X2++;
			}
			else
			{
				selection.X1--;
			}
			return true;
		}
		return false;
	}

	private bool TryGrowY(Cuboidi selection, bool positive)
	{
		bool shouldInclude = false;
		int x = selection.X1;
		while (!shouldInclude && x < selection.X2)
		{
			int z = selection.Z1;
			while (!shouldInclude && z < selection.Z2)
			{
				int y = (positive ? selection.Y2 : (selection.Y1 - 1));
				Block block = blockAccess.GetBlock(x, y, z);
				shouldInclude |= !edgeBlocksCache.Contains(block);
				z++;
			}
			x++;
		}
		if (shouldInclude)
		{
			if (positive)
			{
				selection.Y2++;
			}
			else
			{
				selection.Y1--;
			}
			return true;
		}
		return false;
	}

	private bool TryGrowZ(Cuboidi selection, bool positive)
	{
		bool shouldInclude = false;
		int y = selection.Y1;
		while (!shouldInclude && y < selection.Y2)
		{
			int x = selection.X1;
			while (!shouldInclude && x < selection.X2)
			{
				int z = (positive ? selection.Z2 : (selection.Z1 - 1));
				Block block = blockAccess.GetBlock(x, y, z);
				shouldInclude |= !edgeBlocksCache.Contains(block);
				x++;
			}
			y++;
		}
		if (shouldInclude)
		{
			if (positive)
			{
				selection.Z2++;
			}
			else
			{
				selection.Z1--;
			}
			return true;
		}
		return false;
	}

	public override void Load(ICoreAPI api)
	{
		api.ModLoader.GetModSystem<WorldEdit>().SelectionMode(on: true, (IServerPlayer)api.World.PlayerByUid(workspace.PlayerUID));
	}

	public override void Unload(ICoreAPI api)
	{
		api.ModLoader.GetModSystem<WorldEdit>().SelectionMode(on: false, (IServerPlayer)api.World.PlayerByUid(workspace.PlayerUID));
	}

	public override List<SkillItem> GetAvailableModes(ICoreClientAPI capi)
	{
		string move = EnumWeToolMode.Move.ToString();
		string moveFar = EnumWeToolMode.MoveFar.ToString();
		string moveNear = EnumWeToolMode.MoveNear.ToString();
		List<SkillItem> list = new List<SkillItem>();
		list.Add(new SkillItem
		{
			Name = Lang.Get(move),
			Code = new AssetLocation(move)
		});
		list.Add(new SkillItem
		{
			Texture = capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/worldedit/moveNear.svg"), 48, 48, 5, -1),
			Name = Lang.Get(moveNear),
			Code = new AssetLocation(moveNear)
		});
		list.Add(new SkillItem
		{
			Texture = capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/worldedit/moveFar.svg"), 48, 48, 5, -1),
			Name = Lang.Get(moveFar),
			Code = new AssetLocation(moveFar)
		});
		list[0].WithIcon(capi, "move");
		return list;
	}
}
