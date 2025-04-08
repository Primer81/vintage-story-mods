using System;
using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods.WorldEdit;

public class AirBrushTool : ToolBase
{
	private readonly Random _rand;

	private readonly LCGRandom _lcgRand;

	public float Radius
	{
		get
		{
			return workspace.FloatValues["std.airBrushRadius"];
		}
		set
		{
			workspace.FloatValues["std.airBrushRadius"] = value;
		}
	}

	public float Quantity
	{
		get
		{
			return workspace.FloatValues["std.airBrushQuantity"];
		}
		set
		{
			workspace.FloatValues["std.airBrushQuantity"] = value;
		}
	}

	public EnumAirBrushMode PlaceMode
	{
		get
		{
			return (EnumAirBrushMode)workspace.IntValues["std.airBrushPlaceMode"];
		}
		set
		{
			workspace.IntValues["std.airBrushPlaceMode"] = (int)value;
		}
	}

	public EnumAirBrushApply Apply
	{
		get
		{
			return (EnumAirBrushApply)workspace.IntValues["std.airBrushApply"];
		}
		set
		{
			workspace.IntValues["std.airBrushApply"] = (int)value;
		}
	}

	public EnumBrushMode BrushMode
	{
		get
		{
			return (EnumBrushMode)workspace.IntValues["std.airBrushMode"];
		}
		set
		{
			workspace.IntValues["std.airBrushMode"] = (int)value;
		}
	}

	public override Vec3i Size
	{
		get
		{
			int num = (int)(Radius * 2f);
			return new Vec3i(num, num, num);
		}
	}

	public AirBrushTool()
	{
	}

	public AirBrushTool(WorldEditWorkspace workspace, IBlockAccessorRevertable blockAccessor)
		: base(workspace, blockAccessor)
	{
		if (!workspace.FloatValues.ContainsKey("std.airBrushRadius"))
		{
			Radius = 8f;
		}
		if (!workspace.FloatValues.ContainsKey("std.airBrushQuantity"))
		{
			Quantity = 20f;
		}
		if (!workspace.IntValues.ContainsKey("std.airBrushApply"))
		{
			Apply = EnumAirBrushApply.AnyFace;
		}
		if (!workspace.IntValues.ContainsKey("std.airBrushPlaceMode"))
		{
			PlaceMode = EnumAirBrushMode.Add;
		}
		if (!workspace.IntValues.ContainsKey("std.airBrushMode"))
		{
			BrushMode = EnumBrushMode.ReplaceSelected;
		}
		_rand = new Random();
		_lcgRand = new LCGRandom(workspace.world.Seed);
	}

	public override bool OnWorldEditCommand(WorldEdit worldEdit, TextCommandCallingArgs callerArgs)
	{
		IServerPlayer player = (IServerPlayer)callerArgs.Caller.Player;
		CmdArgs args = callerArgs.RawArgs;
		switch (args[0])
		{
		case "tr":
			Radius = 0f;
			if (args.Length > 1)
			{
				float.TryParse(args[1], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var size);
				Radius = size;
			}
			WorldEdit.Good(player, "Air Brush Radius " + Radius + " set.");
			return true;
		case "tgr":
			Radius++;
			WorldEdit.Good(player, "Air Brush Radius " + Radius + " set");
			return true;
		case "tsr":
			Radius = Math.Max(0f, Radius - 1f);
			WorldEdit.Good(player, "Air Brush Radius " + Radius + " set");
			return true;
		case "tq":
			Quantity = 0f;
			if (args.Length > 1)
			{
				float.TryParse(args[1], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var quant);
				Quantity = quant;
			}
			WorldEdit.Good(player, "Quantity " + Quantity + " set.");
			return true;
		case "tm":
		{
			EnumBrushMode mode2 = EnumBrushMode.ReplaceSelected;
			if (args.Length > 1)
			{
				int.TryParse(args[1], out var index3);
				if (Enum.IsDefined(typeof(EnumBrushMode), index3))
				{
					mode2 = (EnumBrushMode)index3;
				}
			}
			BrushMode = mode2;
			WorldEdit.Good(player, workspace.ToolName + " mode " + mode2.ToString() + " set.");
			workspace.ResendBlockHighlights();
			return true;
		}
		case "tmp":
		{
			EnumAirBrushMode mode = EnumAirBrushMode.Add;
			if (args.Length > 1)
			{
				int.TryParse(args[1], out var index2);
				if (Enum.IsDefined(typeof(EnumAirBrushMode), index2))
				{
					mode = (EnumAirBrushMode)index2;
				}
			}
			PlaceMode = mode;
			WorldEdit.Good(player, workspace.ToolName + " mode " + mode.ToString() + " set.");
			workspace.ResendBlockHighlights();
			return true;
		}
		case "ta":
		{
			EnumAirBrushApply apply = EnumAirBrushApply.AnyFace;
			if (args.Length > 1)
			{
				int.TryParse(args[1], out var index);
				if (Enum.IsDefined(typeof(EnumAirBrushApply), index))
				{
					apply = (EnumAirBrushApply)index;
				}
			}
			Apply = apply;
			WorldEdit.Good(player, workspace.ToolName + " apply " + apply.ToString() + " set.");
			workspace.ResendBlockHighlights();
			return true;
		}
		default:
			return false;
		}
	}

