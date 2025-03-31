using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory;
using System;

namespace ToggleSneak;

public class ToggleSneakModSystem : ModSystem
{
    private ICoreClientAPI _clientApi;

    HotKey _sneakHotKey = null;
    long _listenerId = 0;
    bool _triggerOnUpAlsoOriginal = false;
    private bool _sneakKeyIsPressed = false;
    private bool _sneakToggledOn = false;

    public override void StartClientSide(ICoreClientAPI api)
    {
        _clientApi = api;
        _sneakHotKey = _clientApi.Input.HotKeys["sneak"];
        Activate();
    }

    public override void Dispose()
    {
        base.Dispose();
    }

    private void Activate()
    {
        try
        {
            _triggerOnUpAlsoOriginal = _sneakHotKey.TriggerOnUpAlso;
            _sneakHotKey.TriggerOnUpAlso = true;
            _sneakHotKey.Handler += OnToggleSneakHotkey;
            _listenerId = _clientApi.Event.RegisterGameTickListener(
                OnGameTickCheckSneakToggle, 5);
        }
        catch (NullReferenceException)
        {
            // _sneakHotKey was deleted; nothing to do
            // or
            // game quit; nothing to do
        }
    }

    // Only needed to deactivate during runtime if needed in future.
    // Currently unused as deactivation during Dispose() results in an
    // unnecessary NullReferenceException being thrown by:
    // - Dereferencing _sneakHotKey
    // - _clientApi.Event.UnregisterGameTickListener(_listenerId);
    private void Deactivate()
    {
        try
        {
            _sneakHotKey.TriggerOnUpAlso = _triggerOnUpAlsoOriginal;
            _sneakHotKey.Handler -= OnToggleSneakHotkey;
            _clientApi.Event.UnregisterGameTickListener(_listenerId);
        }
        catch (NullReferenceException)
        {
            // _sneakHotKey was deleted; nothing to do
            // or
            // game quit; nothing to do
        }
    }

    private bool OnToggleSneakHotkey(KeyCombination keyComb)
    {
        bool isPressed = _clientApi.Input.KeyboardKeyState[keyComb.KeyCode];
        if ((_sneakKeyIsPressed == false) && (isPressed == true))
        {
            _sneakKeyIsPressed = true;
            _sneakToggledOn = !_sneakToggledOn;
        }
        if ((_sneakKeyIsPressed == true) && (isPressed == false))
        {
            _sneakKeyIsPressed = false;
        }
        return true;
    }

    private void OnGameTickCheckSneakToggle(float dt)
    {
        _clientApi.World.Player.Entity.Controls.Sneak = _sneakToggledOn;
    }
}
