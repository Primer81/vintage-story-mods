using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.ServerMods.WorldEdit;

namespace Vintagestory.GameContent;

public class ItemMagicWand : Item
{
	private ICoreServerAPI sapi;

	public override void OnLoaded(ICoreAPI api)
	{
		if (api.Side == EnumAppSide.Server)
		{
			sapi = api as ICoreServerAPI;
		}
		base.OnLoaded(api);
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		if (byEntity.World.Side == EnumAppSide.Server && (byEntity as EntityPlayer).Player is IServerPlayer plr)
		{
			sapi.ModLoader.GetModSystem<WorldEdit>().OnAttackStart(plr, blockSel);
		}
		handling = EnumHandHandling.PreventDefaultAction;
	}

	public override void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
	{
		if (!firstEvent)
		{
			handling = EnumHandHandling.PreventDefaultAction;
			return;
		}
		if (useType == EnumHandInteract.HeldItemAttack)
		{
			OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
		}
		if (useType == EnumHandInteract.HeldItemInteract)
		{
			if (byEntity.World.Side == EnumAppSide.Server && (byEntity as EntityPlayer)?.Player is IServerPlayer plr)
			{
				sapi.ModLoader.GetModSystem<WorldEdit>().OnInteractStart(plr, blockSel);
			}
			handling = EnumHandHandling.PreventDefaultAction;
		}
	}
}
