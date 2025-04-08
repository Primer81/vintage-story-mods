using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.ModDb;

public class ModDbUtil
{
	private string installPath;

	private string modDbApiUrl;

	private string modDbDownloadUrl;

	private ICoreAPI api;

	private string cmdLetter;

	private GameVersionResponse gameversions;

	public int selfGameVersionId = -1;

	public int[] sameMinorVersionIds = new int[0];

	private List<ModContainer> mods;

	public bool IsLoading { get; private set; }

	public ModDbUtil(ICoreAPI api, string modDbUrl, string installPath)
	{
		this.api = api;
		modDbApiUrl = modDbUrl + "api/";
		modDbDownloadUrl = modDbUrl;
		this.installPath = installPath;
		cmdLetter = ((api.Side == EnumAppSide.Client) ? "." : "/");
	}

	private void ensureModsLoaded()
	{
		if (mods == null)
		{
			ModLoader modloader = api.ModLoader as ModLoader;
			mods = modloader.LoadModInfos();
		}
	}

	public string preConsoleCommand()
	{
		if (gameversions == null)
		{
			string result = null;
			modDbRequest("gameversions", delegate(EnumModDbResponse state, string text)
			{
				switch (state)
				{
				case EnumModDbResponse.Good:
				{
					gameversions = parseResponse<GameVersionResponse>(text, out var errorText);
					if (errorText != null)
					{
						result = errorText;
					}
					else if (gameversions != null)
					{
						loadVersionIds();
						result = null;
					}
					else
					{
						result = "Bad moddb response - no game versions";
					}
					break;
				}
				case EnumModDbResponse.Offline:
					result = "Mod hub offline";
					break;
				default:
					result = "Bad moddb response - " + text;
					break;
				}
			});
			return result;
		}
		return null;
	}

	public void onInstallCommand(string modid, string forGameVersion, Action<string> onProgressUpdate)
	{
		ensureModsLoaded();
		SearchAndInstall(modid, forGameVersion ?? "1.20.7", delegate(string msg, EnumModInstallState state)
		{
			onProgressUpdate(msg);
		}, deletedOutdated: true);
	}

	public void onRemoveCommand(string modid, Action<string> onProgressUpdate)
	{
		ensureModsLoaded();
		foreach (ModContainer val in mods)
		{
			if (val.Status != ModStatus.Errored && val.Info.ModID == modid)
			{
				File.Delete(val.SourcePath);
				onProgressUpdate("modutil-modremoved");
				return;
			}
		}
		onProgressUpdate("No such mod found.");
	}

	public void onListCommand(Action<string> onProgressUpdate)
	{
		ensureModsLoaded();
		List<string> modids = new List<string>();
		foreach (ModContainer val in mods)
		{
			if (val.Status != ModStatus.Errored && val.Info.ModID != "game" && val.Info.ModID != "creative" && val.Info.ModID != "survival")
			{
				modids.Add(val.Info.ModID);
			}
		}
		if (modids.Count == 0)
		{
			onProgressUpdate(Lang.Get("modutil-list-none"));
			return;
		}
		onProgressUpdate(Lang.Get("modutil-list", modids.Count, string.Join(", ", modids)));
	}

	public void onSearchforCommand(string version, string modid, Action<string> onProgressUpdate)
	{
		ensureModsLoaded();
		int verid = -1;
		GameVersionEntry[] gameVersions = gameversions.GameVersions;
		foreach (GameVersionEntry val in gameVersions)
		{
			if (val.Name == "v" + version)
			{
				verid = val.TagId;
			}
		}
		if (verid <= 0)
		{
			onProgressUpdate("No such version is listed on the moddb");
			return;
		}
		int[] verids = new int[1] { verid };
		search(modid, onProgressUpdate, verids);
	}

	public void onSearchforAndCompatibleCommand(string version, string modid, Action<string> onProgressUpdate)
	{
		ensureModsLoaded();
		string major = version.Substring(0, 1);
		string minor = version.Substring(2, 3);
		List<int> sameminvids = new List<int>();
		GameVersionEntry[] gameVersions = gameversions.GameVersions;
		foreach (GameVersionEntry val in gameVersions)
		{
			if (val.Name.StartsWithOrdinal("v" + major + "." + minor))
			{
				sameminvids.Add(val.TagId);
			}
		}
		int[] verids = sameminvids.ToArray();
		search(modid, onProgressUpdate, verids);
	}

