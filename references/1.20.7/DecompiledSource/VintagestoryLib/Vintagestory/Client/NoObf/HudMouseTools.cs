using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf;

internal class HudMouseTools : GuiDialog
{
	private static int tooltipOffsetX = 10;

	private static int tooltipOffsetY = 30;

	private ElementBounds slotBounds = ElementBounds.Empty;

	private IInventory mouseCursorInv;

	private GuiElementItemstackInfo[] itemstackinfoElements;

	private int currentBackElemIndex;

	private bool bottomOverlap;

	private bool rightOverlap;

	private bool recalcAlignmentOffset;

	private bool dirty;

	private bool lshiftdown;

	private ItemSlot currentSlot;

	private int currentFrontElemIndex => 1 - currentBackElemIndex;

	public override double DrawOrder => 0.9;

	public override string ToggleKeyCombinationCode => null;

	public override bool Focusable => false;

	public override EnumDialogType DialogType => EnumDialogType.HUD;

	public HudMouseTools(ICoreClientAPI capi)
		: base(capi)
	{
		capi.Event.RegisterGameTickListener(RecheckItemInfo, 500);
	}

	public override bool TryClose()
	{
		return false;
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (dirty && !itemstackinfoElements[currentBackElemIndex].Dirty)
		{
			dirty = false;
			currentBackElemIndex = 1 - currentBackElemIndex;
			itemstackinfoElements[currentFrontElemIndex].Render = true;
			itemstackinfoElements[currentBackElemIndex].Render = false;
			recalcAlignmentOffset = true;
		}
		if (capi.Input.InWorldMouseButton.Right)
		{
			dirty = true;
			itemstackinfoElements[currentBackElemIndex].SetSourceSlot(null);
			currentSlot = null;
		}
		slotBounds.fixedX = 5f + (float)capi.Input.MouseX / ClientSettings.GUIScale;
		slotBounds.fixedY = 5f + (float)capi.Input.MouseY / ClientSettings.GUIScale;
		slotBounds.CalcWorldBounds();
		ElementBounds curStackInfoBounds = itemstackinfoElements[currentFrontElemIndex].Bounds;
		double num = (double)capi.Input.MouseX + curStackInfoBounds.OuterWidth + (double)tooltipOffsetX - (double)(capi.Render.FrameWidth - 5);
		double bottomOverlapPixels = (double)capi.Input.MouseY + curStackInfoBounds.OuterHeight + (double)tooltipOffsetY - (double)(capi.Render.FrameHeight - 5);
		bool newRightOverlap = num > 0.0;
		bool newBottomOverlap = bottomOverlapPixels > 0.0;
		if (currentSlot != null && (recalcAlignmentOffset || newBottomOverlap || rightOverlap))
		{
			curStackInfoBounds.WithFixedAlignmentOffset(
				newRightOverlap
				? ((0.0 - (curStackInfoBounds.OuterWidth + (double)(3 * tooltipOffsetX))) / (double)ClientSettings.GUIScale)
				: 0.0, newBottomOverlap
					? ((0.0 - bottomOverlapPixels) / (double)ClientSettings.GUIScale - 10.0)
					: 0.0
			);
			curStackInfoBounds.CalcWorldBounds();
			bottomOverlap = newBottomOverlap;
			rightOverlap = newRightOverlap;
		}
		recalcAlignmentOffset = false;
		capi.Render.GlPushMatrix();
		capi.Render.GlTranslate(0f, 0f, 160f);
		base.OnRenderGUI(deltaTime);
		capi.Render.GlPopMatrix();
		bool nowlshiftdown = capi.Input.KeyboardKeyStateRaw[1];
		if (nowlshiftdown != lshiftdown && ClientSettings.ExtendedDebugInfo)
		{
			itemstackinfoElements[currentBackElemIndex].SetSourceSlot(null);
			if (itemstackinfoElements[currentBackElemIndex].SetSourceSlot(currentSlot))
			{
				dirty = true;
			}
			lshiftdown = nowlshiftdown;
		}
	}

