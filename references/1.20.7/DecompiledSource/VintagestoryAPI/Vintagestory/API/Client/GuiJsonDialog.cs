using System;
using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

/// <summary>
/// This is a dialogue built from JSON files.  
/// </summary>
/// <remarks>
/// JSON made this gui.  Thanks JSON.
/// </remarks>
public class GuiJsonDialog : GuiDialogGeneric
{
	private JsonDialogSettings settings;

	private int elementNumber;

	/// <summary>
	/// The debug name of the GUI
	/// </summary>
	public override string DebugName => "jsondialog-" + settings.Code;

	/// <summary>
	/// Key Combination for the GUI
	/// </summary>
	public override string ToggleKeyCombinationCode => null;

	public override bool PrefersUngrabbedMouse => settings.DisableWorldInteract;

	/// <summary>
	/// Builds the dialog using the dialog settings from JSON.
	/// </summary>
	/// <param name="settings">The dialog settings.</param>
	/// <param name="capi">The Client API</param>
	public GuiJsonDialog(JsonDialogSettings settings, ICoreClientAPI capi)
		: base("", capi)
	{
		this.settings = settings;
		ComposeDialog(focusFirstElement: true);
	}

	/// <summary>
	/// Builds the dialog using the dialog settings from JSON.
	/// </summary>
	/// <param name="settings">The dialog settings.</param>
	/// <param name="capi">The Client API</param>
	/// <param name="focusFirstElement">Should the first element be focused, after building the dialog?</param>
	public GuiJsonDialog(JsonDialogSettings settings, ICoreClientAPI capi, bool focusFirstElement)
		: base("", capi)
	{
		this.settings = settings;
		ComposeDialog(focusFirstElement);
	}

	/// <summary>
	/// Recomposes the GUI.
	/// </summary>
	public override void Recompose()
	{
		ComposeDialog();
	}

	/// <summary>
	/// Composes the dialogue with specifications dictated by JSON.
	/// </summary>
	public void ComposeDialog(bool focusFirstElement = false)
	{
		double factor = settings.SizeMultiplier;
		ElementBounds dlgBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(settings.Alignment).WithFixedPadding(10.0).WithScale(factor)
			.WithFixedPosition(settings.PosX, settings.PosY);
		GuiComposer composer = capi.Gui.CreateCompo("cmdDlg" + settings.Code, dlgBounds).AddDialogBG(ElementStdBounds.DialogBackground().WithScale(factor).WithFixedPadding(settings.Padding), withTitleBar: false).BeginChildElements();
		double y = 0.0;
		int elemKey = 1;
		for (int i = 0; i < settings.Rows.Length; i++)
		{
			DialogRow row = settings.Rows[i];
			y += (double)row.TopPadding;
			double maxheight = 0.0;
			double x = 0.0;
			for (int j = 0; j < row.Elements.Length; j++)
			{
				DialogElement elem = row.Elements[j];
				maxheight = Math.Max(elem.Height, maxheight);
				x += (double)elem.PaddingLeft;
				ComposeElement(composer, settings, elem, elemKey, x, y);
				elemKey++;
				x += (double)(elem.Width + 20);
			}
			y += maxheight + (double)row.BottomPadding;
		}
		Composers["cmdDlg" + settings.Code] = composer.EndChildElements().Compose(focusFirstElement);
	}

