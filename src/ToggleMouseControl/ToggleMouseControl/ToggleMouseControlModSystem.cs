using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory;
using Vintagestory.GameContent;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;
using System;
using System.Linq;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace ToggleMouseControl;

public class ToggleMouseControlModSystem : ModSystem
{
    static private ICoreClientAPI clientApi;
    static private Harmony harmony;
    static private MouseController mouseController;

    private bool triggerOnUpAlsoOriginal = false;
    private bool mouseControlKeyIsPressed = false;
    static private bool mouseControlToggledOn { get; set; }

    public override void StartClientSide(ICoreClientAPI api)
    {
        clientApi = api;
        // Enable mouse controller
        {
            mouseController = new MouseController(clientApi);
            triggerOnUpAlsoOriginal =
                clientApi.Input.HotKeys["togglemousecontrol"].TriggerOnUpAlso;
            clientApi.Input.HotKeys["togglemousecontrol"].TriggerOnUpAlso = true;
            clientApi.Input.HotKeys["togglemousecontrol"].Handler +=
                OnToggleMouseControlHotkey;
            clientApi.Event.RegisterGameTickListener(
                OnGameTickCheckMouseControlToggle, 5);
        }
        // Patch all loaded gui dialogs such that the
        // PrefersUngrabbedMouse property always returns false.
        // This will ensure the mouse toggle applies universally even when
        // the map or handbook are open.
        {
            harmony = new Harmony(Mod.Info.ModID);
            harmony.PatchCategory(Mod.Info.ModID);
        }
    }

    public override void Dispose()
    {
        // Unpatch if possible
        {
            harmony?.UnpatchAll(Mod.Info.ModID);
            harmony = null;
        }
        // Restore hotkey
        {
            clientApi.Input.HotKeys["togglemousecontrol"].TriggerOnUpAlso =
                triggerOnUpAlsoOriginal;
            clientApi.Input.HotKeys["togglemousecontrol"].Handler -=
                OnToggleMouseControlHotkey;
        }
        base.Dispose();
    }

    private bool OnToggleMouseControlHotkey(KeyCombination keyComb)
    {
        bool isPressed = clientApi.Input.KeyboardKeyState[keyComb.KeyCode];
        if ((mouseControlKeyIsPressed == false) && (isPressed == true))
        {
            mouseControlKeyIsPressed = true;
            ToggleMouseControl();
        }
        if ((mouseControlKeyIsPressed == true) && (isPressed == false))
        {
            mouseControlKeyIsPressed = false;
        }
        return true;
    }

    private void OnGameTickCheckMouseControlToggle(float dt)
    {
        if (mouseControlToggledOn == true)
        {
            mouseController.UnlockMouse();
        }
        else
        {
            mouseController.LockMouse();
        }
    }

    public static void ToggleMouseControl()
    {
        mouseControlToggledOn = !mouseControlToggledOn;
    }
}

public class MouseController: GuiDialog
// public class MouseController
{
    public ICoreClientAPI _clientApi;

    public override string ToggleKeyCombinationCode => "togglemousecontrol";
    public override bool PrefersUngrabbedMouse => true;
    // public override bool UnregisterOnClose => true;
    public override EnumDialogType DialogType => EnumDialogType.Dialog;
    public override bool Focusable => false;


    public MouseController(ICoreClientAPI capi) : base(capi)
    // public MouseController(ICoreClientAPI capi)
    {
        _clientApi = capi;
    }

    public void UnlockMouse()
    {
        _clientApi.Input.MouseWorldInteractAnyway = false;
        if (IsOpened() == false)
        {
            TryOpen();
        }
    }

    public void LockMouse()
    {
        _clientApi.Input.MouseWorldInteractAnyway = true;
        if (IsOpened() == true)
        {
            TryClose();
        }
    }

    public override bool ShouldReceiveKeyboardEvents()
    {
        return false;
    }

    public override bool ShouldReceiveMouseEvents()
    {
        return false;
    }

    public override bool ShouldReceiveRenderEvents()
    {
        return false;
    }

    public override bool OnEscapePressed()
    {
        var escapeMenu = _clientApi.Gui.LoadedGuis.FirstOrDefault(
            gui => gui.DebugName == "GuiDialogEscapeMenu");
        // Check if the object was found
        if (escapeMenu != null)
        {
            escapeMenu.TryOpen();
        }
        else
        {
            // This shouldn't happen... but if it does we should definitely
            // toggle the mouse control so that the next time escape key
            // is pressed the main menu should open regardless.
            ToggleMouseControlModSystem.ToggleMouseControl();
        }
        return false;
    }
}

[HarmonyPatchCategory("togglemousecontrol")]
internal static class Patches
{

    // Patches all classes derived from GuiDialog which do not override
    // the PrefersUngrabbedMouse property.
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(GuiDialog), "get_PrefersUngrabbedMouse")]
    public static bool Before_GuiDialog_get_PrefersUngrabbedMouse(
        ref bool __result)
    {
        __result = false;
        return false;
    }

    // Patches all classes derived from HudElement which do not override
    // the PrefersUngrabbedMouse property.
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(HudElement), "get_PrefersUngrabbedMouse")]
    public static bool Before_HudElement_get_PrefersUngrabbedMouse(
        ref bool __result)
    {
        __result = false;
        return false;
    }

    // Requires own patch
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(GuiDialogInventory), "get_PrefersUngrabbedMouse")]
    public static bool Before_GuiDialogInventory_get_PrefersUngrabbedMouse(
        ref bool __result)
    {
        __result = false;
        return false;
    }

    // Requires own patch
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(GuiDialogWorldMap), "get_PrefersUngrabbedMouse")]
    public static bool Before_GuiDialogWorldMap_get_PrefersUngrabbedMouse(
        ref bool __result)
    {
        __result = false;
        return false;
    }

    // Requires own patch
    static AccessTools.FieldRef<GuiDialogHandbook, GuiComposer> overviewGuiRef =
        AccessTools.FieldRefAccess<GuiDialogHandbook, GuiComposer>("overviewGui");
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(GuiDialogHandbook), "get_PrefersUngrabbedMouse")]
    public static bool Before_GuiDialogHandbook_get_PrefersUngrabbedMouse(
        ref bool __result)
    {
        __result = false;
        return false;
    }
    [HarmonyPostfix()]
    [HarmonyPatch(typeof(GuiDialogHandbook), "OnGuiOpened")]
    public static void After_GuiDialogHandbook_OnGuiOpened(
        GuiDialogHandbook __instance)
    {
        overviewGuiRef(__instance).UnfocusOwnElements();
    }

    // Covered by HudElement patch
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(HudElementBlockAndEntityInfo), "get_PrefersUngrabbedMouse")]
    // public static bool Before_HudElementBlockAndEntityInfo_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // Covered by GuiDialog patch
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogConfirmRemapping), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogConfirmRemapping_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // Inaccessible due to protection level
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogMacroEditor), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogMacroEditor_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // Covered by GuiDialog patch
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogTickProfiler), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogTickProfiler_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // Covered by GuiDialog patch
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogTransformEditor), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogTransformEditor_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // Covered by GuiDialog patch
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogDead), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogDead_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // Covered by GuiDialog patch
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogFirstlaunchInfo), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogFirstlaunchInfo_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // Inaccessible due to protection level
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogEscapeMenu), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogEscapeMenu_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // Inaccessible due to protection level
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogSelboxEditor), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogSelboxEditor_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // Inaccessible due to protection level
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogHollowTransform), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogHollowTransform_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }
}