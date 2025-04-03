using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.Gui;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Vintagestory.Client.NoObf;
using Vintagestory.Client;

namespace DataDumper;

public class DataDumperModSystem : ModSystem
{
    ICoreClientAPI clientApi;
    private Harmony patcher;

    public override void StartClientSide(ICoreClientAPI api)
    {
        clientApi = api;
        Dump(new HotKeysDump(clientApi.Input.HotKeys));
        Dump(new LoadedGuisDump(clientApi.Gui.LoadedGuis));
        patcher = new Harmony(Mod.Info.ModID);
        patcher.PatchCategory(Mod.Info.ModID);
    }

    public override void Dispose()
    {
        base.Dispose();
        patcher?.UnpatchAll(Mod.Info.ModID);
    }

    private void Dump(object data)
    {
        clientApi.StoreModConfig(
            data,
            Path.Combine(
                $"{nameof(DataDumper)}",
                $"{data.GetType().Name}.json"));
    }
}

class HotKeyDump
{
    public string Name;
    public string Code;
    public string DefaultMapping;
    public bool TriggerOnUpAlso;

    public HotKeyDump(HotKey hotKey)
    {
        Name = hotKey.Name;
        Code = hotKey.Code;
        DefaultMapping = hotKey.DefaultMapping.ToString();
        TriggerOnUpAlso = hotKey.TriggerOnUpAlso;
    }
}

class HotKeysDump
{
    public List<HotKeyDump> HotKeys = new List<HotKeyDump>();

    public HotKeysDump(OrderedDictionary<string, HotKey> hotKeys)
    {
        foreach (var hotKey in  hotKeys)
        {
            HotKeys.Add(new HotKeyDump(hotKey.Value));
        }
    }
}

class GuiDialogDump
{
    public string DebugName;
    public string DialogType;
    public bool DisableMouseGrab;
    public double DrawOrder;
    public bool Focusable;
    public double InputOrder;
    public bool PrefersUngrabbedMouse;
    public string ToggleKeyCombinationCode;
    public bool UnregisterOnClose;
    public float ZSize;

    public GuiDialogDump(GuiDialog guiDialog)
    {
        DebugName = guiDialog.DebugName;
        DialogType = guiDialog.DialogType.ToString();
        DisableMouseGrab = guiDialog.DisableMouseGrab;
        DrawOrder = guiDialog.DrawOrder;
        Focusable = guiDialog.Focusable;
        InputOrder = guiDialog.InputOrder;
        PrefersUngrabbedMouse = guiDialog.PrefersUngrabbedMouse;
        ToggleKeyCombinationCode = guiDialog.ToggleKeyCombinationCode;
        UnregisterOnClose = guiDialog.UnregisterOnClose;
        ZSize = guiDialog.ZSize;
    }
}

class LoadedGuisDump
{
    public List<GuiDialogDump> LoadedGuis = new List<GuiDialogDump>();

    public LoadedGuisDump(List<GuiDialog> loadedGuis)
    {
        foreach (var loadedGui in loadedGuis)
        {
            LoadedGuis.Add(new GuiDialogDump(loadedGui));
        }
    }
}

class GuiComposerDump
{
    public bool Composed;
    public string DialogName;
    public bool Enabled;
    public string MouseOverCursor;
    public bool Tabbable;
    public ElementBounds Bounds;
    public ElementBounds CurParentBounds;
    public int CurrentElementKey;
    public string CurrentTabIndexElement;
    public string FirstTabbableElement;
    public string LastAddedElement;
    public ElementBounds LastAddedElementBounds;
    public int MaxTabIndex;

