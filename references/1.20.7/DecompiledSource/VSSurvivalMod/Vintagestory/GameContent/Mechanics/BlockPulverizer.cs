using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockPulverizer : BlockMPBase
{
	private BlockFacing orientation;

	public bool InvertPoundersOnRender { get; set; }

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		orientation = BlockFacing.FromFirstLetter(Variant["side"][0]);
		InvertPoundersOnRender = orientation == BlockFacing.WEST || orientation == BlockFacing.SOUTH;
	}

	public bool IsOrientedTo(BlockFacing facing)
	{
		return facing == orientation;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEPulverizer bep)
		{
			return bep.OnInteract(byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
		if (orientation != face)
		{
			return orientation != face.Opposite;
		}
		return false;
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
			if (world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock block && block.HasMechPowerConnectorAt(world, pos, face.Opposite))
			{
				AssetLocation loc = new AssetLocation(FirstCodePart() + "-" + face.GetCCW().Code);
				if (world.GetBlock(loc).DoPlaceBlock(world, byPlayer, blockSel, itemstack))
				{
					block.DidConnectAt(world, pos, face.Opposite);
					WasPlaced(world, blockSel.Position, face);
					return true;
				}
			}
		}
		if (base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode))
		{
			WasPlaced(world, blockSel.Position, null);
			return true;
		}
		return false;
	}

	public override void WasPlaced(IWorldAccessor world, BlockPos ownPos, BlockFacing connectedOnFacing)
	{
		base.WasPlaced(world, ownPos, connectedOnFacing);
		PlaceFakeBlock(world, ownPos);
	}

	private void PlaceFakeBlock(IWorldAccessor world, BlockPos pos)
	{
		Block toPlaceBlock = world.GetBlock(new AssetLocation("mppulverizertop"));
		world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, pos.UpCopy());
		if (world.BlockAccessor.GetBlockEntity(pos.UpCopy()) is BEMPMultiblock be)
		{
			be.Principal = pos;
		}
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (api.World.BlockAccessor.GetBlock(pos.UpCopy()).Code.Path == "mppulverizertop")
		{
			world.BlockAccessor.SetBlock(0, pos.UpCopy());
		}
		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
	}

	public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
	{
		if (!base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		BlockSelection bs = blockSel.Clone();
		bs.Position = blockSel.Position.UpCopy();
		if (!base.CanPlaceBlock(world, byPlayer, bs, ref failureCode))
		{
			return false;
		}
		bs.Position = blockSel.Position.UpCopy(2);
		if (!base.CanPlaceBlock(world, byPlayer, bs, ref failureCode))
		{
			return false;
		}
		return true;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		BlockPos npos = pos.AddCopy(orientation);
		if (world.BlockAccessor.GetBlock(npos) is BlockAngledGears ag2 && ag2.Facings.Contains(orientation.Opposite) && ag2.Facings.Length == 1)
		{
			world.BlockAccessor.BreakBlock(npos, null);
		}
		npos = pos.AddCopy(orientation.Opposite);
		if (world.BlockAccessor.GetBlock(npos) is BlockAngledGears ag && ag.Facings.Contains(orientation) && ag.Facings.Length == 1)
		{
			world.BlockAccessor.BreakBlock(npos, null);
		}
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		ItemStack pulvFrame = new ItemStack(world.BlockAccessor.GetBlock(CodeWithParts("north")));
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BEPulverizer bep))
		{
			return new ItemStack[1] { pulvFrame };
		}
		return bep.getDrops(world, pulvFrame);
	}
}
