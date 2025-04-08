using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class AttachableInteractionHelp
{
	public static WorldInteraction[] GetOrCreateInteractionHelp(ICoreAPI api, EntityBehaviorAttachable eba, WearableSlotConfig[] wearableSlots, int slotIndex, ItemSlot slot)
	{
		string key = string.Concat("interactionhelp-attachable-", eba.entity.Code, "-", slotIndex.ToString());
		List<ItemStack> stacks = ObjectCacheUtil.GetOrCreate(api, key, delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				if (current.CreativeInventoryTabs.Length != 0 || current.CreativeInventoryStacks != null)
				{
					IAttachableToEntity attachableToEntity = IAttachableToEntity.FromCollectible(current);
					if (attachableToEntity != null)
					{
						if (current.CreativeInventoryStacks != null)
						{
							CreativeTabAndStackList[] creativeInventoryStacks = current.CreativeInventoryStacks;
							for (int i = 0; i < creativeInventoryStacks.Length; i++)
							{
								JsonItemStack[] stacks2 = creativeInventoryStacks[i].Stacks;
								foreach (JsonItemStack jsonItemStack in stacks2)
								{
									if (attachableToEntity.IsAttachable(eba.entity, jsonItemStack.ResolvedItemstack))
									{
										string categoryCode = attachableToEntity.GetCategoryCode(jsonItemStack.ResolvedItemstack);
										if (wearableSlots[slotIndex].CanHold(categoryCode))
										{
											list.Add(jsonItemStack.ResolvedItemstack);
										}
									}
								}
							}
						}
						else
						{
							ItemStack itemStack = new ItemStack(current);
							if (attachableToEntity.IsAttachable(eba.entity, itemStack))
							{
								string categoryCode2 = attachableToEntity.GetCategoryCode(itemStack);
								if (wearableSlots[slotIndex].CanHold(categoryCode2))
								{
									list.Add(itemStack);
								}
							}
						}
					}
				}
			}
			list.Shuffle(api.World.Rand);
			return list;
		});
		if (stacks.Count == 0)
		{
			return null;
		}
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = (slot.Empty ? "attachableentity-attach" : "attachableentity-detach"),
				Itemstacks = (slot.Empty ? stacks.ToArray() : null),
				MouseButton = EnumMouseButton.Right,
				HotKeyCode = "ctrl"
			}
		};
	}
}