    public GuiComposerDump(GuiComposer guiComposer)
    {
        Composed = guiComposer.Composed;
        DialogName = guiComposer.DialogName;
        Enabled = guiComposer.Enabled;
        MouseOverCursor = guiComposer.MouseOverCursor;
        Tabbable = guiComposer.Tabbable;
        Bounds = guiComposer.Bounds;
        CurParentBounds = guiComposer.CurParentBounds;
        CurrentElementKey = guiComposer.CurrentElementKey;
        CurrentTabIndexElement = guiComposer.CurrentTabIndexElement.ToString();
        FirstTabbableElement = guiComposer.FirstTabbableElement.ToString();
        LastAddedElement = guiComposer.LastAddedElement.ToString();
        LastAddedElementBounds = guiComposer.LastAddedElementBounds;
        MaxTabIndex = guiComposer.MaxTabIndex;
    }
}

class ComposersDump
{
    Dictionary<string, GuiComposerDump> Composers =
        new Dictionary<string, GuiComposerDump>();

    public ComposersDump(Dictionary<string, GuiComposer> composers)
    {
        foreach (var composer in composers)
        {
            Composers.Add(composer.Key, new GuiComposerDump(composer.Value));
        }
    }
}

[HarmonyPatchCategory("datadumper")]
internal static class Patches
{
    // Using the most significant bit of int
    private const int DELETE_ACTION_FLAG = unchecked((int)0x80000000);

    static AccessTools.FieldRef<GuiCompositeSettings, int?> clickedItemIndexRef =
        AccessTools.FieldRefAccess<GuiCompositeSettings, int?>("clickedItemIndex");

    static AccessTools.FieldRef<GuiCompositeSettings, List<ConfigItem>> mousecontrolItemsRef =
        AccessTools.FieldRefAccess<GuiCompositeSettings, List<ConfigItem>>("mousecontrolItems");

    static AccessTools.FieldRef<GuiCompositeSettings, List<ConfigItem>> keycontrolItemsRef =
        AccessTools.FieldRefAccess<GuiCompositeSettings, List<ConfigItem>>("keycontrolItems");


    static AccessTools.FieldRef<GuiElementConfigList, ElementBounds> innerBoundsRef =
        AccessTools.FieldRefAccess<GuiElementConfigList, ElementBounds>("innerBounds");

    static AccessTools.FieldRef<GuiElementConfigList, ConfigItemClickDelegate> OnItemClickRef =
        AccessTools.FieldRefAccess<GuiElementConfigList, ConfigItemClickDelegate>("OnItemClick");

    static FastInvokeHandler shiftOrCtrlChangedInvoker =
        MethodInvoker.GetHandler(
            AccessTools.Method(
                typeof(GuiCompositeSettings), "ShiftOrCtrlChanged"));

