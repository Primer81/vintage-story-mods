using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockTransmission : BlockMPBase
{
	public bool IsOrientedTo(BlockFacing facing)
	{
		string dirs = LastCodePart();
		if (dirs[0] != facing.Code[0])
		{
			if (dirs.Length > 1)
			{
				return dirs[1] == facing.Code[0];
			}
			return false;
		}
		return true;
	}

	public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
		return IsOrientedTo(face);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing face in hORIZONTALS)
		{
			BlockPos pos = blockSel.Position.AddCopy(face);
			if (!(world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock block2))
			{
				continue;
			}
			BlockFacing faceOpposite = face.Opposite;
			if (!block2.HasMechPowerConnectorAt(world, pos, faceOpposite))
			{
				continue;
			}
			AssetLocation loc = ((face != BlockFacing.EAST && face != BlockFacing.WEST) ? new AssetLocation(FirstCodePart() + "-ns") : new AssetLocation(FirstCodePart() + "-we"));
			if (world.GetBlock(loc).DoPlaceBlock(world, byPlayer, blockSel, itemstack))
			{
				block2.DidConnectAt(world, pos, faceOpposite);
				WasPlaced(world, blockSel.Position, face);
				pos = blockSel.Position.AddCopy(faceOpposite);
				if (world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock block && block.HasMechPowerConnectorAt(world, pos, face))
				{
					block.DidConnectAt(world, pos, face);
					WasPlaced(world, blockSel.Position, faceOpposite);
				}
				return true;
			}
		}
		if (base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode))
		{
			WasPlaced(world, blockSel.Position, null);
			return true;
		}
		return false;
	}

	public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
	}

	public override MechanicalNetwork GetNetwork(IWorldAccessor world, BlockPos pos)
	{
		BEBehaviorMPTransmission be = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPTransmission>();
		if (be == null || !be.engaged)
		{
			return null;
		}
		return be.Network;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		(world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPTransmission>())?.CheckEngaged(world.BlockAccessor, updateNetwork: true);
	}
}