	public void onSearchCommand(string modid, Action<string> onProgressUpdate)
	{
		ensureModsLoaded();
		search(modid, onProgressUpdate, new int[1] { selfGameVersionId });
	}

	public void onSearchCompatibleCommand(string modid, Action<string> onProgressUpdate)
	{
		ensureModsLoaded();
		search(modid, onProgressUpdate, sameMinorVersionIds);
	}

	public void SearchAndInstall(string modid, string forGameVersion, ModInstallProgressUpdate onDone, bool deletedOutdated)
	{
		ensureModsLoaded();
		string[] modidparts = modid.Split('@');
		api.Logger.Debug("ModDbUtil.SearchAndInstall(): Request to mod/" + modidparts[0]);
		modDbRequest("mod/" + modidparts[0], delegate(EnumModDbResponse state, string text)
		{
			api.Logger.Debug("ModDbUtil.SearchAndInstall(): Response: {0}", text);
			switch (state)
			{
			case EnumModDbResponse.Good:
			{
				string errorText;
				ModDbEntryResponse modDbEntryResponse = parseResponse<ModDbEntryResponse>(text, out errorText);
				if (errorText != null)
				{
					if (modDbEntryResponse != null && modDbEntryResponse.StatusCode == 404)
					{
						onDone(Lang.Get("modinstall-notfound", modid), EnumModInstallState.NotFound);
					}
					else
					{
						onDone(errorText, EnumModInstallState.Error);
					}
				}
				else if (api is ICoreServerAPI coreServerAPI && coreServerAPI.Server.Config.HostedMode)
				{
					if (coreServerAPI.Server.Config.HostedModeAllowMods && modDbEntryResponse.Mod.Releases.Any((ModEntryRelease r) => r.HostedModeAllow))
					{
						modDbEntryResponse.Mod.Releases = modDbEntryResponse.Mod.Releases.Where((ModEntryRelease r) => r.HostedModeAllow).ToArray();
						installMod(modDbEntryResponse, onDone, forGameVersion, deletedOutdated, modid);
					}
					else
					{
						onDone(Lang.Get("modinstall-notallowed", modid), EnumModInstallState.Error);
					}
				}
				else
				{
					installMod(modDbEntryResponse, onDone, forGameVersion, deletedOutdated, modid);
				}
				break;
			}
			case EnumModDbResponse.Offline:
				onDone(Lang.Get("modinstall-offline", modid), EnumModInstallState.Offline);
				break;
			default:
				onDone(Lang.Get("modinstall-badresponse", modid, text), EnumModInstallState.Error);
				break;
			}
		});
	}

	private void loadVersionIds()
	{
		List<int> sameMinorVersionIds = new List<int>();
		string major = "1.20.7".Substring(0, 1);
		string minor = "1.20.7".Substring(2, 3);
		string shortVersion = "v1.20.7";
		string longVersion = "v" + major + "." + minor;
		GameVersionEntry[] gameVersions = gameversions.GameVersions;
		foreach (GameVersionEntry val in gameVersions)
		{
			if (val.Name == shortVersion)
			{
				selfGameVersionId = val.TagId;
			}
			if (val.Name.StartsWithOrdinal(longVersion))
			{
				sameMinorVersionIds.Add(val.TagId);
			}
		}
		this.sameMinorVersionIds = sameMinorVersionIds.ToArray();
	}