	private void ComposeElement(GuiComposer composer, JsonDialogSettings settings, DialogElement elem, int elemKey, double x, double y)
	{
		double factor = settings.SizeMultiplier;
		double labelWidth = 0.0;
		if (elem.Label != null)
		{
			CairoFont font5 = CairoFont.WhiteSmallText();
			font5.UnscaledFontsize *= factor;
			labelWidth = font5.GetTextExtents(elem.Label).Width / factor / (double)RuntimeEnv.GUIScale + 1.0;
			FontExtents fext = font5.GetFontExtents();
			ElementBounds labelBounds = ElementBounds.Fixed(x, y + Math.Max(0.0, ((double)elem.Height * factor - fext.Height / (double)RuntimeEnv.GUIScale) / 2.0), labelWidth, elem.Height).WithScale(factor);
			composer.AddStaticText(elem.Label, font5, labelBounds);
			labelWidth += 8.0;
			if (elem.Tooltip != null)
			{
				CairoFont tfont3 = CairoFont.WhiteSmallText();
				tfont3.UnscaledFontsize *= factor;
				composer.AddHoverText(elem.Tooltip, tfont3, 350, labelBounds.FlatCopy(), "tooltip-" + elem.Code);
				composer.GetHoverText("tooltip-" + elem.Code).SetAutoWidth(on: true);
			}
		}
		ElementBounds bounds = ElementBounds.Fixed(x + labelWidth, y, (double)elem.Width - labelWidth, elem.Height).WithScale(factor);
		string currentValue = settings.OnGet?.Invoke(elem.Code);
		switch (elem.Type)
		{
		case EnumDialogElementType.Slider:
		{
			string key5 = "slider-" + elemKey;
			composer.AddSlider(delegate(int newval)
			{
				settings.OnSet?.Invoke(elem.Code, newval.ToString() ?? "");
				return true;
			}, bounds, key5);
			int curVal = 0;
			int.TryParse(currentValue, out curVal);
			composer.GetSlider(key5).SetValues(curVal, elem.MinValue, elem.MaxValue, elem.Step);
			composer.GetSlider(key5).Scale = factor;
			break;
		}
		case EnumDialogElementType.Switch:
		{
			string key4 = "switch-" + elemKey;
			composer.AddSwitch(delegate(bool newval)
			{
				settings.OnSet?.Invoke(elem.Code, newval ? "1" : "0");
			}, bounds, key4, 30.0 * factor, 5.0 * factor);
			composer.GetSwitch(key4).SetValue(currentValue == "1");
			break;
		}
		case EnumDialogElementType.Input:
		{
			string key6 = "input-" + elemKey;
			CairoFont font4 = CairoFont.WhiteSmallText();
			font4.UnscaledFontsize *= factor;
			composer.AddTextInput(bounds, delegate(string newval)
			{
				settings.OnSet?.Invoke(elem.Code, newval);
			}, font4, key6);
			composer.GetTextInput(key6).SetValue(currentValue);
			break;
		}
		case EnumDialogElementType.NumberInput:
		{
			string key3 = "numberinput-" + elemKey;
			CairoFont font3 = CairoFont.WhiteSmallText();
			font3.UnscaledFontsize *= factor;
			composer.AddNumberInput(bounds, delegate(string newval)
			{
				settings.OnSet?.Invoke(elem.Code, newval);
			}, font3, key3);
			composer.GetNumberInput(key3).SetValue(currentValue);
			break;
		}
		case EnumDialogElementType.Button:
			if (elem.Icon != null)
			{
				composer.AddIconButton(elem.Icon, delegate
				{
					settings.OnSet?.Invoke(elem.Code, null);
				}, bounds);
			}
			else
			{
				CairoFont font2 = CairoFont.ButtonText();
				font2.WithFontSize(elem.FontSize);
				composer.AddButton(elem.Text, delegate
				{
					settings.OnSet?.Invoke(elem.Code, null);
					return true;
				}, bounds.WithFixedPadding(8.0, 0.0), font2);
			}
			if (elem.Tooltip != null && elem.Label == null)
			{
				CairoFont tfont2 = CairoFont.WhiteSmallText();
				tfont2.UnscaledFontsize *= factor;
				composer.AddHoverText(elem.Tooltip, tfont2, 350, bounds.FlatCopy(), "tooltip-" + elem.Code);
				composer.GetHoverText("tooltip-" + elem.Code).SetAutoWidth(on: true);
			}
			break;
		case EnumDialogElementType.Text:
			composer.AddStaticText(elem.Text, CairoFont.WhiteMediumText().WithFontSize(elem.FontSize), bounds);
			break;
		case EnumDialogElementType.Select:
		case EnumDialogElementType.DynamicSelect:
		{
			string[] values = elem.Values;
			string[] names = elem.Names;
			if (elem.Type == EnumDialogElementType.DynamicSelect)
			{
				string[] compos = currentValue.Split(new string[1] { "\n" }, StringSplitOptions.None);
				values = compos[0].Split(new string[1] { "||" }, StringSplitOptions.None);
				names = compos[1].Split(new string[1] { "||" }, StringSplitOptions.None);
				currentValue = compos[2];
			}
			int selectedIndex = Array.FindIndex(values, (string w) => w.Equals(currentValue));
			if (elem.Mode == EnumDialogElementMode.DropDown)
			{
				string key = "dropdown-" + elemKey;
				composer.AddDropDown(values, names, selectedIndex, delegate(string newval, bool on)
				{
					settings.OnSet?.Invoke(elem.Code, newval);
				}, bounds, key);
				composer.GetDropDown(key).Scale = factor;
				composer.GetDropDown(key).Font.UnscaledFontsize *= factor;
			}
			else
			{
				if (elem.Icons == null || elem.Icons.Length == 0)
				{
					break;
				}
				ElementBounds[] manybounds = new ElementBounds[elem.Icons.Length];
				double elemHeight = (elem.Height - 4 * elem.Icons.Length) / elem.Icons.Length;
				for (int j = 0; j < manybounds.Length; j++)
				{
					manybounds[j] = bounds.FlatCopy().WithFixedHeight(elemHeight - 4.0).WithFixedOffset(0.0, (double)j * (4.0 + elemHeight))
						.WithScale(factor);
				}
				string key2 = "togglebuttons-" + elemKey;
				CairoFont font = CairoFont.WhiteSmallText();
				font.UnscaledFontsize *= factor;
				composer.AddIconToggleButtons(elem.Icons, font, delegate(int newval)
				{
					settings.OnSet?.Invoke(elem.Code, elem.Values[newval]);
				}, manybounds, key2);
				if (currentValue != null && currentValue.Length > 0)
				{
					composer.ToggleButtonsSetValue(key2, selectedIndex);
				}
				if (elem.Tooltips != null)
				{
					for (int i = 0; i < elem.Tooltips.Length; i++)
					{
						CairoFont tfont = CairoFont.WhiteSmallText();
						tfont.UnscaledFontsize *= factor;
						composer.AddHoverText(elem.Tooltips[i], tfont, 350, manybounds[i].FlatCopy());
					}
				}
			}
			break;
		}
		}
		elementNumber++;
	}

