using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class GuiScreenSingleplayerModify : GuiScreen
{
	private string worldfilename;

	private int worldSeed;

	private string playstylelangcode;

	private GameDatabase gamedb;

	private WorldConfig wcu;

	public GuiScreenSingleplayerModify(string worldfilename, ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		ShowMainMenu = true;
		this.worldfilename = worldfilename;
	}

	private SaveGame getSaveGame(out int version, out bool isreadonly, bool keepOpen = false)
	{
		if (gamedb != null)
		{
			gamedb.Dispose();
		}
		gamedb = new GameDatabase(ScreenManager.GamePlatform.Logger);
		string errorMessage;
		SaveGame saveGame = gamedb.ProbeOpenConnection(worldfilename, corruptionProtection: false, out version, out errorMessage, out isreadonly);
		saveGame?.LoadWorldConfig();
		if (!keepOpen)
		{
			gamedb.CloseConnection();
		}
		return saveGame;
	}

	public void initGui(SaveGame savegame)
	{
		wcu = new WorldConfig(ScreenManager.verifiedMods);
		wcu.loadFromSavegame(savegame);
		wcu.updateJWorldConfig();
		ElementStdBounds.ToggleButton(10.0, 190.0, 120.0, 60.0).WithFixedPadding(0.0, 1.0);
		CairoFont.WhiteSmallText();
		if (savegame != null)
		{
			worldSeed = savegame.Seed;
			playstylelangcode = savegame.PlayStyleLangCode;
			ElementBounds titleElement = ElementBounds.Fixed(0.0, 0.0, 330.0, 80.0);
			ElementBounds leftElement = titleElement.BelowCopy().WithFixedHeight(35.0);
			double saveWidth = CairoFont.ButtonText().GetTextExtents(Lang.Get("Save")).Width / (double)RuntimeEnv.GUIScale + 40.0;
			double customizeWidth = 0.0;
			string rectext = Lang.Get("Create a new world with this world seed");
			double recseedWidth = CairoFont.WhiteSmallText().GetTextExtents(rectext).Width / (double)RuntimeEnv.GUIScale + 40.0;
			string playstyle = ((savegame.PlayStyleLangCode == null) ? Lang.Get("playstyle-" + savegame.PlayStyle) : Lang.Get("playstyle-" + savegame.PlayStyleLangCode));
			GuiComposer composer = dialogBase("mainmenu-singleplayermodifyworld", -1.0, 550.0).AddStaticText(Lang.Get("Modify World"), CairoFont.WhiteSmallishText(), titleElement).AddStaticText(Lang.Get("World name"), CairoFont.WhiteSmallishText(), leftElement).AddTextInput(leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedOffset(0.0, -3.0)
				.WithFixedSize(470.0, 30.0), null, null, "worldname")
				.AddStaticText(Lang.Get("Filename on disk"), CairoFont.WhiteSmallishText(), leftElement = leftElement.BelowCopy())
				.AddTextInput(leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedOffset(0.0, -3.0)
					.WithFixedSize(470.0, 30.0), null, null, "filename")
				.AddStaticText(Lang.Get("Seed"), CairoFont.WhiteSmallishText(), leftElement = leftElement.BelowCopy(0.0, 4.0))
				.AddStaticText(worldSeed.ToString() ?? "", CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0))
				.AddIconButton("copy", OnCopySeed, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0))
				.AddHoverText(Lang.Get("Copies the seed to your clipboard"), CairoFont.WhiteDetailText(), 200, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0)
					.WithFixedPadding(5.0))
				.AddStaticText(Lang.Get("Total Time Played"), CairoFont.WhiteSmallishText(), leftElement = leftElement.BelowCopy())
				.AddStaticText(GuiScreenSingleplayer.PrettyTime(savegame.TotalSecondsPlayed) ?? "", CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0))
				.AddStaticText(Lang.Get("Created with"), CairoFont.WhiteSmallishText(), leftElement = leftElement.BelowCopy())
				.AddStaticText(Lang.Get("versionnumber", savegame.CreatedGameVersion), CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0))
				.AddStaticText(Lang.GetWithFallback("singleplayer-world-creator", "Created by"), CairoFont.WhiteSmallishText(), leftElement = leftElement.BelowCopy())
				.AddStaticText(savegame.CreatedByPlayerName, CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0))
				.AddStaticText(Lang.Get("Playstyle"), CairoFont.WhiteSmallishText(), leftElement = leftElement.BelowCopy())
				.AddStaticText(playstyle, CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0))
				.AddIconButton("copy", OnCopyPlaystyle, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0))
				.AddHoverText(Lang.Get("Copies the playstyle to your clipboard"), CairoFont.WhiteDetailText(), 200, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(30.0, 30.0)
					.WithFixedPadding(5.0));
			string text = Lang.Get("World Size");
			CairoFont font = CairoFont.WhiteSmallishText();
			ElementBounds elementBounds = leftElement.BelowCopy();
			ElementComposer = GuiComposerHelpers.AddStaticText(bounds: elementBounds.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(470.0, 30.0), composer: composer.AddStaticText(text, font, elementBounds), text: savegame.MapSizeX + "x" + savegame.MapSizeY + "x" + savegame.MapSizeZ, font: CairoFont.WhiteSmallishText(), orientation: EnumTextOrientation.Left).AddButton(Lang.Get("Back"), OnCancel, ElementStdBounds.Rowed(5.8f, 0.0, EnumDialogArea.LeftFixed).WithFixedAlignmentOffset(0.0, 0.0).WithFixedPadding(10.0, 2.0)).AddButton(Lang.Get("Delete"), OnDelete, ElementStdBounds.Rowed(5.8f, 0.0, EnumDialogArea.RightFixed).WithFixedAlignmentOffset(0.0 - saveWidth - customizeWidth, 0.0).WithFixedPadding(10.0, 2.0))
				.AddButton(Lang.Get("Save"), OnSave, ElementStdBounds.Rowed(5.8f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
				.AddSmallButton(Lang.Get("Create a backup"), OnCreateBackup, ElementStdBounds.Rowed(6.8f, 0.0, EnumDialogArea.RightFixed).WithFixedAlignmentOffset(0.0 - recseedWidth - 20.0, 0.0).WithFixedPadding(10.0, 3.0))
				.AddSmallButton(rectext, OnNewWorldWithSeed, ElementStdBounds.Rowed(6.8f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 3.0))
				.AddSmallButton(Lang.Get("Run in Repair mode"), OnRunInRepairMode, ElementStdBounds.Rowed(7.5f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 3.0))
				.AddDynamicText("", CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(8.5f, 0.0).WithFixedSize(400.0, 30.0), "dyntextbottom")
				.EndChildElements()
				.Compose();
			ElementComposer.GetTextInput("worldname").SetValue(savegame.WorldName);
			FileInfo file = new FileInfo(worldfilename);
			ElementComposer.GetTextInput("filename").SetValue(file.Name);
		}
		else
		{
			ElementComposer = dialogBase("mainmenu-singleplayermodifyworld", -1.0, 550.0).AddStaticText(Lang.Get("singleplayer-modify"), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(0f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0)).AddStaticText(Lang.Get("singleplayer-corrupt", worldfilename), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(1f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0)).AddButton(Lang.Get("general-cancel"), OnCancel, ElementStdBounds.Rowed(5.2f, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0).WithFixedAlignmentOffset(-10.0, 0.0))
				.AddButton(Lang.Get("general-delete"), OnDelete, ElementStdBounds.Rowed(5.2f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
				.Compose()
				.EndChildElements();
		}
	}

	private void OnCopyPlaystyle(bool ok)
	{
		ScreenManager.Platform.XPlatInterface.SetClipboardText(wcu.ToJson());
	}

	public bool OnCreateBackup()
	{
		ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Create back up now?"), OnDidBackup, ScreenManager, this));
		return true;
	}

	private void OnDidBackup(bool ok)
	{
		if (ok)
		{
			FileInfo fileInfo = new FileInfo(worldfilename);
			string filename = string.Concat(str2: $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}", str0: fileInfo.Name.Replace(".vcdbs", ""), str1: "-bkp-", str3: ".vcdbs");
			File.Copy(worldfilename, Path.Combine(GamePaths.BackupSaves, filename));
			ScreenManager.LoadScreen(this);
			ElementComposer.GetDynamicText("dyntextbottom").SetNewText(Lang.Get("Ok, backup created"));
		}
		else
		{
			ScreenManager.LoadScreen(this);
		}
	}

	public override void OnScreenLoaded()
	{
		base.OnScreenLoaded();
		initGui(getSaveGame(out var _, out var _));
	}

	private void OnCopySeed(bool copy)
	{
		ScreenManager.Platform.XPlatInterface.SetClipboardText(worldSeed.ToString() ?? "");
	}

	private bool OnNewWorldWithSeed()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayerNewWorld));
		GuiScreenSingleplayerNewWorld screen = ScreenManager.CurrentScreen as GuiScreenSingleplayerNewWorld;
		int i = 0;
		foreach (PlaystyleListEntry cell in screen.cells)
		{
			if (cell.PlayStyle.LangCode == playstylelangcode)
			{
				screen.OnClickCellLeft(i);
				break;
			}
			i++;
		}
		screen.OnCustomize();
		if (!(ScreenManager.CurrentScreen is GuiScreenWorldCustomize screen2))
		{
			return false;
		}
		screen2.ElementComposer.GetTextInput("worldseed").SetValue(worldSeed.ToString() ?? "");
		return true;
	}

	private bool OnRunInRepairMode()
	{
		ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("confirm-repairmode"), delegate(bool val)
		{
			if (val)
			{
				repairGame();
			}
			else
			{
				ScreenManager.LoadScreen(this);
			}
		}, ScreenManager, this));
		return true;
	}

	private void repairGame()
	{
		getSaveGame(out var version, out var isreadonly);
		if (version != GameVersion.DatabaseVersion)
		{
			ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("This world uses an old file format that needs upgrading. This might take a while. It is also suggested to first back up your savegame in case the upgrade fails. Proceed?"), OnDidConfirmUpgrade, ScreenManager, this));
			return;
		}
		if (isreadonly)
		{
			ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Have no write access to this file, it seems in use. Make sure no other client or server is currently using this savegame."), delegate
			{
				ScreenManager.LoadScreen(this);
			}, ScreenManager, this, onlyCancel: true));
			return;
		}
		ScreenManager.ConnectToSingleplayer(new StartServerArgs
		{
			SaveFileLocation = worldfilename,
			DisabledMods = ClientSettings.DisabledMods,
			Language = ClientSettings.Language,
			RepairMode = true
		});
	}

	private void OnDidConfirmUpgrade(bool confirm)
	{
		if (confirm)
		{
			ScreenManager.ConnectToSingleplayer(new StartServerArgs
			{
				SaveFileLocation = worldfilename,
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

	private bool OnSave()
	{
		FileInfo file = new FileInfo(worldfilename);
		string nowFileName = ElementComposer.GetTextInput("filename").GetText();
		if (file.Name != nowFileName)
		{
			try
			{
				file.MoveTo(worldfilename = Path.Combine(file.DirectoryName, nowFileName));
			}
			catch (Exception)
			{
			}
		}
		if (!gamedb.OpenConnection(file.FullName, out var errorMessage, corruptionProtection: false, doIntegrityCheck: false))
		{
			ScreenManager.LoadScreen(new GuiScreenMessage(Lang.Get("singleplayer-failedchanges"), Lang.Get("singleplayer-maybecorrupt", errorMessage), OnMessageConfirmed, ScreenManager, this));
			return true;
		}
		int version;
		bool isreadonly;
		SaveGame savegame = getSaveGame(out version, out isreadonly, keepOpen: true);
		savegame.WorldName = ElementComposer.GetTextInput("worldname").GetText();
		gamedb.StoreSaveGame(savegame);
		gamedb.CloseConnection();
		gamedb.Dispose();
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
		return true;
	}

	private void OnMessageConfirmed()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
	}

	private bool OnDelete()
	{
		ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Really delete world '{0}'?", worldfilename), OnDidConfirmDelete, ScreenManager, this));
		return true;
	}

	private void OnDidConfirmDelete(bool confirm)
	{
		if (confirm)
		{
			ScreenManager.GamePlatform.XPlatInterface.MoveFileToRecyclebin(worldfilename);
			ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
		}
		else
		{
			ScreenManager.LoadScreen(this);
		}
	}

	private bool OnCancel()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
		return true;
	}
}
