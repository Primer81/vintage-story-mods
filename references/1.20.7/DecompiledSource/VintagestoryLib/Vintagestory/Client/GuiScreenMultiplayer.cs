using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

public class GuiScreenMultiplayer : GuiScreen
{
	private List<MultiplayerServerEntry> serverentries;

	private ElementBounds tableBounds;

	private ElementBounds clippingBounds;

	public GuiScreenMultiplayer(ScreenManager screenManager, GuiScreen parent)
		: base(screenManager, parent)
	{
		ShowMainMenu = true;
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

	private void invalidate()
	{
		if (base.IsOpened)
		{
			InitGui();
		}
		else
		{
			ScreenManager.GuiComposers.Dispose("mainmenu-multiplayer");
		}
	}

	private void InitGui()
	{
		List<SavegameCellEntry> cells = LoadServerEntries();
		int windowHeight = ScreenManager.GamePlatform.WindowSize.Height;
		int windowWidth = ScreenManager.GamePlatform.WindowSize.Width;
		ElementBounds buttonBounds = ElementBounds.FixedSize(60.0, 30.0).WithFixedPadding(10.0, 2.0);
		ElementBounds titleBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, ScreenManager.ClientIsOffline ? 30 : 0, 690.0, 35.0);
		float height = (float)Math.Max(300, windowHeight) / ClientSettings.GUIScale;
		ElementBounds insetBounds;
		ElementComposer = dialogBase("mainmenu-multiplayer").AddStaticText(Lang.Get("multiplayer-yourservers"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 240.0)).AddIf(ScreenManager.ClientIsOffline).AddRichtext(Lang.Get("offlinemultiplayerwarning"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0.0, 25.0, 690.0, 30.0))
			.EndIf()
			.AddInset(insetBounds = titleBounds.BelowCopy(0.0, 3.0).WithFixedSize(Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale, height - 250f))
			.AddVerticalScrollbar(OnNewScrollbarvalue, ElementStdBounds.VerticalScrollbar(insetBounds), "scrollbar")
			.BeginClip(clippingBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
			.AddCellList(tableBounds = clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0), createCellElem, cells, "serverstable")
			.EndClip()
			.AddButton(Lang.Get("multiplayer-addserver"), OnAddServer, buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0).WithAlignment(EnumDialogArea.RightFixed)
				.WithFixedAlignmentOffset(-13.0, 0.0))
			.AddButton(Lang.Get("multiplayer-browsepublicservers"), OnPublicListing, buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0))
			.AddSmallButton(Lang.Get("multiplayer-selfhosting"), OnSelfHosting, buttonBounds.FlatCopy().FixedUnder(insetBounds, 60.0))
			.EndChildElements()
			.Compose();
		tableBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)tableBounds.fixedHeight);
	}

	private IGuiElementCell createCellElem(SavegameCellEntry cell, ElementBounds bounds)
	{
		GuiElementMainMenuCell guiElementMainMenuCell = new GuiElementMainMenuCell(ScreenManager.api, cell, bounds);
		cell.LeftOffY = -2f;
		guiElementMainMenuCell.OnMouseDownOnCellLeft = OnClickCellLeft;
		guiElementMainMenuCell.OnMouseDownOnCellRight = OnClickCellRight;
		return guiElementMainMenuCell;
	}

	private bool OnSelfHosting()
	{
		ScreenManager.api.Gui.OpenLink("https://www.vintagestory.at/multiplayer");
		return true;
	}

	private bool OnPublicListing()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenPublicServers));
		return true;
	}

	public override void OnScreenLoaded()
	{
		InitGui();
		ElementComposer.GetCellList<SavegameCellEntry>("serverstable").ReloadCells(LoadServerEntries());
		tableBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)tableBounds.fixedHeight);
	}

	private List<SavegameCellEntry> LoadServerEntries()
	{
		serverentries = new List<MultiplayerServerEntry>();
		List<string> entries = ClientSettings.Inst.GetStringListSetting("multiplayerservers", new List<string>());
		List<SavegameCellEntry> cells = new List<SavegameCellEntry>();
		for (int i = 0; i < entries.Count; i++)
		{
			string[] elems = entries[i].Split(',');
			MultiplayerServerEntry serverentry = new MultiplayerServerEntry
			{
				index = i,
				name = elems[0],
				host = elems[1],
				password = ((elems.Length > 2) ? elems[2] : "")
			};
			serverentries.Add(serverentry);
			SavegameCellEntry cell = new SavegameCellEntry
			{
				Title = serverentry.name
			};
			cells.Add(cell);
		}
		return cells;
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = ElementComposer.GetCellList<SavegameCellEntry>("serverstable").Bounds;
		bounds.fixedY = 0f - value;
		bounds.CalcWorldBounds();
	}

	private void OnClickCellLeft(int index)
	{
		MultiplayerServerEntry entry = serverentries[index];
		ScreenManager.ConnectToMultiplayer(entry.host, entry.password);
	}

	private void OnClickCellRight(int cellIndex)
	{
		ScreenManager.LoadScreen(new GuiScreenMultiplayerModify(serverentries[cellIndex], ScreenManager, this));
	}

	private bool OnAddServer()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayerNewServer));
		return true;
	}
}
