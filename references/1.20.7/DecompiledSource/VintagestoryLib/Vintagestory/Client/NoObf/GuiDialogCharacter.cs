using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class GuiDialogCharacter : GuiDialogCharacterBase
{
	protected IInventory characterInv;

	protected ElementBounds insetSlotBounds;

	protected float yaw = -1.2707963f;

	protected bool rotateCharacter;

	protected bool showArmorSlots = true;

	private int curTab;

	private List<GuiTab> tabs = new List<GuiTab>(new GuiTab[1]
	{
		new GuiTab
		{
			Name = Lang.Get("charactertab-character"),
			DataInt = 0
		}
	});

	public List<Action<GuiComposer>> rendertabhandlers = new List<Action<GuiComposer>>();

	private Size2d mainTabInnerSize = new Size2d();

	private Vec4f lighPos = new Vec4f(-1f, -1f, 0f, 0f).NormalizeXYZ();

	private Matrixf mat = new Matrixf();

	public override string ToggleKeyCombinationCode => "characterdialog";

	public override bool PrefersUngrabbedMouse => false;

	public override float ZSize => RuntimeEnv.GUIScale * 280f;

	public override List<GuiTab> Tabs => tabs;

	public override List<Action<GuiComposer>> RenderTabHandlers => rendertabhandlers;

	public override event Action ComposeExtraGuis;

	public override event Action<int> TabClicked;

	public GuiDialogCharacter(ICoreClientAPI capi)
		: base(capi)
	{
		rendertabhandlers.Add(ComposeCharacterTab);
	}

	private void registerArmorIcons()
	{
	}

	private void ComposeCharacterTab(GuiComposer compo)
	{
		if (!capi.Gui.Icons.CustomIcons.ContainsKey("armorhelmet"))
		{
			registerArmorIcons();
		}
		double pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
		ElementBounds leftSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0 + pad, 1, 6).FixedGrow(0.0, pad);
		ElementBounds leftArmorSlotBoundsHead = null;
		ElementBounds leftArmorSlotBoundsBody = null;
		ElementBounds leftArmorSlotBoundsLegs = null;
		if (showArmorSlots)
		{
			leftArmorSlotBoundsHead = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0 + pad, 1, 1).FixedGrow(0.0, pad);
			leftSlotBounds.FixedRightOf(leftArmorSlotBoundsHead, 10.0);
			leftArmorSlotBoundsBody = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0 + pad + 102.0, 1, 1).FixedGrow(0.0, pad);
			leftSlotBounds.FixedRightOf(leftArmorSlotBoundsBody, 10.0);
			leftArmorSlotBoundsLegs = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0 + pad + 204.0, 1, 1).FixedGrow(0.0, pad);
			leftSlotBounds.FixedRightOf(leftArmorSlotBoundsLegs, 10.0);
		}
		insetSlotBounds = ElementBounds.Fixed(0.0, 22.0 + pad, 190.0, leftSlotBounds.fixedHeight - 2.0 * pad - 4.0);
		insetSlotBounds.FixedRightOf(leftSlotBounds, 10.0);
		ElementBounds rightSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0 + pad, 1, 6).FixedGrow(0.0, pad);
		rightSlotBounds.FixedRightOf(insetSlotBounds, 10.0);
		leftSlotBounds.fixedHeight -= 6.0;
		rightSlotBounds.fixedHeight -= 6.0;
		compo.AddIf(showArmorSlots).AddItemSlotGrid(characterInv, SendInvPacket, 1, new int[1] { 12 }, leftArmorSlotBoundsHead, "armorSlotsHead").AddItemSlotGrid(characterInv, SendInvPacket, 1, new int[1] { 13 }, leftArmorSlotBoundsBody, "armorSlotsBody")
			.AddItemSlotGrid(characterInv, SendInvPacket, 1, new int[1] { 14 }, leftArmorSlotBoundsLegs, "armorSlotsLegs")
			.EndIf()
			.AddItemSlotGrid(characterInv, SendInvPacket, 1, new int[6] { 0, 1, 2, 11, 3, 4 }, leftSlotBounds, "leftSlots")
			.AddInset(insetSlotBounds, 0)
			.AddItemSlotGrid(characterInv, SendInvPacket, 1, new int[6] { 6, 7, 8, 10, 5, 9 }, rightSlotBounds, "rightSlots");
	}

	protected virtual void ComposeGuis()
	{
		characterInv = capi.World.Player.InventoryManager.GetOwnInventory("character");
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		if (curTab == 0)
		{
			bgBounds.BothSizing = ElementSizing.FitToChildren;
		}
		else
		{
			bgBounds.BothSizing = ElementSizing.Fixed;
			bgBounds.fixedWidth = mainTabInnerSize.Width;
			bgBounds.fixedHeight = mainTabInnerSize.Height;
		}
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);
		string charClass = capi.World.Player.Entity.WatchedAttributes.GetString("characterClass");
		string title = Lang.Get("characterdialog-title-nameandclass", capi.World.Player.PlayerName, Lang.Get("characterclass-" + charClass));
		if (!Lang.HasTranslation("characterclass-" + charClass))
		{
			title = capi.World.Player.PlayerName;
		}
		ElementBounds tabBounds = ElementBounds.Fixed(5.0, -24.0, 350.0, 25.0);
		ClearComposers();
		Composers["playercharacter"] = capi.Gui.CreateCompo("playercharacter", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(title, OnTitleBarClose)
			.AddHorizontalTabs(tabs.ToArray(), tabBounds, onTabClicked, CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold), CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold), "tabs")
			.BeginChildElements(bgBounds);
		Composers["playercharacter"].GetHorizontalTabs("tabs").activeElement = curTab;
		rendertabhandlers[curTab](Composers["playercharacter"]);
		Composers["playercharacter"].EndChildElements().Compose();
		if (ComposeExtraGuis != null)
		{
			ComposeExtraGuis();
		}
		if (curTab == 0)
		{
			mainTabInnerSize.Width = bgBounds.InnerWidth / (double)RuntimeEnv.GUIScale;
			mainTabInnerSize.Height = bgBounds.InnerHeight / (double)RuntimeEnv.GUIScale;
		}
	}

	private void onTabClicked(int tabindex)
	{
		TabClicked?.Invoke(tabindex);
		curTab = tabindex;
		ComposeGuis();
	}

	public override void OnMouseDown(MouseEvent args)
	{
		base.OnMouseDown(args);
		rotateCharacter = insetSlotBounds.PointInside(args.X, args.Y);
	}

	public override void OnMouseUp(MouseEvent args)
	{
		base.OnMouseUp(args);
		rotateCharacter = false;
	}

	public override void OnMouseMove(MouseEvent args)
	{
		base.OnMouseMove(args);
		if (rotateCharacter)
		{
			yaw -= (float)args.DeltaX / 100f;
		}
	}

	public override void OnRenderGUI(float deltaTime)
	{
		base.OnRenderGUI(deltaTime);
		if (curTab == 0)
		{
			capi.Render.GlPushMatrix();
			if (focused)
			{
				capi.Render.GlTranslate(0f, 0f, 150f);
			}
			double pad = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
			capi.Render.GlRotate(-14f, 1f, 0f, 0f);
			mat.Identity();
			mat.RotateXDeg(-14f);
			Vec4f lightRot = mat.TransformVector(lighPos);
			capi.Render.CurrentActiveShader.Uniform("lightPosition", new Vec3f(lightRot.X, lightRot.Y, lightRot.Z));
			capi.Render.RenderEntityToGui(deltaTime, capi.World.Player.Entity, insetSlotBounds.renderX + pad - GuiElement.scaled(41.0), insetSlotBounds.renderY + pad - GuiElement.scaled(30.0), GuiElement.scaled(250.0), yaw, (float)GuiElement.scaled(135.0), -1);
			capi.Render.GlPopMatrix();
			capi.Render.CurrentActiveShader.Uniform("lightPosition", new Vec3f(1f, -1f, 0f).Normalize());
			if (!insetSlotBounds.PointInside(capi.Input.MouseX, capi.Input.MouseY) && !rotateCharacter)
			{
				yaw += (float)(Math.Sin((float)capi.World.ElapsedMilliseconds / 1000f) / 200.0);
			}
		}
	}

	public override void OnGuiOpened()
	{
		ComposeGuis();
		if ((capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Guest || capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival) && characterInv != null)
		{
			characterInv.Open(capi.World.Player);
		}
	}

	public override void OnGuiClosed()
	{
		if (characterInv != null)
		{
			characterInv.Close(capi.World.Player);
			Composers["playercharacter"].GetSlotGrid("leftSlots")?.OnGuiClosed(capi);
			Composers["playercharacter"].GetSlotGrid("rightSlots")?.OnGuiClosed(capi);
		}
		curTab = 0;
	}

	protected void SendInvPacket(object packet)
	{
		capi.Network.SendPacketClient(packet);
	}
}
