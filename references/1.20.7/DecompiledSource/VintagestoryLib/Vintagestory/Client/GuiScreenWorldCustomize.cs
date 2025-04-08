using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class GuiScreenWorldCustomize : GuiScreen
{
	private ElementBounds listBounds;

	private ElementBounds clippingBounds;

	private Dictionary<string, List<GuiElement>> elementsByCategory = new Dictionary<string, List<GuiElement>>();

	private List<GuiElement> allInputElements = new List<GuiElement>();

	private GuiTab[] tabs;

	private List<string> categories = new List<string>();

	private Action<bool> didApply;

	private GuiElementContainer container;

	public WorldConfig wcu;

	private List<PlaystyleListEntry> cells;

	public GuiScreenWorldCustomize(Action<bool> didApply, ScreenManager screenManager, GuiScreen parentScreen, WorldConfig wcu, List<PlaystyleListEntry> playstyles)
		: base(screenManager, parentScreen)
	{
		this.wcu = wcu;
		if (playstyles == null)
		{
			loadPlaystyleCells();
		}
		this.didApply = didApply;
		ShowMainMenu = true;
		screenManager.GamePlatform.WindowResized += delegate
		{
			invalidate();
		};
		ClientSettings.Inst.AddWatcher<float>("guiScale", delegate
		{
			invalidate();
		});
	}

	public override void OnScreenLoaded()
	{
		base.OnScreenLoaded();
		InitGui();
	}

	private void invalidate()
	{
		if (base.IsOpened)
		{
			InitGui();
		}
		else
		{
			ScreenManager.GuiComposers.Dispose("mainmenu-singleplayercustomize");
		}
	}

	private void InitGui()
	{
		cells = loadPlaystyleCells();
		tabs = loadTabs();
		double windowWidth = (float)ScreenManager.GamePlatform.WindowSize.Width / RuntimeEnv.GUIScale;
		double windowHeight = (float)ScreenManager.GamePlatform.WindowSize.Height / RuntimeEnv.GUIScale;
		ElementBounds buttonBounds = ElementBounds.FixedSize(60.0, 25.0).WithFixedPadding(10.0, 0.0);
		ElementBounds titleBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 35.0);
		double width = Math.Max(400.0, windowWidth * 0.5) + 40.0;
		ElementBounds leftColumn = ElementBounds.Fixed(0.0, 0.0, 300.0, 30.0);
		ElementBounds rightColumn = ElementBounds.Fixed(0.0, 0.0, 300.0, 30.0).FixedRightOf(leftColumn);
		ElementBounds tabBounds = ElementBounds.Fixed(0.0, 185.0, 800.0, 20.0);
		string[] pvalues = cells.Select((PlaystyleListEntry c) => c.PlayStyle.Code).ToArray();
		string[] pnames = cells.Select((PlaystyleListEntry c) => c.Title).ToArray();
		int selectedIndex = cells.FindIndex((PlaystyleListEntry c) => c.PlayStyle.Code == wcu.CurrentPlayStyle.Code);
		double insetWidth = Math.Max(400.0, windowWidth * 0.5);
		double insetHeight = Math.Max(300.0, windowHeight - 160.0);
		ElementBounds insetBounds;
		ElementComposer = dialogBase("mainmenu-singleplayercustomize").AddStaticText(Lang.Get("singleplayer-customize"), CairoFont.WhiteSmallishText(), titleBounds).AddStaticText(Lang.Get("singleplayer-playstyle"), CairoFont.WhiteSmallishText(), leftColumn = leftColumn.BelowCopy(0.0, 12.0)).AddDropDown(pvalues, pnames, selectedIndex, onPlayStyleChanged, rightColumn = rightColumn.BelowCopy(0.0, 12.0), "playstyleDropDown")
			.AddStaticText(Lang.Get("singleplayer-worldheight"), CairoFont.WhiteSmallishText(), leftColumn = leftColumn.BelowCopy(0.0, 12.0))
			.AddSlider(onNewWorldHeightValue, rightColumn = rightColumn.BelowCopy(0.0, 15.0).FixedRightOf(leftColumn).WithFixedSize(300.0, 20.0), "worldHeight")
			.AddStaticText(Lang.Get("singleplayer-seed"), CairoFont.WhiteSmallishText(), leftColumn = leftColumn.BelowCopy(0.0, 11.0))
			.AddTextInput(rightColumn = rightColumn.BelowCopy(0.0, 18.0).WithFixedHeight(30.0), null, null, "worldseed")
			.AddIf(!wcu.IsNewWorld)
			.AddRichtext("<font opacity=\"0.6\">" + Lang.Get("singleplayer-disabledcustomizations") + "</font>", CairoFont.WhiteDetailText(), leftColumn = leftColumn.BelowCopy(0.0, 23.0).WithFixedWidth(600.0))
			.Execute(delegate
			{
				rightColumn = rightColumn.BelowCopy(0.0, 18.0);
			})
			.EndIf()
			.AddHorizontalTabs(tabs, tabBounds, onTabClicked, CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold), CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold).WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
			.AddInset(insetBounds = leftColumn.BelowCopy(0.0, -3.0).WithFixedSize(insetWidth, insetHeight - leftColumn.fixedY - leftColumn.fixedHeight))
			.AddVerticalScrollbar(OnNewScrollbarvalue, ElementStdBounds.VerticalScrollbar(insetBounds), "scrollbar")
			.BeginClip(clippingBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
			.AddContainer(listBounds = clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(10.0), "configlist");
		container = ElementComposer.GetContainer("configlist");
		int i = 0;
		double size = 26.0;
		ElementBounds leftColumni = ElementBounds.Fixed(0.0, 2.0, 300.0, size);
		ElementBounds rightColumni = ElementBounds.Fixed(0.0, 0.0, (int)GameMath.Clamp(width - 370.0, 125.0, 500.0), size).FixedRightOf(leftColumni);
		leftColumni = leftColumni.FlatCopy();
		rightColumni = rightColumni.FlatCopy();
		elementsByCategory.Clear();
		allInputElements.Clear();
		Dictionary<string, List<GuiElement>> hoverElementsByCat = new Dictionary<string, List<GuiElement>>();
		foreach (ModContainer verifiedMod in ScreenManager.verifiedMods)
		{
			ModWorldConfiguration config = verifiedMod.WorldConfig;
			if (config == null)
			{
				continue;
			}
			WorldConfigurationAttribute[] worldConfigAttributes = config.WorldConfigAttributes;
			foreach (WorldConfigurationAttribute attribute in worldConfigAttributes)
			{
				if (!attribute.OnCustomizeScreen)
				{
					continue;
				}
				if (!elementsByCategory.TryGetValue(attribute.Category, out var elements))
				{
					elements = (elementsByCategory[attribute.Category] = new List<GuiElement>());
					leftColumni = ElementBounds.Fixed(0.0, 2.0, 300.0, size);
					rightColumni = ElementBounds.Fixed(0.0, 0.0, (int)GameMath.Clamp(width - 370.0, 125.0, 500.0), size).FixedRightOf(leftColumni);
					leftColumni = leftColumni.FlatCopy();
					rightColumni = rightColumni.FlatCopy();
				}
				if (!hoverElementsByCat.TryGetValue(attribute.Category, out var hoverElements))
				{
					hoverElements = (hoverElementsByCat[attribute.Category] = new List<GuiElement>());
				}
				bool enabled = wcu.IsNewWorld || !attribute.OnlyDuringWorldCreate;
				string key = "elem-" + i;
				WorldConfigurationValue value = wcu[attribute.Code];
				object defaultValue = attribute.TypedDefault;
				GuiElementControl elem = null;
				switch (attribute.DataType)
				{
				case EnumDataType.Bool:
				{
					GuiElementSwitch switchElem = new GuiElementSwitch(ScreenManager.api, null, rightColumni.FlatCopy(), size);
					elem = switchElem;
					bool on = (bool)defaultValue;
					if (value != null)
					{
						on = (bool)value.Value;
					}
					switchElem.SetValue(on);
					elements.Add(switchElem);
					break;
				}
				case EnumDataType.DoubleInput:
				{
					ElementComposer.AddNumberInput(rightColumni.FlatCopy(), null, CairoFont.WhiteDetailText(), key);
					float val5 = (float)defaultValue;
					if (value != null)
					{
						val5 = (float)value.Value;
					}
					elem = ElementComposer.GetNumberInput(key);
					ElementComposer.GetNumberInput(key).SetValue(val5);
					break;
				}
				case EnumDataType.IntInput:
				{
					ElementComposer.AddNumberInput(rightColumni.FlatCopy(), null, CairoFont.WhiteDetailText(), key);
					int val6 = (int)defaultValue;
					if (value != null)
					{
						val6 = (int)value.Value;
					}
					elem = ElementComposer.GetNumberInput(key);
					ElementComposer.GetNumberInput(key).SetValue(val6);
					break;
				}
				case EnumDataType.IntRange:
				{
					ElementComposer.AddSlider(null, rightColumni.FlatCopy(), key);
					int val4 = (int)defaultValue;
					if (value != null)
					{
						val4 = (int)value.Value;
					}
					elem = ElementComposer.GetSlider(key);
					ElementComposer.GetSlider(key).SetValues(val4, (int)attribute.Min, (int)attribute.Max, (int)attribute.Step);
					break;
				}
				case EnumDataType.DropDown:
				{
					string val2 = (string)defaultValue;
					if (value != null)
					{
						val2 = (string)value.Value;
					}
					int selindex = attribute.Values.IndexOf(val2);
					string[] values = attribute.Values;
					string[] names = new string[attribute.Names.Length];
					for (int j = 0; j < values.Length; j++)
					{
						string langkey = "worldconfig-" + attribute.Code + "-" + attribute.Names[j];
						if (ClientSettings.DeveloperMode && !Lang.HasTranslation(langkey))
						{
							Console.WriteLine("\"{0}\": \"{1}\",", langkey, attribute.Names[j]);
						}
						names[j] = Lang.Get(langkey);
					}
					if (selindex < 0)
					{
						values = values.Append(val2);
						names = names.Append(val2);
						selindex = names.Length - 1;
					}
					GuiElementDropDown dropElem = new GuiElementDropDown(ScreenManager.api, values, names, selindex, null, rightColumni.FlatCopy(), CairoFont.WhiteSmallText(), multiSelect: false);
					elem = dropElem;
					elements.Add(dropElem);
					break;
				}
				case EnumDataType.String:
				{
					ElementComposer.AddTextInput(rightColumni, null, CairoFont.WhiteDetailText(), key);
					string val3 = (string)defaultValue;
					if (value != null)
					{
						val3 = (string)value.Value;
					}
					elem = ElementComposer.GetTextInput(key);
					ElementComposer.GetTextInput(key).SetValue(val3);
					break;
				}
				}
				elem.Enabled = enabled;
				elements.Add(elem);
				allInputElements.Add(elem);
				CairoFont font = CairoFont.WhiteSmallText();
				elements.Add(new GuiElementStaticText(ScreenManager.api, Lang.Get("worldattribute-" + attribute.Code), EnumTextOrientation.Left, leftColumni, font));
				string tooltip = Lang.GetIfExists("worldattribute-" + attribute.Code + "-desc");
				if (tooltip != null)
				{
					ElementBounds hbounds = leftColumni.FlatCopy();
					hbounds.fixedWidth -= 50.0;
					GuiElementHoverText hoverelem = new GuiElementHoverText(ScreenManager.api, tooltip, CairoFont.WhiteSmallText(), 320, hbounds);
					hoverElements.Add(hoverelem);
				}
				leftColumni = leftColumni.BelowCopy(0.0, 9.9);
				rightColumni = rightColumni.BelowCopy(0.0, 10.0);
				i++;
			}
		}
		foreach (KeyValuePair<string, List<GuiElement>> val in elementsByCategory)
		{
			if (hoverElementsByCat.TryGetValue(val.Key, out var hoverEles))
			{
				foreach (GuiElement hoverEle in hoverEles)
				{
					val.Value.Add(hoverEle);
				}
			}
			val.Value.Add(new GuiElementStaticText(ScreenManager.api, " ", EnumTextOrientation.Left, leftColumni = leftColumni.BelowCopy(), CairoFont.WhiteDetailText()));
		}
		updateWorldHeightSlider();
		ElementComposer.EndClip().AddButton(Lang.Get("general-back"), OnBack, buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0)).AddButton(Lang.Get("general-apply"), OnApply, buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0).WithFixedWidth(200.0)
			.WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedAlignmentOffset(-13.0, 0.0))
			.EndChildElements()
			.Compose();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
		selectTab(0);
		ElementComposer.GetTextInput("worldseed").SetValue(wcu.Seed);
	}

	private GuiTab[] loadTabs()
	{
		List<GuiTab> tabs = new List<GuiTab>();
		categories.Clear();
		int i = 0;
		foreach (ModContainer verifiedMod in ScreenManager.verifiedMods)
		{
			ModWorldConfiguration config = verifiedMod.WorldConfig;
			if (config == null)
			{
				continue;
			}
			WorldConfigurationAttribute[] worldConfigAttributes = config.WorldConfigAttributes;
			foreach (WorldConfigurationAttribute attribute in worldConfigAttributes)
			{
				if (attribute.OnCustomizeScreen && !categories.Contains(attribute.Category))
				{
					categories.Add(attribute.Category);
					tabs.Add(new GuiTab
					{
						Name = Lang.Get("worldconfig-category-" + attribute.Category),
						DataInt = i++
					});
				}
			}
		}
		return tabs.ToArray();
	}

	private void onTabClicked(int dataint)
	{
		selectTab(dataint);
	}

	private void selectTab(int tabIndex)
	{
		string cat = categories[tabIndex];
		container.Clear();
		foreach (GuiElement ele in elementsByCategory[cat])
		{
			container.Add(ele);
		}
		ElementComposer.ReCompose();
		listBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
		updateWorldHeightSlider();
	}

	private void setFieldValues()
	{
		int i = 0;
		foreach (ModContainer verifiedMod in ScreenManager.verifiedMods)
		{
			ModWorldConfiguration config = verifiedMod.WorldConfig;
			if (config == null)
			{
				continue;
			}
			WorldConfigurationAttribute[] worldConfigAttributes = config.WorldConfigAttributes;
			foreach (WorldConfigurationAttribute attribute in worldConfigAttributes)
			{
				if (!attribute.OnCustomizeScreen)
				{
					continue;
				}
				if (!wcu.IsNewWorld && attribute.OnlyDuringWorldCreate)
				{
					i++;
					continue;
				}
				string key = "elem-" + i;
				WorldConfigurationValue value = wcu[attribute.Code];
				object defaultValue = attribute.TypedDefault;
				GuiElement elem = allInputElements[i];
				switch (attribute.DataType)
				{
				case EnumDataType.Bool:
				{
					GuiElementSwitch obj = elem as GuiElementSwitch;
					bool on = (bool)defaultValue;
					if (value != null)
					{
						on = (bool)value.Value;
					}
					obj.SetValue(on);
					break;
				}
				case EnumDataType.DoubleInput:
				{
					float val4 = (float)defaultValue;
					if (value != null)
					{
						val4 = (float)value.Value;
					}
					ElementComposer.GetNumberInput(key).SetValue(val4);
					break;
				}
				case EnumDataType.IntInput:
				{
					int val5 = (int)defaultValue;
					if (value != null)
					{
						val5 = (int)value.Value;
					}
					ElementComposer.GetNumberInput(key).SetValue(val5);
					break;
				}
				case EnumDataType.IntRange:
				{
					int val3 = (int)defaultValue;
					if (value != null)
					{
						val3 = (int)value.Value;
					}
					ElementComposer.GetSlider(key).SetValues(val3, (int)attribute.Min, (int)attribute.Max, (int)attribute.Step);
					break;
				}
				case EnumDataType.DropDown:
				{
					string val = (string)defaultValue;
					if (value != null)
					{
						val = (string)value.Value;
					}
					int selindex = attribute.Values.IndexOf(val);
					(elem as GuiElementDropDown).SetSelectedIndex(selindex);
					break;
				}
				case EnumDataType.String:
				{
					string val2 = (string)defaultValue;
					if (value != null)
					{
						val2 = (string)value.Value;
					}
					ElementComposer.GetTextInput(key).SetValue(val2);
					break;
				}
				}
				i++;
			}
		}
	}

	private void onPlayStyleChanged(string code, bool selected)
	{
		updateWorldHeightSlider();
		wcu.selectPlayStyle(code);
		setFieldValues();
	}

	private void updateWorldHeightSlider()
	{
		if (wcu.CurrentPlayStyle.Code != "creativebuilding")
		{
			ElementComposer.GetSlider("worldHeight").SetValues(wcu.MapsizeY, 128, 512, 64, " blocks");
			ElementComposer.GetSlider("worldHeight").SetAlarmValue(384);
			ElementComposer.GetSlider("worldHeight").OnSliderTooltip = (int value) => Lang.Get("createworld-worldheight", value) + ((value > 384) ? ("\n" + Lang.Get("createworld-worldheight-warning")) : "");
		}
		else
		{
			ElementComposer.GetSlider("worldHeight").SetValues(wcu.MapsizeY, 128, 2048, 64, " blocks");
			ElementComposer.GetSlider("worldHeight").SetAlarmValue(1024);
			ElementComposer.GetSlider("worldHeight").OnSliderTooltip = (int value) => Lang.Get("createworld-worldheight", value) + ((value > 1024) ? ("\n" + Lang.Get("createworld-worldheight-warning")) : "");
		}
	}

	private List<PlaystyleListEntry> loadPlaystyleCells()
	{
		cells = new List<PlaystyleListEntry>();
		wcu.LoadPlayStyles();
		foreach (PlayStyle ps in wcu.PlayStyles)
		{
			cells.Add(new PlaystyleListEntry
			{
				Title = Lang.Get("playstyle-" + ps.LangCode),
				PlayStyle = ps
			});
		}
		if (cells.Count == 0)
		{
			cells.Add(new PlaystyleListEntry
			{
				Title = Lang.Get("noplaystyles-title"),
				DetailText = Lang.Get("noplaystyles-desc"),
				PlayStyle = null,
				Enabled = false
			});
		}
		return cells;
	}

	private bool onNewWorldHeightValue(int value)
	{
		wcu.MapsizeY = value;
		return true;
	}

	public override void OnKeyDown(KeyEvent e)
	{
		base.OnKeyDown(e);
		if (!e.CtrlPressed || e.KeyCode != 104)
		{
			return;
		}
		try
		{
			string json = ScreenManager.Platform.XPlatInterface.GetClipboardText();
			if (json.StartsWith("{"))
			{
				wcu.FromJson(json);
				ScreenManager.GamePlatform.Logger.Notification("Pasted world config loaded!");
				updateWorldHeightSlider();
				setFieldValues();
			}
		}
		catch (Exception ex)
		{
			ScreenManager.GamePlatform.Logger.Warning("Unable to load pasted world config:");
			ScreenManager.GamePlatform.Logger.Warning(ex);
		}
	}

	private bool OnApply()
	{
		wcu.Seed = ElementComposer.GetTextInput("worldseed").GetText();
		wcu.MapsizeY = ElementComposer.GetSlider("worldHeight").GetValue();
		int i = 0;
		wcu.WorldConfigsCustom.Clear();
		foreach (ModContainer verifiedMod in ScreenManager.verifiedMods)
		{
			ModWorldConfiguration config = verifiedMod.WorldConfig;
			if (config == null)
			{
				continue;
			}
			WorldConfigurationAttribute[] worldConfigAttributes = config.WorldConfigAttributes;
			foreach (WorldConfigurationAttribute attribute in worldConfigAttributes)
			{
				if (attribute.OnCustomizeScreen)
				{
					GuiElement elem = allInputElements[i];
					WorldConfigurationValue value = new WorldConfigurationValue();
					value.Attribute = attribute;
					value.Code = attribute.Code;
					switch (attribute.DataType)
					{
					case EnumDataType.Bool:
					{
						GuiElementSwitch switchElem = elem as GuiElementSwitch;
						value.Value = switchElem.On;
						break;
					}
					case EnumDataType.IntInput:
					case EnumDataType.DoubleInput:
					{
						GuiElementNumberInput numInput = elem as GuiElementNumberInput;
						value.Value = numInput.GetValue();
						break;
					}
					case EnumDataType.IntRange:
					{
						GuiElementSlider slider = elem as GuiElementSlider;
						value.Value = slider.GetValue();
						break;
					}
					case EnumDataType.DropDown:
					{
						GuiElementDropDown dropDown = elem as GuiElementDropDown;
						value.Value = dropDown.SelectedValue;
						break;
					}
					case EnumDataType.String:
					{
						GuiElementTextInput textInput = elem as GuiElementTextInput;
						value.Value = textInput.GetText();
						break;
					}
					}
					wcu.WorldConfigsCustom.Add(value.Code, value);
					i++;
				}
			}
		}
		wcu.updateJWorldConfig();
		didApply(obj: true);
		return true;
	}

	private bool OnBack()
	{
		didApply(obj: false);
		return true;
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = ElementComposer.GetContainer("configlist").Bounds;
		bounds.fixedY = 0f - value;
		bounds.CalcWorldBounds();
		container = ElementComposer.GetContainer("configlist");
		foreach (GuiElement element in container.Elements)
		{
			if (element is GuiElementDropDown gelemd && gelemd.listMenu.IsOpened)
			{
				gelemd.listMenu.Close();
			}
		}
	}
}
