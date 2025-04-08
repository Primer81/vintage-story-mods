using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ServerPlayer : IServerPlayer, IPlayer
{
	private ServerMain server;

	private ServerWorldPlayerData worlddata;

	public ServerPlayerData serverdata;

	internal ConnectedClient client;

	internal PlayerInventoryManager inventoryMgr;

	internal int blockBreakingCounter;

	public List<Entitlement> Entitlements { get; set; } = new List<Entitlement>();


	public int ActiveSlot
	{
		get
		{
			return inventoryMgr.ActiveHotbarSlotNumber;
		}
		set
		{
			inventoryMgr.ActiveHotbarSlotNumber = value;
			worlddata.SelectedHotbarSlot = value;
		}
	}

	public virtual string PlayerUID => worlddata?.PlayerUID;

	public bool ImmersiveFpMode { get; set; }

	public int ItemCollectMode { get; set; }

	public int ClientId
	{
		get
		{
			if (client != null)
			{
				return client.Id;
			}
			return 0;
		}
	}

	public virtual EnumClientState ConnectionState
	{
		get
		{
			if (client != null)
			{
				return client.State;
			}
			return EnumClientState.Offline;
		}
	}

	public EntityPlayer Entity => worlddata.EntityPlayer;

	public IPlayerInventoryManager InventoryManager => inventoryMgr;

	public string LanguageCode { get; set; }

	public string IpAddress
	{
		get
		{
			if (client == null)
			{
				return null;
			}
			return client.Socket.RemoteEndPoint().Address.ToString();
		}
	}

	public float Ping
	{
		get
		{
			if (client != null)
			{
				return client.LastPing;
			}
			return float.NaN;
		}
	}

	public string PlayerName
	{
		get
		{
			if (client != null)
			{
				return client.PlayerName;
			}
			return null;
		}
	}

	public IWorldPlayerData WorldData => worlddata;

	public PlayerGroupMembership[] Groups => serverdata.PlayerGroupMemberShips.Values.ToArray();

	public virtual IPlayerRole Role
	{
		get
		{
			return serverdata.GetPlayerRole(server);
		}
		set
		{
			server.api.Permissions.SetRole(this, value);
		}
	}

	public IServerPlayerData ServerData => serverdata;

	public string[] Privileges => serverdata.GetAllPrivilegeCodes(server.Config).ToArray();

	public int CurrentChunkSentRadius
	{
		get
		{
			return client?.CurrentChunkSentRadius ?? 0;
		}
		set
		{
			if (client != null)
			{
				client.CurrentChunkSentRadius = value;
			}
		}
	}

	public BlockSelection CurrentBlockSelection => Entity.BlockSelection;

	public EntitySelection CurrentEntitySelection => Entity.EntitySelection;

	public BlockSelection CurrentUsingBlockSelection { get; set; }

	public EntitySelection CurrentUsingEntitySelection { get; set; }

	public long LastReceivedClientPosition { get; set; }

	public event OnEntityAction InWorldAction;

	public FuzzyEntityPos GetSpawnPosition(bool consumeSpawnUse)
	{
		return server.GetSpawnPosition(worlddata.PlayerUID, onlyGlobalDefaultSpawn: false, consumeSpawnUse);
	}

	public ServerPlayer(ServerMain server, ServerWorldPlayerData worlddata)
	{
		this.server = server;
		this.worlddata = worlddata;
		LanguageCode = Lang.CurrentLocale;
		Init();
	}

	protected virtual void Init()
	{
		inventoryMgr = new ServerPlayerInventoryManager(worlddata.inventories, this, server);
		inventoryMgr.ActiveHotbarSlotNumber = worlddata.SelectedHotbarSlot;
		if (inventoryMgr.ActiveHotbarSlot == null)
		{
			inventoryMgr.ActiveHotbarSlotNumber = 0;
		}
		serverdata = server.PlayerDataManager.GetOrCreateServerPlayerData(worlddata.PlayerUID);
	}

	public void SetInventory(InventoryBasePlayer inv)
	{
		inventoryMgr.Inventories[inv.InventoryID] = inv;
	}

	public virtual void BroadcastPlayerData(bool sendInventory = false)
	{
		server.BroadcastPlayerData(this, sendInventory);
	}

	public virtual void Disconnect()
	{
		server.DisconnectPlayer(client);
	}

	public virtual void Disconnect(string message)
	{
		server.DisconnectPlayer(client, message);
	}

	public virtual bool HasPrivilege(string privilegeCode)
	{
		return serverdata.HasPrivilege(privilegeCode, server.Config.RolesByCode);
	}

	public void SendIngameError(string code, string message = null, params object[] langparams)
	{
		server.SendIngameError(this, code, message, langparams);
	}

	public void SendMessage(int groupId, string message, EnumChatType chatType, string data = null)
	{
		server.SendMessage(this, groupId, message, chatType, data);
	}

	public void SendLocalisedMessage(int groupId, string message, params object[] args)
	{
		server.SendMessage(this, groupId, Lang.GetL(LanguageCode, message, args), EnumChatType.Notification);
	}

	public void SetSpawnPosition(PlayerSpawnPos pos)
	{
		worlddata.SpawnPosition = pos;
		server.SendOwnPlayerData(this, sendInventory: false, sendPrivileges: true);
	}

	public void ClearSpawnPosition()
	{
		worlddata.SpawnPosition = null;
	}

	public PlayerGroupMembership[] GetGroups()
	{
		return serverdata.PlayerGroupMemberShips.Values.ToArray();
	}

	public PlayerGroupMembership GetGroup(int groupId)
	{
		serverdata.PlayerGroupMemberShips.TryGetValue(groupId, out var mems);
		return mems;
	}

	public void SetRole(string roleCode)
	{
		server.api.Permissions.SetRole(this, roleCode);
	}

	public void SetModdata(string key, byte[] data)
	{
		worlddata.SetModdata(key, data);
	}

	public void RemoveModdata(string key)
	{
		worlddata.RemoveModdata(key);
	}

	public byte[] GetModdata(string key)
	{
		return worlddata.GetModdata(key);
	}

	public void SetModData<T>(string key, T data)
	{
		SetModdata(key, SerializerUtil.Serialize(data));
	}

	public T GetModData<T>(string key, T defaultValue = default(T))
	{
		byte[] data = GetModdata(key);
		if (data == null)
		{
			return defaultValue;
		}
		return SerializerUtil.Deserialize<T>(data);
	}

	public EnumHandling TriggerInWorldAction(EnumEntityAction action, bool on)
	{
		EnumHandling handling = EnumHandling.PassThrough;
		if (this.InWorldAction == null)
		{
			return handling;
		}
		Delegate[] invocationList = this.InWorldAction.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			((OnEntityAction)invocationList[i])(action, on, ref handling);
			if (handling == EnumHandling.PreventSubsequent)
			{
				return handling;
			}
		}
		return handling;
	}
}
