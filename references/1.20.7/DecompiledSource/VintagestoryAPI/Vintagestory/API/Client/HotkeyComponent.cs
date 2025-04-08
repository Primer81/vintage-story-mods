using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class HotkeyComponent : RichTextComponent
{
	private LoadedTexture hotkeyTexture;

	private HotKey hotkey;

	public HotkeyComponent(ICoreClientAPI api, string hotkeycode, CairoFont font)
		: base(api, hotkeycode, font)
	{
		PaddingLeft = 0.0;
		PaddingRight = 1.0;
		if (api.Input.HotKeys.TryGetValue(hotkeycode.ToLowerInvariant(), out hotkey))
		{
			DisplayText = hotkey.CurrentMapping.ToString();
		}
		init();
		hotkeyTexture = new LoadedTexture(api);
		Font = Font.Clone().WithFontSize((float)Font.UnscaledFontsize * 0.9f);
	}

	public override void ComposeElements(Context ctx, ImageSurface surfaceUnused)
	{
	}

	public void GenHotkeyTexture()
	{
		List<string> parts = new List<string>();
		if (hotkey != null)
		{
			if (hotkey.CurrentMapping.Ctrl)
			{
				parts.Add("Ctrl");
			}
			if (hotkey.CurrentMapping.Alt)
			{
				parts.Add("Alt");
			}
			if (hotkey.CurrentMapping.Shift)
			{
				parts.Add("Shift");
			}
			parts.Add(hotkey.CurrentMapping.PrimaryAsString());
			if (hotkey.CurrentMapping.SecondKeyCode.HasValue)
			{
				parts.Add(hotkey.CurrentMapping.SecondaryAsString());
			}
		}
		else
		{
			parts.Add("?");
		}
		double textWidth = 0.0;
		double pluswdith = Font.GetTextExtents("+").Width;
		double symbolspacing = 3.0;
		double leftRightPad = 4.0;
		foreach (string part2 in parts)
		{
			double w = Font.GetTextExtents(part2).Width + GuiElement.scaled(symbolspacing + 2.0 * leftRightPad);
			if (textWidth > 0.0)
			{
				w += GuiElement.scaled(symbolspacing) + pluswdith;
			}
			textWidth += w;
		}
		double textHeight = Font.GetFontExtents().Height;
		int lineheight = (int)textHeight;
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)textWidth + 3, lineheight + 5);
		Context ctx = new Context(surface);
		Font.SetupContext(ctx);
		double hx = 0.0;
		double y = 0.0;
		foreach (string part in parts)
		{
			hx = DrawHotkey(api, part, hx, y, ctx, Font, lineheight, textHeight, pluswdith, symbolspacing, leftRightPad, Font.Color);
		}
		api.Gui.LoadOrUpdateCairoTexture(surface, linearMag: true, ref hotkeyTexture);
		ctx.Dispose();
		surface.Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
	{
		base.RenderInteractiveElements(deltaTime, renderX, renderY, renderZ);
		TextLine obj = Lines[Lines.Length - 1];
		double offsetX = GetFontOrientOffsetX();
		LineRectangled bounds = obj.Bounds;
		api.Render.Render2DTexture(hotkeyTexture.TextureId, (float)(renderX + offsetX + bounds.X), (int)(renderY + bounds.Y), hotkeyTexture.Width, hotkeyTexture.Height, (float)renderZ + 50f);
	}

	public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
	{
		GenHotkeyTexture();
		linebreak = EnumLinebreakBehavior.None;
		EnumCalcBoundsResult result = base.CalcBounds(flowPath, currentLineHeight, offsetX, lineY, out nextOffsetX);
		BoundsPerLine[0].Width += RuntimeEnv.GUIScale * 4f;
		nextOffsetX += RuntimeEnv.GUIScale * 4f;
		return result;
	}

	public override void Dispose()
	{
		base.Dispose();
		hotkeyTexture?.Dispose();
	}

	public static double DrawHotkey(ICoreClientAPI capi, string keycode, double x, double y, Context ctx, CairoFont font, double lineheight, double textHeight, double pluswdith, double symbolspacing, double leftRightPadding, double[] color)
	{
		if (x > 0.0)
		{
			capi.Gui.Text.DrawTextLine(ctx, font, "+", x + symbolspacing, y + (lineheight - textHeight) / 2.0 + GuiElement.scaled(2.0));
			x += pluswdith + 2.0 * symbolspacing;
		}
		double width = font.GetTextExtents(keycode).Width;
		GuiElement.RoundRectangle(ctx, x + 1.0, y + 1.0, (int)(width + GuiElement.scaled(leftRightPadding * 2.0)), lineheight, 3.5);
		ctx.SetSourceRGBA(color);
		ctx.LineWidth = 1.5;
		ctx.StrokePreserve();
		ctx.SetSourceRGBA(new double[4]
		{
			color[0],
			color[1],
			color[2],
			color[3] * 0.5
		});
		ctx.Fill();
		ctx.SetSourceRGBA(new double[4] { 1.0, 1.0, 1.0, 1.0 });
		int textX = (int)(x + 1.0 + GuiElement.scaled(leftRightPadding));
		int textY = (int)(y + (lineheight - textHeight) / 2.0 + GuiElement.scaled(2.0));
		capi.Gui.Text.DrawTextLine(ctx, font, keycode, textX, textY);
		return (int)(x + symbolspacing + width + GuiElement.scaled(leftRightPadding * 2.0));
	}
}
