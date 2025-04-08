using System;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

/// <summary>
/// A stat bar to the GUI for keeping track of progress and numbers.
/// </summary>
public class GuiElementStatbar : GuiElementTextBase
{
	private float minValue;

	private float maxValue = 100f;

	private float value = 32f;

	private float lineInterval = 10f;

	private double[] color;

	private bool rightToLeft;

	private LoadedTexture baseTexture;

	private LoadedTexture barTexture;

	private LoadedTexture flashTexture;

	private LoadedTexture valueTexture;

	private int valueWidth;

	private int valueHeight;

	public bool ShouldFlash;

	public float FlashTime;

	public bool ShowValueOnHover = true;

	private bool valuesSet;

	private bool hideable;

	public StatbarValueDelegate onGetStatbarValue;

	public CairoFont valueFont = CairoFont.WhiteSmallText().WithStroke(ColorUtil.BlackArgbDouble, 0.75);

	public static double DefaultHeight = 8.0;

	public bool HideWhenFull { get; set; }

	/// <summary>
	/// Creates a new stat bar for the GUI.
	/// </summary>
	/// <param name="capi">The client API</param>
	/// <param name="bounds">The bounds of the stat bar.</param>
	/// <param name="color">The color of the stat bar.</param>
	/// <param name="rightToLeft">Determines the direction that the bar fills.</param>
	/// <param name="hideable"></param>
	public GuiElementStatbar(ICoreClientAPI capi, ElementBounds bounds, double[] color, bool rightToLeft, bool hideable)
		: base(capi, "", CairoFont.WhiteDetailText(), bounds)
	{
		barTexture = new LoadedTexture(capi);
		flashTexture = new LoadedTexture(capi);
		valueTexture = new LoadedTexture(capi);
		if (hideable)
		{
			baseTexture = new LoadedTexture(capi);
		}
		this.hideable = hideable;
		this.color = color;
		this.rightToLeft = rightToLeft;
		onGetStatbarValue = () => (float)Math.Round(value, 1) + " / " + (int)maxValue;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		if (hideable)
		{
			surface = new ImageSurface(Format.Argb32, Bounds.OuterWidthInt + 1, Bounds.OuterHeightInt + 1);
			ctx = new Context(surface);
			GuiElement.RoundRectangle(ctx, 0.0, 0.0, Bounds.InnerWidth, Bounds.InnerHeight, 1.0);
			ctx.SetSourceRGBA(0.15, 0.15, 0.15, 1.0);
			ctx.Fill();
			EmbossRoundRectangleElement(ctx, 0.0, 0.0, Bounds.InnerWidth, Bounds.InnerHeight, inverse: false, 3, 1);
		}
		else
		{
			ctx.Operator = Operator.Over;
			GuiElement.RoundRectangle(ctx, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, 1.0);
			ctx.SetSourceRGBA(0.15, 0.15, 0.15, 1.0);
			ctx.Fill();
			EmbossRoundRectangleElement(ctx, Bounds, inverse: false, 3, 1);
		}
		if (valuesSet)
		{
			recomposeOverlays();
		}
		if (hideable)
		{
			generateTexture(surface, ref baseTexture);
			surface.Dispose();
			ctx.Dispose();
		}
	}

	private void recomposeOverlays()
	{
		TyronThreadPool.QueueTask(delegate
		{
			ComposeValueOverlay();
			ComposeFlashOverlay();
		});
		if (ShowValueOnHover)
		{
			api.Gui.TextTexture.GenOrUpdateTextTexture(onGetStatbarValue(), valueFont, ref valueTexture, new TextBackground
			{
				FillColor = GuiStyle.DialogStrongBgColor,
				Padding = 5,
				BorderWidth = 2.0
			});
		}
	}

	private void ComposeValueOverlay()
	{
		Bounds.CalcWorldBounds();
		double widthRel = (double)value / (double)(maxValue - minValue);
		valueWidth = (int)(widthRel * Bounds.OuterWidth) + 1;
		valueHeight = (int)Bounds.OuterHeight + 1;
		ImageSurface surface = new ImageSurface(Format.Argb32, Bounds.OuterWidthInt + 1, valueHeight);
		Context ctx = new Context(surface);
		if (widthRel > 0.01)
		{
			double width = Bounds.OuterWidth * widthRel;
			double x2 = (rightToLeft ? (Bounds.OuterWidth - width) : 0.0);
			GuiElement.RoundRectangle(ctx, x2, 0.0, width, Bounds.OuterHeight, 1.0);
			ctx.SetSourceRGB(color[0], color[1], color[2]);
			ctx.FillPreserve();
			ctx.SetSourceRGB(color[0] * 0.4, color[1] * 0.4, color[2] * 0.4);
			ctx.LineWidth = GuiElement.scaled(3.0);
			ctx.StrokePreserve();
			surface.BlurFull(3.0);
			width = Bounds.InnerWidth * widthRel;
			x2 = (rightToLeft ? (Bounds.InnerWidth - width) : 0.0);
			EmbossRoundRectangleElement(ctx, x2, 0.0, width, Bounds.InnerHeight, inverse: false, 2, 1);
		}
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
		ctx.LineWidth = GuiElement.scaled(2.2);
		int lines = Math.Min(50, (int)((maxValue - minValue) / lineInterval));
		for (int i = 1; i < lines; i++)
		{
			ctx.NewPath();
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
			double x = Bounds.InnerWidth * (double)i / (double)lines;
			ctx.MoveTo(x, 0.0);
			ctx.LineTo(x, Math.Max(3.0, Bounds.InnerHeight - 1.0));
			ctx.ClosePath();
			ctx.Stroke();
		}
		api.Event.EnqueueMainThreadTask(delegate
		{
			generateTexture(surface, ref barTexture);
			ctx.Dispose();
			surface.Dispose();
		}, "recompstatbar");
	}

