using System;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

/// <summary>
/// Text that can be changed after being composed
/// </summary>
public class GuiElementDynamicText : GuiElementTextBase
{
	private EnumTextOrientation orientation;

	private LoadedTexture textTexture;

	public Action OnClick;

	public bool autoHeight;

	public int QuantityTextLines => textUtil.GetQuantityTextLines(Font, text, Bounds.InnerWidth);

	/// <summary>
	/// Adds a new element that renders text dynamically.
	/// </summary>
	/// <param name="capi">The client API.</param>
	/// <param name="text">The starting text on the component.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="bounds">the bounds of the text.</param>
	public GuiElementDynamicText(ICoreClientAPI capi, string text, CairoFont font, ElementBounds bounds)
		: base(capi, text, font, bounds)
	{
		orientation = font.Orientation;
		textTexture = new LoadedTexture(capi);
	}

	public override void ComposeTextElements(Context ctx, ImageSurface surface)
	{
		RecomposeText();
	}

	/// <summary>
	/// Automatically adjusts the height of the dynamic text.
	/// </summary>
	public void AutoHeight()
	{
		Bounds.fixedHeight = GetMultilineTextHeight() / (double)RuntimeEnv.GUIScale;
		Bounds.CalcWorldBounds();
		autoHeight = true;
	}

	/// <summary>
	/// Recomposes the element for lines.
	/// </summary>
	public void RecomposeText(bool async = false)
	{
		if (autoHeight)
		{
			AutoHeight();
		}
		if (async)
		{
			TyronThreadPool.QueueTask(delegate
			{
				ImageSurface surface2 = new ImageSurface(Format.Argb32, (int)Bounds.InnerWidth, (int)Bounds.InnerHeight);
				Context ctx2 = genContext(surface2);
				DrawMultilineTextAt(ctx2, 0.0, 0.0, orientation);
				api.Event.EnqueueMainThreadTask(delegate
				{
					generateTexture(surface2, ref textTexture);
					ctx2.Dispose();
					surface2.Dispose();
				}, "recompstatbar");
			});
		}
		else
		{
			ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.InnerWidth, (int)Bounds.InnerHeight);
			Context ctx = genContext(surface);
			DrawMultilineTextAt(ctx, 0.0, 0.0, orientation);
			generateTexture(surface, ref textTexture);
			ctx.Dispose();
			surface.Dispose();
		}
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.Render2DTexturePremultipliedAlpha(textTexture.TextureId, (int)Bounds.renderX, (int)Bounds.renderY, (int)Bounds.InnerWidth, (int)Bounds.InnerHeight);
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		OnClick?.Invoke();
	}

	public void SetNewTextAsync(string text, bool autoHeight = false, bool forceRedraw = false)
	{
		SetNewText(text, autoHeight, forceRedraw, async: true);
	}

	/// <summary>
	/// Sets the text value of the element.
	/// </summary>
	/// <param name="text">The text of the component.</param>
	/// <param name="autoHeight">Whether the height of the component should be modified.</param>
	/// <param name="forceRedraw">Whether the element should be redrawn.</param>
	/// <param name="async"></param>
	public void SetNewText(string text, bool autoHeight = false, bool forceRedraw = false, bool async = false)
	{
		if (base.text != text || forceRedraw)
		{
			base.text = text;
			Bounds.CalcWorldBounds();
			if (autoHeight)
			{
				AutoHeight();
			}
			RecomposeText(async);
		}
	}

	public override void Dispose()
	{
		textTexture?.Dispose();
	}
}