	/// <summary>
	/// Fires an event when the mouse is held down.
	/// </summary>
	/// <param name="args">The mouse events.</param>
	public override void OnMouseDown(MouseEvent args)
	{
		base.OnMouseDown(args);
		foreach (GuiComposer value in Composers.Values)
		{
			if (value.Bounds.PointInside(args.X, args.Y))
			{
				args.Handled = true;
				break;
			}
		}
	}

	public override void OnMouseUp(MouseEvent args)
	{
		base.OnMouseUp(args);
	}

	/// <summary>
	/// Reloads the values in the GUI.
	/// </summary>
	public void ReloadValues()
	{
		GuiComposer composer = Composers["cmdDlg" + settings.Code];
		int elemKey = 1;
		for (int i = 0; i < settings.Rows.Length; i++)
		{
			DialogRow row = settings.Rows[i];
			for (int j = 0; j < row.Elements.Length; j++)
			{
				DialogElement elem = row.Elements[j];
				string currentValue = settings.OnGet?.Invoke(elem.Code);
				switch (elem.Type)
				{
				case EnumDialogElementType.Slider:
				{
					string key6 = "slider-" + elemKey;
					int curVal = 0;
					int.TryParse(currentValue, out curVal);
					composer.GetSlider(key6).SetValues(curVal, elem.MinValue, elem.MaxValue, elem.Step);
					break;
				}
				case EnumDialogElementType.Switch:
				{
					string key5 = "switch-" + elemKey;
					composer.GetSwitch(key5).SetValue(currentValue == "1");
					break;
				}
				case EnumDialogElementType.Input:
				{
					string key7 = "input-" + elemKey;
					CairoFont.WhiteSmallText();
					composer.GetTextInput(key7).SetValue(currentValue);
					break;
				}
				case EnumDialogElementType.NumberInput:
				{
					string key4 = "numberinput-" + elemKey;
					composer.GetNumberInput(key4).SetValue(currentValue);
					break;
				}
				case EnumDialogElementType.Select:
				case EnumDialogElementType.DynamicSelect:
				{
					string[] values = elem.Values;
					if (elem.Type == EnumDialogElementType.DynamicSelect)
					{
						string[] compos = currentValue.Split(new string[1] { "\n" }, StringSplitOptions.None);
						values = compos[0].Split(new string[1] { "||" }, StringSplitOptions.None);
						string[] names = compos[1].Split(new string[1] { "||" }, StringSplitOptions.None);
						currentValue = compos[2];
						string key3 = "dropdown-" + elemKey;
						composer.GetDropDown(key3).SetList(values, names);
					}
					int selectedIndex = Array.FindIndex(values, (string w) => w.Equals(currentValue));
					if (elem.Mode == EnumDialogElementMode.DropDown)
					{
						string key = "dropdown-" + elemKey;
						composer.GetDropDown(key).SetSelectedIndex(selectedIndex);
					}
					else if (elem.Icons != null && elem.Icons.Length != 0)
					{
						string key2 = "togglebuttons-" + elemKey;
						if (currentValue != null && currentValue.Length > 0)
						{
							composer.ToggleButtonsSetValue(key2, selectedIndex);
						}
					}
					break;
				}
				}
				elemKey++;
			}
		}
	}
}
