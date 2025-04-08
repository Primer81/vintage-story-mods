using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogEditcondition : GuiDialog
{
	private GuiDialogActivityCollection guiDialogActivityCollection;

	public IActionCondition actioncondition;

	public bool Saved;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogEditcondition(ICoreClientAPI capi)
		: base(capi)
	{
	}

	public GuiDialogEditcondition(ICoreClientAPI capi, GuiDialogActivityCollection guiDialogActivityCollection, IActionCondition actioncondition)
		: this(capi)
	{
		this.guiDialogActivityCollection = guiDialogActivityCollection;
		this.actioncondition = actioncondition?.Clone();
		Compose();
	}

	private void Compose()
	{
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);
		ElementBounds leftButton = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds rightButton = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds dropDownBounds = ElementBounds.Fixed(0.0, 30.0, 160.0, 25.0);
		ElementBounds chBounds = ElementBounds.Fixed(0.0, 100.0, 300.0, 400.0);
		chBounds.verticalSizing = ElementSizing.FitToChildren;
		chBounds.AllowNoChildren = true;
		OrderedDictionary<string, Type> conditionTypes = ActivityModSystem.ConditionTypes;
		string[] values = conditionTypes.Keys.ToArray();
		string[] names = conditionTypes.Keys.ToArray();
		base.SingleComposer = capi.Gui.CreateCompo("editcondition", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar("Edit condition", OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddDropDown(values, names, values.IndexOf(actioncondition?.Type ?? ""), onSelectionChanged, dropDownBounds)
			.AddSwitch(null, ElementBounds.FixedSize(20.0, 20.0).FixedUnder(dropDownBounds, 10.0), "invert", 20.0)
			.AddStaticText("Invert Condition", CairoFont.WhiteDetailText(), ElementBounds.Fixed(30.0, 10.0, 200.0, 25.0).FixedUnder(dropDownBounds))
			.BeginChildElements(chBounds);
		if (actioncondition != null)
		{
			actioncondition.AddGuiEditFields(capi, base.SingleComposer);
		}
		ElementBounds b = base.SingleComposer.LastAddedElementBounds;
		base.SingleComposer.EndChildElements().AddSmallButton(Lang.Get("Cancel"), OnClose, leftButton.FixedUnder(b, 110.0)).AddSmallButton(Lang.Get("Confirm"), OnSave, rightButton.FixedUnder(b, 110.0), EnumButtonStyle.Normal, "confirm")
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetButton("confirm").Enabled = actioncondition != null;
		base.SingleComposer.GetSwitch("invert").On = actioncondition?.Invert ?? false;
	}

	private void onSelectionChanged(string code, bool selected)
	{
		OrderedDictionary<string, Type> conditionTypes = ActivityModSystem.ConditionTypes;
		actioncondition = (IActionCondition)Activator.CreateInstance(conditionTypes[code]);
		base.SingleComposer.GetButton("confirm").Enabled = actioncondition != null;
		Compose();
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	private bool OnClose()
	{
		TryClose();
		return true;
	}

	private bool OnSave()
	{
		Saved = true;
		actioncondition.Invert = base.SingleComposer.GetSwitch("invert").On;
		actioncondition.StoreGuiEditFields(capi, base.SingleComposer);
		TryClose();
		return true;
	}
}
