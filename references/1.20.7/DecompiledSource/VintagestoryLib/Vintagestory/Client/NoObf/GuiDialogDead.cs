using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class GuiDialogDead : GuiDialog
{
	private ClientMain game;

	private bool respawning;

	private int livesLeft = -1;

	private float secondsDead;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogDead(ICoreClientAPI capi)
		: base(capi)
	{
		game = capi.World as ClientMain;
		game.RegisterGameTickListener(OnGameTick, 250);
	}

	private void OnGameTick(float dt)
	{
		if (!game.EntityPlayer.Alive)
		{
			secondsDead += dt;
		}
		else
		{
			secondsDead = 0f;
		}
		if (secondsDead >= 2.5f && !game.EntityPlayer.Alive && !IsOpened())
		{
			int lives = game.Config.GetString("playerlives", "-1").ToInt(-1);
			livesLeft = lives - game.Player.WorldData.Deaths;
			ComposeDialog();
			TryOpen();
		}
		if (IsOpened() && game.EntityPlayer.Alive)
		{
			respawning = false;
			TryClose();
		}
	}

	private void ComposeDialog()
	{
		ClearComposers();
		Composers["backgroundd"] = game.GuiComposers.Create("deadbg", ElementBounds.Fill).AddGrayBG(ElementBounds.Fill).Compose();
		string deadMsg = Lang.Get("Congratulations, you died!");
		if (livesLeft > 0)
		{
			deadMsg = Lang.Get("Congratulations, you died! {0} lives left.", livesLeft);
		}
		if (livesLeft == 0)
		{
			deadMsg = Lang.Get("Congratulations, you died! Forever!");
		}
		string respText = (respawning ? Lang.Get("Respawning...") : Lang.Get("Respawn"));
		double buttonWidth = 300.0;
		Composers["menu"] = game.GuiComposers.Create("deadmenu", ElementStdBounds.AutosizedMainDialog.WithFixedAlignmentOffset(0.0, 40.0)).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(0.0, GuiStyle.ElementToDialogPadding), withTitleBar: false).BeginChildElements()
			.AddStaticText(deadMsg, CairoFont.WhiteSmallishText(), EnumTextOrientation.Center, ElementStdBounds.MenuButton(0f).WithFixedWidth(350.0))
			.AddIf(livesLeft != 0)
			.AddButton(respText, OnRespawn, ElementStdBounds.MenuButton(1f).WithFixedWidth(buttonWidth), EnumButtonStyle.Normal, "respawnbtn")
			.EndIf()
			.AddIf(livesLeft == 0 && game.IsSingleplayer)
			.AddButton(Lang.Get("Delete World"), OnDeleteWorld, ElementStdBounds.MenuButton(1f).WithFixedWidth(buttonWidth), EnumButtonStyle.Normal, "deletebtn")
			.EndIf()
			.AddButton(Lang.Get("Rage Quit"), OnLeaveWorld, ElementStdBounds.MenuButton(2f).WithFixedWidth(buttonWidth))
			.EndChildElements()
			.Compose();
		if (Composers["menu"].GetButton("respawnbtn") != null)
		{
			Composers["menu"].GetButton("respawnbtn").Enabled = !respawning;
		}
	}

	private bool OnDeleteWorld()
	{
		game.SendLeave(0);
		game.exitReason = "delete world button pressed";
		game.deleteWorld = true;
		game.DestroyGameSession(gotDisconnected: false);
		return true;
	}

	private bool OnLeaveWorld()
	{
		game.SendLeave(0);
		game.exitReason = "rage quit button pressed";
		game.DestroyGameSession(gotDisconnected: false);
		return true;
	}

	private bool OnRespawn()
	{
		respawning = true;
		ComposeDialog();
		game.Respawn();
		return true;
	}

	public override bool CaptureAllInputs()
	{
		return IsOpened();
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		if (Composers["menu"].GetButton("respawnbtn") != null)
		{
			Composers["menu"].GetButton("respawnbtn").Enabled = true;
		}
		game.ShouldRender2DOverlays = true;
	}

	public override bool TryClose()
	{
		if (!game.EntityPlayer.Alive)
		{
			return false;
		}
		return base.TryClose();
	}
}
