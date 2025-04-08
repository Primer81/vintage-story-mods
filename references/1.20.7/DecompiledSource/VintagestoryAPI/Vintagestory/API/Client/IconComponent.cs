using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

/// <summary>
/// Draws an icon
/// </summary>
public class IconComponent : RichTextComponentBase
{
	protected ICoreClientAPI capi;

	protected ElementBounds parentBounds;

	public double offY;

	public double sizeMulSvg = 0.7;

	protected string iconName;

	protected string iconPath;

	protected CairoFont font;

	public IconComponent(ICoreClientAPI capi, string iconName, string iconPath, CairoFont font)
		: base(capi)
	{
		this.capi = capi;
		this.iconName = iconName;
		this.iconPath = iconPath;
		this.font = font;
		BoundsPerLine = new LineRectangled[1]
		{
			new LineRectangled(0.0, 0.0, GuiElement.scaled(font.UnscaledFontsize), GuiElement.scaled(font.UnscaledFontsize))
		};
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		double size = GuiElement.scaled(font.UnscaledFontsize);
		IAsset svgAsset = null;
		if (iconPath != null)
		{
			svgAsset = capi.Assets.TryGet(new AssetLocation(iconPath).WithPathPrefixOnce("textures/"));
		}
		if (svgAsset != null)
		{
			size *= sizeMulSvg;
			double asc = font.GetFontExtents().Ascent;
			capi.Gui.DrawSvg(svgAsset, surface, (int)BoundsPerLine[0].X, (int)(BoundsPerLine[0].Y + asc - (double)(int)size) + 2, (int)size, (int)size, ColorUtil.ColorFromRgba(font.Color));
		}
		else
		{
			capi.Gui.Icons.DrawIcon(ctx, iconName, BoundsPerLine[0].X, BoundsPerLine[0].Y, size, size, font.Color);
		}
	}

	public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
	{
		TextFlowPath curfp = GetCurrentFlowPathSection(flowPath, lineY);
		offsetX += GuiElement.scaled(PaddingLeft);
		bool requireLinebreak = offsetX + BoundsPerLine[0].Width > curfp.X2;
		BoundsPerLine[0].X = (requireLinebreak ? 0.0 : offsetX);
		BoundsPerLine[0].Y = lineY + (requireLinebreak ? currentLineHeight : 0.0);
		nextOffsetX = (requireLinebreak ? 0.0 : offsetX) + BoundsPerLine[0].Width;
		if (!requireLinebreak)
		{
			return EnumCalcBoundsResult.Continue;
		}
		return EnumCalcBoundsResult.Nextline;
	}

	public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
	{
	}

	public override void Dispose()
	{
		base.Dispose();
	}
}
