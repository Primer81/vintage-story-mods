using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

/// <summary>
/// A configurable list of items.  An example of this is the controls in the settings menu.
/// </summary>
public class GuiElementConfigList : GuiElementTextBase
{
	public static double unscaledPadding = 2.0;

	public double leftWidthRel = 0.65;

	public double rightWidthRel = 0.3;

	public List<ConfigItem> items;

	private ConfigItemClickDelegate OnItemClick;

	private int textureId;

	private LoadedTexture hoverTexture;

	public ElementBounds innerBounds;

	public CairoFont errorFont;

	public CairoFont stdFont;

	public CairoFont titleFont;

	/// <summary>
	/// Creates a new dropdown configuration list.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="items">The list of items in the configuration.</param>
	/// <param name="OnItemClick">The event fired when the particular item is clicked.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="bounds">the bounds of the element.</param>
	public GuiElementConfigList(ICoreClientAPI capi, List<ConfigItem> items, ConfigItemClickDelegate OnItemClick, CairoFont font, ElementBounds bounds)
		: base(capi, "", font, bounds)
	{
		hoverTexture = new LoadedTexture(capi);
		this.items = items;
		this.OnItemClick = OnItemClick;
		errorFont = font.Clone();
		stdFont = font;
		titleFont = font.Clone().WithWeight(FontWeight.Bold);
		titleFont.Color[3] = 0.6;
	}

	/// <summary>
	/// Automatically adjusts the height of the element.
	/// </summary>
	public void Autoheight()
	{
		double totalHeight = 9.0;
		double pad = GuiElement.scaled(unscaledPadding);
		Bounds.CalcWorldBounds();
		bool first = true;
		foreach (ConfigItem item in items)
		{
			double lineHeight = Math.Max(textUtil.GetMultilineTextHeight(Font, item.Key, Bounds.InnerWidth * leftWidthRel), textUtil.GetMultilineTextHeight(Font, item.Value, Bounds.InnerWidth * rightWidthRel));
			if (!first && item.Type == EnumItemType.Title)
			{
				lineHeight += GuiElement.scaled(20.0);
			}
			totalHeight += pad + lineHeight + pad;
			first = false;
		}
		innerBounds = Bounds.FlatCopy();
		innerBounds.fixedHeight = totalHeight / (double)RuntimeEnv.GUIScale;
		innerBounds.CalcWorldBounds();
	}

	public override void ComposeTextElements(Context ctxs, ImageSurface surfaces)
	{
		ImageSurface surface = new ImageSurface(Format.Argb32, 200, 10);
		Context context = genContext(surface);
		context.SetSourceRGBA(1.0, 1.0, 1.0, 0.4);
		context.Paint();
		generateTexture(surface, ref hoverTexture);
		context.Dispose();
		surface.Dispose();
		Refresh();
	}

	/// <summary>
	/// Refreshes the Config List.
	/// </summary>
	public void Refresh()
	{
		Autoheight();
		ImageSurface surface = new ImageSurface(Format.Argb32, innerBounds.OuterWidthInt, innerBounds.OuterHeightInt);
		Context ctx = genContext(surface);
		double height = 4.0;
		double pad = GuiElement.scaled(unscaledPadding);
		bool first = true;
		foreach (ConfigItem item in items)
		{
			if (item.error)
			{
				Font = errorFont;
			}
			else
			{
				Font = stdFont;
			}
			if (item.Type == EnumItemType.Title)
			{
				Font = titleFont;
			}
			double offY = ((!first && item.Type == EnumItemType.Title) ? GuiElement.scaled(20.0) : 0.0);
			double leftHeight = textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, item.Key, 0.0, offY + height + pad, innerBounds.InnerWidth * leftWidthRel);
			double rightHeight = textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, item.Value, innerBounds.InnerWidth * (1.0 - rightWidthRel), offY + height + pad, innerBounds.InnerWidth * rightWidthRel);
			double itemHeight = offY + pad + Math.Max(leftHeight, rightHeight) + pad;
			item.posY = height;
			item.height = itemHeight;
			height += itemHeight;
			first = false;
		}
		generateTexture(surface, ref textureId);
		surface.Dispose();
		ctx.Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.Render2DTexturePremultipliedAlpha(textureId, innerBounds);
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		if (!innerBounds.PointInside(mouseX, mouseY))
		{
			return;
		}
		foreach (ConfigItem item in items)
		{
			double diff = (double)mouseY - (item.posY + innerBounds.absY);
			if (item.Type != EnumItemType.Title && diff > 0.0 && diff < item.height)
			{
				api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, (int)innerBounds.absX, (int)innerBounds.absY + (int)item.posY, innerBounds.InnerWidth, item.height);
			}
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (!innerBounds.ParentBounds.PointInside(args.X, args.Y))
		{
			return;
		}
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		if (!innerBounds.PointInside(mouseX, mouseY))
		{
			return;
		}
		int elemIndex = 0;
		int elemNoTitleIndex = 0;
		foreach (ConfigItem item in items)
		{
			double diff = (double)mouseY - (item.posY + innerBounds.absY);
			if (item.Type != EnumItemType.Title && diff > 0.0 && diff < item.height)
			{
				OnItemClick(elemIndex, elemNoTitleIndex);
			}
			elemIndex++;
			if (item.Type != EnumItemType.Title)
			{
				elemNoTitleIndex++;
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		hoverTexture.Dispose();
	}
}
