using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using DnsClient;
using DnsClient.Protocol;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class ServerConnectData
{
	public string HostRaw;

	public string Host;

	public int Port;

	public string ServerPassword;

	public bool IsServePasswordProtected;

	public string ErrorMessage;

	public int PositionInQueue;

	public bool Connected;

	public string PlayerUID => ClientSettings.PlayerUID;

	public string PlayerName => ClientSettings.PlayerName;

	public string MpToken => ClientSettings.MpToken;

	public static ServerConnectData FromHost(string host)
	{
		ServerConnectData cd = new ServerConnectData();
		cd.HostRaw = host;
		if (host.StartsWithOrdinal("vh-"))
		{
			host = ResolveConnectionString(host);
			if (host.Length == 0 || host == ":")
			{
				throw new Exception("Invalid Vintagehosting address '" + cd.HostRaw + "' - no such server exists. Please double check that you entered the correct address");
			}
		}
		string error;
		UriInfo info = NetUtil.getUriInfo(host, out error);
		ScreenManager.Platform.Logger.Notification("Connecting to " + info.Hostname + "...");
		if (!info.Port.HasValue && !NetUtil.IsPrivateIp(info.Hostname))
		{
			try
			{
				LookupClient lookupClient = new LookupClient(new LookupClientOptions
				{
					UseCache = true,
					Timeout = new TimeSpan(0, 0, 4),
					Retries = 2
				});
				if (lookupClient.NameServers.Count == 0)
				{
					throw new Exception("No name servers found - Please make sure you are connected to the internet.");
				}
				IDnsQueryResponse result = lookupClient.Query("_vintagestory._tcp." + host, QueryType.SRV);
				if (!result.HasError)
				{
					SrvRecord srvRecord = result.Answers.OfType<SrvRecord>().FirstOrDefault();
					if (srvRecord != null)
					{
						info.Port = srvRecord.Port;
						if (srvRecord.Target?.Value != null)
						{
							info.Hostname = srvRecord.Target.Value;
						}
						ScreenManager.Platform.Logger.Notification("SRV record found - port " + srvRecord.Port + ", target " + srvRecord.Target.Value);
					}
					else
					{
						ScreenManager.Platform.Logger.Notification("No SRV record found, will connect to supplied hostname");
					}
				}
				else
				{
					ScreenManager.Platform.Logger.Error("Unable to read srv record, will connect to supplied hostname. Error: " + result.ErrorMessage + "\r\n" + result.Header);
				}
			}
			catch (Exception e)
			{
				ScreenManager.Platform.Logger.Error("Exception thrown during SRV record lookup on {0}. Will ignore SRV record.", host);
				ScreenManager.Platform.Logger.Error(e);
			}
		}
		cd.ErrorMessage = error;
		cd.Port = ((!info.Port.HasValue) ? 42420 : info.Port.Value);
		cd.Host = info.Hostname;
		cd.ServerPassword = info.Password;
		cd.IsServePasswordProtected = info.Password != null;
		return cd;
	}

	private static string ResolveConnectionString(string host)
	{
		FormUrlEncodedContent postData = new FormUrlEncodedContent(new KeyValuePair<string, string>[1]
		{
			new KeyValuePair<string, string>("host", host)
		});
		Uri uri = new Uri("https://auth3.vintagestory.at/resolveserverhost");
		ServerHostResolveResp resp = JsonUtil.FromString<ServerHostResolveResp>(VSWebClient.Inst.Post(uri, postData));
		if (resp.Host == null || resp.Host.Length == 0)
		{
			throw new ArgumentException("Sorry, no such vintagehosting server known");
		}
		if (resp.Status == "expired")
		{
			throw new ArgumentException("Sorry, this vintagehosting server is expired, the owner needs to purchase more server time.");
		}
		return resp.Host;
	}
}
