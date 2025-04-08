using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GuiDialogCreatureContents : GuiDialog
{
	private InventoryGeneric inv;

	private Entity owningEntity;

	public int packetIdOffset;

	private EnumPosFlag screenPos;

	private string title;

	private ICustomDialogPositioning icdp;

	private Vec3d entityPos = new Vec3d();

	public override string ToggleKeyCombinationCode => null;

	protected double FloatyDialogPosition => 0.6;

	protected double FloatyDialogAlign => 0.8;

	public override bool UnregisterOnClose => true;

	public override bool PrefersUngrabbedMouse => false;

	public GuiDialogCreatureContents(InventoryGeneric inv, Entity owningEntity, ICoreClientAPI capi, string code, string title = null, ICustomDialogPositioning icdp = null)
		: base(capi)
	{
		this.inv = inv;
		this.title = title;
		this.owningEntity = owningEntity;
		this.icdp = icdp;
		Compose(code);
	}

	public void Compose(string code)
	{
		double pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
		int rows = (int)Math.Ceiling((float)inv.Count / 4f);
		ElementBounds slotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, 40.0 + pad, 4, rows).FixedGrow(2.0 * pad, 2.0 * pad);
		screenPos = GetFreePos("smallblockgui");
		float elemToDlgPad = 10f;
		ElementBounds dialogBounds = slotBounds.ForkBoundingParent(elemToDlgPad, elemToDlgPad + 30f, elemToDlgPad, elemToDlgPad).WithFixedAlignmentOffset(IsRight(screenPos) ? (0.0 - GuiStyle.DialogToScreenPadding) : GuiStyle.DialogToScreenPadding, 0.0).WithAlignment(IsRight(screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);
		if (!capi.Settings.Bool["immersiveMouseMode"])
		{
			dialogBounds.fixedOffsetY += (dialogBounds.fixedHeight + 10.0) * (double)YOffsetMul(screenPos);
			dialogBounds.fixedOffsetX += (dialogBounds.fixedWidth + 10.0) * (double)XOffsetMul(screenPos);
		}
		base.SingleComposer = capi.Gui.CreateCompo(code + owningEntity.EntityId, dialogBounds).AddShadedDialogBG(ElementBounds.Fill).AddDialogTitleBar(Lang.Get(title ?? code), OnTitleBarClose)
			.AddItemSlotGrid(inv, DoSendPacket, 4, slotBounds, "slots")
			.Compose();
	}

	private void DoSendPacket(object p)
	{
		capi.Network.SendEntityPacketWithOffset(owningEntity.EntityId, packetIdOffset, p);
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		if (capi.Gui.GetDialogPosition(base.SingleComposer.DialogName) == null)
		{
			OccupyPos("smallblockgui", screenPos);
		}
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		capi.Network.SendPacketClient(capi.World.Player.InventoryManager.CloseInventory(inv));
		base.SingleComposer.GetSlotGrid("slots").OnGuiClosed(capi);
		FreePos("smallblockgui", screenPos);
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (capi.Settings.Bool["immersiveMouseMode"])
		{
			double offX = owningEntity.SelectionBox.X2 - owningEntity.OriginSelectionBox.X2;
			double offZ = owningEntity.SelectionBox.Z2 - owningEntity.OriginSelectionBox.Z2;
			Vec3d aboveHeadPos = new Vec3d(owningEntity.Pos.X + offX, owningEntity.Pos.Y + FloatyDialogPosition, owningEntity.Pos.Z + offZ);
			if (icdp != null)
			{
				aboveHeadPos = icdp.GetDialogPosition();
			}
			Vec3d pos = MatrixToolsd.Project(aboveHeadPos, capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);
			if (pos.Z < 0.0)
			{
				return;
			}
			base.SingleComposer.Bounds.Alignment = EnumDialogArea.None;
			base.SingleComposer.Bounds.fixedOffsetX = 0.0;
			base.SingleComposer.Bounds.fixedOffsetY = 0.0;
			base.SingleComposer.Bounds.absFixedX = pos.X - base.SingleComposer.Bounds.OuterWidth / 2.0;
			base.SingleComposer.Bounds.absFixedY = (double)capi.Render.FrameHeight - pos.Y - base.SingleComposer.Bounds.OuterHeight * FloatyDialogAlign;
			base.SingleComposer.Bounds.absMarginX = 0.0;
			base.SingleComposer.Bounds.absMarginY = 0.0;
		}
		base.OnRenderGUI(deltaTime);
	}

	public override void OnFinalizeFrame(float dt)
	{
		base.OnFinalizeFrame(dt);
		entityPos.Set(owningEntity.Pos.X, owningEntity.Pos.Y, owningEntity.Pos.Z);
		entityPos.Add(owningEntity.SelectionBox.X2 - owningEntity.OriginSelectionBox.X2, 0.0, owningEntity.SelectionBox.Z2 - owningEntity.OriginSelectionBox.Z2);
		if (!IsInRangeOfBlock())
		{
			capi.Event.EnqueueMainThreadTask(delegate
			{
				TryClose();
			}, "closedlg");
		}
	}

	public override bool TryClose()
	{
		return base.TryClose();
	}

	public virtual bool IsInRangeOfBlock()
	{
		return (double)GameMath.Sqrt(capi.World.Player.Entity.Pos.XYZ.Add(capi.World.Player.Entity.LocalEyePos).SquareDistanceTo(entityPos)) <= (double)capi.World.Player.WorldData.PickingRange;
	}
}
