using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockMPMultiblockGear : Block
{
	public override bool IsReplacableBy(Block block)
	{
		if (block is BlockAngledGears)
		{
			return true;
		}
		return base.IsReplacableBy(block);
	}

	public bool IsReplacableByGear(IWorldAccessor world, BlockPos pos)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BEMPMultiblock be) || be.Principal == null)
		{
			return true;
		}
		if (world.BlockAccessor.GetBlockEntity(be.Principal) is IGearAcceptor beg)
		{
			return beg.CanAcceptGear(pos);
		}
		return true;
	}

	public BlockEntity GearPlaced(IWorldAccessor world, BlockPos pos)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BEMPMultiblock be) || be.Principal == null)
		{
			return null;
		}
		IGearAcceptor obj = world.BlockAccessor.GetBlockEntity(be.Principal) as IGearAcceptor;
		if (obj == null)
		{
			world.Logger.Notification("no gear acceptor");
		}
		obj?.AddGear(pos);
		return obj as BlockEntity;
	}

	public static void OnGearDestroyed(IWorldAccessor world, BlockPos pos, char orient)
	{
		BlockPos posCenter = orient switch
		{
			's' => pos.NorthCopy(), 
			'w' => pos.EastCopy(), 
			'e' => pos.WestCopy(), 
			_ => pos.SouthCopy(), 
		};
		if (world.BlockAccessor.GetBlockEntity(posCenter) is IGearAcceptor beg)
		{
			beg.RemoveGearAt(pos);
			Block toPlaceBlock = world.GetBlock(new AssetLocation("mpmultiblockwood"));
			world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, pos);
			if (world.BlockAccessor.GetBlockEntity(pos) is BEMPMultiblock be)
			{
				be.Principal = posCenter;
			}
		}
		else
		{
			world.Logger.Notification("no LG found at " + posCenter?.ToString() + " from " + pos);
		}
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
		BlockPos centerPos = be.Principal;
		Block centerBlock = world.BlockAccessor.GetBlock(centerPos);
		if (centerBlock.Id != 0)
		{
			centerBlock.OnBlockBroken(world, centerPos, byPlayer, dropQuantityMultiplier);
		}
		else
		{
			base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		}
		if (api.Side == EnumAppSide.Client)
		{
			BlockFacing[] vERTICALS = BlockFacing.VERTICALS;
			foreach (BlockFacing facing in vERTICALS)
			{
				BlockPos npos = centerPos.AddCopy(facing);
				world.BlockAccessor.GetBlock(npos).OnNeighbourBlockChange(world, npos, centerPos);
			}
		}
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

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		IBlockAccessor blockAccess = world.BlockAccessor;
		if (!(blockAccess.GetBlockEntity(pos) is BEMPMultiblock be) || be.Principal == null)
		{
			return new ItemStack(world.GetBlock(new AssetLocation("largegear3")));
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
