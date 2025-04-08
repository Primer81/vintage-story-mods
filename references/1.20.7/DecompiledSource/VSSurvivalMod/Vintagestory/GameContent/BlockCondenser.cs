using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockCondenser : BlockLiquidContainerTopOpened
{
	public override bool AllowHeldLiquidTransfer => false;

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack stack = base.OnPickBlock(world, pos);
		SetContents(stack, null);
		return stack;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntityCondenser obj = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityCondenser;
		if (obj != null && obj.OnBlockInteractStart(byPlayer, blockSel))
		{
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		StringBuilder dsc = new StringBuilder();
		dsc.AppendLine(base.GetPlacedBlockInfo(world, pos, forPlayer));
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCondenser be && !be.Inventory[1].Empty)
		{
			BlockLiquidContainerBase obj = be.Inventory[1].Itemstack.Collectible as BlockLiquidContainerBase;
			dsc.Append(Lang.Get("Container:") + " ");
			obj.GetContentInfo(be.Inventory[1], dsc, world);
		}
		return dsc.ToString();
	}
}
