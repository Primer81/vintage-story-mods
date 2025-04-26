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
using System.Reflection.Emit;
using Vintagestory.Common;

namespace ToggleMouseControl;

public static class GuiDialogPatcher
{
    public static bool ShouldRunOriginalGuiDialogInputHandlers(GuiDialog __instance)
    {
        bool runOriginal = true;
        if (ToggleMouseControlModSystem.ClientApi.Input.MouseGrabbed == true)
        {
            if (__instance != null)
            {
                // Use reflection to safely check the property
                try
                {
                    var prefersUngrabbedMouse = AccessTools.Property(
                        typeof(GuiDialog), "PrefersUngrabbedMouse")?
                        .GetValue(__instance);
                    runOriginal =
                        (prefersUngrabbedMouse == null) ||
                        ((bool)prefersUngrabbedMouse == false);
                }
                catch
                {
                    // Property access failed, default to true
                    ToggleMouseControlModSystem.ClientApi?.Logger?.Debug(
                        "Failed to access PrefersUngrabbedMouse property");
                }
            }
        }
        return runOriginal;
    }

    public static void PatchAllImplementations(Harmony harmony)
    {
        // Patch OnMouseWheel
        PatchAllImplementationsOfMethodWithPrefixAndPostfix(
            harmony,
            typeof(GuiDialog),
            nameof(GuiDialog.OnMouseWheel),
            nameof(Before_GuiDialog_OnMouseWheel),
            nameof(After_GuiDialog_OnMouseWheel));
        // Patch OnMouseMove
        PatchAllImplementationsOfMethodWithPrefixAndPostfix(
            harmony,
            typeof(GuiDialog),
            nameof(GuiDialog.OnMouseMove),
            nameof(Before_GuiDialog_OnMouseMove),
            nameof(After_GuiDialog_OnMouseMove));
        // Patch OnMouseDown/OnMouseUp
        PatchAllImplementationsOfMethodWithPrefixAndPostfix(
            harmony,
            typeof(GuiDialog),
            nameof(GuiDialog.OnMouseDown),
            nameof(Before_GuiDialog_OnMouseDown),
            nameof(After_GuiDialog_OnMouseDown));
        PatchAllImplementationsOfMethodWithPrefixAndPostfix(
            harmony,
            typeof(GuiDialog),
            nameof(GuiDialog.OnMouseUp),
            nameof(Before_GuiDialog_OnMouseUp),
            nameof(After_GuiDialog_OnMouseUp));
        // Patch OnKeyDown/OnKeyUp
        PatchAllImplementationsOfMethodWithPrefixAndPostfix(
            harmony,
            typeof(GuiDialog),
            nameof(GuiDialog.OnKeyDown),
            nameof(Before_GuiDialog_OnKeyDown),
            nameof(After_GuiDialog_OnKeyDown));
        PatchAllImplementationsOfMethodWithPrefixAndPostfix(
            harmony,
            typeof(GuiDialog),
            nameof(GuiDialog.OnKeyUp),
            nameof(Before_GuiDialog_OnKeyUp),
            nameof(After_GuiDialog_OnKeyUp));
    }

    public static void PatchAllImplementationsOfMethodWithPrefixAndPostfix(
        Harmony harmony,
        Type methodParentClass,
        string method,
        string prefix,
        string postfix)
    {
        // This class's type!
        Type thisClassType = MethodBase.GetCurrentMethod().DeclaringType;
        Patches.PatchAllImplementationsOfMethodWithPrefixAndPostfix(
            harmony,
            methodParentClass,
            method,
            thisClassType,
            prefix,
            postfix);
    }

    public static bool Before_GuiDialog_OnMouseWheel(
        GuiDialog __instance)
    {
        return ShouldRunOriginalGuiDialogInputHandlers(__instance);
    }
    public static void After_GuiDialog_OnMouseWheel(GuiDialog __instance)
    {
    }

    public static bool Before_GuiDialog_OnMouseMove(
        GuiDialog __instance,
        ref MouseEvent args,
        GuiDialog.DlgComposers ___Composers)
    {
        bool runOriginal = ShouldRunOriginalGuiDialogInputHandlers(__instance);
        if (runOriginal == false)
        {
            runOriginal = true;
            {
                int guiDialogOriginX = 0;
                int guiDialogOriginY = 0;
                GuiComposer[] composers = ___Composers.ToArray();
                for (int i = 0; i < composers.Length; i++)
                {
                    guiDialogOriginX = Math.Min(
                        guiDialogOriginX, (int)composers[i].Bounds.absFixedX);
                    guiDialogOriginY = Math.Min(
                        guiDialogOriginY, (int)composers[i].Bounds.absFixedY);
                }
                args = new MouseEvent(
                    (int)(guiDialogOriginX),
                    (int)(guiDialogOriginY),
                    0,
                    0,
                    EnumMouseButton.None,
                    0
                );
            }
        }
        return runOriginal;
    }
    public static void After_GuiDialog_OnMouseMove(GuiDialog __instance)
    {
    }

    public static bool Before_GuiDialog_OnMouseDown(
        GuiDialog __instance)
    {
        return ShouldRunOriginalGuiDialogInputHandlers(__instance);
    }
    public static void After_GuiDialog_OnMouseDown(GuiDialog __instance)
    {
    }

    public static bool Before_GuiDialog_OnMouseUp(
        GuiDialog __instance)
    {
        return ShouldRunOriginalGuiDialogInputHandlers(__instance);
    }
    public static void After_GuiDialog_OnMouseUp(GuiDialog __instance)
    {
    }

    public static bool Before_GuiDialog_OnKeyDown(
        GuiDialog __instance)
    {
        return ShouldRunOriginalGuiDialogInputHandlers(__instance);
    }
    public static void After_GuiDialog_OnKeyDown(GuiDialog __instance)
    {
    }

    public static bool Before_GuiDialog_OnKeyUp(
        GuiDialog __instance)
    {
        return ShouldRunOriginalGuiDialogInputHandlers(__instance);
    }
    public static void After_GuiDialog_OnKeyUp(GuiDialog __instance)
    {
    }
}