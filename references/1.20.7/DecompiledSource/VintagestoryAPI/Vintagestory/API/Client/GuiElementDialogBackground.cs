using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementDialogBackground : GuiElement
{
	public bool Shade = true;

	private bool withTitlebar;

	private double strokeWidth;

	public float Alpha = 1f;

	public bool FullBlur;

	/// <summary>
	/// Adds a Background to the Dialog.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="bounds">The bounds of the element.</param>
	/// <param name="withTitlebar">Minor style adjustments to accomodate title bar</param>
	/// <param name="strokeWidth">The top padding area of the GUI</param>
	/// <param name="alpha"></param>
	public GuiElementDialogBackground(ICoreClientAPI capi, ElementBounds bounds, bool withTitlebar, double strokeWidth = 0.0, float alpha = 1f)
		: base(capi, bounds)
	{
		this.strokeWidth = strokeWidth;
		this.withTitlebar = withTitlebar;
		Alpha = alpha;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		double titleBarOffY = (withTitlebar ? GuiElement.scaled(GuiStyle.TitleBarHeight) : 0.0);
		GuiElement.RoundRectangle(ctx, Bounds.bgDrawX, Bounds.bgDrawY + titleBarOffY, Bounds.OuterWidth, Bounds.OuterHeight - titleBarOffY - 1.0, GuiStyle.DialogBGRadius);
		ctx.SetSourceRGBA(GuiStyle.DialogStrongBgColor[0] * 1.0, GuiStyle.DialogStrongBgColor[1] * 1.0, GuiStyle.DialogStrongBgColor[2] * 1.0, GuiStyle.DialogStrongBgColor[3] * 1.0);
		ctx.FillPreserve();
		if (Shade)
		{
			ctx.SetSourceRGBA(GuiStyle.DialogLightBgColor[0] * 2.1, GuiStyle.DialogStrongBgColor[1] * 2.1, GuiStyle.DialogStrongBgColor[2] * 2.1, 1.0);
			ctx.LineWidth = strokeWidth * 2.0;
			ctx.StrokePreserve();
			double r = GuiElement.scaled(9.0);
			if (FullBlur)
			{
				surface.BlurFull(r);
			}
			else
			{
				surface.BlurPartial(r, (int)(2.0 * r + 1.0), (int)Bounds.bgDrawX, (int)(Bounds.bgDrawY + titleBarOffY), (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
			}
		}
		SurfacePattern pattern = GuiElement.getPattern(api, GuiElement.dirtTextureName, doCache: true, 64, 0.125f);
		ctx.SetSource(pattern);
		ctx.FillPreserve();
		ctx.Operator = Operator.Over;
		if (Shade)
		{
			double[] obj = new double[4]
			{
				0.17647058823529413,
				7.0 / 51.0,
				11.0 / 85.0,
				0.0
			};
			obj[3] = Alpha * Alpha;
			ctx.SetSourceRGBA(obj);
			ctx.LineWidth = strokeWidth;
			ctx.Stroke();
		}
		else
		{
			double[] obj2 = new double[4]
			{
				0.17647058823529413,
				7.0 / 51.0,
				11.0 / 85.0,
				0.0
			};
			obj2[3] = Alpha;
			ctx.SetSourceRGBA(obj2);
			ctx.LineWidth = GuiElement.scaled(2.0);
			ctx.Stroke();
		}
	}
}
