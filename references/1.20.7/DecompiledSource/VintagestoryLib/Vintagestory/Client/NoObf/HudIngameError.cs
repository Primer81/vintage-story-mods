using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class HudIngameError : HudElement
{
	private long errorTextActiveMs;

	private double x;

	private double y;

	private GuiElementHoverText elem;

	public override double InputOrder => 1.0;

	public override string ToggleKeyCombinationCode => null;

	public override bool Focusable => false;

	public HudIngameError(ICoreClientAPI capi)
		: base(capi)
	{
		capi.Event.InGameError += Event_InGameError;
		capi.Event.RegisterGameTickListener(OnGameTick, 20);
	}

	private void Event_InGameError(object sender, string errorCode, string text)
	{
		if (elem != null)
		{
			errorTextActiveMs = capi.InWorldEllapsedMilliseconds;
			elem.SetNewText(text);
			elem.SetVisible(on: true);
			x = elem.Bounds.absFixedX;
			y = elem.Bounds.absFixedX;
		}
	}

	private void OnGameTick(float dt)
	{
		if (errorTextActiveMs != 0L)
		{
			if (capi.InWorldEllapsedMilliseconds - errorTextActiveMs > 5000)
			{
				errorTextActiveMs = 0L;
				elem.SetVisible(on: false);
			}
			if (capi.InWorldEllapsedMilliseconds - errorTextActiveMs < 500)
			{
				float intensity = Math.Min(0.25f, 1f - (float)(capi.InWorldEllapsedMilliseconds - errorTextActiveMs) / 500f) * RuntimeEnv.GUIScale;
				Composers["ingameerror"].Bounds.absFixedX = x + (double)intensity * (capi.World.Rand.NextDouble() * 10.0 - 5.0);
				Composers["ingameerror"].Bounds.absFixedY = y + (double)intensity * (capi.World.Rand.NextDouble() * 10.0 - 5.0);
			}
			else
			{
				Composers["ingameerror"].Bounds.absFixedX = x;
				Composers["ingameerror"].Bounds.absFixedY = y;
			}
		}
	}

	public override void OnOwnPlayerDataReceived()
	{
		ComposeGuis();
	}

	public void ComposeGuis()
	{
		ElementBounds dialogBounds = new ElementBounds
		{
			Alignment = EnumDialogArea.CenterBottom,
			BothSizing = ElementSizing.Fixed,
			fixedWidth = 600.0,
			fixedHeight = 5.0
		};
		ElementBounds iteminfoBounds = ElementBounds.Fixed(0.0, -155.0, 600.0, 30.0);
		ClearComposers();
		CairoFont font = CairoFont.WhiteSmallText().WithColor(GuiStyle.ErrorTextColor).WithStroke(GuiStyle.DialogBorderColor, 2.0)
			.WithOrientation(EnumTextOrientation.Center);
		Composers["ingameerror"] = capi.Gui.CreateCompo("ingameerror", dialogBounds.FlatCopy()).BeginChildElements(dialogBounds).AddTranspHoverText("", font, 600, iteminfoBounds, "errortext")
			.EndChildElements()
			.Compose();
		elem = Composers["ingameerror"].GetHoverText("errortext");
		elem.ZPosition = 100f;
		elem.SetFollowMouse(on: false);
		elem.SetAutoWidth(on: false);
		elem.SetAutoDisplay(on: false);
		elem.fillBounds = true;
		TryOpen();
	}

	public override bool TryClose()
	{
		return false;
	}

	public override bool ShouldReceiveKeyboardEvents()
	{
		return true;
	}

	public override void OnRenderGUI(float deltaTime)
	{
		base.OnRenderGUI(deltaTime);
	}

	protected override void OnFocusChanged(bool on)
	{
	}
}
