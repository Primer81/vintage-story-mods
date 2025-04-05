using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory;
using System;

namespace ToggleSneak;

public class ToggleSneakModSystem : ModSystem
{
    private ICoreClientAPI clientApi;

    private bool triggerOnUpAlsoOriginal = false;
    private bool sneakKeyIsPressed = false;
    static private bool sneakToggledOn = false;

    public override void StartClientSide(ICoreClientAPI api)
    {
        clientApi = api;
        triggerOnUpAlsoOriginal =
            clientApi.Input.HotKeys["sneak"].TriggerOnUpAlso;
        clientApi.Input.HotKeys["sneak"].TriggerOnUpAlso = true;
        clientApi.Input.HotKeys["sneak"].Handler +=
            OnToggleSneakHotkey;
         clientApi.Event.RegisterGameTickListener(
            OnGameTickCheckSneakToggle, 5);
    }

    public override void Dispose()
    {
        // Restore hotkey
        {
            clientApi.Input.HotKeys["sneak"].TriggerOnUpAlso =
                triggerOnUpAlsoOriginal;
            clientApi.Input.HotKeys["sneak"].Handler -=
                OnToggleSneakHotkey;
        }
        base.Dispose();
    }

    private bool OnToggleSneakHotkey(KeyCombination keyComb)
    {
        bool isPressed = clientApi.Input.KeyboardKeyState[keyComb.KeyCode];
        if ((sneakKeyIsPressed == false) && (isPressed == true))
        {
            sneakKeyIsPressed = true;
            ToggleSneak();
        }
        if ((sneakKeyIsPressed == true) && (isPressed == false))
        {
            sneakKeyIsPressed = false;
        }
        return true;
    }

    private void OnGameTickCheckSneakToggle(float dt)
    {
        clientApi.World.Player.Entity.Controls.Sneak = sneakToggledOn;
    }

    public static void ToggleSneak()
    {
        sneakToggledOn = !sneakToggledOn;
    }
}