    static FastInvokeHandler reLoadKeyCombinationsInvoker =
        MethodInvoker.GetHandler(
            AccessTools.Method(
                typeof(GuiCompositeSettings), "ReLoadKeyCombinations"));

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(GuiCompositeSettings), "OnMouseControlItemClick")]
    public static bool Before_GuiCompositeSettings_OnMouseControlItemClick(
        GuiCompositeSettings __instance, int index, int indexNoTitle)
    {
        return Before_GuiCompositeSettings_OnControlItemClick(
            __instance, index, indexNoTitle, mousecontrolItemsRef(__instance));
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(GuiCompositeSettings), "OnKeyControlItemClick")]
    public static bool Before_GuiCompositeSettings_OnKeyControlItemClick(
        GuiCompositeSettings __instance, int index, int indexNoTitle)
    {
        return Before_GuiCompositeSettings_OnControlItemClick(
            __instance, index, indexNoTitle, keycontrolItemsRef(__instance));
    }

    // The code in this function is based on the decompiled
    // GuiCompositeSettings.CompletedCapture() function in
    // Vintage Story v1.20.7. Should be updated for new versions accordingly.
    public static bool Before_GuiCompositeSettings_OnControlItemClick(
        GuiCompositeSettings __instance, int index, int indexNoTitle, List<ConfigItem> controlItems)
    {
        bool runOriginal = true;
        if ((index & DELETE_ACTION_FLAG) != 0)
        {
            // Do not run the original function
            runOriginal = false;

            // Umask the index
            int realIndex = index & ~DELETE_ACTION_FLAG;

            // Clear the displayed string on the GUI
            controlItems[realIndex].Value = "";

            // Retrieve the index known by the hotkey manager's list of hotkeys
            int hotkeyIndex = (int)controlItems[realIndex].Data;

            // Clone the original hotkey and set the KeyCode to Unknown
            string keyAtIndex =
                ScreenManager.hotkeyManager.HotKeys.GetKeyAtIndex(hotkeyIndex);
            HotKey keyCombClone =
                ScreenManager.hotkeyManager.HotKeys[keyAtIndex].Clone();
            keyCombClone.CurrentMapping.KeyCode = (int)GlKeys.Unknown;

            // Assign the modified hotkey clone to
            // the hotkey manager and client settings
            ScreenManager.hotkeyManager.HotKeys[keyAtIndex] = keyCombClone;
            ClientSettings.Inst.SetKeyMapping(keyAtIndex, keyCombClone.CurrentMapping);

            // Handle special cases for when sneak/sprint hotkeys are shared
            // with shift and ctrl key modifiers.
            // Ensures that ShiftOrCtrlChanged is called in this case to match
            // the original code.
            if (!ClientSettings.SeparateCtrl)
            {
                if (keyAtIndex == "sneak")
                {
                    ScreenManager.hotkeyManager.HotKeys["shift"].CurrentMapping =
                        keyCombClone.CurrentMapping;
                    shiftOrCtrlChangedInvoker(__instance);
                }
                if (keyAtIndex == "sprint")
                {
                    ScreenManager.hotkeyManager.HotKeys["ctrl"].CurrentMapping =
                        keyCombClone.CurrentMapping;
                    shiftOrCtrlChangedInvoker(__instance);
                }
            }

            // Call ShiftOrCtrlChanged if any of the following hotkeys
            // are being updated to match the original code.
            switch (keyAtIndex)
            {
                case "shift":
                case "ctrl":
                case "primarymouse":
                case "secondarymouse":
                case "toolmodeselect":
                    shiftOrCtrlChangedInvoker(__instance);
                    break;
            }

            // Call ReLoadKeyCombinations to match the original code.
            reLoadKeyCombinationsInvoker(__instance);
        }
        return runOriginal;
    }

    // The code in this function is based on the decompiled
    // GuiElementConfigList.OnMouseDownOnElement() function in
    // Vintage Story v1.20.7. Should be updated for new versions accordingly.
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(GuiElement), "OnKeyDown")]
    public static bool Before_GuiElementConfigList_OnKeyDown(
        GuiElement __instance, ICoreClientAPI api, KeyEvent args)
    {
        bool runOriginal = true;
        if (__instance is GuiElementConfigList)
        {
            GuiElementConfigList instance = __instance as GuiElementConfigList;
            bool success = true;
            int mouseX = api.Input.MouseX;
            int mouseY = api.Input.MouseY;
            {
                success = innerBoundsRef(instance).PointInside(mouseX, mouseY);
            }
            if (success == true)
            {
                if (args.KeyCode == (int)GlKeys.Delete)
                {
                    int index = 0;
                    int indexNoTitle = 0;
                    foreach (ConfigItem item in instance.items)
                    {
                        double absItemY =
                            item.posY + innerBoundsRef(instance).absY;
                        double deltaY = mouseY - absItemY;
                        if ((item.Type != EnumItemType.Title) &&
                            (deltaY > 0.0) &&
                            (deltaY < item.height))
                        {
                            var onItemClick = OnItemClickRef(instance);
                            if (onItemClick != null)
                            {
                                int flaggedIndex = index | DELETE_ACTION_FLAG;
                                onItemClick(flaggedIndex, indexNoTitle);
                                args.Handled = true;
                            }
                        }
                        index++;
                        if (item.Type != EnumItemType.Title)
                        {
                            indexNoTitle++;
                        }
                    }
                }
            }
        }
        return runOriginal;
    }
}