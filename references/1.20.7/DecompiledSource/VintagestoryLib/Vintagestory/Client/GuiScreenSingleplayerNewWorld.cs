using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class GuiScreenSingleplayerNewWorld : GuiScreen
{
	private int selectedPlaystyleIndex;

	protected static bool allowCheats = true;

	protected ElementBounds listBounds;

	protected ElementBounds clippingBounds;

	internal List<PlaystyleListEntry> cells = new List<PlaystyleListEntry>();

	private bool isCustomWorldName;

	private WorldConfig wcu;

	private GuiScreenWorldCustomize customizeScreen;

	private Random rand = new Random();

	public GuiScreenSingleplayerNewWorld(ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		ShowMainMenu = true;
		wcu = new WorldConfig(screenManager.verifiedMods);
		wcu.IsNewWorld = true;
		InitGui();
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
			ScreenManager.GuiComposers.Dispose("mainmenu-singleplayernewworld");
		}
	}

	private void InitGui()
	{
		wcu.mods = ScreenManager.verifiedMods;
		wcu.LoadPlayStyles();
		cells.Clear();
		cells = loadPlaystyleCells();
		if (wcu.PlayStyles.Count > 0)
		{
			cells[0].Selected = true;
			wcu.selectPlayStyle(selectedPlaystyleIndex);
		}
		int windowWidth = ScreenManager.GamePlatform.WindowSize.Width;
		int windowHeight = ScreenManager.GamePlatform.WindowSize.Height;
		ElementBounds leftColumn = ElementBounds.Fixed(0.0, 0.0, 300.0, 30.0);
		ElementBounds rightColumn = ElementBounds.Fixed(0.0, 0.0, 300.0, 30.0).FixedRightOf(leftColumn);
		ElementBounds insetBounds = null;
		double width = Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale + 40.0;
		ElementComposer = dialogBase("mainmenu-singleplayernewworld").AddStaticText(Lang.Get("singleplayer-newworld"), CairoFont.WhiteSmallishText(), leftColumn.FlatCopy()).AddStaticText(Lang.Get("singleplayer-newworldname"), CairoFont.WhiteSmallishText(), leftColumn = leftColumn.BelowCopy(0.0, 13.0)).AddTextInput(rightColumn = rightColumn.BelowCopy(0.0, 10.0).WithFixedWidth(270.0), null, null, "worldname")
			.AddIconButton("dice", OnPressDice, rightColumn = rightColumn.FlatCopy().FixedRightOf(rightColumn).WithFixedSize(30.0, 30.0))
			.AddStaticText(Lang.Get("singleplayer-selectplaystyle"), CairoFont.WhiteSmallishText(), leftColumn = leftColumn.BelowCopy(0.0, 13.0))
			.AddInset(insetBounds = leftColumn.BelowCopy(0.0, 3.0).WithFixedSize(width - (double)GuiElementScrollbar.DefaultScrollbarWidth - (double)GuiElementScrollbar.DeafultScrollbarPadding - 3.0, (double)((float)Math.Max(300, windowHeight) / ClientSettings.GUIScale - 170f) - leftColumn.fixedY - leftColumn.fixedHeight))
			.AddVerticalScrollbar(OnNewScrollbarvalue, ElementStdBounds.VerticalScrollbar(insetBounds), "scrollbar")
			.BeginClip(clippingBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
			.AddCellList(listBounds = clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(10.0), createCellElem, cells, "playstylelist")
			.EndClip();
		GuiElementCellList<PlaystyleListEntry> cellListElem = ElementComposer.GetCellList<PlaystyleListEntry>("playstylelist");
		cellListElem.BeforeCalcBounds();
		for (int k = 0; k < cells.Count; k++)
		{
			ElementBounds bounds = cellListElem.elementCells[k].Bounds;
			ElementComposer.AddHoverText(cells[k].HoverText, CairoFont.WhiteDetailText(), 320, bounds, "hovertext-" + k);
			ElementComposer.GetHoverText("hovertext-" + k).InsideClipBounds = clippingBounds;
		}
		ElementComposer.AddButton(Lang.Get("general-back"), OnBack, leftColumn = insetBounds.BelowCopy(0.0, 10.0).WithFixedSize(100.0, 30.0).WithFixedPadding(5.0, 0.0)).AddButton(Lang.Get("general-customize"), OnCustomize, leftColumn = leftColumn.FlatCopy().WithFixedWidth(200.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedAlignmentOffset(-220.0, 0.0)).AddButton(Lang.Get("general-createworld"), OnCreate, leftColumn = leftColumn.FlatCopy().WithFixedWidth(200.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedAlignmentOffset(0.0, 0.0))
			.EndChildElements()
			.Compose();
		ElementComposer.GetTextInput("worldname").OnKeyPressed = delegate
		{
			isCustomWorldName = true;
		};
		listBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
		updatePlaysStyleSpecificFields();
		if (selectedPlaystyleIndex >= 0)
		{
			for (int j = 0; j < cells.Count; j++)
			{
				cells[j].Selected = false;
			}
			cells[selectedPlaystyleIndex].Selected = true;
		}
		for (int i = 0; i < cells.Count; i++)
		{
			string hoverText = ((selectedPlaystyleIndex != i) ? wcu.ToRichText(wcu.PlayStyles[i], withCustomConfigs: false) : wcu.ToRichText(withCustomConfigs: true));
			ElementComposer.GetHoverText("hovertext-" + i).SetNewText(hoverText);
		}
	}

	private IGuiElementCell createCellElem(SavegameCellEntry cell, ElementBounds bounds)
	{
		return new GuiElementMainMenuCell(ScreenManager.api, cell, bounds)
		{
			ShowModifyIcons = false,
			cellEntry = 
			{
				DetailTextOffY = 4.0
			},
			OnMouseDownOnCellLeft = OnClickCellLeft
		};
	}

	private void updatePlaysStyleSpecificFields()
	{
		if (!isCustomWorldName)
		{
			ElementComposer.GetTextInput("worldname").SetValue((wcu.CurrentPlayStyle?.Code == "creativebuilding") ? GenRandomCreativeName() : GenRandomSurvivalName());
		}
		if (!isCustomWorldName)
		{
			ElementComposer.GetTextInput("worldname").SetValue((wcu.CurrentPlayStyle?.Code == "creativebuilding") ? GenRandomCreativeName() : GenRandomSurvivalName());
		}
	}

	private List<PlaystyleListEntry> loadPlaystyleCells()
	{
		CairoFont font = CairoFont.WhiteDetailText();
		font.WithFontSize((float)GuiStyle.SmallFontSize);
		foreach (PlayStyle ps2 in wcu.PlayStyles)
		{
			cells.Add(new PlaystyleListEntry
			{
				Title = Lang.Get("playstyle-" + ps2.LangCode),
				DetailText = Lang.Get("playstyle-desc-" + ps2.LangCode),
				PlayStyle = ps2,
				DetailTextFont = font,
				HoverText = ""
			});
		}
		if (cells.Count == 0)
		{
			PlayStyle ps = new PlayStyle
			{
				Code = "default",
				LangCode = "default",
				WorldConfig = new JsonObject(JToken.Parse("{}")),
				WorldType = "none"
			};
			wcu.PlayStyles.Add(ps);
			wcu.selectPlayStyle(0);
			cells.Add(new PlaystyleListEntry
			{
				Title = Lang.Get("noplaystyles-title"),
				DetailText = Lang.Get("noplaystyles-desc"),
				PlayStyle = ps,
				DetailTextFont = font,
				Enabled = true
			});
		}
		return cells;
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = ElementComposer.GetCellList<PlaystyleListEntry>("playstylelist").Bounds;
		bounds.fixedY = 0f - value;
		bounds.CalcWorldBounds();
	}

	private void OnPressDice(bool on)
	{
		ElementComposer.GetTextInput("worldname").SetValue((wcu.CurrentPlayStyle.Code == "creative") ? GenRandomCreativeName() : GenRandomSurvivalName());
		isCustomWorldName = false;
	}

	internal void OnClickCellLeft(int cellIndex)
	{
		wcu.selectPlayStyle(cellIndex);
		foreach (PlaystyleListEntry cell in cells)
		{
			cell.Selected = false;
		}
		cells[cellIndex].Selected = !cells[cellIndex].Selected;
		updatePlaysStyleSpecificFields();
		selectedPlaystyleIndex = cellIndex;
		for (int i = 0; i < cells.Count; i++)
		{
			string hoverText = ((selectedPlaystyleIndex != i) ? wcu.ToRichText(wcu.PlayStyles[i], withCustomConfigs: false) : wcu.ToRichText(withCustomConfigs: true));
			ElementComposer.GetHoverText("hovertext-" + i).SetNewText(hoverText);
		}
	}

	public bool OnCustomize()
	{
		if (wcu.CurrentPlayStyle == null)
		{
			return false;
		}
		customizeScreen = new GuiScreenWorldCustomize(OnReturnFromCustomizer, ScreenManager, this, wcu.Clone(), cells);
		ScreenManager.LoadScreen(customizeScreen);
		return true;
	}

	private void OnReturnFromCustomizer(bool didApply)
	{
		if (didApply)
		{
			wcu = customizeScreen.wcu;
		}
		string worldName = ElementComposer.GetTextInput("worldname").GetText();
		ScreenManager.LoadScreen(this);
		ElementComposer.GetTextInput("worldname").SetValue(worldName);
	}

	private bool OnCreate()
	{
		if (wcu.CurrentPlayStyle.Code == "creativebuilding")
		{
			if (wcu.MapsizeY > 1024)
			{
				string text = Lang.Get("createworld-creativebuilding-warning-largeworldheight", wcu.MapsizeY);
				ScreenManager.LoadScreen(new GuiScreenConfirmAction(text, OnDidConfirmCreate, ScreenManager, this));
				return true;
			}
		}
		else
		{
			if (wcu.MapsizeY > 384)
			{
				string text2 = Lang.Get("createworld-surviveandbuild-warning-largeworldheight", wcu.MapsizeY);
				ScreenManager.LoadScreen(new GuiScreenConfirmAction(text2, OnDidConfirmCreate, ScreenManager, this));
				return true;
			}
			if (wcu.MapsizeY < 256)
			{
				string text2 = Lang.Get("createworld-surviveandbuild-warning-smallworldheight", wcu.MapsizeY);
				ScreenManager.LoadScreen(new GuiScreenConfirmAction(text2, OnDidConfirmCreate, ScreenManager, this));
				return true;
			}
		}
		CreateWorld();
		return true;
	}

	private void OnDidConfirmCreate(bool confirm)
	{
		if (confirm)
		{
			CreateWorld();
		}
		else
		{
			ScreenManager.LoadScreen(this);
		}
	}

	private void CreateWorld()
	{
		string worldname = ElementComposer.GetTextInput("worldname").GetText();
		if (string.IsNullOrWhiteSpace(worldname))
		{
			worldname = ((wcu.CurrentPlayStyle?.Code == "creativebuilding") ? GenRandomCreativeName() : GenRandomSurvivalName());
		}
		string basefilename = Regex.Replace(worldname.ToLowerInvariant(), "[^\\w\\d0-9_\\- ]+", "");
		string filename = basefilename;
		int i = 2;
		while (File.Exists(Path.Combine(GamePaths.Saves, filename) + ".vcdbs"))
		{
			filename = basefilename + "-" + i;
			i++;
		}
		PlayStyle playstyle = wcu.CurrentPlayStyle;
		StartServerArgs args = new StartServerArgs
		{
			AllowCreativeMode = allowCheats,
			PlayStyle = playstyle.Code,
			PlayStyleLangCode = playstyle.LangCode,
			WorldType = playstyle.WorldType,
			WorldName = worldname,
			WorldConfiguration = wcu.Jworldconfig,
			SaveFileLocation = Path.Combine(GamePaths.Saves, filename) + ".vcdbs",
			Seed = wcu.Seed,
			MapSizeY = wcu.MapsizeY,
			CreatedByPlayerName = ClientSettings.PlayerName,
			DisabledMods = ClientSettings.DisabledMods,
			Language = ClientSettings.Language
		};
		ScreenManager.ConnectToSingleplayer(args);
	}

	private bool OnBack()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
		return true;
	}

	public string GenRandomSurvivalName()
	{
		string playername = ClientSettings.PlayerName;
		if (playername == null)
		{
			playername = "Tyron";
		}
		playername = (playername.EndsWith('s') ? (playername + "'") : (playername + "s"));
		string[] firstPart = new string[11]
		{
			playername, playername, "Vintage", "Awesome", "Dark", "Serene", "Creepy", "Gloomy", "Peaceful", "Foggy",
			"Sunny"
		};
		string[] secondPart = new string[5] { "Adventure", "Cave", "Kingdom", "Village", "Hermit" };
		string[] thirdPart = new string[5] { "Tales", "Valley", "Lands", "Story", "World" };
		return firstPart[rand.Next(firstPart.Length)] + " " + secondPart[rand.Next(secondPart.Length)] + " " + thirdPart[rand.Next(thirdPart.Length)];
	}

	public string GenRandomCreativeName()
	{
		string playername = ClientSettings.PlayerName;
		if (playername == null)
		{
			playername = "Tyron";
		}
		playername = (playername.EndsWith('s') ? (playername + "'") : (playername + "s"));
		string[] firstPart = new string[11]
		{
			playername, playername, "Vintage", "Massive", "Dark", "Serene", "Epic", "Gloomy", "Peaceful", "Foggy",
			"Sunny"
		};
		string[] secondPart = new string[5] { "Test", "Superflat", "Creative", "Freestyle", "Doodle" };
		string[] thirdPart = new string[4] { "Place", "Lands", "Story", "World" };
		return firstPart[rand.Next(firstPart.Length)] + " " + secondPart[rand.Next(secondPart.Length)] + " " + thirdPart[rand.Next(thirdPart.Length)];
	}
}
