using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockTorchHolder : Block
{
	public bool Empty => Variant["state"] == "empty";

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (Empty)
		{
			ItemStack heldStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			if (heldStack != null && heldStack.Collectible.Code.Path.Equals("torch-basic-lit-up"))
			{
				byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
				byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
				Block filledBlock = world.GetBlock(CodeWithVariant("state", "filled"));
				world.BlockAccessor.ExchangeBlock(filledBlock.BlockId, blockSel.Position);
				if (Sounds?.Place != null)
				{
					world.PlaySoundAt(Sounds.Place, blockSel.Position, 0.1, byPlayer);
				}
				return true;
			}
		}
		else
		{
			ItemStack stack = new ItemStack(world.GetBlock(new AssetLocation("torch-basic-lit-up")));
			if (byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
			{
				Block filledBlock2 = world.GetBlock(CodeWithVariant("state", "empty"));
				world.BlockAccessor.ExchangeBlock(filledBlock2.BlockId, blockSel.Position);
				if (Sounds?.Place != null)
				{
					world.PlaySoundAt(Sounds.Place, blockSel.Position, 0.1, byPlayer);
				}
				return true;
			}
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		if (Empty)
		{
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-torchholder-addtorch",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = new ItemStack[1]
					{
						new ItemStack(world.GetBlock(new AssetLocation("torch-basic-lit-up")))
					}
				}
			}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
		}
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-torchholder-removetorch",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = null
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
