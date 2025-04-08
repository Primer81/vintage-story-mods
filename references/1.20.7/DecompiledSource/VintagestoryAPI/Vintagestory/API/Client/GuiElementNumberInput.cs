using System;
using System.Globalization;
using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

/// <summary>
/// A numerical input field for inputting numbers.
/// </summary>
public class GuiElementNumberInput : GuiElementTextInput
{
	public float Interval = 1f;

	public LoadedTexture buttonHighlightTexture;

	private bool focusable = true;

	/// <summary>
	/// When enabled and a button is clicked it wont focus on it, leaving your focus on the game to move around 
	/// </summary>
	public bool DisableButtonFocus;

	public override bool Focusable => focusable;

	/// <summary>
	/// Creates a numerical input field.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="bounds">The bounds of the GUI.</param>
	/// <param name="OnTextChanged">The event fired when the number is changed.</param>
	/// <param name="font">The font of the numbers.</param>
	public GuiElementNumberInput(ICoreClientAPI capi, ElementBounds bounds, Action<string> OnTextChanged, CairoFont font)
		: base(capi, bounds, OnTextChanged, font)
	{
		buttonHighlightTexture = new LoadedTexture(capi);
	}

	/// <summary>
	/// Gets the current value of the number.
	/// </summary>
	/// <returns>A float representing the value.</returns>
	public float GetValue()
	{
		float.TryParse(GetText(), NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var val);
		return val;
	}

