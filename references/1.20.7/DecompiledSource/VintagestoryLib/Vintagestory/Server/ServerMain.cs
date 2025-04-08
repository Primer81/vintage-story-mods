using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VSPlatform;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Server.Network;
using Vintagestory.Server.Systems;
using VintagestoryLib.Server.Systems;

namespace Vintagestory.Server;

public sealed class ServerMain : GameMain, IServerWorldAccessor, IWorldAccessor, IShutDownMonitor
{
	internal ConcurrentDictionary<int, ClientLastLogin> RecentClientLogins = new ConcurrentDictionary<int, ClientLastLogin>();

	public static IXPlatformInterface xPlatInterface;

	public GameExit exit;

	private bool suspended;

	public ThreadLocal<Random> rand = new ThreadLocal<Random>(() => new Random(Environment.TickCount));

	public bool Saving;

	public bool SendChunks = true;

	public bool AutoGenerateChunks = true;

	public bool stopped;

	public PlayerSpawnPos mapMiddleSpawnPos;

	public static Logger Logger;

	[ThreadStatic]
	public static FrameProfilerUtil FrameProfiler;

	public AssetManager AssetManager;

	internal EnumServerRunPhase RunPhase = EnumServerRunPhase.Standby;

	public bool readyToAutoSave = true;

	public List<Thread> Serverthreads = new List<Thread>();

	public readonly CancellationTokenSource ServerThreadsCts;

	internal List<ServerThread> ServerThreadLoops = new List<ServerThread>();

	internal ServerSystem[] Systems;

	public ServerEventManager ModEventManager;

	public CoreServerEventManager EventManager;

	public PlayerDataManager PlayerDataManager;

	public ServerUdpNetwork ServerUdpNetwork;

	private Thread ClientPacketParsingThread;

	public ServerWorldMap WorldMap;

	internal int CurrentPort;

	internal string CurrentIp;

	public static ClassRegistry ClassRegistry;

	public bool Standalone;

	private Stopwatch lastFramePassedTime = new Stopwatch();

	public Stopwatch totalUnpausedTime = new Stopwatch();

	private int timeOffsetDuringTick;

	public Stopwatch totalUpTime = new Stopwatch();

	public HashSet<string> AllPrivileges = new HashSet<string>();

	public Dictionary<string, string> PrivilegeDescriptions = new Dictionary<string, string>();

	internal int serverConsoleId = -1;

	private readonly CancellationTokenSource _consoleThreadsCts;

	internal ServerConsoleClient ServerConsoleClient;

	private ServerConsole serverConsole;

	public StatsCollection[] StatsCollector = new StatsCollection[4]
	{
		new StatsCollection(),
		new StatsCollection(),
		new StatsCollection(),
		new StatsCollection()
	};

	public int StatsCollectorIndex;

	public ConcurrentDictionary<int, ConnectedClient> Clients = new ConcurrentDictionary<int, ConnectedClient>();

	public Dictionary<string, ServerPlayer> PlayersByUid = new Dictionary<string, ServerPlayer>();

	public long TotalSentBytes;

	public long TotalSentBytesUdp;

	public long TotalReceivedBytes;

	public long TotalReceivedBytesUdp;

	public ServerConfig Config;

	public bool ConfigNeedsSaving;

	internal long lastDisconnectTotalMs;

	private int lastClientId;

	public ConcurrentQueue<BlockPos> DirtyBlockEntities = new ConcurrentQueue<BlockPos>();

	public ConcurrentQueue<BlockPos> ModifiedBlocks = new ConcurrentQueue<BlockPos>();

	public ConcurrentQueue<Vec4i> DirtyBlocks = new ConcurrentQueue<Vec4i>();

	public ConcurrentQueue<BlockPos> ModifiedDecors = new ConcurrentQueue<BlockPos>();

	public ConcurrentQueue<BlockPos> ModifiedBlocksNoRelight = new ConcurrentQueue<BlockPos>();

	public List<BlockPos> ModifiedBlocksMinimal = new List<BlockPos>();

	public Queue<BlockPos> UpdatedBlocks = new Queue<BlockPos>();

	internal int nextFreeBlockId;

	public OrderedDictionary<AssetLocation, ITreeGenerator> TreeGeneratorsByTreeCode = new OrderedDictionary<AssetLocation, ITreeGenerator>();

	public OrderedDictionary<AssetLocation, EntityProperties> EntityTypesByCode = new OrderedDictionary<AssetLocation, EntityProperties>();

	internal List<EntityProperties> entityTypesCached;

	internal List<string> entityCodesCached;

	public int nextFreeItemId;

	internal Dictionary<EnumClientAwarenessEvent, List<Action<ClientStatistics>>> clientAwarenessEvents;

	internal ServerSystemClientAwareness clientAwarenessSystem;

	public object mainThreadTasksLock = new object();

	private Queue<Action> mainThreadTasks = new Queue<Action>();

	public StartServerArgs serverStartArgs;

	public ServerProgramArgs progArgs;

	public string[] RawCmdLineArgs;

	public int TickPosition;

	internal ChunkServerThread chunkThread;

	internal object suspendLock = new object();

	public int ExitCode;

	private int nextClientID = 1;

	internal DateTime statsupdate;

	public Dictionary<Timer, Timer.Tick> Timers = new Dictionary<Timer, Timer.Tick>();

	private bool ignoreDisconnectCalls;

	internal float[] blockLightLevels = new float[32]
	{
		0.062f, 0.102f, 0.142f, 0.182f, 0.222f, 0.262f, 0.302f, 0.342f, 0.382f, 0.422f,
		0.462f, 0.502f, 0.542f, 0.582f, 0.622f, 0.662f, 0.702f, 0.742f, 0.782f, 0.822f,
		0.862f, 0.902f, 0.943f, 0.985f, 1f, 1f, 1f, 1f, 1f, 1f,
		1f, 1f
	};

	internal float[] sunLightLevels = new float[32]
	{
		0.062f, 0.102f, 0.142f, 0.182f, 0.222f, 0.262f, 0.302f, 0.342f, 0.382f, 0.422f,
		0.462f, 0.502f, 0.542f, 0.582f, 0.622f, 0.662f, 0.702f, 0.742f, 0.782f, 0.822f,
		0.862f, 0.902f, 0.943f, 0.985f, 1f, 1f, 1f, 1f, 1f, 1f,
		1f, 1f
	};

	internal int sunBrightness = 24;

	internal int seaLevel = 110;

	private CollisionTester collTester = new CollisionTester();

	internal ClientPacketHandler<Packet_Client, ConnectedClient>[] PacketHandlers = new ClientPacketHandler<Packet_Client, ConnectedClient>[255];

	public HandleClientCustomUdpPacket HandleCustomUdpPackets;

	internal bool[] PacketHandlingOnConnectingAllowed = new bool[255];

	public List<QueuedClient> ConnectionQueue = new List<QueuedClient>();

	internal ConcurrentQueue<ReceivedClientPacket> ClientPackets = new ConcurrentQueue<ReceivedClientPacket>();

	internal List<int> DisconnectedClientsThisTick = new List<int>();

	[ThreadStatic]
	private static BoxedPacket reusableBuffer;

	private readonly List<BoxedPacket> reusableBuffersDisposalList = new List<BoxedPacket>();

	internal bool doNetBenchmark;

	internal SortedDictionary<int, int> packetBenchmark = new SortedDictionary<int, int>();

	internal SortedDictionary<string, int> packetBenchmarkBlockEntitiesBytes = new SortedDictionary<string, int>();

	internal SortedDictionary<int, int> packetBenchmarkBytes = new SortedDictionary<int, int>();

	internal SortedDictionary<int, int> udpPacketBenchmark = new SortedDictionary<int, int>();

	internal SortedDictionary<int, int> udpPacketBenchmarkBytes = new SortedDictionary<int, int>();

	private readonly BoxedPacket_ServerAssets serverAssetsPacket = new BoxedPacket_ServerAssets();

	private bool serverAssetsSentLocally;

	private bool worldMetaDataPacketAlreadySentToSinglePlayer;

	internal GameCalendar GameWorldCalendar;

	internal long lastUpdateSentToClient;

	public bool DebugPrivileges;

	public HashSet<string> ClearPlayerInvs = new HashSet<string>();

	internal bool SpawnDebug;

	internal ServerCoreAPI api;

	internal HandleHandInteractionDelegate OnHandleBlockInteract;

	internal readonly CachingConcurrentDictionary<long, Entity> LoadedEntities = new CachingConcurrentDictionary<long, Entity>();

	public List<Entity> EntitySpawnSendQueue = new List<Entity>(10);

	public List<KeyValuePair<Entity, EntityDespawnData>> EntityDespawnSendQueue = new List<KeyValuePair<Entity, EntityDespawnData>>(10);

	public ConcurrentQueue<Entity> DelayedSpawnQueue = new ConcurrentQueue<Entity>();

	public Dictionary<string, string> EntityCodeRemappings = new Dictionary<string, string>();

	internal ConcurrentDictionary<long, ServerMapChunk> loadedMapChunks = new ConcurrentDictionary<long, ServerMapChunk>();

	internal ConcurrentDictionary<long, ServerMapRegion> loadedMapRegions = new ConcurrentDictionary<long, ServerMapRegion>();

	internal ConcurrentDictionary<int, IMiniDimension> LoadedMiniDimensions = new ConcurrentDictionary<int, IMiniDimension>();

	internal SaveGame SaveGameData;

	internal ChunkDataPool serverChunkDataPool;

	internal FastRWLock loadedChunksLock;

	internal Dictionary<long, ServerChunk> loadedChunks = new Dictionary<long, ServerChunk>(2000);

	internal object requestedChunkColumnsLock = new object();

	internal UniqueQueue<long> requestedChunkColumns = new UniqueQueue<long>();

	internal ConcurrentDictionary<long, int> ChunkColumnRequested = new ConcurrentDictionary<long, int>();

	internal ConcurrentQueue<long> unloadedChunks = new ConcurrentQueue<long>();

	internal HashSet<long> forceLoadedChunkColumns = new HashSet<long>();

	internal ConcurrentQueue<ChunkColumnLoadRequest> simpleLoadRequests = new ConcurrentQueue<ChunkColumnLoadRequest>();

	internal ConcurrentQueue<long> deleteChunkColumns = new ConcurrentQueue<long>();

	internal ConcurrentQueue<long> deleteMapRegions = new ConcurrentQueue<long>();

	internal ConcurrentQueue<KeyValuePair<HorRectanglei, ChunkLoadOptions>> fastChunkQueue = new ConcurrentQueue<KeyValuePair<HorRectanglei, ChunkLoadOptions>>();

	internal ConcurrentQueue<KeyValuePair<Vec2i, ChunkPeekOptions>> peekChunkColumnQueue = new ConcurrentQueue<KeyValuePair<Vec2i, ChunkPeekOptions>>();

	internal ConcurrentQueue<ChunkLookupRequest> testChunkExistsQueue = new ConcurrentQueue<ChunkLookupRequest>();

	private ChunkLoadOptions defaultOptions = new ChunkLoadOptions();

	public bool requiresRemaps;

	public override IWorldAccessor World => this;

	protected override WorldMap worldmap => WorldMap;

	public int Seed => SaveGameData.Seed;

	public string SavegameIdentifier => SaveGameData.SavegameIdentifier;

	public bool Suspended => suspended;

	FrameProfilerUtil IWorldAccessor.FrameProfiler => FrameProfiler;

	public override ClassRegistry ClassRegistryInt
	{
		get
		{
			return ClassRegistry;
		}
		set
		{
			ClassRegistry = value;
		}
	}

	public int ServerConsoleId => serverConsoleId;

	public NetServer[] MainSockets { get; set; } = new NetServer[2];


	public UNetServer[] UdpSockets { get; set; } = new UNetServer[2];


	ILogger IWorldAccessor.Logger => Logger;

	IAssetManager IWorldAccessor.AssetManager => AssetManager;

	public EnumAppSide Side => EnumAppSide.Server;

	public ICoreAPI Api => api;

	public IChunkProvider ChunkProvider => WorldMap;

	public ILandClaimAPI Claims => WorldMap;

	public EntityPos DefaultSpawnPosition => EntityPosFromSpawnPos((SaveGameData.DefaultSpawn == null) ? mapMiddleSpawnPos : SaveGameData.DefaultSpawn);

	public float[] BlockLightLevels => blockLightLevels;

	public float[] SunLightLevels => sunLightLevels;

	public int SeaLevel => seaLevel;

	public int SunBrightness => sunBrightness;

	public bool IsDedicatedServer { get; }

	public IBlockAccessor BlockAccessor => WorldMap.RelaxedBlockAccess;

	public IBulkBlockAccessor BulkBlockAccessor => WorldMap.BulkBlockAccess;

	Random IWorldAccessor.Rand => rand.Value;

	public long ElapsedMilliseconds => totalUnpausedTime.ElapsedMilliseconds + timeOffsetDuringTick;

	public List<EntityProperties> EntityTypes => entityTypesCached ?? (entityTypesCached = EntityTypesByCode.Values.ToList());

	public List<string> EntityTypeCodes => entityCodesCached ?? (entityCodesCached = makeEntityCodesCache());

	public int DefaultEntityTrackingRange => MagicNum.DefaultEntityTrackingRange;

	List<GridRecipe> IWorldAccessor.GridRecipes => GridRecipes;

	List<CollectibleObject> IWorldAccessor.Collectibles => Collectibles;

	IList<Block> IWorldAccessor.Blocks => Blocks;

	IList<Item> IWorldAccessor.Items => Items;

	List<EntityProperties> IWorldAccessor.EntityTypes => EntityTypes;

	List<string> IWorldAccessor.EntityTypeCodes => EntityTypeCodes;

	public Dictionary<string, string> RemappedEntities => EntityCodeRemappings;

	public OrderedDictionary<AssetLocation, ITreeGenerator> TreeGenerators => TreeGeneratorsByTreeCode;

	public IPlayer[] AllOnlinePlayers => (from c in Clients
		select c.Value.Player into c
		where c != null
		select c).ToArray();

	public IPlayer[] AllPlayers => PlayersByUid.Values.ToArray();

	public bool EntityDebugMode => Config.EntityDebugMode;

	IClassRegistryAPI IWorldAccessor.ClassRegistry => api.ClassRegistry;

	public CollisionTester CollisionTester => collTester;

	ConcurrentDictionary<long, Entity> IServerWorldAccessor.LoadedEntities => LoadedEntities;

	public override Vec3i MapSize => WorldMap.MapSize;

	ITreeAttribute IWorldAccessor.Config => SaveGameData?.WorldConfiguration;

	public override IBlockAccessor blockAccessor => WorldMap.RelaxedBlockAccess;

	public IGameCalendar Calendar => GameWorldCalendar;

	public bool ShuttingDown => RunPhase >= EnumServerRunPhase.Shutdown;

	public long[] LoadedChunkIndices => loadedChunks.Keys.ToArray();

	public long[] LoadedMapChunkIndices => loadedMapChunks.Keys.ToArray();

	private void HandleRequestJoin(Packet_Client packet, ConnectedClient client)
	{
		FrameProfiler.Mark("reqjoin-before");
		Logger.VerboseDebug("HandleRequestJoin: Begin. Player: {0}", client?.PlayerName);
		ServerPlayer player = client.Player;
		player.LanguageCode = packet.RequestJoin.Language ?? Lang.CurrentLocale;
		if (client.IsSinglePlayerClient)
		{
			player.serverdata.RoleCode = Config.Roles.MaxBy((PlayerRole v) => v.PrivilegeLevel).Code;
		}
		Logger.VerboseDebug("HandleRequestJoin: Before set name");
		client.Entityplayer.SetName(player.PlayerName);
		api.networkapi.SendChannelsPacket(player);
		SendPacket(player, ServerPackets.LevelInitialize(Config.MaxChunkRadius * MagicNum.ServerChunkSize));
		Logger.VerboseDebug("HandleRequestJoin: After Level initialize");
		SendLevelProgress(player, 100, Lang.Get("Generating world..."));
		FrameProfiler.Mark("reqjoin-1");
		SendWorldMetaData(player);
		FrameProfiler.Mark("reqjoin-2");
		SendServerAssets(player);
		FrameProfiler.Mark("reqjoin-3");
		client.ServerAssetsSent = true;
		SendPlayerEntities(player);
		FrameProfiler.Mark("reqjoin-4");
		ServerSystem[] systems = Systems;
		for (int i = 0; i < systems.Length; i++)
		{
			systems[i].OnPlayerJoin(player);
		}
		EventManager.TriggerPlayerJoin(player);
		BroadcastPlayerData(player, sendInventory: true, sendPrivileges: true);
		foreach (ConnectedClient oclient in Clients.Values)
		{
			if (oclient != client && oclient.Entityplayer != null)
			{
				SendInitialPlayerDataForOthers(oclient.Player, player);
			}
		}
		Logger.VerboseDebug("HandleRequestJoin: After broadcastplayerdata. hotbarslot: " + player.inventoryMgr.ActiveHotbarSlot);
		ItemStack hotbarstack = player.inventoryMgr?.ActiveHotbarSlot?.Itemstack;
		ItemStack offstack = player.Entity?.LeftHandItemSlot?.Itemstack;
		SendPacket(player, new Packet_Server
		{
			SelectedHotbarSlot = new Packet_SelectedHotbarSlot
			{
				SlotNumber = player.InventoryManager.ActiveHotbarSlotNumber,
				ClientId = player.ClientId,
				Itemstack = ((hotbarstack == null) ? null : StackConverter.ToPacket(hotbarstack)),
				OffhandStack = ((offstack == null) ? null : StackConverter.ToPacket(offstack))
			},
			Id = 53
		});
		SendPacket(player, ServerPackets.LevelFinalize());
		Logger.VerboseDebug("HandleRequestJoin: After LevelFinalize");
		if (client.IsNewEntityPlayer)
		{
			EventManager.TriggerPlayerCreate(client.Player);
		}
		systems = Systems;
		for (int i = 0; i < systems.Length; i++)
		{
			systems[i].OnPlayerJoinPost(player);
		}
		FrameProfiler.Mark("reqjoin-after");
	}

	private void HandleClientLoaded(Packet_Client packet, ConnectedClient client)
	{
		ServerPlayer player = client.Player;
		player.Entity.WatchedAttributes.MarkAllDirty();
		client.State = EnumClientState.Connected;
		client.MillisecsAtConnect = totalUnpausedTime.ElapsedMilliseconds;
		SendMessageToGeneral(Lang.Get("{0} joined. Say hi :)", player.PlayerName), EnumChatType.JoinLeave, player);
		Logger.Event($"{player.PlayerName} {client.Socket.RemoteEndPoint()} joins.");
		string msg = string.Format(Config.WelcomeMessage.Replace("{playername}", "{0}"), player.PlayerName);
		SendMessage(player, GlobalConstants.GeneralChatGroup, msg, EnumChatType.Notification);
		EventManager.TriggerPlayerNowPlaying(client.Player);
		if (Config.RepairMode)
		{
			SendMessage(player, GlobalConstants.GeneralChatGroup, "Server is in repair mode, you are now in spectator mode. If you are not already there, fly to the area that crashes and let the chunks load, then exit the game and run in normal mode.", EnumChatType.Notification);
			client.Player.WorldData.CurrentGameMode = EnumGameMode.Spectator;
			client.Player.WorldData.NoClip = true;
			client.Player.WorldData.FreeMove = true;
			client.Player.WorldData.MoveSpeedMultiplier = 1f;
			broadCastModeChange(client.Player);
		}
		SendRoles(player);
	}

	public void SendRoles(IServerPlayer player)
	{
		Packet_Roles roles = new Packet_Roles();
		roles.SetRoles(Config.RolesByCode.Select((KeyValuePair<string, PlayerRole> val) => new Packet_Role
		{
			Code = val.Value.Code,
			PrivilegeLevel = val.Value.PrivilegeLevel
		}).ToArray());
		SendPacket(player, new Packet_Server
		{
			Id = 76,
			Roles = roles
		});
	}

	public void BroadcastRoles()
	{
		Packet_Roles roles = new Packet_Roles();
		roles.SetRoles(Config.RolesByCode.Select((KeyValuePair<string, PlayerRole> val) => new Packet_Role
		{
			Code = val.Value.Code,
			PrivilegeLevel = val.Value.PrivilegeLevel
		}).ToArray());
		BroadcastPacket(new Packet_Server
		{
			Id = 76,
			Roles = roles
		});
	}

	public void broadCastModeChange(IServerPlayer player)
	{
		BroadcastPacket(new Packet_Server
		{
			Id = 46,
			ModeChange = new Packet_PlayerMode
			{
				PlayerUID = player.PlayerUID,
				FreeMove = (player.WorldData.FreeMove ? 1 : 0),
				GameMode = (int)player.WorldData.CurrentGameMode,
				MoveSpeed = CollectibleNet.SerializeFloat(player.WorldData.MoveSpeedMultiplier),
				NoClip = (player.WorldData.NoClip ? 1 : 0),
				ViewDistance = player.WorldData.LastApprovedViewDistance,
				PickingRange = CollectibleNet.SerializeFloat(player.WorldData.PickingRange),
				FreeMovePlaneLock = (int)player.WorldData.FreeMovePlaneLock
			}
		});
	}

	private void HandleClientPlaying(Packet_Client packet, ConnectedClient client)
	{
		client.State = EnumClientState.Playing;
		WorldMap.SendClaims(client.Player, SaveGameData.LandClaims, null);
	}

