using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public static class GuiComposerHelpers
{
	/// <summary>
	/// Returns a previously added color list picker element
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public static GuiElementColorListPicker GetColorListPicker(this GuiComposer composer, string key)
	{
		return (GuiElementColorListPicker)composer.GetElement(key);
	}

	/// <summary>
	/// Selects one of the colors from a color list picker
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key"></param>
	/// <param name="selectedIndex"></param>
	public static void ColorListPickerSetValue(this GuiComposer composer, string key, int selectedIndex)
	{
		int i = 0;
		GuiElementColorListPicker btn;
		while ((btn = composer.GetColorListPicker(key + "-" + i)) != null)
		{
			btn.SetValue(i == selectedIndex);
			i++;
		}
	}

	/// <summary>
	/// Adds a range of clickable colors
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="colors"></param>
	/// <param name="onToggle"></param>
	/// <param name="startBounds"></param>
	/// <param name="maxLineWidth"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public static GuiComposer AddColorListPicker(this GuiComposer composer, int[] colors, Action<int> onToggle, ElementBounds startBounds, int maxLineWidth, string key = null)
	{
		return composer.AddElementListPicker(typeof(GuiElementColorListPicker), colors, onToggle, startBounds, maxLineWidth, key);
	}

	/// <summary>
	/// Adds a compact vertical scrollbar to the current GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="onNewScrollbarValue">The event fired for the change in the scrollbar.</param>
	/// <param name="bounds">the bounds of the scrollbar.</param>
	/// <param name="key">the internal name of the scrollbar.</param>
	public static GuiComposer AddCompactVerticalScrollbar(this GuiComposer composer, Action<float> onNewScrollbarValue, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementCompactScrollbar(composer.Api, onNewScrollbarValue, bounds), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the scrollbar from the dialogue.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">the internal name of the scrollbar to be gotten</param>
	/// <returns>The scrollbar with the given key.</returns>
	public static GuiElementCompactScrollbar GetCompactScrollbar(this GuiComposer composer, string key)
	{
		return (GuiElementCompactScrollbar)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a multiple select dropdown to the current GUI instance.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="values">The values of the current drodown.</param>
	/// <param name="names">The names of those values.</param>
	/// <param name="selectedIndex">The default selected index.</param>
	/// <param name="onSelectionChanged">The event fired when the index is changed.</param>
	/// <param name="bounds">The bounds of the index.</param>
	/// <param name="key">The name of this dropdown.</param>
	public static GuiComposer AddMultiSelectDropDown(this GuiComposer composer, string[] values, string[] names, int selectedIndex, SelectionChangedDelegate onSelectionChanged, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementDropDown(composer.Api, values, names, selectedIndex, onSelectionChanged, bounds, CairoFont.WhiteSmallText(), multiSelect: true), key);
		}
		return composer;
	}

	/// <summary>
	/// Adds a dropdown to the current GUI instance.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="values">The values of the current drodown.</param>
	/// <param name="names">The names of those values.</param>
	/// <param name="selectedIndex">The default selected index.</param>
	/// <param name="onSelectionChanged">The event fired when the index is changed.</param>
	/// <param name="bounds">The bounds of the index.</param>
	/// <param name="key">The name of this dropdown.</param>
	public static GuiComposer AddDropDown(this GuiComposer composer, string[] values, string[] names, int selectedIndex, SelectionChangedDelegate onSelectionChanged, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementDropDown(composer.Api, values, names, selectedIndex, onSelectionChanged, bounds, CairoFont.WhiteSmallText(), multiSelect: false), key);
		}
		return composer;
	}

	/// <summary>
	/// Adds a dropdown to the current GUI instance.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="values">The values of the current drodown.</param>
	/// <param name="names">The names of those values.</param>
	/// <param name="selectedIndex">The default selected index.</param>
	/// <param name="onSelectionChanged">The event fired when the index is changed.</param>
	/// <param name="bounds">The bounds of the index.</param>
	/// <param name="font"></param>
	/// <param name="key">The name of this dropdown.</param>
	public static GuiComposer AddDropDown(this GuiComposer composer, string[] values, string[] names, int selectedIndex, SelectionChangedDelegate onSelectionChanged, ElementBounds bounds, CairoFont font, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementDropDown(composer.Api, values, names, selectedIndex, onSelectionChanged, bounds, font, multiSelect: false), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the Drop Down element from the GUIComposer by their key.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">the name of the dropdown to fetch.</param>
	public static GuiElementDropDown GetDropDown(this GuiComposer composer, string key)
	{
		return (GuiElementDropDown)composer.GetElement(key);
	}

	/// <summary>
	/// Adds multiple buttons with Text.
	/// </summary>
	/// <param name="elems"></param>
	/// <param name="onToggle">The event fired when the button is pressed.</param>
	/// <param name="startBounds">The bounds of the buttons.</param>
	/// <param name="maxLineWidth"></param>
	/// <param name="key">The key given to the bundle of buttons.</param>
	/// <param name="composer"></param>
	/// <param name="pickertype"></param>
	public static GuiComposer AddElementListPicker<T>(this GuiComposer composer, Type pickertype, T[] elems, Action<int> onToggle, ElementBounds startBounds, int maxLineWidth, string key)
	{
		if (!composer.Composed)
		{
			if (key == null)
			{
				key = "elementlistpicker";
			}
			int quantityButtons = elems.Length;
			double lineWidth = 0.0;
			for (int i = 0; i < elems.Length; i++)
			{
				int index = i;
				if (lineWidth > (double)maxLineWidth)
				{
					startBounds.fixedX -= lineWidth;
					startBounds.fixedY += startBounds.fixedHeight + 5.0;
					lineWidth = 0.0;
				}
				GuiElement elem = Activator.CreateInstance(pickertype, composer.Api, elems[i], startBounds.FlatCopy()) as GuiElement;
				composer.AddInteractiveElement(elem, key + "-" + i);
				(composer[key + "-" + i] as GuiElementElementListPickerBase<T>).handler = delegate(bool on)
				{
					if (on)
					{
						onToggle(index);
						for (int j = 0; j < quantityButtons; j++)
						{
							if (j != index)
							{
								(composer[key + "-" + j] as GuiElementElementListPickerBase<T>).SetValue(on: false);
							}
						}
					}
					else
					{
						(composer[key + "-" + index] as GuiElementElementListPickerBase<T>).SetValue(on: true);
					}
				};
				startBounds.fixedX += startBounds.fixedWidth + 5.0;
				lineWidth += startBounds.fixedWidth + 5.0;
			}
		}
		return composer;
	}

	/// <summary>
	/// Adds a set of horizontal tabs to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="tabs">The collection of tabs.</param>
	/// <param name="bounds">The bounds of the horizontal tabs.</param>
	/// <param name="onTabClicked">The event fired when the tab is clicked.</param>
	/// <param name="font">The font of the tabs.</param>
	/// <param name="selectedFont"></param>
	/// <param name="key">The key for the added horizontal tabs.</param>
	public static GuiComposer AddHorizontalTabs(this GuiComposer composer, GuiTab[] tabs, ElementBounds bounds, Action<int> onTabClicked, CairoFont font, CairoFont selectedFont, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementHorizontalTabs(composer.Api, tabs, font, selectedFont, bounds, onTabClicked), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the HorizontalTabs element from the GUI by name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The key for the horizontal tabs you want to get.</param>
	public static GuiElementHorizontalTabs GetHorizontalTabs(this GuiComposer composer, string key)
	{
		return (GuiElementHorizontalTabs)composer.GetElement(key);
	}

	/// <summary>
	/// Returns the icon list picker
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public static GuiElementIconListPicker GetIconListPicker(this GuiComposer composer, string key)
	{
		return (GuiElementIconListPicker)composer.GetElement(key);
	}

	/// <summary>
	/// Selects one of the clickable icons
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key"></param>
	/// <param name="selectedIndex"></param>
	public static void IconListPickerSetValue(this GuiComposer composer, string key, int selectedIndex)
	{
		int i = 0;
		GuiElementIconListPicker btn;
		while ((btn = composer.GetIconListPicker(key + "-" + i)) != null)
		{
			btn.SetValue(i == selectedIndex);
			i++;
		}
	}

	/// <summary>
	/// Adds multiple clickable icons 
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="icons"></param>
	/// <param name="onToggle"></param>
	/// <param name="startBounds"></param>
	/// <param name="maxLineWidth"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public static GuiComposer AddIconListPicker(this GuiComposer composer, string[] icons, Action<int> onToggle, ElementBounds startBounds, int maxLineWidth, string key = null)
	{
		return composer.AddElementListPicker(typeof(GuiElementIconListPicker), icons, onToggle, startBounds, maxLineWidth, key);
	}

	/// <summary>
	/// Adds a vertical scrollbar to the GUI.  
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="onNewScrollbarValue">The action when the scrollbar changes.</param>
	/// <param name="bounds">The bounds of the scrollbar.</param>
	/// <param name="key">The name of the scrollbar.</param>
	public static GuiComposer AddVerticalScrollbar(this GuiComposer composer, Action<float> onNewScrollbarValue, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementScrollbar(composer.Api, onNewScrollbarValue, bounds), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the scrollbar by name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the scrollbar.</param>
	/// <returns>The scrollbar itself.</returns>
	public static GuiElementScrollbar GetScrollbar(this GuiComposer composer, string key)
	{
		return (GuiElementScrollbar)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a slider to the current GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="onNewSliderValue">The event that fires when the slider's value is changed.</param>
	/// <param name="bounds">The bounds of the slider.</param>
	/// <param name="key">the internal name of the slider.</param>
	public static GuiComposer AddSlider(this GuiComposer composer, ActionConsumable<int> onNewSliderValue, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementSlider(composer.Api, onNewSliderValue, bounds), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the slider by name from the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">the internal name of the slider.</param>
	/// <returns>the slider.</returns>
	public static GuiElementSlider GetSlider(this GuiComposer composer, string key)
	{
		return (GuiElementSlider)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a switch to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="onToggle">The event that happens when the switch is toggled.</param>
	/// <param name="bounds">The bounds of the switch.</param>
	/// <param name="key">the name of the switch. (Default: null)</param>
	/// <param name="size">The size of the switch (Default: 30)</param>
	/// <param name="padding">The padding around the switch (Default: 5)</param>
	public static GuiComposer AddSwitch(this GuiComposer composer, Action<bool> onToggle, ElementBounds bounds, string key = null, double size = 30.0, double padding = 4.0)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementSwitch(composer.Api, onToggle, bounds, size, padding), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the switch by name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The internal name of the switch.</param>
	/// <returns>Returns the named switch.</returns>
	public static GuiElementSwitch GetSwitch(this GuiComposer composer, string key)
	{
		return (GuiElementSwitch)composer.GetElement(key);
	}

	[Obsolete("Use Method without orientation argument")]
	public static GuiComposer AddButton(this GuiComposer composer, string text, ActionConsumable onClick, ElementBounds bounds, CairoFont buttonFont, EnumButtonStyle style, EnumTextOrientation orientation, string key = null)
	{
		return composer.AddButton(text, onClick, bounds, buttonFont, style, key);
	}

	[Obsolete("Use Method without orientation argument")]
	public static GuiComposer AddButton(this GuiComposer composer, string text, ActionConsumable onClick, ElementBounds bounds, EnumButtonStyle style, EnumTextOrientation orientation, string key = null)
	{
		return composer.AddButton(text, onClick, bounds, style, key);
	}

	[Obsolete("Use Method without orientation argument")]
	public static GuiComposer AddSmallButton(this GuiComposer composer, string text, ActionConsumable onClick, ElementBounds bounds, EnumButtonStyle style, EnumTextOrientation orientation, string key = null)
	{
		return composer.AddSmallButton(text, onClick, bounds, style, key);
	}

	/// <summary>
	/// Adds a clickable button
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="text">The text displayed inside the button</param>
	/// <param name="onClick">Handler for when the button is clicked</param>
	/// <param name="bounds"></param>
	/// <param name="buttonFont">The font to be used for the text inside the button.</param>
	/// <param name="style"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public static GuiComposer AddButton(this GuiComposer composer, string text, ActionConsumable onClick, ElementBounds bounds, CairoFont buttonFont, EnumButtonStyle style = EnumButtonStyle.Normal, string key = null)
	{
		if (!composer.Composed)
		{
			CairoFont hoverFont = buttonFont.Clone().WithColor(GuiStyle.ActiveButtonTextColor);
			GuiElementTextButton elem = new GuiElementTextButton(composer.Api, text, buttonFont, hoverFont, onClick, bounds, style);
			elem.SetOrientation(buttonFont.Orientation);
			composer.AddInteractiveElement(elem, key);
		}
		return composer;
	}

	/// <summary>
	/// Adds a clickable button button with font CairoFont.ButtonText()
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="text">The text displayed inside the button</param>
	/// <param name="onClick">Handler for when the button is clicked</param>
	/// <param name="bounds"></param>
	/// <param name="style"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public static GuiComposer AddButton(this GuiComposer composer, string text, ActionConsumable onClick, ElementBounds bounds, EnumButtonStyle style = EnumButtonStyle.Normal, string key = null)
	{
		if (!composer.Composed)
		{
			GuiElementTextButton elem = new GuiElementTextButton(composer.Api, text, CairoFont.ButtonText(), CairoFont.ButtonPressedText(), onClick, bounds, style);
			elem.SetOrientation(CairoFont.ButtonText().Orientation);
			composer.AddInteractiveElement(elem, key);
		}
		return composer;
	}

	/// <summary>
	/// Adds a small clickable button with font size GuiStyle.SmallFontSize
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="text">The text displayed inside the button</param>
	/// <param name="onClick">Handler for when the button is clicked</param>
	/// <param name="bounds"></param>
	/// <param name="style"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public static GuiComposer AddSmallButton(this GuiComposer composer, string text, ActionConsumable onClick, ElementBounds bounds, EnumButtonStyle style = EnumButtonStyle.Normal, string key = null)
	{
		if (!composer.Composed)
		{
			CairoFont fontstd = CairoFont.SmallButtonText(style);
			CairoFont fontpressed = CairoFont.SmallButtonText(style);
			fontpressed.Color = (double[])GuiStyle.ActiveButtonTextColor.Clone();
			GuiElementTextButton elem = new GuiElementTextButton(composer.Api, text, fontstd, fontpressed, onClick, bounds, style);
			elem.SetOrientation(fontstd.Orientation);
			composer.AddInteractiveElement(elem, key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the button by name
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public static GuiElementTextButton GetButton(this GuiComposer composer, string key)
	{
		return (GuiElementTextButton)composer.GetElement(key);
	}

	/// <summary>
	/// Gets the toggle button by name in the GUIComposer.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the button.</param>
	/// <returns>A button.</returns>
	public static GuiElementToggleButton GetToggleButton(this GuiComposer composer, string key)
	{
		return (GuiElementToggleButton)composer.GetElement(key);
	}

	/// <summary>
	/// Creates a toggle button with the given parameters.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="text">The text of the button.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="onToggle">The event that happens once the button is toggled.</param>
	/// <param name="bounds">The bounding box of the button.</param>
	/// <param name="key">The name of the button for easy access.</param>
	public static GuiComposer AddToggleButton(this GuiComposer composer, string text, CairoFont font, Action<bool> onToggle, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementToggleButton(composer.Api, "", text, font, onToggle, bounds, toggleable: true), key);
		}
		return composer;
	}

	/// <summary>
	/// Adds an icon button.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="icon">The name of the icon.</param>
	/// <param name="onToggle">The event that happens once the button is toggled.</param>
	/// <param name="bounds">The bounding box of the button.</param>
	/// <param name="key">The name of the button for easy access.</param>
	public static GuiComposer AddIconButton(this GuiComposer composer, string icon, Action<bool> onToggle, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementToggleButton(composer.Api, icon, "", CairoFont.WhiteDetailText(), onToggle, bounds), key);
		}
		return composer;
	}

	public static GuiComposer AddIconButton(this GuiComposer composer, string icon, CairoFont font, Action<bool> onToggle, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementToggleButton(composer.Api, icon, "", font, onToggle, bounds), key);
		}
		return composer;
	}

	/// <summary>
	/// Toggles the given button.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the button that was set.</param>
	/// <param name="selectedIndex">the index of the button.</param>
	public static void ToggleButtonsSetValue(this GuiComposer composer, string key, int selectedIndex)
	{
		int i = 0;
		GuiElementToggleButton btn;
		while ((btn = composer.GetToggleButton(key + "-" + i)) != null)
		{
			btn.SetValue(i == selectedIndex);
			i++;
		}
	}

	/// <summary>
	/// Adds multiple buttons with icons.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="icons">The collection of icons for the buttons.</param>
	/// <param name="font">The font for the buttons.</param>
	/// <param name="onToggle">The event called when the buttons are pressed.</param>
	/// <param name="bounds">The bounds of the buttons.</param>
	/// <param name="key">The key given to the bundle of buttons.</param>
	public static GuiComposer AddIconToggleButtons(this GuiComposer composer, string[] icons, CairoFont font, Action<int> onToggle, ElementBounds[] bounds, string key = null)
	{
		if (!composer.Composed)
		{
			int quantityButtons = icons.Length;
			for (int i = 0; i < icons.Length; i++)
			{
				int index = i;
				composer.AddInteractiveElement(new GuiElementToggleButton(composer.Api, icons[i], "", font, delegate(bool on)
				{
					if (on)
					{
						onToggle(index);
						for (int j = 0; j < quantityButtons; j++)
						{
							if (j != index)
							{
								composer.GetToggleButton(key + "-" + j).SetValue(on: false);
							}
						}
					}
					else
					{
						composer.GetToggleButton(key + "-" + index).SetValue(on: true);
					}
				}, bounds[i], toggleable: true), key + "-" + i);
			}
		}
		return composer;
	}

	/// <summary>
	/// Adds multiple buttons with Text.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="texts">The texts on all the buttons.</param>
	/// <param name="font">The font for the buttons</param>
	/// <param name="onToggle">The event fired when the button is pressed.</param>
	/// <param name="bounds">The bounds of the buttons.</param>
	/// <param name="key">The key given to the bundle of buttons.</param>
	public static GuiComposer AddTextToggleButtons(this GuiComposer composer, string[] texts, CairoFont font, Action<int> onToggle, ElementBounds[] bounds, string key = null)
	{
		if (!composer.Composed)
		{
			int quantityButtons = texts.Length;
			for (int i = 0; i < texts.Length; i++)
			{
				int index = i;
				composer.AddInteractiveElement(new GuiElementToggleButton(composer.Api, "", texts[i], font, delegate(bool on)
				{
					if (on)
					{
						onToggle(index);
						for (int j = 0; j < quantityButtons; j++)
						{
							if (j != index)
							{
								composer.GetToggleButton(key + "-" + j).SetValue(on: false);
							}
						}
					}
					else
					{
						composer.GetToggleButton(key + "-" + index).SetValue(on: true);
					}
				}, bounds[i], toggleable: true), key + "-" + i);
			}
		}
		return composer;
	}

	/// <summary>
	/// Adds multiple tabs to a group of vertical tabs.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="tabs">The tabs being added.</param>
	/// <param name="bounds">The boundaries of the tab group.</param>
	/// <param name="onTabClicked">The event fired when any of the tabs are clicked.</param>
	/// <param name="key">The name of this tab group.</param>
	public static GuiComposer AddVerticalToggleTabs(this GuiComposer composer, GuiTab[] tabs, ElementBounds bounds, Action<int, GuiTab> onTabClicked, string key = null)
	{
		if (!composer.Composed)
		{
			CairoFont font = CairoFont.WhiteDetailText().WithFontSize(17f);
			CairoFont selectedFont = CairoFont.WhiteDetailText().WithFontSize(17f).WithColor(GuiStyle.ActiveButtonTextColor);
			GuiElementVerticalTabs tabsElem = new GuiElementVerticalTabs(composer.Api, tabs, font, selectedFont, bounds, onTabClicked);
			tabsElem.ToggleTabs = true;
			composer.AddInteractiveElement(tabsElem, key);
		}
		return composer;
	}

	/// <summary>
	/// Adds multiple tabs to a group of vertical tabs.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="tabs">The tabs being added.</param>
	/// <param name="bounds">The boundaries of the tab group.</param>
	/// <param name="onTabClicked">The event fired when any of the tabs are clicked.</param>
	/// <param name="key">The name of this tab group.</param>
	public static GuiComposer AddVerticalTabs(this GuiComposer composer, GuiTab[] tabs, ElementBounds bounds, Action<int, GuiTab> onTabClicked, string key = null)
	{
		if (!composer.Composed)
		{
			CairoFont font = CairoFont.WhiteDetailText().WithFontSize(17f);
			CairoFont selectedFont = CairoFont.WhiteDetailText().WithFontSize(17f).WithColor(GuiStyle.ActiveButtonTextColor);
			composer.AddInteractiveElement(new GuiElementVerticalTabs(composer.Api, tabs, font, selectedFont, bounds, onTabClicked), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the vertical tab group as declared by name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the vertical tab group to get.</param>
	public static GuiElementVerticalTabs GetVerticalTab(this GuiComposer composer, string key)
	{
		return (GuiElementVerticalTabs)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a List to the current GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the cell.</param>
	/// <param name="cellCreator">the event fired when the cell is requested by the GUI</param>
	/// <param name="cells">The cells of the list.</param>
	/// <param name="key">The identifier for the list.</param>
	public static GuiComposer AddCellList<T>(this GuiComposer composer, ElementBounds bounds, OnRequireCell<T> cellCreator, IEnumerable<T> cells = null, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementCellList<T>(composer.Api, bounds, cellCreator, cells), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the list by name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the list to get.</param>
	/// <returns></returns>
	public static GuiElementCellList<T> GetCellList<T>(this GuiComposer composer, string key)
	{
		return (GuiElementCellList<T>)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a chat input to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the text.</param>
	/// <param name="onTextChanged">The event fired when the text is changed.</param>
	/// <param name="key">The name of this chat component.</param>
	public static GuiComposer AddChatInput(this GuiComposer composer, ElementBounds bounds, Action<string> onTextChanged, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementChatInput(composer.Api, bounds, onTextChanged), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the chat input by name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the chat input component.</param>
	/// <returns>The named component.</returns>
	public static GuiElementChatInput GetChatInput(this GuiComposer composer, string key)
	{
		return (GuiElementChatInput)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a config List to the current GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="items">The items to add.</param>
	/// <param name="onItemClick">The event fired when the item is clicked.</param>
	/// <param name="font">The font of the Config List.</param>
	/// <param name="bounds">The bounds of the config list.</param>
	/// <param name="key">The name of the config list.</param>
	public static GuiComposer AddConfigList(this GuiComposer composer, List<ConfigItem> items, ConfigItemClickDelegate onItemClick, CairoFont font, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			GuiElementConfigList element = new GuiElementConfigList(composer.Api, items, onItemClick, font, bounds);
			composer.AddInteractiveElement(element, key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the config list by name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the config list.</param>
	/// <returns></returns>
	public static GuiElementConfigList GetConfigList(this GuiComposer composer, string key)
	{
		return (GuiElementConfigList)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a container to the current GUI. Can be used to add any gui element within a scrollable window.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the cell.</param>
	/// <param name="key">The identifier for the list.</param>
	public static GuiComposer AddContainer(this GuiComposer composer, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementContainer(composer.Api, bounds), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the container by key
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the list to get.</param>
	/// <returns></returns>
	public static GuiElementContainer GetContainer(this GuiComposer composer, string key)
	{
		return (GuiElementContainer)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a dialog title bar to the GUI.  
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="text">The text of the title bar.</param>
	/// <param name="onClose">The event fired when the title bar is closed.</param>
	/// <param name="font">The font of the title bar.</param>
	/// <param name="bounds">The bounds of the title bar.</param>
	public static GuiComposer AddDialogTitleBar(this GuiComposer composer, string text, Action onClose = null, CairoFont font = null, ElementBounds bounds = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementDialogTitleBar(composer.Api, text, composer, onClose, font, bounds));
		}
		return composer;
	}

	/// <summary>
	/// Adds a dialog title bar to the GUI with a background.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="text">The text of the title bar.</param>
	/// <param name="onClose">The event fired when the title bar is closed.</param>
	/// <param name="font">The font of the title bar.</param>
	/// <param name="bounds">The bounds of the title bar.</param>
	public static GuiComposer AddDialogTitleBarWithBg(this GuiComposer composer, string text, Action onClose = null, CairoFont font = null, ElementBounds bounds = null)
	{
		if (!composer.Composed)
		{
			GuiElementDialogTitleBar elem = new GuiElementDialogTitleBar(composer.Api, text, composer, onClose, font, bounds);
			elem.drawBg = true;
			composer.AddInteractiveElement(elem);
		}
		return composer;
	}

	public static GuiElementDialogTitleBar GetTitleBar(this GuiComposer composer, string key)
	{
		return (GuiElementDialogTitleBar)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a numeric input for the current GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the number input.</param>
	/// <param name="onTextChanged">The event fired when the number is changed.</param>
	/// <param name="font">The font for the numbers.</param>
	/// <param name="key">The name for this GuiElementNumberInput</param>
	public static GuiComposer AddNumberInput(this GuiComposer composer, ElementBounds bounds, Action<string> onTextChanged, CairoFont font = null, string key = null)
	{
		if (font == null)
		{
			font = CairoFont.TextInput();
		}
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementNumberInput(composer.Api, bounds, onTextChanged, font), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the number input by name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The internal name of the numeric input.</param>
	/// <returns>The named numeric input.</returns>
	public static GuiElementNumberInput GetNumberInput(this GuiComposer composer, string key)
	{
		return (GuiElementNumberInput)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a stat bar to the current GUI with a minimum of 0 and a maximum of 100.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the stat bar.</param>
	/// <param name="color">The color of the stat bar.</param>
	/// <param name="hideable">If true, the element can be fully hidden without recompose.</param>
	/// <param name="key">The internal name of the stat bar.</param>
	public static GuiComposer AddStatbar(this GuiComposer composer, ElementBounds bounds, double[] color, bool hideable, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementStatbar(composer.Api, bounds, color, rightToLeft: false, hideable), key);
		}
		return composer;
	}

	public static GuiComposer AddStatbar(this GuiComposer composer, ElementBounds bounds, double[] color, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementStatbar(composer.Api, bounds, color, rightToLeft: false, hideable: false), key);
		}
		return composer;
	}

	/// <summary>
	/// Adds a stat bar with filling in the opposite direction. Default values are from 0 to 100.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">the bounds of the stat bar.</param>
	/// <param name="color">the color of the stat bar.</param>
	/// <param name="key">The internal name of the stat bar.</param>
	public static GuiComposer AddInvStatbar(this GuiComposer composer, ElementBounds bounds, double[] color, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementStatbar(composer.Api, bounds, color, rightToLeft: true, hideable: false), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the stat bar by name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The internal name of the stat bar to fetch.</param>
	/// <returns>The named stat bar.</returns>
	public static GuiElementStatbar GetStatbar(this GuiComposer composer, string key)
	{
		return (GuiElementStatbar)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a text area to the GUI.  
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the Text Area</param>
	/// <param name="onTextChanged">The event fired when the text is changed.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="key">The name of the text area.</param>
	public static GuiComposer AddTextArea(this GuiComposer composer, ElementBounds bounds, Action<string> onTextChanged, CairoFont font = null, string key = null)
	{
		if (font == null)
		{
			font = CairoFont.SmallTextInput();
		}
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementTextArea(composer.Api, bounds, onTextChanged, font), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the text area by name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the text area.</param>
	/// <returns>The named Text Area.</returns>
	public static GuiElementTextArea GetTextArea(this GuiComposer composer, string key)
	{
		return (GuiElementTextArea)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a text input to the current GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the text input.</param>
	/// <param name="onTextChanged">The event fired when the text is changed.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="key">The name of this text component.</param>
	public static GuiComposer AddTextInput(this GuiComposer composer, ElementBounds bounds, Action<string> onTextChanged, CairoFont font = null, string key = null)
	{
		if (font == null)
		{
			font = CairoFont.TextInput();
		}
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementTextInput(composer.Api, bounds, onTextChanged, font), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the text input by input name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the text input to get.</param>
	/// <returns>The named text input</returns>
	public static GuiElementTextInput GetTextInput(this GuiComposer composer, string key)
	{
		return (GuiElementTextInput)composer.GetElement(key);
	}

	/// <summary>
	/// Adds an item slot grid to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="inventory">The inventory attached to the slot grid.</param>
	/// <param name="sendPacket">A handler that should send supplied network packet to the server, if the inventory modifications should be synced</param>
	/// <param name="columns">The number of columns in the slot grid.</param>
	/// <param name="bounds">the bounds of the slot grid.</param>
	/// <param name="key">The key for this particular slot grid.</param>
	public static GuiComposer AddItemSlotGrid(this GuiComposer composer, IInventory inventory, Action<object> sendPacket, int columns, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementItemSlotGrid(composer.Api, inventory, sendPacket, columns, null, bounds), key);
			GuiElementItemSlotGridBase.UpdateLastSlotGridFlag(composer);
		}
		return composer;
	}

	/// <summary>
	/// Adds an item slot grid to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="inventory">The inventory attached to the slot grid.</param>
	/// <param name="sendPacket">A handler that should send supplied network packet to the server, if the inventory modifications should be synced</param>
	/// <param name="columns">The number of columns in the slot grid.</param>
	/// <param name="selectiveSlots">The slots within the inventory that are currently accessible.</param>
	/// <param name="bounds">the bounds of the slot grid.</param>
	/// <param name="key">The key for this particular slot grid.</param>
	public static GuiComposer AddItemSlotGrid(this GuiComposer composer, IInventory inventory, Action<object> sendPacket, int columns, int[] selectiveSlots, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementItemSlotGrid(composer.Api, inventory, sendPacket, columns, selectiveSlots, bounds), key);
			GuiElementItemSlotGridBase.UpdateLastSlotGridFlag(composer);
		}
		return composer;
	}

	/// <summary>
	/// Gets the slot grid by name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the slot grid to get.</param>
	public static GuiElementItemSlotGrid GetSlotGrid(this GuiComposer composer, string key)
	{
		return (GuiElementItemSlotGrid)composer.GetElement(key);
	}

	/// <summary>
	/// Adds an ItemSlotGrid with Exclusions.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="inventory">The attached inventory.</param>
	/// <param name="sendPacket">A handler that should send supplied network packet to the server, if the inventory modifications should be synced</param>
	/// <param name="columns">The number of columns.</param>
	/// <param name="excludingSlots">The slots that have been excluded from the slot grid.</param>
	/// <param name="bounds">The bounds of the slot grid.</param>
	/// <param name="key">The name of the slot grid.</param>
	public static GuiComposer AddItemSlotGridExcl(this GuiComposer composer, IInventory inventory, Action<object> sendPacket, int columns, int[] excludingSlots, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementItemSlotGridExcl(composer.Api, inventory, sendPacket, columns, excludingSlots, bounds), key);
			GuiElementItemSlotGridBase.UpdateLastSlotGridFlag(composer);
		}
		return composer;
	}

	/// <summary>
	/// Gets the ItemSlotGridExcl by name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the ItemSlotGridExcl</param>
	public static GuiElementItemSlotGridExcl GetSlotGridExcl(this GuiComposer composer, string key)
	{
		return (GuiElementItemSlotGridExcl)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a passive item slot to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the Slot</param>
	/// <param name="inventory">The inventory attached to the slot.</param>
	/// <param name="slot">The internal slot of the slot.</param>
	/// <param name="drawBackground">Do we draw the background for this slot? (Default: true)</param>
	public static GuiComposer AddPassiveItemSlot(this GuiComposer composer, ElementBounds bounds, IInventory inventory, ItemSlot slot, bool drawBackground = true)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementPassiveItemSlot(composer.Api, bounds, inventory, slot, drawBackground));
		}
		return composer;
	}

	/// <summary>
	/// Adds a skill item grid to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="skillItems">The items that represent skills.</param>
	/// <param name="columns">the columns in the skill item grid.</param>
	/// <param name="rows">The rows in the skill item grid.</param>
	/// <param name="onSlotClick">The effect when a slot is clicked.</param>
	/// <param name="bounds">The bounds of the item grid.</param>
	/// <param name="key">The name of the item grid to add.</param>
	public static GuiComposer AddSkillItemGrid(this GuiComposer composer, List<SkillItem> skillItems, int columns, int rows, Action<int> onSlotClick, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementSkillItemGrid(composer.Api, skillItems, columns, rows, onSlotClick, bounds), key);
		}
		return composer;
	}

	/// <summary>
	/// Fetches the skill item grid by name
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the skill item grid to get.</param>
	/// <returns>The skill item grid to get.</returns>
	public static GuiElementSkillItemGrid GetSkillItemGrid(this GuiComposer composer, string key)
	{
		return (GuiElementSkillItemGrid)composer.GetElement(key);
	}

	/// <summary>
	/// Adds an embossed text component to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="text">The text of the component.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="bounds">The bounds of the component.</param>
	/// <param name="key">The name of the component.</param>
	public static GuiComposer AddEmbossedText(this GuiComposer composer, string text, CairoFont font, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementEmbossedText(composer.Api, text, font, bounds), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the EmbossedText component by name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the component.</param>
	/// <returns>the named component of the text.</returns>
	public static GuiElementEmbossedText GetEmbossedText(this GuiComposer composer, string key)
	{
		return (GuiElementEmbossedText)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a hover text to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="text">The text of the text.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="width">The width of the text.</param>
	/// <param name="bounds">The bounds of the text.</param>
	/// <param name="key">The name of this hover text component.</param>
	public static GuiComposer AddHoverText(this GuiComposer composer, string text, CairoFont font, int width, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			GuiElementHoverText elem = new GuiElementHoverText(composer.Api, text, font, width, bounds);
			composer.AddInteractiveElement(elem, key);
		}
		return composer;
	}

	public static GuiComposer AddAutoSizeHoverText(this GuiComposer composer, string text, CairoFont font, int width, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			GuiElementHoverText elem = new GuiElementHoverText(composer.Api, text, font, width, bounds);
			elem.SetAutoWidth(on: true);
			composer.AddInteractiveElement(elem, key);
		}
		return composer;
	}

	/// <summary>
	/// Adds a hover text to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="text">The text of the text.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="width">The width of the text.</param>
	/// <param name="bounds">The bounds of the text.</param>
	/// <param name="key">The name of this hover text component.</param>
	public static GuiComposer AddTranspHoverText(this GuiComposer composer, string text, CairoFont font, int width, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			GuiElementHoverText elem = new GuiElementHoverText(composer.Api, text, font, width, bounds, new TextBackground());
			composer.AddInteractiveElement(elem, key);
		}
		return composer;
	}

	/// <summary>
	/// Adds a hover text to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="text">The text of the text.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="width">The width of the text.</param>
	/// <param name="bounds">The bounds of the text.</param>
	/// <param name="background"></param>
	/// <param name="key">The name of this hover text component.</param>
	public static GuiComposer AddHoverText(this GuiComposer composer, string text, CairoFont font, int width, ElementBounds bounds, TextBackground background, string key = null)
	{
		if (!composer.Composed)
		{
			GuiElementHoverText elem = new GuiElementHoverText(composer.Api, text, font, width, bounds, background);
			composer.AddInteractiveElement(elem, key);
		}
		return composer;
	}

	/// <summary>
	/// Fetches the hover text component by name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the text component.</param>
	public static GuiElementHoverText GetHoverText(this GuiComposer composer, string key)
	{
		return (GuiElementHoverText)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a rich text element to the GUI
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="vtmlCode"></param>
	/// <param name="baseFont"></param>
	/// <param name="bounds"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public static GuiComposer AddRichtext(this GuiComposer composer, string vtmlCode, CairoFont baseFont, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementRichtext(composer.Api, VtmlUtil.Richtextify(composer.Api, vtmlCode, baseFont), bounds), key);
		}
		return composer;
	}

	/// <summary>
	/// Adds a rich text element to the GUI
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="vtmlCode"></param>
	/// <param name="baseFont"></param>
	/// <param name="bounds"></param>
	/// <param name="didClickLink"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public static GuiComposer AddRichtext(this GuiComposer composer, string vtmlCode, CairoFont baseFont, ElementBounds bounds, Action<LinkTextComponent> didClickLink, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementRichtext(composer.Api, VtmlUtil.Richtextify(composer.Api, vtmlCode, baseFont, didClickLink), bounds), key);
		}
		return composer;
	}

	/// <summary>
	/// Adds a rich text element to the GUI
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="components"></param>
	/// <param name="bounds"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public static GuiComposer AddRichtext(this GuiComposer composer, RichTextComponentBase[] components, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementRichtext(composer.Api, components, bounds), key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the chat input by name.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the chat input component.</param>
	/// <returns>The named component.</returns>
	public static GuiElementRichtext GetRichtext(this GuiComposer composer, string key)
	{
		return (GuiElementRichtext)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a static custom draw component to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the component.</param>
	/// <param name="onRender">The event fired when the element is drawn.</param>
	public static GuiComposer AddCustomRender(this GuiComposer composer, ElementBounds bounds, RenderDelegateWithBounds onRender)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementCustomRender(composer.Api, bounds, onRender));
		}
		return composer;
	}

	public static GuiElementCustomRender GetCustomRender(this GuiComposer composer, string key)
	{
		return (GuiElementCustomRender)composer.GetElement(key);
	}

	/// <summary>
	/// Adds a static custom draw component to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the component.</param>
	/// <param name="onDraw">The event fired when the element is drawn.</param>
	public static GuiComposer AddStaticCustomDraw(this GuiComposer composer, ElementBounds bounds, DrawDelegateWithBounds onDraw)
	{
		if (!composer.Composed)
		{
			composer.AddStaticElement(new GuiElementCustomDraw(composer.Api, bounds, onDraw));
		}
		return composer;
	}

	/// <summary>
	/// Adds a dynamic custom draw component to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the component.</param>
	/// <param name="onDraw">The event fired when the element is drawn.</param>
	/// <param name="key">The name of the element.</param>
	public static GuiComposer AddDynamicCustomDraw(this GuiComposer composer, ElementBounds bounds, DrawDelegateWithBounds onDraw, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementCustomDraw(composer.Api, bounds, onDraw, interactive: true), key);
		}
		return composer;
	}

	public static GuiElementCustomDraw GetCustomDraw(this GuiComposer composer, string key)
	{
		return (GuiElementCustomDraw)composer.GetElement(key);
	}

	/// <summary>
	/// Adds shaded, slighlty dirt textured background to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds"></param>
	/// <param name="withTitleBar"></param>
	/// <param name="strokeWidth"></param>
	/// <param name="alpha"></param>
	/// <returns></returns>
	public static GuiComposer AddShadedDialogBG(this GuiComposer composer, ElementBounds bounds, bool withTitleBar = true, double strokeWidth = 5.0, float alpha = 0.75f)
	{
		if (!composer.Composed)
		{
			composer.AddStaticElement(new GuiElementDialogBackground(composer.Api, bounds, withTitleBar, strokeWidth, alpha));
		}
		return composer;
	}

	public static GuiComposer AddDialogBG(this GuiComposer composer, ElementBounds bounds, bool withTitleBar = true, float alpha = 1f)
	{
		if (!composer.Composed)
		{
			GuiElementDialogBackground elem = new GuiElementDialogBackground(composer.Api, bounds, withTitleBar, 0.0, alpha);
			elem.Shade = false;
			composer.AddStaticElement(elem);
		}
		return composer;
	}

	/// <summary>
	/// Adds a static text component to the GUI
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="text">The text of the text component.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="bounds">The bounds of the text container.</param>
	/// <param name="key">The name of the component.</param>
	public static GuiComposer AddStaticText(this GuiComposer composer, string text, CairoFont font, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddStaticElement(new GuiElementStaticText(composer.Api, text, font.Orientation, bounds, font), key);
		}
		return composer;
	}

	/// <summary>
	/// Adds a static text component to the GUI
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="text">The text of the text component.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="orientation">The orientation of the text.</param>
	/// <param name="bounds">The bounds of the text container.</param>
	/// <param name="key">The name of the component.</param>
	public static GuiComposer AddStaticText(this GuiComposer composer, string text, CairoFont font, EnumTextOrientation orientation, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddStaticElement(new GuiElementStaticText(composer.Api, text, orientation, bounds, font), key);
		}
		return composer;
	}

	/// <summary>
	/// Adds a static text component to the GUI that automatically resizes as necessary.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="text">The text of the text component.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="orientation">The orientation of the text.</param>
	/// <param name="bounds">The bounds of the text container.</param>
	/// <param name="key">The name of the component.</param>
	public static GuiComposer AddStaticTextAutoBoxSize(this GuiComposer composer, string text, CairoFont font, EnumTextOrientation orientation, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			GuiElementStaticText elem = new GuiElementStaticText(composer.Api, text, orientation, bounds, font);
			composer.AddStaticElement(elem, key);
			elem.AutoBoxSize();
		}
		return composer;
	}

	/// <summary>
	/// Adds a static text component to the GUI that automatically resizes as necessary.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="text">The text of the text component.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="bounds">The bounds of the text container.</param>
	/// <param name="key">The name of the component.</param>
	public static GuiComposer AddStaticTextAutoFontSize(this GuiComposer composer, string text, CairoFont font, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			GuiElementStaticText elem = new GuiElementStaticText(composer.Api, text, font.Orientation, bounds, font);
			composer.AddStaticElement(elem, key);
			elem.AutoFontSize();
		}
		return composer;
	}

	/// <summary>
	/// Gets the static text component by name.
	/// </summary>
	/// <param name="composer" />
	/// <param name="key">The name of the component.</param>
	public static GuiElementStaticText GetStaticText(this GuiComposer composer, string key)
	{
		return (GuiElementStaticText)composer.GetElement(key);
	}
}
