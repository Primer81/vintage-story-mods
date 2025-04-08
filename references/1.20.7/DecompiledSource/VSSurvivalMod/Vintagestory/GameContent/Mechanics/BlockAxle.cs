using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockAxle : BlockMPBase
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
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing face in aLLFACES)
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
			ReadOnlySpan<char> readOnlySpan = FirstCodePart();
			char reference = '-';
			ReadOnlySpan<char> readOnlySpan2 = new ReadOnlySpan<char>(in reference);
			char reference2 = faceOpposite.Code[0];
			ReadOnlySpan<char> readOnlySpan3 = new ReadOnlySpan<char>(in reference2);
			char reference3 = face.Code[0];
			AssetLocation loc = new AssetLocation(string.Concat(readOnlySpan, readOnlySpan2, readOnlySpan3, new ReadOnlySpan<char>(in reference3)));
			Block toPlaceBlock = world.GetBlock(loc);
			if (toPlaceBlock == null)
			{
				ReadOnlySpan<char> readOnlySpan4 = FirstCodePart();
				reference3 = '-';
				ReadOnlySpan<char> readOnlySpan5 = new ReadOnlySpan<char>(in reference3);
				reference2 = face.Code[0];
				ReadOnlySpan<char> readOnlySpan6 = new ReadOnlySpan<char>(in reference2);
				reference = faceOpposite.Code[0];
				loc = new AssetLocation(string.Concat(readOnlySpan4, readOnlySpan5, readOnlySpan6, new ReadOnlySpan<char>(in reference)));
				toPlaceBlock = world.GetBlock(loc);
			}
			if (toPlaceBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack))
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

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		BEBehaviorMPAxle bempaxle = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPAxle>();
		if (bempaxle != null && !BEBehaviorMPAxle.IsAttachedToBlock(world.BlockAccessor, bempaxle.Block, pos))
		{
			bool connected = false;
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			foreach (BlockFacing face in aLLFACES)
			{
				BlockPos npos = pos.AddCopy(face);
				IMechanicalPowerBlock block = world.BlockAccessor.GetBlock(npos) as IMechanicalPowerBlock;
				bool prevConnected = connected;
				if (block != null && block.HasMechPowerConnectorAt(world, pos, face.Opposite))
				{
					BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
					if (blockEntity != null && blockEntity.GetBehavior<BEBehaviorMPBase>()?.disconnected == false)
					{
						connected = true;
					}
				}
				if (block is BlockAngledGears blockagears && blockagears.Facings.Contains(face.Opposite) && blockagears.Facings.Length == 1)
				{
					world.BlockAccessor.BreakBlock(npos, null);
					connected = prevConnected;
				}
			}
			if (!connected)
			{
				world.BlockAccessor.BreakBlock(pos, null);
			}
		}
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
	}
}