	public override void OnBreak(WorldEdit worldEdit, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		OnApply(worldEdit, 0, blockSel, null, isbreak: true);
	}

	public override void OnBuild(WorldEdit worldEdit, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack)
	{
		Block placedBlock = ba.GetBlock(blockSel.Position);
		ToolBase.PlaceOldBlock(worldEdit, oldBlockId, blockSel, placedBlock);
		OnApply(worldEdit, oldBlockId, blockSel, withItemStack);
	}

	public void OnApply(WorldEdit worldEdit, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack, bool isbreak = false)
	{
		if (Quantity == 0f || Radius == 0f)
		{
			return;
		}
		float radSq = Radius * Radius;
		Block selectedBlock = (blockSel.DidOffset ? ba.GetBlock(blockSel.Position.AddCopy(blockSel.Face.Opposite)) : ba.GetBlock(blockSel.Position));
		Block block = (isbreak ? ba.GetBlock(0) : withItemStack.Block);
		if (!workspace.MayPlace(block, (int)radSq * 2))
		{
			return;
		}
		_lcgRand.SetWorldSeed(_rand.Next());
		_lcgRand.InitPositionSeed(blockSel.Position.X / 32, blockSel.Position.Z / 32);
		int xRadInt = (int)Math.Ceiling(Radius);
		int yRadInt = (int)Math.Ceiling(Radius);
		int zRadInt = (int)Math.Ceiling(Radius);
		HashSet<BlockPos> viablePositions = new HashSet<BlockPos>();
		EnumAirBrushMode mode = PlaceMode;
		EnumBrushMode bmode = BrushMode;
		for (int dx = -xRadInt; dx <= xRadInt; dx++)
		{
			for (int dy = -yRadInt; dy <= yRadInt; dy++)
			{
				for (int dz = -zRadInt; dz <= zRadInt; dz++)
				{
					if ((float)(dx * dx + dy * dy + dz * dz) > radSq)
					{
						continue;
					}
					BlockPos dpos = blockSel.Position.AddCopy(dx, dy, dz);
					Block hereBlock = ba.GetBlock(dpos);
					if (hereBlock.Replaceable >= 6000 || (bmode == EnumBrushMode.ReplaceSelected && hereBlock.Id != selectedBlock.Id))
					{
						continue;
					}
					for (int i = 0; i < 6; i++)
					{
						if (Apply == EnumAirBrushApply.SelectedFace && BlockFacing.ALLFACES[i] != blockSel.Face)
						{
							continue;
						}
						BlockPos ddpos = dpos.AddCopy(BlockFacing.ALLFACES[i]);
						Block dblock = ba.GetBlock(ddpos);
						if (dblock.Replaceable >= 6000 && dblock.IsLiquid() == block.IsLiquid())
						{
							if (mode == EnumAirBrushMode.Add)
							{
								viablePositions.Add(ddpos);
							}
							else
							{
								viablePositions.Add(dpos);
							}
						}
					}
				}
			}
		}
		List<BlockPos> viablePositionsList = new List<BlockPos>(viablePositions);
		float q = GameMath.Clamp(Quantity / 100f, 0f, 1f) * (float)viablePositions.Count;
		while (q-- > 0f && viablePositionsList.Count != 0 && (!(q < 1f) || !(_rand.NextDouble() > (double)q)))
		{
			int index = _rand.Next(viablePositionsList.Count);
			BlockPos dpos = viablePositionsList[index];
			viablePositionsList.RemoveAt(index);
			if (mode == EnumAirBrushMode.Add)
			{
				block.TryPlaceBlockForWorldGen(ba, dpos, BlockFacing.UP, _lcgRand);
			}
			else
			{
				ba.SetBlock(block.BlockId, dpos, withItemStack);
			}
		}
		ba.Commit();
	}
}
