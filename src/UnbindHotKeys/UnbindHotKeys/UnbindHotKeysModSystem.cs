using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory;
using System;
using HarmonyLib;
using Vintagestory.Client.NoObf;
using Vintagestory.Client;
using System.Collections.Generic;

namespace UnbindHotKeys;

public class UnbindHotKeysModSystem : ModSystem
{
    private Harmony patcher;

    // This will be called during game startup, before any world is loaded
    public override void StartPre(ICoreAPI api)
    {
        if (api is ICoreClientAPI clientApi)
        {
            patcher = new Harmony(Mod.Info.ModID);
            patcher.PatchCategory(Mod.Info.ModID);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        patcher?.UnpatchAll(Mod.Info.ModID);
    }
}

[HarmonyPatchCategory("unbindhotkeys")]
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
                if ((args.KeyCode == (int)GlKeys.Delete) ||
                    (args.KeyCode == (int)GlKeys.D))
                {
                    int index = 0;
                    int indexNoTitle = 0;
                    for (int idx = 0; idx < instance.items.Count; ++idx)
                    {
                        ConfigItem item = instance.items[idx];
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
                            break;
                        }
                        ++index;
                        if (item.Type != EnumItemType.Title)
                        {
                            ++indexNoTitle;
                        }
                    }
                }
            }
        }
        return runOriginal;
    }
}