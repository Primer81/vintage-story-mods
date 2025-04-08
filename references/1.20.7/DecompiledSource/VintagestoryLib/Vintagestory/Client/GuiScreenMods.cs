using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class GuiScreenMods : GuiScreen
{
	private bool ingoreLoadOnce = true;

	private ElementBounds listBounds;

	private ElementBounds clippingBounds;

	private IAsset warningIcon;

	private ScreenManager screenManager;

	public GuiScreenMods(ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		this.screenManager = screenManager;
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
		warningIcon = ScreenManager.api.Assets.Get(new AssetLocation("textures/icons/warning.svg"));
	}

	private void invalidate()
	{
		if (base.IsOpened)
		{
			InitGui();
		}
		else
		{
			ScreenManager.GuiComposers.Dispose("mainmenu-mods");
		}
	}

	public override void OnScreenLoaded()
	{
		if (ingoreLoadOnce)
		{
			ingoreLoadOnce = false;
			return;
		}
		InitGui();
		ElementComposer.GetCellList<ModCellEntry>("modstable").ReloadCells(LoadModCells());
		listBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
	}

	private List<ModCellEntry> LoadModCells()
	{
		List<string> disabledMods = ClientSettings.DisabledMods;
		List<ModCellEntry> cells = new List<ModCellEntry>();
		foreach (ModContainer mod in ScreenManager.allMods)
		{
			ModInfo modinfo = mod.Info;
			CairoFont font = CairoFont.WhiteDetailText();
			font.WithFontSize((float)GuiStyle.SmallFontSize);
			if (mod.Error.HasValue)
			{
				string errorText = mod.Error switch
				{
					ModError.Loading => Lang.Get("Unable to load mod. Check log files."), 
					ModError.Dependency => (mod.MissingDependencies != null) ? ((mod.MissingDependencies.Count != 1) ? Lang.Get("Unable to load mod. Requires dependencies {0}", string.Join(", ", mod.MissingDependencies.Select((string str) => str.Replace("@", " v")))) : Lang.Get("Unable to load mod. Requires dependency {0}", string.Join(", ", mod.MissingDependencies.Select((string str) => str.Replace("@", " v"))))) : Lang.Get("Unable to load mod. A dependency has an error. Make sure they all load correctly."), 
					_ => throw new InvalidOperationException(), 
				};
				cells.Add(new ModCellEntry
				{
					Title = mod.FileName,
					DetailText = errorText,
					Enabled = !disabledMods.Contains(mod.Info?.ModID + "@" + mod.Info?.Version),
					Mod = mod,
					DetailTextFont = font
				});
				continue;
			}
			StringBuilder descriptionBuilder = new StringBuilder();
			if (modinfo.Authors.Count > 0)
			{
				descriptionBuilder.AppendLine(string.Join(", ", modinfo.Authors));
			}
			if (!string.IsNullOrEmpty(modinfo.Description))
			{
				descriptionBuilder.AppendLine(modinfo.Description);
			}
			cells.Add(new ModCellEntry
			{
				Title = modinfo.Name + " (" + modinfo.Type.ToString() + ")",
				RightTopText = ((!string.IsNullOrEmpty(modinfo.Version)) ? modinfo.Version : "--"),
				RightTopOffY = 3f,
				DetailText = descriptionBuilder.ToString().Trim(),
				Enabled = !disabledMods.Contains(mod.Info.ModID + "@" + mod.Info.Version),
				Mod = mod,
				DetailTextFont = font
			});
		}
		return cells;
	}

	private void InitGui()
	{
		int windowHeight = ScreenManager.GamePlatform.WindowSize.Height;
		int windowWidth = ScreenManager.GamePlatform.WindowSize.Width;
		ElementBounds buttonBounds = ElementBounds.FixedSize(60.0, 30.0).WithFixedPadding(10.0, 2.0).WithAlignment(EnumDialogArea.RightFixed);
		ElementBounds titleBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 35.0);
		_ = Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale;
		float height = (float)Math.Max(300, windowHeight) / ClientSettings.GUIScale;
		ElementComposer?.Dispose();
		ElementBounds insetBounds;
		ElementComposer = dialogBase("mainmenu-mods").AddStaticText(Lang.Get("Installed mods"), CairoFont.WhiteSmallishText(), ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 40.0)).AddInset(insetBounds = titleBounds.BelowCopy(0.0, 3.0).WithFixedSize(Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale, height - 190f)).AddVerticalScrollbar(OnNewScrollbarvalue, ElementStdBounds.VerticalScrollbar(insetBounds), "scrollbar")
			.BeginClip(clippingBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0))
			.AddCellList(listBounds = clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0), createCellElem, LoadModCells(), "modstable")
			.EndClip()
			.AddSmallButton(Lang.Get("Reload Mods"), OnReloadMods, buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0).WithFixedAlignmentOffset(-13.0, 0.0))
			.AddSmallButton(Lang.Get("Open Mods Folder"), OnOpenModsFolder, buttonBounds.FlatCopy().FixedUnder(insetBounds, 10.0).WithFixedAlignmentOffset(-150.0, 0.0))
			.EndChildElements()
			.Compose();
		listBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)listBounds.fixedHeight);
	}

	private bool OnBrowseOnlineMods()
	{
		ScreenManager.LoadScreen(new GuiScreenOnlineMods(ScreenManager, this));
		return true;
	}

	private IGuiElementCell createCellElem(ModCellEntry cell, ElementBounds bounds)
	{
		return new GuiElementModCell(ScreenManager.api, cell, bounds, warningIcon)
		{
			On = cell.Enabled,
			OnMouseDownOnCellLeft = OnClickCellLeft,
			OnMouseDownOnCellRight = OnClickCellRight
		};
	}

	private bool OnReloadMods()
	{
		ScreenManager.loadMods();
		OnScreenLoaded();
		return true;
	}

	private bool OnOpenModsFolder()
	{
		NetUtil.OpenUrlInBrowser(GamePaths.DataPathMods);
		return true;
	}

	private void OnClickCellRight(int cellIndex)
	{
		GuiElementModCell guicell = (GuiElementModCell)ElementComposer.GetCellList<ModCellEntry>("modstable").elementCells[cellIndex];
		ModContainer mod = guicell.cell.Mod;
		if (mod.Info != null && mod.Info.CoreMod && mod.Status == ModStatus.Enabled)
		{
			ShowConfirmationDialog(guicell, mod);
		}
		else
		{
			SwitchModStatus(guicell, mod);
		}
	}

	private void SwitchModStatus(GuiElementModCell guicell, ModContainer mod)
	{
		guicell.On = !guicell.On;
		if (mod.Status == ModStatus.Enabled || mod.Status == ModStatus.Disabled)
		{
			mod.Status = (guicell.On ? ModStatus.Enabled : ModStatus.Disabled);
		}
		List<string> disabledMods = ClientSettings.DisabledMods;
		if (mod.Info != null)
		{
			disabledMods.Remove(mod.Info.ModID + "@" + mod.Info.Version);
			if (!guicell.On)
			{
				disabledMods.Add(mod.Info.ModID + "@" + mod.Info.Version);
			}
			ClientSettings.DisabledMods = disabledMods;
			ClientSettings.Inst.Save(force: true);
		}
	}

	private void ShowConfirmationDialog(GuiElementModCell guicell, ModContainer mod)
	{
		screenManager.LoadScreen(new GuiScreenConfirmAction("coremod-warningtitle", Lang.Get("coremod-warning", mod.Info.Name), "general-back", "Confirm", delegate(bool val)
		{
			if (val)
			{
				SwitchModStatus(guicell, mod);
			}
			screenManager.LoadScreen(this);
		}, screenManager, this, "coremod-confirmation"));
	}

	private void OnClickCellLeft(int cellIndex)
	{
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = ElementComposer.GetCellList<ModCellEntry>("modstable").Bounds;
		bounds.fixedY = 0f - value;
		bounds.CalcWorldBounds();
	}
}