	private void HandleRequestModeChange(Packet_Client p, ConnectedClient client)
	{
		Packet_PlayerMode packet = p.RequestModeChange;
		int clientid = client.Id;
		string playerUid = packet.PlayerUID;
		ConnectedClient targetClient = GetClientByUID(playerUid);
		if (client.Player == null)
		{
			Logger.Notification("Mode change request from a player without player object?! Ignoring.");
			return;
		}
		if (targetClient == null)
		{
			ReplyMessage(client.Player, "No such target client found.", EnumChatType.CommandError);
			return;
		}
		ServerWorldPlayerData playerData = targetClient.WorldData;
		playerData.DesiredViewDistance = packet.ViewDistance;
		if (playerData.Viewdistance != packet.ViewDistance)
		{
			Logger.Notification("Player {0} requested new view distance: {1}", client.PlayerName, packet.ViewDistance);
			if (targetClient.FromSocketListener.GetType() == typeof(DummyTcpNetServer))
			{
				Config.MaxChunkRadius = Math.Max(Config.MaxChunkRadius, packet.ViewDistance / 32);
				Logger.Notification("Upped server view distance because player is locally connected");
			}
		}
		playerData.Viewdistance = packet.ViewDistance;
		bool freeMoveAllowed;
		bool gameModeAllowed;
		bool pickRangeAllowed;
		if (playerUid != client.WorldData.PlayerUID)
		{
			freeMoveAllowed = PlayerHasPrivilege(clientid, Privilege.freemove) && PlayerHasPrivilege(clientid, Privilege.commandplayer);
			gameModeAllowed = PlayerHasPrivilege(clientid, Privilege.gamemode) && PlayerHasPrivilege(clientid, Privilege.commandplayer);
			pickRangeAllowed = PlayerHasPrivilege(clientid, Privilege.pickingrange) && PlayerHasPrivilege(clientid, Privilege.commandplayer);
		}
		else
		{
			freeMoveAllowed = PlayerHasPrivilege(clientid, Privilege.freemove);
			gameModeAllowed = PlayerHasPrivilege(clientid, Privilege.gamemode);
			pickRangeAllowed = PlayerHasPrivilege(clientid, Privilege.pickingrange);
		}
		if (freeMoveAllowed)
		{
			playerData.FreeMove = packet.FreeMove > 0;
			playerData.NoClip = packet.NoClip > 0;
			playerData.MoveSpeedMultiplier = CollectibleNet.DeserializeFloat(packet.MoveSpeed);
			try
			{
				playerData.FreeMovePlaneLock = (EnumFreeMovAxisLock)packet.FreeMovePlaneLock;
			}
			catch (Exception)
			{
			}
		}
		else if (((packet.FreeMove > 0) ^ playerData.FreeMove) || (playerData.NoClip ^ (packet.NoClip > 0)) || playerData.MoveSpeedMultiplier != CollectibleNet.DeserializeFloat(packet.MoveSpeed))
		{
			ReplyMessage(client.Player, "Not allowed to change fly mode, noclip or move speed. Missing privilege or not allowed in this world.", EnumChatType.CommandError);
		}
		EnumGameMode requestedMode = EnumGameMode.Guest;
		try
		{
			requestedMode = (EnumGameMode)packet.GameMode;
		}
		catch (Exception)
		{
		}
		if (gameModeAllowed)
		{
			EnumGameMode gameMode = playerData.GameMode;
			playerData.GameMode = requestedMode;
			if (gameMode != requestedMode)
			{
				for (int i = 0; i < Systems.Length; i++)
				{
					Systems[i].OnPlayerSwitchGameMode(targetClient.Player);
				}
				EventManager.TriggerPlayerChangeGamemode(targetClient.Player);
				if (requestedMode == EnumGameMode.Guest || requestedMode == EnumGameMode.Survival)
				{
					playerData.MoveSpeedMultiplier = 1f;
				}
			}
		}
		else if (playerData.GameMode != requestedMode)
		{
			ReplyMessage(client.Player, "Not allowed to change game mode. Missing privilege or not allowed in this world.", EnumChatType.CommandError);
		}
		if (pickRangeAllowed)
		{
			playerData.PickingRange = CollectibleNet.DeserializeFloat(packet.PickingRange);
		}
		else if (playerData.PickingRange != CollectibleNet.DeserializeFloat(packet.PickingRange))
		{
			ReplyMessage(client.Player, "Not allowed to change picking range. Missing privilege or not allowed in this world.", EnumChatType.CommandError);
		}
		bool canFreeMove = playerData.GameMode == EnumGameMode.Creative || playerData.GameMode == EnumGameMode.Spectator;
		playerData.FreeMove = (playerData.FreeMove && canFreeMove) || playerData.GameMode == EnumGameMode.Spectator;
		playerData.NoClip &= canFreeMove;
		playerData.RenderMetaBlocks = packet.RenderMetaBlocks > 0;
		targetClient.Entityplayer.Controls.MovespeedMultiplier = playerData.MoveSpeedMultiplier;
		BroadcastPacket(new Packet_Server
		{
			Id = 46,
			ModeChange = new Packet_PlayerMode
			{
				PlayerUID = playerUid,
				FreeMove = (playerData.FreeMove ? 1 : 0),
				GameMode = (int)playerData.GameMode,
				MoveSpeed = CollectibleNet.SerializeFloat(playerData.MoveSpeedMultiplier),
				NoClip = (playerData.NoClip ? 1 : 0),
				ViewDistance = playerData.LastApprovedViewDistance,
				PickingRange = CollectibleNet.SerializeFloat(playerData.PickingRange),
				FreeMovePlaneLock = (int)playerData.FreeMovePlaneLock
			}
		});
		targetClient.Player.Entity.UpdatePartitioning();
		targetClient.Player.Entity.Controls.NoClip = playerData.NoClip;
	}

	private void HandleChatLine(Packet_Client packet, ConnectedClient client)
	{
		string message = packet.Chatline.Message.Trim();
		int groupId = packet.Chatline.Groupid;
		if (groupId < -1)
		{
			groupId = 0;
		}
		HandleChatMessage(client.Player, groupId, message);
	}

	private void HandleSelectedHotbarSlot(Packet_Client packet, ConnectedClient client)
	{
		int fromSlot = client.Player.ActiveSlot;
		int toSlot = packet.SelectedHotbarSlot.SlotNumber;
		if (EventManager.TriggerBeforeActiveSlotChanged(client.Player, fromSlot, toSlot))
		{
			client.Player.ActiveSlot = toSlot;
			client.Player.InventoryManager.ActiveHotbarSlot.Inventory.DropSlotIfHot(client.Player.InventoryManager.ActiveHotbarSlot, client.Player);
			BroadcastHotbarSlot(client.Player);
			(client.Player.Entity.AnimManager as PlayerAnimationManager)?.OnActiveSlotChanged(client.Player.InventoryManager.ActiveHotbarSlot);
			EventManager.TriggerAfterActiveSlotChanged(client.Player, fromSlot, toSlot);
		}
		else
		{
			BroadcastHotbarSlot(client.Player, skipSelf: false);
		}
	}

	public void BroadcastHotbarSlot(IServerPlayer ofPlayer, bool skipSelf = true)
	{
		IServerPlayer[] skipPlayers = ((!skipSelf) ? new IServerPlayer[0] : new IServerPlayer[1] { ofPlayer });
		if (ofPlayer.InventoryManager?.ActiveHotbarSlot == null)
		{
			if (ofPlayer.InventoryManager == null)
			{
				Logger.Error("BroadcastHotbarSlot: InventoryManager is null?! Ignoring.");
			}
			else
			{
				Logger.Error("BroadcastHotbarSlot: ActiveHotbarSlot is null?! Ignoring.");
			}
			return;
		}
		ItemStack stack = ofPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
		ItemStack offstack = ofPlayer.Entity?.LeftHandItemSlot?.Itemstack;
		BroadcastPacket(new Packet_Server
		{
			SelectedHotbarSlot = new Packet_SelectedHotbarSlot
			{
				ClientId = ofPlayer.ClientId,
				SlotNumber = ofPlayer.InventoryManager.ActiveHotbarSlotNumber,
				Itemstack = ((stack == null) ? null : StackConverter.ToPacket(stack)),
				OffhandStack = ((offstack == null) ? null : StackConverter.ToPacket(offstack))
			},
			Id = 53
		}, skipPlayers);
	}

	private void HandleLeave(Packet_Client packet, ConnectedClient client)
	{
		DisconnectPlayer(client, (packet.Leave.Reason == 1) ? Lang.GetL(client.Player.LanguageCode, "The Players client crashed") : null);
	}

	private void HandleMoveKeyChange(Packet_Client packet, ConnectedClient client)
	{
		EntityControls controls = ((client.Entityplayer.MountedOn == null) ? client.Entityplayer.Controls : client.Entityplayer.MountedOn.Controls);
		if (controls != null)
		{
			client.previousControls.SetFrom(controls);
			controls.UpdateFromPacket(packet.MoveKeyChange.Down > 0, packet.MoveKeyChange.Key);
			if (client.previousControls.ToInt() != controls.ToInt())
			{
				controls.Dirty = true;
				client.Player.TriggerInWorldAction((EnumEntityAction)packet.MoveKeyChange.Key, packet.MoveKeyChange.Down > 0);
			}
		}
	}

	private void HandleEntityPacket(Packet_Client packet, ConnectedClient client)
	{
		Packet_EntityPacket p = packet.EntityPacket;
		if (LoadedEntities.TryGetValue(p.EntityId, out var entity))
		{
			entity.OnReceivedClientPacket(client.Player, p.Packetid, p.Data);
		}
	}

	public void HandleChatMessage(IServerPlayer player, int groupid, string message)
	{
		if (groupid > 0 && !PlayerDataManager.PlayerGroupsById.ContainsKey(groupid))
		{
			SendMessage(player, GlobalConstants.ServerInfoChatGroup, "No such group exists on this server.", EnumChatType.CommandError);
		}
		else
		{
			if (string.IsNullOrEmpty(message))
			{
				return;
			}
			if (message.StartsWith('/'))
			{
				string command = message.Split(new char[1] { ' ' })[0].Replace("/", "");
				command = command.ToLowerInvariant();
				string arguments = ((message.IndexOf(' ') < 0) ? "" : message.Substring(message.IndexOf(' ') + 1));
				api.commandapi.Execute(command, player, groupid, arguments);
			}
			else
			{
				if (message.StartsWith('.'))
				{
					return;
				}
				if (!player.HasPrivilege(Privilege.chat))
				{
					SendMessage(player, groupid, Lang.Get("No privilege to chat"), EnumChatType.CommandError);
					return;
				}
				if (ElapsedMilliseconds - Clients[player.ClientId].LastChatMessageTotalMs < Config.ChatRateLimitMs)
				{
					SendMessage(player, groupid, Lang.Get("Chat not sent. Rate limited to 1 chat message per {0} seconds", (float)Config.ChatRateLimitMs / 1000f), EnumChatType.CommandError);
					return;
				}
				Clients[player.ClientId].LastChatMessageTotalMs = ElapsedMilliseconds;
				message = message.Replace(">", "&gt;").Replace("<", "&lt;");
				string data = "from: " + player.Entity.EntityId + ",withoutPrefix:" + message;
				string originalMessage = message;
				BoolRef consumed = new BoolRef();
				EventManager.TriggerOnplayerChat(player, groupid, ref message, ref data, consumed);
				if (!consumed.value)
				{
					SendMessageToGroup(groupid, message, EnumChatType.OthersMessage, player, data);
					player.SendMessage(groupid, message, EnumChatType.OwnMessage, data);
					Logger.Chat($"{groupid} | {player.PlayerName}: {originalMessage.Replace("{", "{{").Replace("}", "}}")}");
				}
			}
		}
	}

	private void HandleQueryClientPacket(ConnectedClient client, Packet_Client packet)
	{
		if (packet.Id == 33)
		{
			if (Config.LoginFloodProtection)
			{
				int clientIpHash = client.Socket.RemoteEndPoint().Address.GetHashCode();
				int now = Environment.TickCount;
				if (RecentClientLogins.TryGetValue(clientIpHash, out var block))
				{
					if (now - block.LastTickCount < 500 && now - block.LastTickCount >= 0)
					{
						block.LastTickCount = now;
						block.Times++;
						RecentClientLogins[clientIpHash] = block;
						if (Config.TemporaryIpBlockList && block.Times > 50)
						{
							string ipString = client.Socket.RemoteEndPoint().Address.ToString();
							TcpNetConnection.blockedIps.Add(ipString);
							Logger.Notification($"Client {client.Id} | {ipString} send too many request. Adding to blocked IP's");
						}
						DisconnectPlayer(client, "Too many requests", "Your client is sending too many requests");
						return;
					}
					block.LastTickCount = now;
					block.Times = 0;
					RecentClientLogins[clientIpHash] = block;
				}
				else
				{
					RecentClientLogins[clientIpHash] = new ClientLastLogin
					{
						LastTickCount = now,
						Times = 1
					};
				}
			}
			client.LoginToken = Guid.NewGuid().ToString();
			ServerUdpNetwork.connectingClients.Add(client.LoginToken, client);
			SendPacket(client.Id, new Packet_Server
			{
				Id = 77,
				Token = new Packet_LoginTokenAnswer
				{
					Token = client.LoginToken
				}
			});
		}
		else
		{
			DisconnectPlayer(client, "", "Query complete");
		}
	}

	private void HandlePlayerIdentification(Packet_Client p, ConnectedClient client)
	{
		Packet_ClientIdentification packet = p.Identification;
		if (packet == null)
		{
			DisconnectPlayer(client, null, Lang.Get("Invalid join data!"));
			return;
		}
		if ("1.20.8" != packet.NetworkVersion)
		{
			DisconnectPlayer(client, null, Lang.Get("disconnect-wrongversion", packet.ShortGameVersion, packet.NetworkVersion, "1.20.7", "1.20.8"));
			return;
		}
		if (client.FromSocketListener.GetType() != typeof(DummyTcpNetServer) && Config.IsPasswordProtected() && packet.ServerPassword != Config.Password)
		{
			Logger.Event($"{packet.Playername} fails to join (invalid server password).");
			DisconnectPlayer(client, null, Lang.Get("Password is invalid"));
			return;
		}
		if ((Config.WhitelistMode == EnumWhitelistMode.On || (Config.WhitelistMode == EnumWhitelistMode.Default && IsDedicatedServer)) && !client.IsLocalConnection)
		{
			PlayerEntry playerWhitelist = PlayerDataManager.GetPlayerWhitelist(packet.Playername, packet.PlayerUID);
			if (playerWhitelist == null)
			{
				DisconnectPlayer(client, null, "This server only allows whitelisted players to join. You are not on the whitelist.");
				return;
			}
			if (playerWhitelist.UntilDate < DateTime.Now)
			{
				DisconnectPlayer(client, null, "This server only allows whitelisted players to join. Your whitelist entry has expired.");
				return;
			}
		}
		if (packet.Playername == null || packet.PlayerUID == null)
		{
			client.IsNewClient = true;
			Logger.Event($"{packet.Playername} fails to join (player name or playeruid null value sent).");
			DisconnectPlayer(client, null, "Invalid join data");
		}
		PlayerEntry playerBan = PlayerDataManager.GetPlayerBan(packet.Playername, packet.PlayerUID);
		if (playerBan != null && playerBan.UntilDate > DateTime.Now)
		{
			Logger.Event($"{packet.Playername} fails to join (banned).");
			DisconnectPlayer(client, null, Lang.Get("banned-until-reason", playerBan.IssuedByPlayerName, playerBan.UntilDate, playerBan.Reason));
			return;
		}
		client.SentPlayerUid = packet.PlayerUID;
		Logger.Notification("Client {0} uid {1} attempting identification. Name: {2}", client.Id, packet.PlayerUID, packet.Playername);
		string playername = packet.Playername;
		Regex allowedPlayername = new Regex("^(\\w|-){1,16}$");
		if (string.IsNullOrEmpty(playername) || !allowedPlayername.IsMatch(playername))
		{
			Logger.Event($"{client.Socket.RemoteEndPoint()} can't join (invalid Playername: {playername}).");
			DisconnectPlayer(client, null, Lang.Get("Your playername contains not allowed characters or is not set. Are you using a hacked client?"));
		}
		else if (client.IsSinglePlayerClient || !Config.VerifyPlayerAuth)
		{
			string entitlements = (client.IsSinglePlayerClient ? GlobalConstants.SinglePlayerEntitlements : null);
			PreFinalizePlayerIdentification(packet, client, entitlements);
		}
		else
		{
			VerifyPlayerWithAuthServer(packet, client);
		}
	}

	public ServerMain(StartServerArgs serverargs, string[] cmdlineArgsRaw, ServerProgramArgs progArgs, bool isDedicatedServer = true)
	{
		IsDedicatedServer = isDedicatedServer;
		if (Logger == null)
		{
			Logger = new ServerLogger(progArgs);
		}
		serverStartArgs = serverargs;
		_consoleThreadsCts = new CancellationTokenSource();
		ServerThreadsCts = new CancellationTokenSource();
		Logger.TraceLog = progArgs.TraceLog;
		RawCmdLineArgs = cmdlineArgsRaw;
		this.progArgs = progArgs;
		if (progArgs.SetConfigAndExit != null)
		{
			string filename = "serverconfig.json";
			if (!File.Exists(Path.Combine(GamePaths.Config, filename)))
			{
				Logger?.Notification("serverconfig.json not found, creating new one");
				ServerSystemLoadConfig.GenerateConfig(this);
			}
			else
			{
				ServerSystemLoadConfig.LoadConfig(this);
			}
			JObject obj = JToken.Parse(progArgs.SetConfigAndExit) as JObject;
			JObject tokcfg = JToken.FromObject(Config) as JObject;
			foreach (KeyValuePair<string, JToken> val in obj)
			{
				JToken tok = tokcfg[val.Key];
				if (tok == null)
				{
					Logger?.Notification("No such setting '" + val.Key + "'. Ignoring.");
					ExitCode = 404;
					return;
				}
				if (tok is JObject tokobj)
				{
					tokobj.Merge(val.Value);
					Logger?.Notification("Ok, values merged for {0}.", val.Key);
				}
				else
				{
					tokcfg[val.Key] = val.Value;
					Logger?.Notification("Ok, value {0} set for {1}.", val.Value, val.Key);
				}
			}
			try
			{
				Config = tokcfg.ToObject<ServerConfig>();
			}
			catch (Exception e)
			{
				Logger?.Notification("Failed saving config, you are likely suppling an incorrect value type (e.g. a number for a boolean setting). See server-debug.log for exception.");
				Logger?.VerboseDebug("Failed saving config from --setConfig. Exception:");
				Logger?.VerboseDebug(LoggerBase.CleanStackTrace(e.ToString()));
				ExitCode = 500;
				return;
			}
			ExitCode = 200;
			ServerSystemLoadConfig.SaveConfig(this);
			Logger?.Dispose();
			return;
		}
		if (progArgs.GenConfigAndExit)
		{
			ServerSystemLoadConfig.GenerateConfig(this);
			ServerSystemLoadConfig.SaveConfig(this);
			if (Logger != null)
			{
				Logger.Notification("Config generated.");
				Logger.Dispose();
			}
			return;
		}
		ServerConsoleClient = new ServerConsoleClient(serverConsoleId)
		{
			FallbackPlayerName = "Admin",
			IsNewClient = false
		};
		ServerConsoleClient.WorldData = new ServerWorldPlayerData
		{
			PlayerUID = "Admin"
		};
		FrameProfiler = new FrameProfilerUtil(Logger.Notification);
		if (IsDedicatedServer)
		{
			serverConsole = new ServerConsole(this, _consoleThreadsCts.Token);
		}
		foreach (Thread serverthread in Serverthreads)
		{
			serverthread?.Start();
		}
		totalUpTime.Start();
	}

	private Thread CreateThread(string name, ServerSystem[] serversystems, CancellationToken cancellationToken)
	{
		ServerThread serverThread = new ServerThread(this, name, cancellationToken);
		ServerThreadLoops.Add(serverThread);
		serverThread.serversystems = serversystems;
		return new Thread(serverThread.Process)
		{
			IsBackground = true,
			Name = name
		};
	}

	public void AddServerThread(string name, IAsyncServerSystem modsystem)
	{
		ServerSystem serverSystem = new ServerSystemAsync(this, name, modsystem);
		Thread thread = CreateThread(name, new ServerSystem[1] { serverSystem }, ServerThreadsCts.Token);
		Serverthreads.Add(thread);
		Array.Resize(ref Systems, Systems.Length + 1);
		Systems[Systems.Length - 1] = serverSystem;
		if (RunPhase >= EnumServerRunPhase.RunGame)
		{
			thread.Start();
		}
	}

	public void PreLaunch()
	{
		ClientPacketParsingThread = TyronThreadPool.CreateDedicatedThread(new ClientPacketParserOffthread(this).Start, "clientPacketsParser");
		ClientPacketParsingThread.IsBackground = true;
		ClientPacketParsingThread.Priority = Thread.CurrentThread.Priority;
		Serverthreads.Add(ClientPacketParsingThread);
	}

	public void StandbyLaunch()
	{
		MainSockets[1] = new TcpNetServer();
		UdpSockets[1] = new UdpNetServer(Clients);
		ServerSystemLoadConfig.EnsureConfigExists(this);
		ServerSystemLoadConfig.LoadConfig(this);
		startSockets();
		Logger.Event("Server launched in standby mode. Full launch will commence on first connection attempt. Only /stop and /stats commands will be functioning");
	}