	private void search(string stext, Action<string> onDone, int[] gameversionIds)
	{
		if (stext == null)
		{
			onDone("Syntax: " + cmdLetter + "moddb search [text]");
			return;
		}
		Search(stext, delegate(ModSearchResult searchResult)
		{
			if (searchResult.Mods == null)
			{
				onDone(searchResult.StatusMessage);
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder();
				if (searchResult.Mods.Length == 0)
				{
					stringBuilder.AppendLine(Lang.Get("Found no mods compatible for your game version"));
				}
				else
				{
					stringBuilder.AppendLine(Lang.Get("Found {0} compatible mods. Name and mod id:", searchResult.Mods.Length));
				}
				int num = 0;
				ModDbEntrySearchResponse[] array = searchResult.Mods;
				foreach (ModDbEntrySearchResponse modDbEntrySearchResponse in array)
				{
					stringBuilder.AppendLine(Lang.Get("{0}: <strong>{1}</strong>", modDbEntrySearchResponse.Name, modDbEntrySearchResponse.ModIdStrs[0]));
					num++;
					if (num > 10)
					{
						stringBuilder.AppendLine("and more...");
						break;
					}
				}
				onDone(stringBuilder.ToString());
			}
		}, gameversionIds);
	}

	public void Search(string stext, Action<ModSearchResult> onDone, int[] gameversionIds, string mv = null, string sortBy = null, int limit = 100)
	{
		List<string> getParams = new List<string>();
		for (int i = 0; i < gameversionIds.Length; i++)
		{
			int tagid = gameversionIds[i];
			if (tagid != -1)
			{
				getParams.Add("gv[]=" + tagid);
			}
		}
		getParams.Add("text=" + stext);
		if (mv != null)
		{
			getParams.Add("mv=" + mv);
		}
		if (sortBy != null)
		{
			getParams.Add("sortby=" + sortBy);
		}
		getParams.Add("limit=" + limit);
		modDbRequest("mods?" + string.Join("&", getParams), delegate(EnumModDbResponse state, string text)
		{
			switch (state)
			{
			case EnumModDbResponse.Good:
			{
				string errorText;
				ModSearchResult modSearchResult = parseResponse<ModSearchResult>(text, out errorText);
				if (errorText != null)
				{
					onDone(new ModSearchResult
					{
						StatusCode = 500,
						StatusMessage = errorText
					});
				}
				else
				{
					modSearchResult.Mods = modSearchResult.Mods.Where((ModDbEntrySearchResponse m) => m.Type.Equals("mod")).ToArray();
					onDone(modSearchResult);
				}
				break;
			}
			case EnumModDbResponse.Offline:
				onDone(new ModSearchResult
				{
					StatusCode = 500,
					StatusMessage = Lang.Get("Mod hub offline")
				});
				break;
			default:
				onDone(new ModSearchResult
				{
					StatusCode = 500,
					StatusMessage = Lang.Get("Bad moddb response - {0}", text)
				});
				break;
			}
		});
	}

