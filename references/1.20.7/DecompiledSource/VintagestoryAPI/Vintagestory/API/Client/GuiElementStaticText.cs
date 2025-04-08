using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementStaticText : GuiElementTextBase
{
	internal EnumTextOrientation orientation;

	public double offsetX;

	public double offsetY;

	/// <summary>
	/// Creates a new GUIElementStaticText.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="text">The text of the Element</param>
	/// <param name="orientation">The orientation of the text.</param>
	/// <param name="bounds">The bounds of the element.</param>
	/// <param name="font">The font of the text.</param>
	public GuiElementStaticText(ICoreClientAPI capi, string text, EnumTextOrientation orientation, ElementBounds bounds, CairoFont font)
		: base(capi, text, font, bounds)
	{
		this.orientation = orientation;
	}

	public double GetTextHeight()
	{
		return textUtil.GetMultilineTextHeight(Font, text, Bounds.InnerWidth);
	}

	public override void ComposeTextElements(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		Bounds.absInnerHeight = textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, text, (int)(offsetX + Bounds.drawX), (int)(offsetY + Bounds.drawY), Bounds.InnerWidth, orientation);
	}

	/// <summary>
	/// Resize element bounds so that the text fits in one line
	/// </summary>
	/// <param name="onlyGrow"></param>
	public void AutoBoxSize(bool onlyGrow = false)
	{
		Font.AutoBoxSize(text, Bounds, onlyGrow);
	}

	public void SetValue(string text)
	{
		base.text = text;
	}

	/// <summary>
	/// Resize the font so that the text fits in one line
	/// </summary>
	public void AutoFontSize(bool onlyShrink = true)
	{
		Bounds.CalcWorldBounds();
		Font.AutoFontSize(text, Bounds, onlyShrink);
	}
}
