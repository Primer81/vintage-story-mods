using System;
using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementChatInput : GuiElementEditableTextBase
{
	private LoadedTexture highlightTexture;

	private ElementBounds highlightBounds;

	/// <summary>
	/// Adds a chat input element to the UI.
	/// </summary>
	/// <param name="capi">The client API</param>
	/// <param name="bounds">The bounds of the chat input.</param>
	/// <param name="OnTextChanged">The event fired when the text is altered.</param>
	public GuiElementChatInput(ICoreClientAPI capi, ElementBounds bounds, Action<string> OnTextChanged)
		: base(capi, null, bounds)
	{
		highlightTexture = new LoadedTexture(capi);
		base.OnTextChanged = OnTextChanged;
		caretColor = new float[4] { 1f, 1f, 1f, 1f };
		Font = CairoFont.WhiteSmallText();
	}

	public override void ComposeTextElements(Context ctx, ImageSurface surface)
	{
		ctx.LineWidth = 1.0;
		ctx.NewPath();
		ctx.MoveTo(Bounds.drawX + 1.0, Bounds.drawY);
		ctx.LineTo(Bounds.drawX + 1.0 + Bounds.InnerWidth, Bounds.drawY);
		ctx.ClosePath();
		ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.7);
		ctx.Stroke();
		ctx.NewPath();
		ctx.MoveTo(Bounds.drawX + 1.0, Bounds.drawY + 1.0);
		ctx.LineTo(Bounds.drawX + 1.0 + Bounds.InnerWidth, Bounds.drawY + 1.0);
		ctx.ClosePath();
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.7);
		ctx.Stroke();
		ImageSurface surfaceHighlight = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context context = genContext(surfaceHighlight);
		context.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		context.Paint();
		context.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
		context.Paint();
		generateTexture(surfaceHighlight, ref highlightTexture);
		context.Dispose();
		surfaceHighlight.Dispose();
		highlightBounds = Bounds.CopyOffsetedSibling().WithFixedPadding(0.0, 0.0).FixedGrow(2.0 * Bounds.absPaddingX, 2.0 * Bounds.absPaddingY);
		highlightBounds.CalcWorldBounds();
		RecomposeText();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (hasFocus)
		{
			api.Render.Render2DTexturePremultipliedAlpha(highlightTexture.TextureId, highlightBounds);
		}
		api.Render.GlScissor((int)Bounds.renderX, (int)((double)api.Render.FrameHeight - Bounds.renderY - Bounds.InnerHeight), Bounds.OuterWidthInt + 1 - (int)rightSpacing, Bounds.OuterHeightInt + 1 - (int)bottomSpacing);
		api.Render.GlScissorFlag(enable: true);
		api.Render.Render2DTexturePremultipliedAlpha(textTexture.TextureId, Bounds.renderX - renderLeftOffset, Bounds.renderY, textSize.X, textSize.Y);
		api.Render.GlScissorFlag(enable: false);
		base.RenderInteractiveElements(deltaTime);
	}

	public override void Dispose()
	{
		base.Dispose();
		highlightTexture.Dispose();
	}
}
