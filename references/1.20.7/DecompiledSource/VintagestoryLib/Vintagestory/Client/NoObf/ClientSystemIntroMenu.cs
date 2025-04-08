using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ClientSystemIntroMenu : ClientSystem
{
	private ICoreClientAPI capi;

	private LoadedTexture slideInTextBoxTexture;

	private float accum;

	private float pressAccum;

	private bool openkeydown;

	private bool discardkeydown;

	public override string Name => "intromenu";

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.HudElement;
	}

	public ClientSystemIntroMenu(ClientMain game)
		: base(game)
	{
	}

	public override void OnBlockTexturesLoaded()
	{
		capi = game.api;
	}

	private void onEventBus(string eventName, ref EnumHandling handling, IAttribute data)
	{
		if (eventName == "skipcharacterselection" || eventName == "finishcharacterselection")
		{
			bool saveExtendedDebugInfo = game.extendedDebugInfo;
			game.extendedDebugInfo = false;
			capi.Event.RegisterCallback(after15Secs, 15000);
			game.extendedDebugInfo = saveExtendedDebugInfo;
		}
	}

	private void after15Secs(float obj)
	{
		slideInTextBoxTexture = capi.Gui.TextTexture.GenTextTexture(Lang.Get("gameintrotip").Replace("\\n", "\n"), CairoFont.WhiteSmallText(), new TextBackground
		{
			FillColor = GuiStyle.DialogDefaultBgColor,
			BorderColor = GuiStyle.DialogBorderColor,
			BorderWidth = 1.0,
			Padding = 10
		});
	}

	public void OnRenderGUI(float deltaTime)
	{
		if (slideInTextBoxTexture == null)
		{
			return;
		}
		if (openkeydown || discardkeydown)
		{
			pressAccum += deltaTime;
		}
		else
		{
			pressAccum = Math.Max(0f, pressAccum - deltaTime);
		}
		if (pressAccum > 1f)
		{
			pressAccum = 1f;
			if (openkeydown)
			{
				game.LoadedGuis.FirstOrDefault((GuiDialog dlg) => dlg is GuiDialogFirstlaunchInfo).TryOpen();
			}
			slideInTextBoxTexture.Dispose();
			slideInTextBoxTexture = null;
		}
		else
		{
			accum += deltaTime;
			float px = GameMath.Clamp(1.5f * accum / 1f, 0f, 1f) - 1f;
			float dx = (float)(capi.World.Rand.NextDouble() - 0.5) * 6f * RuntimeEnv.GUIScale * pressAccum;
			float dy = (float)(capi.World.Rand.NextDouble() - 0.5) * 6f * RuntimeEnv.GUIScale * pressAccum;
			capi.Render.Render2DLoadedTexture(slideInTextBoxTexture, px * (float)slideInTextBoxTexture.Width + dx, 80f * RuntimeEnv.GUIScale + dy);
		}
	}

	public override void OnKeyDown(KeyEvent args)
	{
		base.OnKeyDown(args);
		if (slideInTextBoxTexture != null)
		{
			if (args.KeyCode == 93)
			{
				args.Handled = true;
				openkeydown = true;
			}
			if (args.KeyCode == 94)
			{
				args.Handled = true;
				discardkeydown = true;
			}
		}
	}

	public override void OnKeyUp(KeyEvent args)
	{
		base.OnKeyUp(args);
		if (slideInTextBoxTexture != null)
		{
			if (args.KeyCode == 93)
			{
				openkeydown = false;
			}
			if (args.KeyCode == 94)
			{
				discardkeydown = false;
			}
		}
	}

	internal override void OnLevelFinalize()
	{
		if (((capi.World as ClientMain).ServerInfo?.Playstyle == "creativebuilding" && ClientSettings.ShowCreativeHelpDialog) || ClientSettings.ShowSurvivalHelpDialog)
		{
			game.eventManager.RegisterRenderer(OnRenderGUI, EnumRenderStage.Ortho, Name, 1.0);
			capi.Event.RegisterEventBusListener(onEventBus);
		}
	}
}
