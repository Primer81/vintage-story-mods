#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.API.Client;

public abstract class GuiDialog : IDisposable
{
    //
    // Summary:
    //     Dialogue Composer for the GUIDialogue.
    public class DlgComposers : IEnumerable<KeyValuePair<string, GuiComposer>>, IEnumerable
    {
        protected ConcurrentSmallDictionary<string, GuiComposer> dialogComposers = new ConcurrentSmallDictionary<string, GuiComposer>();

        protected GuiDialog dialog;

        //
        // Summary:
        //     Returns all composers as a flat list
        public IEnumerable<GuiComposer> Values => dialogComposers.Values;

        //
        // Summary:
        //     Returns the composer for given composer name
        //
        // Parameters:
        //   key:
        public GuiComposer this[string key]
        {
            get
            {
                dialogComposers.TryGetValue(key, out var value);
                return value;
            }
            set
            {
                dialogComposers[key] = value;
                value.OnFocusChanged = dialog.OnFocusChanged;
            }
        }

        //
        // Summary:
        //     Constructor.
        //
        // Parameters:
        //   dialog:
        //     The dialogue this composer belongs to.
        public DlgComposers(GuiDialog dialog)
        {
            this.dialog = dialog;
        }

        //
        // Summary:
        //     Cleans up and clears the composers.
        public void ClearComposers()
        {
            foreach (KeyValuePair<string, GuiComposer> dialogComposer in dialogComposers)
            {
                dialogComposer.Value?.Dispose();
            }

            dialogComposers.Clear();
        }

