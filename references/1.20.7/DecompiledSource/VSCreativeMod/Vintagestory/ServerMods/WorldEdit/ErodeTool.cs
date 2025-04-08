using System;
using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods.WorldEdit;

public class ErodeTool : ToolBase
{
	private double[,] kernel;

	public float BrushRadius
	{
		get
		{
			return workspace.FloatValues["std.erodeToolBrushRadius"];
		}
		set
		{
			workspace.FloatValues["std.erodeToolBrushRadius"] = value;
		}
	}

	public int KernelRadius
	{
		get
		{
			return workspace.IntValues["std.erodeToolKernelRadius"];
		}
		set
		{
			workspace.IntValues["std.erodeToolKernelRadius"] = value;
		}
	}

	public int Iterations
	{
		get
		{
			return workspace.IntValues["std.erodeToolIterations"];
		}
		set
		{
			workspace.IntValues["std.erodeToolIterations"] = value;
		}
	}

	public bool UseSelectedBlock
	{
		get
		{
			return workspace.IntValues["std.useSelectedBlock"] > 0;
		}
		set
		{
			workspace.IntValues["std.useSelectedBlock"] = (value ? 1 : 0);
		}
	}

	public override Vec3i Size
	{
		get
		{
			int num = (int)(BrushRadius * 2f);
			return new Vec3i(num, num, num);
		}
	}

	public ErodeTool()
	{
	}

	public ErodeTool(WorldEditWorkspace workspace, IBlockAccessorRevertable blockAccessor)
		: base(workspace, blockAccessor)
	{
		if (!workspace.FloatValues.ContainsKey("std.erodeToolBrushRadius"))
		{
			BrushRadius = 10f;
		}
		if (!workspace.FloatValues.ContainsKey("std.erodeToolKernelRadius"))
		{
			KernelRadius = 2;
		}
		if (!workspace.IntValues.ContainsKey("std.erodeToolIterations"))
		{
			Iterations = 1;
		}
		if (!workspace.IntValues.ContainsKey("std.useSelectedBlock"))
		{
			UseSelectedBlock = false;
		}
		PrecalcKernel();
	}

	private void PrecalcKernel()
	{
		int blurRad = KernelRadius;
		double sigma = (double)blurRad / 2.0;
		kernel = GameMath.GenGaussKernel(sigma, 2 * blurRad + 1);
	}

	public override bool OnWorldEditCommand(WorldEdit worldEdit, TextCommandCallingArgs callerArgs)
	{
		IServerPlayer player = (IServerPlayer)callerArgs.Caller.Player;
		CmdArgs args = callerArgs.RawArgs;
		switch (args[0])
		{
		case "tusb":
		{
			bool val = args.PopBool(false).Value;
			if (val)
			{
				WorldEdit.Good(player, "Will use only selected block for placement");
			}
			else
			{
				WorldEdit.Good(player, "Will use erode away placed blocks");
			}
			UseSelectedBlock = val;
			return true;
		}
		case "tr":
			BrushRadius = 0f;
			if (args.Length > 1)
			{
				float.TryParse(args[1], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var size);
				BrushRadius = size;
			}
			WorldEdit.Good(player, "Erode radius " + BrushRadius + " set");
			return true;
		case "tgr":
			BrushRadius++;
			WorldEdit.Good(player, "Erode radius " + BrushRadius + " set");
			return true;
		case "tsr":
			BrushRadius = Math.Max(0f, BrushRadius - 1f);
			WorldEdit.Good(player, "Erode radius " + BrushRadius + " set");
			return true;
		case "tkr":
			KernelRadius = 0;
			if (args.Length > 1)
			{
				int.TryParse(args[1], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var size2);
				KernelRadius = size2;
			}
			if (KernelRadius > 10)
			{
				KernelRadius = 10;
				worldEdit.SendPlayerWorkSpace(workspace.PlayerUID);
				WorldEdit.Good(player, "Erode kernel radius " + KernelRadius + " set (limited to 10)");
			}
			else
			{
				WorldEdit.Good(player, "Erode kernel radius " + KernelRadius + " set");
			}
			PrecalcKernel();
			return true;
		case "ti":
			Iterations = 1;
			if (args.Length > 1)
			{
				int.TryParse(args[1], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var iters);
				Iterations = iters;
			}
			if (Iterations > 10)
			{
				Iterations = 10;
				worldEdit.SendPlayerWorkSpace(workspace.PlayerUID);
				WorldEdit.Good(player, "Iterations " + Iterations + " set (limited to 10)");
			}
			else
			{
				WorldEdit.Good(player, "Iterations " + Iterations + " set");
			}
			return true;
		default:
			return false;
		}
	}

	public override void OnInteractStart(WorldEdit worldEdit, BlockSelection blockSel)
	{
		if (BrushRadius <= 0f)
		{
			return;
		}
		Block blockToPlace = ba.GetBlock(blockSel.Position);
		int quantityBlocks = (int)((float)Math.PI * BrushRadius * BrushRadius * (float)(4 * KernelRadius * KernelRadius) * (float)Iterations);
		quantityBlocks *= 4;
		if (workspace.MayPlace(blockToPlace, quantityBlocks))
		{
			int q = Iterations;
			while (q-- > 0)
			{
				ApplyErode(worldEdit, ba, blockSel.Position, blockToPlace, null);
			}
			ba.Commit();
		}
	}

	private void ApplyErode(WorldEdit worldEdit, IBlockAccessor blockAccessor, BlockPos pos, Block blockToPlace, ItemStack withItemStack)
	{
		int radInt = (int)Math.Ceiling(BrushRadius);
		float radSq = BrushRadius * BrushRadius;
		int blurRad = KernelRadius;
		Block prevBlock = ba.GetBlock(0);
		bool useSelected = UseSelectedBlock;
		int mapSizeY = worldEdit.sapi.WorldManager.MapSizeY;
		for (int dx = -radInt; dx <= radInt; dx++)
		{
			for (int dz = -radInt; dz <= radInt; dz++)
			{
				if ((float)(dx * dx + dz * dz) > radSq)
				{
					continue;
				}
				double avgHeight = 0.0;
				BlockPos dpos;
				for (int lx = -blurRad; lx <= blurRad; lx++)
				{
					for (int lz = -blurRad; lz <= blurRad; lz++)
					{
						dpos = pos.AddCopy(dx + lx, 0, dz + lz);
						while (dpos.Y < mapSizeY && blockAccessor.GetBlockId(dpos) != 0)
						{
							dpos.Up();
						}
						while (dpos.Y > 0 && blockAccessor.GetBlockId(dpos) == 0)
						{
							dpos.Down();
						}
						avgHeight += (double)dpos.Y * kernel[lx + blurRad, lz + blurRad];
					}
				}
				dpos = pos.AddCopy(dx, 0, dz);
				while (dpos.Y < mapSizeY && blockAccessor.GetBlockId(dpos) != 0)
				{
					dpos.Up();
				}
				while (dpos.Y > 0 && (prevBlock = blockAccessor.GetBlock(dpos)).BlockId == 0)
				{
					dpos.Down();
				}
				if (!(Math.Abs((double)dpos.Y - avgHeight) < 0.36))
				{
					if ((double)dpos.Y > avgHeight)
					{
						blockAccessor.SetBlock(0, dpos);
					}
					else if (useSelected)
					{
						ba.SetBlock(blockToPlace.BlockId, dpos.Up(), withItemStack);
					}
					else
					{
						ba.SetBlock(prevBlock.BlockId, dpos.Up());
					}
				}
			}
		}
	}
}
