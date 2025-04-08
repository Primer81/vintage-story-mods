using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BEBehaviorMPLargeGear3m : BEBehaviorMPBase
{
	public float ratio = 5.5f;

	public BEBehaviorMPLargeGear3m(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		AxisSign = new int[3] { 0, 1, 0 };
		if (api.Side == EnumAppSide.Client)
		{
			Blockentity.RegisterGameTickListener(onEverySecond, 1000);
		}
	}

	public override bool isInvertedNetworkFor(BlockPos pos)
	{
		return propagationDir == BlockFacing.DOWN;
	}

	private void onEverySecond(float dt)
	{
		float speed = ((network == null) ? 0f : network.Speed);
		if (Api.World.Rand.NextDouble() < (double)(speed / 4f))
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/block/woodcreak"), (double)Position.X + 0.5, (double)Position.Y + 0.5, (double)Position.Z + 0.5, null, 0.85f + speed);
		}
	}

	public override void SetPropagationDirection(MechPowerPath path)
	{
		BlockFacing turnDir = path.NetworkDir();
		if (turnDir != BlockFacing.UP && turnDir != BlockFacing.DOWN)
		{
			turnDir = (path.IsInvertedTowards(Position) ? BlockFacing.UP : BlockFacing.DOWN);
			base.GearedRatio = path.gearingRatio / ratio;
		}
		else
		{
			base.GearedRatio = path.gearingRatio;
		}
		if (propagationDir == turnDir.Opposite && network != null)
		{
			if (!network.DirectionHasReversed)
			{
				network.TurnDir = ((network.TurnDir == EnumRotDirection.Clockwise) ? EnumRotDirection.Counterclockwise : EnumRotDirection.Clockwise);
			}
			network.DirectionHasReversed = true;
		}
		propagationDir = turnDir;
	}

	public override bool IsPropagationDirection(BlockPos fromPos, BlockFacing test)
	{
		if (propagationDir == test)
		{
			return true;
		}
		if (test.IsHorizontal)
		{
			if (fromPos.AddCopy(test) == Position)
			{
				return propagationDir == BlockFacing.DOWN;
			}
			if (fromPos.AddCopy(test.Opposite) == Position)
			{
				return propagationDir == BlockFacing.UP;
			}
		}
		return false;
	}

	public override float GetGearedRatio(BlockFacing face)
	{
		if (!face.IsHorizontal)
		{
			return base.GearedRatio;
		}
		return base.GearedRatio * ratio;
	}

	protected override MechPowerPath[] GetMechPowerExits(MechPowerPath pathDir)
	{
		BlockFacing face = pathDir.OutFacing;
		BELargeGear3m beg = Blockentity as BELargeGear3m;
		int index = 0;
		if (face == BlockFacing.UP || face == BlockFacing.DOWN)
		{
			MechPowerPath[] paths = new MechPowerPath[2 + beg.CountGears(Api)];
			paths[index] = pathDir;
			paths[++index] = new MechPowerPath(pathDir.OutFacing.Opposite, pathDir.gearingRatio, null, !pathDir.invert);
			bool sideInvert = (face == BlockFacing.DOWN) ^ pathDir.invert;
			for (int i = 0; i < 4; i++)
			{
				BlockFacing horizFace = BlockFacing.HORIZONTALS[i];
				if (beg.HasGearAt(Api, Position.AddCopy(horizFace)))
				{
					paths[++index] = new MechPowerPath(horizFace, pathDir.gearingRatio * ratio, null, sideInvert);
				}
			}
			return paths;
		}
		MechPowerPath[] pathss = new MechPowerPath[2 + beg.CountGears(Api)];
		bool invert = pathDir.IsInvertedTowards(Position);
		pathss[0] = new MechPowerPath(BlockFacing.DOWN, pathDir.gearingRatio / ratio, null, invert);
		pathss[1] = new MechPowerPath(BlockFacing.UP, pathDir.gearingRatio / ratio, null, !invert);
		index = 1;
		bool sidesInvert = (face == BlockFacing.DOWN) ^ !invert;
		for (int j = 0; j < 4; j++)
		{
			BlockFacing horizFace2 = BlockFacing.HORIZONTALS[j];
			if (beg.HasGearAt(Api, Position.AddCopy(horizFace2)))
			{
				pathss[++index] = new MechPowerPath(horizFace2, pathDir.gearingRatio, null, sidesInvert);
			}
		}
		return pathss;
	}

	public bool AngledGearNotAlreadyAdded(BlockPos position)
	{
		return ((BELargeGear3m)Blockentity).AngledGearNotAlreadyAdded(position);
	}

	public override float GetResistance()
	{
		return 0.004f;
	}

	internal bool OnInteract(IPlayer byPlayer)
	{
		return true;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		base.GetBlockInfo(forPlayer, sb);
		_ = Api.World.EntityDebugMode;
	}

	internal float GetSmallgearAngleRad()
	{
		return AngleRad * ratio;
	}
}
