using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class GuiDialogActivityCollection : GuiDialog
{
	private GuiDialogActivityCollections dlg;

	public EntityActivityCollection collection;

	private EntityActivitySystem vas;

	public AssetLocation assetpath;

	private bool isNew;

	private int selectedIndex = -1;

	protected ElementBounds clipBounds;

	protected GuiElementCellList<EntityActivity> listElem;

	private bool pause;

	private GuiDialogActivity activityDlg;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogActivityCollection(ICoreClientAPI capi, GuiDialogActivityCollections dlg, EntityActivityCollection collection, EntityActivitySystem vas, AssetLocation assetpath)
		: base(capi)
	{
		if (collection == null)
		{
			isNew = true;
			collection = new EntityActivityCollection();
		}
		this.vas = vas;
		this.assetpath = assetpath;
		this.dlg = dlg;
		this.collection = collection.Clone();
		Compose();
	}

	public GuiDialogActivityCollection(ICoreClientAPI capi)
		: base(capi)
	{
		Compose();
	}

	private void Compose()
	{
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding + 150.0, 20.0);
		ElementBounds textlabelBounds = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 20.0, 200.0, 20.0);
		ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 200.0, 25.0).FixedUnder(textlabelBounds);
		ElementBounds leftButton = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds rightButton = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		double listHeight = 350.0;
		ElementBounds listBounds = ElementBounds.Fixed(0.0, 0.0, 350.0, listHeight).FixedUnder(textBounds, 10.0);
		clipBounds = listBounds.ForkBoundingParent();
		ElementBounds insetBounds = listBounds.FlatCopy().FixedGrow(3.0);
		ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(3.0 + listBounds.fixedWidth + 7.0).WithFixedWidth(20.0);
		CairoFont btnFont = CairoFont.SmallButtonText(EnumButtonStyle.Small);
		ElementBounds dbBounds = leftButton.FlatCopy().FixedUnder(clipBounds, 10.0);
		ElementBounds mbBounds = dbBounds.FlatCopy().WithFixedOffset(btnFont.GetTextExtents("Delete Activity").Width / (double)RuntimeEnv.GUIScale + 16.0 + 10.0, 0.0);
		ElementBounds abBounds = dbBounds.FlatCopy().FixedRightOf(mbBounds, 3.0).WithFixedOffset(btnFont.GetTextExtents("Modify Activity").Width / (double)RuntimeEnv.GUIScale + 16.0 + 10.0, 0.0);
		ElementBounds exeLabelBounds = ElementBounds.Fixed(0.0, 0.0, 200.0, 25.0).WithFixedPadding(4.0, 2.0).FixedUnder(dbBounds, 35.0);
		ElementBounds exeBounds = ElementBounds.Fixed(0, 0).WithFixedPadding(4.0, 2.0).FixedUnder(exeLabelBounds);
		int num = (int)(btnFont.GetTextExtents("Execute Activity").Width / (double)RuntimeEnv.GUIScale + 16.0);
		ElementBounds exe2Bounds = ElementBounds.Fixed(num, 0).WithFixedPadding(4.0, 2.0).FixedUnder(exeLabelBounds);
		ElementBounds exe3Bounds = ElementBounds.Fixed(num + (int)(btnFont.GetTextExtents("Stop actions").Width / (double)RuntimeEnv.GUIScale + 16.0), 0).WithFixedPadding(4.0, 2.0).FixedUnder(exeLabelBounds);
		collection.Activities = collection.Activities.OrderByDescending((EntityActivity a) => a.Priority).ToList();
		base.SingleComposer = capi.Gui.CreateCompo("activitycollection-" + (assetpath?.ToShortString() ?? "new"), dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar("Create/Modify Activity collection", OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddStaticText("Collection Name", CairoFont.WhiteDetailText(), textlabelBounds)
			.AddTextInput(textBounds, onNameChanced, CairoFont.WhiteDetailText(), "name")
			.BeginClip(clipBounds)
			.AddInset(insetBounds, 3)
			.AddCellList(listBounds, createCell, collection.Activities, "activities")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar")
			.AddSmallButton(Lang.Get("Delete Activity"), OnDeleteActivity, dbBounds, EnumButtonStyle.Small, "deleteactivity")
			.AddSmallButton(Lang.Get("Modify Activity"), OnModifyActivity, mbBounds, EnumButtonStyle.Small, "modifyactivity")
			.AddSmallButton(Lang.Get("Add Activity"), OnCreateActivity, abBounds, EnumButtonStyle.Small)
			.AddIf(GuiDialogActivityCollections.EntityId > 0)
			.AddStaticText("For entity with id " + GuiDialogActivityCollections.EntityId, CairoFont.WhiteDetailText(), exeLabelBounds)
			.AddSmallButton(Lang.Get("Execute Activity"), OnExecuteActivity, exeBounds, EnumButtonStyle.Small, "exec")
			.AddSmallButton(Lang.Get("Stop actions"), OnStopActivity, exe2Bounds, EnumButtonStyle.Small, "stop")
			.AddSmallButton(Lang.Get("Toggle Autorun"), OnTogglePauseActivity, exe3Bounds, EnumButtonStyle.Small, "pause")
			.EndIf()
			.AddSmallButton(Lang.Get("Close"), OnCancel, leftButton = leftButton.FlatCopy().FixedUnder(exeBounds, 60.0))
			.AddSmallButton(Lang.Get("Save Edits"), OnSave, rightButton.FixedUnder(exeBounds, 60.0), EnumButtonStyle.Normal, "create");
		listElem = base.SingleComposer.GetCellList<EntityActivity>("activities");
		listElem.BeforeCalcBounds();
		listElem.UnscaledCellVerPadding = 0;
		listElem.unscaledCellSpacing = 5;
		base.SingleComposer.EndChildElements().Compose();
		base.SingleComposer.GetTextInput("name").SetValue(collection.Name);
		updateScrollbarBounds();
		updateButtonStates();
	}

	private bool OnStopActivity()
	{
		capi.SendChatMessage("/dev aee e[id=" + GuiDialogActivityCollections.EntityId + "] stop");
		return true;
	}

	private bool OnTogglePauseActivity()
	{
		pause = !pause;
		capi.SendChatMessage("/dev aee e[id=" + GuiDialogActivityCollections.EntityId + "] pause " + pause);
		return true;
	}

	private bool OnExecuteActivity()
	{
		capi.SendChatMessage("/dev aee e[id=" + GuiDialogActivityCollections.EntityId + "] runa " + collection.Activities[selectedIndex].Code);
		return true;
	}

	public int SaveActivity(EntityActivity activity, int index)
	{
		if (index >= collection.Activities.Count)
		{
			capi.TriggerIngameError(this, "cantsave", "Unable to save, out of index bounds");
			return -1;
		}
		if (index < 0)
		{
			collection.Activities.Add(activity);
		}
		else
		{
			collection.Activities[index] = activity;
		}
		collection.Activities = collection.Activities.OrderByDescending((EntityActivity a) => a.Priority).ToList();
		listElem.ReloadCells(collection.Activities);
		OnSave();
		if (index >= 0)
		{
			return index;
		}
		return collection.Activities.Count - 1;
	}

	private void updateButtonStates()
	{
		base.SingleComposer.GetButton("deleteactivity").Enabled = selectedIndex >= 0;
		base.SingleComposer.GetButton("modifyactivity").Enabled = selectedIndex >= 0;
		if (base.SingleComposer.GetButton("exec") != null)
		{
			base.SingleComposer.GetButton("exec").Enabled = GuiDialogActivityCollections.EntityId != 0L && selectedIndex >= 0;
		}
	}

	private bool OnDeleteActivity()
	{
		if (activityDlg != null && activityDlg.IsOpened())
		{
			capi.TriggerIngameError(this, "cantsave", "Unable to delete, place close any currently opened activity dialogs first");
			return false;
		}
		if (selectedIndex < 0)
		{
			return true;
		}
		collection.Activities.RemoveAt(selectedIndex);
		listElem.ReloadCells(collection.Activities);
		return true;
	}

	private bool OnModifyActivity()
	{
		if (selectedIndex < 0)
		{
			return true;
		}
		if (activityDlg != null && activityDlg.IsOpened())
		{
			capi.TriggerIngameError(this, "cantsave", "Unable to modify. Close any currently opened activity dialogs first");
			return false;
		}
		activityDlg = new GuiDialogActivity(capi, this, vas, collection.Activities[selectedIndex], selectedIndex);
		activityDlg.TryOpen();
		activityDlg.OnClosed += delegate
		{
			activityDlg = null;
		};
		return true;
	}

	private bool OnSave()
	{
		collection.Name = base.SingleComposer.GetTextInput("name").GetText();
		if (collection.Name == null || collection.Name.Length == 0 || collection.Activities.Count == 0)
		{
			capi.TriggerIngameError(this, "missingfields", "Requires at least one activity and a name");
			return false;
		}
		string filepath = ((!isNew) ? Path.Combine(GamePaths.AssetsPath, "survival", assetpath.Path) : Path.Combine(GamePaths.AssetsPath, "survival", "config", "activitycollections", GamePaths.ReplaceInvalidChars(collection.Name) + ".json"));
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.All
		};
		string json = JsonConvert.SerializeObject(collection, Formatting.Indented, settings);
		File.WriteAllText(filepath, json);
		dlg.ReloadCells();
		capi.Network.GetChannel("activityEditor").SendPacket(new ActivityCollectionsJsonPacket
		{
			Collections = new List<string> { json }
		});
		if (GuiDialogActivityCollections.AutoApply && GuiDialogActivityCollections.EntityId != 0L)
		{
			capi.SendChatMessage("/dev aee e[id=" + GuiDialogActivityCollections.EntityId + "] stop");
			dlg.ApplyToEntityId();
		}
		return true;
	}

	private void onNameChanced(string name)
	{
		collection.Name = name;
	}

	private bool OnCreateActivity()
	{
		activityDlg = new GuiDialogActivity(capi, this, vas, null, -1);
		activityDlg.TryOpen();
		activityDlg.OnClosed += delegate
		{
			activityDlg = null;
		};
		return true;
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

	private IGuiElementCell createCell(EntityActivity collection, ElementBounds bounds)
	{
		bounds.fixedPaddingY = 0.0;
		return new ActivityCellEntry(capi, bounds, "P" + Math.Round(collection.Priority, 2) + "  " + collection.Name, collection.Actions.Length + " actions, " + collection.Conditions.Length + " conds", didClickCell, 210f, 200f);
	}

	private void didClickCell(int index)
	{
		foreach (IGuiElementCell elementCell in listElem.elementCells)
		{
			(elementCell as ActivityCellEntry).Selected = false;
		}
		selectedIndex = index;
		(listElem.elementCells[index] as ActivityCellEntry).Selected = true;
		updateButtonStates();
	}

	private void updateScrollbarBounds()
	{
		if (listElem != null)
		{
			base.SingleComposer.GetScrollbar("scrollbar")?.Bounds.CalcWorldBounds();
			base.SingleComposer.GetScrollbar("scrollbar")?.SetHeights((float)clipBounds.fixedHeight, (float)listElem.Bounds.fixedHeight);
		}
	}

	private void OnNewScrollbarValue(float value)
	{
		listElem = base.SingleComposer.GetCellList<EntityActivity>("activities");
		listElem.Bounds.fixedY = 0f - value;
		listElem.Bounds.CalcWorldBounds();
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		activityDlg?.TryClose();
		activityDlg = null;
	}
}