	public override void OnOwnPlayerDataReceived()
	{
		TryOpen();
		mouseCursorInv = capi.World.Player.InventoryManager.GetOwnInventory("mouse");
		double off = (0.0 - GuiElementPassiveItemSlot.unscaledSlotSize) * 0.25;
		slotBounds = ElementStdBounds.Slot().WithFixedAlignmentOffset(off, off);
		Composers["mouseSlot"] = capi.Gui.CreateCompo("mouseSlot", ElementBounds.Fill).AddPassiveItemSlot(slotBounds, mouseCursorInv, mouseCursorInv[0], drawBackground: false).Compose();
		ElementBounds stackInfoBounds = ElementBounds.FixedSize(EnumDialogArea.None, GuiElementItemstackInfo.BoxWidth, 0.0).WithFixedPadding(10.0).WithFixedPosition(25.0, 35.0);
		stackInfoBounds.WithParent(slotBounds);
		Composers["itemstackinfo"] = capi.Gui.CreateCompo("itemstackinfo", ElementBounds.Fill).AddInteractiveElement(new GuiElementItemstackInfo(capi, stackInfoBounds, OnRequireInfoText), "itemstackinfo1").AddInteractiveElement(new GuiElementItemstackInfo(capi, stackInfoBounds.FlatCopy(), OnRequireInfoText), "itemstackinfo2")
			.Compose();
		itemstackinfoElements = new GuiElementItemstackInfo[2]
		{
			(GuiElementItemstackInfo)Composers["itemstackinfo"].GetElement("itemstackinfo1"),
			(GuiElementItemstackInfo)Composers["itemstackinfo"].GetElement("itemstackinfo2")
		};
		itemstackinfoElements[0].Render = false;
		itemstackinfoElements[1].Render = false;
	}

	private string OnRequireInfoText(ItemSlot slot)
	{
		return slot.GetStackDescription(capi.World, ClientSettings.ExtendedDebugInfo);
	}

	public override bool IsOpened()
	{
		if (!capi.Input.MouseGrabbed)
		{
			return capi.World.Player.InventoryManager.OpenedInventories.Count > 0;
		}
		return false;
	}

	public override bool IsOpened(string dialogComposerName)
	{
		if (dialogComposerName == "itemstackinfo")
		{
			return itemstackinfoElements[currentFrontElemIndex].GetSlot()?.Itemstack != null;
		}
		return base.IsOpened(dialogComposerName);
	}

	public override bool ShouldReceiveRenderEvents()
	{
		return true;
	}

	public override void OnMouseDown(MouseEvent args)
	{
	}

	public override void OnMouseUp(MouseEvent args)
	{
	}

	public override void OnMouseMove(MouseEvent args)
	{
	}

	private void RecheckItemInfo(float dt)
	{
		if (itemstackinfoElements[currentBackElemIndex].SetSourceSlot(currentSlot))
		{
			dirty = true;
		}
	}

	public override bool OnMouseEnterSlot(ItemSlot slot)
	{
		if (capi.Input.InWorldMouseButton.Right)
		{
			return false;
		}
		dirty = true;
		itemstackinfoElements[currentBackElemIndex].SetSourceSlot(slot);
		currentSlot = slot;
		recalcAlignmentOffset = true;
		return false;
	}

	public override bool OnMouseLeaveSlot(ItemSlot slot)
	{
		itemstackinfoElements[currentBackElemIndex].SetSourceSlot(null);
		itemstackinfoElements[currentFrontElemIndex].SetSourceSlot(null);
		currentSlot = null;
		return false;
	}

	public override bool OnMouseClickSlot(ItemSlot itemSlot)
	{
		dirty = true;
		itemstackinfoElements[currentBackElemIndex].SetSourceSlot(itemSlot);
		return false;
	}
}
