using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods.WorldEdit;

public class GrowShrinkTool : ToolBase
{
	public float BrushRadius
	{
		get
		{
			return workspace.FloatValues["std.growShrinkRadius"];
		}
		set
		{
			workspace.FloatValues["std.growShrinkRadius"] = value;
		}
	}

	public EnumGrowShrinkMode GrowShrinkMode
	{
		get
		{
			return (EnumGrowShrinkMode)workspace.IntValues["std.growShrinkMode"];
		}
		set
		{
			workspace.IntValues["std.growShrinkMode"] = (int)value;
		}
	}

	public override Vec3i Size => new Vec3i((int)BrushRadius, (int)BrushRadius, (int)BrushRadius);

	public GrowShrinkTool()
	{
	}

	public GrowShrinkTool(WorldEditWorkspace workspace, IBlockAccessorRevertable blockAccess)
		: base(workspace, blockAccess)
	{
		if (!workspace.FloatValues.ContainsKey("std.growShrinkRadius"))
		{
			BrushRadius = 10f;
		}
		if (!workspace.IntValues.ContainsKey("std.growShrinkMode"))
		{
			GrowShrinkMode = EnumGrowShrinkMode.Any;
		}
	}

	public override void OnBreak(WorldEdit worldEdit, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		int oldBlockId = worldEdit.sapi.World.BlockAccessor.GetBlockId(blockSel.Position);
		GrowShrink(worldEdit, oldBlockId, blockSel, null, shrink: true);
	}

	public override void OnBuild(WorldEdit worldEdit, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack)
	{
		GrowShrink(worldEdit, oldBlockId, blockSel, withItemStack);
	}

	public bool GrowShrink(WorldEdit worldEdit, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack, bool shrink = false)
	{
		worldEdit.sapi.World.BlockAccessor.SetBlock(oldBlockId, blockSel.Position);
		ba.SetHistoryStateBlock(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, oldBlockId, ba.GetStagedBlockId(blockSel.Position));
		if (BrushRadius == 0f)
		{
			return false;
		}
		Block blockToPlace = (shrink ? ba.GetBlock(0) : withItemStack.Block);
		int selectedBlockID = (shrink ? ba.GetBlock(blockSel.Position).Id : ba.GetBlock(blockSel.Position.AddCopy(blockSel.Face.Opposite)).Id);
		int radInt = (int)Math.Ceiling(BrushRadius);
		float radSq = BrushRadius * BrushRadius;
		HashSet<BlockPos> viablePositions = new HashSet<BlockPos>();
		for (int dx = -radInt; dx <= radInt; dx++)
		{
			for (int dy = -radInt; dy <= radInt; dy++)
			{
				for (int dz = -radInt; dz <= radInt; dz++)
				{
					if ((float)(dx * dx + dy * dy + dz * dz) > radSq)
					{
						continue;
					}
					BlockPos dpos = blockSel.Position.AddCopy(dx, dy, dz);
					Block blockAtPos = ba.GetBlock(dpos);
					if (blockAtPos.Replaceable >= 6000 || (GrowShrinkMode == EnumGrowShrinkMode.SelectedBlock && blockAtPos.BlockId != selectedBlockID))
					{
						continue;
					}
					for (int i = 0; i < 6; i++)
					{
						BlockPos ddpos = dpos.AddCopy(BlockFacing.ALLFACES[i]);
						if (ba.GetBlock(ddpos).Replaceable >= 6000)
						{
							if (shrink)
							{
								viablePositions.Add(dpos);
								break;
							}
							viablePositions.Add(ddpos);
						}
					}
				}
			}
		}
		foreach (BlockPos p in viablePositions)
		{
			ba.SetBlock(blockToPlace.BlockId, p, withItemStack);
		}
		ba.Commit();
		return true;
	}

	public override bool OnWorldEditCommand(WorldEdit worldEdit, TextCommandCallingArgs callerArgs)
	{
		IServerPlayer player = (IServerPlayer)callerArgs.Caller.Player;
		CmdArgs args = callerArgs.RawArgs;
		switch (args[0])
		{
		case "tm":
		{
			EnumGrowShrinkMode mode = EnumGrowShrinkMode.Any;
			if (args.Length > 1)
			{
				int.TryParse(args[1], out var index);
				if (Enum.IsDefined(typeof(EnumGrowShrinkMode), index))
				{
					mode = (EnumGrowShrinkMode)index;
				}
			}
			GrowShrinkMode = mode;
			WorldEdit.Good(player, workspace.ToolName + " mode " + mode.ToString() + " set.");
			return true;
		}
		case "tr":
			BrushRadius = 0f;
			if (args.Length > 1)
			{
				float.TryParse(args[1], out var size);
				BrushRadius = size;
			}
			WorldEdit.Good(player, "Grow/Shrink radius " + BrushRadius + " set");
			return true;
		case "tgr":
			BrushRadius++;
			WorldEdit.Good(player, "Grow/Shrink radius " + BrushRadius + " set");
			return true;
		case "tsr":
			BrushRadius = Math.Max(0f, BrushRadius - 1f);
			WorldEdit.Good(player, "Grow/Shrink radius " + BrushRadius + " set");
			return true;
		default:
			return false;
		}
	}
}
