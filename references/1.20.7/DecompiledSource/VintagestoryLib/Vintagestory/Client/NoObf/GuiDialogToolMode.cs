using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class GuiDialogToolMode : GuiDialog
{
	private List<List<SkillItem>> multilineItems;

	private BlockSelection blockSele;

	private bool keepOpen;

	private int prevSlotOver = -1;

	private readonly double floatyDialogPosition = 0.5;

	private readonly double floatyDialogAlign = 0.75;

	public override string ToggleKeyCombinationCode => "toolmodeselect";

	public override bool PrefersUngrabbedMouse => false;

	public GuiDialogToolMode(ICoreClientAPI capi)
		: base(capi)
	{
		capi.Event.RegisterEventBusListener(OnEventBusEvent, 0.5, "keepopentoolmodedlg");
	}

	private void OnEventBusEvent(string eventName, ref EnumHandling handling, IAttribute data)
	{
		keepOpen = true;
	}

	internal override bool OnKeyCombinationToggle(KeyCombination viaKeyComb)
	{
		ItemSlot slot = capi.World.Player?.InventoryManager?.ActiveHotbarSlot;
		if (slot?.Itemstack?.Collectible.GetToolModes(slot, capi.World.Player, capi.World.Player.CurrentBlockSelection) == null)
		{
			return false;
		}
		blockSele = capi.World.Player.CurrentBlockSelection?.Clone();
		return base.OnKeyCombinationToggle(viaKeyComb);
	}

	public override void OnGuiOpened()
	{
		ComposeDialog();
	}

	private void ComposeDialog()
	{
		prevSlotOver = -1;
		ClearComposers();
		ItemSlot slot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
		SkillItem[] items = slot.Itemstack.Collectible.GetToolModes(slot, capi.World.Player, blockSele);
		if (items == null)
		{
			return;
		}
		multilineItems = new List<List<SkillItem>>();
		multilineItems.Add(new List<SkillItem>());
		int cols = 1;
		for (int i = 0; i < items.Length; i++)
		{
			List<SkillItem> lineitems2 = multilineItems[multilineItems.Count - 1];
			if (items[i].Linebreak)
			{
				multilineItems.Add(lineitems2 = new List<SkillItem>());
			}
			lineitems2.Add(items[i]);
		}
		foreach (List<SkillItem> val2 in multilineItems)
		{
			cols = Math.Max(cols, val2.Count);
		}
		double size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
		double innerWidth = (double)cols * size;
		int rows = multilineItems.Count;
		SkillItem[] array = items;
		foreach (SkillItem val in array)
		{
			innerWidth = Math.Max(innerWidth, CairoFont.WhiteSmallishText().GetTextExtents(val.Name).Width / (double)RuntimeEnv.GUIScale + 1.0);
		}
		ElementBounds skillGridBounds = ElementBounds.Fixed(0.0, 0.0, innerWidth, (double)rows * size);
		ElementBounds textBounds = ElementBounds.Fixed(0.0, (double)rows * (size + 2.0) + 5.0, innerWidth, 25.0);
		base.SingleComposer = capi.Gui.CreateCompo("toolmodeselect", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding / 2.0), withTitleBar: false).BeginChildElements();
		int idx = 0;
		for (int j = 0; j < multilineItems.Count; j++)
		{
			int line = j;
			int baseIndex = idx;
			List<SkillItem> lineitems = multilineItems[line];
			base.SingleComposer.AddSkillItemGrid(lineitems, lineitems.Count, 1, delegate(int num)
			{
				OnSlotClick(baseIndex + num);
			}, skillGridBounds, "skillitemgrid-" + line);
			base.SingleComposer.GetSkillItemGrid("skillitemgrid-" + line).OnSlotOver = delegate(int num)
			{
				OnSlotOver(line, num);
			};
			skillGridBounds = skillGridBounds.BelowCopy(0.0, 5.0);
			idx += lineitems.Count;
		}
		base.SingleComposer.AddDynamicText("", CairoFont.WhiteSmallishText(), textBounds, "name").EndChildElements().Compose();
	}

	private void OnSlotOver(int line, int num)
	{
		List<SkillItem> skillItems = multilineItems[line];
		if (num < skillItems.Count)
		{
			prevSlotOver = num;
			base.SingleComposer.GetDynamicText("name").SetNewText(skillItems[num].Name);
		}
	}

	private void OnSlotClick(int num)
	{
		ItemSlot slot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
		CollectibleObject obj = slot?.Itemstack?.Collectible;
		if (obj != null)
		{
			obj.SetToolMode(slot, capi.World.Player, blockSele, num);
			Packet_ToolMode pt = new Packet_ToolMode
			{
				Mode = num
			};
			if (blockSele != null)
			{
				pt.X = blockSele.Position.X;
				pt.Y = blockSele.Position.Y;
				pt.Z = blockSele.Position.Z;
				pt.SelectionBoxIndex = blockSele.SelectionBoxIndex;
				pt.Face = blockSele.Face.Index;
				pt.HitX = CollectibleNet.SerializeDouble(blockSele.HitPosition.X);
				pt.HitY = CollectibleNet.SerializeDouble(blockSele.HitPosition.Y);
				pt.HitZ = CollectibleNet.SerializeDouble(blockSele.HitPosition.Z);
			}
			capi.Network.SendPacketClient(new Packet_Client
			{
				Id = 27,
				ToolMode = pt
			});
			slot.MarkDirty();
		}
		if (keepOpen)
		{
			keepOpen = false;
			ComposeDialog();
		}
		else
		{
			TryClose();
		}
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (capi.Settings.Bool["immersiveMouseMode"] && blockSele?.Position != null)
		{
			Vec3d pos = MatrixToolsd.Project(
				new Vec3d(
					(double)blockSele.Position.X + 0.5,
					(double)blockSele.Position.Y + floatyDialogPosition,
					(double)blockSele.Position.Z + 0.5),
					capi.Render.PerspectiveProjectionMat,
					capi.Render.PerspectiveViewMat,
					capi.Render.FrameWidth,
					capi.Render.FrameHeight);
			if (pos.Z < 0.0)
			{
				return;
			}
			base.SingleComposer.Bounds.Alignment = EnumDialogArea.None;
			base.SingleComposer.Bounds.fixedOffsetX = 0.0;
			base.SingleComposer.Bounds.fixedOffsetY = 0.0;
			base.SingleComposer.Bounds.absFixedX = pos.X - base.SingleComposer.Bounds.OuterWidth / 2.0;
			base.SingleComposer.Bounds.absFixedY = (double)capi.Render.FrameHeight - pos.Y - base.SingleComposer.Bounds.OuterHeight * floatyDialogAlign;
			base.SingleComposer.Bounds.absMarginX = 0.0;
			base.SingleComposer.Bounds.absMarginY = 0.0;
		}
		base.OnRenderGUI(deltaTime);
	}
}
