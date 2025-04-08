using System;
using Cairo;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementHoverText : GuiElementTextBase
{
	public static TextBackground DefaultBackground = new TextBackground
	{
		Padding = 5,
		Radius = 1.0,
		FillColor = GuiStyle.DialogStrongBgColor,
		BorderColor = GuiStyle.DialogBorderColor,
		BorderWidth = 3.0,
		Shade = true
	};

	private LoadedTexture hoverTexture;

	private int unscaledMaxWidth;

	private double hoverWidth;

	private double hoverHeight;

	private bool autoDisplay = true;

	private bool visible;

	private bool isnowshown;

	private bool followMouse = true;

	private bool autoWidth;

	public bool fillBounds;

	public TextBackground Background;

	private Vec4f rendercolor;

	private double padding;

	private float zPosition = 500f;

	private GuiElementRichtext descriptionElement;

	public Vec4f RenderColor
	{
		get
		{
			return rendercolor;
		}
		set
		{
			rendercolor = value;
			descriptionElement.RenderColor = value;
		}
	}

	public float ZPosition
	{
		get
		{
			return zPosition;
		}
		set
		{
			zPosition = value;
			descriptionElement.zPos = value;
		}
	}

	public bool IsVisible => visible;

	public bool IsNowShown => isnowshown;

	public override double DrawOrder => 0.9;

	/// <summary>
	/// Creates a new instance of hover text.
	/// </summary>
	/// <param name="capi">The client API.</param>
	/// <param name="text">The text of the text.</param>
	/// <remarks>For the text and the text.</remarks>
	/// <param name="font">The font of the text.</param>
	/// <param name="maxWidth">The width of the text.</param>
	/// <param name="bounds">the bounds of the text.</param>
	/// <param name="background"></param>
	public GuiElementHoverText(ICoreClientAPI capi, string text, CairoFont font, int maxWidth, ElementBounds bounds, TextBackground background = null)
		: base(capi, text, font, bounds)
	{
		Background = background;
		if (background == null)
		{
			Background = DefaultBackground;
		}
		unscaledMaxWidth = maxWidth;
		hoverTexture = new LoadedTexture(capi);
		padding = Background.HorPadding;
		ElementBounds descBounds = bounds.CopyOnlySize();
		descBounds.WithFixedPadding(0.0);
		descBounds.WithParent(bounds);
		descBounds.IsDrawingSurface = true;
		descBounds.fixedWidth = maxWidth;
		descriptionElement = new GuiElementRichtext(capi, new RichTextComponentBase[0], descBounds);
		descriptionElement.zPos = 1001f;
	}

	public override void BeforeCalcBounds()
	{
		base.BeforeCalcBounds();
		descriptionElement.BeforeCalcBounds();
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
	}

	public override int OutlineColor()
	{
		return -2130706688;
	}

	public override void RenderBoundsDebug()
	{
		api.Render.RenderRectangle((int)Bounds.renderX, (int)Bounds.renderY, 550f, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight, OutlineColor());
	}

	private void RecalcBounds()
	{
		double currentWidth = descriptionElement.Bounds.fixedWidth;
		currentWidth = Math.Min(autoWidth ? (descriptionElement.MaxLineWidth / (double)RuntimeEnv.GUIScale) : currentWidth, unscaledMaxWidth);
		hoverWidth = currentWidth + 2.0 * padding;
		double currentHeight = Math.Max(descriptionElement.Bounds.fixedHeight + 2.0 * padding, 20.0);
		hoverHeight = GuiElement.scaled(currentHeight);
		hoverWidth = GuiElement.scaled(hoverWidth);
	}

	private void Recompose()
	{
		descriptionElement.SetNewText(text, Font);
		RecalcBounds();
		Bounds.CalcWorldBounds();
		Bounds.CopyOnlySize().CalcWorldBounds();
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)Math.Ceiling(hoverWidth), (int)Math.Ceiling(hoverHeight));
		Context ctx = genContext(surface);
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		ctx.Paint();
		if (Background?.FillColor != null)
		{
			ctx.SetSourceRGBA(Background.FillColor);
			GuiElement.RoundRectangle(ctx, 0.0, 0.0, hoverWidth, hoverHeight, Background.Radius);
			ctx.Fill();
		}
		TextBackground background = Background;
		if (background != null && background.Shade)
		{
			ctx.SetSourceRGBA(GuiStyle.DialogLightBgColor[0] * 1.4, GuiStyle.DialogStrongBgColor[1] * 1.4, GuiStyle.DialogStrongBgColor[2] * 1.4, 1.0);
			GuiElement.RoundRectangle(ctx, 0.0, 0.0, hoverWidth, hoverHeight, Background.Radius);
			ctx.LineWidth = Background.BorderWidth * 1.75;
			ctx.Stroke();
			surface.BlurFull(8.2);
		}
		if (Background?.BorderColor != null)
		{
			ctx.SetSourceRGBA(Background.BorderColor);
			GuiElement.RoundRectangle(ctx, 0.0, 0.0, hoverWidth, hoverHeight, Background.Radius);
			ctx.LineWidth = Background.BorderWidth;
			ctx.Stroke();
		}
		generateTexture(surface, ref hoverTexture);
		ctx.Dispose();
		surface.Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (text == null || text.Length == 0)
		{
			return;
		}
		if (api.Render.ScissorStack.Count > 0)
		{
			api.Render.GlScissorFlag(enable: false);
		}
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		isnowshown = false;
		if ((autoDisplay && IsPositionInside(mouseX, mouseY)) || visible)
		{
			isnowshown = true;
			if (hoverTexture.TextureId == 0 && !hoverTexture.Disposed)
			{
				Recompose();
			}
			int pad = (int)GuiElement.scaled(padding);
			double x = Bounds.renderX;
			double y = Bounds.renderY;
			if (followMouse)
			{
				x = (double)mouseX + GuiElement.scaled(10.0);
				y = (double)mouseY + GuiElement.scaled(15.0);
			}
			if (x + hoverWidth > (double)api.Render.FrameWidth)
			{
				x -= x + hoverWidth - (double)api.Render.FrameWidth;
			}
			if (y + hoverHeight > (double)api.Render.FrameHeight)
			{
				y -= y + hoverHeight - (double)api.Render.FrameHeight;
			}
			api.Render.Render2DTexture(hoverTexture.TextureId, (int)x + (int)Bounds.absPaddingX, (int)y + (int)Bounds.absPaddingY, (int)hoverWidth + 1, (int)hoverHeight + 1, zPosition, RenderColor);
			Bounds.renderOffsetX = x - Bounds.renderX + (double)pad;
			Bounds.renderOffsetY = y - Bounds.renderY + (double)pad;
			descriptionElement.RenderColor = rendercolor;
			descriptionElement.RenderAsPremultipliedAlpha = base.RenderAsPremultipliedAlpha;
			descriptionElement.RenderInteractiveElements(deltaTime);
			Bounds.renderOffsetX = 0.0;
			Bounds.renderOffsetY = 0.0;
		}
		if (api.Render.ScissorStack.Count > 0)
		{
			api.Render.GlScissorFlag(enable: true);
		}
	}

	/// <summary>
	/// Sets the text of the component and changes it.
	/// </summary>
	/// <param name="text">The text to change.</param>
	public void SetNewText(string text)
	{
		base.text = text;
		Recompose();
	}

	/// <summary>
	/// Sets whether the text automatically displays or not.
	/// </summary>
	/// <param name="on">Whether the text is displayed.</param>
	public void SetAutoDisplay(bool on)
	{
		autoDisplay = on;
	}

	/// <summary>
	/// Sets the visibility to the 
	/// </summary>
	/// <param name="on"></param>
	public void SetVisible(bool on)
	{
		visible = on;
	}

	/// <summary>
	/// Sets whether or not the width of the component should automatiocally adjust.
	/// </summary>
	public void SetAutoWidth(bool on)
	{
		autoWidth = on;
	}

	public void SetFollowMouse(bool on)
	{
		followMouse = on;
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
	}

	public override void Dispose()
	{
		base.Dispose();
		hoverTexture.Dispose();
		descriptionElement.Dispose();
	}
}
