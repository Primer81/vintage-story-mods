using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class AuthServerComm
{
	public static void ValidatePlayerWithServer(string mptokenv2, string playerName, string playerUID, string serverLoginToken, ValidationCompleteDelegate OnValidationComplete)
	{
		FormUrlEncodedContent postData = new FormUrlEncodedContent(new KeyValuePair<string, string>[4]
		{
			new KeyValuePair<string, string>("mptokenv2", mptokenv2),
			new KeyValuePair<string, string>("uid", playerUID),
			new KeyValuePair<string, string>("serverlogintoken", serverLoginToken),
			new KeyValuePair<string, string>("serverversion", "1.20.7")
		});
		Uri uri = new Uri("https://auth3.vintagestory.at/v2/servervalidate");
		VSWebClient.Inst.PostAsync(uri, postData, delegate(CompletedArgs args)
		{
			ServerMain.Logger.Debug("Response from auth server: {0}", args.Response);
			if (args.State != 0)
			{
				ServerMain.Logger.Warning("Unable to connect to auth server: State {0}, Error msg '{1}'", args.State, args.ErrorMessage);
				OnValidationComplete(EnumServerResponse.Offline, null, null);
			}
			else
			{
				ValidateResponse validateResponse = JsonConvert.DeserializeObject<ValidateResponse>(args.Response);
				if (validateResponse.valid == 1 && validateResponse.playername == playerName)
				{
					OnValidationComplete(EnumServerResponse.Good, validateResponse.entitlements, null);
				}
				else
				{
					OnValidationComplete(EnumServerResponse.Bad, validateResponse.entitlements, validateResponse.reason);
				}
			}
		});
	}

	public static void ResolvePlayerName(string playername, Action<EnumServerResponse, string> OnResolveComplete)
	{
		FormUrlEncodedContent postData = new FormUrlEncodedContent(new KeyValuePair<string, string>[1]
		{
			new KeyValuePair<string, string>("playername", playername)
		});
		Uri uri = new Uri("https://auth3.vintagestory.at/resolveplayername");
		VSWebClient.Inst.PostAsync(uri, postData, delegate(CompletedArgs args)
		{
			if (args.State != 0)
			{
				OnResolveComplete(EnumServerResponse.Offline, null);
			}
			else
			{
				ServerMain.Logger.Debug("Response from auth server: {0}", args.Response);
				ResolveResponse resolveResponse = JsonConvert.DeserializeObject<ResolveResponse>(args.Response);
				if (resolveResponse.playeruid == null)
				{
					OnResolveComplete(EnumServerResponse.Bad, null);
				}
				else
				{
					OnResolveComplete(EnumServerResponse.Good, resolveResponse.playeruid);
				}
			}
		});
	}

	public static void ResolvePlayerUid(string playeruid, Action<EnumServerResponse, string> OnResolveComplete)
	{
		FormUrlEncodedContent postData = new FormUrlEncodedContent(new KeyValuePair<string, string>[1]
		{
			new KeyValuePair<string, string>("uid", playeruid)
		});
		Uri uri = new Uri("https://auth3.vintagestory.at/resolveplayeruid");
		VSWebClient.Inst.PostAsync(uri, postData, delegate(CompletedArgs args)
		{
			if (args.State != 0)
			{
				OnResolveComplete(EnumServerResponse.Offline, null);
			}
			else
			{
				ServerMain.Logger.Debug("Response from auth server: {0}", args.Response);
				ResolveResponseUid resolveResponseUid = JsonConvert.DeserializeObject<ResolveResponseUid>(args.Response);
				if (resolveResponseUid.playername == null)
				{
					OnResolveComplete(EnumServerResponse.Bad, null);
				}
				else
				{
					OnResolveComplete(EnumServerResponse.Good, resolveResponseUid.playername);
				}
			}
		});
	}
}
