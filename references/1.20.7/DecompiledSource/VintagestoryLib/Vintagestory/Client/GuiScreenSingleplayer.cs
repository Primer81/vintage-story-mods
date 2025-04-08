using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class GuiScreenSingleplayer : GuiScreen
{
	private SaveGameEntry[] entries;

	private int lastClickedCellIndex;

	private ElementBounds listBounds;

	private ElementBounds clippingBounds;

	public GuiScreenSingleplayer(ScreenManager screenManager, GuiScreen parent)
		: base(screenManager, parent)
	{
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

	private void invalidate()
	{
		if (base.IsOpened)
		{
			InitGui();
		}
		else
		{
			ScreenManager.GuiComposers.Dispose("mainmenu-singleplayer");
		}
	}

	private void InitGui()
	{
		List<SavegameCellEntry> cells = LoadSaveGameCells();
		int windowWidth = ScreenManager.GamePlatform.WindowSize.Width;
		int windowHeight = ScreenManager.GamePlatform.WindowSize.Height;
		ElementBounds buttonBounds = ElementBounds.FixedSize(60.0, 30.0).WithFixedPadding(10.0, 2.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedAlignmentOffset(-13.0, 0.0);
		ElementBounds titleBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 35.0);
		_ = Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale;
		ElementBounds insetBounds;
		ElementComposer = dialogBase("mainmenu-singleplayer").AddStaticText(Lang.Get("singleplayer-worlds"), CairoFont.WhiteSmallishText(), titleBounds).AddInset(insetBounds = titleBounds.BelowCopy(0.0, 3.0).WithFixedSize(Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale, (float)Math.Max(300, windowHeight) / ClientSettings.GUIScale - 205f)).AddVerticalScrollbar(OnNewScrollbarvalue, ElementStdBounds.VerticalScrollbar(insetBounds), "scrollbar")
			.BeginClip(clippingBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
			.AddCellList(listBounds = clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0), createCellElem, cells, "worldstable")
			.EndClip();
		GuiElementCellList<SavegameCellEntry> cellListElem = ElementComposer.GetCellList<SavegameCellEntry>("worldstable");
		cellListElem.BeforeCalcBounds();
		for (int i = 0; i < cells.Count; i++)
		{
			ElementBounds bounds = cellListElem.elementCells[i].Bounds.ForkChild();
			cellListElem.elementCells[i].Bounds.ChildBounds.Add(bounds);
			bounds.fixedWidth -= 56.0;
			bounds.fixedY = -3.0;
			bounds.fixedX -= 6.0;
			bounds.fixedHeight -= 2.0;
			ElementComposer.AddHoverText(cells[i].Title + "\r\n" + cells[i].HoverText, CairoFont.WhiteDetailText(), 320, bounds, "hover-" + i);
			ElementComposer.GetHoverText("hover-" + i).InsideClipBounds = clippingBounds;
		}
		ElementComposer.AddButton(Lang.Get("Open Saves Folder"), OnOpenSavesFolder, buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedAlignmentOffset(0.0, 0.0)).AddButton(Lang.Get("singleplayer-newworld"), OnNewWorld, buttonBounds.FixedUnder(insetBounds, 10.0)).EndChildElements()
			.Compose();
		listBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
	}

	private IGuiElementCell createCellElem(SavegameCellEntry cell, ElementBounds bounds)
	{
		return new GuiElementMainMenuCell(ScreenManager.api, cell, bounds)
		{
			cellEntry = 
			{
				DetailTextOffY = 0.0,
				LeftOffY = -2f
			},
			OnMouseDownOnCellLeft = OnClickCellLeft,
			OnMouseDownOnCellRight = OnClickCellRight
		};
	}

	public override void OnScreenLoaded()
	{
		InitGui();
		listBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
	}

	private bool OnOpenSavesFolder()
	{
		NetUtil.OpenUrlInBrowser(GamePaths.Saves);
		return true;
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = ElementComposer.GetCellList<SavegameCellEntry>("worldstable").Bounds;
		bounds.fixedY = 0f - value;
		bounds.CalcWorldBounds();
	}

	private void OnClickCellRight(int cellIndex)
	{
		lastClickedCellIndex = cellIndex;
		if (entries[cellIndex].IsReadOnly)
		{
			ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Have no write access to this file, it seems in use. Make sure no other client or server is currently using this savegame."), delegate
			{
				ScreenManager.LoadScreen(this);
			}, ScreenManager, this, onlyCancel: true));
		}
		else
		{
			ScreenManager.LoadScreen(new GuiScreenSingleplayerModify(entries[cellIndex].Filename, ScreenManager, this));
		}
	}

	private void OnClickCellLeft(int cellIndex)
	{
		lastClickedCellIndex = cellIndex;
		if (entries[cellIndex].Savegame == null)
		{
			ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("savegame-corrupt-confirmrepair"), OnConfirmRepairMode, ScreenManager, this));
		}
		else
		{
			if (entries[cellIndex].Savegame.HighestChunkdataVersion > 2)
			{
				return;
			}
			if (entries[cellIndex].DatabaseVersion != GameVersion.DatabaseVersion)
			{
				ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("This world uses an old file format that needs upgrading. This might take a while. It is also suggested to first back up your savegame in case the upgrade fails. Proceed?"), OnDidConfirmUpgrade, ScreenManager, this));
				return;
			}
			if (entries[cellIndex].IsReadOnly)
			{
				ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Have no write access to this file, it seems in use. Make sure no other client or server is currently using this savegame."), delegate
				{
					ScreenManager.LoadScreen(this);
				}, ScreenManager, this, onlyCancel: true));
				return;
			}
			ScreenManager.ConnectToSingleplayer(new StartServerArgs
			{
				SaveFileLocation = entries[cellIndex].Filename,
				DisabledMods = ClientSettings.DisabledMods,
				Language = ClientSettings.Language
			});
		}
	}

	private void OnConfirmRepairMode(bool confirm)
	{
		if (confirm)
		{
			ScreenManager.ConnectToSingleplayer(new StartServerArgs
			{
				SaveFileLocation = entries[lastClickedCellIndex].Filename,
				DisabledMods = ClientSettings.DisabledMods,
				Language = ClientSettings.Language,
				RepairMode = true
			});
		}
		else
		{
			ScreenManager.LoadScreen(this);
		}
	}

	private void OnDidConfirmUpgrade(bool confirm)
	{
		if (confirm)
		{
			ScreenManager.ConnectToSingleplayer(new StartServerArgs
			{
				SaveFileLocation = entries[lastClickedCellIndex].Filename,
				DisabledMods = ClientSettings.DisabledMods,
				Language = ClientSettings.Language
			});
		}
		else
		{
			ScreenManager.LoadScreen(this);
		}
	}

	private bool OnNewWorld()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayerNewWorld));
		return true;
	}

	public override bool OnBackPressed()
	{
		ScreenManager.StartMainMenu();
		return true;
	}

	public bool OnCancel()
	{
		OnBackPressed();
		return true;
	}

	private List<SavegameCellEntry> LoadSaveGameCells()
	{
		List<SavegameCellEntry> cells = new List<SavegameCellEntry>();
		LoadSaveGames();
		for (int i = 0; i < entries.Length; i++)
		{
			SaveGameEntry entry = entries[i];
			SavegameCellEntry cell;
			if (entry.Savegame == null)
			{
				cell = new SavegameCellEntry
				{
					Title = new FileInfo(entry.Filename).Name,
					DetailText = (entry.IsReadOnly ? Lang.Get("Unable to load savegame and no write access, likely already opened elsewhere.") : Lang.Get("Invalid or corrupted savegame")),
					TitleFont = CairoFont.WhiteSmallishText().WithColor(GuiStyle.ErrorTextColor),
					DetailTextFont = CairoFont.WhiteSmallText().WithColor(GuiStyle.ErrorTextColor)
				};
			}
			else if (entry.Savegame.HighestChunkdataVersion > 2)
			{
				cell = new SavegameCellEntry
				{
					Title = new FileInfo(entry.Filename).Name,
					DetailText = Lang.Get("versionmismatch-chunk"),
					TitleFont = CairoFont.WhiteSmallishText().WithColor(GuiStyle.ErrorTextColor),
					DetailTextFont = CairoFont.WhiteSmallText().WithColor(GuiStyle.ErrorTextColor)
				};
			}
			else
			{
				bool isNewerVersion = GameVersion.IsNewerVersionThan(entry.Savegame.LastSavedGameVersion, "1.20.7");
				SavegameCellEntry savegameCellEntry = new SavegameCellEntry();
				savegameCellEntry.Title = entry.Savegame.WorldName;
				savegameCellEntry.DetailText = string.Format("{0}, {1}{2}{3}", (entry.Savegame.PlayStyleLangCode == null) ? Lang.Get("playstyle-" + entry.Savegame.PlayStyle) : Lang.Get("playstyle-" + entry.Savegame.PlayStyleLangCode), Lang.Get("Time played: {0}", PrettyTime(entry.Savegame.TotalSecondsPlayed)), (entry.DatabaseVersion != GameVersion.DatabaseVersion) ? ("\nRequires file format upgrade (DB v" + entry.DatabaseVersion + ")") : "", isNewerVersion ? ("\n" + Lang.Get("versionmismatch-savegame")) : "");
				savegameCellEntry.HoverText = getHoverText(entry.Savegame);
				cell = savegameCellEntry;
			}
			cells.Add(cell);
		}
		return cells;
	}

	private string getHoverText(SaveGame savegame)
	{
		ITreeAttribute pworldConfig = savegame.WorldConfiguration;
		StringBuilder sb = new StringBuilder();
		foreach (ModContainer mod in ScreenManager.verifiedMods)
		{
			ModWorldConfiguration config = mod.WorldConfig;
			if (config == null)
			{
				continue;
			}
			WorldConfigurationAttribute[] worldConfigAttributes = config.WorldConfigAttributes;
			foreach (WorldConfigurationAttribute attribute in worldConfigAttributes)
			{
				WorldConfigurationValue value = new WorldConfigurationValue();
				value.Attribute = attribute;
				value.Code = attribute.Code;
				PlayStyle playstyle = null;
				PlayStyle[] playStyles = mod.WorldConfig.PlayStyles;
				foreach (PlayStyle ps in playStyles)
				{
					if (ps.Code == savegame.PlayStyle)
					{
						playstyle = ps;
						break;
					}
				}
				string defaultValue = attribute.Default.ToLowerInvariant();
				if (playstyle != null && playstyle.WorldConfig[value.Code].Exists)
				{
					defaultValue = playstyle.WorldConfig[value.Code].ToString();
				}
				IAttribute attr = pworldConfig[value.Code];
				if (attr != null && attr.ToString().ToLowerInvariant() != defaultValue)
				{
					sb.AppendLine("<font opacity=\"0.6\">" + Lang.Get("worldattribute-" + attribute.Code) + ":</font> " + attribute.valueToHumanReadable(attr.ToString()));
				}
			}
		}
		if (savegame.MapSizeY != 256)
		{
			sb.Append(Lang.Get("worldconfig-worldheight", savegame.MapSizeY));
		}
		if (sb.Length == 0)
		{
			sb.Append("<font opacity=\"0.6\"><i>" + Lang.Get("No custom configurations") + "</i></font>");
		}
		else
		{
			sb.AppendLine();
			sb.Append("<font opacity=\"0.6\"><i>" + Lang.Get("All other configurations are default values") + "</i></font>");
		}
		return sb.ToString();
	}

	public static string PrettyTime(int seconds)
	{
		if (seconds < 60)
		{
			return Lang.Get("{0} seconds", seconds);
		}
		if (seconds < 3600)
		{
			return Lang.Get("{0} minutes, {1} seconds", seconds / 60, seconds - seconds / 60 * 60);
		}
		int hours = seconds / 3600;
		int minutes = seconds / 60 - hours * 60;
		return Lang.Get("{0} hours, {1} minutes", hours, minutes);
	}

	internal string[] GetFilenames()
	{
		string[] files = Directory.GetFiles(GamePaths.Saves);
		List<string> savegames = new List<string>();
		for (int i = 0; i < files.Length; i++)
		{
			if (files[i].EndsWithOrdinal(".vcdbs"))
			{
				savegames.Add(files[i]);
			}
		}
		return savegames.ToArray();
	}

	private void LoadSaveGames()
	{
		string[] filenames = GetFilenames();
		List<SaveGameEntry> savegames = new List<SaveGameEntry>();
		GameDatabase db = new GameDatabase(ScreenManager.GamePlatform.Logger);
		for (int i = 0; i < filenames.Length; i++)
		{
			int version = 0;
			bool isreadonly = true;
			SaveGame savegame = null;
			try
			{
				savegame = db.ProbeOpenConnection(filenames[i], corruptionProtection: false, out version, out isreadonly);
				savegame?.LoadWorldConfig();
				db.CloseConnection();
			}
			catch (Exception)
			{
			}
			SaveGameEntry entry3 = new SaveGameEntry
			{
				DatabaseVersion = version,
				Savegame = savegame,
				Filename = filenames[i],
				IsReadOnly = isreadonly,
				Modificationdate = File.GetLastWriteTime(filenames[i])
			};
			savegames.Add(entry3);
		}
		savegames.Sort((SaveGameEntry entry1, SaveGameEntry entry2) => entry2.Modificationdate.CompareTo(entry1.Modificationdate));
		entries = savegames.ToArray();
	}
}