	public void Launch()
	{
		loadedChunksLock = new FastRWLock(this);
		serverChunkDataPool = new ChunkDataPool(MagicNum.ServerChunkSize, this);
		InitBasicPacketHandlers();
		RuntimeEnv.ServerMainThreadId = Environment.CurrentManagedThreadId;
		ModEventManager = new ServerEventManager(this);
		EventManager = new CoreServerEventManager(this, ModEventManager);
		PlayerDataManager = new PlayerDataManager(this);
		ServerSystemModHandler modhandler = new ServerSystemModHandler(this);
		EnterRunPhase(EnumServerRunPhase.Start);
		ServerSystemCompressChunks compresschunks = new ServerSystemCompressChunks(this);
		ServerSystemRelight relight = new ServerSystemRelight(this);
		chunkThread = new ChunkServerThread(this, "chunkdbthread", ServerThreadsCts.Token);
		ServerThreadLoops.Add(chunkThread);
		ServerSystemSupplyChunkCommands supplychunkcpommands = new ServerSystemSupplyChunkCommands(this, chunkThread);
		ChunkServerThread chunkServerThread = chunkThread;
		ServerSystem[] array = new ServerSystem[3];
		ServerSystemSupplyChunks supplychunks = (ServerSystemSupplyChunks)(array[0] = new ServerSystemSupplyChunks(this, chunkThread));
		ServerSystemLoadAndSaveGame loadsavegame = (ServerSystemLoadAndSaveGame)(array[1] = new ServerSystemLoadAndSaveGame(this, chunkThread));
		ServerSystemUnloadChunks unloadchunks = (ServerSystemUnloadChunks)(array[2] = new ServerSystemUnloadChunks(this, chunkThread));
		chunkServerThread.serversystems = array;
		Thread chunkdbthread = new Thread(chunkThread.Process);
		chunkdbthread.Name = "chunkdbthread";
		chunkdbthread.IsBackground = true;
		ServerSystemBlockSimulation serverBlockSimulation = new ServerSystemBlockSimulation(this);
		Serverthreads.AddRange(new Thread[4]
		{
			chunkdbthread,
			CreateThread("CompressChunks", new ServerSystem[1] { compresschunks }, ServerThreadsCts.Token),
			CreateThread("Relight", new ServerSystem[1] { relight }, ServerThreadsCts.Token),
			CreateThread("ServerBlockTicks", new ServerSystem[1] { serverBlockSimulation }, ServerThreadsCts.Token)
		});
		Systems = new ServerSystem[31]
		{
			new ServerSystemUpnp(this),
			clientAwarenessSystem = new ServerSystemClientAwareness(this),
			new ServerSystemLoadConfig(this),
			new ServerSystemNotifyPing(this),
			modhandler,
			new ServerySystemPlayerGroups(this),
			new ServerSystemEntitySimulation(this),
			new ServerSystemCalendar(this),
			new ServerSystemCommands(this),
			new CmdPlayer(this),
			new ServerSystemInventory(this),
			new ServerSystemAutoSaveGame(this),
			compresschunks,
			supplychunks,
			supplychunkcpommands,
			relight,
			new ServerSystemSendChunks(this),
			unloadchunks,
			new ServerSystemBlockIdRemapper(this),
			new ServerSystemItemIdRemapper(this),
			new ServerSystemEntityCodeRemapper(this),
			new ServerSystemMacros(this),
			new ServerSystemEntitySpawner(this),
			new ServerSystemWorldAmbient(this),
			new ServerSystemHeartbeat(this),
			new ServerSystemRemapperAssistant(this),
			loadsavegame,
			serverBlockSimulation,
			ServerUdpNetwork = new ServerUdpNetwork(this),
			new ServerSystemBlockLogger(this),
			new ServerSystemMonitor(this)
		};
		if (xPlatInterface == null)
		{
			xPlatInterface = XPlatformInterfaces.GetInterface();
		}
		Logger.StoryEvent(Lang.Get("It begins..."));
		Logger.Event("Launching server...");
		PlayerDataManager.Load();
		Logger.StoryEvent(Lang.Get("It senses..."));
		Logger.Event("Server v1.20.7, network v1.20.8, api v1.20.0");
		totalUnpausedTime.Start();
		AssetManager = new AssetManager(GamePaths.AssetsPath, EnumAppSide.Server);
		if (progArgs.AddOrigin != null)
		{
			foreach (string item in progArgs.AddOrigin)
			{
				string[] domainPaths = Directory.GetDirectories(item);
				for (int i = 0; i < domainPaths.Length; i++)
				{
					string domain = new DirectoryInfo(domainPaths[i]).Name;
					AssetManager.CustomAppOrigins.Add(new PathOrigin(domain, domainPaths[i]));
				}
			}
		}
		EnterRunPhase(EnumServerRunPhase.Initialization);
		WorldMap = new ServerWorldMap(this);
		Logger.Event("Loading configuration...");
		EnterRunPhase(EnumServerRunPhase.Configuration);
		if (RunPhase == EnumServerRunPhase.Exit)
		{
			return;
		}
		AfterConfigLoaded();
		LoadAssets();
		Logger.Event("Building assets...");
		EnterRunPhase(EnumServerRunPhase.LoadAssets);
		if (RunPhase == EnumServerRunPhase.Exit)
		{
			return;
		}
		if (AssetManager.TryGet("blocktypes/plant/reedpapyrus-free.json", loadAsset: false) != null)
		{
			string msg = (Standalone ? "blocktypes/plant/reedpapyrus-free.json file detected, which breaks stuff. That means this is an incorrectly updated 1.16 server! When up update a server, make sure to delete the old server installation files (but keep the data folder)" : "blocktypes/plant/reedpapyrus-free.json file detected, which breaks the game. Possible corrupted installation. Please uninstall the game, delete the folder %appdata%/VintageStory, then reinstall.");
			Logger.Fatal(msg);
			throw new ApplicationException(msg);
		}
		FinalizeAssets();
		Logger.Event("Server assets loaded, parsed, registered and finalized");
		Logger.Event("Initialising systems...");
		EnterRunPhase(EnumServerRunPhase.LoadGamePre);
		if (RunPhase == EnumServerRunPhase.Exit)
		{
			return;
		}
		AfterSaveGameLoaded();
		if (RunPhase == EnumServerRunPhase.Exit)
		{
			return;
		}
		Logger.StoryEvent(Lang.Get("A world unbroken..."));
		EnterRunPhase(EnumServerRunPhase.GameReady);
		if (RunPhase == EnumServerRunPhase.Exit)
		{
			return;
		}
		EnterRunPhase(EnumServerRunPhase.WorldReady);
		if (RunPhase == EnumServerRunPhase.Exit)
		{
			return;
		}
		StartBuildServerAssetsPacket();
		Logger.StoryEvent(Lang.Get("The center unfolding..."));
		Logger.Event("Starting world generators...");
		ModEventManager.TriggerWorldgenStartup();
		if (RunPhase == EnumServerRunPhase.Exit)
		{
			return;
		}
		Logger.Event("Begin game ticking...");
		Logger.StoryEvent(Lang.Get("...and calls to you."));
		EnterRunPhase(EnumServerRunPhase.RunGame);
		if (RunPhase == EnumServerRunPhase.Exit)
		{
			return;
		}
		Logger.Notification("Starting server threads");
		foreach (Thread thread in Serverthreads)
		{
			if (!thread.IsAlive)
			{
				thread?.Start();
			}
		}
		bool networkedServer = IsDedicatedServer || MainSockets[1] != null;
		string type = (IsDedicatedServer ? Lang.Get("Dedicated Server") : (networkedServer ? Lang.Get("Threaded Server") : Lang.Get("Singleplayer Server")));
		string bind = ((!networkedServer) ? "" : ((CurrentIp == null) ? Lang.Get(" on Port {0} and all ips", CurrentPort) : Lang.Get(" on Port {0} and ip {1}", CurrentPort, CurrentIp)));
		Logger.Event("{0} now running{1}!", type, bind);
		Logger.StoryEvent(Lang.Get("Return again."));
		if ((Config.WhitelistMode == EnumWhitelistMode.Default || Config.WhitelistMode == EnumWhitelistMode.On) && !Config.AdvertiseServer)
		{
			Logger.Notification("Please be aware that as of 1.20, servers default configurations have changed - servers no longer register themselves to the public servers list and are invite-only (whitelisted) out of the box. If you desire so, you can enable server advertising with '/serverconfig advertise on' and disable the whitelist mode with '/serverconfig whitelistmode off'");
		}
		AssetManager.UnloadUnpatchedAssets();
	}

	internal void EnterRunPhase(EnumServerRunPhase runPhase)
	{
		RunPhase = runPhase;
		if (runPhase == EnumServerRunPhase.Start || runPhase == EnumServerRunPhase.Exit)
		{
			return;
		}
		Logger.Notification("Entering runphase " + runPhase);
		Logger.VerboseDebug("Entering runphase " + runPhase);
		ServerSystem[] systems = Systems;
		foreach (ServerSystem system in systems)
		{
			switch (runPhase)
			{
			case EnumServerRunPhase.Initialization:
				suspended = true;
				system.OnBeginInitialization();
				break;
			case EnumServerRunPhase.Configuration:
				system.OnBeginConfiguration();
				break;
			case EnumServerRunPhase.LoadAssets:
				system.OnLoadAssets();
				break;
			case EnumServerRunPhase.AssetsFinalize:
				system.OnFinalizeAssets();
				break;
			case EnumServerRunPhase.LoadGamePre:
				system.OnBeginModsAndConfigReady();
				break;
			case EnumServerRunPhase.GameReady:
				system.OnBeginGameReady(SaveGameData);
				break;
			case EnumServerRunPhase.WorldReady:
				system.OnBeginWorldReady();
				break;
			case EnumServerRunPhase.RunGame:
				suspended = false;
				system.OnBeginRunGame();
				break;
			case EnumServerRunPhase.Shutdown:
				system.OnBeginShutdown();
				break;
			}
		}
	}

	public void AfterConfigLoaded()
	{
		ServerConsoleClient.Player = new ServerConsolePlayer(this, ServerConsoleClient.WorldData);
		if (IsDedicatedServer && MainSockets[1] == null && UdpSockets[1] == null)
		{
			MainSockets[1] = new TcpNetServer();
			UdpSockets[1] = new UdpNetServer(Clients);
			startSockets();
		}
		string[] allPrivs = Privilege.AllCodes();
		for (int i = 0; i < allPrivs.Length; i++)
		{
			AllPrivileges.Add(allPrivs[i]);
			PrivilegeDescriptions.Add(allPrivs[i], allPrivs[i]);
		}
	}

	private void FinalizeAssets()
	{
		foreach (EntityProperties entityType in EntityTypes)
		{
			BlockDropItemStack[] entityDrops = entityType.Drops;
			if (entityDrops == null)
			{
				continue;
			}
			for (int i = 0; i < entityDrops.Length; i++)
			{
				if (!entityDrops[i].Resolve(this, "Entity ", entityType.Code))
				{
					entityDrops = (entityType.Drops = entityDrops.RemoveEntry(i));
					i--;
				}
			}
		}
		ModEventManager.TriggerFinalizeAssets();
		EnterRunPhase(EnumServerRunPhase.AssetsFinalize);
	}

	private void AfterSaveGameLoaded()
	{
		WorldMap.Init(SaveGameData.MapSizeX, SaveGameData.MapSizeY, SaveGameData.MapSizeZ);
		Logger.Notification("Server map set");
		if (MainSockets[1] == null)
		{
			startSockets();
		}
		PlayerRole serverGroup = new PlayerRole
		{
			Name = "Server",
			Code = "server",
			PrivilegeLevel = 9999,
			Privileges = AllPrivileges.ToList(),
			Color = Color.LightSteelBlue
		};
		Config.RolesByCode.Add("server", serverGroup);
		ServerConsoleClient.serverdata = new ServerPlayerData();
		ServerConsoleClient.serverdata.SetRole(serverGroup);
	}

	private void startSockets()
	{
		if (progArgs.Ip != null)
		{
			CurrentIp = progArgs.Ip;
		}
		else if (Config.Ip != null)
		{
			CurrentIp = Config.Ip;
		}
		if (progArgs.Port != null)
		{
			if (!int.TryParse(progArgs.Port, out CurrentPort))
			{
				CurrentPort = Config.Port;
			}
		}
		else
		{
			CurrentPort = Config.Port;
		}
		ClientPacketParsingThread.Start();
		MainSockets[1]?.SetIpAndPort(CurrentIp, CurrentPort);
		MainSockets[1]?.Start();
		UdpSockets[1]?.SetIpAndPort(CurrentIp, CurrentPort);
		UdpSockets[1]?.Start();
	}

	public void Process()
	{
		TickPosition = 0;
		if (Suspended)
		{
			Thread.Sleep(2);
			return;
		}
		if (RunPhase == EnumServerRunPhase.Standby)
		{
			ProcessMain();
			Thread.Sleep(5);
			return;
		}
		FrameProfiler.Begin(Clients.Count - ConnectionQueue.Count + " players online - ");
		TickPosition++;
		ServerThread.SleepMs = ((Clients.Count > 0) ? 2 : 25);
		float dt = (float)lastFramePassedTime.Elapsed.TotalMilliseconds;
		lastFramePassedTime.Restart();
		int millisecondsToSleep = (timeOffsetDuringTick = (int)Math.Max(0f, Config.TickTime - dt));
		long elapsedMS = totalUnpausedTime.ElapsedMilliseconds + millisecondsToSleep;
		TickPosition++;
		try
		{
			if (FrameProfiler.Enabled)
			{
				for (int j = 0; j < Systems.Length; j++)
				{
					long diff = elapsedMS - Systems[j].millisecondsSinceStart;
					if (diff > Systems[j].GetUpdateInterval())
					{
						Systems[j].millisecondsSinceStart = elapsedMS;
						Systems[j].OnServerTick((float)diff / 1000f);
						FrameProfiler.Mark(Systems[j].FrameprofilerName);
					}
					TickPosition++;
				}
				TickPosition++;
				EventManager.TriggerGameTickDebug(elapsedMS, this);
				int l = 0;
				while (FrameProfilerUtil.offThreadProfiles.Count > 0 && l++ < 25)
				{
					FrameProfilerUtil.offThreadProfiles.TryDequeue(out var tickResults);
					Logger.Notification(tickResults);
				}
			}
			else
			{
				for (int k = 0; k < Systems.Length; k++)
				{
					long diff = elapsedMS - Systems[k].millisecondsSinceStart;
					if (diff > Systems[k].GetUpdateInterval())
					{
						Systems[k].millisecondsSinceStart = elapsedMS;
						Systems[k].OnServerTick((float)diff / 1000f);
					}
					TickPosition++;
				}
				FrameProfiler.Mark("ss-tick");
				TickPosition++;
				EventManager.TriggerGameTick(elapsedMS, this);
			}
			TickPosition++;
			FrameProfiler.Mark("ev-tick");
			ProcessMain();
			TickPosition++;
			if ((DateTime.UtcNow - statsupdate).TotalSeconds >= 2.0)
			{
				statsupdate = DateTime.UtcNow;
				StatsCollectorIndex = (StatsCollectorIndex + 1) % 4;
				StatsCollector[StatsCollectorIndex].statTotalPackets = 0;
				StatsCollector[StatsCollectorIndex].statTotalUdpPackets = 0;
				StatsCollector[StatsCollectorIndex].statTotalPacketsLength = 0;
				StatsCollector[StatsCollectorIndex].statTotalUdpPacketsLength = 0;
				StatsCollector[StatsCollectorIndex].tickTimeTotal = 0L;
				StatsCollector[StatsCollectorIndex].ticksTotal = 0L;
				for (int i = 0; i < 10; i++)
				{
					StatsCollector[StatsCollectorIndex].tickTimes[i] = 0L;
				}
			}
			long lastServerTick = lastFramePassedTime.ElapsedMilliseconds;
			StatsCollection coll = StatsCollector[StatsCollectorIndex];
			coll.tickTimeTotal += lastServerTick;
			coll.ticksTotal++;
			coll.tickTimes[coll.tickTimeIndex] = lastServerTick;
			coll.tickTimeIndex = (coll.tickTimeIndex + 1) % coll.tickTimes.Length;
			if (lastServerTick > 500 && totalUnpausedTime.ElapsedMilliseconds > 5000 && !stopped)
			{
				Logger.Warning("Server overloaded. A tick took {0}ms to complete.", lastServerTick);
			}
			FrameProfiler.Mark("timers-updated");
			int excessTickLength = (int)((float)lastServerTick - Config.TickTime);
			if (excessTickLength < millisecondsToSleep)
			{
				millisecondsToSleep = Math.Max(millisecondsToSleep, excessTickLength);
				if (millisecondsToSleep > 0)
				{
					Thread.Sleep(millisecondsToSleep);
					FrameProfiler.Mark("sleep");
				}
			}
			TickPosition++;
		}
		catch (Exception e)
		{
			Logger.Fatal(e);
		}
		timeOffsetDuringTick = 0;
		FrameProfiler.End();
	}

	public void ProcessMain()
	{
		if (MainSockets == null)
		{
			return;
		}
		ProcessMainThreadTasks();
		FrameProfiler.Mark("mtasks");
		ReceivedClientPacket cpk;
		while (ClientPackets.TryDequeue(out cpk))
		{
			try
			{
				HandleClientPacket_mainthread(cpk);
			}
			catch (Exception e)
			{
				if (IsDedicatedServer)
				{
					Logger.Warning("Exception at client " + cpk.client.Id + ". Disconnecting client.");
					DisconnectPlayer(cpk.client, "Threw an exception at the server", "An action you (or your client) did caused an unhandled exception");
				}
				Logger.Error(e);
			}
		}
		DisconnectedClientsThisTick.Clear();
		FrameProfiler.Mark("net-read-done");
		TickPosition++;
		foreach (KeyValuePair<Timer, Timer.Tick> i in Timers)
		{
			i.Key.Update(i.Value);
		}
		TickPosition++;
	}

	public bool Suspend(bool newSuspendState, int maxWaitMilliseconds = 60000)
	{
		if (newSuspendState == suspended)
		{
			return true;
		}
		if (Monitor.TryEnter(suspendLock, 10000))
		{
			try
			{
				suspended = newSuspendState;
				if (suspended)
				{
					totalUnpausedTime.Stop();
					ServerSystem[] systems = Systems;
					for (int i = 0; i < systems.Length; i++)
					{
						systems[i].OnServerPause();
					}
					while (maxWaitMilliseconds > 0 && (ServerThreadLoops.Any((ServerThread st) => !st.paused && st.Alive && st.threadName != "ServerConsole") || !api.eventapi.CanSuspendServer()))
					{
						Thread.Sleep(10);
						maxWaitMilliseconds -= 10;
					}
				}
				else
				{
					totalUnpausedTime.Start();
					ServerSystem[] systems = Systems;
					for (int i = 0; i < systems.Length; i++)
					{
						systems[i].OnServerResume();
					}
					api.eventapi.ResumeServer();
				}
				if (maxWaitMilliseconds <= 0 && suspended)
				{
					Logger.Warning("Server suspend requested, but reached max wait time. Server is only partially suspended.");
				}
				else
				{
					Logger.Notification("Server ticking has been {0}", suspended ? "suspended" : "resumed");
				}
				return maxWaitMilliseconds > 0;
			}
			finally
			{
				Monitor.Exit(suspendLock);
			}
		}
		return false;
	}

	public void AttemptShutdown(string reason, int timeout)
	{
		if (Environment.CurrentManagedThreadId == RuntimeEnv.MainThreadId)
		{
			Stop(reason);
			return;
		}
		if (RunPhase == EnumServerRunPhase.RunGame)
		{
			EnqueueMainThreadTask(delegate
			{
				Stop(reason);
			});
			for (int i = 0; i < timeout / 15; i++)
			{
				if (stopped)
				{
					return;
				}
				Thread.Sleep(15);
			}
		}
		Stop("Forced: " + reason);
	}

	public void Stop(string reason, string finalLogMessage = null, EnumLogType finalLogType = EnumLogType.Notification)
	{
		if (RunPhase == EnumServerRunPhase.Exit || stopped)
		{
			return;
		}
		stopped = true;
		if (FrameProfiler == null)
		{
			FrameProfiler = new FrameProfilerUtil(delegate(string text)
			{
				Logger.Notification(text);
			});
			FrameProfiler.Begin(null);
		}
		try
		{
			ServerConfig config = Config;
			if (config != null && config.RepairMode)
			{
				foreach (ConnectedClient client2 in Clients.Values)
				{
					if (client2.Player?.WorldData != null)
					{
						client2.Player.WorldData.CurrentGameMode = EnumGameMode.Survival;
						client2.Player.WorldData.FreeMove = false;
						client2.Player.WorldData.NoClip = false;
					}
				}
			}
			ConnectedClient[] array = Clients.Values.ToArray();
			foreach (ConnectedClient client in array)
			{
				string msg = "Server shutting down - " + reason;
				DisconnectPlayer(client, msg, msg);
			}
		}
		catch (Exception e6)
		{
			LogShutdownException(e6);
		}
		Logger.Notification("Server stop requested, begin shutdown sequence. Stop reason: {0}", reason);
		if (reason.Contains("Exception"))
		{
			Logger.StoryEvent(Lang.Get("Something went awry...please check the program logs... ({0})", reason));
		}
		try
		{
			Suspend(newSuspendState: true, 10000);
		}
		catch (Exception e5)
		{
			LogShutdownException(e5);
		}
		new Stopwatch().Start();
		Thread.Sleep(20);
		try
		{
			EnterRunPhase(EnumServerRunPhase.Shutdown);
		}
		catch (Exception e4)
		{
			LogShutdownException(e4);
		}
		try
		{
			if (Blocks != null)
			{
				foreach (Block block in Blocks)
				{
					block?.OnUnloaded(api);
				}
			}
		}
		catch (Exception e3)
		{
			LogShutdownException(e3);
		}
		try
		{
			if (Items != null)
			{
				foreach (Item item in Items)
				{
					item?.OnUnloaded(api);
				}
			}
		}
		catch (Exception e2)
		{
			LogShutdownException(e2);
		}
		Logger.Event("Shutting down {0} server threads... ", Serverthreads.Count);
		_consoleThreadsCts.Cancel();
		Logger.Event("Killed console thread");
		Logger.StoryEvent(Lang.Get("Alone again..."));
		ServerThread.shouldExit = true;
		int shutDownGraceTimer = 120;
		bool anyThreadAlive = false;
		int timer = shutDownGraceTimer;
		while (timer-- > 0)
		{
			Thread.Sleep(500);
			anyThreadAlive = Serverthreads.Aggregate(seed: false, (bool current, Thread t) => current || t.IsAlive);
			if (!anyThreadAlive)
			{
				break;
			}
			if (timer < shutDownGraceTimer - 10 && timer % 4 == 0)
			{
				Logger.Event("Waiting for a server thread to shut down ({0}/{1})...", timer / 2, shutDownGraceTimer / 2);
			}
		}
		if (anyThreadAlive)
		{
			string threadnames = string.Join(", ", from t in Serverthreads
				where t.IsAlive
				select t.Name);
			Logger.Event("One or more server threads {0} didn't shut down within {1}ms, forcefully shutting them down...", threadnames, shutDownGraceTimer * 500);
			ServerThreadsCts.Cancel();
		}
		else
		{
			Logger.Event("All threads gracefully shut down");
		}
		Logger.StoryEvent(Lang.Get("Time to rest."));
		Logger.Event("Doing last tick...");
		try
		{
			ProcessMain();
		}
		catch (Exception e)
		{
			LogShutdownException(e);
		}
		Logger.Event("Stopped the server!");
		ServerThread.shouldExit = false;
		for (int j = 0; j < MainSockets.Length; j++)
		{
			MainSockets[j]?.Dispose();
		}
		for (int i = 0; i < UdpSockets.Length; i++)
		{
			UdpSockets[i]?.Dispose();
		}
		EnterRunPhase(EnumServerRunPhase.Exit);
		exit.SetExit(p: true);
		if (finalLogMessage != null)
		{
			Logger.Log(finalLogType, finalLogMessage);
		}
		Logger.ClearWatchers();
	}

