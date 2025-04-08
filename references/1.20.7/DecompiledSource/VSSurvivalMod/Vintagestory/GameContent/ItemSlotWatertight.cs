using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class ItemSlotWatertight : ItemSlotSurvival
{
	public float capacityLitres;

	public ItemSlotWatertight(InventoryBase inventory, float capacityLitres = 6f)
		: base(inventory)
	{
		this.capacityLitres = capacityLitres;
	}

	public override bool CanTake()
	{
		if (!Empty && itemstack.Collectible.IsLiquid())
		{
			return false;
		}
		return base.CanTake();
	}

	protected override void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		IWorldAccessor world = inventory.Api.World;
		if (sourceSlot.Itemstack?.Block is BlockLiquidContainerBase liqCntBlock)
		{
			ItemStack contentStack = liqCntBlock.GetContent(sourceSlot.Itemstack);
			WaterTightContainableProps liqProps = BlockLiquidContainerBase.GetContainableProps(contentStack);
			bool stackable = !Empty && itemstack.Equals(world, contentStack, GlobalConstants.IgnoredStackAttributes);
			if ((Empty || stackable) && contentStack != null)
			{
				ItemStack bucketStack = sourceSlot.Itemstack;
				float toMoveLitres = ((op?.ActingPlayer?.Entity.Controls.ShiftKey).GetValueOrDefault() ? liqCntBlock.CapacityLitres : liqCntBlock.TransferSizeLitres);
				float curDestLitres = (float)base.StackSize / liqProps.ItemsPerLitre;
				float curSrcLitres = (float)contentStack.StackSize / liqProps.ItemsPerLitre;
				toMoveLitres = Math.Min(toMoveLitres, curSrcLitres);
				toMoveLitres *= (float)bucketStack.StackSize;
				toMoveLitres = Math.Min(toMoveLitres, capacityLitres - curDestLitres);
				if (toMoveLitres > 0f)
				{
					int moveQuantity = (int)(liqProps.ItemsPerLitre * toMoveLitres);
					ItemStack takenContentStack = liqCntBlock.TryTakeContent(bucketStack, moveQuantity / bucketStack.StackSize);
					takenContentStack.StackSize *= bucketStack.StackSize;
					takenContentStack.StackSize += base.StackSize;
					itemstack = takenContentStack;
					MarkDirty();
					op.MovedQuantity = moveQuantity;
				}
			}
			return;
		}
		string contentItemCode = sourceSlot.Itemstack?.ItemAttributes?["contentItemCode"].AsString();
		if (contentItemCode != null)
		{
			ItemStack contentStack2 = new ItemStack(world.GetItem(AssetLocation.Create(contentItemCode, sourceSlot.Itemstack.Collectible.Code.Domain)));
			bool stackable2 = !Empty && itemstack.Equals(world, contentStack2, GlobalConstants.IgnoredStackAttributes);
			if (!(Empty || stackable2) || contentStack2 == null)
			{
				return;
			}
			if (stackable2)
			{
				itemstack.StackSize++;
			}
			else
			{
				itemstack = contentStack2;
			}
			MarkDirty();
			ItemStack bowlStack = new ItemStack(world.GetBlock(AssetLocation.Create(sourceSlot.Itemstack.ItemAttributes["emptiedBlockCode"].AsString(), sourceSlot.Itemstack.Collectible.Code.Domain)));
			if (sourceSlot.StackSize == 1)
			{
				sourceSlot.Itemstack = bowlStack;
			}
			else
			{
				sourceSlot.Itemstack.StackSize--;
				if (!op.ActingPlayer.InventoryManager.TryGiveItemstack(bowlStack))
				{
					world.SpawnItemEntity(bowlStack, op.ActingPlayer.Entity.Pos.XYZ);
				}
			}
			sourceSlot.MarkDirty();
		}
		else
		{
			ItemStack itemStack = sourceSlot.Itemstack;
			if (itemStack == null || !(itemStack.ItemAttributes?["contentItem2BlockCodes"].Exists).GetValueOrDefault())
			{
				base.ActivateSlotLeftClick(sourceSlot, ref op);
			}
		}
	}

	protected override void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		IWorldAccessor world = inventory.Api.World;
		if (sourceSlot.Itemstack?.Block is BlockLiquidContainerBase liqCntBlock)
		{
			if (!Empty)
			{
				ItemStack contentStack = liqCntBlock.GetContent(sourceSlot.Itemstack);
				float toMoveLitres = (op.ShiftDown ? liqCntBlock.CapacityLitres : liqCntBlock.TransferSizeLitres);
				WaterTightContainableProps srcProps = BlockLiquidContainerBase.GetContainableProps(base.Itemstack);
				float availableLitres = (float)base.StackSize / (srcProps?.ItemsPerLitre ?? 1f);
				toMoveLitres *= (float)sourceSlot.Itemstack.StackSize;
				toMoveLitres = Math.Min(toMoveLitres, availableLitres);
				if (contentStack == null)
				{
					int moved = liqCntBlock.TryPutLiquid(sourceSlot.Itemstack, base.Itemstack, toMoveLitres / (float)sourceSlot.Itemstack.StackSize);
					TakeOut(moved * sourceSlot.Itemstack.StackSize);
					MarkDirty();
				}
				else if (itemstack.Equals(world, contentStack, GlobalConstants.IgnoredStackAttributes))
				{
					int moved2 = liqCntBlock.TryPutLiquid(sourceSlot.Itemstack, liqCntBlock.GetContent(sourceSlot.Itemstack), toMoveLitres / (float)sourceSlot.Itemstack.StackSize);
					TakeOut(moved2 * sourceSlot.Itemstack.StackSize);
					MarkDirty();
				}
			}
			return;
		}
		if (itemstack != null)
		{
			ItemStack itemStack = sourceSlot.Itemstack;
			if (itemStack != null && (itemStack.ItemAttributes?["contentItem2BlockCodes"].Exists).GetValueOrDefault())
			{
				string outBlockCode = sourceSlot.Itemstack.ItemAttributes["contentItem2BlockCodes"][itemstack.Collectible.Code.ToShortString()].AsString();
				if (outBlockCode == null)
				{
					return;
				}
				ItemStack outBlockStack = new ItemStack(world.GetBlock(AssetLocation.Create(outBlockCode, sourceSlot.Itemstack.Collectible.Code.Domain)));
				if (sourceSlot.StackSize == 1)
				{
					sourceSlot.Itemstack = outBlockStack;
				}
				else
				{
					sourceSlot.Itemstack.StackSize--;
					if (!op.ActingPlayer.InventoryManager.TryGiveItemstack(outBlockStack))
					{
						world.SpawnItemEntity(outBlockStack, op.ActingPlayer.Entity.Pos.XYZ);
					}
				}
				sourceSlot.MarkDirty();
				TakeOut(1);
				return;
			}
		}
		ItemStack itemStack2 = sourceSlot.Itemstack;
		if ((itemStack2 == null || !(itemStack2.ItemAttributes?["contentItem2BlockCodes"].Exists).GetValueOrDefault()) && sourceSlot.Itemstack?.ItemAttributes?["contentItemCode"].AsString() == null)
		{
			base.ActivateSlotRightClick(sourceSlot, ref op);
		}
	}
}
