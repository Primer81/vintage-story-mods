using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockLargeGear3m : BlockMPBase
{
	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
	}

	public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
	}

	public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
		if (face == BlockFacing.UP || face == BlockFacing.DOWN)
		{
			return true;
		}
		if (world.BlockAccessor.GetBlockEntity(pos) is BELargeGear3m beg)
		{
			return beg.HasGearAt(world.Api, pos.AddCopy(face));
		}
		return false;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		List<BlockPos> smallGears = new List<BlockPos>();
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode, smallGears))
		{
			return false;
		}
		bool ok = base.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
		if (ok)
		{
			BlockEntity beOwn = world.BlockAccessor.GetBlockEntity(blockSel.Position);
			List<BlockFacing> connections = new List<BlockFacing>();
			foreach (BlockPos smallGear in smallGears)
			{
				int dx = smallGear.X - blockSel.Position.X;
				int dz = smallGear.Z - blockSel.Position.Z;
				char orient = 'n';
				switch (dx)
				{
				case 1:
					orient = 'e';
					break;
				case -1:
					orient = 'w';
					break;
				default:
					if (dz == 1)
					{
						orient = 's';
					}
					break;
				}
				ReadOnlySpan<char> readOnlySpan = "angledgears-";
				char reference = orient;
				ReadOnlySpan<char> readOnlySpan2 = new ReadOnlySpan<char>(in reference);
				char reference2 = orient;
				BlockMPBase obj = world.GetBlock(new AssetLocation(string.Concat(readOnlySpan, readOnlySpan2, new ReadOnlySpan<char>(in reference2)))) as BlockMPBase;
				BlockFacing bf = BlockFacing.FromFirstLetter(orient);
				obj.ExchangeBlockAt(world, smallGear);
				obj.DidConnectAt(world, smallGear, bf.Opposite);
				connections.Add(bf);
			}
			PlaceFakeBlocks(world, blockSel.Position, smallGears);
			BEBehaviorMPBase beMechBase = beOwn?.GetBehavior<BEBehaviorMPBase>();
			BlockPos pos = blockSel.Position.DownCopy();
			if (world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock block2 && block2.HasMechPowerConnectorAt(world, pos, BlockFacing.UP))
			{
				block2.DidConnectAt(world, pos, BlockFacing.UP);
				connections.Add(BlockFacing.DOWN);
			}
			else
			{
				pos = blockSel.Position.UpCopy();
				if (world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock block && block.HasMechPowerConnectorAt(world, pos, BlockFacing.DOWN))
				{
					block.DidConnectAt(world, pos, BlockFacing.DOWN);
					connections.Add(BlockFacing.UP);
				}
			}
			foreach (BlockFacing face in connections)
			{
				beMechBase?.WasPlaced(face);
			}
		}
		return ok;
	}

	private void PlaceFakeBlocks(IWorldAccessor world, BlockPos pos, List<BlockPos> skips)
	{
		Block toPlaceBlock = world.GetBlock(new AssetLocation("mpmultiblockwood"));
		BlockPos tmpPos = new BlockPos();
		for (int dx = -1; dx <= 1; dx++)
		{
			for (int dz = -1; dz <= 1; dz++)
			{
				if (dx == 0 && dz == 0)
				{
					continue;
				}
				bool toSkip = false;
				foreach (BlockPos skipPos in skips)
				{
					if (pos.X + dx == skipPos.X && pos.Z + dz == skipPos.Z)
					{
						toSkip = true;
						break;
					}
				}
				if (!toSkip)
				{
					tmpPos.Set(pos.X + dx, pos.Y, pos.Z + dz);
					world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, tmpPos);
					if (world.BlockAccessor.GetBlockEntity(tmpPos) is BEMPMultiblock be)
					{
						be.Principal = pos;
					}
				}
			}
		}
	}

	private bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode, List<BlockPos> smallGears)
	{
		if (!base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		BlockPos pos = blockSel.Position;
		BlockPos tmpPos = new BlockPos();
		BlockSelection bs = blockSel.Clone();
		for (int dx = -1; dx <= 1; dx++)
		{
			for (int dz = -1; dz <= 1; dz++)
			{
				if (dx == 0 && dz == 0)
				{
					continue;
				}
				tmpPos.Set(pos.X + dx, pos.Y, pos.Z + dz);
				if ((dx == 0 || dz == 0) && world.BlockAccessor.GetBlock(tmpPos) is BlockAngledGears)
				{
					smallGears.Add(tmpPos.Copy());
					continue;
				}
				bs.Position = tmpPos;
				if (!base.CanPlaceBlock(world, byPlayer, bs, ref failureCode))
				{
					return false;
				}
			}
		}
		return true;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (blockSel.SelectionBoxIndex == 0)
		{
			blockSel.Face = BlockFacing.UP;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
	{
		base.OnBlockRemoved(world, pos);
		BlockPos tmpPos = new BlockPos();
		for (int dx = -1; dx <= 1; dx++)
		{
			for (int dz = -1; dz <= 1; dz++)
			{
				if (dx != 0 || dz != 0)
				{
					tmpPos.Set(pos.X + dx, pos.Y, pos.Z + dz);
					if (world.BlockAccessor.GetBlockEntity(tmpPos) is BEMPMultiblock be && pos.Equals(be.Principal))
					{
						be.Principal = null;
						world.BlockAccessor.SetBlock(0, tmpPos);
					}
					else if (world.BlockAccessor.GetBlock(tmpPos) is BlockAngledGears smallgear)
					{
						smallgear.ToPegGear(world, tmpPos);
					}
				}
			}
		}
	}
}
