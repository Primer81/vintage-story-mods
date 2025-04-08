using System;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class SkillItem : IDisposable
{
	public string Name;

	public string Description;

	public AssetLocation Code;

	public LoadedTexture Texture;

	public KeyCombination Hotkey;

	public bool Linebreak;

	public bool Enabled = true;

	public RenderSkillItemDelegate RenderHandler;

	public object Data;

	public bool TexturePremultipliedAlpha = true;

	public SkillItem WithIcon(ICoreClientAPI capi, DrawSkillIconDelegate onDrawIcon)
	{
		if (capi == null)
		{
			return this;
		}
		Texture = capi.Gui.Icons.GenTexture(48, 48, delegate(Context ctx, ImageSurface surface)
		{
			onDrawIcon(ctx, 5, 5, 38f, 38f, ColorUtil.WhiteArgbDouble);
		});
		return this;
	}

	public SkillItem WithIcon(ICoreClientAPI capi, string iconCode)
	{
		if (capi == null)
		{
			return this;
		}
		Texture = capi.Gui.Icons.GenTexture(48, 48, delegate(Context ctx, ImageSurface surface)
		{
			capi.Gui.Icons.DrawIcon(ctx, iconCode, 5.0, 5.0, 38.0, 38.0, ColorUtil.WhiteArgbDouble);
		});
		return this;
	}

	public SkillItem WithLetterIcon(ICoreClientAPI capi, string letter)
	{
		if (capi == null)
		{
			return this;
		}
		int isize = (int)GuiElement.scaled(48.0);
		Texture = capi.Gui.Icons.GenTexture(isize, isize, delegate(Context ctx, ImageSurface surface)
		{
			CairoFont cairoFont = CairoFont.WhiteMediumText().WithColor(new double[4] { 1.0, 1.0, 1.0, 1.0 });
			cairoFont.SetupContext(ctx);
			TextExtents textExtents = cairoFont.GetTextExtents(letter);
			double num = cairoFont.GetFontExtents().Ascent + GuiElement.scaled(2.0);
			capi.Gui.Text.DrawTextLine(ctx, cairoFont, letter, ((double)isize - textExtents.Width) / 2.0, ((double)isize - num) / 2.0);
		});
		return this;
	}

	public SkillItem WithIcon(ICoreClientAPI capi, LoadedTexture texture)
	{
		Texture = texture;
		return this;
	}

	public void Dispose()
	{
		Texture?.Dispose();
	}
}
