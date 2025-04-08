using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class GuiDialogItemLootRandomizer : GuiDialogGeneric
{
	private InventoryBase inv;

	private bool save;

	private bool updating;

	public override string ToggleKeyCombinationCode => null;

	public override ITreeAttribute Attributes
	{
		get
		{
			TreeAttribute tree = new TreeAttribute();
			tree.SetInt("save", save ? 1 : 0);
			int num = 0;
			for (int i = 0; i < 10; i++)
			{
				ItemStack stack = inv[i].Itemstack;
				if (stack != null)
				{
					GuiElementNumberInput inp = base.SingleComposer.GetNumberInput("chance" + (i + 1));
					TreeAttribute subtree = new TreeAttribute();
					subtree.SetItemstack("stack", stack.Clone());
					subtree.SetFloat("chance", inp.GetValue());
					tree["stack" + num++] = subtree;
				}
			}
			return tree;
		}
	}

	public GuiDialogItemLootRandomizer(InventoryBase inv, float[] chances, ICoreClientAPI capi, string title = "Item Loot Randomizer")
		: base(title, capi)
	{
		this.inv = inv;
		createDialog(chances, title);
	}

	public GuiDialogItemLootRandomizer(ItemStack[] stacks, float[] chances, ICoreClientAPI capi, string title = "Item Loot Randomizer")
		: base(title, capi)
	{
		inv = new InventoryGeneric(10, "lootrandomizer-1", capi);
		for (int i = 0; i < 10; i++)
		{
			inv[i].Itemstack = stacks[i];
		}
		createDialog(chances, title);
	}

	private void createDialog(float[] chances, string title)
	{
		double pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
		ElementBounds slotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, 45.0 + pad, 10, 1).FixedGrow(2.0 * pad, 2.0 * pad);
		ElementBounds chanceInputBounds = ElementBounds.Fixed(3.0, 0.0, 48.0, 30.0).FixedUnder(slotBounds, -4.0);
		ElementBounds leftButton = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(10.0, 1.0);
		ElementBounds chanceTextBounds = ElementBounds.Fixed(EnumDialogArea.CenterFixed, 0.0, 0.0, 150.0, 30.0).WithFixedPadding(10.0, 1.0);
		ElementBounds rightButton = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(10.0, 1.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);
		float totalChance = chances.Sum();
		string text = "Total chance: " + (int)totalChance + "%";
		base.SingleComposer = capi.Gui.CreateCompo("itemlootrandomizer", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(title, OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddItemSlotGrid(inv, SendInvPacket, 10, slotBounds, "slots")
			.AddNumberInput(chanceInputBounds = chanceInputBounds.FlatCopy(), delegate
			{
				OnTextChanced(0);
			}, CairoFont.WhiteDetailText(), "chance1")
			.AddNumberInput(chanceInputBounds = chanceInputBounds.RightCopy(3.0), delegate
			{
				OnTextChanced(1);
			}, CairoFont.WhiteDetailText(), "chance2")
			.AddNumberInput(chanceInputBounds = chanceInputBounds.RightCopy(3.0), delegate
			{
				OnTextChanced(2);
			}, CairoFont.WhiteDetailText(), "chance3")
			.AddNumberInput(chanceInputBounds = chanceInputBounds.RightCopy(3.0), delegate
			{
				OnTextChanced(3);
			}, CairoFont.WhiteDetailText(), "chance4")
			.AddNumberInput(chanceInputBounds = chanceInputBounds.RightCopy(3.0), delegate
			{
				OnTextChanced(4);
			}, CairoFont.WhiteDetailText(), "chance5")
			.AddNumberInput(chanceInputBounds = chanceInputBounds.RightCopy(3.0), delegate
			{
				OnTextChanced(5);
			}, CairoFont.WhiteDetailText(), "chance6")
			.AddNumberInput(chanceInputBounds = chanceInputBounds.RightCopy(3.0), delegate
			{
				OnTextChanced(6);
			}, CairoFont.WhiteDetailText(), "chance7")
			.AddNumberInput(chanceInputBounds = chanceInputBounds.RightCopy(3.0), delegate
			{
				OnTextChanced(7);
			}, CairoFont.WhiteDetailText(), "chance8")
			.AddNumberInput(chanceInputBounds = chanceInputBounds.RightCopy(3.0), delegate
			{
				OnTextChanced(8);
			}, CairoFont.WhiteDetailText(), "chance9")
			.AddNumberInput(chanceInputBounds = chanceInputBounds.RightCopy(3.0), delegate
			{
				OnTextChanced(9);
			}, CairoFont.WhiteDetailText(), "chance10")
			.AddButton("Close", OnCloseClicked, leftButton.FixedUnder(chanceInputBounds, 25.0))
			.AddDynamicText(text, CairoFont.WhiteDetailText(), chanceTextBounds.FixedUnder(chanceInputBounds, 25.0), "totalchance")
			.AddButton("Save", OnSaveClicked, rightButton.FixedUnder(chanceInputBounds, 25.0))
			.EndChildElements()
			.Compose();
		for (int i = 0; i < 10; i++)
		{
			base.SingleComposer.GetNumberInput("chance" + (i + 1)).SetValue(chances[i].ToString() ?? "");
		}
		base.SingleComposer.GetSlotGrid("slots").CanClickSlot = OnCanClickSlot;
	}

	private bool OnSaveClicked()
	{
		save = true;
		TryClose();
		return true;
	}

	private bool OnCloseClicked()
	{
		TryClose();
		return true;
	}

	private void OnTextChanced(int index)
	{
		if (!updating)
		{
			UpdateRatios(index);
		}
	}

	public void UpdateRatios(int forceUnchanged = -1)
	{
		updating = true;
		int quantityFilledSlots = 0;
		float totalChance = 0f;
		for (int j = 0; j < 10; j++)
		{
			ItemSlot slot = inv[j];
			quantityFilledSlots += ((slot.Itemstack != null) ? 1 : 0);
			GuiElementNumberInput inp2 = base.SingleComposer.GetNumberInput("chance" + (j + 1));
			totalChance += inp2.GetValue();
		}
		float scaleValue = 100f / totalChance;
		int totalNew = 0;
		for (int i = 0; i < 10; i++)
		{
			GuiElementNumberInput inp = base.SingleComposer.GetNumberInput("chance" + (i + 1));
			if (inv[i].Itemstack == null)
			{
				inp.SetValue("");
				continue;
			}
			int newVal = (int)(inp.GetValue() * scaleValue);
			if (inp.GetText().Length != 0)
			{
				if ((i != forceUnchanged || (int)inp.GetValue() > 100) && totalChance > 100f)
				{
					inp.SetValue(newVal.ToString() ?? "");
					totalNew += newVal;
				}
				else
				{
					totalNew += (int)inp.GetValue();
				}
			}
		}
		updating = false;
		GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("totalchance");
		int num = totalNew;
		dynamicText.SetNewText("Total chance: " + num + "%");
	}

	private bool OnCanClickSlot(int slotID)
	{
		ItemStack mousestack = capi.World.Player.InventoryManager.MouseItemSlot.Itemstack;
		if (mousestack == null)
		{
			inv[slotID].Itemstack = null;
		}
		else
		{
			inv[slotID].Itemstack = mousestack.Clone();
		}
		inv[slotID].MarkDirty();
		UpdateRatios();
		return false;
	}

	private void SendInvPacket(object t1)
	{
		UpdateRatios();
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}
}
