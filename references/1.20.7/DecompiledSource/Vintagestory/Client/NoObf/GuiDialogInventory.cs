#region Assembly VintagestoryLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
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
        IPlayerInventoryManager inventoryManager = capi.World.Player.InventoryManager;
        creativeInv = (ITabbedInventory)inventoryManager.GetOwnInventory("creative");
        craftingInv = inventoryManager.GetOwnInventory("craftinggrid");
        backPackInv = inventoryManager.GetOwnInventory("backpack");
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
        //IL_0485: Unknown result type (might be due to invalid IL or missing references)
        //IL_048a: Unknown result type (might be due to invalid IL or missing references)
        if (creativeInv == null)
        {
            ScreenManager.Platform.Logger.Notification("Server did not send a creative inventory, so I won't display one");
            return;
        }

        double elementToDialogPadding = GuiStyle.ElementToDialogPadding;
        double unscaledSlotPadding = GuiElementItemSlotGridBase.unscaledSlotPadding;
        int rows = (int)Math.Ceiling((float)creativeInv.Count / (float)cols);
        ElementBounds elementBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, unscaledSlotPadding, unscaledSlotPadding, cols, 9).FixedGrow(2.0 * unscaledSlotPadding, 2.0 * unscaledSlotPadding);
        ElementBounds elementBounds2 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, cols, rows);
        creativeClippingBounds = elementBounds.ForkBoundingParent();
        creativeClippingBounds.Name = "clip";
        ElementBounds elementBounds3 = creativeClippingBounds.ForkBoundingParent(6.0, 3.0, 0.0, 3.0);
        elementBounds3.Name = "inset";
        ElementBounds elementBounds4 = elementBounds3.ForkBoundingParent(elementToDialogPadding, elementToDialogPadding + 70.0, elementToDialogPadding + 31.0, elementToDialogPadding).WithFixedAlignmentOffset(-3.0, -100.0).WithAlignment(EnumDialogArea.CenterBottom);
        ElementBounds bounds = ElementStdBounds.VerticalScrollbar(elementBounds3).WithParent(elementBounds4);
        ElementBounds bounds2 = ElementBounds.Fixed(elementToDialogPadding, 45.0, 250.0, 30.0);
        ElementBounds elementBounds5 = ElementBounds.Fixed(-130.0, 35.0, 130.0, 545.0);
        ElementBounds bounds3 = ElementBounds.Fixed(0.0, 35.0, 130.0, 545.0).FixedRightOf(elementBounds4).WithFixedAlignmentOffset(-4.0, 0.0);
        ElementBounds bounds4 = ElementBounds.Fixed(elementToDialogPadding, 45.0, 250.0, 30.0).WithAlignment(EnumDialogArea.RightFixed).WithFixedAlignmentOffset(-28.0 - elementToDialogPadding, 7.0);
        CreativeTabsConfig creativeTabsConfig = capi.Assets.TryGet("config/creativetabs.json").ToObject<CreativeTabsConfig>();
        IEnumerable<CreativeTab> tabs2 = creativeInv.CreativeTabs.Tabs;
        List<TabConfig> orderedTabs = new List<TabConfig>();
        foreach (CreativeTab tab2 in tabs2)
        {
            TabConfig tabConfig = creativeTabsConfig.TabConfigs.FirstOrDefault((TabConfig cfg) => cfg.Code == tab2.Code);
            if (tabConfig == null)
            {
                tabConfig = new TabConfig
                {
                    Code = tab2.Code,
                    ListOrder = 1.0
                };
            }

            int num = 0;
            for (int j = 0; j < orderedTabs.Count && orderedTabs[j].ListOrder < tabConfig.ListOrder; j++)
            {
                num++;
            }

            orderedTabs.Insert(num, tabConfig);
        }

        int num2 = 0;
        GuiTab[] tabs = new GuiTab[orderedTabs.Count];
        double val = 0.0;
        double num3 = GuiElement.scaled(3.0);
        CairoFont cairoFont = CairoFont.WhiteDetailText().WithFontSize(17f);
        int i;
        for (i = 0; i < orderedTabs.Count; i++)
        {
            int index2 = tabs2.FirstOrDefault((CreativeTab tab) => tab.Code == orderedTabs[i].Code).Index;
            if (index2 == currentTabIndex)
            {
                num2 = i;
            }

            tabs[i] = new GuiTab
            {
                DataInt = index2,
                Name = Lang.Get("tabname-" + orderedTabs[i].Code),
                PaddingTop = orderedTabs[i].PaddingTop
            };
            TextExtents textExtents = cairoFont.GetTextExtents(tabs[i].Name);
            val = Math.Max(((TextExtents)(ref textExtents)).Width + 1.0 + 2.0 * num3, val);
        }

        elementBounds5.fixedWidth = Math.Max(elementBounds5.fixedWidth, val);
        elementBounds5.fixedX = 0.0 - elementBounds5.fixedWidth;
        if (creativeInvDialog != null)
        {
            creativeInvDialog.Dispose();
        }

        GuiTab[] tabs3 = tabs;
        GuiTab[] array = null;
        if (tabs.Length > 16)
        {
            tabs3 = tabs.Take(16).ToArray();
            array = tabs.Skip(16).ToArray();
        }

        creativeInvDialog = capi.Gui.CreateCompo("inventory-creative", elementBounds4).AddShadedDialogBG(ElementBounds.Fill).AddDialogTitleBar(Lang.Get("Creative Inventory"), CloseIconPressed)
            .AddVerticalTabs(tabs3, elementBounds5, OnTabClicked, "verticalTabs");
        if (array != null)
        {
            creativeInvDialog.AddVerticalTabs(array, bounds3, delegate (int index, GuiTab tab)
            {
                OnTabClicked(index + 16, tabs[index + 16]);
            }, "verticalTabsR");
        }

        creativeInvDialog.AddInset(elementBounds3, 3).BeginClip(creativeClippingBounds).AddItemSlotGrid(creativeInv, SendInvPacket, cols, elementBounds2, "slotgrid")
            .EndClip()
            .AddVerticalScrollbar(OnNewScrollbarvalue, bounds, "scrollbar")
            .AddTextInput(bounds2, OnTextChanged, null, "searchbox")
            .AddDynamicText("", CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Right), bounds4, "searchResults");
        if (array != null)
        {
            creativeInvDialog.GetVerticalTab("verticalTabsR").Right = true;
        }

        creativeInvDialog.Compose();
        creativeInvDialog.UnfocusOwnElements();
        creativeInvDialog.GetScrollbar("scrollbar").SetHeights((float)elementBounds.fixedHeight, (float)(elementBounds2.fixedHeight + unscaledSlotPadding));
        creativeInvDialog.GetTextInput("searchbox").DeleteOnRefocusBackSpace = true;
        creativeInvDialog.GetTextInput("searchbox").SetPlaceHolderText(Lang.Get("Search..."));
        creativeInvDialog.GetVerticalTab((num2 < 16) ? "verticalTabs" : "verticalTabsR").SetValue((num2 < 16) ? num2 : (num2 - 16), triggerHandler: false);
        creativeInv.SetTab(currentTabIndex);
        update();
    }

    private void ComposeSurvivalInvDialog()
    {
        double elementToDialogPadding = GuiStyle.ElementToDialogPadding;
        double unscaledSlotPadding = GuiElementItemSlotGridBase.unscaledSlotPadding;
        int rows = (prevRows = (int)Math.Ceiling((float)backPackInv.Count / 6f));
        ElementBounds elementBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, unscaledSlotPadding, unscaledSlotPadding, 6, 7).FixedGrow(2.0 * unscaledSlotPadding, 2.0 * unscaledSlotPadding);
        ElementBounds elementBounds2 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, 6, rows);
        ElementBounds elementBounds3 = elementBounds.ForkBoundingParent(3.0, 3.0, 3.0, 3.0);
        ElementBounds elementBounds4 = elementBounds.CopyOffsetedSibling();
        elementBounds4.fixedHeight -= 3.0;
        ElementBounds elementBounds5 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, 3, 3).FixedRightOf(elementBounds3, 45.0);
        elementBounds5.fixedY += 50.0;
        ElementBounds elementBounds6 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, 1, 1).FixedRightOf(elementBounds3, 45.0).FixedUnder(elementBounds5, 20.0);
        elementBounds6.fixedX += unscaledSlotPadding + GuiElementPassiveItemSlot.unscaledSlotSize;
        ElementBounds elementBounds7 = elementBounds3.ForkBoundingParent(elementToDialogPadding, elementToDialogPadding + 30.0, elementToDialogPadding + elementBounds5.fixedWidth + 20.0, elementToDialogPadding);
        if (capi.Settings.Bool["immersiveMouseMode"])
        {
            elementBounds7.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(-12.0, 0.0);
        }
        else
        {
            elementBounds7.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(20.0, 0.0);
        }

        ElementBounds elementBounds8 = ElementStdBounds.VerticalScrollbar(elementBounds3).WithParent(elementBounds7);
        elementBounds8.fixedOffsetX -= 2.0;
        elementBounds8.fixedWidth = 15.0;
        survivalInvDialog = capi.Gui.CreateCompo("inventory-backpack", elementBounds7).AddShadedDialogBG(ElementBounds.Fill).AddDialogTitleBar(Lang.Get("Inventory and Crafting"), CloseIconPressed)
            .AddVerticalScrollbar(OnNewScrollbarvalue, elementBounds8, "scrollbar")
            .AddInset(elementBounds3, 3)
            .BeginClip(elementBounds4)
            .AddItemSlotGridExcl(backPackInv, SendInvPacket, 6, new int[4] { 0, 1, 2, 3 }, elementBounds2, "slotgrid")
            .EndClip()
            .AddItemSlotGrid(craftingInv, SendInvPacket, 3, new int[9] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, elementBounds5, "craftinggrid")
            .AddItemSlotGrid(craftingInv, SendInvPacket, 1, new int[1] { 9 }, elementBounds6, "outputslot")
            .Compose();
        survivalInvDialog.GetScrollbar("scrollbar").SetHeights((float)elementBounds.fixedHeight, (float)(elementBounds2.fixedHeight + unscaledSlotPadding));
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
        ElementBounds elementBounds = ElementStdBounds.SlotGrid(rows: (int)Math.Ceiling((float)slotGrid.renderedSlots.Count / (float)cols), alignment: EnumDialogArea.None, x: 0.0, y: 0.0, cols: cols);
        slotGrid.Bounds.fixedHeight = elementBounds.fixedHeight;
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
            GuiElementItemSlotGrid slotGrid = creativeInvDialog.GetSlotGrid("slotgrid");
            slotGrid.FilterItemsBySearchText(text, creativeInv.CurrentTab.SearchCache, creativeInv.CurrentTab.SearchCacheNames);
            int rows = (int)Math.Ceiling((float)slotGrid.renderedSlots.Count / (float)cols);
            ElementBounds elementBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, cols, rows);
            creativeInvDialog.GetScrollbar("scrollbar").SetNewTotalHeight((float)(elementBounds.fixedHeight + 3.0));
            creativeInvDialog.GetScrollbar("scrollbar").SetScrollbarPosition(0);
            creativeInvDialog.GetDynamicText("searchResults").SetNewText(Lang.Get("creative-searchresults", slotGrid.renderedSlots.Count));
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
            foreach (ItemSlot item in craftingInv)
            {
                if (!item.Empty)
                {
                    ItemStackMoveOperation op = new ItemStackMoveOperation(capi.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge, item.StackSize);
                    op.ActingPlayer = capi.World.Player;
                    object[] array = capi.World.Player.InventoryManager.TryTransferAway(item, ref op, onlyPlayerInventory: true);
                    int num = 0;
                    while (array != null && num < array.Length)
                    {
                        capi.Network.SendPacketClient((Packet_Client)array[num]);
                        num++;
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
            ItemSlot itemSlot = capi.World.Player.InventoryManager.GetOwnInventory("mouse")[0];
            if (!itemSlot.Empty)
            {
                ItemStackMoveOperation op = new ItemStackMoveOperation(capi.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge);
                op.ActingPlayer = capi.World.Player;
                op.CurrentPriority = EnumMergePriority.DirectMerge;
                int slotId = (itemSlot.Itemstack.Equals(capi.World, creativeInv[0].Itemstack, GlobalConstants.IgnoredStackAttributes) ? 1 : 0);
                object obj = creativeInv.ActivateSlot(slotId, itemSlot, ref op);
                if (obj != null)
                {
                    SendInvPacket(obj);
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
#if false // Decompilation log
'168' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Could not find by name: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'CommandLine, Version=2.9.1.0, Culture=neutral, PublicKeyToken=5a870481e358d379'
Could not find by name: 'CommandLine, Version=2.9.1.0, Culture=neutral, PublicKeyToken=5a870481e358d379'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Could not find by name: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Http.dll'
------------------
Resolve: 'Open.Nat, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f22a6a4582336c76'
Could not find by name: 'Open.Nat, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f22a6a4582336c76'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'Mono.Nat, Version=3.0.0.0, Culture=neutral, PublicKeyToken=6c9468a3c21bc6d1'
Could not find by name: 'Mono.Nat, Version=3.0.0.0, Culture=neutral, PublicKeyToken=6c9468a3c21bc6d1'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Net.Sockets, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Sockets, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Sockets.dll'
------------------
Resolve: 'Mono.Cecil, Version=0.11.5.0, Culture=neutral, PublicKeyToken=50cebf1cceb9d05e'
Could not find by name: 'Mono.Cecil, Version=0.11.5.0, Culture=neutral, PublicKeyToken=50cebf1cceb9d05e'
------------------
Resolve: 'Microsoft.CodeAnalysis, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'Microsoft.CodeAnalysis, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'Microsoft.CodeAnalysis.CSharp, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'Microsoft.CodeAnalysis.CSharp, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'System.Collections.Immutable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Immutable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\gwlar\.nuget\packages\system.collections.immutable\8.0.0\lib\net7.0\System.Collections.Immutable.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'ICSharpCode.SharpZipLib, Version=1.4.2.13, Culture=neutral, PublicKeyToken=1b03e6acf1164f73'
Could not find by name: 'ICSharpCode.SharpZipLib, Version=1.4.2.13, Culture=neutral, PublicKeyToken=1b03e6acf1164f73'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.IO.Compression.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Could not find by name: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: '0Harmony, Version=2.3.5.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: '0Harmony, Version=2.3.5.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\0Harmony.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Audio.OpenAL, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Audio.OpenAL, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Mathematics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Mathematics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Windowing.Common, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.Common, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'DnsClient, Version=1.7.0.0, Culture=neutral, PublicKeyToken=4574bb5573c51424'
Could not find by name: 'DnsClient, Version=1.7.0.0, Culture=neutral, PublicKeyToken=4574bb5573c51424'
------------------
Resolve: 'System.IO.Pipes, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.IO.Pipes, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.IO.Pipes.dll'
------------------
Resolve: 'OpenTK.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Diagnostics.FileVersionInfo, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.FileVersionInfo, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.FileVersionInfo.dll'
------------------
Resolve: 'System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.dll'
------------------
Resolve: 'csvorbis, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=eaa89a1626beb708'
Could not find by name: 'csvorbis, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=eaa89a1626beb708'
------------------
Resolve: 'csogg, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=cbfcc0aaeece6bdb'
Could not find by name: 'csogg, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=cbfcc0aaeece6bdb'
------------------
Resolve: 'System.Diagnostics.TraceSource, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.TraceSource, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.TraceSource.dll'
------------------
Resolve: 'xplatforminterface, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'xplatforminterface, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'Microsoft.Win32.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'Microsoft.Win32.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\Microsoft.Win32.Primitives.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
