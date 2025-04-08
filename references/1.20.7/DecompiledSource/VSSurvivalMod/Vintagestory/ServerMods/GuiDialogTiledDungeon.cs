using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

namespace Vintagestory.ServerMods;

public class GuiDialogTiledDungeon : GuiDialogGeneric
{
	private bool save;

	public override ITreeAttribute Attributes
	{
		get
		{
			TreeAttribute treeAttribute = new TreeAttribute();
			treeAttribute.SetInt("save", save ? 1 : 0);
			GuiElementTextInput inp = base.SingleComposer.GetTextInput("constraints");
			treeAttribute.SetString("constraints", inp.GetText());
			return treeAttribute;
		}
	}

	public GuiDialogTiledDungeon(string dialogTitle, string constraint, ICoreClientAPI capi)
		: base(dialogTitle, capi)
	{
		double pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
		ElementBounds slotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, 45.0 + pad, 10, 1).FixedGrow(2.0 * pad, 2.0 * pad);
		ElementBounds chanceInputBounds = ElementBounds.Fixed(3.0, 0.0, 48.0, 30.0).FixedUnder(slotBounds, -4.0);
		ElementBounds leftButton = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(10.0, 1.0);
		ElementBounds rightButton = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(10.0, 1.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);
		base.SingleComposer = capi.Gui.CreateCompo("tiledungeon", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(dialogTitle, OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddTextInput(slotBounds, OnTextChanged, CairoFont.TextInput(), "constraints")
			.AddButton("Close", OnCloseClicked, leftButton.FixedUnder(chanceInputBounds, 25.0))
			.AddButton("Save", OnSaveClicked, rightButton.FixedUnder(chanceInputBounds, 25.0))
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetTextInput("constraints").SetValue(constraint);
	}

	private void OnTextChanged(string obj)
	{
	}

	private void OnTitleBarClose()
	{
		TryClose();
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
}
