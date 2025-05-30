using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemSurvivalHandbook : ModSystem
{
	private ICoreClientAPI capi;

	private GuiDialogHandbook dialog;

	protected ItemStack[] allstacks;

	public event InitCustomPagesDelegate OnInitCustomPages;

	internal void TriggerOnInitCustomPages(List<GuiHandbookPage> pages)
	{
		this.OnInitCustomPages?.Invoke(pages);
	}

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Input.RegisterHotKeyFirst("selectionhandbook", Lang.Get("Show Handbook for current selection"), GlKeys.H, HotkeyType.HelpAndOverlays, altPressed: false, ctrlPressed: false, shiftPressed: true);
		api.Input.SetHotKeyHandler("selectionhandbook", OnSelectionHandbookHotkey);
		api.Input.RegisterHotKeyFirst("handbook", Lang.Get("Show Survival handbook"), GlKeys.H, HotkeyType.HelpAndOverlays);
		api.Input.SetHotKeyHandler("handbook", OnSurvivalHandbookHotkey);
		api.Event.LevelFinalize += Event_LevelFinalize;
		api.RegisterLinkProtocol("handbook", onHandBookLinkClicked);
		api.RegisterLinkProtocol("handbooksearch", onHandBookSearchLinkClicked);
	}

	private void onHandBookSearchLinkClicked(LinkTextComponent comp)
	{
		string text = comp.Href.Substring("handbooksearch://".Length);
		if (!dialog.IsOpened())
		{
			dialog.TryOpen();
		}
		dialog.Search(text);
	}

	private void onHandBookLinkClicked(LinkTextComponent comp)
	{
		string target = comp.Href.Substring("handbook://".Length);
		target = target.Replace("\\", "");
		if (target.StartsWithOrdinal("tab-"))
		{
			if (!dialog.IsOpened())
			{
				dialog.TryOpen();
			}
			dialog.selectTab(target.Substring(4));
		}
		else
		{
			if (!dialog.IsOpened())
			{
				dialog.TryOpen();
			}
			dialog.OpenDetailPageFor(target);
		}
	}

	private void Event_LevelFinalize()
	{
		List<ItemStack> allstacks = SetupBehaviorAndGetItemStacks();
		this.allstacks = allstacks.ToArray();
		dialog = new GuiDialogSurvivalHandbook(capi, onCreatePagesAsync, onComposePage);
		capi.Logger.VerboseDebug("Done initialising handbook");
	}

	private List<GuiHandbookPage> onCreatePagesAsync()
	{
		List<GuiHandbookPage> pages = new List<GuiHandbookPage>();
		ItemStack[] array = allstacks;
		foreach (ItemStack stack in array)
		{
			if (capi.IsShuttingDown)
			{
				break;
			}
			GuiHandbookItemStackPage elem = new GuiHandbookItemStackPage(capi, stack)
			{
				Visible = true
			};
			pages.Add(elem);
		}
		return pages;
	}

	private void onComposePage(GuiHandbookPage page, GuiComposer detailViewGui, ElementBounds textBounds, ActionConsumable<string> openDetailPageFor)
	{
		page.ComposePage(detailViewGui, textBounds, allstacks, openDetailPageFor);
	}

	protected List<ItemStack> SetupBehaviorAndGetItemStacks()
	{
		List<ItemStack> allstacks = new List<ItemStack>();
		foreach (CollectibleObject obj in capi.World.Collectibles)
		{
			if (!obj.HasBehavior<CollectibleBehaviorHandbookTextAndExtraInfo>())
			{
				CollectibleBehaviorHandbookTextAndExtraInfo bh = new CollectibleBehaviorHandbookTextAndExtraInfo(obj);
				bh.OnLoaded(capi);
				obj.CollectibleBehaviors = obj.CollectibleBehaviors.Append(bh);
			}
			List<ItemStack> stacks = obj.GetHandBookStacks(capi);
			if (stacks == null)
			{
				continue;
			}
			foreach (ItemStack stack in stacks)
			{
				allstacks.Add(stack);
			}
		}
		return allstacks;
	}

	private bool OnSurvivalHandbookHotkey(KeyCombination key)
	{
		if (dialog.IsOpened())
		{
			dialog.TryClose();
		}
		else
		{
			dialog.TryOpen();
			dialog.ignoreNextKeyPress = true;
			if (capi.World.Player.InventoryManager.CurrentHoveredSlot?.Itemstack != null)
			{
				ItemStack stack = capi.World.Player.InventoryManager.CurrentHoveredSlot.Itemstack;
				string pageCode = GuiHandbookItemStackPage.PageCodeForStack(stack);
				if (!dialog.OpenDetailPageFor(pageCode))
				{
					dialog.OpenDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(new ItemStack(stack.Collectible)));
				}
			}
		}
		return true;
	}

	private bool OnSelectionHandbookHotkey(KeyCombination key)
	{
		if (dialog.IsOpened())
		{
			dialog.TryClose();
		}
		else
		{
			dialog.TryOpen();
			dialog.ignoreNextKeyPress = true;
			if (capi.World.Player.CurrentBlockSelection != null)
			{
				BlockPos pos = capi.World.Player.CurrentBlockSelection.Position;
				ItemStack stack = capi.World.BlockAccessor.GetBlock(pos).OnPickBlock(capi.World, pos);
				string pageCode = GuiHandbookItemStackPage.PageCodeForStack(stack);
				if (!dialog.OpenDetailPageFor(pageCode))
				{
					dialog.OpenDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(new ItemStack(stack.Collectible)));
				}
			}
		}
		return true;
	}

	public override void Dispose()
	{
		base.Dispose();
		dialog?.Dispose();
		capi?.Input.HotKeys.Remove("handbook");
		capi?.Input.HotKeys.Remove("selectionhandbook");
	}
}