	private void ComposeFlashOverlay()
	{
		valueHeight = (int)Bounds.OuterHeight + 1;
		ImageSurface surface = new ImageSurface(Format.Argb32, Bounds.OuterWidthInt + 28, Bounds.OuterHeightInt + 28);
		Context ctx = new Context(surface);
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		ctx.Paint();
		GuiElement.RoundRectangle(ctx, 12.0, 12.0, Bounds.OuterWidthInt + 4, Bounds.OuterHeightInt + 4, 1.0);
		ctx.SetSourceRGB(color[0], color[1], color[2]);
		ctx.FillPreserve();
		surface.BlurFull(3.0);
		ctx.Fill();
		surface.BlurFull(2.0);
		GuiElement.RoundRectangle(ctx, 15.0, 15.0, Bounds.OuterWidthInt - 2, Bounds.OuterHeightInt - 2, 1.0);
		ctx.Operator = Operator.Clear;
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		ctx.Fill();
		api.Event.EnqueueMainThreadTask(delegate
		{
			generateTexture(surface, ref flashTexture);
			ctx.Dispose();
			surface.Dispose();
		}, "recompstatbar");
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		double x = Bounds.renderX;
		double y = Bounds.renderY;
		if (value == maxValue && HideWhenFull)
		{
			return;
		}
		if (hideable)
		{
			api.Render.RenderTexture(baseTexture.TextureId, x, y, Bounds.OuterWidthInt + 1, Bounds.OuterHeightInt + 1);
		}
		float alpha = 0f;
		if (ShouldFlash)
		{
			FlashTime += 6f * deltaTime;
			alpha = GameMath.Sin(FlashTime);
			if (alpha < 0f)
			{
				ShouldFlash = false;
				FlashTime = 0f;
			}
			if (FlashTime < (float)Math.PI / 2f)
			{
				alpha = Math.Min(1f, alpha * 3f);
			}
		}
		if (alpha > 0f)
		{
			api.Render.RenderTexture(flashTexture.TextureId, x - 14.0, y - 14.0, Bounds.OuterWidthInt + 28, Bounds.OuterHeightInt + 28, 50f, new Vec4f(1.5f, 1f, 1f, alpha));
		}
		if (barTexture.TextureId > 0)
		{
			api.Render.RenderTexture(barTexture.TextureId, x, y, Bounds.OuterWidthInt + 1, valueHeight);
		}
		if (ShowValueOnHover && Bounds.PointInside(api.Input.MouseX, api.Input.MouseY))
		{
			double tx = api.Input.MouseX + 16;
			double ty = api.Input.MouseY + valueTexture.Height - 4;
			api.Render.RenderTexture(valueTexture.TextureId, tx, ty, valueTexture.Width, valueTexture.Height, 2000f);
		}
	}

	/// <summary>
	/// Sets the line interval for the Status Bar.
	/// </summary>
	/// <param name="value">The value to set for the line interval/</param>
	public void SetLineInterval(float value)
	{
		lineInterval = value;
	}

	/// <summary>
	/// Sets the value for the status bar and updates the bar.
	/// </summary>
	/// <param name="value">The new value of the status bar.</param>
	public void SetValue(float value)
	{
		this.value = value;
		valuesSet = true;
		recomposeOverlays();
	}

	public float GetValue()
	{
		return value;
	}

	/// <summary>
	/// Sets the value for the status bar as well as the minimum and maximum values.
	/// </summary>
	/// <param name="value">The new value of the status bar.</param>
	/// <param name="min">The minimum value of the status bar.</param>
	/// <param name="max">The maximum value of the status bar.</param>
	public void SetValues(float value, float min, float max)
	{
		valuesSet = true;
		this.value = value;
		minValue = min;
		maxValue = max;
		recomposeOverlays();
	}

	/// <summary>
	/// Sets the minimum and maximum values of the status bar.
	/// </summary>
	/// <param name="min">The minimum value of the status bar.</param>
	/// <param name="max">The maximum value of the status bar.</param>
	public void SetMinMax(float min, float max)
	{
		minValue = min;
		maxValue = max;
		recomposeOverlays();
	}

	public override void Dispose()
	{
		base.Dispose();
		baseTexture?.Dispose();
		barTexture.Dispose();
		flashTexture.Dispose();
		valueTexture.Dispose();
	}
}
