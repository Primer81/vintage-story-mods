using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class DrawWorldInteractionUtil
{
	private ICoreClientAPI capi;

	private GuiDialog.DlgComposers Composers;

	public double ActualWidth;

	private string composerKeyCode;

	public double UnscaledLineHeight = 30.0;

	public float FontSize = 20f;

	private GuiComposer composer;

	public Vec4f Color = ColorUtil.WhiteArgbVec;

	public GuiComposer Composer => Composers[composerKeyCode];

	public DrawWorldInteractionUtil(ICoreClientAPI capi, GuiDialog.DlgComposers composers, string composerSuffixCode)
	{
		this.capi = capi;
		Composers = composers;
		composerKeyCode = "worldInteractionHelp" + composerSuffixCode;
	}

	public void ComposeBlockWorldInteractionHelp(WorldInteraction[] wis)
	{
		if (wis == null || wis.Length == 0)
		{
			Composers.Remove(composerKeyCode);
			return;
		}
		capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-1");
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
		if (composer == null)
		{
			composer = capi.Gui.CreateCompo(composerKeyCode, dialogBounds);
		}
		else
		{
			composer.Clear(dialogBounds);
		}
		Composers[composerKeyCode] = composer;
		double lineHeight = GuiElement.scaled(UnscaledLineHeight);
		int i = 0;
		foreach (WorldInteraction wi in wis)
		{
			ItemStack[] stacks = wi.Itemstacks;
			if (stacks != null && wi.GetMatchingStacks != null)
			{
				stacks = wi.GetMatchingStacks(wi, capi.World.Player.CurrentBlockSelection, capi.World.Player.CurrentEntitySelection);
				if (stacks == null || stacks.Length == 0)
				{
					continue;
				}
			}
			if (stacks != null || wi.ShouldApply == null || wi.ShouldApply(wi, capi.World.Player.CurrentBlockSelection, capi.World.Player.CurrentEntitySelection))
			{
				double yOffset = (double)i * (UnscaledLineHeight + 8.0);
				ElementBounds textBounds = ElementBounds.Fixed(0.0, yOffset, 600.0, 80.0);
				composer.AddIf(stacks != null && stacks.Length != 0).AddCustomRender(textBounds.FlatCopy(), delegate(float dt, ElementBounds bounds)
				{
					long num = capi.World.ElapsedMilliseconds / 1000 % stacks.Length;
					float size = (float)lineHeight * 0.8f;
					capi.Render.RenderItemstackToGui(new DummySlot(stacks[num]), bounds.renderX + lineHeight / 2.0 + 1.0, bounds.renderY + lineHeight / 2.0, 100.0, size, ColorUtil.ColorFromRgba(Color));
				}).EndIf()
					.AddStaticCustomDraw(textBounds, delegate(Context ctx, ImageSurface surface, ElementBounds bounds)
					{
						drawHelp(ctx, surface, bounds, stacks, lineHeight, wi);
					});
				i++;
			}
		}
		capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2");
		if (i == 0)
		{
			Composers.Remove(composerKeyCode);
			return;
		}
		composer.Compose();
		capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-3");
	}

	public void drawHelp(Context ctx, ImageSurface surface, ElementBounds currentBounds, ItemStack[] stacks, double lineheight, WorldInteraction wi)
	{
		capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2.1");
		double x = 0.0;
		double y = currentBounds.drawY;
		double[] color = (double[])GuiStyle.DialogDefaultTextColor.Clone();
		color[0] = (color[0] + 1.0) / 2.0;
		color[1] = (color[1] + 1.0) / 2.0;
		color[2] = (color[2] + 1.0) / 2.0;
		CairoFont font = CairoFont.WhiteMediumText().WithColor(color).WithFontSize(FontSize)
			.WithStroke(GuiStyle.DarkBrownColor, 2.0);
		font.SetupContext(ctx);
		double textHeight = font.GetFontExtents().Height;
		double symbolspacing = 5.0;
		double pluswdith = font.GetTextExtents("+").Width;
		if ((stacks != null && stacks.Length != 0) || wi.RequireFreeHand)
		{
			GuiElement.RoundRectangle(ctx, x, y + 1.0, lineheight, lineheight, 3.5);
			ctx.SetSourceRGBA(color);
			ctx.LineWidth = 1.5;
			ctx.StrokePreserve();
			ctx.SetSourceRGBA(new double[4] { 1.0, 1.0, 1.0, 0.5 });
			ctx.Fill();
			ctx.SetSourceRGBA(new double[4] { 1.0, 1.0, 1.0, 1.0 });
			x += lineheight + symbolspacing + 1.0;
		}
		List<HotKey> hotkeys = new List<HotKey>();
		if (wi.HotKeyCodes != null)
		{
			string[] hotKeyCodes = wi.HotKeyCodes;
			foreach (string keycode in hotKeyCodes)
			{
				HotKey hk6 = capi.Input.GetHotKeyByCode(keycode);
				if (hk6 != null)
				{
					hotkeys.Add(hk6);
				}
			}
		}
		else
		{
			HotKey hk7 = capi.Input.GetHotKeyByCode(wi.HotKeyCode);
			if (hk7 != null)
			{
				hotkeys.Add(hk7);
			}
		}
		foreach (HotKey hk5 in hotkeys)
		{
			if (!(hk5.Code != "ctrl") || hk5.CurrentMapping.Ctrl)
			{
				x = DrawHotkey(hk5, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, color);
			}
		}
		foreach (HotKey hk4 in hotkeys)
		{
			if (!(hk4.Code != "shift") || hk4.CurrentMapping.Shift)
			{
				x = DrawHotkey(hk4, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, color);
			}
		}
		foreach (HotKey hk3 in hotkeys)
		{
			if (!(hk3.Code == "shift") && !(hk3.Code == "ctrl") && !hk3.CurrentMapping.Shift && !hk3.CurrentMapping.Ctrl)
			{
				x = DrawHotkey(hk3, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, color);
			}
		}
		if (wi.MouseButton == EnumMouseButton.Left)
		{
			HotKey hk2 = capi.Input.GetHotKeyByCode("primarymouse");
			x = DrawHotkey(hk2, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, color);
		}
		if (wi.MouseButton == EnumMouseButton.Right)
		{
			HotKey hk = capi.Input.GetHotKeyByCode("secondarymouse");
			x = DrawHotkey(hk, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, color);
		}
		capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2.2");
		string text = ": " + Lang.Get(wi.ActionLangCode);
		capi.Gui.Text.DrawTextLine(ctx, font, text, x - 4.0, y + (lineheight - textHeight) / 2.0 + 2.0);
		ActualWidth = x + font.GetTextExtents(text).Width;
		capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2.3");
	}

	private double DrawHotkey(HotKey hk, double x, double y, Context ctx, CairoFont font, double lineheight, double textHeight, double pluswdith, double symbolspacing, double[] color)
	{
		KeyCombination map = hk.CurrentMapping;
		if (map.IsMouseButton(map.KeyCode))
		{
			return DrawMouseButton(map.KeyCode - 240, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, color);
		}
		if (map.Ctrl)
		{
			x = HotkeyComponent.DrawHotkey(capi, GlKeyNames.ToString(GlKeys.LControl), x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 10.0, color);
		}
		if (map.Shift)
		{
			x = HotkeyComponent.DrawHotkey(capi, GlKeyNames.ToString(GlKeys.LShift), x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 10.0, color);
		}
		x = HotkeyComponent.DrawHotkey(capi, map.PrimaryAsString(), x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 10.0, color);
		return x;
	}

	private double DrawMouseButton(int button, double x, double y, Context ctx, CairoFont font, double lineheight, double textHeight, double pluswdith, double symbolspacing, double[] color)
	{
		object obj;
		switch (button)
		{
		case 0:
		case 2:
			if (x > 0.0)
			{
				capi.Gui.Text.DrawTextLine(ctx, font, "+", (double)(int)x + symbolspacing, y + (double)(int)((lineheight - textHeight) / 2.0) + 2.0);
				x += pluswdith + 2.0 * symbolspacing;
			}
			capi.Gui.Icons.DrawIcon(ctx, (button == 0) ? "leftmousebutton" : "rightmousebutton", x, y + 1.0, lineheight, lineheight, color);
			return x + lineheight + symbolspacing + 1.0;
		default:
			obj = "b" + button;
			break;
		case 1:
			obj = "mb";
			break;
		}
		string text = (string)obj;
		return HotkeyComponent.DrawHotkey(capi, text, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 8.0, color);
	}

	public void Dispose()
	{
		composer?.Dispose();
	}
}
