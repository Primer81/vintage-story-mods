using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogEditAction : GuiDialog
{
	private GuiDialogActivityCollection guiDialogActivityCollection;

	public IEntityAction entityAction;

	public bool Saved;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogEditAction(ICoreClientAPI capi, GuiDialogActivityCollection guiDialogActivityCollection, IEntityAction entityAction)
		: base(capi)
	{
		this.entityAction = entityAction?.Clone();
		this.guiDialogActivityCollection = guiDialogActivityCollection;
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
		ElementBounds chBounds = ElementBounds.Fixed(0.0, 70.0, 350.0, 400.0);
		chBounds.verticalSizing = ElementSizing.FitToChildren;
		chBounds.AllowNoChildren = true;
		OrderedDictionary<string, Type> actionTypes = ActivityModSystem.ActionTypes;
		string[] values = actionTypes.Keys.ToArray();
		string[] names = actionTypes.Keys.ToArray();
		base.SingleComposer = capi.Gui.CreateCompo("editaction", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar("Edit Action", OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddDropDown(values, names, values.IndexOf(entityAction?.Type ?? ""), onSelectionChanged, dropDownBounds)
			.BeginChildElements(chBounds);
		if (entityAction != null)
		{
			entityAction.AddGuiEditFields(capi, base.SingleComposer);
		}
		ElementBounds b = base.SingleComposer.LastAddedElementBounds;
		base.SingleComposer.EndChildElements().AddSmallButton(Lang.Get("Cancel"), OnClose, leftButton.FixedUnder(b, 80.0)).AddSmallButton(Lang.Get("Confirm"), OnSave, rightButton.FixedUnder(b, 80.0), EnumButtonStyle.Normal, "confirm")
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetButton("confirm").Enabled = entityAction != null;
	}

	private bool OnClose()
	{
		TryClose();
		return true;
	}

	private bool OnSave()
	{
		if (!entityAction.StoreGuiEditFields(capi, base.SingleComposer))
		{
			return true;
		}
		Saved = true;
		TryClose();
		return true;
	}

	private void onSelectionChanged(string code, bool selected)
	{
		OrderedDictionary<string, Type> actionTypes = ActivityModSystem.ActionTypes;
		entityAction = (IEntityAction)Activator.CreateInstance(actionTypes[code]);
		base.SingleComposer.GetButton("confirm").Enabled = entityAction != null;
		Compose();
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}
}
