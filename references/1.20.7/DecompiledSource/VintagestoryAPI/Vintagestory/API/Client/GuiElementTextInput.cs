using System;
using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementTextInput : GuiElementEditableTextBase
{
	protected LoadedTexture highlightTexture;

	protected ElementBounds highlightBounds;

	internal bool DeleteOnRefocusBackSpace;

	protected int refocusStage;

	private LoadedTexture placeHolderTextTexture;

	private bool focusLostSinceKeyDown;

	/// <summary>
	/// Adds a text input to the GUI
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="bounds">The bounds of the text input.</param>
	/// <param name="onTextChanged">The event fired when the text is changed.</param>
	/// <param name="font">The font of the text.</param>
	public GuiElementTextInput(ICoreClientAPI capi, ElementBounds bounds, Action<string> onTextChanged, CairoFont font)
		: base(capi, font, bounds)
	{
		MouseOverCursor = "textselect";
		OnTextChanged = onTextChanged;
		highlightTexture = new LoadedTexture(capi);
	}

	/// <summary>
	/// Tells the text component to hide the characters in the text.
	/// </summary>
	public void HideCharacters()
	{
		hideCharacters = true;
	}

	public void SetPlaceHolderText(string text)
	{
		TextTextureUtil util = new TextTextureUtil(api);
		placeHolderTextTexture?.Dispose();
		CairoFont font = Font.Clone();
		font.Color[3] *= 0.5;
		placeHolderTextTexture = util.GenTextTexture(text, font);
	}

	public override void ComposeTextElements(Context ctx, ImageSurface surface)
	{
		EmbossRoundRectangleElement(ctx, Bounds, inverse: true, 2, 1);
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.3);
		ElementRoundRectangle(ctx, Bounds, isBackground: false, 1.0);
		ctx.Fill();
		ImageSurface surfaceHighlight = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context context = genContext(surfaceHighlight);
		context.SetSourceRGBA(1.0, 1.0, 1.0, 0.3);
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
		if (base.HasFocus)
		{
			api.Render.GlToggleBlend(blend: true);
			api.Render.Render2DTexture(highlightTexture.TextureId, highlightBounds);
		}
		else if (placeHolderTextTexture != null && (text == null || text.Length == 0) && (lines == null || lines.Count == 0 || lines[0] == null || lines[0] == ""))
		{
			api.Render.GlToggleBlend(blend: true);
			api.Render.Render2DTexturePremultipliedAlpha(placeHolderTextTexture.TextureId, (int)(highlightBounds.renderX + highlightBounds.absPaddingX + 3.0), (int)(highlightBounds.renderY + highlightBounds.absPaddingY + (highlightBounds.OuterHeight - (double)placeHolderTextTexture.Height) / 2.0), placeHolderTextTexture.Width, placeHolderTextTexture.Height);
		}
		api.Render.GlScissor((int)Bounds.renderX, (int)((double)api.Render.FrameHeight - Bounds.renderY - Bounds.InnerHeight), Math.Max(0, Bounds.OuterWidthInt + 1 - (int)rightSpacing), Math.Max(0, Bounds.OuterHeightInt + 1 - (int)bottomSpacing));
		api.Render.GlScissorFlag(enable: true);
		api.Render.Render2DTexturePremultipliedAlpha(textTexture.TextureId, Bounds.renderX - renderLeftOffset, Bounds.renderY, textSize.X, textSize.Y);
		api.Render.GlScissorFlag(enable: false);
		base.RenderInteractiveElements(deltaTime);
	}

	public override void OnFocusLost()
	{
		focusLostSinceKeyDown = true;
		base.OnFocusLost();
	}

	public override void OnFocusGained()
	{
		base.OnFocusGained();
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (DeleteOnRefocusBackSpace && args.KeyCode == 53 && focusLostSinceKeyDown)
		{
			SetValue("");
			return;
		}
		focusLostSinceKeyDown = false;
		base.OnKeyDown(api, args);
	}

	public override void Dispose()
	{
		base.Dispose();
		highlightTexture.Dispose();
		placeHolderTextTexture?.Dispose();
	}
}
