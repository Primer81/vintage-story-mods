using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;
using System;
using System.Linq;

namespace ToggleMouseControl;

public class ToggleMouseControlModSystem : ModSystem
{
    static public bool MouseControlToggledOn { get; set; }

    private ICoreClientAPI _clientApi;

    private HotKey _mouseControlHotKey = null;
    private long _listenerId = 0;
    private bool _triggerOnUpAlsoOriginal = false;
    private bool _mouseControlKeyIsPressed = false;
    private MouseController _mouseController;

    public override void StartClientSide(ICoreClientAPI api)
    {
        _clientApi = api;
        _mouseControlHotKey = _clientApi.Input.HotKeys["togglemousecontrol"];
        _mouseController = new MouseController(_clientApi);
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
            _triggerOnUpAlsoOriginal =
                _mouseControlHotKey.TriggerOnUpAlso;
            _mouseControlHotKey.TriggerOnUpAlso = true;
            _mouseControlHotKey.Handler += OnToggleMouseControlHotkey;
            _listenerId = _clientApi.Event.RegisterGameTickListener(
                OnGameTickCheckMouseControlToggle, 100);
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
            _clientApi.Event.UnregisterGameTickListener(_listenerId);
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
        bool isPressed = _clientApi.Input.KeyboardKeyState[keyComb.KeyCode];
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
