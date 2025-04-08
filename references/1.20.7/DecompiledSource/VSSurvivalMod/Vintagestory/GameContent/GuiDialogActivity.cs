using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogActivity : GuiDialog
{
	private GuiDialogActivityCollection guiDialogActivityCollection;

	public EntityActivity entityActivity;

	public bool Saved;

	protected ElementBounds actionsClipBounds;

	protected ElementBounds conditionsClipBounds;

	protected GuiElementCellList<IEntityAction> actionListElem;

	protected GuiElementCellList<IActionCondition> conditionsListElem;

	private bool isNew;

	private int selectedActionIndex = -1;

	private int selectedConditionIndex = -1;

	private int collectionIndex;

	internal static ActivityVisualizer visualizer;

	private GuiDialogEditcondition editCondDlg;

	private GuiDialogEditAction editActionDlg;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogActivity(ICoreClientAPI capi, GuiDialogActivityCollection guiDialogActivityCollection, EntityActivitySystem vas, EntityActivity entityActivity, int collectionIndex)
		: base(capi)
	{
		if (entityActivity == null)
		{
			isNew = true;
			entityActivity = new EntityActivity();
		}
		this.guiDialogActivityCollection = guiDialogActivityCollection;
		this.entityActivity = entityActivity.Clone();
		this.entityActivity.OnLoaded(vas);
		this.collectionIndex = collectionIndex;
		Compose();
	}

	private void Compose()
	{
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 40.0);
		CairoFont btnFont = CairoFont.SmallButtonText(EnumButtonStyle.Small);
		ElementBounds namelabelBounds = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 20.0, 180.0, 20.0);
		ElementBounds nameBounds = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 180.0, 25.0).FixedUnder(namelabelBounds);
		ElementBounds codelabelBounds = namelabelBounds.RightCopy(12.0).WithFixedWidth(103.0);
		ElementBounds codeBounds = nameBounds.RightCopy(12.0).WithFixedWidth(103.0);
		ElementBounds prioritylabelBounds = codelabelBounds.RightCopy(12.0).WithFixedWidth(60.0);
		ElementBounds priorityBounds = codeBounds.RightCopy(12.0).WithFixedWidth(60.0);
		ElementBounds slotlabelBounds = prioritylabelBounds.RightCopy(12.0).WithFixedWidth(40.0);
		ElementBounds slotBounds = priorityBounds.RightCopy(12.0).WithFixedWidth(40.0);
		ElementBounds conditionOplabelBounds = slotlabelBounds.RightCopy(12.0).WithFixedWidth(100.0);
		ElementBounds opDropBounds = slotBounds.RightCopy(12.0).WithFixedWidth(80.0);
		ElementBounds actionsLabelBounds = nameBounds.BelowCopy(0.0, 15.0);
		ElementBounds leftButton = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds rightButton = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds actionsListBounds = ElementBounds.Fixed(0.0, 0.0, 500.0, 280.0);
		actionsClipBounds = actionsListBounds.ForkBoundingParent().FixedUnder(actionsLabelBounds, -3.0);
		ElementBounds actionsInsetBounds = actionsListBounds.FlatCopy().FixedGrow(3.0);
		ElementBounds actionsScrollbarBounds = actionsInsetBounds.CopyOffsetedSibling(3.0 + actionsListBounds.fixedWidth + 7.0).WithFixedWidth(20.0);
		ElementBounds daBounds = leftButton.FlatCopy().FixedUnder(actionsClipBounds, 10.0);
		ElementBounds maBounds = daBounds.FlatCopy().WithFixedOffset(btnFont.GetTextExtents("Delete Action").Width / (double)RuntimeEnv.GUIScale + 16.0 + 10.0, 0.0);
		ElementBounds aaBounds = daBounds.FlatCopy().FixedRightOf(maBounds).WithFixedOffset(btnFont.GetTextExtents("Modify Action").Width / (double)RuntimeEnv.GUIScale + 16.0 + 10.0, 0.0);
		ElementBounds upBounds = daBounds.FlatCopy().FixedRightOf(aaBounds).WithFixedOffset(btnFont.GetTextExtents("Add Action").Width / (double)RuntimeEnv.GUIScale + 16.0 + 20.0, 0.0);
		ElementBounds downBounds = daBounds.FlatCopy().FixedRightOf(upBounds).WithFixedOffset(btnFont.GetTextExtents("M. Up").Width / (double)RuntimeEnv.GUIScale + 16.0 + 2.0, 0.0);
		ElementBounds conditionsLabelBounds = nameBounds.FlatCopy().FixedUnder(aaBounds, 10.0);
		ElementBounds conditionsListBounds = ElementBounds.Fixed(0.0, 0.0, 500.0, 120.0);
		conditionsClipBounds = conditionsListBounds.ForkBoundingParent().FixedUnder(conditionsLabelBounds);
		ElementBounds conditionsInsetBounds = conditionsListBounds.FlatCopy().FixedGrow(3.0);
		ElementBounds conditionsScrollbarBounds = conditionsInsetBounds.CopyOffsetedSibling(3.0 + conditionsListBounds.fixedWidth + 7.0).WithFixedWidth(20.0);
		ElementBounds dtBounds = leftButton.FlatCopy().FixedUnder(conditionsClipBounds, 10.0);
		ElementBounds mtBounds = dtBounds.FlatCopy().WithFixedOffset(btnFont.GetTextExtents("Delete Condition").Width / (double)RuntimeEnv.GUIScale + 16.0 + 10.0, 0.0);
		ElementBounds atBounds = dtBounds.FlatCopy().FixedRightOf(mtBounds).WithFixedOffset(btnFont.GetTextExtents("Modify Condition").Width / (double)RuntimeEnv.GUIScale + 16.0 + 10.0, 0.0);
		ElementBounds vsBounds = ElementBounds.Fixed(0.0, 0.0, 25.0, 25.0).WithAlignment(EnumDialogArea.RightFixed).FixedUnder(conditionsClipBounds, 10.0);
		string key = "activityedit-" + (guiDialogActivityCollection.assetpath?.ToShortString() ?? "new") + "-" + collectionIndex;
		base.SingleComposer = capi.Gui.CreateCompo(key, dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar("Create/Modify Activity", OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddStaticText("Activity Name", CairoFont.WhiteDetailText(), namelabelBounds)
			.AddTextInput(nameBounds, onNameChanged, CairoFont.WhiteDetailText(), "name")
			.AddStaticText("Activity Code", CairoFont.WhiteDetailText(), codelabelBounds)
			.AddTextInput(codeBounds, onCodeChanged, CairoFont.WhiteDetailText(), "code")
			.AddStaticText("Priority", CairoFont.WhiteDetailText(), prioritylabelBounds)
			.AddNumberInput(priorityBounds, onPrioChanged, CairoFont.WhiteDetailText(), "priority")
			.AddStaticText("Slot", CairoFont.WhiteDetailText(), slotlabelBounds)
			.AddNumberInput(slotBounds, onSlotChanged, CairoFont.WhiteDetailText(), "slot")
			.AddStaticText("Conditions OP", CairoFont.WhiteDetailText(), conditionOplabelBounds)
			.AddDropDown(new string[2] { "OR", "AND" }, new string[2] { "OR", "AND" }, (int)entityActivity.ConditionsOp, onDropOpChanged, opDropBounds, "opdropdown")
			.AddStaticText("Actions", CairoFont.WhiteDetailText(), actionsLabelBounds)
			.BeginClip(actionsClipBounds)
			.AddInset(actionsInsetBounds, 3)
			.AddCellList(actionsListBounds, createActionCell, entityActivity.Actions, "actions")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarValueActions, actionsScrollbarBounds, "actionsScrollbar")
			.AddSmallButton(Lang.Get("Delete Action"), OnDeleteAction, daBounds, EnumButtonStyle.Small, "deleteaction")
			.AddSmallButton(Lang.Get("Modify Action"), () => OpenActionDlg(entityActivity.Actions[selectedActionIndex]), maBounds, EnumButtonStyle.Small, "modifyaction")
			.AddSmallButton(Lang.Get("Add Action"), () => OpenActionDlg(null), aaBounds, EnumButtonStyle.Small, "addaction")
			.AddSmallButton(Lang.Get("M. Up"), moveUp, upBounds, EnumButtonStyle.Small, "moveup")
			.AddSmallButton(Lang.Get("M. Down"), moveDown, downBounds, EnumButtonStyle.Small, "movedown")
			.AddStaticText("Conditions", CairoFont.WhiteDetailText(), conditionsLabelBounds)
			.BeginClip(conditionsClipBounds)
			.AddInset(conditionsInsetBounds, 3)
			.AddCellList(conditionsListBounds, createConditionCell, entityActivity.Conditions, "conditions")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarValueconditions, conditionsScrollbarBounds, "conditionsScrollbar")
			.AddSmallButton(Lang.Get("Delete condition"), OnDeletecondition, dtBounds, EnumButtonStyle.Small, "deletecondition")
			.AddSmallButton(Lang.Get("Modify condition"), () => OpenconditionDlg(entityActivity.Conditions[selectedConditionIndex]), mtBounds, EnumButtonStyle.Small, "modifycondition")
			.AddSmallButton(Lang.Get("Add condition"), () => OpenconditionDlg(null), atBounds, EnumButtonStyle.Small, "addcondition")
			.AddIconButton("line", OnVisualize, vsBounds, "visualize")
			.AddSmallButton(Lang.Get("Close"), OnCancel, leftButton.FlatCopy().FixedUnder(vsBounds, 40.0))
			.AddSmallButton(Lang.Get("Save"), OnSaveActivity, rightButton.FixedUnder(vsBounds, 40.0), EnumButtonStyle.Normal, "create");
		base.SingleComposer.GetToggleButton("visualize").Toggleable = true;
		actionListElem = base.SingleComposer.GetCellList<IEntityAction>("actions");
		actionListElem.BeforeCalcBounds();
		actionListElem.UnscaledCellVerPadding = 0;
		actionListElem.unscaledCellSpacing = 5;
		conditionsListElem = base.SingleComposer.GetCellList<IActionCondition>("conditions");
		conditionsListElem.BeforeCalcBounds();
		conditionsListElem.UnscaledCellVerPadding = 0;
		conditionsListElem.unscaledCellSpacing = 5;
		base.SingleComposer.EndChildElements().Compose();
		updateButtonStates();
		updateScrollbarBounds();
		base.SingleComposer.GetTextInput("name").SetValue(entityActivity.Name);
		base.SingleComposer.GetTextInput("code").SetValue(entityActivity.Code);
		base.SingleComposer.GetNumberInput("priority").SetValue((float)entityActivity.Priority);
		base.SingleComposer.GetNumberInput("slot").SetValue(entityActivity.Slot.ToString() ?? "");
		base.SingleComposer.GetToggleButton("visualize").On = visualizer != null;
	}

	private bool moveDown()
	{
		if (selectedActionIndex >= entityActivity.Actions.Length - 1)
		{
			return false;
		}
		if (editActionDlg != null && editActionDlg.IsOpened())
		{
			capi.TriggerIngameError(this, "cantsave", "Unable to delete, place close any currently opened action dialogs first");
			return false;
		}
		IEntityAction cur = entityActivity.Actions[selectedActionIndex];
		IEntityAction next = entityActivity.Actions[selectedActionIndex + 1];
		entityActivity.Actions[selectedActionIndex] = next;
		entityActivity.Actions[selectedActionIndex + 1] = cur;
		actionListElem.ReloadCells(entityActivity.Actions);
		didClickActionCell(selectedActionIndex + 1);
		return true;
	}

	private bool moveUp()
	{
		if (selectedActionIndex == 0)
		{
			return false;
		}
		if (editActionDlg != null && editActionDlg.IsOpened())
		{
			capi.TriggerIngameError(this, "cantsave", "Unable to delete, place close any currently opened action dialogs first");
			return false;
		}
		IEntityAction cur = entityActivity.Actions[selectedActionIndex];
		IEntityAction prev = entityActivity.Actions[selectedActionIndex - 1];
		entityActivity.Actions[selectedActionIndex] = prev;
		entityActivity.Actions[selectedActionIndex - 1] = cur;
		actionListElem.ReloadCells(entityActivity.Actions);
		didClickActionCell(selectedActionIndex - 1);
		return true;
	}

	private void onDropOpChanged(string code, bool selected)
	{
		entityActivity.ConditionsOp = ((code == "AND") ? EnumConditionLogicOp.AND : EnumConditionLogicOp.OR);
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		editActionDlg?.TryClose();
		editCondDlg?.TryClose();
	}

	private void OnVisualize(bool on)
	{
		visualizer?.Dispose();
		if (on)
		{
			Entity srcEntity = capi.World.Player.Entity;
			if (GuiDialogActivityCollections.EntityId != 0L)
			{
				srcEntity = capi.World.GetEntityById(GuiDialogActivityCollections.EntityId) ?? capi.World.Player.Entity;
			}
			visualizer = new ActivityVisualizer(capi, entityActivity, srcEntity);
		}
	}

	private void updateButtonStates()
	{
		base.SingleComposer.GetButton("deleteaction").Enabled = selectedActionIndex >= 0;
		base.SingleComposer.GetButton("modifyaction").Enabled = selectedActionIndex >= 0;
		base.SingleComposer.GetButton("moveup").Enabled = selectedActionIndex >= 0;
		base.SingleComposer.GetButton("movedown").Enabled = selectedActionIndex >= 0;
		base.SingleComposer.GetButton("deletecondition").Enabled = selectedConditionIndex >= 0;
		base.SingleComposer.GetButton("modifycondition").Enabled = selectedConditionIndex >= 0;
	}

	private bool OpenconditionDlg(IActionCondition condition)
	{
		editCondDlg?.TryClose();
		editCondDlg = new GuiDialogEditcondition(capi, guiDialogActivityCollection, condition);
		editCondDlg.TryOpen();
		editCondDlg.OnClosed += delegate
		{
			if (editCondDlg.Saved)
			{
				if (condition == null)
				{
					entityActivity.Conditions = entityActivity.Conditions.Append(editCondDlg.actioncondition);
				}
				else
				{
					entityActivity.Conditions[selectedConditionIndex] = editCondDlg.actioncondition;
				}
				conditionsListElem.ReloadCells(entityActivity.Conditions);
				updateScrollbarBounds();
			}
		};
		return true;
	}

	private bool OpenActionDlg(IEntityAction entityAction)
	{
		editActionDlg?.TryClose();
		editActionDlg = new GuiDialogEditAction(capi, guiDialogActivityCollection, entityAction);
		editActionDlg.TryOpen();
		editActionDlg.OnClosed += delegate
		{
			if (editActionDlg.Saved)
			{
				if (entityAction == null)
				{
					if (selectedActionIndex != -1 && selectedActionIndex < entityActivity.Actions.Length - 1)
					{
						entityActivity.Actions = entityActivity.Actions.InsertAt(editActionDlg.entityAction, selectedActionIndex + 1);
					}
					else
					{
						entityActivity.Actions = entityActivity.Actions.Append(editActionDlg.entityAction);
					}
				}
				else
				{
					entityActivity.Actions[selectedActionIndex] = editActionDlg.entityAction;
				}
				actionListElem.ReloadCells(entityActivity.Actions);
				updateScrollbarBounds();
			}
		};
		return true;
	}

	private bool OnDeletecondition()
	{
		if (editCondDlg != null && editCondDlg.IsOpened())
		{
			capi.TriggerIngameError(this, "cantsave", "Unable to delete, place close any currently opened condition dialogs first");
			return false;
		}
		entityActivity.Conditions = entityActivity.Conditions.RemoveAt(selectedConditionIndex);
		selectedConditionIndex = Math.Max(0, selectedConditionIndex - 1);
		conditionsListElem.ReloadCells(entityActivity.Conditions);
		if (entityActivity.Conditions.Length != 0)
		{
			didClickconditionCell(selectedConditionIndex);
		}
		else
		{
			selectedConditionIndex = -1;
		}
		updateButtonStates();
		return true;
	}

	private bool OnDeleteAction()
	{
		if (editActionDlg != null && editActionDlg.IsOpened())
		{
			capi.TriggerIngameError(this, "cantsave", "Unable to delete, place close any currently opened action dialogs first");
			return false;
		}
		entityActivity.Actions = entityActivity.Actions.RemoveAt(selectedActionIndex);
		selectedActionIndex = Math.Max(0, selectedActionIndex - 1);
		actionListElem.ReloadCells(entityActivity.Actions);
		if (entityActivity.Actions.Length != 0)
		{
			didClickActionCell(selectedActionIndex);
		}
		else
		{
			selectedActionIndex = -1;
		}
		updateButtonStates();
		return true;
	}

	private bool OnSaveActivity()
	{
		if (entityActivity.Code == null || entityActivity.Code.Length == 0)
		{
			entityActivity.Code = entityActivity.Name;
			base.SingleComposer.GetTextInput("code").SetValue(entityActivity.Code);
		}
		entityActivity.Priority = base.SingleComposer.GetNumberInput("priority").GetValue();
		int num;
		if (entityActivity.Actions.Length != 0 && entityActivity.Conditions.Length != 0 && entityActivity.Name != null)
		{
			num = ((entityActivity.Name.Length > 0) ? 1 : 0);
			if (num != 0)
			{
				goto IL_00cf;
			}
		}
		else
		{
			num = 0;
		}
		capi.TriggerIngameError(this, "missingfields", "Requires at least 1 action, 1 condition and activity name");
		goto IL_00cf;
		IL_00cf:
		if (num != 0)
		{
			collectionIndex = guiDialogActivityCollection.SaveActivity(entityActivity, collectionIndex);
		}
		return (byte)num != 0;
	}

	private void onSlotChanged(string text)
	{
		entityActivity.Slot = text.ToInt();
	}

	private void onPrioChanged(string text)
	{
		entityActivity.Priority = text.ToDouble();
	}

	private void onNameChanged(string text)
	{
		entityActivity.Name = text;
	}

	private void onCodeChanged(string text)
	{
		entityActivity.Code = text;
	}

	private IGuiElementCell createActionCell(IEntityAction action, ElementBounds bounds)
	{
		bounds.fixedPaddingY = 0.0;
		return new ActivityCellEntry(capi, bounds, entityActivity.Actions.IndexOf(action) + ". " + action.Type, action.ToString(), didClickActionCell, 150f, 350f);
	}

	private IGuiElementCell createConditionCell(IActionCondition condition, ElementBounds bounds)
	{
		bounds.fixedPaddingY = 0.0;
		return new ActivityCellEntry(capi, bounds, condition.Type, condition.ToString(), didClickconditionCell, 150f, 350f);
	}

	private void didClickconditionCell(int index)
	{
		foreach (IGuiElementCell elementCell in conditionsListElem.elementCells)
		{
			(elementCell as ActivityCellEntry).Selected = false;
		}
		selectedConditionIndex = index;
		(conditionsListElem.elementCells[index] as ActivityCellEntry).Selected = true;
		updateButtonStates();
	}

	private void didClickActionCell(int index)
	{
		foreach (IGuiElementCell elementCell in actionListElem.elementCells)
		{
			(elementCell as ActivityCellEntry).Selected = false;
		}
		selectedActionIndex = index;
		(actionListElem.elementCells[index] as ActivityCellEntry).Selected = true;
		updateButtonStates();
	}

	private bool OnCancel()
	{
		TryClose();
		return true;
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	private void updateScrollbarBounds()
	{
		if (actionListElem != null)
		{
			base.SingleComposer.GetScrollbar("actionsScrollbar")?.Bounds.CalcWorldBounds();
			base.SingleComposer.GetScrollbar("actionsScrollbar")?.SetHeights((float)actionsClipBounds.fixedHeight, (float)actionListElem.Bounds.fixedHeight);
			base.SingleComposer.GetScrollbar("conditionsScrollbar")?.Bounds.CalcWorldBounds();
			base.SingleComposer.GetScrollbar("conditionsScrollbar")?.SetHeights((float)conditionsClipBounds.fixedHeight, (float)conditionsListElem.Bounds.fixedHeight);
		}
	}

	private void OnNewScrollbarValueActions(float value)
	{
		actionListElem = base.SingleComposer.GetCellList<IEntityAction>("actions");
		actionListElem.Bounds.fixedY = 0f - value;
		actionListElem.Bounds.CalcWorldBounds();
	}

	private void OnNewScrollbarValueconditions(float value)
	{
		conditionsListElem = base.SingleComposer.GetCellList<IActionCondition>("conditions");
		conditionsListElem.Bounds.fixedY = 0f - value;
		conditionsListElem.Bounds.CalcWorldBounds();
	}
}
