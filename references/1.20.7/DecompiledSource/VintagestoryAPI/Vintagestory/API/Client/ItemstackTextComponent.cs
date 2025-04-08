using System;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

/// <summary>
/// Draws an itemstack 
/// </summary>
public class ItemstackTextComponent : ItemstackComponentBase
{
	private DummySlot slot;

	private double size;

	public bool ShowStacksize;

	private Action<ItemStack> onStackClicked;

	public ItemstackTextComponent(ICoreClientAPI capi, ItemStack itemstack, double size, double rightSidePadding = 0.0, EnumFloat floatType = EnumFloat.Left, Action<ItemStack> onStackClicked = null)
		: base(capi)
	{
		size = GuiElement.scaled(size);
		slot = new DummySlot(itemstack);
		this.onStackClicked = onStackClicked;
		Float = floatType;
		this.size = size;
		BoundsPerLine = new LineRectangled[1]
		{
			new LineRectangled(0.0, 0.0, size, size)
		};
		PaddingRight = GuiElement.scaled(rightSidePadding);
	}

	public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
	{
		TextFlowPath curfp = GetCurrentFlowPathSection(flowPath, lineY);
		offsetX += GuiElement.scaled(PaddingLeft);
		bool requireLinebreak = offsetX + BoundsPerLine[0].Width > curfp.X2;
		BoundsPerLine[0].X = (requireLinebreak ? 0.0 : offsetX);
		BoundsPerLine[0].Y = lineY + (requireLinebreak ? currentLineHeight : 0.0);
		if (Float == EnumFloat.Right)
		{
			BoundsPerLine[0].X = curfp.X2 - size;
		}
		BoundsPerLine[0].Width = size + GuiElement.scaled(PaddingRight);
		nextOffsetX = (requireLinebreak ? 0.0 : offsetX) + BoundsPerLine[0].Width;
		if (!requireLinebreak)
		{
			return EnumCalcBoundsResult.Continue;
		}
		return EnumCalcBoundsResult.Nextline;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
	}

	public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
	{
		LineRectangled bounds = BoundsPerLine[0];
		double padLeft = GuiElement.scaled(PaddingLeft);
		double padRight = GuiElement.scaled(PaddingRight);
		double width = bounds.Width - padLeft - padRight;
		ElementBounds scibounds = ElementBounds.FixedSize((int)(bounds.Width / (double)RuntimeEnv.GUIScale), (int)(bounds.Height / (double)RuntimeEnv.GUIScale));
		scibounds.ParentBounds = capi.Gui.WindowBounds;
		scibounds.CalcWorldBounds();
		scibounds.absFixedX = renderX + bounds.X;
		scibounds.absFixedY = renderY + bounds.Y + offY;
		api.Render.PushScissor(scibounds, stacking: true);
		api.Render.RenderItemstackToGui(slot, renderX + bounds.X + padLeft + width * 0.5 + offX, renderY + bounds.Y + bounds.Height * 0.5 + offY, GuiElement.scaled(100.0), (float)size * 0.58f, -1, shading: true, rotate: false, ShowStacksize);
		api.Render.PopScissor();
		int relx = (int)((double)api.Input.MouseX - renderX);
		int rely = (int)((double)api.Input.MouseY - renderY);
		if (bounds.PointInside(relx, rely))
		{
			RenderItemstackTooltip(slot, renderX + (double)relx + offX, renderY + (double)rely + offY, deltaTime);
		}
	}

	public override void OnMouseDown(MouseEvent args)
	{
		LineRectangled[] boundsPerLine = BoundsPerLine;
		for (int i = 0; i < boundsPerLine.Length; i++)
		{
			if (boundsPerLine[i].PointInside(args.X, args.Y))
			{
				onStackClicked?.Invoke(slot.Itemstack);
			}
		}
	}
}
