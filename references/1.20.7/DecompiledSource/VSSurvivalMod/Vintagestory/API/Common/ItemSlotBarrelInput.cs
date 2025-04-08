using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Vintagestory.API.Common;

public class ItemSlotBarrelInput : ItemSlot
{
	public ItemSlotBarrelInput(InventoryBase inventory)
		: base(inventory)
	{
	}

	public override void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		base.ActivateSlot(sourceSlot, ref op);
	}

	public override void OnItemSlotModified(ItemStack stack)
	{
		base.OnItemSlotModified(stack);
		if (itemstack == null)
		{
			return;
		}
		ItemSlotLiquidOnly liquidSlot = inventory[1] as ItemSlotLiquidOnly;
		bool stackable = !liquidSlot.Empty && liquidSlot.Itemstack.Equals(inventory.Api.World, itemstack, GlobalConstants.IgnoredStackAttributes);
		if (stackable)
		{
			int remaining = liquidSlot.Itemstack.Collectible.MaxStackSize - liquidSlot.Itemstack.StackSize;
			WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(liquidSlot.Itemstack);
			if (props != null)
			{
				int val = (int)(liquidSlot.CapacityLitres * props.ItemsPerLitre);
				int maxOverride = props.MaxStackSize;
				remaining = Math.Max(val, maxOverride) - liquidSlot.Itemstack.StackSize;
			}
			int moved = GameMath.Clamp(itemstack.StackSize, 0, remaining);
			liquidSlot.Itemstack.StackSize += moved;
			itemstack.StackSize -= moved;
			if (itemstack.StackSize <= 0)
			{
				itemstack = null;
			}
			liquidSlot.MarkDirty();
			MarkDirty();
			return;
		}
		JsonObject attributes = itemstack.Collectible.Attributes;
		if (attributes == null || !attributes.IsTrue("barrelMoveToLiquidSlot"))
		{
			if (!stackable)
			{
				return;
			}
			JsonObject attributes2 = itemstack.Collectible.Attributes;
			if (attributes2 == null || !attributes2["waterTightContainerProps"].Exists)
			{
				return;
			}
		}
		if (stackable)
		{
			int remainingspace = itemstack.Collectible.MaxStackSize - liquidSlot.StackSize;
			int movableq = Math.Min(itemstack.StackSize, remainingspace);
			liquidSlot.Itemstack.StackSize += movableq;
			itemstack.StackSize -= movableq;
			if (base.StackSize <= 0)
			{
				itemstack = null;
			}
			MarkDirty();
			liquidSlot.MarkDirty();
		}
		else if (liquidSlot.Empty)
		{
			liquidSlot.Itemstack = itemstack.Clone();
			itemstack = null;
			MarkDirty();
			liquidSlot.MarkDirty();
		}
	}

	protected override void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		ItemSlotLiquidOnly liquidSlot = inventory[1] as ItemSlotLiquidOnly;
		IWorldAccessor world = inventory.Api.World;
		if (sourceSlot?.Itemstack?.Collectible is ILiquidSink sink && !liquidSlot.Empty && sink.AllowHeldLiquidTransfer)
		{
			ItemStack liqSlotStack = liquidSlot.Itemstack;
			ItemStack curTargetLiquidStack = sink.GetContent(sourceSlot.Itemstack);
			if (curTargetLiquidStack != null && !liqSlotStack.Equals(world, curTargetLiquidStack, GlobalConstants.IgnoredStackAttributes))
			{
				return;
			}
			WaterTightContainableProps lprops = BlockLiquidContainerBase.GetContainableProps(liqSlotStack);
			float val = (float)liqSlotStack.StackSize / lprops.ItemsPerLitre;
			float curTargetLitres = sink.GetCurrentLitres(sourceSlot.Itemstack);
			float toMoveLitres = (op.CtrlDown ? sink.TransferSizeLitres : (sink.CapacityLitres - curTargetLitres));
			toMoveLitres *= (float)sourceSlot.StackSize;
			toMoveLitres = Math.Min(val, toMoveLitres);
			if (toMoveLitres > 0f)
			{
				op.MovedQuantity = sink.TryPutLiquid(sourceSlot.Itemstack, liqSlotStack, toMoveLitres / (float)sourceSlot.StackSize);
				liquidSlot.Itemstack.StackSize -= op.MovedQuantity * sourceSlot.StackSize;
				if (liquidSlot.Itemstack.StackSize <= 0)
				{
					liquidSlot.Itemstack = null;
				}
				liquidSlot.MarkDirty();
				sourceSlot.MarkDirty();
				EntityPos pos = op.ActingPlayer?.Entity?.Pos;
				if (pos != null)
				{
					op.World.PlaySoundAt(lprops.PourSound, pos.X, pos.InternalY, pos.Z);
				}
			}
		}
		else
		{
			base.ActivateSlotRightClick(sourceSlot, ref op);
		}
	}

	protected override void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		if (sourceSlot.Empty)
		{
			base.ActivateSlotLeftClick(sourceSlot, ref op);
			return;
		}
		IWorldAccessor world = inventory.Api.World;
		if (sourceSlot.Itemstack.Collectible is ILiquidSource { AllowHeldLiquidTransfer: not false } source)
		{
			ItemSlotLiquidOnly liquidSlot = inventory[1] as ItemSlotLiquidOnly;
			ItemStack bucketContents = source.GetContent(sourceSlot.Itemstack);
			bool stackable = !liquidSlot.Empty && liquidSlot.Itemstack.Equals(world, bucketContents, GlobalConstants.IgnoredStackAttributes);
			if (!(liquidSlot.Empty || stackable) || bucketContents == null)
			{
				return;
			}
			ItemStack bucketStack = sourceSlot.Itemstack;
			WaterTightContainableProps lprops = BlockLiquidContainerBase.GetContainableProps(bucketContents);
			float toMoveLitres = (op.CtrlDown ? source.TransferSizeLitres : source.CapacityLitres);
			float curSourceLitres = (float)bucketContents.StackSize / lprops.ItemsPerLitre * (float)bucketStack.StackSize;
			float curDestLitres = (float)liquidSlot.StackSize / lprops.ItemsPerLitre;
			toMoveLitres = Math.Min(toMoveLitres, curSourceLitres);
			toMoveLitres = Math.Min(toMoveLitres, liquidSlot.CapacityLitres - curDestLitres);
			if (toMoveLitres > 0f)
			{
				int moveQuantity = (int)(toMoveLitres * lprops.ItemsPerLitre);
				ItemStack takenContentStack = source.TryTakeContent(bucketStack, moveQuantity / bucketStack.StackSize);
				takenContentStack.StackSize *= bucketStack.StackSize;
				takenContentStack.StackSize += liquidSlot.StackSize;
				liquidSlot.Itemstack = takenContentStack;
				liquidSlot.MarkDirty();
				op.MovedQuantity = moveQuantity;
				EntityPos pos = op.ActingPlayer?.Entity?.Pos;
				if (pos != null)
				{
					op.World.PlaySoundAt(lprops.FillSound, pos.X, pos.InternalY, pos.Z);
				}
			}
			return;
		}
		string contentItemCode = sourceSlot.Itemstack?.ItemAttributes?["contentItemCode"].AsString();
		if (contentItemCode != null)
		{
			ItemSlot liquidSlot2 = inventory[1];
			ItemStack contentStack = new ItemStack(world.GetItem(new AssetLocation(contentItemCode)));
			bool stackable2 = !liquidSlot2.Empty && liquidSlot2.Itemstack.Equals(world, contentStack, GlobalConstants.IgnoredStackAttributes);
			if (!(liquidSlot2.Empty || stackable2) || contentStack == null)
			{
				return;
			}
			if (stackable2)
			{
				liquidSlot2.Itemstack.StackSize++;
			}
			else
			{
				liquidSlot2.Itemstack = contentStack;
			}
			liquidSlot2.MarkDirty();
			ItemStack bowlStack = new ItemStack(world.GetBlock(new AssetLocation(sourceSlot.Itemstack.ItemAttributes["emptiedBlockCode"].AsString())));
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
			base.ActivateSlotLeftClick(sourceSlot, ref op);
		}
	}
}
