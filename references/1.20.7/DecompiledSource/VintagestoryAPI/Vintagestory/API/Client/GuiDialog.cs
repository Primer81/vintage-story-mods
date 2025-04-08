using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.API.Client;

public abstract class GuiDialog : IDisposable
{
	/// <summary>
	/// Dialogue Composer for the GUIDialogue.
	/// </summary>
	public class DlgComposers : IEnumerable<KeyValuePair<string, GuiComposer>>, IEnumerable
	{
		protected ConcurrentSmallDictionary<string, GuiComposer> dialogComposers = new ConcurrentSmallDictionary<string, GuiComposer>();

		protected GuiDialog dialog;

		/// <summary>
		/// Returns all composers as a flat list
		/// </summary>
		public IEnumerable<GuiComposer> Values => dialogComposers.Values;

		/// <summary>
		/// Returns the composer for given composer name
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public GuiComposer this[string key]
		{
			get
			{
				dialogComposers.TryGetValue(key, out var val);
				return val;
			}
			set
			{
				dialogComposers[key] = value;
				value.OnFocusChanged = dialog.OnFocusChanged;
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="dialog">The dialogue this composer belongs to.</param>
		public DlgComposers(GuiDialog dialog)
		{
			this.dialog = dialog;
		}

		/// <summary>
		/// Cleans up and clears the composers.
		/// </summary>
		public void ClearComposers()
		{
			foreach (KeyValuePair<string, GuiComposer> dialogComposer in dialogComposers)
			{
				dialogComposer.Value?.Dispose();
			}
			dialogComposers.Clear();
		}

		/// <summary>
		/// Clean disposal method.
		/// </summary>
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

		/// <summary>
		/// Checks to see if the key is located within the given dialogue composer.
		/// </summary>
		/// <param name="key">The key you are searching for.</param>
		/// <returns>Do we have your key?</returns>
		public bool ContainsKey(string key)
		{
			return dialogComposers.ContainsKey(key);
		}

		/// <summary>
		/// Removes the given key and the corresponding value from the Dialogue Composer.
		/// </summary>
		/// <param name="key">The Key to remove.</param>
		public void Remove(string key)
		{
			dialogComposers.Remove(key);
		}

		public GuiComposer[] ToArray()
		{
			GuiComposer[] result = new GuiComposer[dialogComposers.Count];
			dialogComposers.Values.CopyTo(result, 0);
			return result;
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

	/// <summary>
	/// The Instance of Dialogue Composer for this GUIDialogue.
	/// </summary>
	public DlgComposers Composers;

	public bool ignoreNextKeyPress;

	protected bool opened;

	protected bool focused;

	protected ICoreClientAPI capi;

	public string MouseOverCursor;

	/// <summary>
	/// A single composer for this GUIDialogue.
	/// </summary>
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

	/// <summary>
	/// Debug name.  For debugging purposes.
	/// </summary>
	public virtual string DebugName => GetType().Name;

	/// <summary>
	/// The amount of depth required for this dialog. Default is 150. Required for correct z-ordering of dialogs.
	/// </summary>
	public virtual float ZSize => 150f;

	/// <summary>
	/// Is the dialogue currently in focus?
	/// </summary>
	public virtual bool Focused => focused;

	/// <summary>
	/// Can this dialog be focused?
	/// </summary>
	public virtual bool Focusable => true;

	/// <summary>
	/// Is this dialogue a dialogue or a HUD object?
	/// </summary>
	public virtual EnumDialogType DialogType => EnumDialogType.Dialog;

	/// <summary>
	/// 0 = draw first, 1 = draw last. Used to enforce tooltips and held itemstack always drawn last to be visible.<br />
	/// Vanilla dialogs draw order:<br />
	/// Name tags: -0.1<br />
	/// Chat dialog: 0<br />
	/// Block Interaction help: 0.05<br />
	/// Worldmap HUD: 0.07<br />
	/// Default value for most other dialogs: 0.1<br />
	/// Worldmap Dialog: 0.11<br />
	/// Player and Chest inventories: 0.2<br />
	/// Various config/edit dialogs: 0.2<br />
	/// Handbook: 0.2<br />
	/// Escape menu: 0.89
	/// </summary>
	public virtual double DrawOrder => 0.1;

	/// <summary>
	/// Determines the order on which dialog receives keyboard input first when the dialog is opened. 0 = handle inputs first, 9999 = handle inputs last.<br />
	/// Reference list:<br />
	/// 0: Escape menu<br />
	/// 0.5 (default): tick profiler, selection box editor, macro editor, survival&amp;creative inventory, first launch info dialog, dead dialog, character dialog, etc.<br />
	/// 1: hotbar<br />
	/// 1.1: chat dialog
	/// </summary>
	public virtual double InputOrder => 0.5;

	/// <summary>
	/// Should this dialogue de-register itself once it's closed? (Defaults to no)
	/// </summary>
	public virtual bool UnregisterOnClose => false;

	/// <summary>
	/// Gets whether it is preferred for the mouse to be not grabbed while this dialog is opened.
	/// If true (default), the Alt button needs to be held to manually grab the mouse.
	/// </summary>
	public virtual bool PrefersUngrabbedMouse => RequiresUngrabbedMouse();

	/// <summary>
	/// Gets whether ability to grab the mouse cursor is disabled while
	/// this dialog is opened. For example, the escape menu. (Default: false)
	/// </summary>
	public virtual bool DisableMouseGrab => false;

	/// <summary>
	/// The key combination string that toggles this GUI object.
	/// </summary>
	public abstract string ToggleKeyCombinationCode { get; }

	/// <summary>
	/// The event fired when this dialogue is opened.
	/// </summary>
	public event Action OnOpened;

	/// <summary>
	/// The event fired when this dialogue is closed.
	/// </summary>
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

	/// <summary>
	/// Constructor for the GUIDialogue.
	/// </summary>
	/// <param name="capi">The Client API.</param>
	public GuiDialog(ICoreClientAPI capi)
	{
		Composers = new DlgComposers(this);
		this.capi = capi;
	}

	/// <summary>
	/// Makes this gui pop up once a pre-set given key combination is set.
	/// </summary>
	public virtual void OnBlockTexturesLoaded()
	{
		string keyCombCode = ToggleKeyCombinationCode;
		if (keyCombCode != null)
		{
			capi.Input.SetHotKeyHandler(keyCombCode, OnKeyCombinationToggle);
		}
	}

	/// <summary>
	///
	/// </summary>
	public virtual void OnLevelFinalize()
	{
	}

	public virtual void OnOwnPlayerDataReceived()
	{
	}

	/// <summary>
	/// Fires when the GUI is opened.
	/// </summary>
	public virtual void OnGuiOpened()
	{
	}

	/// <summary>
	/// Fires when the GUI is closed.
	/// </summary>
	public virtual void OnGuiClosed()
	{
	}

	/// <summary>
	/// Attempts to open this dialogue.
	/// </summary>
	/// <returns>Was this dialogue successfully opened?</returns>
	public virtual bool TryOpen()
	{
		return TryOpen(withFocus: true);
	}

	public virtual bool TryOpen(bool withFocus)
	{
		bool wasOpened = opened;
		if (!capi.Gui.LoadedGuis.Contains(this))
		{
			capi.Gui.RegisterDialog(this);
		}
		opened = true;
		if (DialogType == EnumDialogType.Dialog && withFocus)
		{
			capi.Gui.RequestFocus(this);
		}
		if (!wasOpened)
		{
			OnGuiOpened();
			this.OnOpened?.Invoke();
			capi.Gui.TriggerDialogOpened(this);
		}
		return true;
	}

	/// <summary>
	/// Attempts to close this dialogue- triggering the OnCloseDialogue event.
	/// </summary>
	/// <returns>Was this dialogue successfully closed?</returns>
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

	/// <summary>
	/// Unfocuses the dialogue.
	/// </summary>
	public virtual void UnFocus()
	{
		focused = false;
	}

	/// <summary>
	/// Focuses the dialog
	/// </summary>
	public virtual void Focus()
	{
		if (Focusable)
		{
			focused = true;
		}
	}

	/// <summary>
	/// If the dialogue is opened, this attempts to close it.  If the dialogue is closed, this attempts to open it.
	/// </summary>
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

	/// <summary>
	/// Is this dialogue opened?
	/// </summary>
	/// <returns>Whether this dialogue is opened or not.</returns>
	public virtual bool IsOpened()
	{
		return opened;
	}

	/// <summary>
	/// Is this dialogue opened in the given context?
	/// </summary>
	/// <param name="dialogComposerName">The composer context.</param>
	/// <returns>Whether this dialogue was opened or not within the given context.</returns>
	public virtual bool IsOpened(string dialogComposerName)
	{
		return IsOpened();
	}

	/// <summary>
	/// This runs before the render.  Local update method.
	/// </summary>
	/// <param name="deltaTime">The time that has elapsed.</param>
	public virtual void OnBeforeRenderFrame3D(float deltaTime)
	{
	}

	/// <summary>
	/// This runs when the dialogue is ready to render all of the components.
	/// </summary>
	/// <param name="deltaTime">The time that has elapsed.</param>
	public virtual void OnRenderGUI(float deltaTime)
	{
		foreach (KeyValuePair<string, GuiComposer> val in (IEnumerable<KeyValuePair<string, GuiComposer>>)Composers)
		{
			val.Value.Render(deltaTime);
			MouseOverCursor = val.Value.MouseOverCursor;
		}
	}

	/// <summary>
	/// This runs when the dialogue is finalizing and cleaning up all of the components.
	/// </summary>
	/// <param name="dt">The time that has elapsed.</param>
	public virtual void OnFinalizeFrame(float dt)
	{
		foreach (KeyValuePair<string, GuiComposer> item in (IEnumerable<KeyValuePair<string, GuiComposer>>)Composers)
		{
			item.Value.PostRender(dt);
		}
	}

	internal virtual bool OnKeyCombinationToggle(KeyCombination viaKeyComb)
	{
		HotKey hotkey = capi.Input.GetHotKeyByCode(ToggleKeyCombinationCode);
		if (hotkey == null)
		{
			return false;
		}
		if (hotkey.KeyCombinationType == HotkeyType.CreativeTool && capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			return false;
		}
		Toggle();
		return true;
	}

	/// <summary>
	/// Fires when keys are held down.
	/// </summary>
	/// <param name="args">The key or keys that were held down.</param>
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
		HotKey hotkey = capi.Input.GetHotKeyByCode(ToggleKeyCombinationCode);
		if (hotkey != null && hotkey.DidPress(args, capi.World, capi.World.Player, allowCharacterControls: true) && TryClose())
		{
			args.Handled = true;
		}
	}

	/// <summary>
	/// Fires when the keys are pressed.
	/// </summary>
	/// <param name="args">The key or keys that were pressed.</param>
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

	/// <summary>
	/// Fires when the keys are released.
	/// </summary>
	/// <param name="args">the key or keys that were released.</param>
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

	/// <summary>
	/// Fires explicitly when the Escape key is pressed and attempts to close the dialogue.
	/// </summary>
	/// <returns>Whether the dialogue was closed.</returns>
	public virtual bool OnEscapePressed()
	{
		if (DialogType == EnumDialogType.HUD)
		{
			return false;
		}
		return TryClose();
	}

	/// <summary>
	/// Fires when the mouse enters the given slot.
	/// </summary>
	/// <param name="slot">The slot the mouse entered.</param>
	/// <returns>Whether this event was handled.</returns>
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

	/// <summary>
	/// Fires when the mouse leaves the slot.
	/// </summary>
	/// <param name="itemSlot">The slot the mouse entered.</param>
	/// <returns>Whether this event was handled.</returns>
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

	/// <summary>
	/// Fires when the mouse clicks within the slot.
	/// </summary>
	/// <param name="itemSlot">The slot that the mouse clicked in.</param>
	/// <returns>Whether this event was handled.</returns>
	public virtual bool OnMouseClickSlot(ItemSlot itemSlot)
	{
		return false;
	}

	/// <summary>
	/// Fires when a mouse button is held down.
	/// </summary>
	/// <param name="args">The mouse button or buttons in question.</param>
	public virtual void OnMouseDown(MouseEvent args)
	{
		if (args.Handled)
		{
			return;
		}
		GuiComposer[] composers = Composers.ToArray();
		GuiComposer[] array = composers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnMouseDown(args);
			if (args.Handled)
			{
				return;
			}
		}
		if (!IsOpened())
		{
			return;
		}
		array = composers;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Bounds.PointInside(args.X, args.Y))
			{
				args.Handled = true;
				break;
			}
		}
	}

