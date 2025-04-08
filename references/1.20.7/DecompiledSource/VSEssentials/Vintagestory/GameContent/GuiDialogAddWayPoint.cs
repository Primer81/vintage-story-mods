using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GuiDialogAddWayPoint : GuiDialogGeneric
{
	private EnumDialogType dialogType;

	internal Vec3d WorldPos;

	private int[] colors;

	private string[] icons;

	private string curIcon;

	private string curColor;

	private bool autoSuggest = true;

	private bool ignoreNextAutosuggestDisable;

	public override bool PrefersUngrabbedMouse => true;

	public override EnumDialogType DialogType => dialogType;

	public override double DrawOrder => 0.2;

	public override bool DisableMouseGrab => true;

	public GuiDialogAddWayPoint(ICoreClientAPI capi, WaypointMapLayer wml)
		: base("", capi)
	{
		icons = wml.WaypointIcons.Keys.ToArray();
		colors = wml.WaypointColors.ToArray();
		ComposeDialog();
	}

	public override bool TryOpen()
	{
		ComposeDialog();
		return base.TryOpen();
	}

	private void ComposeDialog()
	{
		ElementBounds leftColumn = ElementBounds.Fixed(0.0, 28.0, 90.0, 25.0);
		ElementBounds rightColumn = leftColumn.RightCopy();
		ElementBounds buttonRow = ElementBounds.Fixed(0.0, 28.0, 360.0, 25.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		bgBounds.WithChildren(leftColumn, rightColumn);
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		if (base.SingleComposer != null)
		{
			base.SingleComposer.Dispose();
		}
		int colorIconSize = 22;
		curIcon = icons[0];
		curColor = ColorUtil.Int2Hex(colors[0]);
		base.SingleComposer = capi.Gui.CreateCompo("worldmap-addwp", dialogBounds).AddShadedDialogBG(bgBounds, withTitleBar: false).AddDialogTitleBar(Lang.Get("Add waypoint"), delegate
		{
			TryClose();
		})
			.BeginChildElements(bgBounds)
			.AddStaticText(Lang.Get("Name"), CairoFont.WhiteSmallText(), leftColumn = leftColumn.FlatCopy())
			.AddTextInput(rightColumn = rightColumn.FlatCopy().WithFixedWidth(200.0), onNameChanged, CairoFont.TextInput(), "nameInput")
			.AddStaticText(Lang.Get("Pinned"), CairoFont.WhiteSmallText(), leftColumn = leftColumn.BelowCopy(0.0, 9.0))
			.AddSwitch(onPinnedToggled, rightColumn = rightColumn.BelowCopy(0.0, 5.0).WithFixedWidth(200.0), "pinnedSwitch")
			.AddRichtext(Lang.Get("waypoint-color"), CairoFont.WhiteSmallText(), leftColumn = leftColumn.BelowCopy(0.0, 5.0))
			.AddColorListPicker(colors, onColorSelected, leftColumn = leftColumn.BelowCopy(0.0, 5.0).WithFixedSize(colorIconSize, colorIconSize), 270, "colorpicker")
			.AddStaticText(Lang.Get("Icon"), CairoFont.WhiteSmallText(), leftColumn = leftColumn.WithFixedPosition(0.0, leftColumn.fixedY + leftColumn.fixedHeight).WithFixedWidth(200.0).BelowCopy())
			.AddIconListPicker(icons, onIconSelected, leftColumn = leftColumn.BelowCopy(0.0, 5.0).WithFixedSize(colorIconSize + 5, colorIconSize + 5), 270, "iconpicker")
			.AddSmallButton(Lang.Get("Cancel"), onCancel, buttonRow.FlatCopy().FixedUnder(leftColumn).WithFixedWidth(100.0))
			.AddSmallButton(Lang.Get("Save"), onSave, buttonRow.FlatCopy().FixedUnder(leftColumn).WithFixedWidth(100.0)
				.WithAlignment(EnumDialogArea.RightFixed), EnumButtonStyle.Normal, "saveButton")
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetButton("saveButton").Enabled = false;
		base.SingleComposer.ColorListPickerSetValue("colorpicker", 0);
		base.SingleComposer.IconListPickerSetValue("iconpicker", 0);
	}

	private void onIconSelected(int index)
	{
		curIcon = icons[index];
		autoSuggestName();
	}

	private void onColorSelected(int index)
	{
		curColor = ColorUtil.Int2Hex(colors[index]);
		autoSuggestName();
	}

	private void onPinnedToggled(bool on)
	{
	}

	private void autoSuggestName()
	{
		if (autoSuggest)
		{
			GuiElementTextInput textElem = base.SingleComposer.GetTextInput("nameInput");
			ignoreNextAutosuggestDisable = true;
			if (Lang.HasTranslation("wpSuggestion-" + curIcon + "-" + curColor))
			{
				textElem.SetValue(Lang.Get("wpSuggestion-" + curIcon + "-" + curColor));
			}
			else if (Lang.HasTranslation("wpSuggestion-" + curIcon))
			{
				textElem.SetValue(Lang.Get("wpSuggestion-" + curIcon));
			}
			else
			{
				textElem.SetValue("");
			}
		}
	}

	private bool onSave()
	{
		string name = base.SingleComposer.GetTextInput("nameInput").GetText();
		bool pinned = base.SingleComposer.GetSwitch("pinnedSwitch").On;
		capi.SendChatMessage($"/waypoint addati {curIcon} ={WorldPos.X.ToString(GlobalConstants.DefaultCultureInfo)} ={WorldPos.Y.ToString(GlobalConstants.DefaultCultureInfo)} ={WorldPos.Z.ToString(GlobalConstants.DefaultCultureInfo)} {pinned} {curColor} {name}");
		TryClose();
		return true;
	}

	private bool onCancel()
	{
		TryClose();
		return true;
	}

	private void onNameChanged(string t1)
	{
		base.SingleComposer.GetButton("saveButton").Enabled = t1.Trim() != "";
		if (!ignoreNextAutosuggestDisable)
		{
			autoSuggest = t1.Length == 0;
		}
		ignoreNextAutosuggestDisable = false;
	}

	public override bool CaptureAllInputs()
	{
		return IsOpened();
	}

	public override void OnMouseDown(MouseEvent args)
	{
		base.OnMouseDown(args);
		args.Handled = true;
	}

	public override void OnMouseUp(MouseEvent args)
	{
		base.OnMouseUp(args);
		args.Handled = true;
	}

	public override void OnMouseMove(MouseEvent args)
	{
		base.OnMouseMove(args);
		args.Handled = true;
	}

	public override void OnMouseWheel(MouseWheelEventArgs args)
	{
		base.OnMouseWheel(args);
		args.SetHandled();
	}
}