        //
        // Summary:
        //     Clean disposal method.
        public void Dispose()
        {
            foreach (KeyValuePair<string, GuiComposer> dialogComposer in dialogComposers)
            {
                dialogComposer.Value?.Dispose();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dialogComposers.GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, GuiComposer>> IEnumerable<KeyValuePair<string, GuiComposer>>.GetEnumerator()
        {
            return dialogComposers.GetEnumerator();
        }

        //
        // Summary:
        //     Checks to see if the key is located within the given dialogue composer.
        //
        // Parameters:
        //   key:
        //     The key you are searching for.
        //
        // Returns:
        //     Do we have your key?
        public bool ContainsKey(string key)
        {
            return dialogComposers.ContainsKey(key);
        }

        //
        // Summary:
        //     Removes the given key and the corresponding value from the Dialogue Composer.
        //
        //
        // Parameters:
        //   key:
        //     The Key to remove.
        public void Remove(string key)
        {
            dialogComposers.Remove(key);
        }

        public GuiComposer[] ToArray()
        {
            GuiComposer[] array = new GuiComposer[dialogComposers.Count];
            dialogComposers.Values.CopyTo(array, 0);
            return array;
        }
    }

    [Flags]
    public enum EnumPosFlag
    {
        RightMid = 1,
        RightTop = 2,
        RightBot = 4,
        LeftMid = 8,
        LeftTop = 0x10,
        LeftBot = 0x20,
        Right2Mid = 0x40,
        Right2Top = 0x80,
        Right2Bot = 0x100,
        Left2Mid = 0x200,
        Left2Top = 0x400,
        Left2Bot = 0x800,
        Right3Mid = 0x1000,
        Right3Top = 0x2000,
        Right3Bot = 0x4000,
        Left3Mid = 0x8000,
        Left3Top = 0x10000,
        Left3Bot = 0x20000
    }

    //
    // Summary:
    //     The Instance of Dialogue Composer for this GUIDialogue.
    public DlgComposers Composers;

    public bool ignoreNextKeyPress;

    protected bool opened;

    protected bool focused;

    protected ICoreClientAPI capi;

    public string MouseOverCursor;

    //
    // Summary:
    //     A single composer for this GUIDialogue.
    public GuiComposer SingleComposer
    {
        get
        {
            return Composers["single"];
        }
        set
        {
            Composers["single"] = value;
        }
    }

    //
    // Summary:
    //     Debug name. For debugging purposes.
    public virtual string DebugName => GetType().Name;

    //
    // Summary:
    //     The amount of depth required for this dialog. Default is 150. Required for correct
    //     z-ordering of dialogs.
    public virtual float ZSize => 150f;

    //
    // Summary:
    //     Is the dialogue currently in focus?
    public virtual bool Focused => focused;

    //
    // Summary:
    //     Can this dialog be focused?
    public virtual bool Focusable => true;

    //
    // Summary:
    //     Is this dialogue a dialogue or a HUD object?
    public virtual EnumDialogType DialogType => EnumDialogType.Dialog;

    //
    // Summary:
    //     0 = draw first, 1 = draw last. Used to enforce tooltips and held itemstack always
    //     drawn last to be visible.
    //     Vanilla dialogs draw order:
    //     Name tags: -0.1
    //     Chat dialog: 0
    //     Block Interaction help: 0.05
    //     Worldmap HUD: 0.07
    //     Default value for most other dialogs: 0.1
    //     Worldmap Dialog: 0.11
    //     Player and Chest inventories: 0.2
    //     Various config/edit dialogs: 0.2
    //     Handbook: 0.2
    //     Escape menu: 0.89
    public virtual double DrawOrder => 0.1;

    //
    // Summary:
    //     Determines the order on which dialog receives keyboard input first when the dialog
    //     is opened. 0 = handle inputs first, 9999 = handle inputs last.
    //     Reference list:
    //     0: Escape menu
    //     0.5 (default): tick profiler, selection box editor, macro editor, survival&creative
    //     inventory, first launch info dialog, dead dialog, character dialog, etc.
    //     1: hotbar
    //     1.1: chat dialog
    public virtual double InputOrder => 0.5;

    //
    // Summary:
    //     Should this dialogue de-register itself once it's closed? (Defaults to no)
    public virtual bool UnregisterOnClose => false;

    //
    // Summary:
    //     Gets whether it is preferred for the mouse to be not grabbed while this dialog
    //     is opened. If true (default), the Alt button needs to be held to manually grab
    //     the mouse.
    public virtual bool PrefersUngrabbedMouse => RequiresUngrabbedMouse();

    //
    // Summary:
    //     Gets whether ability to grab the mouse cursor is disabled while this dialog is
    //     opened. For example, the escape menu. (Default: false)
    public virtual bool DisableMouseGrab => false;

    //
    // Summary:
    //     The key combination string that toggles this GUI object.
    public abstract string ToggleKeyCombinationCode { get; }

    //
    // Summary:
    //     The event fired when this dialogue is opened.
    public event Action OnOpened;

    //
    // Summary:
    //     The event fired when this dialogue is closed.
    public event Action OnClosed;

    protected virtual void OnFocusChanged(bool on)
    {
        if (on != focused && (DialogType != 0 || opened))
        {
            if (on)
            {
                capi.Gui.RequestFocus(this);
            }
            else
            {
                focused = false;
            }
        }
    }

    //
    // Summary:
    //     Constructor for the GUIDialogue.
    //
    // Parameters:
    //   capi:
    //     The Client API.
    public GuiDialog(ICoreClientAPI capi)
    {
        Composers = new DlgComposers(this);
        this.capi = capi;
    }

    //
    // Summary:
    //     Makes this gui pop up once a pre-set given key combination is set.
    public virtual void OnBlockTexturesLoaded()
    {
        string toggleKeyCombinationCode = ToggleKeyCombinationCode;
        if (toggleKeyCombinationCode != null)
        {
            capi.Input.SetHotKeyHandler(toggleKeyCombinationCode, OnKeyCombinationToggle);
        }
    }

    public virtual void OnLevelFinalize()
    {
    }

    public virtual void OnOwnPlayerDataReceived()
    {
    }

    //
    // Summary:
    //     Fires when the GUI is opened.
    public virtual void OnGuiOpened()
    {
    }

    //
    // Summary:
    //     Fires when the GUI is closed.
    public virtual void OnGuiClosed()
    {
    }

    //
    // Summary:
    //     Attempts to open this dialogue.
    //
    // Returns:
    //     Was this dialogue successfully opened?
    public virtual bool TryOpen()
    {
        return TryOpen(withFocus: true);
    }

    public virtual bool TryOpen(bool withFocus)
    {
        bool flag = opened;
        if (!capi.Gui.LoadedGuis.Contains(this))
        {
            capi.Gui.RegisterDialog(this);
        }

        opened = true;
        if (DialogType == EnumDialogType.Dialog && withFocus)
        {
            capi.Gui.RequestFocus(this);
        }

        if (!flag)
        {
            OnGuiOpened();
            this.OnOpened?.Invoke();
            capi.Gui.TriggerDialogOpened(this);
        }

        return true;
    }

    //
    // Summary:
    //     Attempts to close this dialogue- triggering the OnCloseDialogue event.
    //
    // Returns:
    //     Was this dialogue successfully closed?
    public virtual bool TryClose()
    {
        bool num = opened;
        opened = false;
        UnFocus();
        if (num)
        {
            OnGuiClosed();
            this.OnClosed?.Invoke();
        }

        focused = false;
        if (num)
        {
            capi.Gui.TriggerDialogClosed(this);
        }

        return true;
    }

    //
    // Summary:
    //     Unfocuses the dialogue.
    public virtual void UnFocus()
    {
        focused = false;
    }

    //
    // Summary:
    //     Focuses the dialog
    public virtual void Focus()
    {
        if (Focusable)
        {
            focused = true;
        }
    }

    //
    // Summary:
    //     If the dialogue is opened, this attempts to close it. If the dialogue is closed,
    //     this attempts to open it.
    public virtual void Toggle()
    {
        if (IsOpened())
        {
            TryClose();
        }
        else
        {
            TryOpen();
        }
    }

    //
    // Summary:
    //     Is this dialogue opened?
    //
    // Returns:
    //     Whether this dialogue is opened or not.
    public virtual bool IsOpened()
    {
        return opened;
    }

    //
    // Summary:
    //     Is this dialogue opened in the given context?
    //
    // Parameters:
    //   dialogComposerName:
    //     The composer context.
    //
    // Returns:
    //     Whether this dialogue was opened or not within the given context.
    public virtual bool IsOpened(string dialogComposerName)
    {
        return IsOpened();
    }

    //
    // Summary:
    //     This runs before the render. Local update method.
    //
    // Parameters:
    //   deltaTime:
    //     The time that has elapsed.
    public virtual void OnBeforeRenderFrame3D(float deltaTime)
    {
    }

    //
    // Summary:
    //     This runs when the dialogue is ready to render all of the components.
    //
    // Parameters:
    //   deltaTime:
    //     The time that has elapsed.
    public virtual void OnRenderGUI(float deltaTime)
    {
        foreach (KeyValuePair<string, GuiComposer> item in (IEnumerable<KeyValuePair<string, GuiComposer>>)Composers)
        {
            item.Value.Render(deltaTime);
            MouseOverCursor = item.Value.MouseOverCursor;
        }
    }

    //
    // Summary:
    //     This runs when the dialogue is finalizing and cleaning up all of the components.
    //
    //
    // Parameters:
    //   dt:
    //     The time that has elapsed.
    public virtual void OnFinalizeFrame(float dt)
    {
        foreach (KeyValuePair<string, GuiComposer> item in (IEnumerable<KeyValuePair<string, GuiComposer>>)Composers)
        {
            item.Value.PostRender(dt);
        }
    }

    internal virtual bool OnKeyCombinationToggle(KeyCombination viaKeyComb)
    {
        HotKey hotKeyByCode = capi.Input.GetHotKeyByCode(ToggleKeyCombinationCode);
        if (hotKeyByCode == null)
        {
            return false;
        }

        if (hotKeyByCode.KeyCombinationType == HotkeyType.CreativeTool && capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Creative)
        {
            return false;
        }

        Toggle();
        return true;
    }

    //
    // Summary:
    //     Fires when keys are held down.
    //
    // Parameters:
    //   args:
    //     The key or keys that were held down.
    public virtual void OnKeyDown(KeyEvent args)
    {
        GuiComposer[] array = Composers.ToArray();
        for (int i = 0; i < array.Length; i++)
        {
            array[i].OnKeyDown(args, focused);
            if (args.Handled)
            {
                return;
            }
        }

        HotKey hotKeyByCode = capi.Input.GetHotKeyByCode(ToggleKeyCombinationCode);
        if (hotKeyByCode != null && hotKeyByCode.DidPress(args, capi.World, capi.World.Player, allowCharacterControls: true) && TryClose())
        {
            args.Handled = true;
        }
    }

    //
    // Summary:
    //     Fires when the keys are pressed.
    //
    // Parameters:
    //   args:
    //     The key or keys that were pressed.
    public virtual void OnKeyPress(KeyEvent args)
    {
        if (ignoreNextKeyPress)
        {
            ignoreNextKeyPress = false;
            args.Handled = true;
        }
        else
        {
            if (args.Handled)
            {
                return;
            }

            GuiComposer[] array = Composers.ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                array[i].OnKeyPress(args);
                if (args.Handled)
                {
                    break;
                }
            }
        }
    }

