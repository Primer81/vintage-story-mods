using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class InventoryCharacter : InventoryBasePlayer
{
	private ItemSlot[] slots;

	private Dictionary<EnumCharacterDressType, string> iconByDressType = new Dictionary<EnumCharacterDressType, string>
	{
		{
			EnumCharacterDressType.Foot,
			"boots"
		},
		{
			EnumCharacterDressType.Hand,
			"gloves"
		},
		{
			EnumCharacterDressType.Shoulder,
			"cape"
		},
		{
			EnumCharacterDressType.Head,
			"hat"
		},
		{
			EnumCharacterDressType.LowerBody,
			"trousers"
		},
		{
			EnumCharacterDressType.UpperBody,
			"shirt"
		},
		{
			EnumCharacterDressType.UpperBodyOver,
			"pullover"
		},
		{
			EnumCharacterDressType.Neck,
			"necklace"
		},
		{
			EnumCharacterDressType.Arm,
			"bracers"
		},
		{
			EnumCharacterDressType.Waist,
			"belt"
		},
		{
			EnumCharacterDressType.Emblem,
			"medal"
		},
		{
			EnumCharacterDressType.Face,
			"mask"
		},
		{
			EnumCharacterDressType.ArmorHead,
			"armorhead"
		},
		{
			EnumCharacterDressType.ArmorBody,
			"armorbody"
		},
		{
			EnumCharacterDressType.ArmorLegs,
			"armorlegs"
		}
	};

	public override int Count => slots.Length;

	public override ItemSlot this[int slotId]
	{
		get
		{
			if (slotId < 0 || slotId >= Count)
			{
				return null;
			}
			return slots[slotId];
		}
		set
		{
			if (slotId < 0 || slotId >= Count)
			{
				throw new ArgumentOutOfRangeException("slotId");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			slots[slotId] = value;
		}
	}

	public InventoryCharacter(string className, string playerUID, ICoreAPI api)
		: base(className, playerUID, api)
	{
		slots = GenEmptySlots(15);
		baseWeight = 2.5f;
	}

	public InventoryCharacter(string inventoryId, ICoreAPI api)
		: base(inventoryId, api)
	{
		slots = GenEmptySlots(15);
		baseWeight = 2.5f;
	}

	public override void OnItemSlotModified(ItemSlot slot)
	{
		base.OnItemSlotModified(slot);
	}

	public override void FromTreeAttributes(ITreeAttribute tree)
	{
		slots = SlotsFromTreeAttributes(tree);
		if (slots.Length == 10)
		{
			ItemSlot[] prevSlots2 = slots;
			slots = GenEmptySlots(12);
			for (int j = 0; j < prevSlots2.Length; j++)
			{
				slots[j] = prevSlots2[j];
			}
		}
		if (slots.Length == 12)
		{
			ItemSlot[] prevSlots = slots;
			slots = GenEmptySlots(15);
			for (int i = 0; i < prevSlots.Length; i++)
			{
				slots[i] = prevSlots[i];
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		SlotsToTreeAttributes(slots, tree);
		ResolveBlocksOrItems();
	}

	protected override ItemSlot NewSlot(int slotId)
	{
		ItemSlotCharacter slot = new ItemSlotCharacter((EnumCharacterDressType)slotId, this);
		iconByDressType.TryGetValue((EnumCharacterDressType)slotId, out slot.BackgroundIcon);
		return slot;
	}

	public override void DiscardAll()
	{
	}

	public override void OnOwningEntityDeath(Vec3d pos)
	{
	}

	public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null)
	{
		return new WeightedSlot();
	}
}
