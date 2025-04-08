using System;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementSliderOld : GuiElement
{
	private int minValue;

	private int maxValue = 100;

	private int step = 1;

	private string unit = "";

	private int currentValue;

	private int alarmValue;

	private bool mouseDownOnSlider;

	private bool triggerOnMouseUp;

	private bool didChangeValue;

	private int handleTextureId;

	private int hoverTextTextureId;

	private GuiElementStaticText textElem;

	private int alarmValueTextureId;

	private Rectangled alarmTextureRect;

	private ActionConsumable<int> onNewSliderValue;

	internal const int unscaledHeight = 20;

	internal const int unscaledPadding = 6;

	private int unscaledHandleWidth = 15;

	private int unscaledHandleHeight = 40;

	private int unscaledHoverTextHeight = 50;

	private double handleWidth;

	private double handleHeight;

	private double hoverTextWidth;

	private double hoverTextHeight;

	private double padding;

	public GuiElementSliderOld(ICoreClientAPI capi, ActionConsumable<int> onNewSliderValue, ElementBounds bounds)
		: base(capi, bounds)
	{
		this.onNewSliderValue = onNewSliderValue;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		handleWidth = GuiElement.scaled(unscaledHandleWidth);
		handleHeight = GuiElement.scaled(unscaledHandleHeight);
		hoverTextWidth = GuiElement.scaled(unscaledHoverTextHeight);
		hoverTextHeight = GuiElement.scaled(unscaledHoverTextHeight);
		padding = GuiElement.scaled(6.0);
		Bounds.CalcWorldBounds();
		GuiElement.RoundRectangle(ctxStatic, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, GuiStyle.ElementBGRadius);
		GuiElement.fillWithPattern(api, ctxStatic, GuiElement.woodTextureName);
		EmbossRoundRectangleElement(ctxStatic, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight);
		double insetWidth = Bounds.InnerWidth - 2.0 * padding;
		double insetHeight = Bounds.InnerHeight - 2.0 * padding;
		ctxStatic.SetSourceRGBA(0.0, 0.0, 0.0, 0.6);
		GuiElement.RoundRectangle(ctxStatic, Bounds.drawX + padding, Bounds.drawY + padding, insetWidth, insetHeight, GuiStyle.ElementBGRadius);
		ctxStatic.Fill();
		EmbossRoundRectangleElement(ctxStatic, Bounds.drawX + padding, Bounds.drawY + padding, insetWidth, insetHeight, inverse: true);
		if (alarmValue > 0 && alarmValue < maxValue)
		{
			float alarmValueRel = (float)alarmValue / (float)maxValue;
			alarmTextureRect = new Rectangled
			{
				X = padding + (Bounds.InnerWidth - 2.0 * padding) * (double)alarmValueRel,
				Y = padding,
				Width = (Bounds.InnerWidth - 2.0 * padding) * (double)(1f - alarmValueRel),
				Height = Bounds.InnerHeight - 2.0 * padding
			};
			ctxStatic.SetSourceRGBA(0.62, 0.0, 0.0, 0.4);
			GuiElement.RoundRectangle(ctxStatic, Bounds.drawX + padding + insetWidth * (double)alarmValueRel, Bounds.drawY + padding, insetWidth * (double)(1f - alarmValueRel), insetHeight, GuiStyle.ElementBGRadius);
			ctxStatic.Fill();
		}
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)handleWidth + 5, (int)handleHeight + 5);
		Context ctx = genContext(surface);
		ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.0);
		ctx.Paint();
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
		GuiElement.RoundRectangle(ctx, 0.0, 0.0, handleWidth, handleHeight, GuiStyle.ElementBGRadius);
		ctx.Fill();
		surface.BlurFull(3.0);
		GuiElement.RoundRectangle(ctx, 0.0, 0.0, handleWidth, handleHeight, GuiStyle.ElementBGRadius);
		GuiElement.fillWithPattern(api, ctx, GuiElement.woodTextureName);
		EmbossRoundRectangleElement(ctx, 0.0, 0.0, handleWidth, handleHeight);
		generateTexture(surface, ref handleTextureId);
		ctx.Dispose();
		surface.Dispose();
		ComposeHoverTextElement();
	}

	internal void ComposeHoverTextElement()
	{
		ElementBounds bounds = new ElementBounds().WithFixedPadding(7.0).WithParent(ElementBounds.Empty);
		textElem = new GuiElementStaticText(api, currentValue + unit, EnumTextOrientation.Center, bounds, CairoFont.WhiteMediumText());
		textElem.Font.UnscaledFontsize = GuiStyle.SmallishFontSize;
		textElem.AutoBoxSize();
		textElem.Bounds.CalcWorldBounds();
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)bounds.OuterWidth, (int)bounds.OuterHeight);
		Context ctx = genContext(surface);
		ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.0);
		ctx.Paint();
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.3);
		GuiElement.RoundRectangle(ctx, 0.0, 0.0, bounds.OuterWidth, bounds.OuterHeight, GuiStyle.ElementBGRadius);
		ctx.Fill();
		textElem.ComposeElements(ctx, surface);
		generateTexture(surface, ref hoverTextTextureId);
		ctx.Dispose();
		surface.Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if ((float)(alarmValue - minValue) / (float)(maxValue - minValue) > 0f)
		{
			_ = (float)alarmValue / (float)maxValue;
			api.Render.RenderTexture(alarmValueTextureId, Bounds.renderX + alarmTextureRect.X, Bounds.renderY + alarmTextureRect.Y, alarmTextureRect.Width, alarmTextureRect.Height);
		}
		double handlePosition = (Bounds.InnerWidth - 2.0 * padding - handleWidth / 2.0) * (1.0 * (double)currentValue - (double)minValue) / (double)(maxValue - minValue);
		double dy = (handleHeight - Bounds.InnerHeight) / 2.0;
		api.Render.RenderTexture(handleTextureId, Bounds.renderX + padding + handlePosition, Bounds.renderY - dy, (int)handleWidth + 5, (int)handleHeight + 5);
		if (mouseDownOnSlider || Bounds.PointInside(api.Input.MouseX, api.Input.MouseY))
		{
			ElementBounds elemBounds = textElem.Bounds;
			api.Render.RenderTexture(hoverTextTextureId, Bounds.renderX + padding + handlePosition - elemBounds.OuterWidth / 2.0 + handleWidth / 2.0, Bounds.renderY - GuiElement.scaled(20.0) - elemBounds.OuterHeight, elemBounds.OuterWidth, elemBounds.OuterHeight);
		}
	}

	private void MakeAlarmValueTexture()
	{
		float alarmValueRel = (float)(alarmValue - minValue) / (float)(maxValue - minValue);
		alarmTextureRect = new Rectangled
		{
			X = padding + (Bounds.InnerWidth - 2.0 * padding) * (double)alarmValueRel,
			Y = padding,
			Width = (Bounds.InnerWidth - 2.0 * padding) * (double)(1f - alarmValueRel),
			Height = Bounds.InnerHeight - 2.0 * padding
		};
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)alarmTextureRect.Width, (int)alarmTextureRect.Height);
		Context context = genContext(surface);
		context.SetSourceRGBA(1.0, 0.0, 1.0, 0.4);
		GuiElement.RoundRectangle(context, 0.0, 0.0, alarmTextureRect.Width, alarmTextureRect.Height, GuiStyle.ElementBGRadius);
		context.Fill();
		generateTexture(surface, ref alarmValueTextureId);
		context.Dispose();
		surface.Dispose();
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (Bounds.PointInside(api.Input.MouseX, api.Input.MouseY))
		{
			args.Handled = updateValue(api.Input.MouseX);
			mouseDownOnSlider = true;
		}
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		mouseDownOnSlider = false;
		if (onNewSliderValue != null && didChangeValue && triggerOnMouseUp)
		{
			onNewSliderValue(currentValue);
		}
		didChangeValue = false;
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		if (mouseDownOnSlider)
		{
			args.Handled = updateValue(api.Input.MouseX);
		}
	}

	/// <summary>
	/// Trigger event only once user release the mouse
	/// </summary>
	/// <param name="trigger"></param>
	internal void triggerOnlyOnMouseUp(bool trigger = true)
	{
		triggerOnMouseUp = trigger;
	}

	private bool updateValue(int mouseX)
	{
		double sliderWidth = Bounds.InnerWidth - 2.0 * padding - handleWidth / 2.0;
		double mouseDeltaX = GameMath.Clamp((double)mouseX - Bounds.renderX - padding, 0.0, sliderWidth);
		double value = (double)minValue + (double)(maxValue - minValue) * mouseDeltaX / sliderWidth;
		int newValue = Math.Max(minValue, Math.Min(maxValue, step * (int)Math.Round(1.0 * value / (double)step)));
		if (newValue != currentValue)
		{
			didChangeValue = true;
		}
		currentValue = newValue;
		ComposeHoverTextElement();
		if (onNewSliderValue != null && !triggerOnMouseUp)
		{
			return onNewSliderValue(currentValue);
		}
		return false;
	}

	public void SetAlarmValue(int value)
	{
		alarmValue = value;
		MakeAlarmValueTexture();
	}

	public void setValues(int currentValue, int minValue, int maxValue, int step, string unit = "")
	{
		this.currentValue = currentValue;
		this.minValue = minValue;
		this.maxValue = maxValue;
		this.step = step;
		this.unit = unit;
		ComposeHoverTextElement();
	}

	public int GetValue()
	{
		return currentValue;
	}
}
