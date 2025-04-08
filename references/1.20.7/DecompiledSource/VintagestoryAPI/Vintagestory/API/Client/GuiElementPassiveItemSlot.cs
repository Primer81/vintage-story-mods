using Cairo;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

/// <summary>
/// Displays a single slot from given inventory, cannot be directly interacted with. Currently used for the mouse slot
/// </summary>
public class GuiElementPassiveItemSlot : GuiElement
{
	public static double unscaledItemSize = 25.600000381469727;

	public static double unscaledSlotSize = 48.0;

	private ItemSlot slot;

	private IInventory inventory;

	private bool drawBackground;

	private GuiElementStaticText textComposer;

	/// <summary>
	/// Creates a new passive item slot.
	/// </summary>
	/// <param name="capi">The client API</param>
	/// <param name="bounds">the bounds of the Slot.</param>
	/// <param name="inventory">the attached inventory for the slot.</param>
	/// <param name="slot">The slot of the slot.</param>
	/// <param name="drawBackground">Do we draw the background for this slot? (Default: true)</param>
	public GuiElementPassiveItemSlot(ICoreClientAPI capi, ElementBounds bounds, IInventory inventory, ItemSlot slot, bool drawBackground = true)
		: base(capi, bounds)
	{
		this.slot = slot;
		this.inventory = inventory;
		this.drawBackground = drawBackground;
		bounds.fixedWidth = unscaledSlotSize;
		bounds.fixedHeight = unscaledSlotSize;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		if (drawBackground)
		{
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.6);
			ElementRoundRectangle(ctx, Bounds);
			ctx.Fill();
			EmbossRoundRectangleElement(ctx, Bounds, inverse: true);
		}
		GuiElement.scaled(unscaledSlotSize);
		ElementBounds textBounds = ElementBounds.Fixed(0.0, unscaledSlotSize - GuiStyle.SmallFontSize - 2.0, unscaledSlotSize - 5.0, unscaledSlotSize - 5.0).WithEmptyParent();
		CairoFont font = CairoFont.WhiteSmallText();
		font.FontWeight = FontWeight.Bold;
		textComposer = new GuiElementStaticText(api, "", EnumTextOrientation.Right, textBounds, font);
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (slot.Itemstack != null)
		{
			double offset = GuiElement.scaled(unscaledSlotSize) / 2.0;
			api.Render.PushScissor(Bounds, stacking: true);
			api.Render.RenderItemstackToGui(slot, Bounds.renderX + offset, Bounds.renderY + offset, 450.0, (float)GuiElement.scaled(unscaledItemSize), -1);
			api.Render.PopScissor();
		}
	}
}
