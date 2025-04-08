using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent.Mechanics;

public class BEBehaviorMPAngledGears : BEBehaviorMPBase
{
	public BlockFacing axis1;

	public BlockFacing axis2;

	private BEBehaviorMPLargeGear3m largeGear;

	public BlockFacing turnDir1;

	public BlockFacing turnDir2;

	public BlockFacing orientation;

	public bool newlyPlaced;

	public override float AngleRad
	{
		get
		{
			float angle = base.AngleRad;
			bool flip = propagationDir == BlockFacing.DOWN || propagationDir == BlockFacing.WEST;
			if (propagationDir == orientation && propagationDir != BlockFacing.NORTH && propagationDir != BlockFacing.SOUTH && propagationDir != BlockFacing.UP)
			{
				flip = !flip;
			}
			if (!flip)
			{
				return angle;
			}
			return (float)Math.PI * 2f - angle;
		}
	}

	public BEBehaviorMPAngledGears(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		if (api.Side == EnumAppSide.Client)
		{
			Blockentity.RegisterGameTickListener(onEverySecond, 1000);
		}
		if (largeGear != null && largeGear.AngledGearNotAlreadyAdded(Position))
		{
			tryConnect(orientation.Opposite);
		}
	}

	private void onEverySecond(float dt)
	{
		float speed = ((network == null) ? 0f : network.Speed);
		if (Api.World.Rand.NextDouble() < (double)(speed / 3f))
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/block/woodcreak"), (double)Position.X + 0.5, (double)Position.Y + 0.5, (double)Position.Z + 0.5, null, 0.75f + speed);
		}
	}

	public override void SetOrientations()
	{
		string orientations = (base.Block as BlockAngledGears).Orientation;
		if (turnDir1 != null)
		{
			if (propagationDir == turnDir1)
			{
				propagationDir = turnDir2.Opposite;
			}
			else if (propagationDir == turnDir2)
			{
				propagationDir = turnDir1.Opposite;
			}
			else if (propagationDir == turnDir2.Opposite)
			{
				propagationDir = turnDir1;
			}
			else if (propagationDir == turnDir1.Opposite)
			{
				propagationDir = turnDir2;
			}
			turnDir1 = null;
			turnDir2 = null;
		}
		orientation = null;
		if (orientations != null)
		{
			int length = orientations.Length;
			if (length == 1)
			{
				switch (orientations[0])
				{
				case 'n':
					break;
				case 's':
					goto IL_02ab;
				case 'e':
					goto IL_02cb;
				case 'w':
					goto IL_02eb;
				case 'u':
					goto IL_0316;
				case 'd':
					goto IL_0336;
				default:
					goto IL_0677;
				}
				goto IL_0280;
			}
			if (length == 2)
			{
				switch (orientations[1])
				{
				case 'n':
					break;
				case 's':
					goto IL_0187;
				case 'e':
					goto IL_01bc;
				case 'w':
					goto IL_01d1;
				case 'd':
					goto IL_01f6;
				case 'u':
					goto IL_023b;
				default:
					goto IL_0677;
				}
				if (orientations == "nn")
				{
					goto IL_0280;
				}
				if (orientations == "en")
				{
					AxisSign = new int[6] { 0, 0, 1, 1, 0, 0 };
					axis1 = BlockFacing.SOUTH;
					axis2 = BlockFacing.NORTH;
					turnDir1 = BlockFacing.EAST;
					turnDir2 = BlockFacing.NORTH;
					goto IL_0699;
				}
			}
		}
		goto IL_0677;
		IL_0361:
		AxisSign = new int[6] { 1, 0, 0, 0, 0, -1 };
		axis1 = BlockFacing.EAST;
		axis2 = null;
		turnDir1 = BlockFacing.EAST;
		turnDir2 = BlockFacing.SOUTH;
		goto IL_0699;
		IL_02cb:
		AxisSign = new int[3] { 1, 0, 0 };
		orientation = BlockFacing.EAST;
		goto IL_0699;
		IL_02eb:
		AxisSign = new int[3] { -1, 0, 0 };
		orientation = BlockFacing.WEST;
		axis1 = BlockFacing.WEST;
		goto IL_0699;
		IL_0699:
		if (Api != null)
		{
			CheckLargeGearJoin();
		}
		return;
		IL_01f6:
		switch (orientations)
		{
		case "sd":
			break;
		case "ed":
			goto IL_0461;
		case "wd":
			goto IL_04a6;
		case "nd":
			goto IL_04eb;
		default:
			goto IL_0677;
		}
		AxisSign = new int[6] { 0, 0, -1, 0, -1, 0 };
		axis1 = null;
		axis2 = null;
		turnDir1 = BlockFacing.SOUTH;
		turnDir2 = BlockFacing.DOWN;
		goto IL_0699;
		IL_0677:
		AxisSign = new int[6] { 0, 0, 1, 1, 0, 0 };
		axis1 = null;
		axis2 = null;
		goto IL_0699;
		IL_01d1:
		if (orientations == "ww")
		{
			goto IL_02eb;
		}
		if (!(orientations == "nw"))
		{
			goto IL_0677;
		}
		AxisSign = new int[6] { 1, 0, 0, 0, 0, -1 };
		axis1 = null;
		axis2 = BlockFacing.EAST;
		turnDir1 = BlockFacing.NORTH;
		turnDir2 = BlockFacing.WEST;
		goto IL_0699;
		IL_01bc:
		if (orientations == "ee")
		{
			goto IL_02cb;
		}
		goto IL_0677;
		IL_03a2:
		AxisSign = new int[6] { 0, 0, -1, -1, 0, 0 };
		axis1 = BlockFacing.WEST;
		axis2 = null;
		turnDir1 = BlockFacing.WEST;
		turnDir2 = BlockFacing.SOUTH;
		goto IL_0699;
		IL_02ab:
		AxisSign = new int[3] { 0, 0, -1 };
		orientation = BlockFacing.SOUTH;
		goto IL_0699;
		IL_05f3:
		AxisSign = new int[6] { 0, -1, 0, -1, 0, 0 };
		axis1 = BlockFacing.WEST;
		axis2 = BlockFacing.UP;
		turnDir1 = BlockFacing.WEST;
		turnDir2 = BlockFacing.UP;
		goto IL_0699;
		IL_04a6:
		AxisSign = new int[6] { -1, 0, 0, 0, 1, 0 };
		axis1 = BlockFacing.DOWN;
		axis2 = BlockFacing.WEST;
		turnDir1 = BlockFacing.WEST;
		turnDir2 = BlockFacing.DOWN;
		goto IL_0699;
		IL_0316:
		AxisSign = new int[3] { 0, -1, 0 };
		orientation = BlockFacing.UP;
		goto IL_0699;
		IL_0336:
		AxisSign = new int[3] { 0, 1, 0 };
		orientation = BlockFacing.DOWN;
		axis1 = BlockFacing.DOWN;
		goto IL_0699;
		IL_0461:
		AxisSign = new int[6] { 0, 1, 0, 1, 0, 0 };
		axis1 = BlockFacing.EAST;
		axis2 = BlockFacing.DOWN;
		turnDir1 = BlockFacing.EAST;
		turnDir2 = BlockFacing.DOWN;
		goto IL_0699;
		IL_04eb:
		AxisSign = new int[6] { 0, 0, -1, 0, 1, 0 };
		axis1 = BlockFacing.DOWN;
		axis2 = null;
		turnDir1 = BlockFacing.NORTH;
		turnDir2 = BlockFacing.DOWN;
		goto IL_0699;
		IL_023b:
		switch (orientations)
		{
		case "nu":
			break;
		case "eu":
			goto IL_056d;
		case "su":
			goto IL_05b2;
		case "wu":
			goto IL_05f3;
		default:
			goto IL_0677;
		}
		AxisSign = new int[6] { 0, -1, 0, 0, 0, -1 };
		axis1 = BlockFacing.UP;
		axis2 = null;
		turnDir1 = BlockFacing.NORTH;
		turnDir2 = BlockFacing.UP;
		goto IL_0699;
		IL_0280:
		orientation = BlockFacing.NORTH;
		AxisSign = new int[3] { 0, 0, -1 };
		axis1 = BlockFacing.NORTH;
		goto IL_0699;
		IL_05b2:
		AxisSign = new int[6] { 0, 1, 0, 0, 0, -1 };
		axis1 = BlockFacing.DOWN;
		axis2 = null;
		turnDir1 = BlockFacing.SOUTH;
		turnDir2 = BlockFacing.UP;
		goto IL_0699;
		IL_056d:
		AxisSign = new int[6] { 0, -1, 0, 1, 0, 0 };
		axis1 = BlockFacing.UP;
		axis2 = BlockFacing.EAST;
		turnDir1 = BlockFacing.EAST;
		turnDir2 = BlockFacing.UP;
		goto IL_0699;
		IL_0187:
		switch (orientations)
		{
		case "ss":
			break;
		case "es":
			goto IL_0361;
		case "ws":
			goto IL_03a2;
		default:
			goto IL_0677;
		}
		goto IL_02ab;
	}

	protected void CheckLargeGearJoin()
	{
		if (Api == null)
		{
			return;
		}
		string orientations = (base.Block as BlockAngledGears).Orientation;
		if (orientations.Length == 2 && orientations[0] == orientations[1])
		{
			BlockPos largeGearPos = Position.AddCopy(orientation.Opposite);
			BlockEntity be = Api?.World.BlockAccessor.GetBlockEntity(largeGearPos);
			if (be != null)
			{
				SetLargeGear(be);
			}
		}
	}

	public void SetLargeGear(BlockEntity be)
	{
		if (largeGear == null)
		{
			largeGear = be.GetBehavior<BEBehaviorMPLargeGear3m>();
		}
	}

	public void AddToLargeGearNetwork(BEBehaviorMPLargeGear3m largeGear, BlockFacing outFacing)
	{
		JoinNetwork(largeGear.Network);
		SetPropagationDirection(new MechPowerPath(outFacing, largeGear.GearedRatio * largeGear.ratio, null, largeGear.GetPropagationDirection() == BlockFacing.DOWN));
	}

	public override bool isInvertedNetworkFor(BlockPos pos)
	{
		if (orientation != null)
		{
			return orientation != propagationDir;
		}
		return false;
	}

	public float LargeGearAngleRad(float unchanged)
	{
		if (largeGear == null)
		{
			BlockPos largeGearPos = Position.AddCopy(orientation.Opposite);
			BlockEntity be = Api.World?.BlockAccessor.GetBlockEntity(largeGearPos);
			if (be != null)
			{
				SetLargeGear(be);
			}
			if (largeGear == null)
			{
				return unchanged;
			}
		}
		return (float)((orientation != BlockFacing.SOUTH) ? 1 : (-1)) * largeGear.GetSmallgearAngleRad() % ((float)Math.PI * 2f);
	}

	internal void CreateNetworkFromHere()
	{
		if (Api.Side == EnumAppSide.Server)
		{
			CreateJoinAndDiscoverNetwork(orientation);
			CreateJoinAndDiscoverNetwork(orientation.Opposite);
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		return base.OnTesselation(mesher, tesselator);
	}

	public override void SetPropagationDirection(MechPowerPath path)
	{
		BlockFacing turnDir = path.NetworkDir();
		if (turnDir1 != null)
		{
			if (turnDir == turnDir1)
			{
				turnDir = turnDir2.Opposite;
			}
			else if (turnDir == turnDir2)
			{
				turnDir = turnDir1.Opposite;
			}
			else if (turnDir == turnDir2.Opposite)
			{
				turnDir = turnDir1;
			}
			else if (turnDir == turnDir1.Opposite)
			{
				turnDir = turnDir2;
			}
		}
		path = new MechPowerPath(turnDir, path.gearingRatio);
		base.SetPropagationDirection(path);
	}

	public override BlockFacing GetPropagationDirectionInput()
	{
		if (turnDir1 != null)
		{
			if (propagationDir == turnDir1)
			{
				return turnDir2.Opposite;
			}
			if (propagationDir == turnDir2)
			{
				return turnDir1.Opposite;
			}
			if (propagationDir == turnDir1.Opposite)
			{
				return turnDir2;
			}
			if (propagationDir == turnDir2.Opposite)
			{
				return turnDir1;
			}
		}
		return propagationDir;
	}

	public override bool IsPropagationDirection(BlockPos fromPos, BlockFacing test)
	{
		if (turnDir1 != null)
		{
			if (propagationDir == turnDir1)
			{
				if (propagationDir != test)
				{
					return test == turnDir2.Opposite;
				}
				return true;
			}
			if (propagationDir == turnDir2)
			{
				if (propagationDir != test)
				{
					return test == turnDir1.Opposite;
				}
				return true;
			}
			if (propagationDir == turnDir1.Opposite)
			{
				if (propagationDir != test)
				{
					return test == turnDir2;
				}
				return true;
			}
			if (propagationDir == turnDir2.Opposite)
			{
				if (propagationDir != test)
				{
					return test == turnDir1;
				}
				return true;
			}
		}
		return propagationDir == test;
	}

	public override void WasPlaced(BlockFacing connectedOnFacing)
	{
		if ((Api.Side != EnumAppSide.Client && OutFacingForNetworkDiscovery != null) || connectedOnFacing == null)
		{
			return;
		}
		if (connectedOnFacing.Axis == EnumAxis.X)
		{
			if (!tryConnect(BlockFacing.NORTH) && !tryConnect(BlockFacing.SOUTH))
			{
				Api.Logger.Notification("AG was placed fail connect 2nd: " + connectedOnFacing?.ToString() + " at " + Position);
			}
		}
		else if (connectedOnFacing.Axis == EnumAxis.Z && !tryConnect(BlockFacing.WEST) && !tryConnect(BlockFacing.EAST))
		{
			Api.Logger.Notification("AG was placed fail connect 2nd: " + connectedOnFacing?.ToString() + " at " + Position);
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
	}

	public override float GetResistance()
	{
		return 0.0005f;
	}

	protected override MechPowerPath[] GetMechPowerExits(MechPowerPath fromExitTurnDir)
	{
		if (orientation == null)
		{
			SetOrientations();
		}
		string orientations = (base.Block as BlockAngledGears).Orientation;
		if (orientations.Length < 2 || orientations[0] != orientations[1])
		{
			bool invert = fromExitTurnDir.invert;
			BlockFacing[] connectors = (base.Block as BlockAngledGears).Facings;
			BlockFacing inputSide = fromExitTurnDir.OutFacing;
			if (!connectors.Contains(inputSide))
			{
				inputSide = inputSide.Opposite;
				invert = !invert;
			}
			if (!newlyPlaced)
			{
				connectors = connectors.Remove(inputSide);
			}
			MechPowerPath[] paths = new MechPowerPath[connectors.Length];
			for (int i = 0; i < paths.Length; i++)
			{
				BlockFacing pathFacing = connectors[i];
				paths[i] = new MechPowerPath(pathFacing, base.GearedRatio, null, (pathFacing == inputSide) ? invert : (!invert));
			}
			return paths;
		}
		return new MechPowerPath[2]
		{
			new MechPowerPath(orientation.Opposite, base.GearedRatio, null, (orientation == fromExitTurnDir.OutFacing) ? (!fromExitTurnDir.invert) : fromExitTurnDir.invert),
			new MechPowerPath(orientation, base.GearedRatio, null, (orientation == fromExitTurnDir.OutFacing) ? fromExitTurnDir.invert : (!fromExitTurnDir.invert))
		};
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		base.GetBlockInfo(forPlayer, sb);
		if (Api.World.EntityDebugMode)
		{
			string orientations = base.Block.Variant["orientation"];
			bool rev = propagationDir == axis1 || propagationDir == axis2;
			sb.AppendLine(string.Format(Lang.Get("Orientation: {0} {1} {2}", orientations, orientation, rev ? "-" : "")));
		}
	}

	public void ClearLargeGear()
	{
		largeGear = null;
	}
}
