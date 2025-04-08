using System;
using System.Collections.Generic;
using Cairo;

namespace Vintagestory.API.Client;

/// <summary>
/// A slot for item skills.
/// </summary>
public class GuiElementSkillItemGrid : GuiElement
{
	private List<SkillItem> skillItems;

	private int cols;

	private int rows;

	public Action<int> OnSlotClick;

	public Action<int> OnSlotOver;

	public int selectedIndex = -1;

	private LoadedTexture hoverTexture;

	public override bool Focusable => true;

	/// <summary>
	/// Creates a Skill Item Grid.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="skillItems">The items with skills.</param>
	/// <param name="columns">The columns of the Item Grid</param>
	/// <param name="rows">The Rows of the Item Grid.</param>
	/// <param name="OnSlotClick">The event fired when the slot is clicked.</param>
	/// <param name="bounds">The bounds of the Item Grid.</param>
	public GuiElementSkillItemGrid(ICoreClientAPI capi, List<SkillItem> skillItems, int columns, int rows, Action<int> OnSlotClick, ElementBounds bounds)
		: base(capi, bounds)
	{
		hoverTexture = new LoadedTexture(capi);
		this.skillItems = skillItems;
		cols = columns;
		this.rows = rows;
		this.OnSlotClick = OnSlotClick;
		Bounds.fixedHeight = (double)rows * (GuiElementItemSlotGridBase.unscaledSlotPadding + GuiElementPassiveItemSlot.unscaledSlotSize);
		Bounds.fixedWidth = (double)columns * (GuiElementItemSlotGridBase.unscaledSlotPadding + GuiElementPassiveItemSlot.unscaledSlotSize);
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		ComposeSlots(ctx, surface);
		ComposeHover();
	}

	private void ComposeSlots(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		double slotPadding = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
		double slotWidth = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		double slotHeight = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		for (int row = 0; row < rows; row++)
		{
			for (int col = 0; col < cols; col++)
			{
				double posX = (double)col * (slotWidth + slotPadding);
				double posY = (double)row * (slotHeight + slotPadding);
				ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.2);
				GuiElement.RoundRectangle(ctx, Bounds.drawX + posX, Bounds.drawY + posY, slotWidth, slotHeight, GuiStyle.ElementBGRadius);
				ctx.Fill();
				EmbossRoundRectangleElement(ctx, Bounds.drawX + posX, Bounds.drawY + posY, slotWidth, slotHeight, inverse: true);
			}
		}
	}

	private void ComposeHover()
	{
		double slotWidth = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		double slotHeight = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)slotWidth - 2, (int)slotHeight - 2);
		Context context = genContext(surface);
		context.SetSourceRGBA(1.0, 1.0, 1.0, 0.7);
		GuiElement.RoundRectangle(context, 1.0, 1.0, slotWidth, slotHeight, GuiStyle.ElementBGRadius);
		context.Fill();
		generateTexture(surface, ref hoverTexture);
		context.Dispose();
		surface.Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		double slotPadding = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
		double slotWidth = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		double slotHeight = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		int dx = api.Input.MouseX - (int)Bounds.absX;
		int dy = api.Input.MouseY - (int)Bounds.absY;
		for (int i = 0; i < rows * cols; i++)
		{
			int num = i / cols;
			double posX = (double)(i % cols) * (slotWidth + slotPadding);
			double posY = (double)num * (slotHeight + slotPadding);
			bool over = (double)dx >= posX && (double)dy >= posY && (double)dx < posX + slotWidth + slotPadding && (double)dy < posY + slotHeight + slotPadding;
			if (over || i == selectedIndex)
			{
				api.Render.Render2DTexture(hoverTexture.TextureId, (float)(Bounds.renderX + posX), (float)(Bounds.renderY + posY), (float)slotWidth, (float)slotHeight);
				if (over)
				{
					OnSlotOver?.Invoke(i);
				}
			}
			if (skillItems.Count <= i)
			{
				continue;
			}
			SkillItem skillItem = skillItems[i];
			if (skillItem == null)
			{
				continue;
			}
			if (skillItem.Texture != null)
			{
				if (skillItem.TexturePremultipliedAlpha)
				{
					api.Render.Render2DTexturePremultipliedAlpha(skillItem.Texture.TextureId, Bounds.renderX + posX + 1.0, Bounds.renderY + posY + 1.0, slotWidth, slotHeight);
				}
				else
				{
					api.Render.Render2DTexture(skillItem.Texture.TextureId, (float)(Bounds.renderX + posX + 1.0), (float)(Bounds.renderY + posY + 1.0), (float)slotWidth, (float)slotHeight);
				}
			}
			skillItem.RenderHandler?.Invoke(skillItem.Code, deltaTime, Bounds.renderX + posX + 1.0, Bounds.renderY + posY + 1.0);
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		int dx = api.Input.MouseX - (int)Bounds.absX;
		int num = api.Input.MouseY - (int)Bounds.absY;
		double slotPadding = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
		double slotWidth = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		double slotHeight = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		int num2 = (int)((double)num / (slotHeight + slotPadding));
		int col = (int)((double)dx / (slotWidth + slotPadding));
		int index = num2 * cols + col;
		if (index >= 0 && index < skillItems.Count)
		{
			OnSlotClick?.Invoke(index);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		hoverTexture.Dispose();
	}
}
