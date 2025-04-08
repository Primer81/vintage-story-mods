using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GuiDialogBlockEntityFirepit : GuiDialogBlockEntity
{
	private bool haveCookingContainer;

	private string currentOutputText;

	private ElementBounds cookingSlotsSlotBounds;

	private long lastRedrawMs;

	private EnumPosFlag screenPos;

	protected override double FloatyDialogPosition => 0.6;

	protected override double FloatyDialogAlign => 0.8;

	public override double DrawOrder => 0.2;

	public GuiDialogBlockEntityFirepit(string dlgTitle, InventoryBase Inventory, BlockPos bePos, SyncedTreeAttribute tree, ICoreClientAPI capi)
		: base(dlgTitle, Inventory, bePos, capi)
	{
		if (!base.IsDuplicate)
		{
			tree.OnModified.Add(new TreeModifiedListener
			{
				listener = OnAttributesModified
			});
			Attributes = tree;
		}
	}

	private void OnInventorySlotModified(int slotid)
	{
		capi.Event.EnqueueMainThreadTask(SetupDialog, "setupfirepitdlg");
	}

	private void SetupDialog()
	{
		ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
		if (hoveredSlot != null && hoveredSlot.Inventory?.InventoryID != base.Inventory?.InventoryID)
		{
			hoveredSlot = null;
		}
		string newOutputText = Attributes.GetString("outputText", "");
		bool newHaveCookingContainer = Attributes.GetInt("haveCookingContainer") > 0;
		GuiElementDynamicText outputTextElem;
		if (haveCookingContainer == newHaveCookingContainer && base.SingleComposer != null)
		{
			outputTextElem = base.SingleComposer.GetDynamicText("outputText");
			outputTextElem.Font.WithFontSize(14f);
			outputTextElem.SetNewText(newOutputText, autoHeight: true);
			base.SingleComposer.GetCustomDraw("symbolDrawer").Redraw();
			haveCookingContainer = newHaveCookingContainer;
			currentOutputText = newOutputText;
			outputTextElem.Bounds.fixedOffsetY = 0.0;
			if (outputTextElem.QuantityTextLines > 2)
			{
				outputTextElem.Bounds.fixedOffsetY = (0.0 - outputTextElem.Font.GetFontExtents().Height) / (double)RuntimeEnv.GUIScale * 0.65;
				outputTextElem.Font.WithFontSize(12f);
				outputTextElem.RecomposeText();
			}
			outputTextElem.Bounds.CalcWorldBounds();
			return;
		}
		haveCookingContainer = newHaveCookingContainer;
		currentOutputText = newOutputText;
		int qCookingSlots = Attributes.GetInt("quantityCookingSlots");
		ElementBounds stoveBounds = ElementBounds.Fixed(0.0, 0.0, 210.0, 250.0);
		cookingSlotsSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 75.0, 4, qCookingSlots / 4);
		cookingSlotsSlotBounds.fixedHeight += 10.0;
		double top = cookingSlotsSlotBounds.fixedHeight + cookingSlotsSlotBounds.fixedY;
		ElementBounds inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, top, 1, 1);
		ElementBounds fuelSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 110.0 + top, 1, 1);
		ElementBounds outputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 153.0, top, 1, 1);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		bgBounds.WithChildren(stoveBounds);
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithFixedAlignmentOffset(IsRight(screenPos) ? (0.0 - GuiStyle.DialogToScreenPadding) : GuiStyle.DialogToScreenPadding, 0.0).WithAlignment(IsRight(screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);
		if (!capi.Settings.Bool["immersiveMouseMode"])
		{
			dialogBounds.fixedOffsetY += (stoveBounds.fixedHeight + 65.0 + (double)(haveCookingContainer ? 25 : 0)) * (double)YOffsetMul(screenPos);
			dialogBounds.fixedOffsetX += (stoveBounds.fixedWidth + 10.0) * (double)XOffsetMul(screenPos);
		}
		int[] cookingSlotIds = new int[qCookingSlots];
		for (int i = 0; i < qCookingSlots; i++)
		{
			cookingSlotIds[i] = 3 + i;
		}
		base.SingleComposer = capi.Gui.CreateCompo("blockentitystove" + base.BlockEntityPosition, dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddDynamicCustomDraw(stoveBounds, OnBgDraw, "symbolDrawer")
			.AddDynamicText("", CairoFont.WhiteDetailText(), ElementBounds.Fixed(0.0, 30.0, 210.0, 45.0), "outputText")
			.AddIf(haveCookingContainer)
			.AddItemSlotGrid(base.Inventory, SendInvPacket, 4, cookingSlotIds, cookingSlotsSlotBounds, "ingredientSlots")
			.EndIf()
			.AddItemSlotGrid(base.Inventory, SendInvPacket, 1, new int[1], fuelSlotBounds, "fuelslot")
			.AddDynamicText("", CairoFont.WhiteDetailText(), fuelSlotBounds.RightCopy(17.0, 16.0).WithFixedSize(60.0, 30.0), "fueltemp")
			.AddItemSlotGrid(base.Inventory, SendInvPacket, 1, new int[1] { 1 }, inputSlotBounds, "oreslot")
			.AddDynamicText("", CairoFont.WhiteDetailText(), inputSlotBounds.RightCopy(23.0, 16.0).WithFixedSize(60.0, 30.0), "oretemp")
			.AddItemSlotGrid(base.Inventory, SendInvPacket, 1, new int[1] { 2 }, outputSlotBounds, "outputslot")
			.EndChildElements()
			.Compose();
		lastRedrawMs = capi.ElapsedMilliseconds;
		if (hoveredSlot != null)
		{
			base.SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
		}
		outputTextElem = base.SingleComposer.GetDynamicText("outputText");
		outputTextElem.SetNewText(currentOutputText, autoHeight: true);
		outputTextElem.Bounds.fixedOffsetY = 0.0;
		if (outputTextElem.QuantityTextLines > 2)
		{
			outputTextElem.Bounds.fixedOffsetY = (0.0 - outputTextElem.Font.GetFontExtents().Height) / (double)RuntimeEnv.GUIScale * 0.65;
			outputTextElem.Font.WithFontSize(12f);
			outputTextElem.RecomposeText();
		}
		outputTextElem.Bounds.CalcWorldBounds();
	}

	private void OnAttributesModified()
	{
		if (!IsOpened())
		{
			return;
		}
		float ftemp = Attributes.GetFloat("furnaceTemperature");
		float otemp = Attributes.GetFloat("oreTemperature");
		string fuelTemp = ftemp.ToString("#");
		string oreTemp = otemp.ToString("#");
		fuelTemp += ((fuelTemp.Length > 0) ? "°C" : "");
		oreTemp += ((oreTemp.Length > 0) ? "°C" : "");
		if (ftemp > 0f && ftemp <= 20f)
		{
			fuelTemp = Lang.Get("Cold");
		}
		if (otemp > 0f && otemp <= 20f)
		{
			oreTemp = Lang.Get("Cold");
		}
		base.SingleComposer.GetDynamicText("fueltemp").SetNewText(fuelTemp);
		base.SingleComposer.GetDynamicText("oretemp").SetNewText(oreTemp);
		if (capi.ElapsedMilliseconds - lastRedrawMs > 500)
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
		double top = cookingSlotsSlotBounds.fixedHeight + cookingSlotsSlotBounds.fixedY;
		ctx.Save();
		Matrix i = ctx.Matrix;
		i.Translate(GuiElement.scaled(5.0), GuiElement.scaled(53.0 + top));
		i.Scale(GuiElement.scaled(0.25), GuiElement.scaled(0.25));
		ctx.Matrix = i;
		capi.Gui.Icons.DrawFlame(ctx);
		double dy = 210f - 210f * (Attributes.GetFloat("fuelBurnTime") / Attributes.GetFloat("maxFuelBurnTime", 1f));
		ctx.Rectangle(0.0, dy, 200.0, 210.0 - dy);
		ctx.Clip();
		LinearGradient gradient = new LinearGradient(0.0, GuiElement.scaled(250.0), 0.0, 0.0);
		gradient.AddColorStop(0.0, new Color(1.0, 1.0, 0.0, 1.0));
		gradient.AddColorStop(1.0, new Color(1.0, 0.0, 0.0, 1.0));
		ctx.SetSource(gradient);
		capi.Gui.Icons.DrawFlame(ctx, 0.0, strokeOrFill: false, defaultPattern: false);
		gradient.Dispose();
		ctx.Restore();
		ctx.Save();
		i = ctx.Matrix;
		i.Translate(GuiElement.scaled(63.0), GuiElement.scaled(top + 2.0));
		i.Scale(GuiElement.scaled(0.6), GuiElement.scaled(0.6));
		ctx.Matrix = i;
		capi.Gui.Icons.DrawArrowRight(ctx, 2.0);
		double cookingRel = Attributes.GetFloat("oreCookingTime") / Attributes.GetFloat("maxOreCookingTime", 1f);
		ctx.Rectangle(5.0, 0.0, 125.0 * cookingRel, 100.0);
		ctx.Clip();
		gradient = new LinearGradient(0.0, 0.0, 200.0, 0.0);
		gradient.AddColorStop(0.0, new Color(0.0, 0.4, 0.0, 1.0));
		gradient.AddColorStop(1.0, new Color(0.2, 0.6, 0.2, 1.0));
		ctx.SetSource(gradient);
		capi.Gui.Icons.DrawArrowRight(ctx, 0.0, strokeOrFill: false, defaultPattern: false);
		gradient.Dispose();
		ctx.Restore();
	}

	private void SendInvPacket(object packet)
	{
		capi.Network.SendBlockEntityPacket(base.BlockEntityPosition.X, base.BlockEntityPosition.Y, base.BlockEntityPosition.Z, packet);
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		base.Inventory.SlotModified += OnInventorySlotModified;
		screenPos = GetFreePos("smallblockgui");
		OccupyPos("smallblockgui", screenPos);
		SetupDialog();
	}

	public override void OnGuiClosed()
	{
		base.Inventory.SlotModified -= OnInventorySlotModified;
		base.SingleComposer.GetSlotGrid("fuelslot").OnGuiClosed(capi);
		base.SingleComposer.GetSlotGrid("oreslot").OnGuiClosed(capi);
		base.SingleComposer.GetSlotGrid("outputslot").OnGuiClosed(capi);
		base.SingleComposer.GetSlotGrid("ingredientSlots")?.OnGuiClosed(capi);
		base.OnGuiClosed();
		FreePos("smallblockgui", screenPos);
	}
}
