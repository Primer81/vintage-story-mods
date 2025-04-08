using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods.WorldEdit;

public class RaiseLowerTool : ToolBase
{
	public readonly NormalizedSimplexNoise noiseGen;

	public float Radius
	{
		get
		{
			return workspace.FloatValues["std.raiseLowerRadius"];
		}
		set
		{
			workspace.FloatValues["std.raiseLowerRadius"] = value;
		}
	}

	public float Depth
	{
		get
		{
			return workspace.FloatValues["std.raiseLowerDepth"];
		}
		set
		{
			workspace.FloatValues["std.raiseLowerDepth"] = value;
		}
	}

	public EnumHeightToolMode Mode
	{
		get
		{
			return (EnumHeightToolMode)workspace.IntValues["std.raiseLowerMode"];
		}
		set
		{
			workspace.IntValues["std.raiseLowerMode"] = (int)value;
		}
	}

	public override Vec3i Size => new Vec3i(0, 0, 0);

	public RaiseLowerTool()
	{
	}

	public RaiseLowerTool(WorldEditWorkspace workspace, IBlockAccessorRevertable blockAccessor)
		: base(workspace, blockAccessor)
	{
		noiseGen = NormalizedSimplexNoise.FromDefaultOctaves(2, 0.05, 0.8, 0L);
		if (!workspace.FloatValues.ContainsKey("std.raiseLowerRadius"))
		{
			Radius = 4f;
		}
		if (!workspace.FloatValues.ContainsKey("std.raiseLowerDepth"))
		{
			Depth = 3f;
		}
		if (!workspace.IntValues.ContainsKey("std.raiseLowerMode"))
		{
			Mode = EnumHeightToolMode.Uniform;
		}
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
				float.TryParse(args[1], out var size2);
				Radius = size2;
			}
			WorldEdit.Good(player, "Raise/Lower radius " + Radius + " set.");
			return true;
		case "tgr":
			Radius++;
			WorldEdit.Good(player, "Raise/Lower radius " + Radius + " set");
			return true;
		case "tsr":
			Radius = Math.Max(0f, Radius - 1f);
			WorldEdit.Good(player, "Raise/Lower radius " + Radius + " set");
			return true;
		case "tdepth":
			Depth = 0f;
			if (args.Length > 1)
			{
				float.TryParse(args[1], out var size);
				Depth = size;
			}
			WorldEdit.Good(player, "Raise/Lower depth " + Depth + " set.");
			return true;
		case "tm":
			Mode = EnumHeightToolMode.Uniform;
			if (args.Length > 1)
			{
				int.TryParse(args[1], out var mode);
				try
				{
					Mode = (EnumHeightToolMode)mode;
				}
				catch (Exception)
				{
				}
			}
			WorldEdit.Good(player, "Raise/Lower mode " + Mode.ToString() + " set.");
			return true;
		default:
			return false;
		}
	}

	public override void OnBreak(WorldEdit worldEdit, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		OnUse(worldEdit, blockSel.Position, 0, -1, null);
	}

	public override void OnBuild(WorldEdit worldEdit, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack)
	{
		OnUse(worldEdit, blockSel.Position, oldBlockId, 1, withItemStack);
	}

	private void OnUse(WorldEdit worldEdit, BlockPos pos, int oldBlockId, int sign, ItemStack withItemStack)
	{
		if (Radius <= 0f)
		{
			return;
		}
		int radInt = (int)Math.Ceiling(Radius);
		float radSq = Radius * Radius;
		Block block = ba.GetBlock(pos);
		if (sign > 0)
		{
			worldEdit.sapi.World.BlockAccessor.SetBlock(oldBlockId, pos);
		}
		float maxhgt = Depth;
		EnumHeightToolMode dist = Mode;
		int quantityBlocks = (int)((float)Math.PI * radSq) * (int)maxhgt;
		if (!workspace.MayPlace(block, quantityBlocks))
		{
			return;
		}
		for (int dx = -radInt; dx <= radInt; dx++)
		{
			for (int dz = -radInt; dz <= radInt; dz++)
			{
				float distanceSq = dx * dx + dz * dz;
				if (!(distanceSq > radSq))
				{
					BlockPos dpos = pos.AddCopy(dx, 0, dz);
					float height = (float)sign * maxhgt;
					switch (dist)
					{
					case EnumHeightToolMode.Pyramid:
						height *= 1f - distanceSq / radSq;
						break;
					case EnumHeightToolMode.Gaussian:
					{
						float sigmaSq = 0.1f;
						float sigma = GameMath.Sqrt(sigmaSq);
						float num = 1f / (sigma * GameMath.Sqrt((float)Math.PI * 2f));
						float x = distanceSq / radSq;
						double gaussValue = (double)num * Math.Exp((0f - x * x) / (2f * sigmaSq));
						height *= (float)gaussValue;
						break;
					}
					case EnumHeightToolMode.Perlin:
						height *= (float)noiseGen.Noise(dpos.X, dpos.Y, dpos.Z);
						break;
					}
					while (dpos.Y > 0 && ba.GetBlock(dpos).Replaceable >= 6000)
					{
						dpos.Down();
					}
					if (height < 0f)
					{
						Erode(0f - height, dpos);
						continue;
					}
					dpos.Up();
					Grow(worldEdit.sapi.World, height, dpos, block, BlockFacing.UP, withItemStack);
				}
			}
		}
		ba.SetHistoryStateBlock(pos.X, pos.Y, pos.Z, oldBlockId, ba.GetBlock(pos).Id);
		ba.Commit();
	}

	private void Grow(IWorldAccessor world, float quantity, BlockPos dpos, Block block, BlockFacing face, ItemStack withItemstack)
	{
		while (quantity-- >= 1f)
		{
			ba.SetBlock(block.BlockId, dpos, withItemstack);
			dpos.Up();
		}
	}

	private void Erode(float quantity, BlockPos dpos)
	{
		while (quantity-- >= 1f)
		{
			ba.SetBlock(0, dpos);
			dpos.Down();
		}
	}
}
