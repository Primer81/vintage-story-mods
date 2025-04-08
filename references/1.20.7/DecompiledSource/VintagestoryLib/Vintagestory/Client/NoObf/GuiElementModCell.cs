using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class GuiElementModCell : GuiElementTextBase, IGuiElementCell, IDisposable
{
	public static double unscaledRightBoxWidth = 40.0;

	public ModCellEntry cell;

	private double titleTextheight;

	private bool showModifyIcons = true;

	public bool On;

	internal int leftHighlightTextureId;

	internal int rightHighlightTextureId;

	internal int switchOnTextureId;

	internal double unscaledSwitchPadding = 4.0;

	internal double unscaledSwitchSize = 25.0;

	private LoadedTexture modcellTexture;

	private LoadedTexture warningTextTexture;

	private IAsset warningIcon;

	private ICoreClientAPI capi;

	public Action<int> OnMouseDownOnCellLeft;

	public Action<int> OnMouseDownOnCellRight;

	ElementBounds IGuiElementCell.Bounds => Bounds;

	public GuiElementModCell(ICoreClientAPI capi, ModCellEntry cell, ElementBounds bounds, IAsset warningIcon)
		: base(capi, "", null, bounds)
	{
		this.cell = cell;
		if (cell.TitleFont == null)
		{
			cell.TitleFont = CairoFont.WhiteSmallishText();
		}
		if (cell.DetailTextFont == null)
		{
			cell.DetailTextFont = CairoFont.WhiteSmallText();
			cell.DetailTextFont.Color[3] *= 0.6;
		}
		modcellTexture = new LoadedTexture(capi);
		if (cell.Mod.Info?.Dependencies != null)
		{
			foreach (ModDependency dep in cell.Mod.Info.Dependencies)
			{
				if (dep.Version.Length != 0 && !(dep.Version == "*") && cell.Mod.Enabled && (dep.ModID == "game" || dep.ModID == "creative" || dep.ModID == "survival") && !GameVersion.IsCompatibleApiVersion(dep.Version))
				{
					this.warningIcon = warningIcon;
					warningTextTexture = capi.Gui.TextTexture.GenTextTexture(Lang.Get("mod-versionmismatch", dep.Version, "1.20.7"), CairoFont.WhiteDetailText(), new TextBackground
					{
						FillColor = GuiStyle.DialogLightBgColor,
						Padding = 3,
						Radius = GuiStyle.ElementBGRadius
					});
				}
			}
		}
		this.capi = capi;
	}

	private void Compose()
	{
		ComposeHover(left: true, ref leftHighlightTextureId);
		ComposeHover(left: false, ref rightHighlightTextureId);
		genOnTexture();
		ImageSurface surface = new ImageSurface(Format.Argb32, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
		Context ctx = new Context(surface);
		double rightBoxWidth = GuiElement.scaled(unscaledRightBoxWidth);
		Bounds.CalcWorldBounds();
		ModContainer mod = cell.Mod;
		bool num = mod?.Info != null && (mod == null || !mod.Error.HasValue);
		if (cell.DrawAsButton)
		{
			GuiElement.RoundRectangle(ctx, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, 0.0);
			ctx.SetSourceRGB(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2]);
			ctx.Fill();
		}
		double textOffset = 0.0;
		if (mod.Icon != null)
		{
			int imageSize = (int)(Bounds.InnerHeight - Bounds.absPaddingY * 2.0 - 10.0);
			textOffset = imageSize + 15;
			surface.Image(mod.Icon, (int)Bounds.absPaddingX + 5, (int)Bounds.absPaddingY + 5, imageSize, imageSize);
		}
		Font = cell.TitleFont;
		titleTextheight = textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, cell.Title, Bounds.absPaddingX + textOffset, Bounds.absPaddingY, Bounds.InnerWidth - textOffset);
		Font = cell.DetailTextFont;
		textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, cell.DetailText, Bounds.absPaddingX + textOffset, Bounds.absPaddingY + titleTextheight + Bounds.absPaddingY, Bounds.InnerWidth - textOffset);
		if (cell.RightTopText != null)
		{
			TextExtents extents = Font.GetTextExtents(cell.RightTopText);
			textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, cell.RightTopText, Bounds.absPaddingX + Bounds.InnerWidth - extents.Width - rightBoxWidth - GuiElement.scaled(10.0), Bounds.absPaddingY + GuiElement.scaled(cell.RightTopOffY), extents.Width + 1.0, EnumTextOrientation.Right);
		}
		if (cell.DrawAsButton)
		{
			EmbossRoundRectangleElement(ctx, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, inverse: false, (int)GuiElement.scaled(4.0), 0);
		}
		if (!num)
		{
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
			GuiElement.RoundRectangle(ctx, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, 1.0);
			ctx.Fill();
		}
		double checkboxsize = GuiElement.scaled(unscaledSwitchSize);
		double padd = GuiElement.scaled(unscaledSwitchPadding);
		double x = Bounds.absPaddingX + Bounds.InnerWidth - GuiElement.scaled(0.0) - checkboxsize - padd;
		double y = Bounds.absPaddingY + Bounds.absPaddingY;
		if (showModifyIcons)
		{
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
			GuiElement.RoundRectangle(ctx, x, y, checkboxsize, checkboxsize, 3.0);
			ctx.Fill();
			EmbossRoundRectangleElement(ctx, x, y, checkboxsize, checkboxsize, inverse: true, (int)GuiElement.scaled(2.0), 2);
		}
		if (warningIcon != null)
		{
			capi.Gui.DrawSvg(warningIcon, surface, (int)(x - GuiElement.scaled(3.0)), (int)(y + GuiElement.scaled(35.0)), (int)GuiElement.scaled(30.0), (int)GuiElement.scaled(30.0), ColorUtil.ColorFromRgba(255, 209, 74, 255));
			capi.Gui.DrawSvg(capi.Assets.Get("textures/icons/excla.svg"), surface, (int)(x - GuiElement.scaled(3.0)), (int)(y + GuiElement.scaled(35.0)), (int)GuiElement.scaled(30.0), (int)GuiElement.scaled(30.0), -16777216);
		}
		generateTexture(surface, ref modcellTexture);
		ctx.Dispose();
		surface.Dispose();
	}

	private void genOnTexture()
	{
		double size = GuiElement.scaled(unscaledSwitchSize - 2.0 * unscaledSwitchPadding);
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)size, (int)size);
		Context ctx = genContext(surface);
		GuiElement.RoundRectangle(ctx, 0.0, 0.0, size, size, 2.0);
		GuiElement.fillWithPattern(api, ctx, GuiElement.waterTextureName);
		generateTexture(surface, ref switchOnTextureId);
		ctx.Dispose();
		surface.Dispose();
	}

	private void ComposeHover(bool left, ref int textureId)
	{
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context ctx = genContext(surface);
		double boxWidth = GuiElement.scaled(unscaledRightBoxWidth);
		if (left)
		{
			ctx.NewPath();
			ctx.LineTo(0.0, 0.0);
			ctx.LineTo(Bounds.InnerWidth - boxWidth, 0.0);
			ctx.LineTo(Bounds.InnerWidth - boxWidth, Bounds.OuterHeight);
			ctx.LineTo(0.0, Bounds.OuterHeight);
			ctx.ClosePath();
		}
		else
		{
			ctx.NewPath();
			ctx.LineTo(Bounds.InnerWidth - boxWidth, 0.0);
			ctx.LineTo(Bounds.OuterWidth, 0.0);
			ctx.LineTo(Bounds.OuterWidth, Bounds.OuterHeight);
			ctx.LineTo(Bounds.InnerWidth - boxWidth, Bounds.OuterHeight);
			ctx.ClosePath();
		}
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.15);
		ctx.Fill();
		generateTexture(surface, ref textureId);
		ctx.Dispose();
		surface.Dispose();
	}

	public void UpdateCellHeight()
	{
		Bounds.CalcWorldBounds();
		double unscaledPadding = Bounds.absPaddingY / (double)RuntimeEnv.GUIScale;
		double boxwidth = Bounds.InnerWidth;
		ModContainer mod = cell.Mod;
		if (mod?.Info != null && mod.Icon != null)
		{
			int imageSize = (int)(Bounds.InnerHeight - Bounds.absPaddingY * 2.0 - 10.0);
			boxwidth -= (double)(imageSize + 10);
		}
		Font = cell.TitleFont;
		base.Text = cell.Title;
		titleTextheight = textUtil.GetMultilineTextHeight(Font, cell.Title, boxwidth) / (double)RuntimeEnv.GUIScale;
		Font = cell.DetailTextFont;
		base.Text = cell.DetailText;
		double detailTextHeight = textUtil.GetMultilineTextHeight(Font, cell.DetailText, boxwidth) / (double)RuntimeEnv.GUIScale;
		Bounds.fixedHeight = unscaledPadding + titleTextheight + unscaledPadding + detailTextHeight + unscaledPadding;
		if (showModifyIcons && Bounds.fixedHeight < 73.0)
		{
			Bounds.fixedHeight = 73.0;
		}
	}

	public void OnRenderInteractiveElements(ICoreClientAPI api, float deltaTime)
	{
		if (modcellTexture.TextureId == 0)
		{
			Compose();
		}
		api.Render.Render2DTexturePremultipliedAlpha(modcellTexture.TextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
		int mx = api.Input.MouseX;
		int my = api.Input.MouseY;
		Vec2d pos = Bounds.PositionInside(mx, my);
		if (cell.Mod?.Info != null && pos != null)
		{
			if (pos.X > Bounds.InnerWidth - GuiElement.scaled(GuiElementMainMenuCell.unscaledRightBoxWidth))
			{
				api.Render.Render2DTexturePremultipliedAlpha(rightHighlightTextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
			}
			else
			{
				api.Render.Render2DTexturePremultipliedAlpha(leftHighlightTextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
			}
		}
		if (On)
		{
			double size = GuiElement.scaled(unscaledSwitchSize - 2.0 * unscaledSwitchPadding);
			double padding = GuiElement.scaled(unscaledSwitchPadding);
			double x = Bounds.renderX + Bounds.InnerWidth - size + padding - GuiElement.scaled(5.0);
			double y = Bounds.renderY + GuiElement.scaled(8.0) + padding;
			api.Render.Render2DTexturePremultipliedAlpha(switchOnTextureId, x, y, (int)size, (int)size);
		}
		else
		{
			api.Render.Render2DTexturePremultipliedAlpha(rightHighlightTextureId, (int)Bounds.renderX, (int)Bounds.renderY, Bounds.OuterWidth, Bounds.OuterHeight);
			api.Render.Render2DTexturePremultipliedAlpha(leftHighlightTextureId, (int)Bounds.renderX, (int)Bounds.renderY, Bounds.OuterWidth, Bounds.OuterHeight);
		}
		if (warningTextTexture != null && IsPositionInside(api.Input.MouseX, api.Input.MouseY))
		{
			api.Render.GlScissorFlag(enable: false);
			api.Render.Render2DTexturePremultipliedAlpha(warningTextTexture.TextureId, mx + 25, my + 10, warningTextTexture.Width, warningTextTexture.Height, 500f);
			api.Render.GlScissorFlag(enable: true);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		modcellTexture?.Dispose();
		warningTextTexture?.Dispose();
		api.Render.GLDeleteTexture(leftHighlightTextureId);
		api.Render.GLDeleteTexture(rightHighlightTextureId);
		api.Render.GLDeleteTexture(switchOnTextureId);
	}

	public void OnMouseUpOnElement(MouseEvent args, int elementIndex)
	{
		int mousex = api.Input.MouseX;
		int mousey = api.Input.MouseY;
		Vec2d vec2d = Bounds.PositionInside(mousex, mousey);
		api.Gui.PlaySound("menubutton_press");
		if (vec2d.X > Bounds.InnerWidth - GuiElement.scaled(GuiElementMainMenuCell.unscaledRightBoxWidth))
		{
			OnMouseDownOnCellRight?.Invoke(elementIndex);
			args.Handled = true;
		}
		else
		{
			OnMouseDownOnCellLeft?.Invoke(elementIndex);
			args.Handled = true;
		}
	}

	public void OnMouseMoveOnElement(MouseEvent args, int elementIndex)
	{
	}

	public void OnMouseDownOnElement(MouseEvent args, int elementIndex)
	{
	}
}
