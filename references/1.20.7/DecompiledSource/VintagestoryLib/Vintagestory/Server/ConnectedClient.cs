using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ConnectedClient
{
	public int AuditFlySuspicion;

	public long AuditFlySuspicionPrintedTotalMs = -99999L;

	public long LastAuditFlySuspicionTotalMs;

	public int TotalFlySuspicions;

	public int TotalTeleSupicions;

	public string LoginToken;

	private ServerWorldPlayerData worlddata;

	public EntityControls previousControls = new EntityControls();

	public ServerPlayer Player;

	public string SentPlayerUid;

	public bool IsNewEntityPlayer;

	public bool FallBackToTcp;

	public bool stopSent;

	public bool IsLocalConnection;

	public bool IsSinglePlayerClient;

	public long MillisecsAtConnect;

	public HashSet<long> ChunkSent;

	public HashSet<long> MapChunkSent;

	public HashSet<long> MapRegionSent;

	public int CurrentChunkSentRadius;

	public bool IsInventoryDirty;

	public bool IsPlayerStatsDirty;

	public bool ServerDidReceiveUdp;

	public List<EntityDespawn> entitiesNowOutOfRange = new List<EntityDespawn>();

	public List<EntityInRange> entitiesNowInRange = new List<EntityInRange>();

	public Dictionary<long, bool> TrackedEntities;

	public int Id;

	public HashSet<long> forceSendChunks = new HashSet<long>();

	public HashSet<long> forceSendMapChunks = new HashSet<long>();

	private EnumClientState clientStateOnServer;

	private NetConnection socket;

	private string ipAddress = "";

	public bool IsNewClient;

	public NetServer FromSocketListener;

	public Ping Ping;

	public float LastPing;

	public string FallbackPlayerName = "Unknown";

	public virtual ServerPlayerData ServerData => Player?.serverdata;

	public EntityPlayer Entityplayer => worlddata?.EntityPlayer;

	public ServerWorldPlayerData WorldData
	{
		get
		{
			return worlddata;
		}
		set
		{
			worlddata = value;
			worlddata.currentClientId = Id;
		}
	}

	public EntityPos Position => worlddata?.EntityPlayer?.ServerPos;

	public BlockPos ChunkPos
	{
		get
		{
			EntityPos Pos = Position;
			return new BlockPos((int)Pos.X / 32, (int)Pos.Y / 32, (int)Pos.Z / 32, Pos.Dimension);
		}
	}

	public string PlayerName
	{
		get
		{
			if (ServerData == null || ServerData.LastKnownPlayername == null)
			{
				return FallbackPlayerName;
			}
			return ServerData.LastKnownPlayername;
		}
	}

	public virtual bool IsPlayingClient => State == EnumClientState.Playing;

	public virtual bool ServerAssetsSent { get; set; }

	public EnumClientState State
	{
		get
		{
			return clientStateOnServer;
		}
		set
		{
			clientStateOnServer = value;
			if (worlddata != null)
			{
				worlddata.connected = true;
			}
		}
	}

	public long LastChatMessageTotalMs { get; set; }

	public NetConnection Socket
	{
		get
		{
			return socket;
		}
		set
		{
			socket = value;
			ipAddress = socket.RemoteEndPoint().Address.ToString();
			IsSinglePlayerClient = socket is DummyNetConnection;
			IsLocalConnection = ipAddress == "127.0.0.1" || ipAddress.StartsWithFast("::1") || ipAddress.StartsWithFast("::ffff:127.0.0.1");
		}
	}

	public ConnectedClient(int clientId)
	{
		Id = clientId;
		State = EnumClientState.Connecting;
		IsNewClient = true;
		Ping = new Ping();
	}

	public void Initialise()
	{
		ChunkSent = new HashSet<long>();
		MapChunkSent = new HashSet<long>();
		MapRegionSent = new HashSet<long>();
		IsInventoryDirty = true;
		IsPlayerStatsDirty = true;
		TrackedEntities = new Dictionary<long, bool>(100);
	}

	public void LoadOrCreatePlayerData(ServerMain server, string playername, string playerUid)
	{
		worlddata = null;
		if (server.PlayerDataManager.WorldDataByUID.TryGetValue(playerUid, out worlddata))
		{
			Player = new ServerPlayer(server, worlddata);
			server.PlayersByUid[worlddata.PlayerUID] = Player;
		}
		else
		{
			byte[] data = server.chunkThread.gameDatabase.GetPlayerData(playerUid);
			if (data != null)
			{
				try
				{
					worlddata = SerializerUtil.Deserialize<ServerWorldPlayerData>(data);
					worlddata.Init(server);
				}
				catch (Exception e)
				{
					ServerMain.Logger.Notification("Unable to deserlialize and init player data for playeruid {0}. Will create new one.", playerUid);
					ServerMain.Logger.Notification(LoggerBase.CleanStackTrace(e.ToString()));
				}
			}
			if (data == null)
			{
				worlddata = ServerWorldPlayerData.CreateNew(playername, playerUid);
				worlddata.Init(server);
				EntityProperties type2 = server.GetEntityType(GlobalConstants.EntityPlayerTypeCode);
				if (type2 == null)
				{
					throw new Exception(string.Concat("Cannot init player, there is no entity type with code ", GlobalConstants.EntityPlayerTypeCode, " was loaded!"));
				}
				worlddata.EntityPlayer.Code = type2.Code;
				IsNewEntityPlayer = true;
			}
			Player = new ServerPlayer(server, worlddata);
			server.PlayerDataManager.WorldDataByUID[playerUid] = worlddata;
			server.PlayersByUid[worlddata.PlayerUID] = Player;
		}
		if (worlddata.EntityPlayer == null)
		{
			ServerMain.Logger.Warning("Player had no entityplayer assigned to it? Creating new one.");
			worlddata.EntityPlayer = new EntityPlayer();
			EntityProperties type = server.GetEntityType(GlobalConstants.EntityPlayerTypeCode);
			if (type == null)
			{
				throw new Exception(string.Concat("Cannot init player, there is no entity type with code ", GlobalConstants.EntityPlayerTypeCode, " was loaded!"));
			}
			worlddata.EntityPlayer.Code = type.Code;
			IsNewEntityPlayer = true;
		}
		ServerData.LastKnownPlayername = playername;
	}

	public bool DidSendChunk(long index3d)
	{
		return ChunkSent.Contains(index3d);
	}

	public bool DidSendMapChunk(long index2d)
	{
		return MapChunkSent.Contains(index2d);
	}

	public bool DidSendMapRegion(long index2d)
	{
		return MapRegionSent.Contains(index2d);
	}

	public void SetMapRegionSent(long index2d)
	{
		MapRegionSent.Add(index2d);
	}

	public void SetChunkSent(long index3d)
	{
		ChunkSent.Add(index3d);
	}

	public void SetMapChunkSent(long index2d)
	{
		MapChunkSent.Add(index2d);
	}

	public void RemoveMapRegionSent(long index2d)
	{
		MapRegionSent.Remove(index2d);
	}

	public void RemoveChunkSent(long index3d)
	{
		ChunkSent.Remove(index3d);
	}

	public void RemoveMapChunkSent(long index2d)
	{
		MapChunkSent.Remove(index2d);
	}

	public override string ToString()
	{
		string name = Entityplayer.WatchedAttributes.GetString("name");
		return $"{name}:{ServerData.RoleCode}:{PlayerRole.PrivilegesString(ServerData.PermaPrivileges.ToList())} {ipAddress}";
	}

	public void CloseConnection()
	{
		if (Socket != null)
		{
			Socket.Shutdown();
			TyronThreadPool.QueueLongDurationTask(delegate
			{
				Thread.Sleep(1000);
				Socket?.Close();
			}, "connectedclientclose");
		}
	}

	public bool ShouldReceiveUpdatesForPos(BlockPos pos)
	{
		if (State != EnumClientState.Playing || Entityplayer == null)
		{
			return false;
		}
		return Entityplayer.ServerPos.InRangeOf(pos, worlddata.Viewdistance);
	}
}
