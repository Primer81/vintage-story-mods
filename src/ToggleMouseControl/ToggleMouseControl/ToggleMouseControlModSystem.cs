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
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;

namespace ToggleMouseControl;

public class ToggleMouseControlModSystem : ModSystem
{
    static public ICoreClientAPI ClientApi;
    static private Dictionary<string, bool> originalPrefersMouseUngrabbedSettings;
    static private Harmony harmony;
    static private MouseController mouseController;

    private bool triggerOnUpAlsoOriginal = false;
    private bool mouseControlKeyIsPressed = false;
    static private bool mouseControlToggledOn = false;
    static private bool immersiveMouseModeEnabled;
    // static private bool originalImmersiveMouseModeSetting;
    static public bool DialogsWantMouseControlPrev;
    static public bool DialogsDisableMouseGrabPrev;

    public override double ExecuteOrder()
    {
        return 1.0;
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        ClientApi = api;
        // Initialize static members
        DialogsWantMouseControlPrev = false;
        DialogsDisableMouseGrabPrev = false;
        // Collect the original PreferredMouseUngrabbedSetting settings for each dialog
        originalPrefersMouseUngrabbedSettings = new Dictionary<string, bool>();
        foreach (GuiDialog guiDialog in ClientApi.Gui.LoadedGuis)
        {
            if (!originalPrefersMouseUngrabbedSettings
                .ContainsKey(guiDialog.DebugName))
            {
                AddOriginalPrefersMouseUngrabbedSetting(
                    guiDialog.DebugName, guiDialog.PrefersUngrabbedMouse);
            }
        }
        // Enable mouse controller
        {
            mouseController = new MouseController(ClientApi);
            triggerOnUpAlsoOriginal =
                ClientApi.Input.HotKeys["togglemousecontrol"].TriggerOnUpAlso;
            ClientApi.Input.HotKeys["togglemousecontrol"].TriggerOnUpAlso = true;
            ClientApi.Input.HotKeys["togglemousecontrol"].Handler +=
                OnToggleMouseControlHotkey;
            ClientApi.Event.RegisterGameTickListener(
                OnGameTickCheckMouseControlToggle, 5);
        }
        // Ensure the internal immersive mouse mode is enabled before patching!
        // originalImmersiveMouseModeSetting = ClientSettings.ImmersiveMouseMode;
        // immersiveMouseModeEnabled = originalImmersiveMouseModeSetting;
        // ClientSettings.ImmersiveMouseMode = true;
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
        // Preserve immersive mouse mode setting
        // ClientSettings.ImmersiveMouseMode = immersiveMouseModeEnabled;
        // Restore hotkey
        {
            ClientApi.Input.HotKeys["togglemousecontrol"].TriggerOnUpAlso =
                triggerOnUpAlsoOriginal;
            ClientApi.Input.HotKeys["togglemousecontrol"].Handler -=
                OnToggleMouseControlHotkey;
        }
        base.Dispose();
    }

