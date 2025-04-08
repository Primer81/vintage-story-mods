using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockBrake : BlockMPBase
{
	public bool IsOrientedTo(BlockFacing facing)
	{
		return facing.Code == Variant["side"];
	}

	public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
		BlockFacing ownFacing = BlockFacing.FromCode(Variant["side"]);
		BlockFacing leftFacing = BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(ownFacing.HorizontalAngleIndex - 1, 4)];
		BlockFacing rightFacing = BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(ownFacing.HorizontalAngleIndex + 1, 4)];
		if (face != leftFacing)
		{
			return face == rightFacing;
		}
		return true;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		BlockFacing[] horVer = Block.SuggestedHVOrientation(byPlayer, blockSel);
		AssetLocation blockCode = CodeWithParts(horVer[0].Code);
		Block orientedBlock = world.BlockAccessor.GetBlock(blockCode);
		BlockFacing ownFacing = BlockFacing.FromCode(orientedBlock.Variant["side"]);
		BlockFacing leftFacing = BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(ownFacing.HorizontalAngleIndex - 1, 4)];
		BlockFacing rightFacing = BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(ownFacing.HorizontalAngleIndex + 1, 4)];
		if (world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(leftFacing)) is IMechanicalPowerBlock leftBlock)
		{
			return DoPlaceMechBlock(world, byPlayer, itemstack, blockSel, orientedBlock, leftBlock, leftFacing);
		}
		if (world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(rightFacing)) is IMechanicalPowerBlock rightBlock)
		{
			return DoPlaceMechBlock(world, byPlayer, itemstack, blockSel, orientedBlock, rightBlock, rightFacing);
		}
		BlockFacing frontFacing = ownFacing;
		BlockFacing backFacing = ownFacing.Opposite;
		Block rotBlock = world.GetBlock(orientedBlock.CodeWithVariant("side", leftFacing.Code));
		if (world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(frontFacing)) is IMechanicalPowerBlock frontBlock)
		{
			return DoPlaceMechBlock(world, byPlayer, itemstack, blockSel, rotBlock, frontBlock, frontFacing);
		}
		if (world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(backFacing)) is IMechanicalPowerBlock backBlock)
		{
			return DoPlaceMechBlock(world, byPlayer, itemstack, blockSel, rotBlock, backBlock, backFacing);
		}
		if (base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode))
		{
			WasPlaced(world, blockSel.Position, null);
			return true;
		}
		return false;
	}

	private bool DoPlaceMechBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, Block block, IMechanicalPowerBlock connectingBlock, BlockFacing connectingFace)
	{
		if (block.DoPlaceBlock(world, byPlayer, blockSel, itemstack))
		{
			connectingBlock.DidConnectAt(world, blockSel.Position.AddCopy(connectingFace), connectingFace.Opposite);
			WasPlaced(world, blockSel.Position, connectingFace);
			return true;
		}
		return false;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		BEBehaviorMPAxle bempaxle = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPAxle>();
		if (bempaxle != null && !BEBehaviorMPAxle.IsAttachedToBlock(world.BlockAccessor, bempaxle.Block, pos))
		{
			BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
			foreach (BlockFacing face in hORIZONTALS)
			{
				BlockPos npos = pos.AddCopy(face);
				if (world.BlockAccessor.GetBlock(npos) is BlockAngledGears blockagears && blockagears.Facings.Contains(face.Opposite) && blockagears.Facings.Length == 1)
				{
					world.BlockAccessor.BreakBlock(npos, null);
				}
			}
		}
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		return (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEBrake)?.OnInteract(byPlayer) ?? false;
	}
}
