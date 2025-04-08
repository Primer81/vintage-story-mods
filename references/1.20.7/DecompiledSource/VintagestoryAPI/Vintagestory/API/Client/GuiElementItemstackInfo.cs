using System;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class GuiElementItemstackInfo : GuiElementTextBase
{
	public bool Dirty;

	public bool Render = true;

	public GuiElementRichtext titleElement;

	public GuiElementRichtext descriptionElement;

	public LoadedTexture texture;

	public static double ItemStackSize = GuiElementPassiveItemSlot.unscaledItemSize * 2.5;

	public static int MarginTop = 24;

	public static int BoxWidth = 415;

	public static int MinBoxHeight = 80;

	private static double[] backTint = GuiStyle.DialogStrongBgColor;

	public ItemSlot curSlot;

	private ItemStack curStack;

	private CairoFont titleFont;

	private double maxWidth;

	private InfoTextDelegate OnRequireInfoText;

	private ElementBounds scissorBounds;

	public Action onRenderStack;

	public string[] RecompCheckIgnoredStackAttributes;

	/// <summary>
	/// Creates an ItemStackInfo element.
	/// </summary>
	/// <param name="capi">The client API</param>
	/// <param name="bounds">The bounds of the object.</param>
	/// <param name="OnRequireInfoText">The function that is called when an item information is called.</param>
	public GuiElementItemstackInfo(ICoreClientAPI capi, ElementBounds bounds, InfoTextDelegate OnRequireInfoText)
		: base(capi, "", CairoFont.WhiteSmallText(), bounds)
	{
		this.OnRequireInfoText = OnRequireInfoText;
		texture = new LoadedTexture(capi);
		ElementBounds textBounds = bounds.CopyOnlySize();
		ElementBounds descBounds = textBounds.CopyOffsetedSibling(ItemStackSize + 50.0, MarginTop, 0.0 - ItemStackSize - 50.0);
		descBounds.WithParent(bounds);
		textBounds.WithParent(bounds);
		descriptionElement = new GuiElementRichtext(capi, new RichTextComponentBase[0], descBounds);
		descriptionElement.zPos = 1001f;
		titleFont = Font.Clone();
		titleFont.FontWeight = FontWeight.Bold;
		titleElement = new GuiElementRichtext(capi, new RichTextComponentBase[0], textBounds);
		titleElement.zPos = 1001f;
		maxWidth = bounds.fixedWidth;
		onRenderStack = delegate
		{
			double num = (int)GuiElement.scaled(30.0 + ItemStackSize / 2.0);
			api.Render.RenderItemstackToGui(curSlot, (double)(int)Bounds.renderX + num, (double)(int)Bounds.renderY + num + (double)(int)GuiElement.scaled(MarginTop), 1000.0 + GuiElement.scaled(GuiElementPassiveItemSlot.unscaledItemSize) * 2.0, (float)GuiElement.scaled(ItemStackSize), -1, shading: true, rotate: true, showStackSize: false);
		};
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
	}

	private void RecalcBounds()
	{
		descriptionElement.BeforeCalcBounds();
		titleElement.BeforeCalcBounds();
		double currentWidth = Math.Max(titleElement.MaxLineWidth / (double)RuntimeEnv.GUIScale, descriptionElement.MaxLineWidth / (double)RuntimeEnv.GUIScale + 10.0 + 40.0 + GuiElementPassiveItemSlot.unscaledItemSize * 3.0);
		currentWidth = Math.Min(currentWidth, maxWidth);
		double descWidth = currentWidth - ItemStackSize - 50.0;
		Bounds.fixedWidth = currentWidth;
		descriptionElement.Bounds.fixedWidth = descWidth;
		titleElement.Bounds.fixedWidth = currentWidth;
		descriptionElement.Bounds.CalcWorldBounds();
		double unscaledTotalHeight = Math.Max(descriptionElement.Bounds.fixedHeight, 25.0 + GuiElementPassiveItemSlot.unscaledItemSize * 3.0);
		titleElement.Bounds.fixedHeight = unscaledTotalHeight;
		descriptionElement.Bounds.fixedHeight = unscaledTotalHeight;
		Bounds.fixedHeight = 25.0 + unscaledTotalHeight;
	}

	public void AsyncRecompose()
	{
		if (curSlot?.Itemstack == null)
		{
			return;
		}
		Dirty = true;
		string title = curSlot.GetStackName();
		string desc = OnRequireInfoText(curSlot);
		desc.TrimEnd();
		titleElement.Bounds.fixedWidth = maxWidth - 10.0;
		descriptionElement.Bounds.fixedWidth = maxWidth - 40.0 - GuiElement.scaled(GuiElementPassiveItemSlot.unscaledItemSize) * 3.0 - 10.0;
		descriptionElement.Bounds.CalcWorldBounds();
		titleElement.Bounds.CalcWorldBounds();
		titleElement.SetNewTextWithoutRecompose(title, titleFont, null, recalcBounds: true);
		descriptionElement.SetNewTextWithoutRecompose(desc, Font, null, recalcBounds: true);
		RecalcBounds();
		Bounds.CalcWorldBounds();
		ElementBounds textBounds = Bounds.CopyOnlySize();
		textBounds.CalcWorldBounds();
		TyronThreadPool.QueueTask(delegate
		{
			ImageSurface surface = new ImageSurface(Format.Argb32, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
			Context ctx = genContext(surface);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
			ctx.Paint();
			ctx.SetSourceRGBA(backTint[0], backTint[1], backTint[2], backTint[3]);
			GuiElement.RoundRectangle(ctx, textBounds.bgDrawX, textBounds.bgDrawY, textBounds.OuterWidthInt, textBounds.OuterHeightInt, GuiStyle.DialogBGRadius);
			ctx.FillPreserve();
			ctx.SetSourceRGBA(GuiStyle.DialogLightBgColor[0] * 1.4, GuiStyle.DialogStrongBgColor[1] * 1.4, GuiStyle.DialogStrongBgColor[2] * 1.4, 1.0);
			ctx.LineWidth = 5.25;
			ctx.StrokePreserve();
			surface.BlurFull(8.2);
			ctx.SetSourceRGBA(backTint[0] / 2.0, backTint[1] / 2.0, backTint[2] / 2.0, backTint[3]);
			ctx.Stroke();
			int num = (int)(GuiElement.scaled(ItemStackSize) + GuiElement.scaled(40.0));
			int num2 = (int)(GuiElement.scaled(ItemStackSize) + GuiElement.scaled(40.0));
			ImageSurface imageSurface = new ImageSurface(Format.Argb32, num, num2);
			Context context = genContext(imageSurface);
			context.SetSourceRGBA(GuiStyle.DialogSlotBackColor);
			GuiElement.RoundRectangle(context, 0.0, 0.0, num, num2, 0.0);
			context.FillPreserve();
			context.SetSourceRGBA(GuiStyle.DialogSlotFrontColor);
			context.LineWidth = 5.0;
			context.Stroke();
			imageSurface.BlurFull(7.0);
			imageSurface.BlurFull(7.0);
			imageSurface.BlurFull(7.0);
			EmbossRoundRectangleElement(context, 0.0, 0.0, num, num2, inverse: true);
			ctx.SetSourceSurface(imageSurface, (int)textBounds.drawX, (int)(textBounds.drawY + GuiElement.scaled(MarginTop)));
			ctx.Rectangle(textBounds.drawX, textBounds.drawY + GuiElement.scaled(MarginTop), num, num2);
			ctx.Fill();
			context.Dispose();
			imageSurface.Dispose();
			api.Event.EnqueueMainThreadTask(delegate
			{
				titleElement.Compose();
				descriptionElement.Compose();
				generateTexture(surface, ref texture);
				ctx.Dispose();
				surface.Dispose();
				double num3 = (int)(30.0 + ItemStackSize / 2.0);
				scissorBounds = ElementBounds.Fixed(4.0 + num3 - ItemStackSize, 2.0 + num3 + (double)MarginTop - ItemStackSize, ItemStackSize + 38.0, ItemStackSize + 38.0).WithParent(Bounds);
				scissorBounds.CalcWorldBounds();
				Dirty = false;
			}, "genstackinfotexture");
		});
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (curSlot?.Itemstack != null && !Dirty && Render)
		{
			api.Render.Render2DTexturePremultipliedAlpha(texture.TextureId, Bounds, 1000f);
			titleElement.RenderInteractiveElements(deltaTime);
			descriptionElement.RenderInteractiveElements(deltaTime);
			api.Render.PushScissor(scissorBounds);
			onRenderStack();
			api.Render.PopScissor();
		}
	}

	/// <summary>
	/// Gets the item slot for this stack info.
	/// </summary>
	/// <returns></returns>
	public ItemSlot GetSlot()
	{
		return curSlot;
	}

	/// <summary>
	/// Sets the source slot for stacks.
	/// </summary>
	/// <param name="nowSlot"></param>
	/// <param name="forceRecompose"></param>
	/// <returns>True if recomposed</returns>
	public bool SetSourceSlot(ItemSlot nowSlot, bool forceRecompose = false)
	{
		bool num = forceRecompose || curStack == null != (nowSlot?.Itemstack == null) || (nowSlot?.Itemstack != null && !nowSlot.Itemstack.Equals(api.World, curStack, RecompCheckIgnoredStackAttributes));
		if (nowSlot?.Itemstack == null)
		{
			curSlot = null;
		}
		if (num)
		{
			curSlot = nowSlot;
			curStack = nowSlot?.Itemstack?.Clone();
			if (nowSlot?.Itemstack == null)
			{
				Bounds.fixedHeight = 0.0;
			}
			AsyncRecompose();
		}
		return num;
	}

	public override void Dispose()
	{
		base.Dispose();
		texture.Dispose();
		descriptionElement?.Dispose();
		titleElement?.Dispose();
	}
}
