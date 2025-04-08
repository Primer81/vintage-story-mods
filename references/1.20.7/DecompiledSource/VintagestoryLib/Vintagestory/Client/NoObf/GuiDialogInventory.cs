using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class GuiDialogInventory : GuiDialog
{
	private ITabbedInventory creativeInv;

	private IInventory backPackInv;

	private IInventory craftingInv;

	private GuiComposer creativeInvDialog;

	private GuiComposer survivalInvDialog;

	private int currentTabIndex;

	private int cols = 15;

	private ElementBounds creativeClippingBounds;

	private int prevRows;

	private EnumGameMode prevGameMode;

	public override double DrawOrder => 0.2;

	public override string ToggleKeyCombinationCode => "inventorydialog";

	public override bool PrefersUngrabbedMouse => Composers["maininventory"] == creativeInvDialog;

	public override float ZSize => 250f;

	public GuiDialogInventory(ICoreClientAPI capi)
		: base(capi)
	{
		(capi.World as ClientMain).eventManager.OnPlayerModeChange.Add(OnPlayerModeChanged);
		capi.Input.RegisterHotKey("creativesearch", Lang.Get("Search Creative inventory"), GlKeys.F, HotkeyType.CreativeTool, altPressed: false, ctrlPressed: true);
		capi.Input.SetHotKeyHandler("creativesearch", onSearchCreative);
	}

	private bool onSearchCreative(KeyCombination t1)
	{
		if (capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			return false;
		}
		if (TryOpen())
		{
			creativeInvDialog.FocusElement(creativeInvDialog.GetTextInput("searchbox").TabIndex);
		}
		return true;
	}

	public override void OnOwnPlayerDataReceived()
	{
		capi.Logger.VerboseDebug("GuiDialogInventory: starting composeGUI");
		ComposeGui(firstBuild: true);
		capi.Logger.VerboseDebug("GuiDialogInventory: done composeGUI");
		TyronThreadPool.QueueTask(delegate
		{
			creativeInv.CreativeTabs.CreateSearchCache(capi.World);
		});
		prevGameMode = capi.World.Player.WorldData.CurrentGameMode;
	}

	public void ComposeGui(bool firstBuild)
	{
		IPlayerInventoryManager invm = capi.World.Player.InventoryManager;
		creativeInv = (ITabbedInventory)invm.GetOwnInventory("creative");
		craftingInv = invm.GetOwnInventory("craftinggrid");
		backPackInv = invm.GetOwnInventory("backpack");
		if (firstBuild)
		{
			backPackInv.SlotModified += BackPackInv_SlotModified;
		}
		if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			ComposeCreativeInvDialog();
			Composers["maininventory"] = creativeInvDialog;
		}
		if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Guest || capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival)
		{
			ComposeSurvivalInvDialog();
			Composers["maininventory"] = survivalInvDialog;
		}
		if (firstBuild)
		{
			OnPlayerModeChanged();
		}
	}

	private void ComposeCreativeInvDialog()
	{
		if (creativeInv == null)
		{
			ScreenManager.Platform.Logger.Notification("Server did not send a creative inventory, so I won't display one");
			return;
		}
		double elemToDlgPad = GuiStyle.ElementToDialogPadding;
		double pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
		int rows = (int)Math.Ceiling((float)creativeInv.Count / (float)cols);
		ElementBounds slotGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, pad, cols, 9).FixedGrow(2.0 * pad, 2.0 * pad);
		ElementBounds fullGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, cols, rows);
		creativeClippingBounds = slotGridBounds.ForkBoundingParent();
		creativeClippingBounds.Name = "clip";
		ElementBounds insetBounds = creativeClippingBounds.ForkBoundingParent(6.0, 3.0, 0.0, 3.0);
		insetBounds.Name = "inset";
		ElementBounds dialogBounds = insetBounds.ForkBoundingParent(elemToDlgPad, elemToDlgPad + 70.0, elemToDlgPad + 31.0, elemToDlgPad).WithFixedAlignmentOffset(-3.0, -100.0).WithAlignment(EnumDialogArea.CenterBottom);
		ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(insetBounds).WithParent(dialogBounds);
		ElementBounds textInputBounds = ElementBounds.Fixed(elemToDlgPad, 45.0, 250.0, 30.0);
		ElementBounds tabBoundsL = ElementBounds.Fixed(-130.0, 35.0, 130.0, 545.0);
		ElementBounds tabBoundsR = ElementBounds.Fixed(0.0, 35.0, 130.0, 545.0).FixedRightOf(dialogBounds).WithFixedAlignmentOffset(-4.0, 0.0);
		ElementBounds rightTextBounds = ElementBounds.Fixed(elemToDlgPad, 45.0, 250.0, 30.0).WithAlignment(EnumDialogArea.RightFixed).WithFixedAlignmentOffset(-28.0 - elemToDlgPad, 7.0);
		CreativeTabsConfig creativeTabsConfig = capi.Assets.TryGet("config/creativetabs.json").ToObject<CreativeTabsConfig>();
		IEnumerable<CreativeTab> unorderedTabs = creativeInv.CreativeTabs.Tabs;
		List<TabConfig> orderedTabs = new List<TabConfig>();
		foreach (CreativeTab tab2 in unorderedTabs)
		{
			TabConfig tabcfg = creativeTabsConfig.TabConfigs.FirstOrDefault((TabConfig cfg) => cfg.Code == tab2.Code);
			if (tabcfg == null)
			{
				tabcfg = new TabConfig
				{
					Code = tab2.Code,
					ListOrder = 1.0
				};
			}
			int pos = 0;
			for (int j = 0; j < orderedTabs.Count && orderedTabs[j].ListOrder < tabcfg.ListOrder; j++)
			{
				pos++;
			}
			orderedTabs.Insert(pos, tabcfg);
		}
		int currentGuiTabIndex = 0;
		GuiTab[] tabs = new GuiTab[orderedTabs.Count];
		double maxWidth = 0.0;
		double padding = GuiElement.scaled(3.0);
		CairoFont font = CairoFont.WhiteDetailText().WithFontSize(17f);
		int i;
		for (i = 0; i < orderedTabs.Count; i++)
		{
			int tabIndex = unorderedTabs.FirstOrDefault((CreativeTab tab) => tab.Code == orderedTabs[i].Code).Index;
			if (tabIndex == currentTabIndex)
			{
				currentGuiTabIndex = i;
			}
			tabs[i] = new GuiTab
			{
				DataInt = tabIndex,
				Name = Lang.Get("tabname-" + orderedTabs[i].Code),
				PaddingTop = orderedTabs[i].PaddingTop
			};
			maxWidth = Math.Max(font.GetTextExtents(tabs[i].Name).Width + 1.0 + 2.0 * padding, maxWidth);
		}
		tabBoundsL.fixedWidth = Math.Max(tabBoundsL.fixedWidth, maxWidth);
		tabBoundsL.fixedX = 0.0 - tabBoundsL.fixedWidth;
		if (creativeInvDialog != null)
		{
			creativeInvDialog.Dispose();
		}
		GuiTab[] tabsL = tabs;
		GuiTab[] tabsR = null;
		if (tabs.Length > 16)
		{
			tabsL = tabs.Take(16).ToArray();
			tabsR = tabs.Skip(16).ToArray();
		}
		creativeInvDialog = capi.Gui.CreateCompo("inventory-creative", dialogBounds).AddShadedDialogBG(ElementBounds.Fill).AddDialogTitleBar(Lang.Get("Creative Inventory"), CloseIconPressed)
			.AddVerticalTabs(tabsL, tabBoundsL, OnTabClicked, "verticalTabs");
		if (tabsR != null)
		{
			creativeInvDialog.AddVerticalTabs(tabsR, tabBoundsR, delegate(int index, GuiTab tab)
			{
				OnTabClicked(index + 16, tabs[index + 16]);
			}, "verticalTabsR");
		}
		creativeInvDialog.AddInset(insetBounds, 3).BeginClip(creativeClippingBounds).AddItemSlotGrid(creativeInv, SendInvPacket, cols, fullGridBounds, "slotgrid")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarvalue, scrollbarBounds, "scrollbar")
			.AddTextInput(textInputBounds, OnTextChanged, null, "searchbox")
			.AddDynamicText("", CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Right), rightTextBounds, "searchResults");
		if (tabsR != null)
		{
			creativeInvDialog.GetVerticalTab("verticalTabsR").Right = true;
		}
		creativeInvDialog.Compose();
		creativeInvDialog.UnfocusOwnElements();
		creativeInvDialog.GetScrollbar("scrollbar").SetHeights((float)slotGridBounds.fixedHeight, (float)(fullGridBounds.fixedHeight + pad));
		creativeInvDialog.GetTextInput("searchbox").DeleteOnRefocusBackSpace = true;
		creativeInvDialog.GetTextInput("searchbox").SetPlaceHolderText(Lang.Get("Search..."));
		creativeInvDialog.GetVerticalTab((currentGuiTabIndex < 16) ? "verticalTabs" : "verticalTabsR").SetValue((currentGuiTabIndex < 16) ? currentGuiTabIndex : (currentGuiTabIndex - 16), triggerHandler: false);
		creativeInv.SetTab(currentTabIndex);
		update();
	}

	private void ComposeSurvivalInvDialog()
	{
		double elemToDlgPad = GuiStyle.ElementToDialogPadding;
		double pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
		int rows = (prevRows = (int)Math.Ceiling((float)backPackInv.Count / 6f));
		ElementBounds slotGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, pad, 6, 7).FixedGrow(2.0 * pad, 2.0 * pad);
		ElementBounds fullGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, 6, rows);
		ElementBounds insetBounds = slotGridBounds.ForkBoundingParent(3.0, 3.0, 3.0, 3.0);
		ElementBounds clippingBounds = slotGridBounds.CopyOffsetedSibling();
		clippingBounds.fixedHeight -= 3.0;
		ElementBounds gridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, 3, 3).FixedRightOf(insetBounds, 45.0);
		gridBounds.fixedY += 50.0;
		ElementBounds outputBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, 1, 1).FixedRightOf(insetBounds, 45.0).FixedUnder(gridBounds, 20.0);
		outputBounds.fixedX += pad + GuiElementPassiveItemSlot.unscaledSlotSize;
		ElementBounds dialogBounds = insetBounds.ForkBoundingParent(elemToDlgPad, elemToDlgPad + 30.0, elemToDlgPad + gridBounds.fixedWidth + 20.0, elemToDlgPad);
		if (capi.Settings.Bool["immersiveMouseMode"])
		{
			dialogBounds.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(-12.0, 0.0);
		}
		else
		{
			dialogBounds.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(20.0, 0.0);
		}
		ElementBounds scrollBarBounds = ElementStdBounds.VerticalScrollbar(insetBounds).WithParent(dialogBounds);
		scrollBarBounds.fixedOffsetX -= 2.0;
		scrollBarBounds.fixedWidth = 15.0;
		survivalInvDialog = capi.Gui.CreateCompo("inventory-backpack", dialogBounds).AddShadedDialogBG(ElementBounds.Fill).AddDialogTitleBar(Lang.Get("Inventory and Crafting"), CloseIconPressed)
			.AddVerticalScrollbar(OnNewScrollbarvalue, scrollBarBounds, "scrollbar")
			.AddInset(insetBounds, 3)
			.BeginClip(clippingBounds)
			.AddItemSlotGridExcl(backPackInv, SendInvPacket, 6, new int[4] { 0, 1, 2, 3 }, fullGridBounds, "slotgrid")
			.EndClip()
			.AddItemSlotGrid(craftingInv, SendInvPacket, 3, new int[9] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, gridBounds, "craftinggrid")
			.AddItemSlotGrid(craftingInv, SendInvPacket, 1, new int[1] { 9 }, outputBounds, "outputslot")
			.Compose();
		survivalInvDialog.GetScrollbar("scrollbar").SetHeights((float)slotGridBounds.fixedHeight, (float)(fullGridBounds.fixedHeight + pad));
	}

	private void BackPackInv_SlotModified(int t1)
	{
		if ((int)Math.Ceiling((float)backPackInv.Count / 6f) == prevRows)
		{
			return;
		}
		ComposeSurvivalInvDialog();
		Composers.Remove("maininventory");
		if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			if (creativeInvDialog == null)
			{
				ComposeCreativeInvDialog();
			}
			Composers["maininventory"] = creativeInvDialog ?? survivalInvDialog;
		}
		if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Guest || capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival)
		{
			Composers["maininventory"] = survivalInvDialog;
		}
	}

	private void update()
	{
		OnTextChanged(creativeInvDialog.GetTextInput("searchbox").GetText());
	}

	private void OnTabClicked(int index, GuiTab tab)
	{
		currentTabIndex = tab.DataInt;
		creativeInv.SetTab(tab.DataInt);
		creativeInvDialog.GetSlotGrid("slotgrid").DetermineAvailableSlots();
		GuiElementItemSlotGrid slotGrid = creativeInvDialog.GetSlotGrid("slotgrid");
		ElementBounds bounds = ElementStdBounds.SlotGrid(rows: (int)Math.Ceiling((float)slotGrid.renderedSlots.Count / (float)cols), alignment: EnumDialogArea.None, x: 0.0, y: 0.0, cols: cols);
		slotGrid.Bounds.fixedHeight = bounds.fixedHeight;
		update();
	}

	private void SendInvPacket(object packet)
	{
		capi.Network.SendPacketClient(packet);
	}

	private void CloseIconPressed()
	{
		TryClose();
	}

	private void OnNewScrollbarvalue(float value)
	{
		if (IsOpened())
		{
			if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
			{
				ElementBounds bounds = creativeInvDialog.GetSlotGrid("slotgrid").Bounds;
				bounds.fixedY = 10.0 - GuiElementItemSlotGridBase.unscaledSlotPadding - (double)value;
				bounds.CalcWorldBounds();
			}
			else if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival && survivalInvDialog != null)
			{
				ElementBounds bounds2 = survivalInvDialog.GetSlotGridExcl("slotgrid").Bounds;
				bounds2.fixedY = 10.0 - GuiElementItemSlotGridBase.unscaledSlotPadding - (double)value;
				bounds2.CalcWorldBounds();
			}
		}
	}

	private void OnTextChanged(string text)
	{
		if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			GuiElementItemSlotGrid slotgrid = creativeInvDialog.GetSlotGrid("slotgrid");
			slotgrid.FilterItemsBySearchText(text, creativeInv.CurrentTab.SearchCache, creativeInv.CurrentTab.SearchCacheNames);
			int rows = (int)Math.Ceiling((float)slotgrid.renderedSlots.Count / (float)cols);
			ElementBounds fullGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, cols, rows);
			creativeInvDialog.GetScrollbar("scrollbar").SetNewTotalHeight((float)(fullGridBounds.fixedHeight + 3.0));
			creativeInvDialog.GetScrollbar("scrollbar").SetScrollbarPosition(0);
			creativeInvDialog.GetDynamicText("searchResults").SetNewText(Lang.Get("creative-searchresults", slotgrid.renderedSlots.Count));
		}
	}

	public override bool TryOpen()
	{
		if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
		{
			return false;
		}
		return base.TryOpen();
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		ComposeGui(firstBuild: false);
		capi.World.Player.Entity.TryStopHandAction(forceStop: true, EnumItemUseCancelReason.OpenedGui);
		if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Guest || capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival)
		{
			if (craftingInv != null)
			{
				capi.Network.SendPacketClient((Packet_Client)craftingInv.Open(capi.World.Player));
			}
			if (backPackInv != null)
			{
				capi.Network.SendPacketClient((Packet_Client)backPackInv.Open(capi.World.Player));
			}
		}
		if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative && creativeInv != null)
		{
			capi.Network.SendPacketClient((Packet_Client)creativeInv.Open(capi.World.Player));
		}
	}

	public override void OnGuiClosed()
	{
		if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			creativeInvDialog?.GetTextInput("searchbox")?.SetValue("");
			creativeInvDialog?.GetSlotGrid("slotgrid")?.OnGuiClosed(capi);
			capi.Network.SendPacketClient((Packet_Client)creativeInv.Close(capi.World.Player));
			return;
		}
		if (craftingInv != null)
		{
			foreach (ItemSlot slot in craftingInv)
			{
				if (!slot.Empty)
				{
					ItemStackMoveOperation moveop = new ItemStackMoveOperation(capi.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge, slot.StackSize);
					moveop.ActingPlayer = capi.World.Player;
					object[] packets = capi.World.Player.InventoryManager.TryTransferAway(slot, ref moveop, onlyPlayerInventory: true);
					int i = 0;
					while (packets != null && i < packets.Length)
					{
						capi.Network.SendPacketClient((Packet_Client)packets[i]);
						i++;
					}
				}
			}
			capi.World.Player.InventoryManager.DropAllInventoryItems(craftingInv);
			capi.Network.SendPacketClient((Packet_Client)craftingInv.Close(capi.World.Player));
			survivalInvDialog.GetSlotGrid("craftinggrid").OnGuiClosed(capi);
			survivalInvDialog.GetSlotGrid("outputslot").OnGuiClosed(capi);
		}
		if (survivalInvDialog != null)
		{
			capi.Network.SendPacketClient((Packet_Client)backPackInv.Close(capi.World.Player));
			survivalInvDialog.GetSlotGridExcl("slotgrid").OnGuiClosed(capi);
		}
	}

	private void OnPlayerModeChanged()
	{
		if (IsOpened() && prevGameMode != capi.World.Player.WorldData.CurrentGameMode)
		{
			Composers.Remove("maininventory");
			ComposeGui(firstBuild: false);
			prevGameMode = capi.World.Player.WorldData.CurrentGameMode;
			if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
			{
				capi.Network.SendPacketClient((Packet_Client)creativeInv.Open(capi.World.Player));
				capi.Network.SendPacketClient((Packet_Client)backPackInv.Close(capi.World.Player));
			}
			if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Guest || capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival)
			{
				capi.Network.SendPacketClient((Packet_Client)backPackInv.Open(capi.World.Player));
				capi.Network.SendPacketClient((Packet_Client)creativeInv.Close(capi.World.Player));
			}
		}
	}

	internal override bool OnKeyCombinationToggle(KeyCombination viaKeyComb)
	{
		if (IsOpened() && creativeInv != null && creativeInvDialog != null)
		{
			GuiElementTextInput textInput = creativeInvDialog.GetTextInput("searchbox");
			if (textInput != null && textInput.HasFocus)
			{
				return false;
			}
		}
		return base.OnKeyCombinationToggle(viaKeyComb);
	}

	public override void OnMouseDown(MouseEvent args)
	{
		if (args.Handled)
		{
			return;
		}
		foreach (GuiComposer value in Composers.Values)
		{
			value.OnMouseDown(args);
			if (args.Handled)
			{
				return;
			}
		}
		if (!args.Handled && creativeInv != null && creativeClippingBounds != null && creativeClippingBounds.PointInside(args.X, args.Y) && capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			ItemSlot mouseCursorSlot = capi.World.Player.InventoryManager.GetOwnInventory("mouse")[0];
			if (!mouseCursorSlot.Empty)
			{
				ItemStackMoveOperation op = new ItemStackMoveOperation(capi.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge);
				op.ActingPlayer = capi.World.Player;
				op.CurrentPriority = EnumMergePriority.DirectMerge;
				int slotid = (mouseCursorSlot.Itemstack.Equals(capi.World, creativeInv[0].Itemstack, GlobalConstants.IgnoredStackAttributes) ? 1 : 0);
				object packet = creativeInv.ActivateSlot(slotid, mouseCursorSlot, ref op);
				if (packet != null)
				{
					SendInvPacket(packet);
				}
			}
		}
		if (args.Handled)
		{
			return;
		}
		foreach (GuiComposer value2 in Composers.Values)
		{
			if (value2.Bounds.PointInside(args.X, args.Y))
			{
				args.Handled = true;
			}
		}
	}

	public override bool CaptureAllInputs()
	{
		if (IsOpened())
		{
			return creativeInvDialog?.GetTextInput("searchbox").HasFocus ?? false;
		}
		return false;
	}

	public override void Dispose()
	{
		base.Dispose();
		creativeInvDialog?.Dispose();
		survivalInvDialog?.Dispose();
	}
}
