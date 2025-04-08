using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemPadlock : Item
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "padlockInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Block current in api.World.Blocks)
			{
				if (!(current.Code == null) && current.HasBehavior<BlockBehaviorLockable>(withInheritance: true) && current.CreativeInventoryTabs != null && current.CreativeInventoryTabs.Length != 0)
				{
					list.Add(new ItemStack(current));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-lock",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				}
			};
		});
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel != null && byEntity.World.BlockAccessor.GetBlock(blockSel.Position).HasBehavior<BlockBehaviorLockable>(withInheritance: true))
		{
			ModSystemBlockReinforcement modBre = byEntity.World.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
			IPlayer player = (byEntity as EntityPlayer).Player;
			if (!modBre.IsReinforced(blockSel.Position))
			{
				(byEntity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "cannotlock", Lang.Get("ingameerror-cannotlock-notreinforced"));
				return;
			}
			if (!modBre.TryLock(blockSel.Position, player, Code.ToString()))
			{
				(byEntity.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "cannotlock", Lang.Get("ingameerror-cannotlock"));
			}
			else
			{
				api.World.PlaySoundAt(new AssetLocation("sounds/tool/padlock.ogg"), player, player, randomizePitch: false, 12f);
				slot.TakeOut(1);
				slot.MarkDirty();
			}
			handling = EnumHandHandling.PreventDefault;
		}
		else
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
