using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

internal class InventoryPlayerGround : InventoryBasePlayer
{
	private ItemSlot slot;

	public override int Count => 1;

	public override ItemSlot this[int slotId]
	{
		get
		{
			if (slotId != 0)
			{
				return null;
			}
			return slot;
		}
		set
		{
			if (slotId != 0)
			{
				throw new ArgumentOutOfRangeException("slotId");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			slot = value;
		}
	}

	public InventoryPlayerGround(string className, string playerUID, ICoreAPI api)
		: base(className, playerUID, api)
	{
		slot = new ItemSlotGround(this);
	}

	public InventoryPlayerGround(string inventoryID, ICoreAPI api)
		: base(inventoryID, api)
	{
		slot = new ItemSlotGround(this);
	}

	public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null)
	{
		return new WeightedSlot
		{
			slot = null,
			weight = 0f
		};
	}

	public override void FromTreeAttributes(ITreeAttribute tree)
	{
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
	}

	public override void OnItemSlotModified(ItemSlot slot)
	{
		Entity entityplayer = Api.World.PlayerByUid(playerUID)?.Entity;
		if (slot.Itemstack != null && entityplayer != null)
		{
			Vec3d spawnpos = entityplayer.SidedPos.XYZ.Add(0.0, entityplayer.CollisionBox.Y1 + entityplayer.CollisionBox.Y2 * 0.75f, 0.0);
			Vec3d velocity = (entityplayer.SidedPos.AheadCopy(1.0).XYZ.Add(entityplayer.LocalEyePos) - spawnpos) * 0.1 + entityplayer.SidedPos.Motion * 1.5;
			ItemStack stack = slot.Itemstack;
			slot.Itemstack = null;
			while (stack.StackSize > 0)
			{
				Vec3d velo = velocity.Clone().Add((float)(Api.World.Rand.NextDouble() - 0.5) / 60f, (float)(Api.World.Rand.NextDouble() - 0.5) / 60f, (float)(Api.World.Rand.NextDouble() - 0.5) / 60f);
				ItemStack dropStack = stack.Clone();
				dropStack.StackSize = Math.Min(4, stack.StackSize);
				stack.StackSize -= dropStack.StackSize;
				Api.World.SpawnItemEntity(dropStack, spawnpos, velo);
			}
		}
	}

	public override void MarkSlotDirty(int slotId)
	{
	}
}