	private void LogShutdownException(Exception exception)
	{
		Logger.Error("While shutting down the server:");
		Logger.Error(exception);
	}

	public void Dispose()
	{
		serverAssetsPacket.Dispose();
		serverAssetsSentLocally = false;
		worldMetaDataPacketAlreadySentToSinglePlayer = false;
		lock (reusableBuffersDisposalList)
		{
			foreach (BoxedPacket reusableBuffersDisposal in reusableBuffersDisposalList)
			{
				reusableBuffersDisposal.Dispose();
			}
			reusableBuffersDisposalList.Clear();
		}
		ServerSystem[] systems = Systems;
		for (int i = 0; i < systems.Length; i++)
		{
			systems[i].Dispose();
		}
		TyronThreadPool.Inst.Dispose();
		ClassRegistry = null;
		Logger?.Dispose();
		Logger = null;
		_consoleThreadsCts.Dispose();
		serverConsole?.Dispose();
		ServerThreadsCts.Dispose();
		rand?.Dispose();
	}

	public bool DidExit()
	{
		return RunPhase == EnumServerRunPhase.Exit;
	}

	public void ReceiveServerConsole(string message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return;
		}
		if (message.StartsWith('/'))
		{
			string command = message.Split(new char[1] { ' ' })[0].Replace("/", "");
			string args = ((message.IndexOf(' ') < 0) ? "" : message.Substring(message.IndexOf(' ') + 1));
			Logger.Notification("Handling Console Command /{0} {1}", command, args);
			api.commandapi.Execute(command, new TextCommandCallingArgs
			{
				Caller = new Caller
				{
					Type = EnumCallerType.Console,
					CallerRole = "admin",
					CallerPrivileges = new string[1] { "*" },
					FromChatGroupId = GlobalConstants.ConsoleGroup
				},
				RawArgs = new CmdArgs(args)
			}, delegate(TextCommandResult result)
			{
				if (result.StatusMessage != null)
				{
					Logger.Notification(result.StatusMessage);
				}
			});
		}
		else if (!message.StartsWith('.'))
		{
			BroadcastMessageToAllGroups($"<strong>Admin:</strong>{message}", EnumChatType.AllGroups);
			Logger.Chat(string.Format("{0}: {1}", ServerConsoleClient.PlayerName, message.Replace("{", "{{").Replace("}", "}}")));
		}
	}

	public string GetSaveFilename()
	{
		if (Config.WorldConfig.SaveFileLocation != null)
		{
			return Config.WorldConfig.SaveFileLocation;
		}
		return Path.Combine(GamePaths.Saves, GamePaths.DefaultSaveFilenameWithoutExtension + ".vcdbs");
	}

	public int GenerateClientId()
	{
		if (nextClientID + 1 < 0)
		{
			nextClientID = 1;
		}
		return nextClientID++;
	}

	public void DisconnectPlayer(ConnectedClient client, string othersKickmessage = null, string hisKickMessage = null)
	{
		if (client == null || ignoreDisconnectCalls || !Clients.ContainsKey(client.Id))
		{
			return;
		}
		ServerPlayer player = client.Player;
		if (!client.IsNewClient || player != null || !string.IsNullOrEmpty(hisKickMessage))
		{
			ignoreDisconnectCalls = true;
			try
			{
				SendPacket(client.Id, ServerPackets.DisconnectPlayer(hisKickMessage));
			}
			catch
			{
			}
			Logger.Notification($"Client {client.Id} disconnected: {hisKickMessage}");
			ignoreDisconnectCalls = false;
		}
		lastDisconnectTotalMs = totalUpTime.ElapsedMilliseconds;
		if (client.IsNewClient)
		{
			Clients.Remove(client.Id);
			client.CloseConnection();
			UpdateQueuedPlayersAfterDisconnect(client);
		}
		else if (player != null)
		{
			if (othersKickmessage != null && othersKickmessage.Length > 0)
			{
				Logger.Audit("Client {0} got removed: '{1}' ({2})", client.PlayerName, othersKickmessage, hisKickMessage);
			}
			else
			{
				Logger.Audit("Client {0} disconnected.", client.PlayerName);
			}
			EventManager.TriggerPlayerDisconnect(player);
			ServerSystem[] systems = Systems;
			for (int i = 0; i < systems.Length; i++)
			{
				systems[i].OnPlayerDisconnect(player);
			}
			BroadcastPacket(new Packet_Server
			{
				Id = 41,
				PlayerData = new Packet_PlayerData
				{
					PlayerUID = player.PlayerUID,
					ClientId = -99
				}
			}, player);
			EntityPlayer playerEntity = player.Entity;
			if (playerEntity != null)
			{
				DespawnEntity(playerEntity, new EntityDespawnData
				{
					Reason = EnumDespawnReason.Disconnect
				});
			}
			string playerName = client.PlayerName;
			Clients.Remove(client.Id);
			player.client = null;
			if (client.State == EnumClientState.Connected || client.State == EnumClientState.Playing)
			{
				othersKickmessage = ((othersKickmessage != null) ? string.Format(Lang.Get("Player {0} got removed. Reason: {1}", playerName, othersKickmessage)) : string.Format(Lang.Get("Player {0} left.", playerName)));
				SendMessageToGeneral(othersKickmessage, EnumChatType.JoinLeave);
				Logger.Event(othersKickmessage);
			}
			client.CloseConnection();
			UpdateQueuedPlayersAfterDisconnect(client);
		}
		else
		{
			Clients.Remove(client.Id);
			client.CloseConnection();
		}
	}

	private void UpdateQueuedPlayersAfterDisconnect(ConnectedClient client)
	{
		if (Config.MaxClientsInQueue <= 0 || stopped)
		{
			return;
		}
		List<QueuedClient> nextPlayers = null;
		QueuedClient[] updatedPositions = null;
		int count;
		lock (ConnectionQueue)
		{
			if (client.State == EnumClientState.Queued)
			{
				ConnectionQueue.RemoveAll((QueuedClient e) => e.Client.Id == client.Id);
			}
			count = ConnectionQueue.Count;
			if (count > 0)
			{
				int maxClients = Config.GetMaxClients(this);
				int clientsCount = Clients.Count - count;
				int clientsToConnect = Math.Max(0, maxClients - clientsCount);
				if (clientsToConnect > 0)
				{
					nextPlayers = new List<QueuedClient>();
					for (int j = 0; j < clientsToConnect; j++)
					{
						if (ConnectionQueue.Count > 0)
						{
							QueuedClient nextPlayer2 = ConnectionQueue.First();
							ConnectionQueue.RemoveAll((QueuedClient e) => e.Client.Id == nextPlayer2.Client.Id);
							nextPlayers.Add(nextPlayer2);
						}
					}
				}
				updatedPositions = ConnectionQueue.ToArray();
			}
		}
		if (count <= 0)
		{
			return;
		}
		if (nextPlayers != null)
		{
			foreach (QueuedClient nextPlayer in nextPlayers)
			{
				FinalizePlayerIdentification(nextPlayer.Identification, nextPlayer.Client, nextPlayer.Entitlements);
			}
		}
		if (updatedPositions != null)
		{
			for (int i = 0; i < updatedPositions.Length; i++)
			{
				QueuedClient queuedClient = updatedPositions[i];
				Packet_Server pq = new Packet_Server
				{
					Id = 82,
					QueuePacket = new Packet_QueuePacket
					{
						Position = i + 1
					}
				};
				SendPacket(queuedClient.Client.Id, pq);
			}
		}
	}

	public int GetPlayingClients()
	{
		return Clients.Count((KeyValuePair<int, ConnectedClient> c) => c.Value.State == EnumClientState.Playing);
	}

	public int GetAllowedChunkRadius(ConnectedClient client)
	{
		int desiredChunkRadius = (int)Math.Ceiling((float)((client.WorldData == null) ? 128 : client.WorldData.Viewdistance) / (float)MagicNum.ServerChunkSize);
		int reducedChunkRadius = Math.Min(Config.MaxChunkRadius, desiredChunkRadius);
		if (client.FromSocketListener.GetType() == typeof(DummyTcpNetServer))
		{
			return desiredChunkRadius;
		}
		return reducedChunkRadius;
	}

	public FuzzyEntityPos GetSpawnPosition(string playerUID = null, bool onlyGlobalDefaultSpawn = false, bool consumeSpawn = false)
	{
		PlayerSpawnPos playerSpawn = null;
		ServerPlayerData serverPlayerData = GetServerPlayerData(playerUID);
		ServerPlayer plrdata = PlayerByUid(playerUID) as ServerPlayer;
		PlayerRole plrrole = serverPlayerData.GetPlayerRole(this);
		float radius = 0f;
		if (plrrole.ForcedSpawn != null && !onlyGlobalDefaultSpawn)
		{
			playerSpawn = plrrole.ForcedSpawn;
			if (consumeSpawn && playerSpawn != null && playerSpawn.RemainingUses > 0)
			{
				playerSpawn.RemainingUses--;
				if (playerSpawn.RemainingUses <= 0)
				{
					plrrole.ForcedSpawn = null;
				}
			}
		}
		if (playerSpawn == null && plrdata?.WorldData != null && !onlyGlobalDefaultSpawn)
		{
			playerSpawn = (plrdata.WorldData as ServerWorldPlayerData).SpawnPosition;
			if (consumeSpawn && playerSpawn != null && playerSpawn.RemainingUses > 0)
			{
				playerSpawn.RemainingUses--;
				if (playerSpawn.RemainingUses <= 0)
				{
					(plrdata.WorldData as ServerWorldPlayerData).SpawnPosition = null;
				}
			}
		}
		if (playerSpawn == null && !onlyGlobalDefaultSpawn)
		{
			playerSpawn = plrrole.DefaultSpawn;
			if (consumeSpawn && playerSpawn != null && playerSpawn.RemainingUses > 0)
			{
				playerSpawn.RemainingUses--;
				if (playerSpawn.RemainingUses <= 0)
				{
					plrrole.DefaultSpawn = null;
				}
			}
		}
		if (playerSpawn == null)
		{
			playerSpawn = SaveGameData.DefaultSpawn;
			if (playerSpawn != null)
			{
				playerSpawn.RemainingUses = 99;
			}
			radius = World.Config.GetString("spawnRadius").ToInt();
		}
		if (playerSpawn == null)
		{
			playerSpawn = mapMiddleSpawnPos;
			if (playerSpawn != null)
			{
				playerSpawn.RemainingUses = 99;
			}
			radius = World.Config.GetString("spawnRadius").ToInt();
		}
		FuzzyEntityPos fuzzyEntityPos = EntityPosFromSpawnPos(playerSpawn);
		fuzzyEntityPos.Radius = radius;
		fuzzyEntityPos.UsesLeft = playerSpawn.RemainingUses;
		return fuzzyEntityPos;
	}

	public EntityPos GetJoinPosition(ConnectedClient client)
	{
		PlayerRole plrgroup = client.ServerData.GetPlayerRole(this);
		if (plrgroup.ForcedSpawn != null)
		{
			return EntityPosFromSpawnPos(plrgroup.ForcedSpawn);
		}
		EntityPos serverPos = client.Entityplayer.ServerPos;
		EntityPos pos = client.Entityplayer.Pos;
		if (serverPos.AnyNaN())
		{
			Logger.Error("Player " + client.PlayerName + " has an impossible (bugged) ServerPos, placing player at world spawn.");
			serverPos.SetFrom(DefaultSpawnPosition);
			pos.SetFrom(DefaultSpawnPosition);
		}
		if (pos.AnyNaN())
		{
			Logger.Error("Player " + client.PlayerName + " has an impossible (bugged) Pos, placing player at world spawn.");
			serverPos.SetFrom(DefaultSpawnPosition);
			pos.SetFrom(DefaultSpawnPosition);
		}
		return serverPos;
	}

	private FuzzyEntityPos EntityPosFromSpawnPos(PlayerSpawnPos playerSpawn)
	{
		if (!playerSpawn.y.HasValue || playerSpawn.y == 0)
		{
			playerSpawn.y = WorldMap.GetTerrainGenSurfacePosY(playerSpawn.x, playerSpawn.z);
			if (!playerSpawn.y.HasValue)
			{
				return null;
			}
		}
		if (!WorldMap.IsValidPos(playerSpawn.x, playerSpawn.y.Value, playerSpawn.z))
		{
			if (Config.RepairMode)
			{
				int x = SaveGameData.MapSizeX / 2;
				int z = SaveGameData.MapSizeZ / 2;
				return new FuzzyEntityPos(x, WorldMap.GetTerrainGenSurfacePosY(x, z), z);
			}
			throw new Exception("Invalid spawn coordinates found. It is outside the world map.");
		}
		return new FuzzyEntityPos((double)playerSpawn.x + 0.5, playerSpawn.y.Value, (double)playerSpawn.z + 0.5)
		{
			Pitch = (float)Math.PI,
			Yaw = ((!playerSpawn.yaw.HasValue) ? ((float)rand.Value.NextDouble() * 2f * (float)Math.PI) : playerSpawn.yaw.Value)
		};
	}

	private void LoadAssets()
	{
		Logger.Notification("Start discovering assets");
		int quantity = AssetManager.InitAndLoadBaseAssets(Logger);
		Logger.Notification("Found {0} base assets in total", quantity);
	}

	public ConnectedClient GetClientByPlayername(string playerName)
	{
		foreach (ConnectedClient client in Clients.Values)
		{
			if (client.PlayerName.ToLowerInvariant() == playerName.ToLowerInvariant())
			{
				return client;
			}
		}
		return null;
	}

	public void GetOnlineOrOfflinePlayer(string targetPlayerName, Action<EnumServerResponse, string> onPlayerReceived)
	{
		ConnectedClient targetClient = GetClientByPlayername(targetPlayerName);
		if (targetClient == null)
		{
			AuthServerComm.ResolvePlayerName(targetPlayerName, delegate(EnumServerResponse result, string playeruid)
			{
				EnqueueMainThreadTask(delegate
				{
					onPlayerReceived(result, playeruid);
					FrameProfiler.Mark("onplayerreceived");
				});
			});
		}
		else
		{
			onPlayerReceived(EnumServerResponse.Good, targetClient.WorldData.PlayerUID);
		}
	}

	public void GetOnlineOrOfflinePlayerByUid(string targetPlayeruid, Action<EnumServerResponse, string> onPlayerReceived)
	{
		ConnectedClient targetClient = GetClientByUID(targetPlayeruid);
		if (targetClient == null)
		{
			AuthServerComm.ResolvePlayerUid(targetPlayeruid, delegate(EnumServerResponse result, string playername)
			{
				EnqueueMainThreadTask(delegate
				{
					onPlayerReceived(result, playername);
					FrameProfiler.Mark("onplayerreceived");
				});
			});
		}
		else
		{
			onPlayerReceived(EnumServerResponse.Good, targetClient.WorldData.PlayerUID);
		}
	}

	public ConnectedClient GetClient(int id)
	{
		if (id == serverConsoleId)
		{
			return ServerConsoleClient;
		}
		if (!Clients.ContainsKey(id))
		{
			return null;
		}
		return Clients[id];
	}

	public ConnectedClient GetClientByUID(string playerUID)
	{
		if (ServerConsoleClient.WorldData.PlayerUID.Equals(playerUID, StringComparison.InvariantCultureIgnoreCase))
		{
			return ServerConsoleClient;
		}
		foreach (KeyValuePair<int, ConnectedClient> i in Clients)
		{
			if (i.Value.WorldData?.PlayerUID != null && i.Value.WorldData.PlayerUID.Equals(playerUID, StringComparison.InvariantCultureIgnoreCase))
			{
				return i.Value;
			}
		}
		return null;
	}

	internal void RemapItem(Item removedItem)
	{
		while (removedItem.ItemId >= Items.Count)
		{
			Items.Add(new Item
			{
				ItemId = Items.Count,
				IsMissing = true
			});
		}
		if (Items[removedItem.ItemId] != null && Items[removedItem.ItemId].Code != null)
		{
			Item prevItem = Items[removedItem.ItemId];
			Items[removedItem.ItemId] = new Item();
			ItemsByCode.Remove(prevItem.Code);
			RegisterItem(prevItem);
		}
		Items[removedItem.ItemId] = removedItem;
		nextFreeItemId = Math.Max(nextFreeItemId, removedItem.ItemId + 1);
	}

	internal void FillMissingItem(int ItemId, Item Item)
	{
		Item noitem = new Item(0);
		while (ItemId >= Items.Count)
		{
			Items.Add(noitem);
		}
		Item.ItemId = ItemId;
		Items[ItemId] = Item;
		ItemsByCode[Item.Code] = Item;
		nextFreeItemId = Math.Max(nextFreeItemId, Item.ItemId + 1);
	}

	internal void RemapBlock(Block removedBlock)
	{
		new FastSmallDictionary<string, CompositeTexture>("all", new CompositeTexture(new AssetLocation("unknown")));
		while (removedBlock.BlockId >= Blocks.Count)
		{
			Blocks.Add(new Block
			{
				BlockId = Blocks.Count,
				IsMissing = true
			});
		}
		if (Blocks[removedBlock.BlockId] != null && Blocks[removedBlock.BlockId].Code != null)
		{
			Block prevBlock = Blocks[removedBlock.BlockId];
			Blocks[removedBlock.BlockId] = new Block
			{
				BlockId = removedBlock.Id
			};
			BlocksByCode.Remove(prevBlock.Code);
			RegisterBlock(prevBlock);
		}
		Blocks[removedBlock.BlockId] = removedBlock;
		nextFreeBlockId = Math.Max(nextFreeBlockId, removedBlock.BlockId + 1);
	}

	internal void FillMissingBlock(int blockId, Block block)
	{
		block.BlockId = blockId;
		Blocks[blockId] = block;
		BlocksByCode[block.Code] = block;
		nextFreeBlockId = Math.Max(nextFreeBlockId, block.BlockId + 1);
	}

	public void RegisterBlock(Block block)
	{
		if (block.Code == null || block.Code.Path.Length == 0)
		{
			throw new Exception(Lang.Get("Attempted to register Block with no code. Must use a unique code"));
		}
		if (BlocksByCode.ContainsKey(block.Code))
		{
			throw new Exception(Lang.Get("Block must have a unique code ('{0}' is already in use). This is often caused right after a game update when there are old installation files left behind. Try full uninstall and reinstall.", block.Code));
		}
		if (block.Sounds == null)
		{
			block.Sounds = new BlockSounds();
		}
		if (nextFreeBlockId >= Blocks.Count)
		{
			FastSmallDictionary<string, CompositeTexture> unknownTex = new FastSmallDictionary<string, CompositeTexture>("all", new CompositeTexture(new AssetLocation("unknown")));
			(Blocks as BlockList).PreAlloc(nextFreeBlockId + 1);
			while (Blocks.Count <= nextFreeBlockId)
			{
				Blocks.Add(new Block
				{
					Textures = unknownTex,
					Code = new AssetLocation("unknown"),
					BlockId = Blocks.Count,
					DrawType = EnumDrawType.Cube,
					MatterState = EnumMatterState.Solid,
					IsMissing = true,
					Replaceable = 1
				});
			}
		}
		block.BlockId = nextFreeBlockId;
		Blocks[nextFreeBlockId] = block;
		BlocksByCode.Add(block.Code, block);
		nextFreeBlockId++;
	}

	internal void RegisterItem(Item item)
	{
		if (item.Code == null || item.Code.Path.Length == 0)
		{
			throw new Exception(Lang.Get("Attempted to register Item with no code. Must use a unique code"));
		}
		if (ItemsByCode.ContainsKey(item.Code))
		{
			throw new Exception(Lang.Get("Attempted to register Item with code {0}, but an item with such code already exists. Must use a unique code", item.Code));
		}
		if (nextFreeItemId >= Items.Count)
		{
			while (Items.Count <= nextFreeItemId)
			{
				Items.Add(new Item
				{
					Textures = new Dictionary<string, CompositeTexture> { 
					{
						"all",
						new CompositeTexture(new AssetLocation("unknown"))
					} },
					Code = new AssetLocation("unknown"),
					ItemId = Items.Count,
					MatterState = EnumMatterState.Solid,
					IsMissing = true
				});
			}
		}
		item.ItemId = nextFreeItemId;
		Items[nextFreeItemId] = item;
		ItemsByCode.Add(item.Code, item);
		nextFreeItemId++;
	}

	public Item GetItem(int itemId)
	{
		if (Items.Count <= itemId)
		{
			return null;
		}
		return Items[itemId];
	}

	public Block GetBlock(int blockId)
	{
		return Blocks[blockId];
	}

	public EntityProperties GetEntityType(AssetLocation entityCode)
	{
		EntityTypesByCode.TryGetValue(entityCode, out var eclass);
		return eclass;
	}

	public void SetSeaLevel(int seaLevel)
	{
		this.seaLevel = seaLevel;
	}

	public void SetBlockLightLevels(float[] lightLevels)
	{
		blockLightLevels = lightLevels;
	}

	public void SetSunLightLevels(float[] lightLevels)
	{
		sunLightLevels = lightLevels;
	}

	internal void SetSunBrightness(int lightlevel)
	{
		sunBrightness = lightlevel;
	}

	private List<string> makeEntityCodesCache()
	{
		ICollection<AssetLocation> keys = EntityTypesByCode.Keys;
		List<string> list = new List<string>(keys.Count);
		foreach (AssetLocation key in keys)
		{
			list.Add(key.ToShortString());
		}
		return list;
	}

	public ConnectedClient GetConnectedClient(string playerUID)
	{
		foreach (ConnectedClient client in Clients.Values)
		{
			if (client.WorldData?.PlayerUID == playerUID)
			{
				return client;
			}
		}
		return null;
	}

	public IWorldPlayerData GetWorldPlayerData(string playerUID)
	{
		if (playerUID == null)
		{
			return null;
		}
		PlayerDataManager.WorldDataByUID.TryGetValue(playerUID, out var plrdata);
		if (plrdata == null)
		{
			return GetConnectedClient(playerUID)?.WorldData;
		}
		return plrdata;
	}

	public ServerPlayerData FindServerPlayerDataByLastKnownPlayerName(string playerName)
	{
		foreach (ServerPlayerData plrData in PlayerDataManager.PlayerDataByUid.Values)
		{
			if (plrData.LastKnownPlayername.ToLowerInvariant() == playerName.ToLowerInvariant())
			{
				return plrData;
			}
		}
		return null;
	}

	public ServerPlayerData GetServerPlayerData(string playeruid)
	{
		PlayerDataManager.PlayerDataByUid.TryGetValue(playeruid, out var plrData);
		return plrData;
	}

	public bool PlayerHasPrivilege(int clientid, string privilege)
	{
		if (privilege == null)
		{
			return true;
		}
		if (clientid == serverConsoleId)
		{
			return true;
		}
		if (!Clients.ContainsKey(clientid))
		{
			return false;
		}
		return Clients[clientid].ServerData.HasPrivilege(privilege, Config.RolesByCode);
	}

	public void PlaySoundAt(string location, IPlayer atPlayer, IPlayer ignorePlayerUID = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		PlaySoundAt(new AssetLocation(location), atPlayer, ignorePlayerUID, randomizePitch, range, volume);
	}

	public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer, EnumSoundType soundType, float pitch, float range = 32f, float volume = 1f)
	{
		PlaySoundAtExceptPlayer(location, posx, posy, posz, dualCallByPlayer?.ClientId, pitch, range, volume, soundType);
	}

	public void PlaySoundAt(AssetLocation location, Entity entity, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		float yoff = 0f;
		if (entity.SelectionBox != null)
		{
			yoff = entity.SelectionBox.Y2 / 2f;
		}
		else if (entity.Properties?.CollisionBoxSize != null)
		{
			yoff = entity.Properties.CollisionBoxSize.Y / 2f;
		}
		PlaySoundAt(location, entity.ServerPos.X, entity.ServerPos.InternalY + (double)yoff, entity.ServerPos.Z, dualCallByPlayer, randomizePitch, range, volume);
	}

	public void PlaySoundAt(AssetLocation location, IPlayer atPlayer, IPlayer ignorePlayerUID = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		if (atPlayer != null)
		{
			int? clientId = null;
			if (ignorePlayerUID != null)
			{
				clientId = GetConnectedClient(ignorePlayerUID.PlayerUID)?.Id;
			}
			float pitch = (randomizePitch ? RandomPitch() : 1f);
			PlaySoundAtExceptPlayer(location, atPlayer.Entity.Pos.X, atPlayer.Entity.Pos.InternalY, atPlayer.Entity.Pos.Z, clientId, pitch, range, volume);
		}
	}

	public void PlaySoundAt(AssetLocation location, Entity entity, IPlayer ignorePlayerUID, float pitch, float range = 32f, float volume = 1f)
	{
		float yoff = 0f;
		if (entity.SelectionBox != null)
		{
			yoff = entity.SelectionBox.Y2 / 2f;
		}
		else if (entity.Properties?.CollisionBoxSize != null)
		{
			yoff = entity.Properties.CollisionBoxSize.Y / 2f;
		}
		PlaySoundAt(location, entity.ServerPos.X, entity.ServerPos.InternalY + (double)yoff, entity.ServerPos.Z, ignorePlayerUID, pitch, range, volume);
	}

	public void PlaySoundAt(string location, double posx, double posy, double posz, IPlayer ignorePlayerUID = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		PlaySoundAt(new AssetLocation(location), posx, posy, posz, ignorePlayerUID, randomizePitch, range, volume);
	}

	public void PlaySoundAt(AssetLocation location, BlockPos pos, double yOffsetFromCenter, IPlayer ignorePlayerUid = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		PlaySoundAt(location, (double)pos.X + 0.5, (double)pos.InternalY + 0.5 + yOffsetFromCenter, (double)pos.Z + 0.5, ignorePlayerUid, randomizePitch, range, volume);
	}

	public void PlaySoundAt(string location, Entity entity, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		PlaySoundAt(new AssetLocation(location), entity.ServerPos.X, entity.ServerPos.InternalY, entity.ServerPos.Z, dualCallByPlayer, randomizePitch, range, volume);
	}

	public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer ignorePlayerUID = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		if (!(location == null))
		{
			int? clientId = null;
			if (ignorePlayerUID != null)
			{
				clientId = GetConnectedClient(ignorePlayerUID.PlayerUID)?.Id;
			}
			float pitch = (randomizePitch ? RandomPitch() : 1f);
			PlaySoundAtExceptPlayer(location, posx, posy, posz, clientId, pitch, range, volume);
		}
	}

	public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer ignorePlayerUID, float pitch, float range = 32f, float volume = 1f)
	{
		if (!(location == null))
		{
			int? clientId = null;
			if (ignorePlayerUID != null)
			{
				clientId = GetConnectedClient(ignorePlayerUID.PlayerUID)?.Id;
			}
			PlaySoundAtExceptPlayer(location, posx, posy, posz, clientId, pitch, range, volume);
		}
	}

	public void PlaySoundFor(AssetLocation location, IPlayer forPlayer, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		float pitch = (randomizePitch ? RandomPitch() : 1f);
		SendSound(forPlayer as IServerPlayer, location, 0.0, 0.0, 0.0, pitch, range, volume);
	}

	public void PlaySoundFor(AssetLocation location, IPlayer forPlayer, float pitch, float range = 32f, float volume = 1f)
	{
		SendSound(forPlayer as IServerPlayer, location, 0.0, 0.0, 0.0, pitch, range, volume);
	}

	public void PlaySoundAtExceptPlayer(AssetLocation location, double posx, double posy, double posz, int? clientId = null, float pitch = 1f, float range = 32f, float volume = 1f, EnumSoundType soundType = EnumSoundType.Sound)
	{
		if (location == null)
		{
			return;
		}
		foreach (KeyValuePair<int, ConnectedClient> i in Clients)
		{
			if (clientId != i.Key && i.Value.State == EnumClientState.Playing && i.Value.Position.InRangeOf(posx, posy, posz, range * range))
			{
				SendSound(i.Value.Player, location, posx, posy, posz, pitch, range, volume, soundType);
			}
		}
	}

	public void TriggerNeighbourBlocksUpdate(BlockPos pos)
	{
		Block liquidBlock = WorldMap.RelaxedBlockAccess.GetBlock(pos, 2);
		if (liquidBlock.IsLiquid())
		{
			liquidBlock.OnNeighbourBlockChange(this, pos, pos);
		}
		BlockPos neibPos = new BlockPos(pos.dimension);
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing facing in aLLFACES)
		{
			neibPos.Set(pos).Offset(facing);
			if (!worldmap.IsValidPos(neibPos))
			{
				continue;
			}
			Block block = WorldMap.RelaxedBlockAccess.GetBlock(neibPos);
			block.OnNeighbourBlockChange(this, neibPos, pos);
			if (block.ForFluidsLayer)
			{
				continue;
			}
			liquidBlock = WorldMap.RelaxedBlockAccess.GetBlock(neibPos, 2);
			if (liquidBlock.BlockId == 0)
			{
				continue;
			}
			EnumHandling handled = EnumHandling.PassThrough;
			BlockBehavior[] blockBehaviors = liquidBlock.BlockBehaviors;
			for (int j = 0; j < blockBehaviors.Length; j++)
			{
				blockBehaviors[j].OnNeighbourBlockChange(this, neibPos, pos, ref handled);
				if (handled == EnumHandling.PreventSubsequent)
				{
					break;
				}
			}
		}
	}

	internal Entity GetEntity(long entityId)
	{
		LoadedEntities.TryGetValue(entityId, out var entity);
		return entity;
	}

	public override bool IsValidPos(BlockPos pos)
	{
		return WorldMap.IsValidPos(pos);
	}

	public override Block GetBlock(BlockPos pos)
	{
		return WorldMap.RelaxedBlockAccess.GetBlock(pos);
	}

	public bool IsFullyLoadedChunk(BlockPos pos)
	{
		return ((ServerChunk)WorldMap.GetChunk(pos))?.NotAtEdge ?? false;
	}

	public Entity SpawnItemEntity(ItemStack itemstack, Vec3d position, Vec3d velocity = null)
	{
		if (itemstack == null || itemstack.StackSize <= 0)
		{
			return null;
		}
		Entity entity = EntityItem.FromItemstack(itemstack, position, velocity, this);
		SpawnEntity(entity);
		return entity;
	}

	public Entity SpawnItemEntity(ItemStack itemstack, BlockPos pos, Vec3d velocity = null)
	{
		return SpawnItemEntity(itemstack, pos.ToVec3d().Add(0.5), velocity);
	}

	public bool LoadEntity(Entity entity, long fromChunkIndex3d)
	{
		try
		{
			if (Config.RepairMode)
			{
				SaveGameData.LastEntityId = Math.Max(SaveGameData.LastEntityId, entity.EntityId);
			}
			EntityProperties type = api.World.GetEntityType(entity.Code);
			if (type == null)
			{
				Logger.Warning("Couldn't load entity class {0} saved type code {1} - its Type is null! Will remove from chunk, sorry!", entity.GetType(), entity.Code);
				return false;
			}
			entity.Initialize(type.Clone(), api, fromChunkIndex3d);
			entity.AfterInitialized(onFirstSpawn: false);
			if (!LoadedEntities.TryAdd(entity.EntityId, entity))
			{
				Logger.Warning("Couldn't add entity {0}, type {1} to list of loaded entities (duplicate entityid)! Will remove from chunk, sorry!", entity.EntityId, entity.Properties.Code);
				return false;
			}
			entity.OnEntityLoaded();
			EventManager.TriggerEntityLoaded(entity);
			return true;
		}
		catch (Exception e)
		{
			Logger.Error("Couldn't add entity type {0} at {1} due to exception in code. Will remove from chunk, sorry!", entity.Code, entity.ServerPos.OnlyPosToString());
			Logger.Error(e);
			return false;
		}
	}

	public void SpawnEntity(Entity entity)
	{
		SpawnEntity(entity, GetEntityType(entity.Code));
	}

	public void SpawnEntity(Entity entity, EntityProperties type)
	{
		if (Config.RepairMode && !(entity is EntityPlayer))
		{
			Logger.Warning("Rejected one entity spawn. Server in repair mode. Will not spawn new entities.");
			return;
		}
		long entityid = ++SaveGameData.LastEntityId;
		long chunkindex3d = WorldMap.ChunkIndex3D(entity.ServerPos);
		entity.EntityId = entityid;
		entity.DespawnReason = null;
		if (type == null)
		{
			Logger.Error("Couldn't spawn entity {0} with id {1} and code {2} - it's Type is null!", entity.GetType(), entityid, entity.Code);
			return;
		}
		entity.Initialize(type.Clone(), api, chunkindex3d);
		entity.AfterInitialized(onFirstSpawn: true);
		AddEntityToChunk(entity, (int)entity.ServerPos.X, (int)entity.ServerPos.InternalY, (int)entity.ServerPos.Z);
		if (!LoadedEntities.TryAdd(entityid, entity))
		{
			Logger.Warning("SpawnEntity: Duplicate entity id discovered, will updating SaveGameData.LastEntityId to reflect this. This was likely caused by an ungraceful server exit.");
			RemoveEntityFromChunk(entity, (int)entity.ServerPos.X, (int)entity.ServerPos.InternalY, (int)entity.ServerPos.Z);
			SaveGameData.LastEntityId = LoadedEntities.Max((KeyValuePair<long, Entity> val) => val.Value.EntityId);
			entityid = (entity.EntityId = ++SaveGameData.LastEntityId);
			if (!LoadedEntities.TryAdd(entityid, entity))
			{
				Logger.Warning("SpawnEntity: Still not able to add entity after updating LastEntityId. Looks like a programming error. Killing server...");
				throw new Exception("Unable to spawn entity");
			}
			AddEntityToChunk(entity, (int)entity.ServerPos.X, (int)entity.ServerPos.InternalY, (int)entity.ServerPos.Z);
		}
		entity.OnEntitySpawn();
		lock (EntitySpawnSendQueue)
		{
			EntitySpawnSendQueue.Add(entity);
		}
		EventManager.TriggerEntitySpawned(entity);
	}

	public long GetNextHerdId()
	{
		return ++SaveGameData.LastHerdId;
	}

	public void DespawnEntity(Entity entity, EntityDespawnData despawnData)
	{
		entity.OnEntityDespawn(despawnData);
		FrameProfiler.Mark("despawned-1-" + entity.Code.Path);
		LoadedEntities.TryRemove(entity.EntityId, out var _);
		if (despawnData == null || despawnData.Reason != EnumDespawnReason.Unload)
		{
			RemoveEntityFromChunk(entity, (int)entity.ServerPos.X, (int)entity.ServerPos.Y, (int)entity.ServerPos.Z);
		}
		EntityDespawnSendQueue.Add(new KeyValuePair<Entity, EntityDespawnData>(entity, entity.DespawnReason));
		entity.State = EnumEntityState.Despawned;
		FrameProfiler.Mark("despawned-2-" + entity.Code.Path);
		EventManager.TriggerEntityDespawned(entity, despawnData);
	}

	private void AddEntityToChunk(Entity entity, int x, int y, int z)
	{
		WorldMap.GetServerChunk(x / MagicNum.ServerChunkSize, y / MagicNum.ServerChunkSize, z / MagicNum.ServerChunkSize)?.AddEntity(entity);
	}

	private void RemoveEntityFromChunk(Entity entity, int x, int y, int z)
	{
		WorldMap.GetServerChunk(entity.InChunkIndex3d)?.RemoveEntity(entity.EntityId);
	}

	public Entity GetNearestEntity(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null)
	{
		return GetEntitiesAround(position, horRange, vertRange, matches).MinBy((Entity entity) => entity.Pos.SquareDistanceTo(position));
	}

	public Entity GetEntityById(long entityId)
	{
		LoadedEntities.TryGetValue(entityId, out var entity);
		return entity;
	}

	public long RegisterGameTickListener(Action<float> OnGameTick, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		return EventManager.AddGameTickListener(OnGameTick, millisecondInterval, initialDelayOffsetMs);
	}

	public long RegisterGameTickListener(Action<float> OnGameTick, Action<Exception> errorHandler, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		return EventManager.AddGameTickListener(OnGameTick, errorHandler, millisecondInterval, initialDelayOffsetMs);
	}

	public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay)
	{
		return EventManager.AddDelayedCallback(OnTimePassed, millisecondDelay);
	}

	public long RegisterGameTickListener(Action<IWorldAccessor, BlockPos, float> OnGameTick, BlockPos pos, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		return EventManager.AddGameTickListener(OnGameTick, pos, millisecondInterval, initialDelayOffsetMs);
	}

	public long RegisterCallback(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay)
	{
		return EventManager.AddDelayedCallback(OnTimePassed, pos, millisecondDelay);
	}

	public long RegisterCallbackUnique(Action<IWorldAccessor, BlockPos, float> OnGameTick, BlockPos pos, int millisecondInterval)
	{
		return EventManager.AddSingleDelayedCallback(OnGameTick, pos, millisecondInterval);
	}

	public void UnregisterCallback(long callbackId)
	{
		if (callbackId > 0)
		{
			EventManager.RemoveDelayedCallback(callbackId);
		}
	}

	public void UnregisterGameTickListener(long listenerId)
	{
		if (listenerId > 0)
		{
			EventManager.RemoveGameTickListener(listenerId);
		}
	}

	public void SpawnParticles(float quantity, int color, Vec3d minPos, Vec3d maxPos, Vec3f minVelocity, Vec3f maxVelocity, float lifeLength, float gravityEffect, float scale = 1f, EnumParticleModel model = EnumParticleModel.Quad, IPlayer dualCallByPlayer = null)
	{
		SimpleParticleProperties props = new SimpleParticleProperties(quantity, quantity, color, minPos, maxPos, minVelocity, maxVelocity, lifeLength, gravityEffect);
		props.ParticleModel = model;
		props.MinSize = (props.MaxSize = scale);
		SpawnParticles(props, dualCallByPlayer);
	}

	public void SpawnParticles(IParticlePropertiesProvider provider, IPlayer dualCallByPlayer = null)
	{
		string className = ClassRegistry.ParticleProviderTypeToClassnameMapping[provider.GetType()];
		Packet_SpawnParticles p = new Packet_SpawnParticles();
		p.ParticlePropertyProviderClassName = className;
		using (MemoryStream ms = new MemoryStream())
		{
			BinaryWriter writer = new BinaryWriter(ms);
			provider.ToBytes(writer);
			p.SetData(ms.ToArray());
		}
		Packet_Server packet = new Packet_Server
		{
			Id = 61,
			SpawnParticles = p
		};
		provider.BeginParticle();
		Vec3d pos = provider.Pos;
		long chunkindex3d = WorldMap.ChunkIndex3D((int)pos.X / 32, (int)pos.Y / 32, (int)pos.Z / 32);
		Serialize_(packet);
		foreach (ConnectedClient client in Clients.Values)
		{
			if (client.IsPlayingClient && client.Player != dualCallByPlayer && client.DidSendChunk(chunkindex3d))
			{
				SendPacket(client.Id, reusableBuffer);
			}
		}
	}

	public void SpawnCubeParticles(Vec3d pos, ItemStack stack, float radius, int quantity, float scale = 0.5f, IPlayer dualCallByPlayer = null, Vec3f velocity = null)
	{
		SpawnParticles(new StackCubeParticles(pos, stack, radius, quantity, scale, velocity), dualCallByPlayer);
	}

	public void SpawnCubeParticles(BlockPos blockpos, Vec3d pos, float radius, int quantity, float scale = 0.5f, IPlayer dualCallByPlayer = null, Vec3f velocity = null)
	{
		SpawnParticles(new BlockCubeParticles(this, blockpos, pos, radius, quantity, scale, velocity), dualCallByPlayer);
	}

	public void CreateExplosion(BlockPos pos, EnumBlastType blastType, double destructionRadius, double injureRadius, float blockDropChanceMultiplier = 1f)
	{
		destructionRadius = GameMath.Clamp(destructionRadius, 1.0, 16.0);
		double num = Math.Max(1.2000000476837158 * destructionRadius, injureRadius);
		if (num > (double)ShapeUtil.MaxShells)
		{
			throw new ArgumentOutOfRangeException("Radius cannot be greater than " + (int)((float)ShapeUtil.MaxShells / 1.2f));
		}
		Vec3f[] shellPositions = ShapeUtil.GetCachedCubicShellNormalizedVectors((int)num);
		double minDestroRadius = 0.800000011920929 * destructionRadius;
		double addDestroRadius = 0.4000000059604645 * destructionRadius;
		BlockPos tmpPos = new BlockPos();
		int maxRadiusCeil = (int)Math.Ceiling(num);
		BlockPos minPos = pos.AddCopy(-maxRadiusCeil);
		BlockPos maxPos = pos.AddCopy(maxRadiusCeil);
		WorldMap.PrefetchBlockAccess.PrefetchBlocks(minPos, maxPos);
		DamageSource testSrc = new DamageSource
		{
			Source = EnumDamageSource.Explosion,
			SourcePos = pos.ToVec3d(),
			Type = EnumDamageType.BluntAttack
		};
		Entity[] entities = GetEntitiesAround(pos.ToVec3d(), (float)injureRadius + 2f, (float)injureRadius + 2f, (Entity e) => e.ShouldReceiveDamage(testSrc, (float)injureRadius));
		Dictionary<long, double> strongestRayOnEntity = new Dictionary<long, double>();
		for (int k = 0; k < entities.Length; k++)
		{
			strongestRayOnEntity[entities[k].EntityId] = 0.0;
		}
		ExplosionSmokeParticles particleProvider = new ExplosionSmokeParticles();
		particleProvider.basePos = new Vec3d((double)pos.X + 0.5, (double)pos.Y + 0.5, (double)pos.Z + 0.5);
		Dictionary<BlockPos, Block> explodedBlocks = new Dictionary<BlockPos, Block>();
		Cuboidd testBox = Block.DefaultCollisionBox.ToDouble();
		for (int j = 0; j < shellPositions.Length; j++)
		{
			double curDestroStrength;
			double val2 = (curDestroStrength = minDestroRadius + rand.Value.NextDouble() * addDestroRadius);
			double curInjureStrength = injureRadius;
			double maxStrength = Math.Max(val2, injureRadius);
			Vec3f vec = shellPositions[j];
			for (double r = 0.0; r < maxStrength; r += 0.25)
			{
				tmpPos.Set(pos.X + (int)((double)vec.X * r + 0.5), pos.Y + (int)((double)vec.Y * r + 0.5), pos.Z + (int)((double)vec.Z * r + 0.5));
				if (!worldmap.IsValidPos(tmpPos))
				{
					break;
				}
				curDestroStrength -= 0.25;
				curInjureStrength -= 0.25;
				if (!explodedBlocks.ContainsKey(tmpPos))
				{
					Block block = WorldMap.PrefetchBlockAccess.GetBlock(tmpPos);
					double resist = block.GetBlastResistance(this, tmpPos, vec, blastType);
					curDestroStrength -= resist;
					if (curDestroStrength > 0.0)
					{
						explodedBlocks[tmpPos.Copy()] = block;
						curInjureStrength -= resist;
					}
					if (curDestroStrength <= 0.0 && resist > 0.0)
					{
						curInjureStrength = 0.0;
					}
				}
				if (curDestroStrength <= 0.0 && curInjureStrength <= 0.0)
				{
					break;
				}
				if (!(curInjureStrength > 0.0))
				{
					continue;
				}
				foreach (Entity entity2 in entities)
				{
					testBox.Set(tmpPos.X, tmpPos.Y, tmpPos.Z, tmpPos.X + 1, tmpPos.Y + 1, tmpPos.Z + 1);
					if (testBox.IntersectsOrTouches(entity2.SelectionBox, entity2.ServerPos.X, entity2.ServerPos.Y, entity2.ServerPos.Z))
					{
						strongestRayOnEntity[entity2.EntityId] = Math.Max(strongestRayOnEntity[entity2.EntityId], curInjureStrength);
					}
				}
			}
		}
		foreach (Entity entity in entities)
		{
			double strength = strongestRayOnEntity[entity.EntityId];
			if (strength != 0.0)
			{
				double damage = Math.Max(injureRadius / Math.Max(0.5, injureRadius - strength), strength);
				if (!(damage < 0.25))
				{
					DamageSource src = new DamageSource
					{
						Source = EnumDamageSource.Explosion,
						Type = EnumDamageType.BluntAttack,
						SourcePos = new Vec3d((double)pos.X + 0.5, pos.Y, (double)pos.Z + 0.5)
					};
					entity.ReceiveDamage(src, (float)damage);
				}
			}
		}
		particleProvider.AddBlocks(explodedBlocks);
		foreach (KeyValuePair<BlockPos, Block> val in explodedBlocks)
		{
			if (val.Value.BlockMaterial != 0)
			{
				val.Value.OnBlockExploded(this, val.Key, pos, blastType);
			}
		}
		WorldMap.BulkBlockAccess.Commit();
		foreach (KeyValuePair<BlockPos, Block> item in explodedBlocks)
		{
			TriggerNeighbourBlocksUpdate(item.Key);
		}
		string soundName = "effect/smallexplosion";
		if (destructionRadius > 12.0)
		{
			soundName = "effect/largeexplosion";
		}
		else if (destructionRadius > 6.0)
		{
			soundName = "effect/mediumexplosion";
		}
		PlaySoundAt("sounds/" + soundName, (double)pos.X + 0.5, (double)pos.InternalY + 0.5, (double)pos.Z + 0.5, null, randomizePitch: false, (float)(24.0 * Math.Pow(destructionRadius, 0.5)));
		SimpleParticleProperties p = ExplosionParticles.ExplosionFireParticles;
		float mul = (float)destructionRadius / 3f;
		p.MinPos.Set(pos.X, pos.Y, pos.Z);
		p.MinQuantity = 100f * mul;
		p.AddQuantity = (int)(20.0 * Math.Pow(destructionRadius, 0.75));
		SpawnParticles(p);
		AdvancedParticleProperties p2 = ExplosionParticles.ExplosionFireTrailCubicles;
		p2.Velocity = new NatFloat[3]
		{
			NatFloat.createUniform(0f, 8f + mul),
			NatFloat.createUniform(3f + mul, 3f + mul),
			NatFloat.createUniform(0f, 8f + mul)
		};
		p2.basePos.Set((double)pos.X + 0.5, (double)pos.Y + 0.5, (double)pos.Z + 0.5);
		p2.GravityEffect = NatFloat.createUniform(0.5f, 0f);
		p2.LifeLength = NatFloat.createUniform(1.5f * mul, 0.5f);
		p2.Quantity = NatFloat.createUniform(30f * mul, 10f);
		float f2 = (float)Math.Pow(mul, 0.75);
		p2.Size = NatFloat.createUniform(0.5f * f2, 0.2f * f2);
		p2.SecondaryParticles[0].Size = NatFloat.createUniform(0.25f * (float)Math.Pow(mul, 0.5), 0.05f * f2);
		SpawnParticles(p2);
		SpawnParticles(particleProvider);
		TreeAttribute tree = new TreeAttribute();
		tree.SetBlockPos("pos", pos);
		tree.SetInt("blasttype", (int)blastType);
		tree.SetDouble("destructionRadius", destructionRadius);
		tree.SetDouble("injureRadius", injureRadius);
		api.eventapi.PushEvent("onexplosion", tree);
	}

	public IWorldPlayerData GetWorldPlayerData(int clientID)
	{
		if (!Clients.ContainsKey(clientID))
		{
			return null;
		}
		return Clients[clientID].WorldData;
	}

	public IPlayer NearestPlayer(double x, double y, double z)
	{
		IPlayer closestplayer = null;
		float closestSqDistance = -1f;
		foreach (ConnectedClient client in Clients.Values)
		{
			if (client.State == EnumClientState.Playing && client.Entityplayer != null)
			{
				float distanceSq = client.Position.SquareDistanceTo(x, y, z);
				if (closestSqDistance == -1f || distanceSq < closestSqDistance)
				{
					closestSqDistance = distanceSq;
					closestplayer = client.Player;
				}
			}
		}
		return closestplayer;
	}

	public IPlayer[] GetPlayersAround(Vec3d position, float horRange, float vertRange, ActionConsumable<IPlayer> matches = null)
	{
		List<IPlayer> players = new List<IPlayer>();
		float horRangeSq = horRange * horRange;
		foreach (ConnectedClient client in Clients.Values)
		{
			if (client.State == EnumClientState.Playing && client.Entityplayer != null && client.Position.InRangeOf(position, horRangeSq, vertRange) && (matches == null || matches(client.Player)))
			{
				players.Add(client.Player);
			}
		}
		return players.ToArray();
	}

	public IPlayer PlayerByUid(string playerUid)
	{
		if (playerUid == null)
		{
			return null;
		}
		ServerPlayer plr = null;
		PlayersByUid.TryGetValue(playerUid, out plr);
		return plr;
	}

	public void EnqueueMainThreadTask(Action task)
	{
		if (task == null)
		{
			throw new ArgumentNullException();
		}
		lock (mainThreadTasksLock)
		{
			mainThreadTasks.Enqueue(task);
		}
	}

	public void ProcessMainThreadTasks()
	{
		if (FrameProfiler != null && FrameProfiler.Enabled)
		{
			FrameProfiler.Enter("mainthreadtasks");
			while (mainThreadTasks.Count > 0)
			{
				Action task;
				lock (mainThreadTasksLock)
				{
					task = mainThreadTasks.Dequeue();
				}
				task();
				if (task.Target != null)
				{
					string code = task.Target.GetType().ToString();
					FrameProfiler.Mark(code);
				}
			}
			FrameProfiler.Leave();
			return;
		}
		while (mainThreadTasks.Count > 0)
		{
			Action task2;
			lock (mainThreadTasksLock)
			{
				task2 = mainThreadTasks.Dequeue();
			}
			task2();
		}
	}

	public void HighlightBlocks(IPlayer player, int slotId, List<BlockPos> blocks, List<int> colors, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary, float scale = 1f)
	{
		SendHighlightBlocksPacket((IServerPlayer)player, slotId, blocks, colors, mode, shape, scale);
	}

	public void HighlightBlocks(IPlayer player, int slotId, List<BlockPos> blocks, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary)
	{
		SendHighlightBlocksPacket((IServerPlayer)player, slotId, blocks, null, mode, shape);
	}

	private void InitBasicPacketHandlers()
	{
		PacketHandlers[1] = HandlePlayerIdentification;
		PacketHandlers[11] = HandleRequestJoin;
		PacketHandlers[20] = HandleRequestModeChange;
		PacketHandlers[4] = HandleChatLine;
		PacketHandlers[13] = HandleSelectedHotbarSlot;
		PacketHandlers[14] = HandleLeave;
		PacketHandlers[21] = HandleMoveKeyChange;
		PacketHandlers[31] = HandleEntityPacket;
		PacketHandlers[26] = HandleClientLoaded;
		PacketHandlers[29] = HandleClientPlaying;
		PacketHandlers[34] = HandleRequestPositionTcp;
		PacketHandlingOnConnectingAllowed[1] = true;
		PacketHandlingOnConnectingAllowed[14] = true;
		PacketHandlingOnConnectingAllowed[34] = true;
	}

	private void HandleRequestPositionTcp(Packet_Client packet, ConnectedClient player)
	{
		player.FallBackToTcp = true;
		Logger.Debug($"UDP: Client {player.Id} [{player.PlayerName}] is unable to receive data via UDP, switching to send positions over TCP.");
	}

	public void PacketParsingLoop()
	{
		for (int i = 0; i < MainSockets.Length; i++)
		{
			NetServer mainSocket = MainSockets[i];
			if (mainSocket != null)
			{
				NetIncomingMessage msg;
				while ((msg = mainSocket.ReadMessage()) != null)
				{
					ProcessNetMessage(msg, mainSocket);
				}
			}
		}
	}

	private void ProcessNetMessage(NetIncomingMessage msg, NetServer mainSocket)
	{
		if (RunPhase == EnumServerRunPhase.Shutdown || exit.exit || msg.SenderConnection == null)
		{
			return;
		}
		switch (msg.Type)
		{
		case NetworkMessageType.Connect:
		{
			if (RunPhase == EnumServerRunPhase.Standby)
			{
				EnqueueMainThreadTask(delegate
				{
					Launch();
				});
			}
			NetConnection clientConnection = msg.SenderConnection;
			lastClientId = GenerateClientId();
			ConnectedClient newClient = (clientConnection.client = new ConnectedClient(lastClientId)
			{
				FromSocketListener = mainSocket,
				Socket = clientConnection
			});
			newClient.Ping.SetTimeoutThreshold(Config.ClientConnectionTimeout);
			ClientPackets.Enqueue(new ReceivedClientPacket(newClient));
			break;
		}
		case NetworkMessageType.Data:
		{
			ConnectedClient client = msg.SenderConnection.client;
			if (client != null)
			{
				TotalReceivedBytes += msg.messageLength;
				ParseClientPacket_offthread(client, msg.message, msg.messageLength);
			}
			break;
		}
		case NetworkMessageType.Disconnect:
		{
			ConnectedClient client2 = msg.SenderConnection.client;
			if (client2 != null)
			{
				DisconnectedClientsThisTick.Add(client2.Id);
				ClientPackets.Enqueue(new ReceivedClientPacket(client2, ""));
			}
			break;
		}
		}
	}

	private void ParseClientPacket_offthread(ConnectedClient client, byte[] data, int length)
	{
		Packet_Client packet = new Packet_Client();
		try
		{
			Packet_ClientSerializer.DeserializeBuffer(data, length, packet);
		}
		catch
		{
			packet = null;
		}
		ReceivedClientPacket cpk;
		if (packet == null)
		{
			DisconnectedClientsThisTick.Add(client.Id);
			cpk = new ReceivedClientPacket(client, (client.Player == null) ? "" : "Network error: invalid client packet");
		}
		else
		{
			cpk = new ReceivedClientPacket(client, packet);
		}
		ClientPackets.Enqueue(cpk);
	}

	private void HandleClientPacket_mainthread(ReceivedClientPacket cpk)
	{
		ConnectedClient client = cpk.client;
		Packet_Client packet = cpk.packet;
		if (cpk.type == ReceivedClientPacketType.NewConnection)
		{
			if (!DisconnectedClientsThisTick.Contains(client.Id))
			{
				client.Initialise();
				Clients[client.Id] = client;
				Logger.Notification($"A Client attempts connecting via {client.FromSocketListener.Name} on {client.Socket.RemoteEndPoint()}, assigning client id " + client.Id);
			}
			return;
		}
		if (cpk.type == ReceivedClientPacketType.Disconnect)
		{
			if (client.Player != null && cpk.disconnectReason.Length == 0)
			{
				Logger.Event("Client " + client.Id + " disconnected.");
				EventManager.TriggerPlayerLeave(client.Player);
			}
			DisconnectPlayer(client, null, cpk.disconnectReason);
			return;
		}
		if (client.IsNewClient && packet.Id != 1 && packet.Id != 2 && packet.Id != 14 && packet.Id != 34)
		{
			HandleQueryClientPacket(client, packet);
			if (FrameProfiler.Enabled)
			{
				FrameProfiler.Mark("net-read-" + packet.Id);
			}
			return;
		}
		ClientPacketHandler<Packet_Client, ConnectedClient> handler = PacketHandlers[packet.Id];
		if (handler != null && (client.Player != null || PacketHandlingOnConnectingAllowed[packet.Id]))
		{
			if (client.Player == null || client.Player.client == client)
			{
				handler(packet, client);
				if (FrameProfiler.Enabled)
				{
					FrameProfiler.Mark("net-read-" + packet.Id);
				}
			}
		}
		else
		{
			Logger.Error("Unhandled player packet: {0}, clientid:{1}", packet.Id, client.Id);
			if (FrameProfiler.Enabled)
			{
				FrameProfiler.Mark("net-readerror-" + packet.Id);
			}
		}
	}

	private void VerifyPlayerWithAuthServer(Packet_ClientIdentification packet, ConnectedClient client)
	{
		Logger.Debug("Client uid {0}, mp token {1}: Verifying with auth server", packet.PlayerUID, packet.MpToken, packet.Playername);
		AuthServerComm.ValidatePlayerWithServer(packet.MpToken, packet.Playername, packet.PlayerUID, client.LoginToken, delegate(EnumServerResponse result, string entitlements, string errorReason)
		{
			EnqueueMainThreadTask(delegate
			{
				if (Clients.ContainsKey(client.Id))
				{
					if (result == EnumServerResponse.Good)
					{
						PreFinalizePlayerIdentification(packet, client, entitlements);
						FrameProfiler.Mark("finalizeplayeridentification");
					}
					else if (result == EnumServerResponse.Bad)
					{
						switch (errorReason)
						{
						case "missingmptoken":
						case "missingaccount":
						case "banned":
						case "serverbanned":
						case "badplayeruid":
							DisconnectPlayer(client, null, Lang.Get("servervalidate-error-" + errorReason));
							break;
						default:
							DisconnectPlayer(client, null, Lang.Get("Auth server reports issue " + errorReason));
							break;
						}
					}
					else
					{
						DisconnectPlayer(client, null, Lang.Get("Unable to check wether your game session is ok, auth server probably offline. Please try again later. If you are the server owner, check server-main.log and server-debug.log for details"));
					}
				}
			});
		});
	}

	private void PreFinalizePlayerIdentification(Packet_ClientIdentification packet, ConnectedClient client, string entitlements)
	{
		int maxClients = Config.GetMaxClients(this);
		if (Clients.Count - 1 >= maxClients)
		{
			ServerPlayerData data = PlayerDataManager.GetOrCreateServerPlayerData(packet.PlayerUID);
			if (!data.HasPrivilege(Privilege.controlserver, Config.RolesByCode) && !data.HasPrivilege("ignoremaxclients", Config.RolesByCode))
			{
				if (Config.MaxClientsInQueue > 0)
				{
					int connectionQueueCount;
					lock (ConnectionQueue)
					{
						connectionQueueCount = ConnectionQueue.Count;
					}
					if (connectionQueueCount < Config.MaxClientsInQueue)
					{
						client.State = EnumClientState.Queued;
						int pos;
						lock (ConnectionQueue)
						{
							ConnectionQueue.Add(new QueuedClient(client, packet, entitlements));
							pos = ConnectionQueue.Count;
						}
						Packet_Server pq = new Packet_Server
						{
							Id = 82,
							QueuePacket = new Packet_QueuePacket
							{
								Position = pos
							}
						};
						Logger.Notification($"Player {packet.Playername} was put into the connection queue at position {pos}");
						SendPacket(client.Id, pq);
						return;
					}
				}
				DisconnectPlayer(client, null, Lang.Get("Server is full ({0} max clients)", maxClients));
				return;
			}
		}
		FinalizePlayerIdentification(packet, client, entitlements);
	}

	private void FinalizePlayerIdentification(Packet_ClientIdentification packet, ConnectedClient client, string entitlements)
	{
		if (RunPhase == EnumServerRunPhase.Shutdown)
		{
			return;
		}
		string playername = packet.Playername;
		Logger.VerboseDebug("Received identification packet from " + playername);
		bool found = false;
		foreach (KeyValuePair<int, ConnectedClient> val in Clients)
		{
			bool equaluid = packet.PlayerUID.Equals(val.Value.SentPlayerUid, StringComparison.InvariantCultureIgnoreCase);
			if (equaluid && client.Id != val.Key)
			{
				Logger.Event($"{packet.Playername} joined again, killing previous client.");
				DisconnectPlayer(val.Value);
				break;
			}
			if (equaluid)
			{
				found = true;
			}
		}
		if (!found)
		{
			Logger.Notification("Was about to finalize player ident, but player {0} is no longer online. Ignoring.", packet.Playername);
			return;
		}
		if (client.ServerDidReceiveUdp)
		{
			Logger.Debug($"UDP: Client {client.Id} did send UDP");
		}
		else
		{
			Task.Run(async delegate
			{
				for (int j = 0; j < 20; j++)
				{
					await Task.Delay(500);
					if (client.State == EnumClientState.Offline)
					{
						return;
					}
					if (client.ServerDidReceiveUdp)
					{
						break;
					}
				}
				if (!client.ServerDidReceiveUdp)
				{
					Logger.Debug($"UDP: Server did not receive any UDP packets from Client {client.Id}, telling the client to send positions over TCP.");
					Packet_Server packetTcp = new Packet_Server
					{
						Id = 78
					};
					SendPacket(client.Id, packetTcp);
					client.FallBackToTcp = true;
					ServerUdpNetwork.connectingClients.Remove(client.LoginToken);
				}
				else
				{
					Logger.Debug($"UDP: Client {client.Id} did send UDP");
				}
			});
		}
		string playerUID = packet.PlayerUID;
		client.LoadOrCreatePlayerData(this, playername, playerUID);
		client.Player.client = client;
		if (client.Socket is TcpNetConnection tcpSocket)
		{
			tcpSocket.SetLengthLimit(((ServerWorldPlayerData)client.Player.WorldData).GameMode == EnumGameMode.Creative);
		}
		if (entitlements != null)
		{
			string[] array = entitlements.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string entitlement in array)
			{
				client.Player.Entitlements.Add(new Entitlement
				{
					Code = entitlement,
					Name = Lang.Get("entitlement-" + entitlement)
				});
			}
		}
		PlayersByUid[playerUID] = client.Player;
		EntityPos targetPos = (client.IsNewEntityPlayer ? GetSpawnPosition(playerUID) : GetJoinPosition(client));
		if (!client.IsNewEntityPlayer && targetPos.X == 0.0 && targetPos.Y == 0.0 && targetPos.Z == 0.0 && targetPos.Pitch == 0f && targetPos.Roll == 0f)
		{
			Logger.Warning("Player {0} is at position 0/0/0? Did something get corrupted? Placing player to the global default spawn position...", client.PlayerName);
			targetPos = GetSpawnPosition(playerUID);
		}
		client.WorldData.EntityPlayer.WatchedAttributes.SetString("playerUID", playerUID);
		client.WorldData.Viewdistance = packet.ViewDistance;
		client.WorldData.RenderMetaBlocks = packet.RenderMetaBlocks > 0;
		if (client.FromSocketListener.GetType() == typeof(DummyTcpNetServer) && Config.MaxChunkRadius != packet.ViewDistance / 32)
		{
			Config.MaxChunkRadius = Math.Max(Config.MaxChunkRadius, packet.ViewDistance / 32);
			Logger.Notification("Upped server view distance because player is locally connected");
		}
		client.ServerData.LastKnownPlayername = playername;
		SendPacket(client.Player, new Packet_Server
		{
			Id = 51,
			EntityPosition = ServerPackets.getEntityPositionPacket(GetSpawnPosition(playerUID, onlyGlobalDefaultSpawn: true), client.Entityplayer, 0)
		});
		if (World.Config.GetString("spawnRadius").ToInt() > 0 && client.IsNewEntityPlayer)
		{
			Logger.Notification("Delayed join, attempt random spawn position.");
			SendLevelProgress(client.Player, 99, Lang.Get("Loading spawn chunk..."));
			SendServerIdentification(client.Player);
			SpawnPlayerRandomlyAround(client, playername, targetPos, 10);
			client.IsNewClient = false;
			return;
		}
		if (WorldMap.IsPosLoaded(targetPos.AsBlockPos))
		{
			SendServerIdentification(client.Player);
			client.Entityplayer.ServerPos.SetFrom(targetPos);
			SpawnEntity(client.Entityplayer);
			client.Entityplayer.SetName(playername);
			Logger.Notification("Placing {0} at {1} {2} {3}", playername, targetPos.X, targetPos.Y, targetPos.Z);
			SendServerReady(client.Player);
		}
		else
		{
			Logger.Notification("Delayed join, need to load one spawn chunk first.");
			SendLevelProgress(client.Player, 99, Lang.Get("Loading spawn chunk..."));
			SendServerIdentification(client.Player);
			KeyValuePair<HorRectanglei, ChunkLoadOptions> data = new KeyValuePair<HorRectanglei, ChunkLoadOptions>(new HorRectanglei((int)targetPos.X / 32, (int)targetPos.Z / 32, (int)targetPos.X / 32, (int)targetPos.Z / 32), new ChunkLoadOptions
			{
				OnLoaded = delegate
				{
					ConnectedClient value = null;
					Clients.TryGetValue(client.Id, out value);
					if (value != null)
					{
						client.CurrentChunkSentRadius = 0;
						EntityPos entityPos = (client.IsNewEntityPlayer ? GetSpawnPosition(playerUID) : GetJoinPosition(value));
						client.Entityplayer.ServerPos.SetFrom(entityPos);
						SpawnEntity(client.Entityplayer);
						client.WorldData.EntityPlayer.SetName(playername);
						Logger.Notification("Placing {0} at {1} {2} {3}", playername, entityPos.X, entityPos.Y, entityPos.Z);
						SendServerReady(client.Player);
					}
				}
			});
			fastChunkQueue.Enqueue(data);
			Logger.VerboseDebug("Spawn chunk load request enqueued.");
		}
		client.IsNewClient = false;
	}

	public void LocateRandomPosition(Vec3d centerPos, float radius, int tries, ActionConsumable<BlockPos> testThisPosition, Action<BlockPos> onSearchOver)
	{
		Vec3d targetPos = centerPos.Clone();
		targetPos.X += rand.Value.NextDouble() * 2.0 * (double)radius - (double)radius;
		targetPos.Z += rand.Value.NextDouble() * 2.0 * (double)radius - (double)radius;
		BlockPos pos = targetPos.AsBlockPos;
		if (tries <= 0)
		{
			onSearchOver(null);
			return;
		}
		if (WorldMap.IsPosLoaded(pos) && testThisPosition(pos))
		{
			onSearchOver(pos);
			return;
		}
		KeyValuePair<HorRectanglei, ChunkLoadOptions> data = new KeyValuePair<HorRectanglei, ChunkLoadOptions>(new HorRectanglei((int)targetPos.X / 32, (int)targetPos.Z / 32, (int)targetPos.X / 32, (int)targetPos.Z / 32), new ChunkLoadOptions
		{
			OnLoaded = delegate
			{
				BlockPos asBlockPos = targetPos.AsBlockPos;
				if (WorldMap.IsPosLoaded(asBlockPos) && testThisPosition(asBlockPos))
				{
					onSearchOver(asBlockPos);
				}
				else
				{
					LocateRandomPosition(targetPos, radius, tries - 1, testThisPosition, onSearchOver);
				}
			}
		});
		Logger.Event("Searching for chunk column suitable for player spawn");
		Logger.StoryEvent("...");
		fastChunkQueue.Enqueue(data);
	}

	private void SpawnPlayerRandomlyAround(ConnectedClient client, string playername, EntityPos centerPos, int tries)
	{
		float radius = World.Config.GetString("spawnRadius").ToInt();
		LocateRandomPosition(centerPos.XYZ, radius, tries, (BlockPos pos) => ServerSystemSupplyChunks.AdjustForSaveSpawnSpot(this, pos, client.Player, rand.Value), delegate(BlockPos pos)
		{
			EntityPos entityPos = centerPos.Copy();
			if (pos == null)
			{
				entityPos.X += rand.Value.NextDouble() * 2.0 * (double)radius - (double)radius;
				entityPos.Z += rand.Value.NextDouble() * 2.0 * (double)radius - (double)radius;
			}
			else
			{
				entityPos.X += (double)pos.X + 0.5 - entityPos.X;
				entityPos.Y += (double)pos.Y - entityPos.Y;
				entityPos.Z += (double)pos.Z + 0.5 - entityPos.Z;
			}
			SpawnPlayerHere(client, playername, entityPos);
		});
	}

	private void SpawnPlayerHere(ConnectedClient client, string playername, EntityPos targetPos)
	{
		Clients.TryGetValue(client.Id, out var finalClient);
		if (finalClient != null)
		{
			client.CurrentChunkSentRadius = 0;
			client.Entityplayer.ServerPos.SetFrom(targetPos);
			SpawnEntity(client.Entityplayer);
			client.WorldData.EntityPlayer.SetName(playername);
			Logger.Notification("Placing {0} at {1} {2} {3}", playername, targetPos.X, targetPos.Y, targetPos.Z);
			SendServerReady(client.Player);
		}
	}

	public void SendArbitraryUdpPacket(Packet_UdpPacket packet, params IServerPlayer[] players)
	{
		for (int i = 0; i < players.Length; i++)
		{
			SendPacket(players[i].ClientId, packet);
		}
	}

	public void SendArbitraryPacket(byte[] data, params IServerPlayer[] players)
	{
		for (int i = 0; i < players.Length; i++)
		{
			SendPacket(players[i], data);
		}
	}

	internal void SendBlockEntity(IServerPlayer targetPlayer, BlockEntity blockentity)
	{
		Packet_BlockEntity[] blockentitiespackets = new Packet_BlockEntity[1];
		int i = 0;
		MemoryStream ms = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(ms);
		TreeAttribute tree = new TreeAttribute();
		blockentity.ToTreeAttributes(tree);
		tree.ToBytes(writer);
		blockentitiespackets[i] = new Packet_BlockEntity
		{
			Classname = ClassRegistry.blockEntityTypeToClassnameMapping[blockentity.GetType()],
			Data = ms.ToArray(),
			PosX = blockentity.Pos.X,
			PosY = blockentity.Pos.InternalY,
			PosZ = blockentity.Pos.Z
		};
		Packet_BlockEntities packet = new Packet_BlockEntities();
		packet.SetBlockEntitites(blockentitiespackets);
		SendPacket(targetPlayer, new Packet_Server
		{
			Id = 48,
			BlockEntities = packet
		});
	}

	public void SendBlockEntityMessagePacket(IServerPlayer player, int x, int y, int z, int packetId, byte[] data = null)
	{
		Packet_BlockEntityMessage packet = new Packet_BlockEntityMessage
		{
			PacketId = packetId,
			X = x,
			Y = y,
			Z = z
		};
		packet.SetData(data);
		SendPacket(player, new Packet_Server
		{
			Id = 44,
			BlockEntityMessage = packet
		});
	}

	public void SendEntityPacket(IServerPlayer player, long entityId, int packetId, byte[] data = null)
	{
		Packet_EntityPacket packet = new Packet_EntityPacket
		{
			Packetid = packetId,
			EntityId = entityId
		};
		packet.SetData(data);
		SendPacket(player, new Packet_Server
		{
			Id = 67,
			EntityPacket = packet
		});
	}

	public void BroadcastEntityPacket(long entityId, int packetId, byte[] data = null)
	{
		Packet_EntityPacket packet = new Packet_EntityPacket
		{
			Packetid = packetId,
			EntityId = entityId
		};
		packet.SetData(data);
		BroadcastPacket(new Packet_Server
		{
			Id = 67,
			EntityPacket = packet
		});
	}

	public void BroadcastBlockEntityPacket(int x, int y, int z, int packetId, byte[] data = null, params IServerPlayer[] skipPlayers)
	{
		Packet_BlockEntityMessage packet = new Packet_BlockEntityMessage
		{
			PacketId = packetId,
			X = x,
			Y = y,
			Z = z
		};
		packet.SetData(data);
		BroadcastPacket(new Packet_Server
		{
			Id = 44,
			BlockEntityMessage = packet
		}, skipPlayers);
	}

	public void SendMessageToGeneral(string message, EnumChatType chatType, IServerPlayer exceptPlayer = null, string data = null)
	{
		SendMessageToGroup(GlobalConstants.GeneralChatGroup, message, chatType, exceptPlayer, data);
	}

	public void SendMessageToGroup(int groupid, string message, EnumChatType chatType, IServerPlayer exceptPlayer = null, string data = null)
	{
		bool isCommonGroup = groupid == GlobalConstants.AllChatGroups || groupid == GlobalConstants.GeneralChatGroup || groupid == GlobalConstants.CurrentChatGroup || groupid == GlobalConstants.ServerInfoChatGroup || groupid == GlobalConstants.InfoLogChatGroup;
		foreach (KeyValuePair<int, ConnectedClient> val in Clients)
		{
			if ((exceptPlayer == null || val.Key != exceptPlayer.ClientId) && val.Value.State != 0 && val.Value.State != EnumClientState.Connecting && val.Value.State != EnumClientState.Queued && (isCommonGroup || val.Value.ServerData.PlayerGroupMemberShips.ContainsKey(groupid)))
			{
				SendMessage(val.Value.Player, groupid, message, chatType, data);
			}
		}
	}

	public void BroadcastMessageToAllGroups(string message, EnumChatType chatType, string data = null)
	{
		Logger.Notification("Message to all in group " + GlobalConstants.GeneralChatGroup + ": {0}", message);
		foreach (KeyValuePair<int, ConnectedClient> client in Clients)
		{
			SendMessage(client.Value.Player, GlobalConstants.AllChatGroups, message, chatType, data);
		}
	}

	public void SendMessageToCurrentCh(IServerPlayer player, string message, EnumChatType chatType, string data = null)
	{
		SendMessage(player, GlobalConstants.CurrentChatGroup, message, chatType);
	}

	public void ReplyMessage(IServerPlayer player, string message, EnumChatType chatType, string data = null)
	{
		SendMessage(player, GlobalConstants.CurrentChatGroup, message, chatType, data);
	}

	public void SendMessage(Caller caller, string message, EnumChatType chatType, string data = null)
	{
		SendMessage(caller.Player as IServerPlayer, caller.FromChatGroupId, message, chatType, data);
	}

	public void SendMessage(IServerPlayer player, int groupid, string message, EnumChatType chatType, string data = null)
	{
		if (groupid == GlobalConstants.ConsoleGroup)
		{
			Console.WriteLine(message);
			Logger.Notification(message);
		}
		else
		{
			SendPacket(player, ServerPackets.ChatLine(groupid, message, chatType, data));
		}
	}

	public void SendIngameError(IServerPlayer player, string errorCode, string text = null, params object[] langparams)
	{
		SendPacket(player, ServerPackets.IngameError(errorCode, text, langparams));
	}

	public void SendIngameDiscovery(IServerPlayer player, string discoveryCode, string text = null, params object[] langparams)
	{
		SendPacket(player, ServerPackets.IngameDiscovery(discoveryCode, text, langparams));
	}

	public byte[] Serialize(Packet_Server p)
	{
		return Packet_ServerSerializer.SerializeToBytes(p);
	}

	private int Serialize_(Packet_Server p)
	{
		if (reusableBuffer == null)
		{
			reusableBuffer = new BoxedPacket();
			lock (reusableBuffersDisposalList)
			{
				reusableBuffersDisposalList.Add(reusableBuffer);
			}
		}
		return reusableBuffer.Serialize(p);
	}

	internal void SendSetBlock(int blockId, int posX, int posY, int posZ, int exceptClientid = -1, bool exchangeOnly = false)
	{
		foreach (KeyValuePair<int, ConnectedClient> val in Clients)
		{
			if (exceptClientid != val.Key && val.Value.State != EnumClientState.Connecting && val.Value.State != EnumClientState.Queued && val.Value.Player != null)
			{
				SendSetBlock(val.Value.Player, blockId, posX, posY, posZ, exchangeOnly);
			}
		}
	}

	internal void BroadcastUnloadMapRegion(long index)
	{
		WorldMap.MapRegionPosFromIndex2D(index, out var rx, out var rz);
		foreach (KeyValuePair<int, ConnectedClient> val in Clients)
		{
			if (val.Value.State != EnumClientState.Connecting && val.Value.State != EnumClientState.Queued && val.Value.Player != null)
			{
				Packet_UnloadMapRegion p = new Packet_UnloadMapRegion
				{
					RegionX = rx,
					RegionZ = rz
				};
				SendPacket(val.Value.Player, new Packet_Server
				{
					Id = 74,
					UnloadMapRegion = p
				});
				val.Value.RemoveMapRegionSent(index);
			}
		}
	}

	internal void SendSetBlock(IServerPlayer player, int blockId, int posX, int posY, int posZ, bool exchangeOnly = false)
	{
		if (Clients[player.ClientId].DidSendChunk(WorldMap.ChunkIndex3D(posX / MagicNum.ServerChunkSize, posY / MagicNum.ServerChunkSize, posZ / MagicNum.ServerChunkSize)))
		{
			if (exchangeOnly)
			{
				Packet_ServerExchangeBlock p = new Packet_ServerExchangeBlock
				{
					X = posX,
					Y = posY,
					Z = posZ,
					BlockType = blockId
				};
				SendPacket(player, new Packet_Server
				{
					Id = 58,
					ExchangeBlock = p
				});
			}
			else
			{
				Packet_ServerSetBlock p2 = new Packet_ServerSetBlock
				{
					X = posX,
					Y = posY,
					Z = posZ,
					BlockType = blockId
				};
				SendPacket(player, new Packet_Server
				{
					Id = 7,
					SetBlock = p2
				});
			}
		}
	}

	public void SendSetBlocksPacket(List<BlockPos> positions, int packetId)
	{
		if (positions.Count != 0)
		{
			byte[] compressedBlocks = BlockTypeNet.PackSetBlocksList(positions, WorldMap.RelaxedBlockAccess);
			Packet_ServerSetBlocks setblocks = new Packet_ServerSetBlocks();
			setblocks.SetSetBlocks(compressedBlocks);
			BroadcastPacket(new Packet_Server
			{
				Id = packetId,
				SetBlocks = setblocks
			});
		}
	}

	public void SendSetDecorsPackets(List<BlockPos> positions)
	{
		if (positions.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<long, WorldChunk> val in WorldMap.PositionsToUniqueChunks(positions))
		{
			if (val.Value != null)
			{
				byte[] compressedDecors = BlockTypeNet.PackSetDecorsList(val.Value, val.Key, WorldMap.RelaxedBlockAccess);
				Packet_ServerSetDecors setdecors = new Packet_ServerSetDecors();
				setdecors.SetSetDecors(compressedDecors);
				BroadcastPacket(new Packet_Server
				{
					Id = 71,
					SetDecors = setdecors
				});
			}
		}
	}

	public void SendHighlightBlocksPacket(IServerPlayer player, int slotId, List<BlockPos> justBlocks, List<int> colors, EnumHighlightBlocksMode mode, EnumHighlightShape shape, float scale = 1f)
	{
		byte[] compressedBlocks = BlockTypeNet.PackBlocksPositions(justBlocks);
		Packet_HighlightBlocks setblocks = new Packet_HighlightBlocks();
		setblocks.SetBlocks(compressedBlocks);
		setblocks.Mode = (int)mode;
		setblocks.Shape = (int)shape;
		setblocks.Slotid = slotId;
		setblocks.Scale = CollectibleNet.SerializeFloatVeryPrecise(scale);
		if (colors != null)
		{
			setblocks.SetColors(colors.ToArray());
		}
		SendPacket(player, new Packet_Server
		{
			Id = 52,
			HighlightBlocks = setblocks
		});
	}

	public void SendSound(IServerPlayer player, AssetLocation location, double x, double y, double z, float pitch, float range, float volume, EnumSoundType soundType = EnumSoundType.Sound)
	{
		Packet_ServerSound p = new Packet_ServerSound
		{
			Name = location.ToString(),
			X = CollectibleNet.SerializeFloat((float)x),
			Y = CollectibleNet.SerializeFloat((float)y),
			Z = CollectibleNet.SerializeFloat((float)z),
			Range = CollectibleNet.SerializeFloat(range),
			Pitch = CollectibleNet.SerializeFloatPrecise(pitch),
			Volume = CollectibleNet.SerializeFloatPrecise(volume),
			SoundType = (int)soundType
		};
		SendPacket(player, new Packet_Server
		{
			Id = 18,
			Sound = p
		});
	}

	public void BroadcastPacket(Packet_Server packet, params IServerPlayer[] skipPlayers)
	{
		byte[] data = Serialize(packet);
		if (doNetBenchmark)
		{
			recordInBenchmark(packet.Id, data.Length);
		}
		BroadcastArbitraryPacket(data, skipPlayers);
	}

	internal void BroadcastArbitraryPacket(byte[] data, params IServerPlayer[] skipPlayers)
	{
		foreach (ConnectedClient client in Clients.Values)
		{
			if (client.State != 0 && client.State != EnumClientState.Queued && (skipPlayers == null || !skipPlayers.Any((IServerPlayer plr) => plr?.ClientId == client.Id)))
			{
				SendPacket(client.Player, data);
			}
		}
	}

	internal void BroadcastArbitraryUdpPacket(Packet_UdpPacket data, params IServerPlayer[] skipPlayers)
	{
		foreach (ConnectedClient client in Clients.Values)
		{
			if (client.State != 0 && client.State != EnumClientState.Queued && (skipPlayers == null || skipPlayers.All((IServerPlayer plr) => plr?.ClientId != client.Id)))
			{
				SendPacket(client, data);
			}
		}
	}

	private void recordInBenchmark(int packetId, int dataLength)
	{
		if (packetBenchmark.ContainsKey(packetId))
		{
			packetBenchmark[packetId]++;
			packetBenchmarkBytes[packetId] += dataLength;
		}
		else
		{
			packetBenchmark[packetId] = 1;
			packetBenchmarkBytes[packetId] = dataLength;
		}
	}

	public void SendPacket(int clientId, Packet_Server packet)
	{
		int len = Serialize_(packet);
		if (doNetBenchmark)
		{
			recordInBenchmark(packet.Id, len);
		}
		SendPacket(clientId, reusableBuffer);
	}

	public void SendPacketFast(int clientId, Packet_Server packet)
	{
		if (!Clients.TryGetValue(clientId, out var client) || !client.IsSinglePlayerClient || !DummyNetConnection.SendServerPacketDirectly(packet))
		{
			SendPacket(clientId, packet);
		}
	}

	public void SendPacket(int clientId, Packet_UdpPacket packet)
	{
		SendPacket(Clients[clientId], packet);
	}

	public void SendPacket(ConnectedClient client, Packet_UdpPacket packet)
	{
		if (client.FallBackToTcp)
		{
			Packet_Server packetServer = new Packet_Server
			{
				Id = 79,
				UdpPacket = packet
			};
			SendPacket(client.Id, packetServer);
		}
		else if (client.IsSinglePlayerClient)
		{
			UdpSockets[0].SendToClient(client.Id, packet);
		}
		else
		{
			int bytesSend = UdpSockets[1].SendToClient(client.Id, packet);
			UpdateUdpStatsAndBenchmark(packet, bytesSend);
		}
	}

	internal void UpdateUdpStatsAndBenchmark(Packet_UdpPacket packet, int byteCount)
	{
		StatsCollector[StatsCollectorIndex].statTotalUdpPackets++;
		StatsCollector[StatsCollectorIndex].statTotalUdpPacketsLength += byteCount;
		TotalSentBytesUdp += byteCount;
		if (doNetBenchmark)
		{
			if (!udpPacketBenchmark.TryAdd(packet.Id, 1))
			{
				udpPacketBenchmark[packet.Id]++;
				udpPacketBenchmarkBytes[packet.Id] += byteCount;
			}
			else
			{
				udpPacketBenchmarkBytes[packet.Id] = byteCount;
			}
		}
	}

	public void SendPacket(IServerPlayer player, Packet_Server packet)
	{
		if (player != null && player.ConnectionState != 0)
		{
			int len = Serialize_(packet);
			if (doNetBenchmark)
			{
				recordInBenchmark(packet.Id, len);
			}
			SendPacket(player.ClientId, reusableBuffer);
		}
	}

	private void SendPacket(IServerPlayer player, byte[] packet)
	{
		if (player != null && player.ConnectionState != 0)
		{
			SendPacket(player.ClientId, packet);
		}
	}

	private void SendPacket(int clientId, byte[] packet)
	{
		bool compressed = false;
		StatsCollector[StatsCollectorIndex].statTotalPackets++;
		if (packet.Length > 5120 && Config.CompressPackets && !Clients[clientId].IsSinglePlayerClient)
		{
			packet = Compression.Compress(packet);
			compressed = true;
		}
		StatsCollector[StatsCollectorIndex].statTotalPacketsLength += packet.Length;
		TotalSentBytes += packet.Length;
		EnumSendResult result = EnumSendResult.Ok;
		try
		{
			result = Clients[clientId].Socket.Send(packet, compressed);
		}
		catch (Exception e)
		{
			Logger.Error("Network exception:.");
			Logger.Error(e);
			if (Clients.TryGetValue(clientId, out var client))
			{
				DisconnectPlayer(client, "Lost connection");
			}
		}
		if (result == EnumSendResult.Disconnected)
		{
			ConnectedClient client2 = Clients[clientId];
			EnqueueMainThreadTask(delegate
			{
				DisconnectPlayer(client2, "Lost connection/disconnected");
				FrameProfiler.Mark("disconnectplayer");
			});
		}
	}

	private void SendPacket(int clientId, BoxedPacket box)
	{
		if (!Clients.TryGetValue(clientId, out var client))
		{
			return;
		}
		switch (client.Socket.HiPerformanceSend(box, Logger, Config.CompressPackets))
		{
		case EnumSendResult.Ok:
		{
			StatsCollection obj = StatsCollector[StatsCollectorIndex];
			obj.statTotalPackets++;
			obj.statTotalPacketsLength += box.LengthSent;
			TotalSentBytes += box.LengthSent;
			break;
		}
		case EnumSendResult.Error:
			DisconnectPlayer(client, "Lost connection");
			break;
		default:
			EnqueueMainThreadTask(delegate
			{
				DisconnectPlayer(client, "Lost connection/disconnected");
				FrameProfiler.Mark("disconnectplayer");
			});
			break;
		}
	}

	private void SendPlayerEntities(IServerPlayer player)
	{
		Packet_Entities entitiesPacket = new Packet_Entities
		{
			Entities = new Packet_Entity[Clients.Count],
			EntitiesCount = Clients.Count,
			EntitiesLength = Clients.Count
		};
		using (FastMemoryStream ms = new FastMemoryStream())
		{
			BinaryWriter writer = new BinaryWriter(ms);
			int i = 0;
			foreach (ConnectedClient client in Clients.Values)
			{
				if (client.Entityplayer != null)
				{
					entitiesPacket.Entities[i] = ServerPackets.GetEntityPacket(client.Entityplayer, ms, writer);
					i++;
				}
			}
			entitiesPacket.EntitiesCount = i;
		}
		SendPacket(player, new Packet_Server
		{
			Id = 40,
			Entities = entitiesPacket
		});
	}

	public void SendServerAssets(IServerPlayer player)
	{
		if (player == null || player.ConnectionState == EnumClientState.Offline)
		{
			return;
		}
		if (serverAssetsPacket.Length == 0)
		{
			if (serverAssetsPacket.packet == null)
			{
				WaitOnBuildServerAssetsPacket();
			}
			if (serverAssetsPacket.Length == 0)
			{
				if (Clients.TryGetValue(player.ClientId, out var client) && client.IsSinglePlayerClient)
				{
					if (serverAssetsSentLocally || DummyNetConnection.SendServerAssetsPacketDirectly(serverAssetsPacket.packet))
					{
						return;
					}
				}
				else
				{
					serverAssetsPacket.Serialize(serverAssetsPacket.packet);
				}
			}
		}
		SendPacket(player.ClientId, serverAssetsPacket);
	}

	private void StartBuildServerAssetsPacket()
	{
		TyronThreadPool.QueueLongDurationTask(BuildServerAssetsPacket, "serverassetspacket");
		Logger.VerboseDebug("Starting to build server assets packet");
	}

	private void WaitOnBuildServerAssetsPacket()
	{
		int countDown = 500;
		while (serverAssetsPacket.Length == 0 && serverAssetsPacket.packet == null && countDown-- > 0)
		{
			Thread.Sleep(20);
		}
		if (serverAssetsPacket.Length == 0 && serverAssetsPacket.packet == null)
		{
			Logger.Error("Waiting on buildServerAssetsPacket thread for longer than 10 seconds timeout, trying again ... this may take a while!");
			BuildServerAssetsPacket();
		}
	}

	private void BuildServerAssetsPacket()
	{
		try
		{
			using FastMemoryStream reusableMemoryStream = new FastMemoryStream();
			Packet_ServerAssets packet = new Packet_ServerAssets();
			List<Packet_BlockType> blockPackets = new List<Packet_BlockType>();
			int j = 0;
			foreach (Block block in Blocks)
			{
				try
				{
					blockPackets.Add(BlockTypeNet.GetBlockTypePacket(block, api.ClassRegistry, reusableMemoryStream));
					block.FreeRAMServer();
				}
				catch (Exception e3)
				{
					Logger.Fatal("Failed networking encoding block {0}:", block.Code);
					Logger.Fatal(e3);
					throw new Exception("SendServerAssets failed. See log files.");
				}
				if (j++ % 1000 == 999)
				{
					Thread.Sleep(5);
				}
			}
			packet.SetBlocks(blockPackets.ToArray());
			Thread.Sleep(5);
			List<Packet_ItemType> itemPackets = new List<Packet_ItemType>();
			for (int i = 0; i < Items.Count; i++)
			{
				Item item = Items[i];
				if (item != null && !(item.Code == null))
				{
					try
					{
						itemPackets.Add(ItemTypeNet.GetItemTypePacket(item, api.ClassRegistry, reusableMemoryStream));
						item.FreeRAMServer();
					}
					catch (Exception e2)
					{
						Logger.Fatal("Failed network encoding block {0}:", item.Code);
						Logger.Fatal(e2);
						throw new Exception("SendServerAssets failed. See log files.");
					}
					if (i % 1000 == 999)
					{
						Thread.Sleep(5);
					}
				}
			}
			packet.SetItems(itemPackets.ToArray());
			Thread.Sleep(5);
			Packet_EntityType[] entityPackets = new Packet_EntityType[EntityTypesByCode.Count];
			j = 0;
			foreach (EntityProperties entityType in EntityTypes)
			{
				try
				{
					entityPackets[j++] = EntityTypeNet.EntityPropertiesToPacket(entityType, reusableMemoryStream);
					entityType.Client?.FreeRAMServer();
				}
				catch (Exception e)
				{
					Logger.Fatal("Failed network encoding entity type {0}:", entityType?.Code);
					Logger.Fatal(e);
					throw new Exception("SendServerAssets failed. See log files.");
				}
				if (j % 100 == 99)
				{
					Thread.Sleep(5);
				}
			}
			packet.SetEntities(entityPackets);
			Thread.Sleep(5);
			Packet_Recipes[] recipeRecipes = new Packet_Recipes[recipeRegistries.Count];
			j = 0;
			foreach (KeyValuePair<string, RecipeRegistryBase> val in recipeRegistries)
			{
				recipeRecipes[j++] = RecipesToPacket(val.Value, val.Key, this, reusableMemoryStream);
			}
			packet.SetRecipes(recipeRecipes);
			Thread.Sleep(5);
			Packet_Server packetToSend = new Packet_Server
			{
				Id = 19,
				Assets = packet
			};
			Logger.VerboseDebug("Finished building server assets packet");
			if (IsDedicatedServer)
			{
				serverAssetsPacket.Serialize(packetToSend);
				return;
			}
			serverAssetsPacket.packet = packetToSend;
			if (DummyNetConnection.SendServerPacketDirectly(CreatePacketIdentification(controlServerPrivilege: true)))
			{
				if (DummyNetConnection.SendServerAssetsPacketDirectly(packetToSend))
				{
					serverAssetsSentLocally = true;
					worldMetaDataPacketAlreadySentToSinglePlayer = DummyNetConnection.SendServerPacketDirectly(WorldMetaDataPacket());
				}
				Logger.VerboseDebug("Single player: sent server assets packet to client");
			}
		}
		catch (ThreadAbortException)
		{
		}
	}

	private static Packet_Recipes RecipesToPacket(RecipeRegistryBase reg, string code, ServerMain world, FastMemoryStream ms)
	{
		if (reg is RecipeRegistryGeneric<GridRecipe> greg)
		{
			greg.ToBytes(world, out var recdata, out var quantity, ms);
			greg.FreeRAMServer();
			return new Packet_Recipes
			{
				Code = code,
				Data = recdata,
				Quantity = quantity
			};
		}
		reg.ToBytes(world, out var recdata2, out var quantity2);
		return new Packet_Recipes
		{
			Code = code,
			Data = recdata2,
			Quantity = quantity2
		};
	}

	private void SendWorldMetaData(IServerPlayer player)
	{
		if (!worldMetaDataPacketAlreadySentToSinglePlayer || !Clients.TryGetValue(player.ClientId, out var client) || client == null || !client.IsSinglePlayerClient)
		{
			SendPacket(player, WorldMetaDataPacket());
		}
	}

	internal Packet_Server WorldMetaDataPacket()
	{
		Packet_WorldMetaData p = new Packet_WorldMetaData();
		int[] serializedBlockLightLevels = new int[blockLightLevels.Length];
		int[] serializedSunLightLevels = new int[sunLightLevels.Length];
		for (int i = 0; i < blockLightLevels.Length; i++)
		{
			serializedBlockLightLevels[i] = CollectibleNet.SerializeFloat(blockLightLevels[i]);
			serializedSunLightLevels[i] = CollectibleNet.SerializeFloat(sunLightLevels[i]);
		}
		p.SetBlockLightlevels(serializedBlockLightLevels);
		p.SetSunLightlevels(serializedSunLightLevels);
		p.SunBrightness = sunBrightness;
		p.SetWorldConfiguration((SaveGameData.WorldConfiguration as TreeAttribute).ToBytes());
		p.SeaLevel = seaLevel;
		return new Packet_Server
		{
			Id = 21,
			WorldMetaData = p
		};
	}

	private void SendLevelProgress(IServerPlayer player, int percentcomplete, string status)
	{
		Packet_ServerLevelProgress pprogress = new Packet_ServerLevelProgress
		{
			PercentComplete = percentcomplete,
			Status = status
		};
		Packet_Server p = new Packet_Server
		{
			Id = 5,
			LevelDataChunk = pprogress
		};
		SendPacket(player, p);
	}

	private void SendServerReady(IServerPlayer player)
	{
		Logger.Audit("{0} joined.", player.PlayerName);
		SendPacket(player, new Packet_Server
		{
			Id = 73,
			ServerReady = new Packet_ServerReady()
		});
	}

	private void SendServerIdentification(ServerPlayer player)
	{
		if (serverAssetsSentLocally && player.client.IsSinglePlayerClient)
		{
			((DummyUdpNetServer)UdpSockets[0]).LocalPlayer = player;
		}
		else
		{
			SendPacket(player, CreatePacketIdentification(player.HasPrivilege("controlserver")));
		}
	}

	private Packet_Server CreatePacketIdentification(bool controlServerPrivilege)
	{
		List<Packet_ModId> mods = (from mod in api.ModLoader.Mods
			where mod.Info.Side.IsUniversal()
			select new Packet_ModId
			{
				Modid = mod.Info.ModID,
				Name = mod.Info.Name,
				Networkversion = mod.Info.NetworkVersion,
				Version = mod.Info.Version,
				RequiredOnClient = mod.Info.RequiredOnClient
			}).ToList();
		Packet_ServerIdentification p = new Packet_ServerIdentification
		{
			GameVersion = "1.20.7",
			NetworkVersion = "1.20.8",
			ServerName = Config.ServerName,
			Seed = SaveGameData.Seed,
			SavegameIdentifier = SaveGameData.SavegameIdentifier,
			MapSizeX = WorldMap.MapSizeX,
			MapSizeY = WorldMap.MapSizeY,
			MapSizeZ = WorldMap.MapSizeZ,
			RegionMapSizeX = WorldMap.RegionMapSizeX,
			RegionMapSizeY = WorldMap.RegionMapSizeY,
			RegionMapSizeZ = WorldMap.RegionMapSizeZ,
			PlayStyle = SaveGameData.PlayStyle,
			PlayListCode = api.WorldManager.CurrentPlayStyle?.PlayListCode,
			RequireRemapping = ((controlServerPrivilege && requiresRemaps) ? 1 : 0)
		};
		Logger.Notification("Sending server identification with remap " + requiresRemaps + ".  Server control privilege is " + controlServerPrivilege);
		p.SetMods(mods.ToArray());
		p.SetWorldConfiguration((SaveGameData.WorldConfiguration as TreeAttribute).ToBytes());
		if (Config.ModIdBlackList != null && Config.ModIdWhiteList == null)
		{
			p.SetServerModIdBlackList(Config.ModIdBlackList);
		}
		if (Config.ModIdWhiteList != null)
		{
			p.SetServerModIdWhiteList(Config.ModIdWhiteList);
		}
		return new Packet_Server
		{
			Id = 1,
			Identification = p
		};
	}

	public void BroadcastPlayerData(IServerPlayer owningPlayer, bool sendInventory = true, bool sendPrivileges = false)
	{
		Packet_Server ownplayerpacket = ((ServerWorldPlayerData)owningPlayer.WorldData).ToPacket(owningPlayer, sendInventory, sendPrivileges);
		Packet_Server otherplayerspacket = ((ServerWorldPlayerData)owningPlayer.WorldData).ToPacketForOtherPlayers(owningPlayer);
		SendPacket(owningPlayer, ownplayerpacket);
		BroadcastPacket(otherplayerspacket, owningPlayer);
	}

	public void SendOwnPlayerData(IServerPlayer owningPlayer, bool sendInventory = true, bool sendPrivileges = false)
	{
		Packet_Server ownplayerpacket = ((ServerWorldPlayerData)owningPlayer.WorldData).ToPacket(owningPlayer, sendInventory, sendPrivileges);
		SendPacket(owningPlayer, ownplayerpacket);
	}

	public void SendInitialPlayerDataForOthers(IServerPlayer owningPlayer, IServerPlayer toPlayer)
	{
		Packet_Entities entitiesPacket = new Packet_Entities();
		entitiesPacket.SetEntities(new Packet_Entity[1] { ServerPackets.GetEntityPacket(owningPlayer.Entity) });
		IServerPlayer[] array = (from pair in Clients
			where !pair.Value.ServerAssetsSent || pair.Value.Id == owningPlayer.ClientId
			select pair.Value.Player).ToArray();
		IServerPlayer[] skipPlayers = array;
		BroadcastPacket(new Packet_Server
		{
			Id = 40,
			Entities = entitiesPacket
		}, skipPlayers);
		Packet_Server otherplayerspacket = ((ServerWorldPlayerData)owningPlayer.WorldData).ToPacketForOtherPlayers(owningPlayer);
		SendPacket(toPlayer, otherplayerspacket);
	}

	public void BroadcastPlayerPings()
	{
		Packet_ServerPlayerPing p = new Packet_ServerPlayerPing();
		int[] clientids = new int[Clients.Count];
		int[] pings = new int[Clients.Count];
		int i = 0;
		foreach (ConnectedClient client in Clients.Values)
		{
			if (client.State != EnumClientState.Connecting && client.State != 0 && client.State != EnumClientState.Queued)
			{
				clientids[i] = client.Id;
				pings[i] = (int)(1000f * client.Player.Ping);
				i++;
			}
		}
		p.SetPings(pings);
		p.SetClientIds(clientids);
		byte[] packet = Serialize(new Packet_Server
		{
			Id = 3,
			PlayerPing = p
		});
		BroadcastArbitraryPacket(packet);
	}

	public void SendServerRedirect(IServerPlayer player, string host, string name)
	{
		Packet_Server p = new Packet_Server
		{
			Id = 29,
			Redirect = new Packet_ServerRedirect
			{
				Host = host,
				Name = name
			}
		};
		SendPacket(player, p);
	}

	public void UpdateEntityChunk(Entity entity, long newChunkIndex3d)
	{
		IWorldChunk newChunk = worldmap.GetChunk(newChunkIndex3d);
		if (newChunk != null)
		{
			worldmap.GetChunk(entity.InChunkIndex3d)?.RemoveEntity(entity.EntityId);
			newChunk.AddEntity(entity);
			entity.InChunkIndex3d = newChunkIndex3d;
		}
	}

	public int SetMiniDimension(IMiniDimension dimension, int index)
	{
		LoadedMiniDimensions[index] = dimension;
		return index;
	}

	public IMiniDimension GetMiniDimension(int index)
	{
		LoadedMiniDimensions.TryGetValue(index, out var dimension);
		return dimension;
	}

	public ServerChunk GetLoadedChunk(long index3d)
	{
		ServerChunk chunk = null;
		loadedChunksLock.AcquireReadLock();
		try
		{
			loadedChunks.TryGetValue(index3d, out chunk);
			return chunk;
		}
		finally
		{
			loadedChunksLock.ReleaseReadLock();
		}
	}

	public void SendChunk(int chunkX, int chunkY, int chunkZ, IServerPlayer player, bool onlyIfInRange)
	{
		ServerPlayer plr = player as ServerPlayer;
		if (plr?.Entity == null || plr?.client == null)
		{
			return;
		}
		ConnectedClient client = plr.client;
		long index3d = WorldMap.ChunkIndex3D(chunkX, chunkY, chunkZ);
		long index2d = WorldMap.MapChunkIndex2D(chunkX, chunkZ);
		float viewDist = client.WorldData.Viewdistance + 16;
		if (!onlyIfInRange || client.Entityplayer.ServerPos.InHorizontalRangeOf(chunkX * 32 + 16, chunkZ * 32 + 16, viewDist * viewDist))
		{
			if (!client.DidSendMapChunk(index2d))
			{
				client.forceSendMapChunks.Add(index2d);
			}
			client.forceSendChunks.Add(index3d);
		}
	}

	public void BroadcastChunk(int chunkX, int chunkY, int chunkZ, bool onlyIfInRange)
	{
		foreach (ConnectedClient client in Clients.Values)
		{
			if (client.Entityplayer == null)
			{
				continue;
			}
			long index3d = WorldMap.ChunkIndex3D(chunkX, chunkY, chunkZ);
			long index2d = WorldMap.MapChunkIndex2D(chunkX, chunkZ);
			float viewDist = client.WorldData.Viewdistance + 16;
			if (!onlyIfInRange || client.Entityplayer.ServerPos.InHorizontalRangeOf(chunkX * 32 + 16, chunkZ * 32 + 16, viewDist * viewDist))
			{
				if (!client.DidSendMapChunk(index2d))
				{
					client.forceSendMapChunks.Add(index2d);
				}
				client.forceSendChunks.Add(index3d);
			}
		}
	}

	public void BroadcastChunkColumn(int chunkX, int chunkZ, bool onlyIfInRange)
	{
		foreach (ConnectedClient client in Clients.Values)
		{
			if (client.Entityplayer == null)
			{
				continue;
			}
			float viewDist = client.WorldData.Viewdistance + 16;
			if (!onlyIfInRange || client.Entityplayer.ServerPos.InHorizontalRangeOf(chunkX * 32 + 16, chunkZ * 32 + 16, viewDist * viewDist))
			{
				for (int cy = 0; cy < WorldMap.ChunkMapSizeY; cy++)
				{
					long index3d = WorldMap.ChunkIndex3D(chunkX, cy, chunkZ);
					client.forceSendChunks.Add(index3d);
				}
			}
		}
	}

	public void ResendMapChunk(int chunkX, int chunkZ, bool onlyIfInRange)
	{
		foreach (ConnectedClient client in Clients.Values)
		{
			if (client.Entityplayer != null)
			{
				long index2d = WorldMap.MapChunkIndex2D(chunkX, chunkZ);
				float viewDist = client.WorldData.Viewdistance + 16;
				if (!onlyIfInRange || client.Entityplayer.ServerPos.InHorizontalRangeOf(chunkX * 32 + 16, chunkZ * 32 + 16, viewDist * viewDist))
				{
					client.forceSendMapChunks.Add(index2d);
				}
			}
		}
	}

	public void LoadChunkColumnFast(int chunkX, int chunkZ, ChunkLoadOptions options = null)
	{
		if (options == null)
		{
			options = defaultOptions;
		}
		long mapindex2d = WorldMap.MapChunkIndex2D(chunkX, chunkZ);
		if (options.KeepLoaded)
		{
			AddChunkColumnToForceLoadedList(mapindex2d);
		}
		if (!IsChunkColumnFullyLoaded(chunkX, chunkZ))
		{
			KeyValuePair<HorRectanglei, ChunkLoadOptions> data = new KeyValuePair<HorRectanglei, ChunkLoadOptions>(new HorRectanglei(chunkX, chunkZ, chunkX, chunkZ), options);
			fastChunkQueue.Enqueue(data);
		}
		else
		{
			options.OnLoaded?.Invoke();
		}
	}

	public void LoadChunkColumnFast(int chunkX1, int chunkZ1, int chunkX2, int chunkZ2, ChunkLoadOptions options = null)
	{
		if (options == null)
		{
			options = defaultOptions;
		}
		if (options.KeepLoaded)
		{
			for (int chunkX3 = chunkX1; chunkX3 <= chunkX2; chunkX3++)
			{
				for (int chunkZ3 = chunkZ1; chunkZ3 <= chunkZ2; chunkZ3++)
				{
					long mapindex2d = WorldMap.MapChunkIndex2D(chunkX3, chunkZ3);
					AddChunkColumnToForceLoadedList(mapindex2d);
				}
			}
		}
		KeyValuePair<HorRectanglei, ChunkLoadOptions> data = new KeyValuePair<HorRectanglei, ChunkLoadOptions>(new HorRectanglei(chunkX1, chunkZ1, chunkX2, chunkZ2), options);
		fastChunkQueue.Enqueue(data);
	}

	public void PeekChunkColumn(int chunkX, int chunkZ, ChunkPeekOptions options)
	{
		if (options == null)
		{
			throw new ArgumentNullException("options argument must not be null");
		}
		if (options.OnGenerated == null)
		{
			throw new ArgumentNullException("options.OnGenerated must not be null (there is no point to calling this method otherwise)");
		}
		KeyValuePair<Vec2i, ChunkPeekOptions> data = new KeyValuePair<Vec2i, ChunkPeekOptions>(new Vec2i(chunkX, chunkZ), options);
		peekChunkColumnQueue.Enqueue(data);
	}

	public void TestChunkExists(int chunkX, int chunkY, int chunkZ, Action<bool> onTested, EnumChunkType type)
	{
		testChunkExistsQueue.Enqueue(new ChunkLookupRequest(chunkX, chunkY, chunkZ, onTested)
		{
			Type = type
		});
	}

	public void LoadChunkColumn(int chunkX, int chunkZ, bool keepLoaded = false)
	{
		long mapindex2d = WorldMap.MapChunkIndex2D(chunkX, chunkZ);
		if (keepLoaded)
		{
			AddChunkColumnToForceLoadedList(mapindex2d);
		}
		if (!IsChunkColumnFullyLoaded(chunkX, chunkZ))
		{
			lock (requestedChunkColumnsLock)
			{
				requestedChunkColumns.Enqueue(mapindex2d);
			}
		}
	}

	public void AddChunkColumnToForceLoadedList(long mapindex2d)
	{
		forceLoadedChunkColumns.Add(mapindex2d);
	}

	public void RemoveChunkColumnFromForceLoadedList(long mapindex2d)
	{
		forceLoadedChunkColumns.Remove(mapindex2d);
	}

	public bool IsChunkColumnFullyLoaded(int chunkX, int chunkZ)
	{
		long xzMultiplier = 2097152L;
		xzMultiplier *= xzMultiplier;
		long chunkIndex3d = WorldMap.ChunkIndex3D(chunkX, 0, chunkZ);
		loadedChunksLock.AcquireReadLock();
		try
		{
			for (long cy = 0L; cy < WorldMap.ChunkMapSizeY; cy++)
			{
				if (!loadedChunks.ContainsKey(chunkIndex3d + cy * xzMultiplier))
				{
					return false;
				}
			}
		}
		finally
		{
			loadedChunksLock.ReleaseReadLock();
		}
		return true;
	}

	public void CreateChunkColumnForDimension(int cx, int cz, int dim)
	{
		int quantity = WorldMap.ChunkMapSizeY;
		ServerMapChunk mapChunk = (ServerMapChunk)WorldMap.GetMapChunk(cx, cz);
		int cy = dim * 32768 / 32;
		loadedChunksLock.AcquireWriteLock();
		try
		{
			for (int y = 0; y < quantity; y++)
			{
				long index3d = WorldMap.ChunkIndex3D(cx, cy + y, cz);
				ServerChunk chunk = ServerChunk.CreateNew(serverChunkDataPool);
				chunk.serverMapChunk = mapChunk;
				loadedChunks[index3d] = chunk;
				chunk.MarkToPack();
			}
		}
		finally
		{
			loadedChunksLock.ReleaseWriteLock();
		}
	}

	public void LoadChunkColumnForDimension(int cx, int cz, int dim)
	{
		ChunkColumnLoadRequest request = new ChunkColumnLoadRequest(WorldMap.MapChunkIndex2D(cx, cz), cx, cz, -1, 6, this);
		request.dimension = dim;
		simpleLoadRequests.Enqueue(request);
	}

	public void ForceSendChunkColumn(IServerPlayer player, int cx, int cz, int dimension)
	{
		ConnectedClient client = ((ServerPlayer)player).client;
		int maxY = WorldMap.ChunkMapSizeY;
		for (int cy = 0; cy < maxY; cy++)
		{
			long index = WorldMap.ChunkIndex3D(cx, cy, cz, dimension);
			client.forceSendChunks.Add(index);
		}
	}

	public bool BlockingTestMapRegionExists(int regionX, int regionZ)
	{
		return chunkThread.gameDatabase.MapRegionExists(regionX, regionZ);
	}

	public bool BlockingTestMapChunkExists(int chunkX, int chunkZ)
	{
		return chunkThread.gameDatabase.MapChunkExists(chunkX, chunkZ);
	}

	public IServerChunk[] BlockingLoadChunkColumn(int chunkX, int chunkZ)
	{
		ChunkColumnLoadRequest chunkColumnLoadRequest = new ChunkColumnLoadRequest(0L, chunkX, chunkZ, -1, 0, this);
		return chunkThread.loadsavechunks.TryLoadChunkColumn(chunkColumnLoadRequest);
	}
}