	/// <summary>
	/// Fires when a mouse button is released.
	/// </summary>
	/// <param name="args">The mouse button or buttons in question.</param>
	public virtual void OnMouseUp(MouseEvent args)
	{
		if (args.Handled)
		{
			return;
		}
		GuiComposer[] composers = Composers.ToArray();
		GuiComposer[] array = composers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnMouseUp(args);
			if (args.Handled)
			{
				return;
			}
		}
		array = composers;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Bounds.PointInside(args.X, args.Y))
			{
				args.Handled = true;
				break;
			}
		}
	}

	/// <summary>
	/// Fires when the mouse is moved.
	/// </summary>
	/// <param name="args">The mouse movements in question.</param>
	public virtual void OnMouseMove(MouseEvent args)
	{
		if (args.Handled)
		{
			return;
		}
		GuiComposer[] composers = Composers.ToArray();
		GuiComposer[] array = composers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnMouseMove(args);
			if (args.Handled)
			{
				return;
			}
		}
		array = composers;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Bounds.PointInside(args.X, args.Y))
			{
				args.Handled = true;
				break;
			}
		}
	}

	/// <summary>
	/// Fires when the mouse wheel is scrolled.
	/// </summary>
	/// <param name="args"></param>
	public virtual void OnMouseWheel(MouseWheelEventArgs args)
	{
		GuiComposer[] composers = Composers.ToArray();
		GuiComposer[] array = composers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnMouseWheel(args);
			if (args.IsHandled)
			{
				return;
			}
		}
		if (!focused)
		{
			return;
		}
		array = composers;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Bounds.PointInside(capi.Input.MouseX, capi.Input.MouseY))
			{
				args.SetHandled();
			}
		}
	}

	/// <summary>
	/// A check for whether the dialogue should recieve Render events.
	/// </summary>
	/// <returns>Whether the dialogue is opened or not.</returns>
	public virtual bool ShouldReceiveRenderEvents()
	{
		return opened;
	}

	/// <summary>
	/// A check for whether the dialogue should recieve keyboard events.
	/// </summary>
	/// <returns>Whether the dialogue is focused or not.</returns>
	public virtual bool ShouldReceiveKeyboardEvents()
	{
		return focused;
	}

	/// <summary>
	/// A check if the dialogue should recieve mouse events.
	/// </summary>
	/// <returns>Whether the mouse events should fire.</returns>
	public virtual bool ShouldReceiveMouseEvents()
	{
		return IsOpened();
	}

	[Obsolete("Use PrefersUngrabbedMouse instead")]
	public virtual bool RequiresUngrabbedMouse()
	{
		return true;
	}

	/// <summary>
	/// Should this dialog (e.g. textbox) capture all the keyboard events except for escape.
	/// </summary>
	/// <returns></returns>
	public virtual bool CaptureAllInputs()
	{
		return false;
	}

	/// <summary>
	/// Should this dialog capture the raw mouse button clicks - useful for example for the settings menu itself (in case the user has unset them or set them to something crazy)
	/// </summary>
	/// <returns></returns>
	public virtual bool CaptureRawMouse()
	{
		return false;
	}

	/// <summary>
	/// Disposes the Dialog.
	/// </summary>
	public virtual void Dispose()
	{
		Composers?.Dispose();
	}

	/// <summary>
	/// Clears the composers.
	/// </summary>
	public void ClearComposers()
	{
		Composers?.ClearComposers();
	}

	/// <summary>
	/// Checks if the player is in range (pickingrange) of the given position
	/// </summary>
	/// <param name="pos"></param>
	/// <returns>In range or no?</returns>
	public virtual bool IsInRangeOf(Vec3d pos)
	{
		Vec3d playerEye = capi.World.Player.Entity.Pos.XYZ.Add(capi.World.Player.Entity.LocalEyePos);
		return (double)pos.DistanceTo(playerEye) <= (double)capi.World.Player.WorldData.PickingRange + 0.5;
	}

	public EnumPosFlag GetFreePos(string code)
	{
		Array values = Enum.GetValues(typeof(EnumPosFlag));
		int flags = 0;
		posFlagDict().TryGetValue(code, out flags);
		foreach (EnumPosFlag flag in values)
		{
			if ((int)((uint)flags & (uint)flag) <= 0)
			{
				return flag;
			}
		}
		return (EnumPosFlag)0;
	}

	public void OccupyPos(string code, EnumPosFlag pos)
	{
		int flags = 0;
		posFlagDict().TryGetValue(code, out flags);
		posFlagDict()[code] = flags | (int)pos;
	}

	public void FreePos(string code, EnumPosFlag pos)
	{
		int flags = 0;
		posFlagDict().TryGetValue(code, out flags);
		posFlagDict()[code] = flags & (int)(~pos);
	}

	private Dictionary<string, int> posFlagDict()
	{
		capi.ObjectCache.TryGetValue("dialogCount", out var valObj);
		Dictionary<string, int> val = valObj as Dictionary<string, int>;
		if (val == null)
		{
			val = (Dictionary<string, int>)(capi.ObjectCache["dialogCount"] = new Dictionary<string, int>());
		}
		return val;
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