	private void installMod(ModDbEntryResponse modentry, ModInstallProgressUpdate onProgressUpdate, string forGameVersion, bool deleteOutdated, string installExactVer = null)
	{
		ModEntryRelease selectedRelease = null;
		string installExactModVersion = null;
		if (installExactVer != null)
		{
			string[] modidparts = installExactVer?.Split('@');
			installExactModVersion = ((modidparts.Length > 1) ? modidparts[1] : null);
			onProgressUpdate(Lang.Get("Checking {0}...", installExactVer) + " ", EnumModInstallState.InProgress);
		}
		else
		{
			onProgressUpdate(Lang.Get("Checking {0}...", modentry.Mod.Name) + " ", EnumModInstallState.InProgress);
		}
		if (installExactModVersion != null)
		{
			ModEntryRelease[] releases = modentry.Mod.Releases;
			foreach (ModEntryRelease release in releases)
			{
				if (release.ModVersion == installExactModVersion)
				{
					selectedRelease = release;
				}
			}
			if (selectedRelease == null)
			{
				onProgressUpdate(Lang.Get("modinstall-versionnotfound", modentry.Mod.Name, installExactModVersion), EnumModInstallState.NotFound);
				return;
			}
		}
		else
		{
			List<ModEntryRelease> compaReleases = new List<ModEntryRelease>();
			HashSet<string> gameVersions = new HashSet<string>();
			ModEntryRelease[] releases = modentry.Mod.Releases;
			foreach (ModEntryRelease release2 in releases)
			{
				if (release2.Tags.Contains(forGameVersion) || release2.Tags.Contains<string>("v" + forGameVersion))
				{
					compaReleases.Add(release2);
				}
				string[] tags = release2.Tags;
				foreach (string tag in tags)
				{
					gameVersions.Add(tag.Substring(1));
				}
			}
			if (compaReleases.Count == 0)
			{
				onProgressUpdate(Lang.Get("mod-outdated-notavailable", string.Join(", ", gameVersions), cmdLetter, modentry.Mod.Releases[0].ModIdStr), EnumModInstallState.TooOld);
				return;
			}
			compaReleases.Sort((ModEntryRelease mod1, ModEntryRelease mod2) => (!(mod1.ModVersion == mod2.ModVersion)) ? ((!GameVersion.IsNewerVersionThan(mod1.ModVersion, mod2.ModVersion)) ? 1 : (-1)) : 0);
			selectedRelease = compaReleases[0];
		}
		foreach (ModContainer mod3 in mods)
		{
			if (mod3.Enabled && mod3.Info.ModID == selectedRelease.ModIdStr)
			{
				if (mod3.Info.Version == selectedRelease.ModVersion)
				{
					onProgressUpdate(Lang.Get("mod-installed-willenable"), EnumModInstallState.InstalledOrReady);
					List<string> disabledMods = ClientSettings.DisabledMods;
					disabledMods.Remove(mod3.Info.ModID + "@" + mod3.Info.Version);
					ClientSettings.DisabledMods = disabledMods;
					ClientSettings.Inst.Save(force: true);
					return;
				}
				if (deleteOutdated)
				{
					onProgressUpdate(Lang.Get("{0} v{1} is already installed, which is outdated. Will delete it.", modentry.Mod.Name, mod3.Info.Version), EnumModInstallState.InstalledOrReady);
					File.Delete(mod3.SourcePath);
				}
			}
		}
		onProgressUpdate(Lang.Get("found! Downloading..."), EnumModInstallState.InProgress);
		Console.WriteLine(Lang.Get("Downloading {0}...", selectedRelease.Filename) + " ");
		string filepath = Path.Combine(installPath, selectedRelease.Filename);
		GamePaths.EnsurePathExists(installPath);
		try
		{
			using Stream streamAsync = VSWebClient.Inst.GetStreamAsync(new Uri(modDbDownloadUrl + "download?fileid=" + selectedRelease.Fileid)).Result;
			using FileStream fileStream = new FileStream(filepath, FileMode.Create);
			streamAsync.CopyTo(fileStream);
			onProgressUpdate(Lang.Get("mod-successfully-downloaded", new FileInfo(filepath).Length / 1024), EnumModInstallState.InstalledOrReady);
		}
		catch (Exception e)
		{
			onProgressUpdate("Failed to download mod " + selectedRelease.Filename + " | " + e.Message, EnumModInstallState.Error);
		}
	}

	private void modDbRequest(string action, ModDbResponseDelegate onComplete, FormUrlEncodedContent postData = null)
	{
		IsLoading = true;
		Uri uri = new Uri(modDbApiUrl + action);
		api.Logger.Notification("Send request: {0}", action);
		VSWebClient.Inst.PostAsync(uri, postData, delegate(CompletedArgs args)
		{
			api.Event.EnqueueMainThreadTask(delegate
			{
				if (args.State != 0)
				{
					onComplete(EnumModDbResponse.Offline, null);
				}
				else if (args.Response == null)
				{
					onComplete(EnumModDbResponse.Bad, null);
				}
				else
				{
					IsLoading = false;
					onComplete(EnumModDbResponse.Good, args.Response);
				}
			}, "moddbrequest");
		});
	}

	public T parseResponse<T>(string text, out string errorText) where T : ModDbResponse
	{
		errorText = null;
		T response;
		try
		{
			response = JsonConvert.DeserializeObject<T>(text);
		}
		catch (Exception e)
		{
			api.Logger.Notification("{0}", e);
			errorText = LoggerBase.CleanStackTrace(e.ToString());
			return null;
		}
		if (response.StatusCode != 200)
		{
			errorText = "Invalid request - " + response.StatusCode;
		}
		return response;
	}
}
