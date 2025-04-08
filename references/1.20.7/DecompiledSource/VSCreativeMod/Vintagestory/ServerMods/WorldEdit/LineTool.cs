using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods.WorldEdit;

public class LineTool : ToolBase
{
	private BlockPos startPos;

	public EnumLineStartPoint LineMode
	{
		get
		{
			return (EnumLineStartPoint)workspace.IntValues["std.lineStartPoint"];
		}
		set
		{
			workspace.IntValues["std.lineStartPoint"] = (int)value;
		}
	}

	public bool PlaceMode
	{
		get
		{
			return workspace.IntValues["std.lineRemove"] == 1;
		}
		set
		{
			workspace.IntValues["std.lineRemove"] = (value ? 1 : 0);
		}
	}

	public override Vec3i Size => new Vec3i(0, 0, 0);

	public LineTool()
	{
	}

	public LineTool(WorldEditWorkspace workspace, IBlockAccessorRevertable blockAccess)
		: base(workspace, blockAccess)
	{
		if (!workspace.IntValues.ContainsKey("std.lineStartPoint"))
		{
			LineMode = EnumLineStartPoint.LineStrip;
		}
		if (!workspace.IntValues.ContainsKey("std.lineRemove"))
		{
			PlaceMode = false;
		}
	}

	public override bool OnWorldEditCommand(WorldEdit worldEdit, TextCommandCallingArgs callerArgs)
	{
		IServerPlayer player = (IServerPlayer)callerArgs.Caller.Player;
		CmdArgs args = callerArgs.RawArgs;
		string text = args[0];
		if (!(text == "tremove"))
		{
			if (text == "tm")
			{
				EnumLineStartPoint startpoint = EnumLineStartPoint.LineStrip;
				if (args.Length > 1)
				{
					int.TryParse(args[1], out var index);
					if (Enum.IsDefined(typeof(EnumLineStartPoint), index))
					{
						startpoint = (EnumLineStartPoint)index;
					}
				}
				LineMode = startpoint;
				WorldEdit.Good(player, workspace.ToolName + " mode " + startpoint.ToString() + " set.");
				workspace.ResendBlockHighlights();
				return true;
			}
			return false;
		}
		PlaceMode = args.Length > 1 && (args[1] == "1" || args[1] == "on");
		WorldEdit.Good(player, workspace.ToolName + " remove mode now " + (PlaceMode ? "on" : "off"));
		return true;
	}

	public override void OnBreak(WorldEdit worldEdit, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		startPos = blockSel.Position.Copy();
		WorldEdit.Good((IServerPlayer)worldEdit.sapi.World.PlayerByUid(workspace.PlayerUID), "Line Tool start position set");
	}

	public override void OnBuild(WorldEdit worldEdit, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack)
	{
		if (startPos == null)
		{
			return;
		}
		BlockPos destPos = blockSel.Position.AddCopy(blockSel.Face.Opposite);
		Block block = (PlaceMode ? ba.GetBlock(0) : withItemStack.Block);
		worldEdit.sapi.World.BlockAccessor.SetBlock(oldBlockId, blockSel.Position);
		if (workspace.MayPlace(block, startPos.ManhattenDistance(destPos)))
		{
			GameMath.BresenHamPlotLine3d(startPos.X, startPos.Y, startPos.Z, destPos.X, destPos.Y, destPos.Z, delegate(BlockPos pos)
			{
				ba.SetBlock(block.BlockId, pos, withItemStack);
			});
			if (LineMode == EnumLineStartPoint.LineStrip)
			{
				startPos = destPos.Copy();
			}
			ba.Commit();
		}
	}
}
