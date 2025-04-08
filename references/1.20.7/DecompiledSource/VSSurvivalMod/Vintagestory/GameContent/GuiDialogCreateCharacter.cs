using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogCreateCharacter : GuiDialog
{
	private bool didSelect;

	protected IInventory characterInv;

	protected ElementBounds insetSlotBounds;

	private CharacterSystem modSys;

	private int currentClassIndex;

	private int curTab;

	private int rows = 7;

	private float charZoom = 1f;

	private bool charNaked = true;

	protected int dlgHeight = 513;

	protected float yaw = -1.2707963f;

	protected bool rotateCharacter;

	private Vec4f lighPos = new Vec4f(-1f, -1f, 0f, 0f).NormalizeXYZ();

	private Matrixf mat = new Matrixf();

	public override string ToggleKeyCombinationCode => null;

	public override bool PrefersUngrabbedMouse => true;

	public override float ZSize => (float)GuiElement.scaled(280.0);

	public GuiDialogCreateCharacter(ICoreClientAPI capi, CharacterSystem modSys)
		: base(capi)
	{
		this.modSys = modSys;
	}

	protected void ComposeGuis()
	{
		double pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
		double slotsize = GuiElementPassiveItemSlot.unscaledSlotSize;
		characterInv = capi.World.Player.InventoryManager.GetOwnInventory("character");
		ElementBounds tabBounds = ElementBounds.Fixed(0.0, -25.0, 450.0, 25.0);
		double ypos = 20.0 + pad;
		ElementBounds bgBounds = ElementBounds.FixedSize(717.0, dlgHeight).WithFixedPadding(GuiStyle.ElementToDialogPadding);
		ElementBounds dialogBounds = ElementBounds.FixedSize(757.0, dlgHeight + 40).WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);
		GuiTab[] tabs = new GuiTab[2]
		{
			new GuiTab
			{
				Name = Lang.Get("tab-skinandvoice"),
				DataInt = 0
			},
			new GuiTab
			{
				Name = Lang.Get("tab-charclass"),
				DataInt = 1
			}
		};
		GuiComposer createCharacterComposer = (Composers["createcharacter"] = capi.Gui.CreateCompo("createcharacter", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar((curTab == 0) ? Lang.Get("Customize Skin") : ((curTab == 1) ? Lang.Get("Select character class") : Lang.Get("Select your outfit")), OnTitleBarClose)
			.AddHorizontalTabs(tabs, tabBounds, onTabClicked, CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold), CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold), "tabs")
			.BeginChildElements(bgBounds));
		EntityBehaviorPlayerInventory bh = capi.World.Player.Entity.GetBehavior<EntityBehaviorPlayerInventory>();
		bh.hideClothing = false;
		if (curTab == 0)
		{
			EntityBehaviorExtraSkinnable skinMod = capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
			bh.hideClothing = charNaked;
			(capi.World.Player.Entity.Properties.Client.Renderer as EntityShapeRenderer).TesselateShape();
			CairoFont smallfont = CairoFont.WhiteSmallText();
			TextExtents textExt = smallfont.GetTextExtents(Lang.Get("Show dressed"));
			int colorIconSize = 22;
			ElementBounds leftColBounds2 = ElementBounds.Fixed(0.0, ypos, 204.0, dlgHeight - 59).FixedGrow(2.0 * pad, 2.0 * pad);
			insetSlotBounds = ElementBounds.Fixed(0.0, ypos + 2.0, 265.0, leftColBounds2.fixedHeight - 2.0 * pad - 10.0).FixedRightOf(leftColBounds2, 10.0);
			ElementBounds.Fixed(0.0, ypos, 54.0, dlgHeight - 59).FixedGrow(2.0 * pad, 2.0 * pad).FixedRightOf(insetSlotBounds, 10.0);
			ElementBounds toggleButtonBounds = ElementBounds.Fixed((double)(int)insetSlotBounds.fixedX + insetSlotBounds.fixedWidth / 2.0 - textExt.Width / (double)RuntimeEnv.GUIScale / 2.0 - 12.0, 0.0, textExt.Width / (double)RuntimeEnv.GUIScale + 1.0, textExt.Height / (double)RuntimeEnv.GUIScale).FixedUnder(insetSlotBounds, 4.0).WithAlignment(EnumDialogArea.LeftFixed)
				.WithFixedPadding(12.0, 6.0);
			ElementBounds bounds = null;
			ElementBounds prevbounds = null;
			double leftX = 0.0;
			SkinnablePart[] availableSkinParts = skinMod.AvailableSkinParts;
			foreach (SkinnablePart skinpart in availableSkinParts)
			{
				bounds = ElementBounds.Fixed(leftX, (prevbounds == null || prevbounds.fixedY == 0.0) ? (-10.0) : (prevbounds.fixedY + 8.0), colorIconSize, colorIconSize);
				string code = skinpart.Code;
				AppliedSkinnablePartVariant appliedVar = skinMod.AppliedSkinParts.FirstOrDefault((AppliedSkinnablePartVariant sp) => sp.PartCode == code);
				if (skinpart.Type == EnumSkinnableType.Texture && !skinpart.UseDropDown)
				{
					int selectedIndex = 0;
					int[] colors = new int[skinpart.Variants.Length];
					for (int j = 0; j < skinpart.Variants.Length; j++)
					{
						colors[j] = skinpart.Variants[j].Color;
						if (appliedVar?.Code == skinpart.Variants[j].Code)
						{
							selectedIndex = j;
						}
					}
					createCharacterComposer.AddRichtext(Lang.Get("skinpart-" + code), CairoFont.WhiteSmallText(), bounds = bounds.BelowCopy(0.0, 10.0).WithFixedSize(210.0, 22.0));
					createCharacterComposer.AddColorListPicker(colors, delegate(int index)
					{
						onToggleSkinPartColor(code, index);
					}, bounds = bounds.BelowCopy().WithFixedSize(colorIconSize, colorIconSize), 180, "picker-" + code);
					for (int i = 0; i < colors.Length; i++)
					{
						GuiElementColorListPicker colorListPicker = createCharacterComposer.GetColorListPicker("picker-" + code + "-" + i);
						colorListPicker.ShowToolTip = true;
						colorListPicker.TooltipText = Lang.Get("color-" + skinpart.Variants[i].Code);
					}
					createCharacterComposer.ColorListPickerSetValue("picker-" + code, selectedIndex);
				}
				else
				{
					int selectedIndex2 = 0;
					string[] names = new string[skinpart.Variants.Length];
					string[] values = new string[skinpart.Variants.Length];
					for (int k = 0; k < skinpart.Variants.Length; k++)
					{
						names[k] = Lang.Get("skinpart-" + code + "-" + skinpart.Variants[k].Code);
						values[k] = skinpart.Variants[k].Code;
						if (appliedVar?.Code == values[k])
						{
							selectedIndex2 = k;
						}
					}
					createCharacterComposer.AddRichtext(Lang.Get("skinpart-" + code), CairoFont.WhiteSmallText(), bounds = bounds.BelowCopy(0.0, 10.0).WithFixedSize(210.0, 22.0));
					string tooltip = Lang.GetIfExists("skinpartdesc-" + code);
					if (tooltip != null)
					{
						createCharacterComposer.AddHoverText(tooltip, CairoFont.WhiteSmallText(), 300, bounds = bounds.FlatCopy());
					}
					createCharacterComposer.AddDropDown(values, names, selectedIndex2, delegate(string variantcode, bool selected)
					{
						onToggleSkinPartColor(code, variantcode);
					}, bounds = bounds.BelowCopy().WithFixedSize(200.0, 25.0), "dropdown-" + code);
				}
				prevbounds = bounds.FlatCopy();
				if (skinpart.Colbreak)
				{
					leftX = insetSlotBounds.fixedX + insetSlotBounds.fixedWidth + 22.0;
					prevbounds.fixedY = 0.0;
				}
			}
			createCharacterComposer.AddInset(insetSlotBounds, 2).AddToggleButton(Lang.Get("Show dressed"), smallfont, OnToggleDressOnOff, toggleButtonBounds, "showdressedtoggle").AddButton(Lang.Get("Randomize"), () => OnRandomizeSkin(new Dictionary<string, string>()), ElementBounds.Fixed(0, dlgHeight - 25).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(8.0, 6.0), CairoFont.WhiteSmallText(), EnumButtonStyle.Small)
				.AddIf(capi.Settings.String.Exists("lastSkinSelection"))
				.AddButton(Lang.Get("Last selection"), () => OnRandomizeSkin(modSys.getPreviousSelection()), ElementBounds.Fixed(130, dlgHeight - 25).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(8.0, 6.0), CairoFont.WhiteSmallText(), EnumButtonStyle.Small)
				.EndIf()
				.AddSmallButton(Lang.Get("Confirm Skin"), OnNext, ElementBounds.Fixed(0, dlgHeight - 25).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(12.0, 6.0));
			createCharacterComposer.GetToggleButton("showdressedtoggle").SetValue(!charNaked);
		}
		if (curTab == 1)
		{
			(capi.World.Player.Entity.Properties.Client.Renderer as EntityShapeRenderer).TesselateShape();
			ypos -= 10.0;
			ElementBounds leftColBounds = ElementBounds.Fixed(0.0, ypos, 0.0, dlgHeight - 47).FixedGrow(2.0 * pad, 2.0 * pad);
			insetSlotBounds = ElementBounds.Fixed(0.0, ypos + 25.0, 190.0, leftColBounds.fixedHeight - 2.0 * pad + 10.0).FixedRightOf(leftColBounds, 10.0);
			ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, ypos, 1, rows).FixedGrow(2.0 * pad, 2.0 * pad).FixedRightOf(insetSlotBounds, 10.0);
			ElementBounds prevButtonBounds = ElementBounds.Fixed(0.0, ypos + 25.0, 35.0, slotsize - 4.0).WithFixedPadding(2.0).FixedRightOf(insetSlotBounds, 20.0);
			ElementBounds centerTextBounds = ElementBounds.Fixed(0.0, ypos + 25.0, 200.0, slotsize - 4.0 - 8.0).FixedRightOf(prevButtonBounds, 20.0);
			ElementBounds charclasssInset = centerTextBounds.ForkBoundingParent(4.0, 4.0, 4.0, 4.0);
			ElementBounds nextButtonBounds = ElementBounds.Fixed(0.0, ypos + 25.0, 35.0, slotsize - 4.0).WithFixedPadding(2.0).FixedRightOf(charclasssInset, 20.0);
			CairoFont font = CairoFont.WhiteMediumText();
			centerTextBounds.fixedY += (centerTextBounds.fixedHeight - font.GetFontExtents().Height / (double)RuntimeEnv.GUIScale) / 2.0;
			ElementBounds charTextBounds = ElementBounds.Fixed(0.0, 0.0, 480.0, 100.0).FixedUnder(prevButtonBounds, 20.0).FixedRightOf(insetSlotBounds, 20.0);
			createCharacterComposer.AddInset(insetSlotBounds, 2).AddIconButton("left", delegate
			{
				changeClass(-1);
			}, prevButtonBounds.FlatCopy()).AddInset(charclasssInset, 2)
				.AddDynamicText("Commoner", font.Clone().WithOrientation(EnumTextOrientation.Center), centerTextBounds, "className")
				.AddIconButton("right", delegate
				{
					changeClass(1);
				}, nextButtonBounds.FlatCopy())
				.AddRichtext("", CairoFont.WhiteDetailText(), charTextBounds, "characterDesc")
				.AddSmallButton(Lang.Get("Confirm Class"), OnConfirm, ElementBounds.Fixed(0, dlgHeight - 30).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(12.0, 6.0));
			changeClass(0);
		}
		GuiElementHorizontalTabs horizontalTabs = createCharacterComposer.GetHorizontalTabs("tabs");
		horizontalTabs.unscaledTabSpacing = 20.0;
		horizontalTabs.unscaledTabPadding = 10.0;
		horizontalTabs.activeElement = curTab;
		createCharacterComposer.Compose();
	}

	private bool OnRandomizeSkin(Dictionary<string, string> preselection)
	{
		EntityPlayer entity = capi.World.Player.Entity;
		EntityBehaviorPlayerInventory bh = capi.World.Player.Entity.GetBehavior<EntityBehaviorPlayerInventory>();
		bh.doReloadShapeAndSkin = false;
		modSys.randomizeSkin(entity, preselection);
		EntityBehaviorExtraSkinnable skinMod = entity.GetBehavior<EntityBehaviorExtraSkinnable>();
		foreach (AppliedSkinnablePartVariant appliedPart in skinMod.AppliedSkinParts)
		{
			string partcode = appliedPart.PartCode;
			SkinnablePart skinPart = skinMod.AvailableSkinParts.FirstOrDefault((SkinnablePart part) => part.Code == partcode);
			int index = skinPart.Variants.IndexOf((SkinnablePartVariant part) => part.Code == appliedPart.Code);
			if (skinPart.Type == EnumSkinnableType.Texture && !skinPart.UseDropDown)
			{
				Composers["createcharacter"].ColorListPickerSetValue("picker-" + partcode, index);
			}
			else
			{
				Composers["createcharacter"].GetDropDown("dropdown-" + partcode).SetSelectedIndex(index);
			}
		}
		bh.doReloadShapeAndSkin = true;
		reTesselate();
		return true;
	}

	private void OnToggleDressOnOff(bool on)
	{
		charNaked = !on;
		capi.World.Player.Entity.GetBehavior<EntityBehaviorPlayerInventory>().hideClothing = charNaked;
		reTesselate();
	}

	private void onToggleSkinPartColor(string partCode, string variantCode)
	{
		capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>().selectSkinPart(partCode, variantCode);
	}

	private void onToggleSkinPartColor(string partCode, int index)
	{
		EntityBehaviorExtraSkinnable behavior = capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
		string variantCode = behavior.AvailableSkinPartsByCode[partCode].Variants[index].Code;
		behavior.selectSkinPart(partCode, variantCode);
	}

	private bool OnNext()
	{
		curTab = 1;
		ComposeGuis();
		return true;
	}

	private void onTabClicked(int tabid)
	{
		curTab = tabid;
		ComposeGuis();
	}

	public override void OnGuiOpened()
	{
		string charclass = capi.World.Player.Entity.WatchedAttributes.GetString("characterClass");
		if (charclass != null)
		{
			modSys.setCharacterClass(capi.World.Player.Entity, charclass);
		}
		else
		{
			modSys.setCharacterClass(capi.World.Player.Entity, modSys.characterClasses[0].Code);
		}
		ComposeGuis();
		(capi.World.Player.Entity.Properties.Client.Renderer as EntityShapeRenderer).TesselateShape();
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
			Composers["createcharacter"].GetSlotGrid("leftSlots")?.OnGuiClosed(capi);
			Composers["createcharacter"].GetSlotGrid("rightSlots")?.OnGuiClosed(capi);
		}
		CharacterClass chclass = modSys.characterClasses[currentClassIndex];
		modSys.ClientSelectionDone(characterInv, chclass.Code, didSelect);
		capi.World.Player.Entity.GetBehavior<EntityBehaviorPlayerInventory>().hideClothing = false;
		reTesselate();
	}

	private bool OnConfirm()
	{
		didSelect = true;
		TryClose();
		return true;
	}

	protected virtual void OnTitleBarClose()
	{
		TryClose();
	}

	protected void SendInvPacket(object packet)
	{
		capi.Network.SendPacketClient(packet);
	}

	private void changeClass(int dir)
	{
		currentClassIndex = GameMath.Mod(currentClassIndex + dir, modSys.characterClasses.Count);
		CharacterClass chclass = modSys.characterClasses[currentClassIndex];
		Composers["createcharacter"].GetDynamicText("className").SetNewText(Lang.Get("characterclass-" + chclass.Code));
		StringBuilder fulldesc = new StringBuilder();
		StringBuilder attributes = new StringBuilder();
		fulldesc.AppendLine(Lang.Get("characterdesc-" + chclass.Code));
		fulldesc.AppendLine();
		fulldesc.AppendLine(Lang.Get("traits-title"));
		foreach (Trait trait2 in from code in chclass.Traits
			select modSys.TraitsByCode[code] into trait
			orderby (int)trait.Type
			select trait)
		{
			attributes.Clear();
			foreach (KeyValuePair<string, double> val in trait2.Attributes)
			{
				if (attributes.Length > 0)
				{
					attributes.Append(", ");
				}
				attributes.Append(Lang.Get(string.Format(GlobalConstants.DefaultCultureInfo, "charattribute-{0}-{1}", val.Key, val.Value)));
			}
			if (attributes.Length > 0)
			{
				fulldesc.AppendLine(Lang.Get("traitwithattributes", Lang.Get("trait-" + trait2.Code), attributes));
				continue;
			}
			string desc = Lang.GetIfExists("traitdesc-" + trait2.Code);
			if (desc != null)
			{
				fulldesc.AppendLine(Lang.Get("traitwithattributes", Lang.Get("trait-" + trait2.Code), desc));
			}
			else
			{
				fulldesc.AppendLine(Lang.Get("trait-" + trait2.Code));
			}
		}
		if (chclass.Traits.Length == 0)
		{
			fulldesc.AppendLine(Lang.Get("No positive or negative traits"));
		}
		Composers["createcharacter"].GetRichtext("characterDesc").SetNewText(fulldesc.ToString(), CairoFont.WhiteDetailText());
		modSys.setCharacterClass(capi.World.Player.Entity, chclass.Code);
		reTesselate();
	}

	protected void reTesselate()
	{
		(capi.World.Player.Entity.Properties.Client.Renderer as EntityShapeRenderer).TesselateShape();
	}

	public void PrepAndOpen()
	{
		TryOpen();
	}

	public override bool CaptureAllInputs()
	{
		return IsOpened();
	}

	public override void OnMouseWheel(MouseWheelEventArgs args)
	{
		base.OnMouseWheel(args);
		if (insetSlotBounds.PointInside(capi.Input.MouseX, capi.Input.MouseY) && curTab == 0)
		{
			charZoom = GameMath.Clamp(charZoom + args.deltaPrecise / 5f, 0.5f, 1f);
		}
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
		if (capi.IsGamePaused)
		{
			capi.World.Player.Entity.talkUtil.OnGameTick(deltaTime);
		}
		capi.Render.GlPushMatrix();
		if (focused)
		{
			capi.Render.GlTranslate(0f, 0f, 150f);
		}
		capi.Render.GlRotate(-14f, 1f, 0f, 0f);
		mat.Identity();
		mat.RotateXDeg(-14f);
		Vec4f lightRot = mat.TransformVector(lighPos);
		double pad = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
		capi.Render.CurrentActiveShader.Uniform("lightPosition", new Vec3f(lightRot.X, lightRot.Y, lightRot.Z));
		capi.Render.PushScissor(insetSlotBounds);
		if (curTab == 0)
		{
			capi.Render.RenderEntityToGui(deltaTime, capi.World.Player.Entity, insetSlotBounds.renderX + pad - GuiElement.scaled(195.0) * (double)charZoom + GuiElement.scaled(115f * (1f - charZoom)), insetSlotBounds.renderY + pad + GuiElement.scaled(10f * (1f - charZoom)), (float)GuiElement.scaled(230.0), yaw, (float)GuiElement.scaled(330f * charZoom), -1);
		}
		else
		{
			capi.Render.RenderEntityToGui(deltaTime, capi.World.Player.Entity, insetSlotBounds.renderX + pad - GuiElement.scaled(110.0), insetSlotBounds.renderY + pad - GuiElement.scaled(15.0), (float)GuiElement.scaled(230.0), yaw, (float)GuiElement.scaled(205.0), -1);
		}
		capi.Render.PopScissor();
		capi.Render.CurrentActiveShader.Uniform("lightPosition", new Vec3f(1f, -1f, 0f).Normalize());
		capi.Render.GlPopMatrix();
	}
}
