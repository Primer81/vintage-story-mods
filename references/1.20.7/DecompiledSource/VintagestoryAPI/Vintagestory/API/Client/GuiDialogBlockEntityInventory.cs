using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

/// <summary>
/// A block entity inventory system for things like a campfire, or other things like that.
/// </summary>
public class GuiDialogBlockEntityInventory : GuiDialogBlockEntity
{
	private int cols;

	private EnumPosFlag screenPos;

	public override double DrawOrder => 0.2;

	public GuiDialogBlockEntityInventory(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, int cols, ICoreClientAPI capi)
		: base(dialogTitle, inventory, blockEntityPos, capi)
	{
		if (base.IsDuplicate)
		{
			return;
		}
		this.cols = cols;
		double elemToDlgPad = GuiStyle.ElementToDialogPadding;
		double pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
		int rows = (int)Math.Ceiling((float)inventory.Count / (float)cols);
		int visibleRows = Math.Min(rows, 7);
		ElementBounds slotGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, pad, cols, visibleRows);
		ElementBounds fullGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, cols, rows);
		ElementBounds insetBounds = slotGridBounds.ForkBoundingParent(6.0, 6.0, 6.0, 6.0);
		screenPos = GetFreePos("smallblockgui");
		if (visibleRows < rows)
		{
			ElementBounds clippingBounds = slotGridBounds.CopyOffsetedSibling();
			clippingBounds.fixedHeight -= 3.0;
			ElementBounds dialogBounds = insetBounds.ForkBoundingParent(elemToDlgPad, elemToDlgPad + 30.0, elemToDlgPad + 20.0, elemToDlgPad).WithFixedAlignmentOffset(IsRight(screenPos) ? (0.0 - GuiStyle.DialogToScreenPadding) : GuiStyle.DialogToScreenPadding, 0.0).WithAlignment(IsRight(screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);
			if (!capi.Settings.Bool["immersiveMouseMode"])
			{
				dialogBounds.fixedOffsetY += (dialogBounds.fixedHeight + 10.0) * (double)YOffsetMul(screenPos);
				dialogBounds.fixedOffsetX += (dialogBounds.fixedWidth + 10.0) * (double)XOffsetMul(screenPos);
			}
			ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(insetBounds).WithParent(dialogBounds);
			base.SingleComposer = capi.Gui.CreateCompo("blockentityinventory" + blockEntityPos, dialogBounds).AddShadedDialogBG(ElementBounds.Fill).AddDialogTitleBar(dialogTitle, base.CloseIconPressed)
				.AddInset(insetBounds)
				.AddVerticalScrollbar(base.OnNewScrollbarvalue, scrollbarBounds, "scrollbar")
				.BeginClip(clippingBounds)
				.AddItemSlotGrid(inventory, base.DoSendPacket, cols, fullGridBounds, "slotgrid")
				.EndClip()
				.Compose();
			base.SingleComposer.GetScrollbar("scrollbar").SetHeights((float)slotGridBounds.fixedHeight, (float)(fullGridBounds.fixedHeight + pad));
		}
		else
		{
			ElementBounds dialogBounds2 = insetBounds.ForkBoundingParent(elemToDlgPad, elemToDlgPad + 20.0, elemToDlgPad, elemToDlgPad).WithFixedAlignmentOffset(IsRight(screenPos) ? (0.0 - GuiStyle.DialogToScreenPadding) : GuiStyle.DialogToScreenPadding, 0.0).WithAlignment(IsRight(screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);
			if (!capi.Settings.Bool["immersiveMouseMode"])
			{
				dialogBounds2.fixedOffsetY += (dialogBounds2.fixedHeight + 10.0) * (double)YOffsetMul(screenPos);
				dialogBounds2.fixedOffsetX += (dialogBounds2.fixedWidth + 10.0) * (double)XOffsetMul(screenPos);
			}
			base.SingleComposer = capi.Gui.CreateCompo("blockentityinventory" + blockEntityPos, dialogBounds2).AddShadedDialogBG(ElementBounds.Fill).AddDialogTitleBar(dialogTitle, base.CloseIconPressed)
				.AddInset(insetBounds)
				.AddItemSlotGrid(inventory, base.DoSendPacket, cols, slotGridBounds, "slotgrid")
				.Compose();
		}
		base.SingleComposer.UnfocusOwnElements();
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		FreePos("smallblockgui", screenPos);
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		if (capi.Gui.GetDialogPosition(base.SingleComposer.DialogName) == null)
		{
			OccupyPos("smallblockgui", screenPos);
		}
	}
}
