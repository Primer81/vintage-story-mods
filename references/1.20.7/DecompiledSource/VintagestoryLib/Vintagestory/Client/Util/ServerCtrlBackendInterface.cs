using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client.Util;

public class ServerCtrlBackendInterface
{
	public bool IsLoading;

	public VSWebClient webClient = new VSWebClient
	{
		Timeout = TimeSpan.FromSeconds(60.0)
	};

	public void Start(OnSrvActionComplete<ServerCtrlResponse> onComplete)
	{
		runAction(onComplete, "start");
	}

	public void Stop(OnSrvActionComplete<ServerCtrlResponse> onComplete)
	{
		runAction(onComplete, "stop");
	}

	public void ForceStop(OnSrvActionComplete<ServerCtrlResponse> onComplete)
	{
		runAction(onComplete, "forcestop");
	}

	public void DeleteSaves(OnSrvActionComplete<GameServerStatus> onComplete)
	{
		runAction(onComplete, "clearsaves");
	}

	public void DeleteAll(OnSrvActionComplete<GameServerStatus> onComplete)
	{
		runAction(onComplete, "deleteall");
	}

	public void GetLog(OnSrvActionComplete<GameServerLogResponse> onComplete)
	{
		runAction(onComplete, "getlog");
	}

	public void GetGameVersions(OnActionComplete<string[]> onComplete)
	{
		try
		{
			JObject jObject = JObject.Parse(webClient.GetStringAsync("http://api.vintagestory.at/stable-unstable.json").Result);
			List<string> versions = new List<string>();
			foreach (KeyValuePair<string, JToken> val in jObject)
			{
				if (GameVersion.IsAtLeastVersion(val.Key, "1.14.9"))
				{
					versions.Add(val.Key);
				}
			}
			GetVSHostingUnsupportedGameVersions(delegate(EnumAuthServerResponse status, string[] unsupversions)
			{
				if (status != 0)
				{
					throw new Exception("Unable to load unsupported versions");
				}
				foreach (string item in unsupversions)
				{
					versions.Remove(item);
				}
				ScreenManager.EnqueueMainThreadTask(delegate
				{
					onComplete(EnumAuthServerResponse.Good, versions.ToArray());
				});
			});
		}
		catch (Exception)
		{
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				onComplete(EnumAuthServerResponse.Bad, null);
			});
		}
	}

	public void GetVSHostingUnsupportedGameVersions(OnActionComplete<string[]> onComplete)
	{
		try
		{
			JObject jObject = JObject.Parse(webClient.GetStringAsync("http://api.vintagestory.at/vshostingunsupported.json").Result);
			List<string> versions = new List<string>();
			foreach (JToken val in (IEnumerable<JToken>)jObject["versions"])
			{
				versions.Add(val.ToString());
			}
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				onComplete(EnumAuthServerResponse.Good, versions.ToArray());
			});
		}
		catch (Exception)
		{
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				onComplete(EnumAuthServerResponse.Bad, null);
			});
		}
	}

	public void RequestDownload(OnSrvActionComplete<GameServerStatus> onComplete)
	{
		runAction(delegate(EnumAuthServerResponse status, GameServerStatus response)
		{
			onComplete(status, response);
		}, "downloadsaves");
	}

	public void GetStatus(OnSrvActionComplete<GameServerStatus> onComplete)
	{
		runAction(delegate(EnumAuthServerResponse status, GameServerStatus response)
		{
			onComplete(status, response);
		}, "status");
	}

	public void GetConfig(OnSrvActionComplete<GameServerConfigResponse> onComplete)
	{
		runAction(onComplete, "getconfig");
	}

	public void SetConfig(OnSrvActionComplete<GameServerStatus> onComplete, string serverconfig, string worldconfig)
	{
		List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();
		postData.Add(new KeyValuePair<string, string>("serverconfig", serverconfig));
		postData.Add(new KeyValuePair<string, string>("worldconfig", worldconfig));
		runAction(onComplete, "setconfig", postData);
	}

	public void SelectRegion(string region, OnSrvActionComplete<ServerCtrlResponse> onComplete)
	{
		List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();
		postData.Add(new KeyValuePair<string, string>("region", region));
		runAction(onComplete, "selectregion", postData);
	}

	public void SelectVersion(string version, OnSrvActionComplete<ServerCtrlResponse> onComplete)
	{
		List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();
		postData.Add(new KeyValuePair<string, string>("version", version));
		runAction(onComplete, "install", postData);
	}

	public void DeleteMod(string mod, OnSrvActionComplete<ServerCtrlResponse> onComplete)
	{
		List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();
		postData.Add(new KeyValuePair<string, string>("mod", mod));
		runAction(onComplete, "deletemod", postData);
	}

	public void DeleteAllMods(OnSrvActionComplete<ServerCtrlResponse> onComplete)
	{
		runAction(onComplete, "deleteallmods");
	}

	private void runAction<T>(OnSrvActionComplete<T> onComplete, string action, List<KeyValuePair<string, string>> postData = null) where T : ServerCtrlResponse
	{
		IsLoading = true;
		if (postData == null)
		{
			postData = new List<KeyValuePair<string, string>>();
		}
		postData.Add(new KeyValuePair<string, string>("action", action));
		postData.Add(new KeyValuePair<string, string>("uid", ClientSettings.PlayerUID));
		postData.Add(new KeyValuePair<string, string>("sessionkey", ClientSettings.Sessionkey));
		FormUrlEncodedContent formContent = new FormUrlEncodedContent(postData);
		Uri uri = new Uri("https://auth3.vintagestory.at/v2/gameserverctrl");
		ScreenManager.Platform.Logger.Notification("Send request: {0}", action);
		webClient.PostAsync(uri, formContent, delegate(CompletedArgs args)
		{
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				if (args.State != 0)
				{
					onComplete(EnumAuthServerResponse.Offline, null);
				}
				else
				{
					ScreenManager.Platform.Logger.Notification("Response {0}: {1}", args.StatusCode, args.Response);
					if (args.Response == null)
					{
						onComplete(EnumAuthServerResponse.Bad, null);
					}
					else
					{
						IsLoading = false;
						T val;
						try
						{
							val = JsonConvert.DeserializeObject<T>(args.Response);
						}
						catch (Exception ex)
						{
							ScreenManager.Platform.Logger.Notification(LoggerBase.CleanStackTrace(ex.ToString()));
							onComplete(EnumAuthServerResponse.Bad, null);
							return;
						}
						if (val != null && val.Valid == 1)
						{
							onComplete(EnumAuthServerResponse.Good, val);
						}
						else
						{
							ScreenManager.Platform.Logger.Notification("Response is bad. Valid flag not set.");
							onComplete(EnumAuthServerResponse.Bad, val);
						}
					}
				}
			});
		});
	}
}
