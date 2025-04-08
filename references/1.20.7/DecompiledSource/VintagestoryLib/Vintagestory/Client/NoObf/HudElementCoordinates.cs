using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class HudElementCoordinates : HudElement
{
	public override string ToggleKeyCombinationCode => "coordinateshud";

	public HudElementCoordinates(ICoreClientAPI capi)
		: base(capi)
	{
	}

	public override void OnOwnPlayerDataReceived()
	{
		ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.None, 0.0, 0.0, 190.0, 48.0);
		ElementBounds overlayBounds = textBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0);
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightTop).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, GuiStyle.DialogToScreenPadding);
		base.SingleComposer = capi.Gui.CreateCompo("coordinateshud", dialogBounds).AddGameOverlay(overlayBounds).AddDynamicText("", CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center), textBounds, "text")
			.Compose();
		if (ClientSettings.ShowCoordinateHud)
		{
			TryOpen();
		}
	}

	public override void OnBlockTexturesLoaded()
	{
		base.OnBlockTexturesLoaded();
		if (!capi.World.Config.GetBool("allowCoordinateHud", defaultValue: true))
		{
			(capi.World as ClientMain).EnqueueMainThreadTask(delegate
			{
				(capi.World as ClientMain).UnregisterDialog(this);
				capi.Input.SetHotKeyHandler("coordinateshud", null);
				Dispose();
			}, "unreg");
			return;
		}
		capi.Event.RegisterGameTickListener(Every250ms, 250);
		ClientSettings.Inst.AddWatcher("showCoordinateHud", delegate(bool on)
		{
			if (on)
			{
				TryOpen();
			}
			else
			{
				TryClose();
			}
		});
	}

	private void Every250ms(float dt)
	{
		if (!IsOpened())
		{
			return;
		}
		BlockPos pos = capi.World.Player.Entity.Pos.AsBlockPos;
		int ypos = pos.Y;
		pos.Sub(capi.World.DefaultSpawnPosition.AsBlockPos);
		string facing = BlockFacing.HorizontalFromYaw(capi.World.Player.Entity.Pos.Yaw).ToString();
		facing = Lang.Get("facing-" + facing);
		string coords = pos.X + ", " + ypos + ", " + pos.Z + "\n" + facing;
		if (ClientSettings.ExtendedDebugInfo)
		{
			coords += facing switch
			{
				"North" => " / Z-", 
				"East" => " / X+", 
				"South" => " / Z+", 
				"West" => " / X-", 
				_ => string.Empty, 
			};
		}
		base.SingleComposer.GetDynamicText("text").SetNewText(coords);
		List<ElementBounds> boundsList = capi.Gui.GetDialogBoundsInArea(EnumDialogArea.RightTop);
		base.SingleComposer.Bounds.absOffsetY = GuiStyle.DialogToScreenPadding;
		for (int i = 0; i < boundsList.Count; i++)
		{
			if (boundsList[i] != base.SingleComposer.Bounds)
			{
				ElementBounds bounds = boundsList[i];
				base.SingleComposer.Bounds.absOffsetY = GuiStyle.DialogToScreenPadding + bounds.absY + bounds.OuterHeight;
				break;
			}
		}
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		ClientSettings.ShowCoordinateHud = true;
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		ClientSettings.ShowCoordinateHud = false;
	}

	public override void OnRenderGUI(float deltaTime)
	{
		base.OnRenderGUI(deltaTime);
	}
}
