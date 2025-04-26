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

public class ToggleMouseControlModSystem : ModSystem
{
    static public ICoreClientAPI ClientApi;
    static public bool DialogsWantMouseControlPrev;
    static public int DialogsOpenCountPrev;
    static public Config Config;

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
        DialogsOpenCountPrev = 0;
        mouseControlToggledOn = false;
        SystemPlayerControlMembers.IsValid = false;
        // Configuration
        {
            Config = Fetch<Config>();
            Dump(Config);
        }
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

    public static void Dump(object data)
    {
        ClientApi.StoreModConfig(
            data,
            System.IO.Path.Combine(
                $"{nameof(ToggleMouseControl)}",
                $"{data.GetType().Name}.json"));
    }

    public static dynamic Fetch<T>() where T : new()
    {
        string path = System.IO.Path.Combine(
            $"{nameof(ToggleMouseControl)}",
            $"{typeof(T).Name}.json");
        try
        {
            // Get the ICoreAPICommon interface type
            var interfaceType = typeof(ICoreAPICommon);

            // Get all methods from the interface with the name LoadModConfig
            var methods = interfaceType.GetMethods()
                .Where(m => m.Name == "LoadModConfig")
                .ToList();

            // Find the generic method specifically
            var genericMethod = methods.FirstOrDefault(m =>
                m.IsGenericMethod &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(string));

            if (genericMethod == null)
            {
                return new T();
            }

            // Make the generic method with our type parameter
            var constructedMethod = genericMethod.MakeGenericMethod(typeof(T));

            // Invoke the method through the interface
            return constructedMethod.Invoke(ClientApi, new object[] { path });
        }
        catch
        {
            return new T();
        }
    }
}