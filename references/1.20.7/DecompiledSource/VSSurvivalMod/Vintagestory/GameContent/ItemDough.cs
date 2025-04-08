using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemDough : Item
{
	private static ItemStack[] tableStacks;

	public override void OnLoaded(ICoreAPI api)
	{
		if (tableStacks != null)
		{
			return;
		}
		List<ItemStack> foundStacks = new List<ItemStack>();
		api.World.Collectibles.ForEach(delegate(CollectibleObject obj)
		{
			if (obj is Block block)
			{
				JsonObject attributes = block.Attributes;
				if (attributes != null && attributes.IsTrue("pieFormingSurface"))
				{
					foundStacks.Add(new ItemStack(obj));
				}
			}
		});
		tableStacks = foundStacks.ToArray();
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		tableStacks = null;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel != null)
		{
			JsonObject attributes = api.World.BlockAccessor.GetBlock(blockSel.Position).Attributes;
			if (attributes != null && attributes.IsTrue("pieFormingSurface"))
			{
				if (slot.StackSize >= 2)
				{
					(api.World.GetBlock(new AssetLocation("pie-raw")) as BlockPie).TryPlacePie(byEntity, blockSel);
				}
				else if (api is ICoreClientAPI capi)
				{
					capi.TriggerIngameError(this, "notpieable", Lang.Get("Need at least 2 dough"));
				}
				handling = EnumHandHandling.PreventDefault;
				return;
			}
		}
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-makepie",
				Itemstacks = tableStacks,
				HotKeyCode = "shift",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
