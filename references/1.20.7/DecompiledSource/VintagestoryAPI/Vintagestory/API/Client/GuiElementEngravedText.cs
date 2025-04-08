using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

internal class GuiElementEngravedText : GuiElementTextBase
{
	private EnumTextOrientation orientation;

	/// <summary>
	/// Creates a new Engraved Text element.
	/// </summary>
	/// <param name="capi">The client API.</param>
	/// <param name="text">The text on the element.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="bounds">The bounds of the Text Element.</param>
	/// <param name="orientation">The orientation of the text.</param>
	public GuiElementEngravedText(ICoreClientAPI capi, string text, CairoFont font, ElementBounds bounds, EnumTextOrientation orientation = EnumTextOrientation.Left)
		: base(capi, text, font, bounds)
	{
		this.orientation = orientation;
	}

	public override void ComposeTextElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		Font.SetupContext(ctxStatic);
		Bounds.CalcWorldBounds();
		ImageSurface insetShadowSurface = new ImageSurface(Format.Argb32, Bounds.ParentBounds.OuterWidthInt, Bounds.ParentBounds.OuterHeightInt);
		Context ctxInsetShadow = new Context(insetShadowSurface);
		ctxInsetShadow.SetSourceRGB(0.0, 0.0, 0.0);
		ctxInsetShadow.Paint();
		Font.Color = new double[4] { 20.0, 20.0, 20.0, 0.3499999940395355 };
		Font.SetupContext(ctxInsetShadow);
		DrawMultilineTextAt(ctxInsetShadow, Bounds.drawX + GuiElement.scaled(2.0), Bounds.drawY + GuiElement.scaled(2.0), orientation);
		insetShadowSurface.BlurFull(7.0);
		ImageSurface surface = new ImageSurface(Format.Argb32, Bounds.ParentBounds.OuterWidthInt, Bounds.ParentBounds.OuterHeightInt);
		Context ctxText = new Context(surface);
		ctxText.Operator = Operator.Source;
		ctxText.Antialias = Antialias.Best;
		Font.Color = new double[4] { 0.0, 0.0, 0.0, 0.4 };
		Font.SetupContext(ctxText);
		ctxText.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
		DrawMultilineTextAt(ctxText, Bounds.drawX - GuiElement.scaled(0.5), Bounds.drawY - GuiElement.scaled(0.5), orientation);
		ctxText.SetSourceRGBA(1.0, 1.0, 1.0, 1.0);
		DrawMultilineTextAt(ctxText, Bounds.drawX + GuiElement.scaled(1.0), Bounds.drawY + GuiElement.scaled(1.0), orientation);
		ctxText.Operator = Operator.Atop;
		ctxText.SetSourceSurface(insetShadowSurface, 0, 0);
		ctxText.Paint();
		ctxInsetShadow.Dispose();
		insetShadowSurface.Dispose();
		ctxText.Operator = Operator.Over;
		Font.Color = new double[4] { 0.0, 0.0, 0.0, 0.35 };
		Font.SetupContext(ctxText);
		DrawMultilineTextAt(ctxText, (int)Bounds.drawX, (int)Bounds.drawY, orientation);
		ctxStatic.Antialias = Antialias.Best;
		ctxStatic.Operator = Operator.HardLight;
		ctxStatic.SetSourceSurface(surface, 0, 0);
		ctxStatic.Paint();
		surface.Dispose();
		ctxText.Dispose();
	}

	internal void TextWithSpacing(Context ctx, string text, double x, double y, float spacing)
	{
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			TextExtents extents = ctx.TextExtents(c.ToString() ?? "");
			ctx.MoveTo(x - extents.XBearing, x - extents.YBearing);
			ctx.ShowText(c.ToString() ?? "");
			x += extents.Width + (double)(spacing * RuntimeEnv.GUIScale);
		}
	}
}
