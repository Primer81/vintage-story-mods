using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf;

public class SystemPlayerControl : ClientSystem
{
	private int forwardKey;

	private int backwardKey;

	private int leftKey;

	private int rightKey;

	private int jumpKey;

	private int sneakKey;

	private int sprintKey;

	private int sittingKey;

	private int ctrlKey;

	private int shiftKey;

	private bool nowFloorSitting;

	private EntityControls prevControls;

	public override string Name => "plco";

	public SystemPlayerControl(ClientMain game)
		: base(game)
	{
		base.game.RegisterGameTickListener(OnGameTick, 20);
		ClientSettings.Inst.AddKeyCombinationUpdatedWatcher(delegate
		{
			LoadKeyCodes();
		});
		LoadKeyCodes();
		prevControls = new EntityControls();
	}

	private void LoadKeyCodes()
	{
		forwardKey = ScreenManager.hotkeyManager.HotKeys["walkforward"].CurrentMapping.KeyCode;
		backwardKey = ScreenManager.hotkeyManager.HotKeys["walkbackward"].CurrentMapping.KeyCode;
		leftKey = ScreenManager.hotkeyManager.HotKeys["walkleft"].CurrentMapping.KeyCode;
		rightKey = ScreenManager.hotkeyManager.HotKeys["walkright"].CurrentMapping.KeyCode;
		sneakKey = ScreenManager.hotkeyManager.HotKeys["sneak"].CurrentMapping.KeyCode;
		sprintKey = ScreenManager.hotkeyManager.HotKeys["sprint"].CurrentMapping.KeyCode;
		jumpKey = ScreenManager.hotkeyManager.HotKeys["jump"].CurrentMapping.KeyCode;
		sittingKey = ScreenManager.hotkeyManager.HotKeys["sitdown"].CurrentMapping.KeyCode;
		ctrlKey = ScreenManager.hotkeyManager.HotKeys["ctrl"].CurrentMapping.KeyCode;
		shiftKey = ScreenManager.hotkeyManager.HotKeys["shift"].CurrentMapping.KeyCode;
	}

	public override void OnKeyDown(KeyEvent args)
	{
		EntityPlayer entity = game.EntityPlayer;
		if (args.KeyCode == sittingKey && !entity.Controls.TriesToMove && !entity.Controls.IsFlying)
		{
			nowFloorSitting = !entity.Controls.FloorSitting;
		}
		if (args.KeyCode == jumpKey || args.KeyCode == forwardKey || args.KeyCode == backwardKey || args.KeyCode == leftKey || args.KeyCode == rightKey)
		{
			nowFloorSitting = false;
		}
	}

	public void OnGameTick(float dt)
	{
		EntityControls controls = ((game.EntityPlayer.MountedOn == null) ? game.EntityPlayer.Controls : game.EntityPlayer.MountedOn.Controls);
		if (controls != null)
		{
			game.EntityPlayer.Controls.OnAction = game.api.inputapi.TriggerInWorldAction;
			bool allMovementCaptured = game.MouseGrabbed || (game.api.Settings.Bool["immersiveMouseMode"] && game.OpenedGuis.All((GuiDialog gui) => !gui.PrefersUngrabbedMouse));
			controls.Forward = game.KeyboardState[forwardKey];
			controls.Backward = game.KeyboardState[backwardKey];
			controls.Left = game.KeyboardState[leftKey];
			controls.Right = game.KeyboardState[rightKey];
			controls.Jump =
				game.KeyboardState[jumpKey] &&
				allMovementCaptured &&
				(game.EntityPlayer.PrevFrameCanStandUp ||
					game.player.worlddata.NoClip);
			controls.Sneak = game.KeyboardState[sneakKey] && allMovementCaptured;
			bool wasSprint = controls.Sprint;
			controls.Sprint =
				(game.KeyboardState[sprintKey] ||
					(wasSprint &&
					controls.TriesToMove &&
					ClientSettings.ToggleSprint)) &&
				allMovementCaptured;
			controls.CtrlKey = game.KeyboardState[ctrlKey];
			controls.ShiftKey = game.KeyboardState[shiftKey];
			controls.DetachedMode =
				game.player.worlddata.FreeMove ||
				game.EntityPlayer.IsEyesSubmerged();
			controls.FlyPlaneLock = game.player.worlddata.FreeMovePlaneLock;
			controls.Up = controls.DetachedMode && controls.Jump;
			controls.Down = controls.DetachedMode && controls.Sneak;
			controls.MovespeedMultiplier = game.player.worlddata.MoveSpeedMultiplier;
			controls.IsFlying = game.player.worlddata.FreeMove;
			controls.NoClip = game.player.worlddata.NoClip;
			controls.LeftMouseDown = game.InWorldMouseState.Left;
			controls.RightMouseDown = game.InWorldMouseState.Right;
			controls.FloorSitting = nowFloorSitting;
			SendServerPackets(prevControls, controls);
		}
	}

	private void SendServerPackets(EntityControls before, EntityControls now)
	{
		for (int i = 0; i < before.Flags.Length; i++)
		{
			if (before.Flags[i] != now.Flags[i])
			{
				game.SendPacketClient(new Packet_Client
				{
					Id = 21,
					MoveKeyChange = new Packet_MoveKeyChange
					{
						Down = (now.Flags[i] ? 1 : 0),
						Key = i
					}
				});
				before.Flags[i] = now.Flags[i];
			}
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