    //
    // Summary:
    //     Fires when the keys are released.
    //
    // Parameters:
    //   args:
    //     the key or keys that were released.
    public virtual void OnKeyUp(KeyEvent args)
    {
        GuiComposer[] array = Composers.ToArray();
        for (int i = 0; i < array.Length; i++)
        {
            array[i].OnKeyUp(args);
            if (args.Handled)
            {
                break;
            }
        }
    }

    //
    // Summary:
    //     Fires explicitly when the Escape key is pressed and attempts to close the dialogue.
    //
    //
    // Returns:
    //     Whether the dialogue was closed.
    public virtual bool OnEscapePressed()
    {
        if (DialogType == EnumDialogType.HUD)
        {
            return false;
        }

        return TryClose();
    }

    //
    // Summary:
    //     Fires when the mouse enters the given slot.
    //
    // Parameters:
    //   slot:
    //     The slot the mouse entered.
    //
    // Returns:
    //     Whether this event was handled.
    public virtual bool OnMouseEnterSlot(ItemSlot slot)
    {
        GuiComposer[] array = Composers.ToArray();
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].OnMouseEnterSlot(slot))
            {
                return true;
            }
        }

        return false;
    }

    //
    // Summary:
    //     Fires when the mouse leaves the slot.
    //
    // Parameters:
    //   itemSlot:
    //     The slot the mouse entered.
    //
    // Returns:
    //     Whether this event was handled.
    public virtual bool OnMouseLeaveSlot(ItemSlot itemSlot)
    {
        GuiComposer[] array = Composers.ToArray();
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].OnMouseLeaveSlot(itemSlot))
            {
                return true;
            }
        }

        return false;
    }

    //
    // Summary:
    //     Fires when the mouse clicks within the slot.
    //
    // Parameters:
    //   itemSlot:
    //     The slot that the mouse clicked in.
    //
    // Returns:
    //     Whether this event was handled.
    public virtual bool OnMouseClickSlot(ItemSlot itemSlot)
    {
        return false;
    }

    //
    // Summary:
    //     Fires when a mouse button is held down.
    //
    // Parameters:
    //   args:
    //     The mouse button or buttons in question.
    public virtual void OnMouseDown(MouseEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        GuiComposer[] array = Composers.ToArray();
        GuiComposer[] array2 = array;
        for (int i = 0; i < array2.Length; i++)
        {
            array2[i].OnMouseDown(args);
            if (args.Handled)
            {
                return;
            }
        }

        if (!IsOpened())
        {
            return;
        }

        array2 = array;
        for (int i = 0; i < array2.Length; i++)
        {
            if (array2[i].Bounds.PointInside(args.X, args.Y))
            {
                args.Handled = true;
                break;
            }
        }
    }

    //
    // Summary:
    //     Fires when a mouse button is released.
    //
    // Parameters:
    //   args:
    //     The mouse button or buttons in question.
    public virtual void OnMouseUp(MouseEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        GuiComposer[] array = Composers.ToArray();
        GuiComposer[] array2 = array;
        for (int i = 0; i < array2.Length; i++)
        {
            array2[i].OnMouseUp(args);
            if (args.Handled)
            {
                return;
            }
        }

        array2 = array;
        for (int i = 0; i < array2.Length; i++)
        {
            if (array2[i].Bounds.PointInside(args.X, args.Y))
            {
                args.Handled = true;
                break;
            }
        }
    }

    //
    // Summary:
    //     Fires when the mouse is moved.
    //
    // Parameters:
    //   args:
    //     The mouse movements in question.
    public virtual void OnMouseMove(MouseEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        GuiComposer[] array = Composers.ToArray();
        GuiComposer[] array2 = array;
        for (int i = 0; i < array2.Length; i++)
        {
            array2[i].OnMouseMove(args);
            if (args.Handled)
            {
                return;
            }
        }

        array2 = array;
        for (int i = 0; i < array2.Length; i++)
        {
            if (array2[i].Bounds.PointInside(args.X, args.Y))
            {
                args.Handled = true;
                break;
            }
        }
    }

    //
    // Summary:
    //     Fires when the mouse wheel is scrolled.
    //
    // Parameters:
    //   args:
    public virtual void OnMouseWheel(MouseWheelEventArgs args)
    {
        GuiComposer[] array = Composers.ToArray();
        GuiComposer[] array2 = array;
        for (int i = 0; i < array2.Length; i++)
        {
            array2[i].OnMouseWheel(args);
            if (args.IsHandled)
            {
                return;
            }
        }

        if (!focused)
        {
            return;
        }

        array2 = array;
        for (int i = 0; i < array2.Length; i++)
        {
            if (array2[i].Bounds.PointInside(capi.Input.MouseX, capi.Input.MouseY))
            {
                args.SetHandled();
            }
        }
    }

    //
    // Summary:
    //     A check for whether the dialogue should recieve Render events.
    //
    // Returns:
    //     Whether the dialogue is opened or not.
    public virtual bool ShouldReceiveRenderEvents()
    {
        return opened;
    }

    //
    // Summary:
    //     A check for whether the dialogue should recieve keyboard events.
    //
    // Returns:
    //     Whether the dialogue is focused or not.
    public virtual bool ShouldReceiveKeyboardEvents()
    {
        return focused;
    }

    //
    // Summary:
    //     A check if the dialogue should recieve mouse events.
    //
    // Returns:
    //     Whether the mouse events should fire.
    public virtual bool ShouldReceiveMouseEvents()
    {
        return IsOpened();
    }

    [Obsolete("Use PrefersUngrabbedMouse instead")]
    public virtual bool RequiresUngrabbedMouse()
    {
        return true;
    }

    //
    // Summary:
    //     Should this dialog (e.g. textbox) capture all the keyboard events except for
    //     escape.
    public virtual bool CaptureAllInputs()
    {
        return false;
    }

    //
    // Summary:
    //     Should this dialog capture the raw mouse button clicks - useful for example for
    //     the settings menu itself (in case the user has unset them or set them to something
    //     crazy)
    public virtual bool CaptureRawMouse()
    {
        return false;
    }

    //
    // Summary:
    //     Disposes the Dialog.
    public virtual void Dispose()
    {
        Composers?.Dispose();
    }

    //
    // Summary:
    //     Clears the composers.
    public void ClearComposers()
    {
        Composers?.ClearComposers();
    }

    //
    // Summary:
    //     Checks if the player is in range (pickingrange) of the given position
    //
    // Parameters:
    //   pos:
    //
    // Returns:
    //     In range or no?
    public virtual bool IsInRangeOf(Vec3d pos)
    {
        Vec3d pos2 = capi.World.Player.Entity.Pos.XYZ.Add(capi.World.Player.Entity.LocalEyePos);
        return (double)pos.DistanceTo(pos2) <= (double)capi.World.Player.WorldData.PickingRange + 0.5;
    }

    public EnumPosFlag GetFreePos(string code)
    {
        Array values = Enum.GetValues(typeof(EnumPosFlag));
        int value = 0;
        posFlagDict().TryGetValue(code, out value);
        foreach (EnumPosFlag item in values)
        {
            if ((int)((uint)value & (uint)item) <= 0)
            {
                return item;
            }
        }

        return (EnumPosFlag)0;
    }

    public void OccupyPos(string code, EnumPosFlag pos)
    {
        int value = 0;
        posFlagDict().TryGetValue(code, out value);
        posFlagDict()[code] = value | (int)pos;
    }

    public void FreePos(string code, EnumPosFlag pos)
    {
        int value = 0;
        posFlagDict().TryGetValue(code, out value);
        posFlagDict()[code] = value & (int)(~pos);
    }

    private Dictionary<string, int> posFlagDict()
    {
        capi.ObjectCache.TryGetValue("dialogCount", out var value);
        Dictionary<string, int> dictionary = value as Dictionary<string, int>;
        if (dictionary == null)
        {
            dictionary = (Dictionary<string, int>)(capi.ObjectCache["dialogCount"] = new Dictionary<string, int>());
        }

        return dictionary;
    }

    protected bool IsRight(EnumPosFlag flag)
    {
        if (flag != EnumPosFlag.RightBot && flag != EnumPosFlag.RightMid && flag != EnumPosFlag.RightTop && flag != EnumPosFlag.Right2Top && flag != EnumPosFlag.Right2Mid && flag != EnumPosFlag.Right2Bot && flag != EnumPosFlag.Right3Top && flag != EnumPosFlag.Right3Mid)
        {
            return flag == EnumPosFlag.Right3Bot;
        }

        return true;
    }

    protected float YOffsetMul(EnumPosFlag flag)
    {
        switch (flag)
        {
            case EnumPosFlag.RightTop:
            case EnumPosFlag.LeftTop:
            case EnumPosFlag.Right2Top:
            case EnumPosFlag.Left2Top:
            case EnumPosFlag.Right3Top:
            case EnumPosFlag.Left3Top:
                return -1f;
            case EnumPosFlag.RightBot:
            case EnumPosFlag.LeftBot:
            case EnumPosFlag.Right2Bot:
            case EnumPosFlag.Left2Bot:
            case EnumPosFlag.Right3Bot:
            case EnumPosFlag.Left3Bot:
                return 1f;
            default:
                return 0f;
        }
    }

    protected float XOffsetMul(EnumPosFlag flag)
    {
        switch (flag)
        {
            case EnumPosFlag.Right2Mid:
            case EnumPosFlag.Right2Top:
            case EnumPosFlag.Right2Bot:
                return -1f;
            case EnumPosFlag.Left2Mid:
            case EnumPosFlag.Left2Top:
            case EnumPosFlag.Left2Bot:
                return 1f;
            case EnumPosFlag.Right3Mid:
            case EnumPosFlag.Right3Top:
            case EnumPosFlag.Right3Bot:
                return -2f;
            case EnumPosFlag.Left3Mid:
            case EnumPosFlag.Left3Top:
            case EnumPosFlag.Left3Bot:
                return 2f;
            default:
                return 0f;
        }
    }
}
#if false // Decompilation log
'168' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Could not find by name: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Could not find by name: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Could not find by name: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
