using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace Vintagestory.Client;

internal class GuiScreenGetUpdate : GuiScreen
{
	private CancellationTokenSource _gameReleaseVersionCts;

	private CancellationTokenSource _gameReleaseDownloadCts;

	private string versionnumber;

	private Dictionary<string, GameReleaseVersion> releases;

	public GuiScreenGetUpdate(string versionnumber, ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		this.versionnumber = versionnumber;
		ShowMainMenu = false;
		Compose();
		BeginDownload();
	}

	private void Compose()
	{
		CairoFont.WhiteSmallText().WithFontSize(17f).WithLineHeightMultiplier(1.25);
		ElementBounds titleBounds = ElementBounds.Fixed(0.0, 0.0, 500.0, 50.0);
		ElementBounds btnBounds = ElementBounds.Fixed(0.0, 90.0, 0.0, 0.0).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPadding(10.0, 2.0);
		ElementComposer = dialogBase("mainmenu-confirmaction", -1.0, 160.0).AddStaticText(Lang.Get("Download in progress"), CairoFont.WhiteSmallishText().WithWeight(FontWeight.Bold), titleBounds).AddDynamicText("Downloading releases meta information...", CairoFont.WhiteSmallText(), titleBounds = titleBounds.BelowCopy(0.0, 10.0).WithFixedWidth(500.0), "status").AddButton(Lang.Get("Cancel"), OnCancel, btnBounds.FixedUnder(titleBounds, 10.0))
			.EndChildElements()
			.Compose();
	}

	private bool OnCancel()
	{
		_gameReleaseVersionCts.Cancel();
		ScreenManager.StartMainMenu();
		return false;
	}

	private void BeginDownload()
	{
		Task.Run(async delegate
		{
			ScreenManager.Platform.Logger.Notification("Retrieving releases meta data");
			try
			{
				_gameReleaseVersionCts = new CancellationTokenSource();
				releases = JsonUtil.FromString<Dictionary<string, GameReleaseVersion>>(await VSWebClient.Inst.GetStringAsync("http://api.vintagestory.at/stable-unstable.json", _gameReleaseVersionCts.Token));
				onReleasesDownloadComplete(null);
			}
			catch (Exception e)
			{
				onReleasesDownloadComplete(e);
			}
		});
	}

	private void onReleasesDownloadComplete(Exception errorExc)
	{
		if (errorExc == null)
		{
			downloadInstaller(releases[versionnumber]);
			return;
		}
		ScreenManager.EnqueueMainThreadTask(delegate
		{
			ElementComposer.GetDynamicText("status").SetNewText("Download failed: " + errorExc);
		});
	}

	private void downloadInstaller(GameReleaseVersion release)
	{
		if (release == null)
		{
			ElementComposer.GetDynamicText("status").SetNewText(Lang.Get("Download failed. Release {0} not found, possibly programming error. Please send us a bug report", ScreenManager.newestVersion));
			return;
		}
		GameBuild build = release.WindowsUpdate ?? release.Windows;
		string dstPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), build.filename);
		FileStream fileStream = File.Create(dstPath);
		_gameReleaseDownloadCts = new CancellationTokenSource();
		Progress<Tuple<int, long>> progress = new Progress<Tuple<int, long>>();
		progress.ProgressChanged += onProgress;
		Task.Run(async delegate
		{
			try
			{
				await VSWebClient.Inst.DownloadAsync(build.urls["cdn"], fileStream, progress, _gameReleaseDownloadCts.Token);
				fileStream.Close();
				Process.Start(dstPath, "/SILENT");
				ScreenManager.GamePlatform.WindowExit("Installing update");
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Exception e = ex2;
				ScreenManager.Platform.Logger.Error(e);
				ScreenManager.EnqueueMainThreadTask(delegate
				{
					ElementComposer.GetDynamicText("status").SetNewText("Download failed: " + e.Message);
				});
			}
		});
	}

	private void onProgress(object sender, Tuple<int, long> progress)
	{
		ScreenManager.EnqueueMainThreadTask(delegate
		{
			ElementComposer.GetDynamicText("status").SetNewText(Lang.Get("{0:0.#}% complete ({1:0.0} of {2:0.#} MB)", (int)(100.0 * (double)progress.Item1 / (double)progress.Item2), (double)progress.Item1 / 1024.0 / 1024.0, (double)progress.Item2 / 1024.0 / 1024.0));
		});
	}
}
