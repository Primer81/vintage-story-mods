using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ServerSystemHeartbeat : ServerSystem
{
	private string token;

	private bool upnpComplete;

	public override int GetUpdateInterval()
	{
		return 120000;
	}

	public ServerSystemHeartbeat(ServerMain server)
		: base(server)
	{
		server.EventManager.OnUpnpComplete += EventManager_OnUpnpComplete;
	}

	public override void OnBeginModsAndConfigReady()
	{
		server.Config.onAdvertiseChanged += Config_onAdvertiseChanged;
		server.Config.onUpnpChanged += Config_onUpnpChanged;
	}

	private void Config_onUpnpChanged()
	{
		if (!server.Config.Upnp)
		{
			upnpComplete = false;
		}
	}

	private void Config_onAdvertiseChanged()
	{
		if ((!server.Config.Upnp && !server.Config.RuntimeUpnp) || upnpComplete)
		{
			if (server.Config.AdvertiseServer)
			{
				SendRegister();
			}
			else
			{
				SendUnregister();
			}
		}
	}

	private void EventManager_OnUpnpComplete(bool success)
	{
		if (!server.Config.AdvertiseServer || (token != null && token.Length > 0))
		{
			return;
		}
		if (!success)
		{
			ServerMain.Logger.Error("Upnp failed, will not attempt to register to the master server");
			return;
		}
		ServerMain.Logger.Notification("Server Advertising enabled. Attempt to register at the master server.");
		upnpComplete = true;
		try
		{
			SendRegister();
		}
		catch (Exception)
		{
			ServerMain.Logger.Error("Failed to register on the master server");
		}
	}

	public override void OnBeginRunGame()
	{
		if (!server.Config.AdvertiseServer || !server.IsDedicatedServer || server.Config.Upnp)
		{
			return;
		}
		ServerMain.Logger.Notification("Server Advertising enabled. Attempt to register at the master server.");
		try
		{
			SendRegister();
		}
		catch (Exception)
		{
			ServerMain.Logger.Error("Failed to register on the master server");
		}
	}

	public override void OnBeginShutdown()
	{
		SendUnregister();
	}

	public override void OnServerTick(float dt)
	{
		if (token != null && token.Length != 0)
		{
			SendHeartbeat();
		}
	}

	public void SendHeartbeat()
	{
		if (!server.Config.VerifyPlayerAuth)
		{
			return;
		}
		HeartbeatPacket packet = new HeartbeatPacket
		{
			token = token,
			players = server.GetPlayingClients()
		};
		SendRequestAsync(server.Config.MasterserverUrl + "heartbeat", packet, delegate(ResponsePacket response)
		{
			if (response.status == "invalid" || response.status == "timeout")
			{
				ServerMain.Logger.Notification("Master server sent response {0}. Will re-register now.", response.status);
				server.EnqueueMainThreadTask(delegate
				{
					SendRegister();
				});
			}
		});
	}

	public void SendUnregister()
	{
		if (token != null && token.Length != 0)
		{
			ServerMain.Logger.Notification("Unregistering from master server...");
			SendRequestAsync(server.Config.MasterserverUrl + "unregister", new UnregisterPacket
			{
				token = token
			}, delegate
			{
			});
		}
	}

	private void SendRegister()
	{
		if (!server.Config.VerifyPlayerAuth)
		{
			ServerMain.Logger.Notification("VerifyPlayerAuth is off. Will not register to master server");
		}
		ServerMain.Logger.Notification("Registering to master server...");
		ModPacket[] mods = (from mod in server.api.ModLoader.Mods
			where mod.Info.Side.IsUniversal() && mod.Info.RequiredOnClient
			select new ModPacket
			{
				id = mod.Info.ModID,
				version = mod.Info.Version
			}).ToArray();
		bool whitelistonly = server.Config.WhitelistMode == EnumWhitelistMode.On || (server.Config.WhitelistMode == EnumWhitelistMode.Default && server.IsDedicatedServer);
		RegisterRequestPacket packet = new RegisterRequestPacket
		{
			gameVersion = "1.20.7",
			maxPlayers = (ushort)server.Config.GetMaxClients(server),
			name = server.Config.ServerName,
			serverUrl = server.Config.ServerUrl,
			gameDescription = server.Config.ServerDescription,
			hasPassword = server.Config.IsPasswordProtected(),
			playstyle = new PlaystylePacket
			{
				id = server.SaveGameData.PlayStyle,
				langCode = server.SaveGameData.PlayStyleLangCode
			},
			port = (ushort)server.CurrentPort,
			Mods = mods,
			whitelisted = whitelistonly
		};
		SendRequestAsync(server.Config.MasterserverUrl + "register", packet, delegate(ResponsePacket response)
		{
			ServerMain.Logger.Notification("Master server response status: {0}", response.status);
			if (response.status == "blacklisted")
			{
				ServerMain.Logger.Warning("Your server has been blacklisted from the public server list. Likely due to inappropriate naming. Other players can still connect however. You may request to be removed from the blacklist through a support ticket on the official site.");
			}
			if (response.status == "ok")
			{
				token = response.data;
				server.SendMessageToGroup(GlobalConstants.ServerInfoChatGroup, "Successfully registered to master server", EnumChatType.Notification, null, "masterserverstatus:ok");
			}
			else
			{
				string message = "Could not register to master server, master server says: " + response.data;
				ServerMain.Logger.Notification(message);
				server.SendMessageToGroup(GlobalConstants.ServerInfoChatGroup, message, EnumChatType.Notification, null, "masterserverstatus:fail");
			}
		});
	}

	private async void SendRequestAsync<T>(string url, T packet, Action<ResponsePacket> onComplete)
	{
		string json = string.Empty;
		try
		{
			json = JsonConvert.SerializeObject(packet);
			StringContent jsonContent = new StringContent(json, null, "application/json");
			HttpResponseMessage httpResponseMessage = await VSWebClient.Inst.PostAsync(url, jsonContent);
			ResponsePacket responsePacket = JsonConvert.DeserializeObject<ResponsePacket>(await httpResponseMessage.Content.ReadAsStringAsync());
			if (responsePacket == null)
			{
				onComplete(new ResponsePacket
				{
					data = $"StatusCode: {httpResponseMessage.StatusCode}",
					status = "timeout"
				});
			}
			else
			{
				onComplete(responsePacket);
			}
		}
		catch (TaskCanceledException es)
		{
			ServerMain.Logger.Warning("Socket exception on master server async request: {0}", es.Message);
			onComplete(new ResponsePacket
			{
				data = null,
				status = "timeout"
			});
		}
		catch (Exception e)
		{
			ServerMain.Logger.Error("Failed request to master server url {0}.\nSent Json data: {1}", url, json);
			ServerMain.Logger.Error(e);
		}
	}
}
