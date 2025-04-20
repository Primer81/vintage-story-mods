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
    static public bool DialogsWantMouseControlPrev;

    static private Harmony harmony;
    static private bool mouseControlToggledOn;

    private bool triggerOnUpAlsoOriginal = false;
    private bool mouseControlKeyIsPressed = false;

    public override double ExecuteOrder()
    {
        return 1.0;
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        ClientApi = api;
        // Initial static state defaults
        DialogsWantMouseControlPrev = false;
        mouseControlToggledOn = false;
        // Enable mouse toggle
        {
            triggerOnUpAlsoOriginal =
                ClientApi.Input.HotKeys["togglemousecontrol"].TriggerOnUpAlso;
            ClientApi.Input.HotKeys["togglemousecontrol"].TriggerOnUpAlso = true;
            ClientApi.Input.HotKeys["togglemousecontrol"].Handler +=
                OnToggleMouseControlHotkey;
        }
        // Apply harmony patches
        {
            harmony = new Harmony(Mod.Info.ModID);
            harmony.PatchCategory(Mod.Info.ModID);
            GuiDialogPatcher.PatchAllImplementations(harmony);
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

    public static void ToggleMouseControl()
    {
        mouseControlToggledOn = !mouseControlToggledOn;
    }

    public static bool IsMouseControlToggledOn()
    {
        return mouseControlToggledOn;
    }
}

[HarmonyPatchCategory("togglemousecontrol")]
internal static class Patches
{
    // Stop filter text input from taking focus on handbook open
    [HarmonyPostfix()]
    [HarmonyPatch(typeof(GuiDialogHandbook), "OnGuiOpened")]
    public static void After_GuiDialogHandbook_OnGuiOpened(
        GuiComposer ___overviewGui)
    {
        ___overviewGui.UnfocusOwnElements();
    }

    // Stops GUIs with scrollbars from intercepting the mouse wheel
    // events when the mouse is grabbed
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(GuiElementScrollbar), "OnMouseWheel")]
    public static bool Before_GuiElementScrollbar_OnMouseWheel(
        GuiElementScrollbar __instance,
        ICoreClientAPI api, MouseWheelEventArgs args)
    {
        bool runOriginal = !ToggleMouseControlModSystem.ClientApi.Input.MouseGrabbed;
        return runOriginal;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ClientMain), "UpdateFreeMouse")]
    public static bool Before_ClientMain_UpdateFreeMouse(
        ClientMain __instance,
        ref ICoreClientAPI ___api,
        ref bool ___mouseWorldInteractAnyway,
        ref bool ___exitToDisconnectScreen,
        ref bool ___exitToMainMenu)
    {
        bool dialogsWantMouseControl =
            ___api.Gui.OpenedGuis
                .Where((GuiDialog gui) => gui.DialogType == EnumDialogType.Dialog)
                .Any((GuiDialog dlg) => dlg.PrefersUngrabbedMouse) ||
            (__instance.DialogsOpened != 0);
        bool forceDisableMouseGrab =
            ___api.Gui.OpenedGuis.Any((GuiDialog gui) => gui.DisableMouseGrab) ||
            ___api.IsGamePaused ||
            (___api.World.Player.Entity.Alive == false);
        bool canGrabMouse =
            ScreenManager.Platform.IsFocused &&
            !___exitToDisconnectScreen &&
            !___exitToMainMenu &&
            __instance.BlocksReceivedAndLoaded &&
            !forceDisableMouseGrab;
        if (ClientSettings.ImmersiveMouseMode == false)
        {
            if (dialogsWantMouseControl != ToggleMouseControlModSystem.DialogsWantMouseControlPrev)
            {
                if (ToggleMouseControlModSystem.IsMouseControlToggledOn() != dialogsWantMouseControl)
                {
                    ToggleMouseControlModSystem.ToggleMouseControl();
                }
            }
        }
        if (canGrabMouse)
        {
            __instance.MouseGrabbed = !ToggleMouseControlModSystem.IsMouseControlToggledOn();
        }
        else
        {
            __instance.MouseGrabbed = false;
        }
        ___mouseWorldInteractAnyway = !__instance.MouseGrabbed;
        ToggleMouseControlModSystem.DialogsWantMouseControlPrev = dialogsWantMouseControl;
        return false;
    }
}

public static class GuiDialogPatcher
{
    public static void PatchAllImplementations(Harmony harmony)
    {
        // Get the parent method
        MethodInfo parentMethod = AccessTools.Method(typeof(GuiDialog), nameof(GuiDialog.OnMouseWheel));
        if (parentMethod == null)
        {
            ToggleMouseControlModSystem.ClientApi.Logger.Error(
                $"Failed to find parent method {nameof(GuiDialog)}.{nameof(GuiDialog.OnMouseWheel)}");
            return;
        }

        // Patch the parent method
        harmony.Patch(
            parentMethod,
            prefix: new HarmonyMethod(AccessTools.Method(typeof(GuiDialogPatcher), nameof(Before_GuiDialog_OnMouseWheel))),
            postfix: new HarmonyMethod(AccessTools.Method(typeof(GuiDialogPatcher), nameof(After_GuiDialog_OnMouseWheel)))
        );

        // Find all types in all loaded assemblies
        List<Type> derivedTypes = new List<Type>();
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (Type type in assembly.GetTypes())
                {
                    // Check if this type derives from GuiDialog
                    if (type != typeof(GuiDialog) && typeof(GuiDialog).IsAssignableFrom(type))
                    {
                        derivedTypes.Add(type);
                    }
                }
            }
            catch (Exception ex)
            {
                ToggleMouseControlModSystem.ClientApi.Logger.Error(
                    $"Error scanning assembly {assembly.FullName}: {ex.Message}");
            }
        }

        // Patch all derived implementations that override the method
        foreach (Type derivedType in derivedTypes)
        {
            // Look for the method in this specific type (not inherited)
            MethodInfo overriddenMethod = derivedType.GetMethod(nameof(GuiDialog.OnMouseWheel),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (overriddenMethod != null)
            {
                ToggleMouseControlModSystem.ClientApi.Logger.Debug(
                    $"Patching overridden method in {derivedType.Name}");
                harmony.Patch(
                    overriddenMethod,
                    prefix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(GuiDialogPatcher), nameof(Before_GuiDialog_OnMouseWheel))),
                    postfix: new HarmonyMethod(
                        AccessTools.Method(
                            typeof(GuiDialogPatcher), nameof(After_GuiDialog_OnMouseWheel)))
                );
            }
        }
    }

    public static bool Before_GuiDialog_OnMouseWheel(
        GuiDialog __instance)
    {
        bool runOriginal = true;
        if (ToggleMouseControlModSystem.ClientApi.Input.MouseGrabbed == true)
        {
            runOriginal = __instance is HudHotbar;
        }
        return runOriginal;
    }

    public static void After_GuiDialog_OnMouseWheel(GuiDialog __instance)
    {
    }
}