using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockMPMultiblockPulverizer : Block
{
	public override bool IsReplacableBy(Block block)
	{
		return base.IsReplacableBy(block);
	}

	public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		IWorldAccessor world = player?.Entity?.World;
		if (world == null)
		{
			world = api.World;
		}
		if (!(world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEMPMultiblock be) || be.Principal == null)
		{
			return 1f;
		}
		Block block = world.BlockAccessor.GetBlock(be.Principal);
		_ = api.Side;
		_ = 2;
		BlockSelection bs = blockSel.Clone();
		bs.Position = be.Principal;
		return block.OnGettingBroken(player, bs, itemslot, remainingResistance, dt, counter);
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(world, blockPos, byItemStack);
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BEMPMultiblock be) || be.Principal == null)
		{
			base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
			return;
		}
		BlockPos principalPos = be.Principal;
		world.BlockAccessor.GetBlock(principalPos).OnBlockBroken(world, principalPos, byPlayer, dropQuantityMultiplier);
		if (api.Side == EnumAppSide.Client)
		{
			BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
			foreach (BlockFacing facing in hORIZONTALS)
			{
				BlockPos npos = principalPos.AddCopy(facing);
				world.BlockAccessor.GetBlock(npos).OnNeighbourBlockChange(world, npos, principalPos);
			}
		}
		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
	}

	public override Cuboidf GetParticleBreakBox(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing)
	{
		if (!(blockAccess.GetBlockEntity(pos) is BEMPMultiblock be) || be.Principal == null)
		{
			return base.GetParticleBreakBox(blockAccess, pos, facing);
		}
		return blockAccess.GetBlock(be.Principal).GetParticleBreakBox(blockAccess, be.Principal, facing);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		IBlockAccessor blockAccess = capi.World.BlockAccessor;
		if (!(blockAccess.GetBlockEntity(pos) is BEMPMultiblock be) || be.Principal == null)
		{
			return 0;
		}
		return blockAccess.GetBlock(be.Principal).GetRandomColor(capi, be.Principal, facing, rndIndex);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEMPMultiblock bem && world.BlockAccessor.GetBlockEntity(bem.Principal) is BEPulverizer bep)
		{
			return bep.OnInteract(byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		IBlockAccessor blockAccess = world.BlockAccessor;
		if (!(blockAccess.GetBlockEntity(pos) is BEMPMultiblock be) || be.Principal == null)
		{
			return new ItemStack(world.GetBlock(new AssetLocation("pulverizerframe")));
		}
		return blockAccess.GetBlock(be.Principal).OnPickBlock(world, be.Principal);
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(base.OnPickBlock(world, pos)?.GetName());
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			blockBehaviors[i].GetPlacedBlockName(sb, world, pos);
		}
		return sb.ToString().TrimEnd();
	}
}
