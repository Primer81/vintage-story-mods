using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemBook : ItemRollable
{
	private ModSystemEditableBook bookModSys;

	private int maxPageCount;

	private bool editable;

	private ICoreClientAPI capi;

	private WorldInteraction[] interactions;

	private ItemSlot curSlot;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
		editable = Attributes["editable"].AsBool();
		maxPageCount = Attributes["maxPageCount"].AsInt(90);
		bookModSys = api.ModLoader.GetModSystem<ModSystemEditableBook>();
		interactions = ObjectCacheUtil.GetOrCreate(api, "bookInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				if (current.Attributes != null && current.Attributes.IsTrue("writingTool"))
				{
					list.Add(new ItemStack(current));
				}
			}
			return new WorldInteraction[2]
			{
				new WorldInteraction
				{
					MouseButton = EnumMouseButton.Right,
					ActionLangCode = "heldhelp-read",
					ShouldApply = delegate
					{
						ItemSlot activeHotbarSlot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
						return isReadable(activeHotbarSlot) && (activeHotbarSlot.Itemstack.Attributes.HasAttribute("text") || activeHotbarSlot.Itemstack.Attributes.HasAttribute("textCodes"));
					}
				},
				new WorldInteraction
				{
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					ActionLangCode = "heldhelp-write",
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (capi.World.Player.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.GetString("signedby") == null) ? wi.Itemstacks : null
				}
			};
		});
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (byEntity.Controls.ShiftKey)
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
			return;
		}
		if (!isReadable(slot))
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
			return;
		}
		IPlayer player = (byEntity as EntityPlayer).Player;
		if (editable && isWritingTool(byEntity.LeftHandItemSlot) && !isSigned(slot))
		{
			bookModSys.BeginEdit(player, slot);
			if (api.Side == EnumAppSide.Client)
			{
				GuiDialogEditableBook dlg = new GuiDialogEditableBook(slot.Itemstack, api as ICoreClientAPI, maxPageCount);
				dlg.OnClosed += delegate
				{
					if (dlg.DidSave)
					{
						bookModSys.EndEdit(player, dlg.AllPagesText, dlg.Title, dlg.DidSign);
					}
					else
					{
						bookModSys.CancelEdit(player);
					}
				};
				dlg.TryOpen();
			}
			handling = EnumHandHandling.PreventDefault;
		}
		else if (slot.Itemstack.Attributes.HasAttribute("text") || slot.Itemstack.Attributes.HasAttribute("textCodes"))
		{
			bookModSys.BeginEdit(player, slot);
			if (api.Side == EnumAppSide.Client)
			{
				curSlot = slot;
				GuiDialogReadonlyBook guiDialogReadonlyBook = new GuiDialogReadonlyBook(slot.Itemstack, api as ICoreClientAPI, onTranscribePressed);
				guiDialogReadonlyBook.OnClosed += delegate
				{
					curSlot = null;
					bookModSys.CancelEdit(player);
				};
				guiDialogReadonlyBook.TryOpen();
			}
			handling = EnumHandHandling.PreventDefault;
		}
		else
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		}
	}

	private void onTranscribePressed(string pageText, string pageTitle, int pageNumber)
	{
		bookModSys.Transcribe(capi.World.Player, pageText, pageTitle, pageNumber, curSlot);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		string title = itemStack.Attributes.GetString("title");
		if (title != null && title.Length > 0)
		{
			return title;
		}
		return base.GetHeldItemName(itemStack);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string signedby = inSlot.Itemstack.Attributes.GetString("signedby");
		string transcribedby = inSlot.Itemstack.Attributes.GetString("transcribedby");
		if (signedby != null)
		{
			dsc.AppendLine(Lang.Get("Written by {0}", (signedby.Length == 0) ? Lang.Get("Unknown author") : signedby));
		}
		if (transcribedby != null && transcribedby.Length > 0)
		{
			dsc.AppendLine(Lang.Get("Transcribed by {0}", (transcribedby.Length == 0) ? Lang.Get("Unknown author") : transcribedby));
		}
	}

	public static bool isReadable(ItemSlot slot)
	{
		return slot.Itemstack.Collectible.Attributes["readable"].AsBool();
	}

	public static bool isSigned(ItemSlot slot)
	{
		return slot.Itemstack.Attributes.GetString("signedby") != null;
	}

	public static bool isWritingTool(ItemSlot slot)
	{
		ItemStack itemstack = slot.Itemstack;
		if (itemstack == null)
		{
			return false;
		}
		return (itemstack.Collectible.Attributes?.IsTrue("writingTool")).GetValueOrDefault();
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
