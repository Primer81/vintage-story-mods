using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class InventoryStoneCoffin : InventoryGeneric
{
	private Vec3d secondaryPos;

	public InventoryStoneCoffin(int size, string invId, ICoreAPI api)
		: base(size, invId, api)
	{
	}

	public override void DropAll(Vec3d pos, int maxStackSize = 0)
	{
		using IEnumerator<ItemSlot> enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			ItemSlot slot = enumerator.Current;
			if (slot.Itemstack == null)
			{
				continue;
			}
			int count = slot.Itemstack.StackSize;
			if (count != 0)
			{
				int i;
				for (i = 0; i + 2 <= count; i += 2)
				{
					ItemStack newStack2 = slot.Itemstack.Clone();
					newStack2.StackSize = 1;
					Api.World.SpawnItemEntity(newStack2, pos);
					Api.World.SpawnItemEntity(newStack2.Clone(), secondaryPos);
				}
				if (i < count)
				{
					ItemStack newStack = slot.Itemstack.Clone();
					newStack.StackSize = 1;
					Api.World.SpawnItemEntity(newStack, pos);
				}
				slot.Itemstack = null;
				slot.MarkDirty();
			}
		}
	}

	internal void SetSecondaryPos(BlockPos blockPos)
	{
		secondaryPos = blockPos.ToVec3d();
	}
}
