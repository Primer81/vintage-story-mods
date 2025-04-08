using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BEBehaviorMPTransmission : BEBehaviorMPBase
{
	public bool engaged;

	protected float[] rotPrev = new float[2];

	private BlockFacing[] orients = new BlockFacing[2];

	private string orientations;

	public override CompositeShape Shape
	{
		get
		{
			string side = base.Block.Variant["orientation"];
			CompositeShape compositeShape = new CompositeShape();
			compositeShape.Base = new AssetLocation("shapes/block/wood/mechanics/transmission-leftgear.json");
			compositeShape.Overlays = new CompositeShape[1]
			{
				new CompositeShape
				{
					Base = new AssetLocation("shapes/block/wood/mechanics/transmission-rightgear.json")
				}
			};
			CompositeShape shape = compositeShape;
			if (side == "ns")
			{
				shape.rotateY = 90f;
				shape.Overlays[0].rotateY = 90f;
			}
			return shape;
		}
		set
		{
		}
	}

	public BEBehaviorMPTransmission(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		orientations = base.Block.Variant["orientation"];
		string text = orientations;
		if (!(text == "ns"))
		{
			if (text == "we")
			{
				AxisSign = new int[3] { 1, 0, 0 };
				orients[0] = BlockFacing.EAST;
				orients[1] = BlockFacing.WEST;
			}
		}
		else
		{
			AxisSign = new int[3] { 0, 0, -1 };
			orients[0] = BlockFacing.NORTH;
			orients[1] = BlockFacing.SOUTH;
		}
		if (engaged)
		{
			ChangeState(newEngaged: true);
		}
	}

	public void CheckEngaged(IBlockAccessor access, bool updateNetwork)
	{
		BlockFacing side = ((orients[0] == BlockFacing.NORTH) ? BlockFacing.EAST : BlockFacing.NORTH);
		bool clutchEngaged = false;
		BEClutch bec = access.GetBlockEntity(Position.AddCopy(side)) as BEClutch;
		if (bec?.Facing == side.Opposite)
		{
			clutchEngaged = bec.Engaged;
		}
		if (!clutchEngaged)
		{
			bec = access.GetBlockEntity(Position.AddCopy(side.Opposite)) as BEClutch;
			if (bec?.Facing == side)
			{
				clutchEngaged = bec.Engaged;
			}
		}
		if (clutchEngaged != engaged)
		{
			engaged = clutchEngaged;
			if (updateNetwork)
			{
				ChangeState(clutchEngaged);
			}
		}
	}

	protected override MechPowerPath[] GetMechPowerExits(MechPowerPath fromExitTurnDir)
	{
		if (!engaged)
		{
			return new MechPowerPath[0];
		}
		return base.GetMechPowerExits(fromExitTurnDir);
	}

	public override float GetResistance()
	{
		return 0.0005f;
	}

	private void ChangeState(bool newEngaged)
	{
		if (newEngaged)
		{
			CreateJoinAndDiscoverNetwork(orients[0]);
			CreateJoinAndDiscoverNetwork(orients[1]);
			tryConnect(orients[0]);
			Blockentity.MarkDirty(redrawOnClient: true);
		}
		else if (network != null)
		{
			manager.OnNodeRemoved(this);
		}
	}

	internal float RotationNeighbour(int side, bool allowIndirect)
	{
		BlockPos pos = Position.AddCopy(orients[side]);
		IMechanicalPowerBlock block = Api.World.BlockAccessor.GetBlock(pos) as IMechanicalPowerBlock;
		if (block == null || !block.HasMechPowerConnectorAt(Api.World, pos, orients[side].Opposite))
		{
			block = null;
		}
		IMechanicalPowerDevice node = ((block == null) ? null : Api.World.BlockAccessor.GetBlockEntity(pos))?.GetBehavior<BEBehaviorMPBase>();
		if (node is BEBehaviorMPTransmission { engaged: false })
		{
			node = null;
		}
		float rot;
		if (node == null || node.Network == null)
		{
			if (engaged && allowIndirect)
			{
				rot = RotationNeighbour(1 - side, allowIndirect: false);
				rotPrev[side] = rot;
			}
			else
			{
				rot = rotPrev[side];
			}
		}
		else
		{
			rot = node.Network.AngleRad * node.GearedRatio;
			bool invert = node.GetPropagationDirection() != orients[side];
			if (side == 1)
			{
				invert = !invert;
			}
			if (invert)
			{
				rot = (float)Math.PI * 2f - rot;
			}
			rotPrev[side] = rot;
		}
		return rot;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		engaged = tree.GetBool("engaged");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("engaged", engaged);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		base.OnTesselation(mesher, tesselator);
		return false;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		base.GetBlockInfo(forPlayer, sb);
		if (Api.World.EntityDebugMode)
		{
			sb.AppendLine(string.Format(Lang.Get(engaged ? "Engaged" : "Disengaged")));
		}
	}
}
