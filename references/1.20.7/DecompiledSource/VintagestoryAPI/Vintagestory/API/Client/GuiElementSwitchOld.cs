using System;
using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementSwitchOld : GuiElementTextBase
{
	private Action<bool> handler;

	internal const double unscaledWidth = 60.0;

	internal const double unscaledHandleWidth = 30.0;

	internal const double unscaledHeight = 30.0;

	internal const double unscaledPadding = 3.0;

	private int offHandleTextureId;

	private int onHandleTextureId;

	public bool On;

	public GuiElementSwitchOld(ICoreClientAPI capi, Action<bool> OnToggled, ElementBounds bounds)
		: base(capi, "", null, bounds)
	{
		Font = CairoFont.WhiteSmallText().WithFontSize((float)GuiStyle.SubNormalFontSize);
		handler = OnToggled;
		bounds.fixedWidth = 60.0;
		bounds.fixedHeight = 30.0;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		ctxStatic.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
		GuiElement.RoundRectangle(ctxStatic, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, GuiStyle.ElementBGRadius);
		ctxStatic.Fill();
		EmbossRoundRectangleElement(ctxStatic, Bounds, inverse: true);
		createHandle("0", ref offHandleTextureId);
		createHandle("1", ref onHandleTextureId);
	}

	private void createHandle(string text, ref int textureId)
	{
		double handleWidth = GuiElement.scaled(30.0);
		double handleHeight = GuiElement.scaled(30.0) - 2.0 * GuiElement.scaled(3.0);
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)Math.Ceiling(handleWidth), (int)Math.Ceiling(handleHeight));
		Context ctx = genContext(surface);
		GuiElement.RoundRectangle(ctx, 0.0, 0.0, handleWidth, handleHeight, 1.0);
		GuiElement.fillWithPattern(api, ctx, GuiElement.stoneTextureName);
		EmbossRoundRectangleElement(ctx, 0.0, 0.0, handleWidth, handleHeight, inverse: false, 2, 1);
		Font.SetupContext(ctx);
		generateTexture(surface, ref textureId);
		ctx.Dispose();
		surface.Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		double handleWidth = GuiElement.scaled(30.0);
		double handleHeight = GuiElement.scaled(30.0) - 2.0 * GuiElement.scaled(3.0);
		double padding = GuiElement.scaled(3.0);
		api.Render.RenderTexture(On ? onHandleTextureId : offHandleTextureId, Bounds.renderX + (On ? (GuiElement.scaled(60.0) - handleWidth - 2.0 * padding) : 0.0) + padding, Bounds.renderY + padding, handleWidth, handleHeight);
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		On = !On;
		handler(On);
		api.Gui.PlaySound("toggleswitch");
	}
}
