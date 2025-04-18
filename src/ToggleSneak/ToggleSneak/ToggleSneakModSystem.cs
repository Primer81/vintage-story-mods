using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory;
using Vintagestory.Client.NoObf;
using System;
using HarmonyLib;

namespace ToggleSneak;

public class ToggleSneakModSystem : ModSystem
{
    static public ICoreClientAPI ClientApi;

    static private Harmony harmony;
    static private bool sneakToggledOn;

    private bool triggerOnUpAlsoOriginal = false;

    public override void StartClientSide(ICoreClientAPI api)
    {
        ClientApi = api;
        // Initial static state defaults
        sneakToggledOn = false;
        // Enable sneak toggle
        {
            triggerOnUpAlsoOriginal =
                ClientApi.Input.HotKeys["sneak"].TriggerOnUpAlso;
            ClientApi.Input.HotKeys["sneak"].TriggerOnUpAlso = false;
        }
        // Apply harmony patches
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
            ClientApi.Input.HotKeys["sneak"].TriggerOnUpAlso =
                triggerOnUpAlsoOriginal;
        }
        base.Dispose();
    }

    public static void ToggleSneak()
    {
        sneakToggledOn = !sneakToggledOn;
    }

    public static bool IsSneakToggledOn()
    {
        return sneakToggledOn;
    }

    public static int GetSneakKeyCode()
    {
        return ClientApi.Input.HotKeys["sneak"].CurrentMapping.KeyCode;
    }
}

[HarmonyPatchCategory("togglesneak")]
internal static class Patches
{
    private const int ACTION_FLAG_TOGGLE_SNEAK_OFF = unchecked((int)0x80000000);

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(ClientMain), "OnKeyDown")]
    public static bool Before_ClientMain_OnKeyDown(
        ClientMain __instance,
        KeyEvent args)
    {
        bool runOriginal = true;
        if (args.KeyCode == ToggleSneakModSystem.GetSneakKeyCode())
        {
            ToggleSneakModSystem.ToggleSneak();
            if (ToggleSneakModSystem.IsSneakToggledOn() == false)
            {
                args.KeyCode |= ACTION_FLAG_TOGGLE_SNEAK_OFF;
                __instance.OnKeyUp(args);
                runOriginal = false;
            }
        }
        return runOriginal;
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(ClientMain), "OnKeyUp")]
    public static bool Before_ClientMain_OnKeyUp(ref KeyEvent args)
    {
        bool runOriginal = true;
        int actualKeyCode = args.KeyCode & ~ACTION_FLAG_TOGGLE_SNEAK_OFF;
        bool shouldToggleSneakOff = (args.KeyCode & ACTION_FLAG_TOGGLE_SNEAK_OFF) != 0;
        if (actualKeyCode == ToggleSneakModSystem.GetSneakKeyCode())
        {
            runOriginal = shouldToggleSneakOff;
        }
        args.KeyCode = actualKeyCode;
        return runOriginal;
    }
}