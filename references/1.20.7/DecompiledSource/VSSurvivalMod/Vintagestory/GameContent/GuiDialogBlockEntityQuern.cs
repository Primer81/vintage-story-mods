using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GuiDialogBlockEntityQuern : GuiDialogBlockEntity
{
	private long lastRedrawMs;

	private float inputGrindTime;

	private float maxGrindTime;

	protected override double FloatyDialogPosition => 0.75;

	public GuiDialogBlockEntityQuern(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
		: base(DialogTitle, Inventory, BlockEntityPosition, capi)
	{
		if (!base.IsDuplicate)
		{
			capi.World.Player.InventoryManager.OpenInventory(Inventory);
			SetupDialog();
		}
	}

	private void OnInventorySlotModified(int slotid)
	{
		capi.Event.EnqueueMainThreadTask(SetupDialog, "setupquerndlg");
	}

	private void SetupDialog()
	{
		ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
		if (hoveredSlot != null && hoveredSlot.Inventory == base.Inventory)
		{
			capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot);
		}
		else
		{
			hoveredSlot = null;
		}
		ElementBounds quernBounds = ElementBounds.Fixed(0.0, 0.0, 200.0, 90.0);
		ElementBounds inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 30.0, 1, 1);
		ElementBounds outputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 153.0, 30.0, 1, 1);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		bgBounds.WithChildren(quernBounds);
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		ClearComposers();
		base.SingleComposer = capi.Gui.CreateCompo("blockentitymillstone" + base.BlockEntityPosition, dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddDynamicCustomDraw(quernBounds, OnBgDraw, "symbolDrawer")
			.AddItemSlotGrid(base.Inventory, SendInvPacket, 1, new int[1], inputSlotBounds, "inputSlot")
			.AddItemSlotGrid(base.Inventory, SendInvPacket, 1, new int[1] { 1 }, outputSlotBounds, "outputslot")
			.EndChildElements()
			.Compose();
		lastRedrawMs = capi.ElapsedMilliseconds;
		if (hoveredSlot != null)
		{
			base.SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
		}
	}

	public void Update(float inputGrindTime, float maxGrindTime)
	{
		this.inputGrindTime = inputGrindTime;
		this.maxGrindTime = maxGrindTime;
		if (IsOpened() && capi.ElapsedMilliseconds - lastRedrawMs > 500)
		{
			if (base.SingleComposer != null)
			{
				base.SingleComposer.GetCustomDraw("symbolDrawer").Redraw();
			}
			lastRedrawMs = capi.ElapsedMilliseconds;
		}
	}

	private void OnBgDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
	{
		double top = 30.0;
		ctx.Save();
		Matrix i = ctx.Matrix;
		i.Translate(GuiElement.scaled(63.0), GuiElement.scaled(top + 2.0));
		i.Scale(GuiElement.scaled(0.6), GuiElement.scaled(0.6));
		ctx.Matrix = i;
		capi.Gui.Icons.DrawArrowRight(ctx, 2.0);
		double dx = inputGrindTime / maxGrindTime;
		ctx.Rectangle(GuiElement.scaled(5.0), 0.0, GuiElement.scaled(125.0 * dx), GuiElement.scaled(100.0));
		ctx.Clip();
		LinearGradient gradient = new LinearGradient(0.0, 0.0, GuiElement.scaled(200.0), 0.0);
		gradient.AddColorStop(0.0, new Color(0.0, 0.4, 0.0, 1.0));
		gradient.AddColorStop(1.0, new Color(0.2, 0.6, 0.2, 1.0));
		ctx.SetSource(gradient);
		capi.Gui.Icons.DrawArrowRight(ctx, 0.0, strokeOrFill: false, defaultPattern: false);
		gradient.Dispose();
		ctx.Restore();
	}

	private void SendInvPacket(object p)
	{
		capi.Network.SendBlockEntityPacket(base.BlockEntityPosition.X, base.BlockEntityPosition.Y, base.BlockEntityPosition.Z, p);
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		base.Inventory.SlotModified += OnInventorySlotModified;
	}

	public override void OnGuiClosed()
	{
		base.Inventory.SlotModified -= OnInventorySlotModified;
		base.SingleComposer.GetSlotGrid("inputSlot").OnGuiClosed(capi);
		base.SingleComposer.GetSlotGrid("outputslot").OnGuiClosed(capi);
		base.OnGuiClosed();
	}
}
