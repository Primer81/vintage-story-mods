using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class GuiScreenPublicServerView : GuiScreen
{
	private ServerListEntry entry;

	public GuiScreenPublicServerView(ServerListEntry entry, ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		this.entry = entry;
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
			ScreenManager.GuiComposers.Dispose("mainmenu-browserpublicserverview");
		}
	}

	private void InitGui()
	{
		ElementBounds titleElement = ElementBounds.Fixed(0.0, 0.0, 700.0, 30.0);
		ElementBounds leftElement = titleElement.BelowCopy().WithFixedSize(170.0, 35.0);
		List<string> configsList = new List<string>();
		if (entry.hasPassword)
		{
			configsList.Add(Lang.Get("Password protected"));
		}
		if (entry.whitelisted)
		{
			configsList.Add(Lang.Get("Whitelisted players only"));
		}
		string configs = string.Join(", ", configsList);
		List<string> modList = new List<string>();
		int i = 0;
		ModPacket[] mods2 = entry.mods;
		foreach (ModPacket val in mods2)
		{
			if (i++ > 20 && entry.mods.Length > 25)
			{
				break;
			}
			modList.Add(val.id);
		}
		string mods = string.Join(", ", modList);
		if (modList.Count < entry.mods.Length)
		{
			mods += Lang.Get(" and {0} more", entry.mods.Length - modList.Count);
		}
		if (mods.Length == 0)
		{
			mods = Lang.Get("server-nomods");
		}
		CairoFont font = CairoFont.WhiteSmallText();
		ElementComposer = dialogBase("mainmenu-browserpublicserverview").AddStaticText(entry.serverName, CairoFont.WhiteSmallishText(), titleElement.FlatCopy()).AddStaticText(Lang.Get("Description"), font, leftElement = leftElement.BelowCopy()).AddRichtext((entry.gameDescription.Length == 0) ? "<i>No description</i>" : entry.gameDescription, font, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 35.0), "desc");
		ElementBounds rtBounds = ElementComposer.GetRichtext("desc").Bounds;
		ElementComposer.GetRichtext("desc").BeforeCalcBounds();
		ElementComposer.AddStaticText(Lang.Get("Playstyle"), font, leftElement = leftElement.BelowCopy().WithFixedOffset(0.0, Math.Max(0.0, rtBounds.fixedHeight - 30.0))).AddStaticText((entry.playstyle.langCode == null) ? Lang.Get("playstyle-" + entry.playstyle.id) : Lang.Get("playstyle-" + entry.playstyle.langCode), font, EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 70.0)).AddStaticText(Lang.Get("Currently online"), font, leftElement = leftElement.BelowCopy())
			.AddStaticText(entry.players + " / " + entry.maxPlayers, font, EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 30.0));
		if (configs.Length > 0)
		{
			ElementComposer.AddStaticText(Lang.Get("Configuration"), font, leftElement = leftElement.BelowCopy()).AddStaticText(configs, font, EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 30.0));
		}
		ElementComposer.AddStaticText(Lang.Get("Game version"), font, leftElement = leftElement.BelowCopy()).AddStaticText(entry.gameVersion, font, EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 30.0)).AddStaticText(Lang.Get("Mods", entry.mods.Length), font, leftElement = leftElement.BelowCopy())
			.AddStaticText(mods, font, EnumTextOrientation.Left, leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedSize(540.0, 30.0), "mods");
		ElementComposer.GetStaticText("mods").Bounds.CalcWorldBounds();
		double height = ElementComposer.GetStaticText("mods").GetTextHeight() / (double)RuntimeEnv.GUIScale;
		if (entry.hasPassword)
		{
			ElementComposer.AddIf(entry.hasPassword).AddStaticText(Lang.Get("Password"), CairoFont.WhiteSmallishText(), leftElement = leftElement.BelowCopy(0.0, height - 20.0)).AddTextInput(leftElement.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedOffset(0.0, -3.0)
				.WithFixedSize(540.0, 30.0), null, null, "password")
				.EndIf();
		}
		else
		{
			leftElement = leftElement.FlatCopy();
			leftElement.fixedY += height;
		}
		double joinlen = CairoFont.ButtonText().GetTextExtents(Lang.Get("Join Server")).Width / (double)RuntimeEnv.GUIScale;
		ElementComposer.AddButton(Lang.Get("Back"), OnBack, ElementBounds.Fixed(0, 0).FixedUnder(leftElement, 20.0).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedAlignmentOffset(0.0, 0.0)
			.WithFixedPadding(10.0, 2.0)).AddButton(Lang.Get("Add to Favorites"), OnAddToFavorites, ElementBounds.Fixed(0, 0).FixedUnder(leftElement, 20.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedAlignmentOffset(-30.0 - joinlen, 0.0)
			.WithFixedPadding(10.0, 2.0)).AddButton(Lang.Get("Join Server"), OnJoin, ElementBounds.Fixed(0, 0).FixedUnder(leftElement, 20.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0))
			.EndChildElements()
			.Compose();
	}

	private bool OnAddToFavorites()
	{
		List<string> entries = ClientSettings.Inst.GetStringListSetting("multiplayerservers", new List<string>());
		string uri = entry.serverIp;
		string name = entry.serverName.Replace(",", "");
		string password = ElementComposer.GetTextInput("password")?.GetText().Replace(",", "&comma;");
		entries.Add(name + "," + uri + "," + password);
		ClientSettings.Inst.Strings["multiplayerservers"] = entries;
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
		return true;
	}

	private bool OnBack()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenPublicServers));
		return true;
	}

	private bool OnJoin()
	{
		if (!entry.hasPassword)
		{
			ScreenManager.ConnectToMultiplayer(entry.serverIp, null);
		}
		else
		{
			ScreenManager.ConnectToMultiplayer(entry.serverIp, ElementComposer.GetTextInput("password").GetText());
		}
		return true;
	}
}
