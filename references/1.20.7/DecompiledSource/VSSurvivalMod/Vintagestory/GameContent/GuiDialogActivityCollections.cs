using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogActivityCollections : GuiDialog
{
	public OrderedDictionary<AssetLocation, EntityActivityCollection> collections = new OrderedDictionary<AssetLocation, EntityActivityCollection>();

	protected ElementBounds clipBounds;

	protected GuiElementCellList<EntityActivityCollection> listElem;

	private int selectedIndex = -1;

	private EntityActivitySystem vas;

	public static long EntityId;

	public static bool AutoApply = true;

	private GuiDialogActivityCollection editDlg;

	private GuiDialogActivityCollection createDlg;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogActivityCollections(ICoreClientAPI capi)
		: base(capi)
	{
		vas = new EntityActivitySystem(capi.World.Player.Entity);
		Compose();
	}

	private void Compose()
	{
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0.0);
		ElementBounds leftButton = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds rightButton = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		double listHeight = 400.0;
		ElementBounds listBounds = ElementBounds.Fixed(0.0, 25.0, 270.0, listHeight);
		clipBounds = listBounds.ForkBoundingParent();
		ElementBounds insetBounds = listBounds.FlatCopy().FixedGrow(3.0).WithFixedOffset(0.0, 0.0);
		ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(3.0 + listBounds.fixedWidth + 7.0).WithFixedWidth(20.0);
		CairoFont cairoFont = CairoFont.SmallButtonText(EnumButtonStyle.Small);
		ElementBounds mbBounds = leftButton.FlatCopy().FixedUnder(clipBounds, 10.0);
		ElementBounds textfieldbounds = ElementBounds.FixedSize(90.0, 21.0).WithAlignment(EnumDialogArea.RightFixed).FixedUnder(clipBounds, 10.0);
		double offx = cairoFont.GetTextExtents("Modify Activity").Width / (double)RuntimeEnv.GUIScale + 20.0;
		base.SingleComposer = capi.Gui.CreateCompo("activitycollections", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar("Activity collections", OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.BeginClip(clipBounds)
			.AddInset(insetBounds, 3)
			.AddCellList(listBounds, createCell, collections.Values, "collections")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar")
			.AddSmallButton(Lang.Get("Modify"), OnModifyCollection, mbBounds, EnumButtonStyle.Small, "modifycollection")
			.AddTextInput(textfieldbounds.FlatCopy().WithFixedOffset(0.0 - offx, 2.0), null, CairoFont.WhiteDetailText(), "entityid")
			.AddSmallButton(Lang.Get("Apply to entity"), ApplyToEntityId, textfieldbounds.FlatCopy().WithFixedPadding(4.0, 1.0), EnumButtonStyle.Small, "applytoentityid")
			.AddSwitch(onAutoApply, textfieldbounds.BelowCopy(0.0, 7.0).WithFixedOffset(0.0 - offx - 69.0, 0.0), "autocopy", 20.0)
			.AddStaticText("Autoapply modifications", CairoFont.WhiteDetailText().WithFontSize(14f), textfieldbounds.BelowCopy(0.0, 9.0).WithFixedOffset(0.0, 0.0).WithFixedWidth(178.0))
			.AddSmallButton(Lang.Get("Close"), OnClose, leftButton.FixedUnder(clipBounds, 80.0))
			.AddIconButton("line", CairoFont.WhiteMediumText().WithColor(GuiStyle.ErrorTextColor), clearVisualize, leftButton.RightCopy(50.0, 1.0).WithFixedSize(22.0, 22.0).WithFixedPadding(3.0, 3.0), "clearvisualize")
			.AddSmallButton(Lang.Get("Create collection"), OnCreateCollection, rightButton.FixedUnder(clipBounds, 80.0), EnumButtonStyle.Normal, "create");
		if (EntityId != 0L)
		{
			base.SingleComposer.GetTextInput("entityid").SetValue(EntityId);
		}
		else
		{
			base.SingleComposer.GetTextInput("entityid").SetPlaceHolderText("entity id");
		}
		base.SingleComposer.GetSwitch("autocopy").On = AutoApply;
		listElem = base.SingleComposer.GetCellList<EntityActivityCollection>("collections");
		listElem.BeforeCalcBounds();
		listElem.UnscaledCellVerPadding = 0;
		listElem.unscaledCellSpacing = 5;
		base.SingleComposer.EndChildElements().Compose();
		ReloadCells();
		updateScrollbarBounds();
		updateButtonStates();
	}

	private void clearVisualize(bool on)
	{
		GuiDialogActivity.visualizer?.Dispose();
		GuiDialogActivity.visualizer = null;
	}

	private void onAutoApply(bool on)
	{
		AutoApply = on;
	}

	public bool ApplyToEntityId()
	{
		if (selectedIndex < 0)
		{
			return true;
		}
		capi.Network.GetChannel("activityEditor").SendPacket(new ApplyConfigPacket
		{
			ActivityCollectionName = collections.GetValueAtIndex(selectedIndex).Name,
			EntityId = (EntityId = base.SingleComposer.GetTextInput("entityid").GetText().ToInt())
		});
		return true;
	}

	private void updateButtonStates()
	{
		base.SingleComposer.GetButton("modifycollection").Enabled = selectedIndex >= 0;
		base.SingleComposer.GetButton("applytoentityid").Enabled = selectedIndex >= 0;
	}

	private bool OnModifyCollection()
	{
		if (selectedIndex < 0)
		{
			return true;
		}
		AssetLocation key = collections.GetKeyAtIndex(selectedIndex);
		if (EntityId > 0)
		{
			Entity selectedEntity = capi.World.GetEntityById(EntityId);
			if (selectedEntity != null)
			{
				vas.ActivityOffset = selectedEntity.WatchedAttributes.GetBlockPos("importOffset", new BlockPos(selectedEntity.Pos.Dimension));
			}
		}
		editDlg = new GuiDialogActivityCollection(capi, this, collections[key], vas, key);
		editDlg.TryOpen();
		return true;
	}

	public void ReloadCells()
	{
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.All
		};
		capi.Assets.Reload(AssetCategory.config);
		List<IAsset> many = capi.Assets.GetMany("config/activitycollections/");
		collections.Clear();
		foreach (IAsset file in many)
		{
			EntityActivityCollection entityActivityCollection2 = (collections[file.Location] = file.ToObject<EntityActivityCollection>(settings));
			entityActivityCollection2.OnLoaded(vas);
		}
		listElem.ReloadCells(collections.Values);
	}

	private bool OnCreateCollection()
	{
		if (createDlg != null && createDlg.IsOpened())
		{
			capi.TriggerIngameError(this, "alreadyopened", Lang.Get("Close the other activity collection dialog first"));
			return false;
		}
		createDlg = new GuiDialogActivityCollection(capi, this, null, vas, null);
		createDlg.TryOpen();
		return true;
	}

	private bool OnClose()
	{
		TryClose();
		return true;
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	private IGuiElementCell createCell(EntityActivityCollection collection, ElementBounds bounds)
	{
		bounds.fixedPaddingY = 0.0;
		return new ActivityCellEntry(capi, bounds, collection.Name, collection.Activities.Count + " activities", didClickCell, 160f, 200f);
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
		listElem = base.SingleComposer.GetCellList<EntityActivityCollection>("collections");
		listElem.Bounds.fixedY = 0f - value;
		listElem.Bounds.CalcWorldBounds();
	}
}
