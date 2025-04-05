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
    static public bool MouseControlToggledOn { get; set; }

    static public ICoreClientAPI clientApi;

    private Harmony _harmony;

    private HotKey _mouseControlHotKey = null;
    private long _listenerId = 0;
    private bool _triggerOnUpAlsoOriginal = false;
    private bool _mouseControlKeyIsPressed = false;
    private MouseController _mouseController;

    public override void StartClientSide(ICoreClientAPI api)
    {
        clientApi = api;

        // Enable mouse controller
        _mouseControlHotKey = clientApi.Input.HotKeys["togglemousecontrol"];
        _mouseController = new MouseController(clientApi);
        Activate();

        // Patch all loaded gui dialogs such that the
        // PrefersUngrabbedMouse property always returns false.
        // This will ensure the mouse toggle applies universally even when
        // the map or handbook are open.
        _harmony = new Harmony(Mod.Info.ModID);
        _harmony.PatchCategory(Mod.Info.ModID);
    }

    public override void Dispose()
    {
        _harmony?.UnpatchAll(Mod.Info.ModID);
        base.Dispose();
    }

    private void Activate()
    {
        try
        {
            _triggerOnUpAlsoOriginal =
                _mouseControlHotKey.TriggerOnUpAlso;
            _mouseControlHotKey.TriggerOnUpAlso = true;
            _mouseControlHotKey.Handler += OnToggleMouseControlHotkey;
            _listenerId = clientApi.Event.RegisterGameTickListener(
                OnGameTickCheckMouseControlToggle, 5);
        }
        catch (NullReferenceException)
        {
            // _mouseControlHotKey was deleted; nothing to do
            // or
            // game quit; nothing to do
        }
    }

    // Only needed to deactivate during runtime if needed in future.
    // Currently unused as deactivation during Dispose() results in an
    // unnecessary NullReferenceException being thrown by:
    // - Dereferencing _mouseControlHotKey
    // - _clientApi.Event.UnregisterGameTickListener(_listenerId);
    private void Deactivate()
    {
        try
        {
            _mouseControlHotKey.TriggerOnUpAlso = _triggerOnUpAlsoOriginal;
            _mouseControlHotKey.Handler -= OnToggleMouseControlHotkey;
            clientApi.Event.UnregisterGameTickListener(_listenerId);
        }
        catch (NullReferenceException)
        {
            // _mouseControlHotKey was deleted; nothing to do
            // or
            // game quit; nothing to do
        }
    }

    private bool OnToggleMouseControlHotkey(KeyCombination keyComb)
    {
        bool isPressed = clientApi.Input.KeyboardKeyState[keyComb.KeyCode];
        if ((_mouseControlKeyIsPressed == false) && (isPressed == true))
        {
            _mouseControlKeyIsPressed = true;
            MouseControlToggledOn = !MouseControlToggledOn;
        }
        if ((_mouseControlKeyIsPressed == true) && (isPressed == false))
        {
            _mouseControlKeyIsPressed = false;
        }
        return true;
    }

    private void OnGameTickCheckMouseControlToggle(float dt)
    {
        if (MouseControlToggledOn == true)
        {
            _mouseController.UnlockMouse();
        }
        else
        {
            _mouseController.LockMouse();
        }
    }
}

public class MouseController: GuiDialog
// public class MouseController
{
    public ICoreClientAPI _clientApi;

    public override string ToggleKeyCombinationCode => "togglemousecontrol";
    public override bool PrefersUngrabbedMouse => true;
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
            // lock the mouse so that the next time escape key is pressed
            // the main menu should open regardless.
            ToggleMouseControlModSystem.MouseControlToggledOn = false;
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
    public static void Before_GuiDialogHandbook_OnGuiOpened(
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