	public override void ComposeTextElements(Context ctx, ImageSurface surface)
	{
		rightSpacing = GuiElement.scaled(17.0);
		EmbossRoundRectangleElement(ctx, Bounds, inverse: true, 2, 1);
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.3);
		ElementRoundRectangle(ctx, Bounds, isBackground: false, 1.0);
		ctx.Fill();
		GenTextHighlightTexture();
		GenButtonHighlightTexture();
		highlightBounds = Bounds.CopyOffsetedSibling().WithFixedPadding(0.0, 0.0).FixedGrow(2.0 * Bounds.absPaddingX, 2.0 * Bounds.absPaddingY);
		highlightBounds.CalcWorldBounds();
		RecomposeText();
		double heightHalf = Bounds.OuterHeight / 2.0 - 1.0;
		ctx.SetSourceRGBA(GuiStyle.DialogHighlightColor);
		GuiElement.RoundRectangle(ctx, Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(18.0) * Scale, Bounds.drawY, rightSpacing * Scale, heightHalf, 1.0);
		ctx.Fill();
		EmbossRoundRectangleElement(ctx, Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(18.0) * Scale, Bounds.drawY, rightSpacing * Scale, heightHalf, inverse: false, 2, 1);
		ctx.NewPath();
		ctx.LineTo(Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(9.0) * Scale, Bounds.drawY + GuiElement.scaled(1.0) * Scale);
		ctx.LineTo(Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(14.0) * Scale, Bounds.drawY + (heightHalf - GuiElement.scaled(2.0)) * Scale);
		ctx.LineTo(Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(4.0) * Scale, Bounds.drawY + (heightHalf - GuiElement.scaled(2.0)) * Scale);
		ctx.ClosePath();
		ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.4);
		ctx.Fill();
		ctx.SetSourceRGBA(GuiStyle.DialogHighlightColor);
		GuiElement.RoundRectangle(ctx, Bounds.drawX + Bounds.InnerWidth - (rightSpacing + GuiElement.scaled(1.0)) * Scale, Bounds.drawY + heightHalf + GuiElement.scaled(1.0) * Scale, rightSpacing * Scale, heightHalf, 1.0);
		ctx.Fill();
		EmbossRoundRectangleElement(ctx, Bounds.drawX + Bounds.InnerWidth - (rightSpacing + GuiElement.scaled(1.0)) * Scale, Bounds.drawY + heightHalf + GuiElement.scaled(1.0) * Scale, rightSpacing * Scale, heightHalf, inverse: false, 2, 1);
		ctx.NewPath();
		ctx.LineTo(Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(14.0) * Scale, Bounds.drawY + (heightHalf + GuiElement.scaled(3.0)) * Scale);
		ctx.LineTo(Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(4.0) * Scale, Bounds.drawY + (heightHalf + GuiElement.scaled(3.0)) * Scale);
		ctx.LineTo(Bounds.drawX + Bounds.InnerWidth - GuiElement.scaled(9.0) * Scale, Bounds.drawY + heightHalf * 2.0 * Scale);
		ctx.ClosePath();
		ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.4);
		ctx.Fill();
		highlightBounds.fixedWidth -= rightSpacing / (double)RuntimeEnv.GUIScale;
		highlightBounds.CalcWorldBounds();
	}

	private void GenButtonHighlightTexture()
	{
		double heightHalf = Bounds.OuterHeight / 2.0 - 1.0;
		ImageSurface surfaceHighlight = new ImageSurface(Format.Argb32, (int)rightSpacing, (int)heightHalf);
		Context context = genContext(surfaceHighlight);
		context.SetSourceRGBA(1.0, 1.0, 1.0, 0.2);
		context.Paint();
		generateTexture(surfaceHighlight, ref buttonHighlightTexture);
		context.Dispose();
		surfaceHighlight.Dispose();
	}

	private void GenTextHighlightTexture()
	{
		ImageSurface surfaceHighlight = new ImageSurface(Format.Argb32, (int)(Bounds.OuterWidth - rightSpacing), (int)Bounds.OuterHeight);
		Context context = genContext(surfaceHighlight);
		context.SetSourceRGBA(1.0, 1.0, 1.0, 0.3);
		context.Paint();
		generateTexture(surfaceHighlight, ref highlightTexture);
		context.Dispose();
		surfaceHighlight.Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		base.RenderInteractiveElements(deltaTime);
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		MouseOverCursor = "textselect";
		if ((double)mouseX >= Bounds.absX + Bounds.InnerWidth - GuiElement.scaled(21.0) && (double)mouseX <= Bounds.absX + Bounds.OuterWidth && (double)mouseY >= Bounds.absY && (double)mouseY <= Bounds.absY + Bounds.OuterHeight)
		{
			MouseOverCursor = null;
			double heightHalf = Bounds.OuterHeight / 2.0 - 1.0;
			if ((double)mouseY > Bounds.absY + heightHalf + 1.0)
			{
				api.Render.Render2DTexturePremultipliedAlpha(buttonHighlightTexture.TextureId, Bounds.renderX + Bounds.OuterWidth - rightSpacing - 1.0, Bounds.renderY + heightHalf + 1.0, rightSpacing, heightHalf);
			}
			else
			{
				api.Render.Render2DTexturePremultipliedAlpha(buttonHighlightTexture.TextureId, Bounds.renderX + Bounds.OuterWidth - rightSpacing - 1.0, Bounds.renderY, rightSpacing, heightHalf);
			}
		}
	}

	public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
	{
		if (IsPositionInside(api.Input.MouseX, api.Input.MouseY))
		{
			rightSpacing = GuiElement.scaled(17.0);
			float size = ((args.deltaPrecise > 0f) ? 1 : (-1));
			size *= Interval;
			if (api.Input.KeyboardKeyStateRaw[1])
			{
				size /= 10f;
			}
			if (api.Input.KeyboardKeyStateRaw[3])
			{
				size /= 100f;
			}
			UpdateValue(size);
			args.SetHandled();
		}
	}

	private void UpdateValue(float size)
	{
		double.TryParse(lines[0], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var val);
		val += (double)size;
		lines[0] = Math.Round(val, 4).ToString(GlobalConstants.DefaultCultureInfo);
		SetValue(lines[0]);
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		rightSpacing = GuiElement.scaled(17.0);
		int mouseX = args.X;
		int mouseY = args.Y;
		float size = Interval;
		if (api.Input.KeyboardKeyStateRaw[1])
		{
			size /= 10f;
		}
		if (api.Input.KeyboardKeyStateRaw[3])
		{
			size /= 100f;
		}
		if ((double)mouseX >= Bounds.absX + Bounds.OuterWidth - rightSpacing && (double)mouseX <= Bounds.absX + Bounds.OuterWidth && (double)mouseY >= Bounds.absY && (double)mouseY <= Bounds.absY + Bounds.OuterHeight)
		{
			if (DisableButtonFocus)
			{
				focusable = false;
			}
			double heightHalf = Bounds.OuterHeight / 2.0 - 1.0;
			if ((double)mouseY > Bounds.absY + heightHalf + 1.0)
			{
				UpdateValue(0f - size);
			}
			else
			{
				UpdateValue(size);
			}
		}
		else if (DisableButtonFocus)
		{
			focusable = true;
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		buttonHighlightTexture.Dispose();
	}
}
