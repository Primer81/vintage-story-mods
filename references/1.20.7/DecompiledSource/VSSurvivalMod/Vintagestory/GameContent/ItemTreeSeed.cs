using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemTreeSeed : Item
{
	private WorldInteraction[] interactions;

	private bool isMapleSeed;

	public override void OnLoaded(ICoreAPI api)
	{
		isMapleSeed = Variant["type"] == "maple" || Variant["type"] == "crimsonkingmaple";
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "treeSeedInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Block current in api.World.Blocks)
			{
				if (!(current.Code == null) && current.EntityClass != null && current.Fertility > 0)
				{
					list.Add(new ItemStack(current));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-plant",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "shift",
					Itemstacks = list.ToArray()
				}
			};
		});
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
		if (isMapleSeed && target == EnumItemRenderTarget.Ground)
		{
			EntityItem ei = (renderinfo.InSlot as EntityItemSlot).Ei;
			if (!ei.Collided && !ei.Swimming)
			{
				renderinfo.Transform = renderinfo.Transform.Clone();
				renderinfo.Transform.Rotation.X = -90f;
				renderinfo.Transform.Rotation.Y = (float)((double)capi.World.ElapsedMilliseconds % 360.0) * 2f;
				renderinfo.Transform.Rotation.Z = 0f;
			}
		}
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null || !byEntity.Controls.ShiftKey)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		string treetype = Variant["type"];
		Block saplBlock = byEntity.World.GetBlock(AssetLocation.Create("sapling-" + treetype + "-free", Code.Domain));
		if (saplBlock == null)
		{
			return;
		}
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		blockSel = blockSel.Clone();
		blockSel.Position.Up();
		string failureCode = "";
		if (!saplBlock.TryPlaceBlock(api.World, byPlayer, itemslot.Itemstack, blockSel, ref failureCode))
		{
			if (api is ICoreClientAPI capi && failureCode != null && failureCode != "__ignore__")
			{
				capi.TriggerIngameError(this, failureCode, Lang.Get("placefailure-" + failureCode));
			}
		}
		else
		{
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/dirt1"), (float)blockSel.Position.X + 0.5f, blockSel.Position.InternalY, (float)blockSel.Position.Z + 0.5f, byPlayer);
			((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			if (byPlayer == null || (byPlayer.WorldData?.CurrentGameMode).GetValueOrDefault() != EnumGameMode.Creative)
			{
				itemslot.TakeOut(1);
				itemslot.MarkDirty();
			}
		}
		handHandling = EnumHandHandling.PreventDefault;
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
