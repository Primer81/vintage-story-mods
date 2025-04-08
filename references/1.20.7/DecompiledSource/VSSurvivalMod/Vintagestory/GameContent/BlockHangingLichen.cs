using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockHangingLichen : Block
{
	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (VertexFlags.WindMode != 0)
		{
			int sideDisableWindWave = 0;
			BlockFacing facing = BlockFacing.ALLFACES[4];
			Block nblock = api.World.BlockAccessor.GetBlock(pos.AddCopy(facing));
			if (nblock.BlockMaterial != EnumBlockMaterial.Leaves && nblock.SideSolid[BlockFacing.ALLFACES[4].Opposite.Index])
			{
				sideDisableWindWave |= 0x10;
			}
			int groundOffset = 0;
			bool enableWind = ((lightRgbsByCorner[24] >> 24) & 0xFF) >= 159;
			if (enableWind)
			{
				groundOffset = 1;
			}
			sourceMesh.ToggleWindModeSetWindData(sideDisableWindWave, enableWind, groundOffset);
		}
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (!blockAccessor.GetBlock(pos).IsReplacableBy(this))
		{
			return false;
		}
		if (onBlockFace.IsHorizontal && TryAttachTo(blockAccessor, pos, onBlockFace))
		{
			return true;
		}
		if (blockAccessor.GetBlock(pos.UpCopy()) is BlockHangingLichen)
		{
			blockAccessor.SetBlock(BlockId, pos);
			return true;
		}
		for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
		{
			if (TryAttachTo(blockAccessor, pos, BlockFacing.HORIZONTALS[i]))
			{
				return true;
			}
		}
		return false;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		if (blockSel.Face.IsHorizontal && TryAttachTo(world.BlockAccessor, blockSel.Position, blockSel.Face))
		{
			return true;
		}
		BlockPos upPos = blockSel.Position.UpCopy();
		Block upBlock = world.BlockAccessor.GetBlock(upPos);
		if (upBlock is BlockHangingLichen || upBlock.CanAttachBlockAt(world.BlockAccessor, this, upPos, BlockFacing.DOWN) || upBlock is BlockLeaves)
		{
			world.BlockAccessor.SetBlock(BlockId, blockSel.Position);
			return true;
		}
		failureCode = "requirelichenattachable";
		return false;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[0];
	}

	private bool TryAttachTo(IBlockAccessor blockAccessor, BlockPos blockpos, BlockFacing onBlockFace)
	{
		if (!onBlockFace.IsVertical)
		{
			return false;
		}
		BlockPos attachingBlockPos = blockpos.AddCopy(onBlockFace.Opposite);
		if (blockAccessor.GetBlock(attachingBlockPos).CanAttachBlockAt(blockAccessor, this, attachingBlockPos, onBlockFace))
		{
			int blockId = blockAccessor.GetBlock(CodeWithParts(onBlockFace.Code)).BlockId;
			blockAccessor.SetBlock(blockId, blockpos);
			return true;
		}
		return false;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		if (!CanVineStay(world, pos))
		{
			world.BlockAccessor.SetBlock(0, pos);
			world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
		}
	}

	private bool CanVineStay(IWorldAccessor world, BlockPos pos)
	{
		Block block = world.BlockAccessor.GetBlock(pos.UpCopy());
		if (!(block is BlockLeaves) && !(block is BlockHangingLichen))
		{
			return block.CanAttachBlockAt(world.BlockAccessor, this, pos.UpCopy(), BlockFacing.DOWN);
		}
		return true;
	}
}