    private bool OnToggleMouseControlHotkey(KeyCombination keyComb)
    {
        bool isPressed = ClientApi.Input.KeyboardKeyState[keyComb.KeyCode];
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

    private static void OnGameTickCheckMouseControlToggle(float dt)
    {
        OnGameTickCheckMouseControlToggle();
    }

    private static void OnGameTickCheckMouseControlToggle()
    {
        // Check open dialogs for when immersive mouse mode is OFF
        CheckIfMouseControlShouldAutoToggle();
        // Normal routine
        CheckMouseControlToggle();
    }

    private static void CheckIfMouseControlShouldAutoToggle()
    {
        // bool dialogsWantsMouseControl = ClientApi.Gui.OpenedGuis
        //     .Where((GuiDialog gui) => gui.DialogType == EnumDialogType.Dialog)
        //     .Any((GuiDialog dlg) =>
        //         originalPrefersMouseUngrabbedSettings
        //             .GetValueOrDefault(dlg.DebugName, false));
        // if (IsImmersiveMouseModeEnabled() == false)
        // {
        //     if (dialogsWantsMouseControl != dialogsWantsMouseControlPrev)
        //     {
        //         if (IsMouseControlToggledOn() != dialogsWantsMouseControl)
        //         {
        //             ToggleMouseControl();
        //         }
        //     }
        // }
        // dialogsWantsMouseControlPrev = dialogsWantsMouseControl;
    }

    private static void CheckMouseControlToggle()
    {
        if (IsMouseControlToggledOn() == true)
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
        CheckMouseControlToggle();
    }

    public static bool IsMouseControlToggledOn()
    {
        return mouseControlToggledOn;
    }

    public static bool IsImmersiveMouseModeEnabled()
    {
        return immersiveMouseModeEnabled;
    }

    public static void SetImmersiveMouseMode(bool enabled)
    {
        immersiveMouseModeEnabled = enabled;
    }

    public static void AddOriginalPrefersMouseUngrabbedSetting(
        string guiDialogDebugName, bool setting)
    {
        if (!originalPrefersMouseUngrabbedSettings
            .ContainsKey(guiDialogDebugName))
        {
            originalPrefersMouseUngrabbedSettings
                .Add(guiDialogDebugName, setting);
        }
    }
}

public class MouseController: GuiDialog
// public class MouseController
{
    public ICoreClientAPI clientApi;

    public override string ToggleKeyCombinationCode => "togglemousecontrol";
    public override bool PrefersUngrabbedMouse => true;
    // public override bool UnregisterOnClose => true;
    public override EnumDialogType DialogType => EnumDialogType.Dialog;
    public override bool Focusable => false;


    public MouseController(ICoreClientAPI capi) : base(capi)
    // public MouseController(ICoreClientAPI capi)
    {
        clientApi = capi;
    }

    public void UnlockMouse()
    {
        // clientApi.Input.MouseWorldInteractAnyway = false;
        // clientApi.Input.MouseWorldInteractAnyway = true;
        // if (IsOpened() == false)
        // {
        //     TryOpen();
        // }
    }

    public void LockMouse()
    {
        // clientApi.Input.MouseWorldInteractAnyway = true;
        // clientApi.Input.MouseWorldInteractAnyway = false;
        // if (IsOpened() == true)
        // {
        //     TryClose();
        // }
        // Fix focus to hotbar when mouse is locked.
        // Stops the GUIs with scrollbars from taking over the mouse wheel
        // such as the handbook.
        {
            // GuiDialog hotbar = clientApi.Gui.LoadedGuis.FirstOrDefault(
            //     gui => gui.DebugName == "HudHotbar");
            // if (hotbar != null)
            // {
            //     clientApi.Gui.RequestFocus(hotbar);
            // }
            // ScreenManager.GuiComposers.UnfocusElements();
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
        bool guiClosed;
        // var escapeMenu = clientApi.Gui.LoadedGuis.FirstOrDefault(
        //     gui => gui.DebugName == "GuiDialogEscapeMenu");
        // Check if the object was found
        // if (escapeMenu != null)
        // {
        //     escapeMenu.TryOpen();
        //     guiClosed = false;
        // }
        // else
        // {
            // Toggle mouse control so that the next time escape key
            // is pressed, the main menu should open.
            ToggleMouseControlModSystem.ToggleMouseControl();
            // Try to close the dialog just in case 😉
            if (IsOpened() == true)
            {
                TryClose();
            }
            guiClosed = true;
        // }
        return guiClosed;
    }
}

[HarmonyPatchCategory("togglemousecontrol")]
internal static class Patches
{
    // // Patches all classes derived from GuiDialog which do not override
    // // the PrefersUngrabbedMouse property.
    // [HarmonyPostfix()]
    // [HarmonyPatch(typeof(GuiDialog), "get_PrefersUngrabbedMouse")]
    // public static void Before_GuiDialog_get_PrefersUngrabbedMouse(
    //     GuiDialog __instance, ref bool __result)
    // {
    //     // ToggleMouseControlModSystem.AddOriginalPrefersMouseUngrabbedSetting(
    //     //     __instance.DebugName, __result);
    //     __result = ToggleMouseControlModSystem.IsMouseControlToggledOn();
    // }

    // // Patches all classes derived from HudElement which do not override
    // // the PrefersUngrabbedMouse property.
    // [HarmonyPostfix()]
    // [HarmonyPatch(typeof(HudElement), "get_PrefersUngrabbedMouse")]
    // public static void Before_HudElement_get_PrefersUngrabbedMouse(
    //     GuiDialog __instance, ref bool __result)
    // {
    //     // ToggleMouseControlModSystem.AddOriginalPrefersMouseUngrabbedSetting(
    //     //     __instance.DebugName, __result);
    //     __result = ToggleMouseControlModSystem.IsMouseControlToggledOn();
    // }

    // // Requires own patch
    // [HarmonyPostfix()]
    // [HarmonyPatch(typeof(GuiDialogInventory), "get_PrefersUngrabbedMouse")]
    // public static void Before_GuiDialogInventory_get_PrefersUngrabbedMouse(
    //     GuiDialog __instance, ref bool __result)
    // {
    //     // ToggleMouseControlModSystem.AddOriginalPrefersMouseUngrabbedSetting(
    //     //     __instance.DebugName, __result);
    //     __result = ToggleMouseControlModSystem.IsMouseControlToggledOn();
    // }

    // // Requires own patch
    // [HarmonyPostfix()]
    // [HarmonyPatch(typeof(GuiDialogWorldMap), "get_PrefersUngrabbedMouse")]
    // public static void Before_GuiDialogWorldMap_get_PrefersUngrabbedMouse(
    //     GuiDialog __instance, ref bool __result)
    // {
    //     // ToggleMouseControlModSystem.AddOriginalPrefersMouseUngrabbedSetting(
    //     //     __instance.DebugName, __result);
    //     __result = ToggleMouseControlModSystem.IsMouseControlToggledOn();
    // }

    // Requires own patch

    // static AccessTools.FieldRef<GuiDialogHandbook, GuiComposer> overviewGuiRef =
    //     AccessTools.FieldRefAccess<GuiDialogHandbook, GuiComposer>("overviewGui");

    // [HarmonyPostfix()]
    // [HarmonyPatch(typeof(GuiDialogHandbook), "get_PrefersUngrabbedMouse")]
    // public static void Before_GuiDialogHandbook_get_PrefersUngrabbedMouse(
    //     GuiDialog __instance, ref bool __result)
    // {
    //     // ToggleMouseControlModSystem.AddOriginalPrefersMouseUngrabbedSetting(
    //     //     __instance.DebugName, __result);
    //     __result = ToggleMouseControlModSystem.IsMouseControlToggledOn();
    // }

    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogHandbook), "OnGuiOpened")]
    // public static bool Before_GuiDialogHandbook_OnGuiOpened(
    //     GuiDialogHandbook __instance)
    // {
    //     bool runOriginal = true;
    //     if (ToggleMouseControlModSystem.IsImmersiveMouseModeEnabled() == true)
    //     {
    //         // It's not wrong that no positive negativity never hurt nobody
    //         if ((ToggleMouseControlModSystem.ClientApi.Settings.Bool["noHandbookPause"] == false) &&
    //             (ToggleMouseControlModSystem.IsMouseControlToggledOn() == false))
    //         {
    //             ToggleMouseControlModSystem.ToggleMouseControl();
    //         }
    //     }
    //     return runOriginal;
    // }

    // Stop filter text input from taking focus on handbook open
    [HarmonyPostfix()]
    [HarmonyPatch(typeof(GuiDialogHandbook), "OnGuiOpened")]
    public static void After_GuiDialogHandbook_OnGuiOpened(
        GuiComposer ___overviewGui)
    {
        ___overviewGui.UnfocusOwnElements();
    }

    // [HarmonyPostfix()]
    // [HarmonyPatch(typeof(GuiDialogHandbook), "OnGuiClosed")]
    // public static void After_GuiDialogHandbook_OnGuiClosed(
    //     GuiDialogHandbook __instance)
    // {
    //     // if (clientApi.Settings.Bool["immersiveMouseMode"] == true)
    //     {
    //         // It's not wrong that no positive negativity never hurt nobody
    //         if ((ToggleMouseControlModSystem.ClientApi.Settings.Bool["noHandbookPause"] == false) &&
    //             (ToggleMouseControlModSystem.IsMouseControlToggledOn() == true))
    //         {
    //             ToggleMouseControlModSystem.ToggleMouseControl();
    //         }
    //     }
    // }

    // Stops GUIs with scrollbars from intercepting the mouse wheel
    // events when the mouse is grabbed
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(GuiElementScrollbar), "OnMouseWheel")]
    public static bool Before_GuiElementScrollbar_OnMouseWheel(
        GuiElementScrollbar __instance,
        ICoreClientAPI api, MouseWheelEventArgs args)
    {
        // bool runOriginal;
        bool runOriginal = !ToggleMouseControlModSystem.ClientApi.Input.MouseGrabbed;
        // if (ToggleMouseControlModSystem.ClientApi.Settings.Bool["immersiveMouseMode"] == true)
        // {
        //     // Only run the original function if mouse control is toggled ON
        //     // to closely match the expected behavior.
        //     runOriginal = ToggleMouseControlModSystem.IsMouseControlToggledOn();
        // }
        // else
        // {
        // //     // TODO: Temporary patch to fix B02. Will need to be removed after
        // //     // fixing B03!
        // //     runOriginal = true;
        //     // Only run the original function if mouse control is toggled OFF
        //     // to closely match the expected behavior.
        //     runOriginal = !ToggleMouseControlModSystem.IsMouseControlToggledOn();
        // }
        return runOriginal;
    }

    // // Covered by HudElement patch
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(HudElementBlockAndEntityInfo), "get_PrefersUngrabbedMouse")]
    // public static bool Before_HudElementBlockAndEntityInfo_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // // Covered by GuiDialog patch
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogConfirmRemapping), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogConfirmRemapping_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // // Inaccessible due to protection level
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogMacroEditor), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogMacroEditor_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // // Covered by GuiDialog patch
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogTickProfiler), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogTickProfiler_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // // Covered by GuiDialog patch
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
    // But needs special handling to ensure mouse control is granted!
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogDead), "OnGuiOpened")]
    // public static bool Before_GuiDialogDead_OnGuiOpened(
    //     GuiDialogDead __instance)
    // {
    //     bool runOriginal = true;
    //     if (ToggleMouseControlModSystem.IsImmersiveMouseModeEnabled() == true)
    //     {
    //         if (ToggleMouseControlModSystem.IsMouseControlToggledOn() == false)
    //         {
    //             ToggleMouseControlModSystem.ToggleMouseControl();
    //         }
    //     }
    //     return runOriginal;
    // }
    // [HarmonyPostfix()]
    // [HarmonyPatch(typeof(GuiDialogDead), "OnRespawn")]
    // public static void After_GuiDialogDead_OnRespawn(
    //     GuiDialogDead __instance)
    // {
    //     // if (ToggleMouseControlModSystem.ClientApi.Settings.Bool["immersiveMouseMode"] == true)
    //     {
    //         // It's not wrong that no positive negativity never hurt nobody
    //         if (ToggleMouseControlModSystem.IsMouseControlToggledOn() == true)
    //         {
    //             ToggleMouseControlModSystem.ToggleMouseControl();
    //         }
    //     }
    // }

    // // Covered by GuiDialog patch
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogFirstlaunchInfo), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogFirstlaunchInfo_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // // Inaccessible due to protection level
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogEscapeMenu), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogEscapeMenu_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // // Inaccessible due to protection level
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogSelboxEditor), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogSelboxEditor_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // // Inaccessible due to protection level
    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(GuiDialogHollowTransform), "get_PrefersUngrabbedMouse")]
    // public static bool Before_GuiDialogHollowTransform_get_PrefersUngrabbedMouse(
    //     ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(ClientSettings), "get_ImmersiveMouseMode")]
    // public static bool Before_ClientSettings_get_ImmersiveMouseMode(
    //     ref bool __result)
    // {
    //     if (ToggleMouseControlModSystem.ClientApi.IsGamePaused)
    //     {
    //         __result = ToggleMouseControlModSystem.IsImmersiveMouseModeEnabled();
    //     }
    //     else
    //     {
    //         __result = true;
    //     }
    //     return false;
    // }

    // [HarmonyPrefix()]
    // [HarmonyPatch(typeof(ClientSettings), "set_ImmersiveMouseMode")]
    // public static bool Before_ClientSettings_set_ImmersiveMouseMode(
    //     bool value)
    // {
    //     bool runOriginal = false;
    //     ToggleMouseControlModSystem.SetImmersiveMouseMode(value);
    //     return runOriginal;
    // }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ClientMain), "UpdateFreeMouse")]
    public static bool Before_ClientMain_UpdateFreeMouse(
        ClientMain __instance,
        ref ICoreClientAPI ___api,
        ref bool ___mouseWorldInteractAnyway,
        ref bool ___exitToDisconnectScreen,
        ref bool ___exitToMainMenu)
    {
        // int altKey = ScreenManager.hotkeyManager.HotKeys["togglemousecontrol"].CurrentMapping.KeyCode;
        // bool isAltKeyDown = ToggleMouseControlModSystem.IsMouseControlToggledOn();
        bool dialogsWantMouseControl =
            ___api.Gui.OpenedGuis
                .Where((GuiDialog gui) => gui.DialogType == EnumDialogType.Dialog)
                .Any((GuiDialog dlg) => dlg.PrefersUngrabbedMouse) &&
            ClientSettings.ImmersiveMouseMode == false;
        bool dialogsDisableMouseGrab =
            ___api.Gui.OpenedGuis.Any((GuiDialog gui) => gui.DisableMouseGrab) ||
            ___api.IsGamePaused &&
            (___api.World.Player.Entity.Alive == false);
            // ((ToggleMouseControlModSystem.ClientApi.Settings.Bool["noHandbookPause"] == true) ||
            //     ___api.Gui.OpenedGuis.Any((GuiDialog gui) => gui is GuiDialogHandbook));

        bool mouseGrabbed =
            ScreenManager.Platform.IsFocused &&
            !___exitToDisconnectScreen &&
            !___exitToMainMenu &&
            __instance.BlocksReceivedAndLoaded &&
            !dialogsDisableMouseGrab &&
            !dialogsWantMouseControl;
        if (mouseGrabbed)
        {
            bool someDialogIsOpen = __instance.DialogsOpened > 0;
            if (someDialogIsOpen)
            {
                // who cares?
            }
            // if (ClientSettings.ImmersiveMouseMode == false)
            // {
                // if (dialogsWantMouseControl != ToggleMouseControlModSystem.DialogsWantMouseControlPrev)
                // {
                //     ToggleMouseControlModSystem.ToggleMouseControl();
                // }
            // }
            // if (dialogsDisableMouseGrab != ToggleMouseControlModSystem.DialogsDisableMouseGrabPrev)
            // {
                // if (ToggleMouseControlModSystem.IsMouseControlToggledOn() == false)
                // {
                    // ToggleMouseControlModSystem.ToggleMouseControl();
                // }
            // }
            // if (dialogWantsMouseGrabbed != ToggleMouseControlModSystem.DialogsWantsMouseGrabbedPrev)
            // {
            //     if (ToggleMouseControlModSystem.IsMouseControlToggledOn() == dialogWantsMouseGrabbed)
            //     {
            //         ToggleMouseControlModSystem.ToggleMouseControl();
            //     }
            // }
            __instance.MouseGrabbed = !ToggleMouseControlModSystem.IsMouseControlToggledOn();
        }
        else
        {
            __instance.MouseGrabbed = false;
        }
        ___mouseWorldInteractAnyway = !__instance.MouseGrabbed; // && !dialogsWantMouseControl;
        ToggleMouseControlModSystem.DialogsWantMouseControlPrev = dialogsWantMouseControl;
        ToggleMouseControlModSystem.DialogsDisableMouseGrabPrev = dialogsDisableMouseGrab;
        return false;
    }
}