using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Client.NoObf;

public sealed class ClientMain : GameMain, IWorldIntersectionSupplier, IClientWorldAccessor, IWorldAccessor
{
	public const int millisecondsToTriggerNewFrame = 60;

	public GuiComposerManager GuiComposers;

	public bool clientPlayingFired;

	public GuiScreenRunningGame ScreenRunningGame;

	public bool[] PreviousKeyboardState;

	public bool[] KeyboardState;

	public bool[] KeyboardStateRaw;

	public bool mouseWorldInteractAnyway;

	public int MouseCurrentX;

	public int MouseCurrentY;

	public int particleLevel;

	public bool BlocksReceivedAndLoaded;

	public bool AssetsReceived;

	public bool DoneColorMaps;

	public bool DoneBlockAndItemShapeLoading;

	public bool AutoJumpEnabled;

	public bool IsPaused;

	public int FrameRateMode;

	public bool LagSimulation;

	public bool Spawned;

	public bool doReconnect;

	public bool exitToMainMenu;

	public bool disposed;

	public string disconnectReason;

	public string disconnectAction;

	public List<string> disconnectMissingMods;

	public string exitReason;

	public bool deleteWorld;

	public bool exitToDisconnectScreen;

	public bool ShouldRender2DOverlays;

	public bool AllowFreemove;

	public bool forceLiquidSelectable;

	public float timelapse;

	public float timelapsedCurrent;

	public float timelapseEnd;

	public Stopwatch InWorldStopwatch = new Stopwatch();

	public SystemMusicEngine MusicEngine;

	public bool AllowCameraControl = true;

	public bool AllowCharacterControl = true;

	public bool StartedConnecting;

	public bool UdpTryConnect;

	public bool FallBackToTcp;

	public bool threadsShouldExit;

	public bool ShouldRedrawAllBlocks;

	public bool doTransparentRenderPass = ClientSettings.TransparentRenderPass;

	public bool extendedDebugInfo;

	public bool HandSetAttackBuild;

	public bool HandSetAttackDestroy;

	public string ServerNetworkVersion;

	public string ServerGameVersion;

	public ServerPacketHandler<Packet_Server>[] PacketHandlers;

	public HandleServerCustomUdpPacket HandleCustomUdpPackets;

	public SystemModHandler modHandler;

	public double[] PerspectiveProjectionMat = new double[16];

	public double[] PerspectiveViewMat = new double[16];

	public const int ClientChunksize = 32;

	public const int ClientChunksizeMask = 31;

	public const int ClientChunksizebits = 5;

	public ClientPlayer player;

	public static ClassRegistry ClassRegistry;

	public Dictionary<string, ClientPlayer> PlayersByUid = new Dictionary<string, ClientPlayer>(10);

	public Dictionary<long, Entity> LoadedEntities = new Dictionary<long, Entity>(100);

	public EntityPos SpawnPosition;

	public ClientSystem[] clientSystems;

	public CancellationTokenSource MusicEngineCts;

	public OrderedDictionary<AssetLocation, EntityProperties> EntityClassesByCode = new OrderedDictionary<AssetLocation, EntityProperties>();

	private List<EntityProperties> entityTypesCached;

	private List<string> entityCodesCached;

	public OrderedDictionary<string, string> DebugScreenInfo;

	public AmbientManager AmbientManager;

	public int[][] FastBlockTextureSubidsByBlockAndFace;

	public int textureSize = 32;

	public ShapeTesselatorManager TesselatorManager;

	public BlockTextureAtlasManager BlockAtlasManager;

	public ItemTextureAtlasManager ItemAtlasManager;

	public EntityTextureAtlasManager EntityAtlasManager;

	private Dictionary<AssetLocation, LoadedTexture> texturesByLocation;

	private List<Thread> clientThreads = new List<Thread>();

	private CancellationTokenSource _clientThreadsCts;

	public ClientEventManager eventManager;

	public MacroManager macroManager;

	public PlayerCamera MainCamera;

	public ClientPlatformAbstract Platform;

	public ClientCoreAPI api;

	public ChunkTesselator TerrainChunkTesselator;

	public bool ShouldTesselateTerrain = true;

	public TerrainIlluminator terrainIlluminator;

	public ClientWorldMap WorldMap;

	public float DeltaTimeLimiter = -1f;

	public MeshRef quadModel;

	public const int DisconnectedIconAfterSeconds = 5;

	public bool Drawblockinfo = ClientSettings.ShowBlockInfoHud;

	public bool offScreenRendering = true;

	public NetClient MainNetClient;

	public UNetClient UdpNetClient;

	public ThreadLocal<Random> rand;

	public long LastReceivedMilliseconds;

	public ServerConnectData Connectdata;

	public bool IsSingleplayer;

	public bool OpenedToLan;

	public bool OpenedToInternet;

	public bool KillNextFrame;

	public FrustumCulling frustumCuller;

	public IColorPresets ColorPreset;

	public DefaultShaderUniforms shUniforms = new DefaultShaderUniforms();

	private ClientSystemStartup csStartup;

	public SystemNetworkProcess networkProc;

	private Queue<ClientTask> reversedQueue = new Queue<ClientTask>();

	private Queue<ClientTask> holdingQueue = new Queue<ClientTask>();

	public ShaderProgramGui guiShaderProg;

	public string tickSummary;

	private LoadedTexture tmpTex;

	private double[] set3DProjectionTempMat4;

	public bool CurrentMatrixModeProjection;

	public StackMatrix4 MvMatrix;

	public StackMatrix4 PMatrix;

	private double[] identityMatrix;

	private double[] glScaleTempVec3;

	private double[] glRotateTempVec3;

	private float[] tmpMatrix = new float[16];

	private double[] tmpMatrixd = new double[16];

	public bool preventPlacementInLava = true;

	public MouseButtonState MouseStateRaw = new MouseButtonState();

	public MouseButtonState InWorldMouseState = new MouseButtonState();

	public bool PickBlock;

	public double MouseDeltaX;

	public double MouseDeltaY;

	public double DelayedMouseDeltaX;

	public double DelayedMouseDeltaY;

	private float rotationspeed;

	private float swimmingMouseSmoothing;

	public float mouseYaw;

	public float mousePitch;

	private Vec3f prevMountAngles = new Vec3f();

	private Dictionary<int, Block> noBlocks = new Dictionary<int, Block>();

	private int whitetexture;

	public MultiplayerServerEntry RedirectTo;

	private int lastWidth;

	private int lastHeight;

	public object MainThreadTasksLock = new object();

	public Queue<ClientTask> GameLaunchTasks;

	public Queue<ClientTask> MainThreadTasks;

	private CollisionTester collTester = new CollisionTester();

	internal SoundConfig SoundConfig;

	internal Dictionary<AssetLocation, int> SoundIteration = new Dictionary<AssetLocation, int>();

	internal Queue<ILoadedSound> ActiveSounds = new Queue<ILoadedSound>();

	internal EnumRenderStage currentRenderStage;

	private long lastSkipTotalMs = -1000L;

	private int cntSkip;

	internal ClientGameCalendar GameWorldCalendar;

	internal bool ignoreServerCalendarUpdates;

	public HashSet<ChunkPos> chunkPositionsForRegenTrav = new HashSet<ChunkPos>();

	public object chunkPositionsLock = new object();

	public Queue<long> compactedClientChunks = new Queue<long>();

	public object compactSyncLock = new object();

	internal bool unbindSamplers;

	public EntityBehaviorManager entityBehaviors = new EntityBehaviorManager();

	public object EntityLoadQueueLock = new object();

	public Stack<Entity> EntityLoadQueue = new Stack<Entity>();

	internal List<ModId> ServerMods;

	internal List<string> ServerModIdBlacklist = new List<string>();

	internal List<string> ServerModIdWhitelist = new List<string>();

	private TreeAttribute WorldConfig;

	internal List<GuiDialog> LoadedGuis = new List<GuiDialog>();

	internal List<GuiDialog> OpenedGuis = new List<GuiDialog>();

	internal Dictionary<int, PlayerGroup> OwnPlayerGroupsById = new Dictionary<int, PlayerGroup>();

	internal Dictionary<int, PlayerGroupMembership> OwnPlayerGroupMemembershipsById = new Dictionary<int, PlayerGroupMembership>();

	public int currentGroupid = GlobalConstants.GeneralChatGroup;

	public Dictionary<int, LimitedList<string>> ChatHistoryByPlayerGroup = new Dictionary<int, LimitedList<string>>();

	internal Dictionary<BlockPos, BlockDamage> damagedBlocks = new Dictionary<BlockPos, BlockDamage>();

	internal PickingRayUtil pickingRayUtil = new PickingRayUtil();

	public TrackedPlayerProperties playerProperties = new TrackedPlayerProperties();

	internal object asyncParticleSpawnersLock = new object();

	internal List<ContinousParticleSpawnTaskDelegate> asyncParticleSpawners = new List<ContinousParticleSpawnTaskDelegate>();

	internal List<IPointLight> pointlights = new List<IPointLight>();

	internal Dictionary<Entity, EntityRenderer> EntityRenderers = new Dictionary<Entity, EntityRenderer>();

	internal float[] toShadowMapSpaceMatrixFar = Mat4f.Create();

	internal float[] toShadowMapSpaceMatrixNear = Mat4f.Create();

	internal float[] shadowMvpMatrix = Mat4f.Create();

	internal Vec3f FogColorSky = new Vec3f();

	internal int skyGlowTextureId;

	internal int skyTextureId;

	internal int frameSeed;

	internal ChunkRenderer chunkRenderer;

	internal object dirtyChunksLastLock = new object();

	internal UniqueQueue<long> dirtyChunksLast = new UniqueQueue<long>();

	internal object dirtyChunksLock = new object();

	internal UniqueQueue<long> dirtyChunks = new UniqueQueue<long>();

	internal object dirtyChunksPriorityLock = new object();

	internal UniqueQueue<long> dirtyChunksPriority = new UniqueQueue<long>();

	internal object tesselatedChunksLock = new object();

	internal Queue<long> tesselatedChunks = new Queue<long>();

	internal object tesselatedChunksPriorityLock = new object();

	internal Queue<long> tesselatedChunksPriority = new Queue<long>();

	internal ServerInformation ServerInfo;

	internal bool SuspendMainThreadTasks;

	internal bool AssetLoadingOffThread;

	internal bool ServerReady;

	internal ParticleManager particleManager = new ParticleManager();

	public override IWorldAccessor World => this;

	protected override WorldMap worldmap => WorldMap;

	public int Seed => ServerInfo.Seed;

	public string SavegameIdentifier => ServerInfo.SavegameIdentifier;

	FrameProfilerUtil IWorldAccessor.FrameProfiler => ScreenManager.FrameProfiler;

	public long InWorldEllapsedMs => InWorldStopwatch.ElapsedMilliseconds;

	public bool LiquidSelectable => forceLiquidSelectable;

	public bool AmbientParticles
	{
		get
		{
			return ClientSettings.AmbientParticles;
		}
		set
		{
			ClientSettings.AmbientParticles = value;
		}
	}

	public IClientPlayer Player => player;

	public EntityPlayer EntityPlayer => player?.Entity;

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

	public EntityPos DefaultSpawnPosition => SpawnPosition;

	public BlockSelection BlockSelection
	{
		get
		{
			return EntityPlayer?.BlockSelection;
		}
		set
		{
			if (EntityPlayer != null)
			{
				EntityPlayer.BlockSelection = value;
			}
		}
	}

	public EntitySelection EntitySelection
	{
		get
		{
			return EntityPlayer?.EntitySelection;
		}
		set
		{
			if (EntityPlayer != null)
			{
				EntityPlayer.EntitySelection = value;
			}
		}
	}

	public long[] LoadedChunkIndices => WorldMap.chunks.Keys.ToArray();

	public long[] LoadedMapChunkIndices => WorldMap.MapChunks.Keys.ToArray();

	public float[] CurrentProjectionMatrix
	{
		get
		{
			for (int i = 0; i < 16; i++)
			{
				tmpMatrix[i] = (float)PMatrix.Top[i];
			}
			return tmpMatrix;
		}
	}

	public float[] CurrentModelViewMatrix
	{
		get
		{
			for (int i = 0; i < 16; i++)
			{
				tmpMatrix[i] = (float)MvMatrix.Top[i];
			}
			return tmpMatrix;
		}
	}

	public double[] CurrentModelViewMatrixd
	{
		get
		{
			for (int i = 0; i < 16; i++)
			{
				tmpMatrixd[i] = MvMatrix.Top[i];
			}
			return tmpMatrixd;
		}
	}

	public bool MouseGrabbed
	{
		get
		{
			return Platform.MouseGrabbed;
		}
		set
		{
			bool mouseGrabbed = Platform.MouseGrabbed;
			Platform.MouseGrabbed = value;
			if (!mouseGrabbed && value && DialogsOpened == 0)
			{
				player.inventoryMgr?.DropMouseSlotItems(fullStack: true);
				OnMouseMove(new MouseEvent(Width / 2, Height / 2));
			}
		}
	}

	public int Width => Platform.WindowSize.Width;

	public int Height => Platform.WindowSize.Height;

	public IBlockAccessor BlockAccessor => WorldMap.RelaxedBlockAccess;

	public IBulkBlockAccessor BulkBlockAccessor => WorldMap.BulkBlockAccess;

	IClassRegistryAPI IWorldAccessor.ClassRegistry => api.instancerapi;

	public Random Rand => rand.Value;

	public long ElapsedMilliseconds => Platform.EllapsedMs;

	public List<EntityProperties> EntityTypes => entityTypesCached ?? (entityTypesCached = EntityClassesByCode.Values.ToList());

	public List<string> EntityTypeCodes => entityCodesCached ?? (entityCodesCached = makeEntityCodesCache());

	public int DefaultEntityTrackingRange => 32;

	public ILogger Logger => Platform.Logger;

	public IAssetManager AssetManager => Platform.AssetManager;

	public EnumAppSide Side => EnumAppSide.Client;

	List<CollectibleObject> IWorldAccessor.Collectibles => Collectibles;

	List<GridRecipe> IWorldAccessor.GridRecipes => GridRecipes;

	IList<Block> IWorldAccessor.Blocks => Blocks;

	IList<Item> IWorldAccessor.Items => Items;

	List<EntityProperties> IWorldAccessor.EntityTypes => EntityTypes;

	public IPlayer[] AllOnlinePlayers => PlayersByUid.Values.Select((ClientPlayer player) => player).ToArray();

	public IPlayer[] AllPlayers => PlayersByUid.Values.Select((ClientPlayer player) => player).ToArray();

	float[] IWorldAccessor.BlockLightLevels => WorldMap.BlockLightLevels;

	float[] IWorldAccessor.SunLightLevels => WorldMap.SunLightLevels;

	public int SeaLevel => ClientWorldMap.seaLevel;

	public int MapSizeY => WorldMap.MapSizeY;

	int IWorldAccessor.SunBrightness => WorldMap.SunBrightness;

	public bool EntityDebugMode { get; set; }

	bool IClientWorldAccessor.ForceLiquidSelectable
	{
		get
		{
			return forceLiquidSelectable;
		}
		set
		{
			forceLiquidSelectable = value;
		}
	}

	public CollisionTester CollisionTester => collTester;

	Dictionary<long, Entity> IClientWorldAccessor.LoadedEntities => LoadedEntities;

	public Dictionary<int, IMiniDimension> MiniDimensions => ((ClientWorldMap)worldmap).dimensions;

	public ICoreAPI Api => api;

	public IChunkProvider ChunkProvider => WorldMap;

	public ILandClaimAPI Claims => WorldMap;

	public override Vec3i MapSize => WorldMap.MapSize;

	public ITreeAttribute Config => WorldConfig;

	public override IBlockAccessor blockAccessor => WorldMap.RelaxedBlockAccess;

	public IClientGameCalendar Calendar => GameWorldCalendar;

	IGameCalendar IWorldAccessor.Calendar => GameWorldCalendar;

	public int DialogsOpened => OpenedGuis.Count((GuiDialog elem) => elem.DialogType == EnumDialogType.Dialog);

	public ClientPlayer GetPlayerFromEntityId(long entityId)
	{
		return PlayersByUid.Values.FirstOrDefault((ClientPlayer player) => player.Entity != null && player.Entity.EntityId == entityId);
	}

	public ClientPlayer GetPlayerFromClientId(int clientId)
	{
		return PlayersByUid.Values.FirstOrDefault((ClientPlayer player) => player.ClientId == clientId);
	}

	public Dictionary<long, IWorldChunk> GetAllChunks()
	{
		Dictionary<long, IWorldChunk> dict = new Dictionary<long, IWorldChunk>();
		lock (WorldMap.chunksLock)
		{
			foreach (KeyValuePair<long, ClientChunk> val in WorldMap.chunks)
			{
				if (val.Key < 4503599627370496L)
				{
					dict[val.Key] = val.Value;
				}
			}
			return dict;
		}
	}

	public ClientMain(GuiScreenRunningGame screenRunningGame, ClientPlatformAbstract platform)
	{
		ClientMain clientMain = this;
		RuntimeStats.Reset();
		Platform = platform;
		ShapeElement.Logger = platform.Logger;
		ScreenRunningGame = screenRunningGame;
		eventManager = new ClientEventManager(this);
		MainCamera = new PlayerCamera(this);
		WorldMap = new ClientWorldMap(this);
		terrainIlluminator = new TerrainIlluminator(this);
		AmbientManager = new AmbientManager(this);
		BlockAtlasManager = new BlockTextureAtlasManager(this);
		ItemAtlasManager = new ItemTextureAtlasManager(this);
		EntityAtlasManager = new EntityTextureAtlasManager(this);
		TesselatorManager = new ShapeTesselatorManager(this);
		MeshData.Recycler = new MeshDataRecycler(this);
		if (ClientSettings.Inst.Bool.Get("disableMeshRecycler", defaultValue: false))
		{
			MeshData.Recycler.Dispose();
		}
		DebugScreenInfo = new OrderedDictionary<string, string>();
		DebugScreenInfo["fps"] = "";
		DebugScreenInfo["mem"] = "";
		DebugScreenInfo["triangles"] = "";
		DebugScreenInfo["occludedchunks"] = "";
		DebugScreenInfo["gpumemfrag"] = "";
		DebugScreenInfo["chunkstats"] = "";
		DebugScreenInfo["position"] = "";
		DebugScreenInfo["chunkpos"] = "";
		DebugScreenInfo["regpos"] = "";
		DebugScreenInfo["orientation"] = "";
		DebugScreenInfo["curblock"] = "";
		DebugScreenInfo["curblockentity"] = "";
		DebugScreenInfo["curblocklight"] = "";
		DebugScreenInfo["curblocklight2"] = "";
		DebugScreenInfo["entitycount"] = "";
		DebugScreenInfo["quadparticlepool"] = "";
		DebugScreenInfo["cubeparticlepool"] = "";
		DebugScreenInfo["incomingbytes"] = "";
		AutoJumpEnabled = false;
		MvMatrix = new StackMatrix4();
		PMatrix = new StackMatrix4();
		MvMatrix.Push(Mat4d.Create());
		PMatrix.Push(Mat4d.Create());
		whitetexture = -1;
		ShouldRender2DOverlays = true;
		AllowFreemove = true;
		AllowCameraControl = true;
		texturesByLocation = new Dictionary<AssetLocation, LoadedTexture>();
		FrameRateMode = 0;
		MainCamera.Fov = (float)ClientSettings.FieldOfView * ((float)Math.PI / 180f);
		ClientSettings.Inst.AddWatcher("leftDialogMargin", delegate(int newvalue)
		{
			GuiStyle.LeftDialogMargin = newvalue;
		});
		ClientSettings.Inst.AddWatcher("rightDialogMargin", delegate(int newvalue)
		{
			GuiStyle.RightDialogMargin = newvalue;
		});
		GuiStyle.LeftDialogMargin = ClientSettings.LeftDialogMargin;
		GuiStyle.RightDialogMargin = ClientSettings.RightDialogMargin;
		ClientSettings.Inst.AddWatcher<int>("fieldOfView", OnFowChanged);
		ClientSettings.Inst.AddWatcher("extendedDebugInfo", delegate(bool newvalue)
		{
			clientMain.extendedDebugInfo = newvalue;
		});
		EntityDebugMode = ClientSettings.ShowEntityDebugInfo;
		ClientSettings.Inst.AddWatcher("showEntityDebugInfo", delegate(bool newvalue)
		{
			clientMain.EntityDebugMode = newvalue;
		});
		ClientSettings.Inst.AddWatcher("showBlockInfo", delegate(bool newvalue)
		{
			clientMain.Drawblockinfo = newvalue;
		});
		ClientSettings.Inst.AddWatcher<float>("ssaa", delegate
		{
			platform.RebuildFrameBuffers();
		});
		ClientSettings.Inst.AddWatcher<int>("ssaoQuality", delegate
		{
			ShaderRegistry.ReloadShaders();
			platform.RebuildFrameBuffers();
			clientMain.eventManager?.TriggerReloadShaders();
		});
		ClientSettings.Inst.AddWatcher<float>("minbrightness", delegate
		{
			ShaderRegistry.ReloadShaders();
			clientMain.eventManager?.TriggerReloadShaders();
		});
		ClientSettings.Inst.AddWatcher<float>("guiScale", delegate
		{
			clientMain.GuiComposers.MarkAllDialogsForRecompose();
		});
		extendedDebugInfo = ClientSettings.ExtendedDebugInfo;
		particleLevel = ClientSettings.ParticleLevel;
		ClientSettings.Inst.AddWatcher("particleLevel", delegate(int val)
		{
			clientMain.particleLevel = val;
		});
		ClientSettings.Inst.AddWatcher<bool>("immersiveFpMode", delegate
		{
			clientMain.sendRuntimeSettings();
		});
		ClientSettings.Inst.AddWatcher<int>("itemCollectMode", delegate
		{
			clientMain.sendRuntimeSettings();
		});
		ClientSettings.Inst.AddWatcher("renderMetaBlocks", delegate(bool newvalue)
		{
			clientMain.player.worlddata.RenderMetablocks = newvalue;
			clientMain.player.worlddata.RequestMode(clientMain);
		});
		rotationspeed = 0.15f;
		swimmingMouseSmoothing = ClientSettings.SwimmingMouseSmoothing;
		ClientSettings.Inst.AddWatcher("swimmingMouseSmoothing", delegate(float newvalue)
		{
			clientMain.swimmingMouseSmoothing = newvalue;
		});
		KeyboardState = new bool[512];
		PreviousKeyboardState = new bool[512];
		KeyboardStateRaw = new bool[512];
		for (int i = 0; i < 512; i++)
		{
			KeyboardState[i] = false;
			KeyboardStateRaw[i] = false;
			PreviousKeyboardState[i] = false;
		}
		glScaleTempVec3 = Vec3Utilsd.Create();
		glRotateTempVec3 = Vec3Utilsd.Create();
		identityMatrix = Mat4d.Identity(Mat4d.Create());
		set3DProjectionTempMat4 = Mat4d.Create();
		PacketHandlers = new ServerPacketHandler<Packet_Server>[256];
		csStartup = new ClientSystemStartup(this);
		MainThreadTasks = new Queue<ClientTask>();
		GameLaunchTasks = new Queue<ClientTask>();
		ClientSettings.Inst.AddWatcher<int>("viewDistance", ViewDistanceChanged);
		ClientSettings.Inst.AddWatcher<int>("vsyncMode", OnVsyncChanged);
		api = new ClientCoreAPI(this);
		GuiComposers = new GuiComposerManager(api);
		ColorPreset = new ColorPresets(this, api);
		eventManager?.AddGameTickListener(freeMouseOnDefocus, 500);
	}

	public void sendRuntimeSettings()
	{
		SendPacketClient(new Packet_Client
		{
			Id = 32,
			RuntimeSetting = new Packet_RuntimeSetting
			{
				ImmersiveFpMode = (ClientSettings.ImmersiveFpMode ? 1 : 0),
				ItemCollectMode = ClientSettings.ItemCollectMode
			}
		});
	}

	private void freeMouseOnDefocus(float t1)
	{
		if (!Platform.IsFocused && Platform.MouseGrabbed)
		{
			MouseGrabbed = false;
		}
	}

	private void OnFowChanged(int newValue)
	{
		MainCamera.Fov = (float)ClientSettings.FieldOfView * ((float)Math.PI / 180f);
		MainCamera.ZNear = GameMath.Clamp(0.1f - (float)ClientSettings.FieldOfView / 90f / 25f, 0.025f, 0.1f);
		Reset3DProjection();
	}

	public void Start()
	{
		Compression.Reset();
		Platform.ResetGamePauseAndUptimeState();
		disconnectAction = null;
		disconnectMissingMods = null;
		quadModel = Platform.UploadMesh(QuadMeshUtilExt.GetQuadModelData());
		FrustumCulling frustumculling = new FrustumCulling();
		frustumCuller = frustumculling;
		frustumCuller.UpdateViewDistance(ClientSettings.ViewDistance);
		ChunkTesselator terrainchunktesselator = new ChunkTesselator(this);
		TerrainChunkTesselator = terrainchunktesselator;
		Platform.AddOnCrash(OnCrashHandlerLeave.Create(this));
		rand = new ThreadLocal<Random>(() => new Random(Environment.TickCount));
		macroManager = new MacroManager(this);
		Platform.ShaderUniforms = shUniforms;
		_clientThreadsCts = new CancellationTokenSource();
		ClientSystem compresschunks = new SystemCompressChunks(this);
		Thread thread = new Thread(new ClientThread(this, "compresschunks", new ClientSystem[1] { compresschunks }, _clientThreadsCts.Token).Process);
		thread.IsBackground = true;
		thread.Start();
		thread.Name = "compresschunks";
		clientThreads.Add(thread);
		ClientSystem blockTicking = new SystemClientTickingBlocks(this);
		Thread thread3 = new Thread(new ClientThread(this, "blockticking", new ClientSystem[1] { blockTicking }, _clientThreadsCts.Token).Process);
		thread3.IsBackground = true;
		thread3.Start();
		thread3.Name = "blockticking";
		clientThreads.Add(thread3);
		ClientSystem relight = new ClientSystemRelight(this);
		Thread thread4 = new Thread(new ClientThread(this, "relight", new ClientSystem[1] { relight }, _clientThreadsCts.Token).Process);
		thread4.IsBackground = true;
		thread4.Start();
		thread4.Name = "relight";
		clientThreads.Add(thread4);
		ClientSystem tesselateterrain = new ChunkTesselatorManager(this);
		Thread thread2 = new Thread(new ClientThread(this, "tesselateterrain", new ClientSystem[1] { tesselateterrain }, _clientThreadsCts.Token).Process);
		thread2.IsBackground = true;
		thread2.Start();
		thread2.Name = "tesselateterrain";
		clientThreads.Add(thread2);
		ClientSystem chunkvis = new SystemChunkVisibilityCalc(this);
		Thread thread5 = new Thread(new ClientThread(this, "chunkvis", new ClientSystem[1] { chunkvis }, _clientThreadsCts.Token).Process);
		thread5.IsBackground = true;
		thread5.Start();
		thread5.Name = "chunkvis";
		clientThreads.Add(thread5);
		networkProc = new SystemNetworkProcess(this);
		Thread thread6 = new Thread(new ClientThread(this, "networkproc", new ClientSystem[1] { networkProc }, _clientThreadsCts.Token).Process);
		thread6.IsBackground = true;
		thread6.Start();
		thread6.Name = "networkproc";
		clientThreads.Add(thread6);
		ClientSystem rendTerra = new SystemRenderTerrain(this);
		Thread thread7 = new Thread(new ClientThread(this, "chunkculling", new ClientSystem[1] { rendTerra }, _clientThreadsCts.Token).Process);
		thread7.IsBackground = true;
		thread7.Start();
		thread7.Name = "chunkculling";
		clientThreads.Add(thread7);
		ClientSystem particleSimulation = new SystemRenderParticles(this);
		Thread thread8 = new Thread(new ClientThread(this, "asyncparticles", new ClientSystem[1] { particleSimulation }, _clientThreadsCts.Token).Process);
		thread8.IsBackground = true;
		thread8.Start();
		thread8.Name = "asyncparticles";
		clientThreads.Add(thread8);
		if (PhysicsBehaviorBase.collisionTester == null)
		{
			PhysicsBehaviorBase.collisionTester = new CachingCollisionTester();
		}
		new GeneralPacketHandler(this);
		new ClientSystemDebugCommands(this);
		new ClientSystemEntities(this);
		new SystemClientCommands(this);
		modHandler = new SystemModHandler(this);
		MusicEngineCts = new CancellationTokenSource();
		clientSystems = new ClientSystem[39]
		{
			new SystemSoundEngine(this),
			modHandler,
			new ClientSystemIntroMenu(this),
			new SystemHotkeys(this),
			api.guiapi.guimgr = new GuiManager(this),
			MusicEngine = new SystemMusicEngine(this, MusicEngineCts.Token),
			networkProc,
			compresschunks,
			tesselateterrain,
			relight,
			chunkvis,
			new SystemRenderFrameBufferDebug(this),
			new SystemUnloadChunks(this),
			new SystemPlayerSounds(this),
			new SystemRenderSkyColor(this),
			new SystemRenderNightSky(this),
			new SystemRenderSunMoon(this),
			new SystemCinematicCamera(this),
			new SystemRenderShadowMap(this),
			new SystemRenderDebugWireframes(this),
			rendTerra,
			new SystemRenderRiftTest(this),
			new SystemRenderEntities(this),
			new SystemRenderDecals(this),
			particleSimulation,
			new SystemRenderPlayerEffects(this),
			new SystemRenderInsideBlock(this),
			new SystemHighlightBlocks(this),
			new SystemSelectedBlockOutline(this),
			new SystemMouseInWorldInteractions(this),
			new SystemPlayerControl(this),
			new SystemRenderAim(this),
			new SystemRenderPlayerAimAcc(this),
			new SystemScreenshot(this),
			new SystemPlayerEnvAwarenessTracker(this),
			new SystemCalendar(this),
			new SystemVideoRecorder(this),
			blockTicking,
			csStartup
		};
		ScreenManager.Platform.CheckGlError("init end");
		LastReceivedMilliseconds = Platform.EllapsedMs;
		Platform.GlDepthMask(flag: true);
		Platform.GlEnableDepthTest();
		Platform.GlCullFaceBack();
		Platform.GlEnableCullFace();
		ScreenManager.Platform.CheckGlError();
		Reset3DProjection();
		ScreenManager.Platform.CheckGlError();
	}

	public void OnOwnPlayerDataReceived()
	{
		ScreenRunningGame.BubbleUpEvent("maploaded");
		EntityPlayer.PhysicsUpdateWatcher = MainCamera.OnPlayerPhysicsTick;
		EntityPlayer.SetCurrentlyControlledPlayer();
		for (int i = 0; i < clientSystems.Length; i++)
		{
			clientSystems[i].OnOwnPlayerDataReceived();
		}
	}

	public void MainGameLoop(float deltaTime)
	{
		if (disposed)
		{
			return;
		}
		if (KillNextFrame)
		{
			SendLeave(0);
			exitReason = "client thread crash";
			DestroyGameSession(gotDisconnected: false);
			KillNextFrame = false;
			return;
		}
		if (DeltaTimeLimiter > 0f)
		{
			deltaTime = DeltaTimeLimiter;
		}
		MainRenderLoop(deltaTime);
		ExecuteMainThreadTasks(deltaTime);
	}

	public void ExecuteMainThreadTasks(float deltaTime)
	{
		ScreenManager.FrameProfiler.Mark("beginMTT");
		if (GameLaunchTasks.Count > 0)
		{
			GameLaunchTasks.Dequeue().Action();
		}
		else
		{
			if (SuspendMainThreadTasks)
			{
				return;
			}
			lock (MainThreadTasksLock)
			{
				while (MainThreadTasks.Count > 0)
				{
					reversedQueue.Enqueue(MainThreadTasks.Dequeue());
				}
			}
			while (reversedQueue.Count > 0)
			{
				ClientTask task = reversedQueue.Dequeue();
				task.Action();
				if (extendedDebugInfo)
				{
					ScreenManager.FrameProfiler.Mark(task.Code);
				}
				if (SuspendMainThreadTasks && reversedQueue.Count > 0)
				{
					requeueTasks();
				}
			}
			ScreenManager.FrameProfiler.Mark("doneMTT");
		}
	}

	private void requeueTasks()
	{
		lock (MainThreadTasksLock)
		{
			while (MainThreadTasks.Count > 0)
			{
				holdingQueue.Enqueue(MainThreadTasks.Dequeue());
			}
			while (reversedQueue.Count > 0)
			{
				MainThreadTasks.Enqueue(reversedQueue.Dequeue());
			}
			while (holdingQueue.Count > 0)
			{
				MainThreadTasks.Enqueue(holdingQueue.Dequeue());
			}
		}
	}

	public void TriggerRenderStage(EnumRenderStage stage, float dt)
	{
		ScreenManager.FrameProfiler.Mark("beginrenderstage-" + stage);
		currentRenderStage = stage;
		eventManager?.TriggerRenderStage(stage, dt);
		Platform.CheckGlError("After render stage " + stage);
	}

	public void MainRenderLoop(float dt)
	{
		ScreenManager.FrameProfiler.Mark("mrl");
		if ((timelapsedCurrent += timelapse) > timelapseEnd)
		{
			timelapse = 0f;
			timelapsedCurrent = 0f;
			timelapseEnd = float.MaxValue;
			ShouldRender2DOverlays = true;
		}
		GameWorldCalendar.Timelapse = timelapsedCurrent;
		UpdateResize();
		UpdateFreeMouse();
		UpdateCameraYawPitch(dt);
		if (EntityPlayer?.Pos != null)
		{
			shUniforms.FlagFogDensity = AmbientManager.BlendedFlatFogDensity;
			shUniforms.FlatFogStartYPos = AmbientManager.BlendedFlatFogYPosForShader;
		}
		if (!IsPaused)
		{
			eventManager?.TriggerGameTick(InWorldEllapsedMs, this);
		}
		ScreenManager.FrameProfiler.Mark("gametick");
		if (LagSimulation)
		{
			Platform.ThreadSpinWait(10000000);
		}
		shUniforms.Update(dt, api);
		shUniforms.ZNear = MainCamera.ZNear;
		shUniforms.ZFar = MainCamera.ZFar;
		TriggerRenderStage(EnumRenderStage.Before, dt);
		Platform.GlEnableDepthTest();
		Platform.GlDepthMask(flag: true);
		if (AmbientManager.ShadowQuality > 0 && (double)AmbientManager.DropShadowIntensity > 0.01)
		{
			TriggerRenderStage(EnumRenderStage.ShadowFar, dt);
			TriggerRenderStage(EnumRenderStage.ShadowFarDone, dt);
			if (AmbientManager.ShadowQuality > 1)
			{
				TriggerRenderStage(EnumRenderStage.ShadowNear, dt);
				TriggerRenderStage(EnumRenderStage.ShadowNearDone, dt);
			}
		}
		GlMatrixModeModelView();
		GlLoadMatrix(MainCamera.CameraMatrix);
		double[] pmat = api.Render.PMatrix.Top;
		double[] mvmat = api.Render.MvMatrix.Top;
		for (int i = 0; i < 16; i++)
		{
			PerspectiveProjectionMat[i] = pmat[i];
			PerspectiveViewMat[i] = mvmat[i];
		}
		frustumCuller.CalcFrustumEquations(player.Entity.Pos.AsBlockPos, pmat, mvmat);
		float lod0Bias = (float)Math.Min(640, ClientSettings.ViewDistance) * ClientSettings.LodBias;
		float lod2Bias = (float)Math.Min(640, ClientSettings.ViewDistance) * ClientSettings.LodBiasFar;
		if (ClientSettings.ViewDistance <= 64)
		{
			lod2Bias = ClientSettings.ViewDistance;
		}
		frustumCuller.lod0BiasSq = lod0Bias * lod0Bias;
		frustumCuller.lod2BiasSq = lod2Bias * lod2Bias;
		TriggerRenderStage(EnumRenderStage.Opaque, dt);
		if (doTransparentRenderPass)
		{
			ScreenManager.FrameProfiler.Mark("rendTransp-begin");
			Platform.LoadFrameBuffer(EnumFrameBuffer.Transparent);
			ScreenManager.FrameProfiler.Mark("rendTransp-fbloaded");
			Platform.ClearFrameBuffer(EnumFrameBuffer.Transparent);
			ScreenManager.FrameProfiler.Mark("rendTransp-bufscleared");
			TriggerRenderStage(EnumRenderStage.OIT, dt);
			Platform.UnloadFrameBuffer(EnumFrameBuffer.Transparent);
			ScreenManager.FrameProfiler.Mark("rendTranspDone");
			Platform.MergeTransparentRenderPass();
			ScreenManager.FrameProfiler.Mark("mergeTranspPassDone");
		}
		Platform.GlDepthMask(flag: true);
		Platform.GlEnableDepthTest();
		Platform.GlCullFaceBack();
		Platform.GlEnableCullFace();
		TriggerRenderStage(EnumRenderStage.AfterOIT, dt);
	}

	public void RenderAfterPostProcessing(float dt)
	{
		if (DeltaTimeLimiter > 0f)
		{
			dt = DeltaTimeLimiter;
		}
		TriggerRenderStage(EnumRenderStage.AfterPostProcessing, dt);
	}

	public void RenderAfterFinalComposition(float dt)
	{
		if (DeltaTimeLimiter > 0f)
		{
			dt = DeltaTimeLimiter;
		}
		TriggerRenderStage(EnumRenderStage.AfterFinalComposition, dt);
		Platform.CheckGlErrorAlways("after final compo ");
	}

	public void RenderAfterBlit(float dt)
	{
		if (DeltaTimeLimiter > 0f)
		{
			dt = DeltaTimeLimiter;
		}
		TriggerRenderStage(EnumRenderStage.AfterBlit, dt);
		Platform.CheckGlErrorAlways("after blit");
	}

	public void RenderToDefaultFramebuffer(float dt)
	{
		if (DeltaTimeLimiter > 0f)
		{
			dt = DeltaTimeLimiter;
		}
		if (ShouldRender2DOverlays)
		{
			guiShaderProg = ShaderPrograms.Gui;
			guiShaderProg.Use();
			OrthoMode(Platform.WindowSize.Width, Platform.WindowSize.Height);
			TriggerRenderStage(EnumRenderStage.Ortho, dt);
			guiShaderProg.Stop();
		}
		ScreenManager.FrameProfiler.Mark("rendOrthoDone");
		PerspectiveMode();
		Platform.GlDepthFunc(EnumDepthFunction.Less);
		TriggerRenderStage(EnumRenderStage.Done, dt);
		ScreenManager.FrameProfiler.Mark("finfr");
		tickSummary = ScreenManager.FrameProfiler.summary;
	}

	public void Render2DBitmapFile(AssetLocation filename, float x, float y, float w, float h)
	{
		if (tmpTex == null)
		{
			tmpTex = new LoadedTexture(api);
		}
		GetOrLoadCachedTexture(filename, ref tmpTex);
		Render2DTexture(tmpTex.TextureId, x, y, w, h);
	}

	public void Render2DLoadedTexture(LoadedTexture texture, float posX, float posY, float z = 50f, Vec4f color = null)
	{
		Render2DTexture(texture.TextureId, posX, posY, texture.Width, texture.Height, z, color);
	}

	public void RenderTextureIntoTexture(LoadedTexture fromTexture, LoadedTexture intoTexture, float x1, float y1)
	{
		RenderTextureIntoTexture(fromTexture, 0f, 0f, fromTexture.Width, fromTexture.Height, intoTexture, x1, y1);
	}

	public void RenderTextureIntoTexture(LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, LoadedTexture intoTexture, float targetX, float targetY, float alphaTest = 0.005f)
	{
		FrameBufferRef fb = Platform.CreateFramebuffer(new FramebufferAttrs("Render2DLoadedTexture", intoTexture.Width, intoTexture.Height)
		{
			Attachments = new FramebufferAttrsAttachment[1]
			{
				new FramebufferAttrsAttachment
				{
					AttachmentType = EnumFramebufferAttachment.ColorAttachment0,
					Texture = new RawTexture
					{
						Width = intoTexture.Width,
						Height = intoTexture.Height,
						TextureId = intoTexture.TextureId
					}
				}
			}
		});
		RenderTextureIntoFrameBuffer(0, fromTexture, sourceX, sourceY, sourceWidth, sourceHeight, fb, targetX, targetY, alphaTest);
		Platform.DisposeFrameBuffer(fb, disposeTextures: false);
	}

	public void RenderTextureIntoFrameBuffer(int atlasTextureId, LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, FrameBufferRef fb, float targetX, float targetY, float alphaTest = 0.005f)
	{
		if (!disposed)
		{
			ShaderProgramBase oldprog = ShaderProgramBase.CurrentShaderProgram;
			oldprog?.Stop();
			ShaderProgramTexture2texture texture2texture = ShaderPrograms.Texture2texture;
			if (atlasTextureId == 0)
			{
				Platform.LoadFrameBuffer(fb);
			}
			else
			{
				Platform.LoadFrameBuffer(fb, atlasTextureId);
			}
			Platform.GlDisableDepthTest();
			Platform.GlToggleBlend(alphaTest >= 0f);
			texture2texture.Use();
			texture2texture.Tex2d2D = fromTexture.TextureId;
			texture2texture.Texu = sourceX / (float)fromTexture.Width;
			texture2texture.Texv = sourceY / (float)fromTexture.Height;
			texture2texture.Texw = sourceWidth / (float)fromTexture.Width;
			texture2texture.Texh = sourceHeight / (float)fromTexture.Height;
			texture2texture.AlphaTest = alphaTest;
			texture2texture.Xs = targetX / (float)fb.Width;
			texture2texture.Ys = targetY / (float)fb.Height;
			texture2texture.Width = sourceWidth / (float)fb.Width;
			texture2texture.Height = sourceHeight / (float)fb.Height;
			Platform.RenderMesh(quadModel);
			Platform.GlEnableDepthTest();
			Platform.LoadFrameBuffer((oldprog?.PassName == "gui") ? EnumFrameBuffer.Default : EnumFrameBuffer.Primary);
			texture2texture.Stop();
			oldprog?.Use();
		}
	}

	public void Render2DTexture(int textureid, float x1, float y1, float width, float height, float z = 10f, Vec4f color = null)
	{
		Render2DTexture(quadModel, textureid, x1, y1, width, height, z, color);
	}

	public void Render2DTexture(MeshRef quadModel, int textureid, float x1, float y1, float width, float height, float z = 10f, Vec4f color = null)
	{
		guiShaderProg.RgbaIn = ((color == null) ? ColorUtil.WhiteArgbVec : color);
		guiShaderProg.ExtraGlow = 0;
		guiShaderProg.ApplyColor = 0;
		guiShaderProg.NoTexture = 0f;
		guiShaderProg.Tex2d2D = textureid;
		guiShaderProg.OverlayOpacity = 0f;
		guiShaderProg.NormalShaded = 0;
		GlPushMatrix();
		GlTranslate(x1, y1, z);
		GlScale(width, height, 0.0);
		GlScale(0.5, 0.5, 0.0);
		GlTranslate(1.0, 1.0, 0.0);
		guiShaderProg.ProjectionMatrix = CurrentProjectionMatrix;
		guiShaderProg.ModelViewMatrix = CurrentModelViewMatrix;
		Platform.RenderMesh(quadModel);
		GlPopMatrix();
	}

	public void Render2DTexture(MultiTextureMeshRef meshRef, float x1, float y1, float width, float height, float z = 10f, Vec4f color = null)
	{
		guiShaderProg.RgbaIn = ((color == null) ? ColorUtil.WhiteArgbVec : color);
		guiShaderProg.ExtraGlow = 0;
		guiShaderProg.ApplyColor = 0;
		guiShaderProg.NoTexture = 0f;
		guiShaderProg.OverlayOpacity = 0f;
		guiShaderProg.NormalShaded = 0;
		GlPushMatrix();
		GlTranslate(x1, y1, z);
		GlScale(width, height, 0.0);
		GlScale(0.5, 0.5, 0.0);
		GlTranslate(1.0, 1.0, 0.0);
		guiShaderProg.ProjectionMatrix = CurrentProjectionMatrix;
		guiShaderProg.ModelViewMatrix = CurrentModelViewMatrix;
		for (int i = 0; i < meshRef.meshrefs.Length; i++)
		{
			MeshRef j = meshRef.meshrefs[i];
			guiShaderProg.BindTexture2D("tex2d", meshRef.textureids[i], 0);
			Platform.RenderMesh(j);
		}
		GlPopMatrix();
	}

	public void Render2DTexture(int textureid, ModelTransform transform, Vec4f color = null)
	{
		guiShaderProg.RgbaIn = ((color == null) ? ColorUtil.WhiteArgbVec : color);
		guiShaderProg.ExtraGlow = 0;
		guiShaderProg.ApplyColor = 0;
		guiShaderProg.NoTexture = 0f;
		guiShaderProg.Tex2d2D = textureid;
		guiShaderProg.OverlayOpacity = 0f;
		guiShaderProg.NormalShaded = 0;
		GlPushMatrix();
		GlTranslate(transform.Translation.X, transform.Translation.Y, transform.Translation.Z);
		GlRotate(transform.Rotation.X, 1.0, 0.0, 0.0);
		GlRotate(transform.Rotation.Y, 0.0, 1.0, 0.0);
		GlRotate(transform.Rotation.Z, 0.0, 0.0, 1.0);
		GlScale(transform.ScaleXYZ.X, transform.ScaleXYZ.Y, 0.0);
		GlScale(0.5, 0.5, 0.0);
		GlTranslate(1.0, 1.0, 0.0);
		guiShaderProg.ProjectionMatrix = CurrentProjectionMatrix;
		guiShaderProg.ModelViewMatrix = CurrentModelViewMatrix;
		Platform.RenderMesh(quadModel);
		GlPopMatrix();
	}

	public void Render2DTextureFlipped(int textureid, float x1, float y1, float width, float height, float z = 10f, Vec4f color = null)
	{
		guiShaderProg.RgbaIn = ((color == null) ? ColorUtil.WhiteArgbVec : color);
		guiShaderProg.ExtraGlow = 0;
		guiShaderProg.ApplyColor = 0;
		guiShaderProg.NoTexture = 0f;
		guiShaderProg.Tex2d2D = textureid;
		guiShaderProg.NormalShaded = 0;
		GlPushMatrix();
		GlTranslate(x1, y1, z);
		GlScale(width, height, 0.0);
		GlScale(0.5, 0.5, 0.0);
		GlTranslate(1.0, 1.0, 0.0);
		GlRotate(180f, 1.0, 0.0, 0.0);
		guiShaderProg.ProjectionMatrix = CurrentProjectionMatrix;
		guiShaderProg.ModelViewMatrix = CurrentModelViewMatrix;
		Platform.RenderMesh(quadModel);
		GlPopMatrix();
	}

	public void Set3DProjection(float zfar, float fov)
	{
		float aspectRatio = (float)Platform.WindowSize.Width / (float)Platform.WindowSize.Height;
		Mat4d.Perspective(set3DProjectionTempMat4, fov, aspectRatio, MainCamera.ZNear, zfar);
		GlMatrixModeProjection();
		GlLoadMatrix(set3DProjectionTempMat4);
		shUniforms.ZNear = MainCamera.ZNear;
		shUniforms.ZFar = MainCamera.ZFar;
		GlMatrixModeModelView();
	}

	public void Reset3DProjection()
	{
		Set3DProjection(MainCamera.ZFar, MainCamera.Fov);
	}

	public void Set3DProjection(float zfar)
	{
		Set3DProjection(zfar, MainCamera.Fov);
	}

	public void GlMatrixModeModelView()
	{
		CurrentMatrixModeProjection = false;
	}

	public void GlMatrixModeProjection()
	{
		CurrentMatrixModeProjection = true;
	}

	public void GlLoadMatrix(double[] m)
	{
		if (CurrentMatrixModeProjection)
		{
			if (PMatrix.Count > 0)
			{
				PMatrix.Pop();
			}
			PMatrix.Push(m);
		}
		else
		{
			if (MvMatrix.Count > 0)
			{
				MvMatrix.Pop();
			}
			MvMatrix.Push(m);
		}
	}

	public void GlPopMatrix()
	{
		if (CurrentMatrixModeProjection)
		{
			if (PMatrix.Count > 1)
			{
				PMatrix.Pop();
			}
		}
		else if (MvMatrix.Count > 1)
		{
			MvMatrix.Pop();
		}
	}

	public void GlScale(double x, double y, double z)
	{
		double[] i = ((!CurrentMatrixModeProjection) ? MvMatrix.Top : PMatrix.Top);
		Vec3Utilsd.Set(glScaleTempVec3, x, y, z);
		Mat4d.Scale(i, i, glScaleTempVec3);
	}

	public void GlRotate(float angle, double x, double y, double z)
	{
		angle /= 360f;
		angle *= (float)Math.PI * 2f;
		double[] i = ((!CurrentMatrixModeProjection) ? MvMatrix.Top : PMatrix.Top);
		Vec3Utilsd.Set(glRotateTempVec3, x, y, z);
		Mat4d.Rotate(i, i, angle, glRotateTempVec3);
	}

	public void GlTranslate(Vec3d vec)
	{
		GlTranslate((float)vec.X, (float)vec.Y, (float)vec.Z);
	}

	public void GlTranslate(double x, double y, double z)
	{
		double[] i = ((!CurrentMatrixModeProjection) ? MvMatrix.Top : PMatrix.Top);
		Mat4d.Translate(i, i, new double[3] { x, y, z });
	}

	public void GlPushMatrix()
	{
		if (CurrentMatrixModeProjection)
		{
			PMatrix.Push(PMatrix.Top);
		}
		else
		{
			MvMatrix.Push(MvMatrix.Top);
		}
	}

	public void GlLoadIdentity()
	{
		if (CurrentMatrixModeProjection)
		{
			if (PMatrix.Count > 0)
			{
				PMatrix.Pop();
			}
			PMatrix.Push(identityMatrix);
		}
		else
		{
			if (MvMatrix.Count > 0)
			{
				MvMatrix.Pop();
			}
			MvMatrix.Push(identityMatrix);
		}
	}

	public void GlOrtho(double left, double right, double bottom, double top, double zNear, double zFar)
	{
		if (CurrentMatrixModeProjection)
		{
			Mat4d.Ortho(PMatrix.Top, left, right, bottom, top, zNear, zFar);
			return;
		}
		throw new Exception("Invalid call. CurrentMatrixModeProjection is false");
	}

	public void OrthoMode(int width, int height, bool inverseY = false)
	{
		GlMatrixModeProjection();
		GlPushMatrix();
		GlLoadIdentity();
		if (inverseY)
		{
			GlOrtho(0.0, width, 0.0, height, 0.4000000059604645, 20001.0);
		}
		else
		{
			GlOrtho(0.0, width, height, 0.0, 0.4000000059604645, 20001.0);
		}
		GlMatrixModeModelView();
		GlPushMatrix();
		GlLoadIdentity();
		GL.DepthRange(0f, 20000f);
		GlTranslate(0.0, 0.0, -19849.0);
	}

	public void PerspectiveMode()
	{
		GlMatrixModeProjection();
		GlPopMatrix();
		GlMatrixModeModelView();
		GlPopMatrix();
		GL.DepthRange(0f, 1f);
	}

	public void Connect()
	{
		Compression.Reset();
		MainNetClient.Connect(Connectdata.Host, Connectdata.Port, OnConnectionResult, OnDisconnected);
		UdpNetClient.Connect(Connectdata.Host, Connectdata.Port);
		if (UdpNetClient is UdpNetClient udpClient)
		{
			Logger.Notification($"UDP: connected on local endpoint: {udpClient.udpClient.Client.LocalEndPoint}");
		}
		SendPacketClient(new Packet_Client
		{
			Id = 33
		});
	}

	private void OnDisconnected(Exception caughtException)
	{
		Compression.Reset();
		if (exitToDisconnectScreen)
		{
			return;
		}
		MainNetClient.Dispose();
		UdpNetClient.Dispose();
		TyronThreadPool.QueueLongDurationTask(delegate
		{
			Thread.Sleep(1000);
			EnqueueMainThreadTask(delegate
			{
				Compression.Reset();
				if (!exitToMainMenu && !exitToDisconnectScreen)
				{
					disconnectReason = "The connection closed unexpectedly: " + caughtException.Message;
					Logger.Error("The connection closed unexpectedly.");
					Logger.Error(caughtException);
					DestroyGameSession(gotDisconnected: true);
				}
			}, "disconnect");
		});
	}

	private void OnConnectionResult(ConnectionResult result)
	{
		Connectdata.Connected = result.connected;
		Connectdata.ErrorMessage = result.errorMessage;
	}

	public void SendRequestJoin()
	{
		Logger.VerboseDebug("Sending request to join server");
		SendPacketClient(ClientPackets.RequestJoin());
	}

	public void SendLeave(int reason)
	{
		SendPacketClient(ClientPackets.Leave(reason));
	}

	public byte[] Serialize(Packet_Client packet)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Packet_ClientSerializer.Serialize(citoMemoryStream, packet);
		return citoMemoryStream.ToArray();
	}

	public void SendPacket(byte[] packet)
	{
		if (disposed || MainNetClient == null)
		{
			return;
		}
		try
		{
			MainNetClient.Send(packet);
		}
		catch (ObjectDisposedException e)
		{
			OnDisconnected(e);
		}
	}

	public void SendPacketClient(Packet_Client packetClient)
	{
		if (packetClient != null)
		{
			byte[] packet = Serialize(packetClient);
			SendPacket(packet);
		}
	}

	public void SendPingReply()
	{
		SendPacketClient(ClientPackets.PingReply());
	}

	public void Respawn()
	{
		SendPacketClient(ClientPackets.SpecialKeyRespawn());
		EntityPlayer.Pos.Motion.Set(0.0, 0.0, 0.0);
	}

	public void SendHandInteraction(int mouseButton, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, EnumHandInteractNw state, bool firstEvent, EnumItemUseCancelReason cancelReason = EnumItemUseCancelReason.ReleasedMouse)
	{
		if (blockSel == null && entitySel == null)
		{
			SendPacketClient(new Packet_Client
			{
				Id = 25,
				HandInteraction = new Packet_ClientHandInteraction
				{
					SlotId = player.inventoryMgr.ActiveHotbarSlotNumber,
					MouseButton = mouseButton,
					EnumHandInteract = (int)state,
					UsingCount = EntityPlayer.Controls.UsingCount,
					UseType = (int)useType,
					CancelReason = (int)cancelReason,
					FirstEvent = (firstEvent ? 1 : 0)
				}
			});
		}
		else if (blockSel != null)
		{
			SendPacketClient(new Packet_Client
			{
				Id = 25,
				HandInteraction = new Packet_ClientHandInteraction
				{
					SlotId = player.inventoryMgr.ActiveHotbarSlotNumber,
					MouseButton = mouseButton,
					X = blockSel.Position.X,
					Y = blockSel.Position.InternalY,
					Z = blockSel.Position.Z,
					HitX = CollectibleNet.SerializeDoublePrecise(blockSel.HitPosition.X),
					HitY = CollectibleNet.SerializeDoublePrecise(blockSel.HitPosition.Y),
					HitZ = CollectibleNet.SerializeDoublePrecise(blockSel.HitPosition.Z),
					OnBlockFace = blockSel.Face.Index,
					SelectionBoxIndex = blockSel.SelectionBoxIndex,
					EnumHandInteract = (int)state,
					UsingCount = EntityPlayer.Controls.UsingCount,
					UseType = (int)useType,
					CancelReason = (int)cancelReason,
					FirstEvent = (firstEvent ? 1 : 0)
				}
			});
		}
		else
		{
			SendPacketClient(new Packet_Client
			{
				Id = 25,
				HandInteraction = new Packet_ClientHandInteraction
				{
					SlotId = player.inventoryMgr.ActiveHotbarSlotNumber,
					MouseButton = mouseButton,
					HitX = CollectibleNet.SerializeDoublePrecise(entitySel.HitPosition.X),
					HitY = CollectibleNet.SerializeDoublePrecise(entitySel.HitPosition.Y),
					HitZ = CollectibleNet.SerializeDoublePrecise(entitySel.HitPosition.Z),
					OnBlockFace = entitySel.Face.Index,
					OnEntityId = entitySel.Entity.EntityId,
					SelectionBoxIndex = entitySel.SelectionBoxIndex,
					EnumHandInteract = (int)state,
					UsingCount = EntityPlayer.Controls.UsingCount,
					UseType = (int)useType,
					FirstEvent = (firstEvent ? 1 : 0)
				}
			});
		}
	}

	public bool tryAccess(BlockSelection blockSel, EnumBlockAccessFlags flag)
	{
		string claimant;
		EnumWorldAccessResponse resp = WorldMap.TestBlockAccess(player, BlockSelection, flag, out claimant);
		if (resp != 0)
		{
			string code = "noprivilege-" + ((flag == EnumBlockAccessFlags.Use) ? "use" : "buildbreak") + "-" + resp.ToString().ToLowerInvariant();
			if (claimant == null)
			{
				claimant = "?";
			}
			else if (claimant.StartsWithOrdinal("custommessage-"))
			{
				code = "noprivilege-buildbreak-" + claimant.Substring("custommessage-".Length);
			}
			api.TriggerIngameError(this, code, Lang.Get("ingameerror-" + code, claimant));
			return false;
		}
		return true;
	}

	public bool OnPlayerTryPlace(BlockSelection blockSelection, ref string failureCode)
	{
		if (!WorldMap.IsValidPos(blockSelection.Position))
		{
			failureCode = "outsideworld";
			return false;
		}
		if (!tryAccess(blockSelection, EnumBlockAccessFlags.BuildOrBreak))
		{
			return false;
		}
		ItemStack itemstack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
		if (itemstack != null && itemstack.Class == EnumItemClass.Block)
		{
			Block oldBlock = World.BlockAccessor.GetBlock(blockSelection.Position);
			Block liqBlock = World.BlockAccessor.GetBlock(blockSelection.Position, 2);
			if (preventPlacementInLava && liqBlock.LiquidCode == "lava" && player.worlddata.CurrentGameMode != EnumGameMode.Creative)
			{
				failureCode = "toohottoplacehere";
				return false;
			}
			failureCode = null;
			if (itemstack.Block.TryPlaceBlock(this, player, itemstack, blockSelection, ref failureCode))
			{
				SendPacketClient(ClientPackets.BlockInteraction(blockSelection, 1, 0));
				eventManager?.TriggerBlockChanged(this, blockSelection.Position, oldBlock);
				TriggerNeighbourBlocksUpdate(BlockSelection.Position);
				return true;
			}
			if (failureCode == null)
			{
				failureCode = "generic";
			}
		}
		return false;
	}

	public void OnPlayerTryDestroyBlock(BlockSelection blockSelection)
	{
		if (WorldMap.IsValidPos(blockSelection.Position) && tryAccess(blockSelection, EnumBlockAccessFlags.BuildOrBreak))
		{
			ItemSlot hotbarslot = player.InventoryManager.ActiveHotbarSlot;
			IItemStack stack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
			bool ok = true;
			Block oldBlock = blockSelection.Block ?? World.BlockAccessor.GetBlock(blockSelection.Position);
			if (stack != null)
			{
				ok = stack.Collectible.OnBlockBrokenWith(this, Player.Entity, hotbarslot, blockSelection);
			}
			else
			{
				oldBlock.OnBlockBroken(World, blockSelection.Position, Player);
			}
			if (ok)
			{
				eventManager?.TriggerBlockChanged(this, blockSelection.Position, oldBlock);
				TriggerNeighbourBlocksUpdate(blockSelection.Position);
				SendPacketClient(ClientPackets.BlockInteraction(blockSelection, 0, 0));
			}
		}
	}

	public void OnKeyDown(KeyEvent args)
	{
		if (disposed)
		{
			return;
		}
		api.eventapi.TriggerKeyDown(args);
		if (args.Handled)
		{
			return;
		}
		int eKey = args.KeyCode;
		KeyboardStateRaw[eKey] = true;
		if (args.Handled = ScreenManager.hotkeyManager.TriggerGlobalHotKey(args, this, player, keyUp: false))
		{
			return;
		}
		ClientSystem[] array = clientSystems;
		foreach (ClientSystem system in array)
		{
			if (system.CaptureAllInputs())
			{
				system.OnKeyDown(args);
				return;
			}
		}
		array = clientSystems;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnKeyDown(args);
			if (args.Handled)
			{
				return;
			}
		}
		PreviousKeyboardState[eKey] = KeyboardState[eKey];
		KeyboardState[eKey] = true;
		bool handled = ScreenManager.hotkeyManager.TriggerHotKey(args, this, player, AllowCharacterControl, keyUp: false);
		args.Handled = handled;
	}

	public void OnKeyUp(KeyEvent args)
	{
		if (disposed)
		{
			return;
		}
		int eKey = args.KeyCode;
		KeyboardStateRaw[eKey] = false;
		PreviousKeyboardState[eKey] = KeyboardState[eKey];
		KeyboardState[eKey] = false;
		api.eventapi.TriggerKeyUp(args);
		if (args.Handled || (args.Handled = ScreenManager.hotkeyManager.TriggerGlobalHotKey(args, this, player, keyUp: true)))
		{
			return;
		}
		ClientSystem[] array = clientSystems;
		foreach (ClientSystem system in array)
		{
			if (system.CaptureAllInputs())
			{
				system.OnKeyUp(args);
				return;
			}
		}
		array = clientSystems;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnKeyUp(args);
			if (args.Handled)
			{
				return;
			}
		}
		args.Handled = ScreenManager.hotkeyManager.TriggerHotKey(args, this, player, AllowCharacterControl, keyUp: true);
	}

	public void OnKeyPress(KeyEvent eventArgs)
	{
		if (disposed)
		{
			return;
		}
		ClientSystem[] array = clientSystems;
		foreach (ClientSystem system in array)
		{
			if (system.CaptureAllInputs())
			{
				system.OnKeyPress(eventArgs);
				return;
			}
		}
		array = clientSystems;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnKeyPress(eventArgs);
			if (eventArgs.Handled)
			{
				break;
			}
		}
	}

	public void OnMouseDownRaw(MouseEvent args)
	{
		if (disposed)
		{
			return;
		}
		UpdateMouseButtonState(args.Button, MouseStateRaw, value: true);
		ClientSystem[] array = clientSystems;
		foreach (ClientSystem system in array)
		{
			if (system.CaptureRawMouse())
			{
				system.OnMouseDown(args);
				return;
			}
		}
		int eKey = (int)(args.Button + 240);
		PreviousKeyboardState[eKey] = KeyboardState[eKey];
		KeyboardState[eKey] = true;
		ScreenManager.hotkeyManager.OnMouseButton(this, args.Button, args.Modifiers, buttonDown: true);
	}

	private void UpdateMouseButtonState(EnumMouseButton button, MouseButtonState mouseState, bool value)
	{
		if (button == EnumMouseButton.Left)
		{
			mouseState.Left = value;
		}
		if (button == EnumMouseButton.Middle)
		{
			mouseState.Middle = value;
		}
		if (button == EnumMouseButton.Right)
		{
			mouseState.Right = value;
		}
	}

	public bool UpdateMouseButtonState(EnumMouseButton button, bool down)
	{
		MouseEvent args = Platform.CreateMouseEvent(button);
		if (down)
		{
			api.eventapi.TriggerMouseDown(args);
			if (args.Handled)
			{
				return true;
			}
			ClientSystem[] array = clientSystems;
			foreach (ClientSystem system in array)
			{
				if (system.CaptureAllInputs())
				{
					system.OnMouseDown(args);
					return true;
				}
			}
			array = clientSystems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnMouseDown(args);
				if (args.Handled)
				{
					return true;
				}
			}
			UpdateMouseButtonState(button, InWorldMouseState, value: true);
		}
		else
		{
			api.eventapi.TriggerMouseUp(args);
			if (args.Handled)
			{
				return true;
			}
			UpdateMouseButtonState(button, InWorldMouseState, value: false);
			ClientSystem[] array = clientSystems;
			foreach (ClientSystem system2 in array)
			{
				if (system2.CaptureAllInputs())
				{
					system2.OnMouseUp(args);
					return true;
				}
			}
			array = clientSystems;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnMouseUp(args);
				if (args.Handled)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void OnMouseUpRaw(MouseEvent args)
	{
		int eKey = (int)(args.Button + 240);
		PreviousKeyboardState[eKey] = KeyboardState[eKey];
		KeyboardState[eKey] = false;
		UpdateMouseButtonState(args.Button, MouseStateRaw, value: false);
		ClientSystem[] array = clientSystems;
		foreach (ClientSystem system in array)
		{
			if (system.CaptureRawMouse())
			{
				system.OnMouseUp(args);
				return;
			}
		}
		ScreenManager.hotkeyManager.OnMouseButton(this, args.Button, args.Modifiers, buttonDown: false);
	}

	public void OnMouseWheel(Vintagestory.API.Client.MouseWheelEventArgs args)
	{
		_ = args.deltaPrecise;
		ClientSystem[] array = clientSystems;
		foreach (ClientSystem system in array)
		{
			if (system.CaptureAllInputs())
			{
				system.OnMouseWheel(args);
				return;
			}
		}
		array = clientSystems;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnMouseWheel(args);
			if (args.IsHandled)
			{
				break;
			}
		}
	}

	public void OnMouseMove(MouseEvent args)
	{
		api.eventapi.TriggerMouseMove(args);
		if (args.Handled)
		{
			return;
		}
		MouseCurrentX = (MouseGrabbed ? (Width / 2) : args.X);
		MouseCurrentY = (MouseGrabbed ? (Height / 2) : args.Y);
		MouseDeltaX += (double)(args.DeltaX * ClientSettings.MouseSensivity) / 100.0;
		MouseDeltaY += (double)(args.DeltaY * ClientSettings.MouseSensivity) / 100.0;
		ClientSystem[] array = clientSystems;
		foreach (ClientSystem system in array)
		{
			if (system.CaptureAllInputs())
			{
				system.OnMouseMove(args);
				return;
			}
		}
		array = clientSystems;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnMouseMove(args);
			if (args.Handled)
			{
				break;
			}
		}
	}

	public void UpdateFreeMouse()
	{
		int altKey = ScreenManager.hotkeyManager.HotKeys["togglemousecontrol"].CurrentMapping.KeyCode;
		bool isAltKeyDown = KeyboardState[altKey];
		bool preferUngrabbedMouse = OpenedGuis.Where((GuiDialog gui) => gui.DialogType == EnumDialogType.Dialog).Any((GuiDialog dlg) => dlg.PrefersUngrabbedMouse);
		bool disableMouseGrab = OpenedGuis.Any((GuiDialog gui) => gui.DisableMouseGrab);
		MouseGrabbed = ScreenManager.Platform.IsFocused && !exitToDisconnectScreen && !exitToMainMenu && BlocksReceivedAndLoaded && !disableMouseGrab && ((DialogsOpened == 0 || (ClientSettings.ImmersiveMouseMode && !preferUngrabbedMouse)) ^ isAltKeyDown);
		mouseWorldInteractAnyway = !MouseGrabbed && !preferUngrabbedMouse;
	}

	public void UpdateCameraYawPitch(float dt)
	{
		if (player.worlddata.CurrentGameMode == EnumGameMode.Survival && api.renderapi.ShaderUniforms.GlitchStrength > 0.75f && Platform.GetWindowState() == WindowState.Normal && rand.Value.NextDouble() < 0.01)
		{
			Size2i scsize = Platform.ScreenSize;
			Size2i wdsize = Platform.WindowSize;
			if (wdsize.Width < scsize.Width && wdsize.Height < scsize.Height)
			{
				int maxx = scsize.Width - wdsize.Width;
				int maxy = scsize.Height - wdsize.Height;
				Vector2i vector2I = ((ClientPlatformWindows)Platform).window.ClientSize;
				int x = vector2I.X;
				int y = vector2I.Y;
				if (x > 0 && x < maxx)
				{
					vector2I.X = GameMath.Clamp(x + rand.Value.Next(10) - 5, 0, maxx);
				}
				if (y > 0 && y < maxy)
				{
					vector2I.Y = GameMath.Clamp(y + rand.Value.Next(10) - 5, 0, maxy);
				}
				((ClientPlatformWindows)Platform).window.ClientSize = vector2I;
			}
		}
		double fpsFac = GameMath.Clamp(dt / (1f / 75f), 0f, 3f);
		double fac = GameMath.Clamp((double)((float)ClientSettings.MouseSmoothing / 100f) * fpsFac, 0.009999999776482582, 1.0);
		float mountedSmoothing = 0.5f * (float)fpsFac;
		if (player.Entity.Swimming && player.CameraMode == EnumCameraMode.FirstPerson && swimmingMouseSmoothing > 0f)
		{
			fac = GameMath.Clamp(1f - swimmingMouseSmoothing, 0f, 1f);
		}
		double velX = fac * (MouseDeltaX - DelayedMouseDeltaX);
		double velY = fac * (MouseDeltaY - DelayedMouseDeltaY);
		DelayedMouseDeltaX += velX;
		DelayedMouseDeltaY += velY;
		if (!MouseGrabbed)
		{
			EntityPlayer entityPlayer = EntityPlayer;
			if (entityPlayer != null && (entityPlayer.MountedOn?.AngleMode).GetValueOrDefault() == EnumMountAngleMode.FixateYaw && EntityPlayer.MountedOn.Controls.TriesToMove)
			{
				float mountyaw2 = EntityPlayer.MountedOn.SeatPosition.Yaw;
				EntityPlayer.Pos.Yaw = mountyaw2;
				EntityPlayer.BodyYaw = mountyaw2;
				if (MainCamera.CameraMode == EnumCameraMode.FirstPerson)
				{
					mouseYaw = mountyaw2;
				}
			}
		}
		if (!AllowCameraControl || !Platform.IsFocused || !MouseGrabbed)
		{
			return;
		}
		EnumMountAngleMode angleMode = EnumMountAngleMode.Unaffected;
		float? mountyaw = null;
		IMountableSeat mount = EntityPlayer.MountedOn;
		if (EntityPlayer.MountedOn != null)
		{
			angleMode = mount.AngleMode;
			mountyaw = mount.SeatPosition.Yaw;
		}
		if (player.CameraMode == EnumCameraMode.Overhead)
		{
			float d = GameMath.AngleRadDistance(EntityPlayer.Pos.Yaw, EntityPlayer.WalkYaw) * 0.4f;
			EntityPlayer.Pos.Yaw += d;
			mouseYaw -= (float)(velX * (double)rotationspeed * 1.0 / 75.0);
			mouseYaw = GameMath.Mod(mouseYaw, (float)Math.PI * 2f);
		}
		else
		{
			mouseYaw -= (float)(velX * (double)rotationspeed * 1.0 / 75.0);
			if (EntityPlayer.HeadYawLimits != null)
			{
				AngleConstraint constr = EntityPlayer.HeadYawLimits;
				float range = GameMath.AngleRadDistance(constr.CenterRad, mouseYaw);
				mouseYaw = constr.CenterRad + GameMath.Clamp(range, 0f - constr.RangeRad, constr.RangeRad);
			}
			EntityPlayer.Pos.Yaw = mouseYaw;
		}
		bool handRenderMode = MainCamera.CameraMode == EnumCameraMode.FirstPerson && mount != null;
		if (angleMode == EnumMountAngleMode.PushYaw || angleMode == EnumMountAngleMode.Push || handRenderMode)
		{
			float diff = (0f - mountedSmoothing) * GameMath.AngleRadDistance(mount.SeatPosition.Yaw, prevMountAngles.Y);
			prevMountAngles.Y += diff;
			if (angleMode == EnumMountAngleMode.Push)
			{
				EntityPlayer.Pos.Roll -= GameMath.AngleRadDistance(mount.SeatPosition.Roll, prevMountAngles.X);
				EntityPlayer.Pos.Pitch -= GameMath.AngleRadDistance(mount.SeatPosition.Pitch, prevMountAngles.Z);
			}
			if (player.CameraMode == EnumCameraMode.Overhead)
			{
				EntityPlayer.WalkYaw += diff;
			}
			else
			{
				mouseYaw += diff;
				EntityPlayer.Pos.Yaw += diff;
				EntityPlayer.BodyYaw += diff;
			}
		}
		if ((angleMode == EnumMountAngleMode.Fixate || angleMode == EnumMountAngleMode.FixateYaw) && !handRenderMode)
		{
			EntityPlayer.Pos.Yaw = mountyaw.Value;
		}
		if (angleMode == EnumMountAngleMode.Fixate)
		{
			EntityPlayer.Pos.Pitch = EntityPlayer.MountedOn.SeatPosition.Pitch;
		}
		else
		{
			EntityPlayer.Pos.Pitch += (float)(velY * (double)rotationspeed * 1.0 / 75.0 * (double)((!ClientSettings.InvertMouseYAxis) ? 1 : (-1)));
		}
		if (mount != null)
		{
			prevMountAngles.Set(mount.SeatPosition.Roll, prevMountAngles.Y, mount.SeatPosition.Pitch);
		}
		EntityPlayer.Pos.Pitch = GameMath.Clamp(EntityPlayer.Pos.Pitch, 1.5857964f, 4.697389f);
		EntityPlayer.Pos.Yaw = GameMath.Mod(EntityPlayer.Pos.Yaw, (float)Math.PI * 2f);
		mousePitch = EntityPlayer.Pos.Pitch;
	}

	public void OnFocusChanged(bool focus)
	{
		if (!disposed)
		{
			eventManager?.TriggerGameWindowFocus(focus);
			if (!focus)
			{
				MouseStateRaw.Clear();
				InWorldMouseState.Clear();
			}
		}
	}

	public bool OnFileDrop(string filename)
	{
		return api.eventapi.TriggerFileDrop(filename);
	}

	public Item GetItem(int itemId)
	{
		if (itemId >= Items.Count)
		{
			throw new ArgumentOutOfRangeException($"Cannot get item of id {itemId}, item list count is only until {Items.Count}!");
		}
		return Items[itemId];
	}

	public Block GetBlock(int blockId)
	{
		if (blockId >= Blocks.Count)
		{
			return getOrCreateNoBlock(blockId);
		}
		return Blocks[blockId];
	}

	private Block getOrCreateNoBlock(int id)
	{
		if (!noBlocks.TryGetValue(id, out var block))
		{
			block = (noBlocks[id] = BlockList.getNoBlock(id, api));
		}
		return block;
	}

	public EntityProperties GetEntityType(AssetLocation entityCode)
	{
		EntityClassesByCode.TryGetValue(entityCode, out var eclass);
		return eclass;
	}

	public void ReloadTextures()
	{
		BlockAtlasManager.PauseRegenMipmaps().ReloadTextures();
		ItemAtlasManager.PauseRegenMipmaps().ReloadTextures();
		EntityAtlasManager.PauseRegenMipmaps().ReloadTextures();
		eventManager?.TriggerReloadTextures();
		BlockAtlasManager.ResumeRegenMipmaps();
		ItemAtlasManager.ResumeRegenMipmaps();
		EntityAtlasManager.ResumeRegenMipmaps();
		foreach (Block block in Blocks)
		{
			block.LoadTextureSubIdForBlockColor();
		}
	}

	public int WhiteTexture()
	{
		if (whitetexture == -1)
		{
			BitmapRef bmp = Platform.CreateBitmap(1, 1);
			int[] pixels = new int[1] { ColorUtil.ToRgba(255, 255, 255, 255) };
			Platform.SetBitmapPixelsArgb(bmp, pixels);
			whitetexture = Platform.LoadTexture(bmp);
		}
		return whitetexture;
	}

	public int GetOrLoadCachedTexture(AssetLocation name)
	{
		name = name.WithPathPrefixOnce("textures/");
		if (!texturesByLocation.ContainsKey(name))
		{
			byte[] assetData = Platform.AssetManager.TryGet(name)?.Data;
			if (assetData == null)
			{
				return 0;
			}
			BitmapRef bmp = Platform.CreateBitmapFromPng(assetData, assetData.Length);
			int textureId = Platform.LoadTexture(bmp);
			texturesByLocation[name] = new LoadedTexture(api, textureId, bmp.Width, bmp.Height);
			bmp.Dispose();
			return textureId;
		}
		return texturesByLocation[name].TextureId;
	}

	public void GetOrLoadCachedTexture(AssetLocation name, ref LoadedTexture intoTexture)
	{
		intoTexture.IgnoreUndisposed = true;
		if (!texturesByLocation.ContainsKey(name))
		{
			byte[] assetData = Platform.AssetManager.TryGet(name.Clone().WithPathPrefixOnce("textures/"))?.Data;
			if (assetData != null)
			{
				BitmapRef bmp = Platform.CreateBitmapFromPng(assetData, assetData.Length);
				int textureId = Platform.LoadTexture(bmp);
				if (textureId != intoTexture.TextureId && intoTexture.TextureId != 0)
				{
					intoTexture.Dispose();
				}
				intoTexture.TextureId = textureId;
				intoTexture.Width = bmp.Width;
				intoTexture.Height = bmp.Height;
				texturesByLocation[name] = new LoadedTexture(api, textureId, bmp.Width, bmp.Height);
				bmp.Dispose();
			}
		}
		else
		{
			LoadedTexture cachedTex = texturesByLocation[name];
			if (cachedTex.TextureId != intoTexture.TextureId && intoTexture.TextureId != 0)
			{
				intoTexture.Dispose();
			}
			intoTexture.TextureId = cachedTex.TextureId;
			intoTexture.Width = cachedTex.Width;
			intoTexture.Height = cachedTex.Height;
		}
	}

	public void GetOrLoadCachedTexture(AssetLocation name, BitmapRef bmp, ref LoadedTexture intoTexture)
	{
		if (!texturesByLocation.TryGetValue(name, out var cachedTex))
		{
			cachedTex = (texturesByLocation[name] = new LoadedTexture(api, Platform.LoadTexture(bmp), bmp.Width, bmp.Height));
		}
		if (cachedTex.TextureId != intoTexture.TextureId && intoTexture.TextureId != 0)
		{
			intoTexture.Dispose();
		}
		intoTexture.TextureId = cachedTex.TextureId;
		intoTexture.Width = cachedTex.Width;
		intoTexture.Height = cachedTex.Height;
	}

	public bool DeleteCachedTexture(AssetLocation name)
	{
		if (name == null || !texturesByLocation.ContainsKey(name))
		{
			return false;
		}
		LoadedTexture loadedTexture = texturesByLocation[name];
		texturesByLocation.Remove(name);
		loadedTexture?.Dispose();
		return true;
	}

	public void PauseGame(bool paused)
	{
		if (IsSingleplayer && BlocksReceivedAndLoaded)
		{
			IsPaused = paused;
			Platform.SetGamePausedState(paused);
			api.eventapi.TriggerPauseResume(paused);
			if (paused)
			{
				GameWorldCalendar.watchIngameTime.Stop();
				InWorldStopwatch.Stop();
			}
			else
			{
				GameWorldCalendar.watchIngameTime.Start();
				InWorldStopwatch.Start();
			}
			World.Logger.Notification("Client pause state is now {0}", paused ? "on" : "off");
		}
	}

	private void ViewDistanceChanged(int newValue)
	{
		if (newValue != player.worlddata.LastApprovedViewDistance)
		{
			player.worlddata.RequestNewViewDistance(this);
		}
		frustumCuller.UpdateViewDistance(newValue);
	}

	private void OnVsyncChanged(int vsyncmode)
	{
		Platform.SetVSync(vsyncmode != 0);
	}

	public void FindCmd(string search)
	{
		ICoreClientAPI capi = api;
		List<int> searchIds = new List<int>();
		foreach (Block block in Blocks)
		{
			if (block.Code != null && block.Code.Path.Contains(search))
			{
				searchIds.Add(block.BlockId);
			}
		}
		EntityPos defaultSpawnPosition = capi.World.DefaultSpawnPosition;
		int centreX = (int)defaultSpawnPosition.X;
		int centreZ = (int)defaultSpawnPosition.Z;
		StringBuilder sb = new StringBuilder();
		foreach (KeyValuePair<long, ClientChunk> pair in WorldMap.chunks)
		{
			pair.Value.Unpack();
			if (pair.Value.Data is ChunkData data)
			{
				BlockPos found = data.FindFirst(searchIds);
				if (!(found == null))
				{
					long key = pair.Key;
					int cx = (int)(key % WorldMap.index3dMulX) * 32 + found.X;
					int cy = (int)(key / WorldMap.index3dMulX / WorldMap.index3dMulZ) * 32 + found.Y;
					int cz = (int)(key / WorldMap.index3dMulX % WorldMap.index3dMulZ) * 32 + found.Z;
					sb.Append("\nFound at " + (cx - centreX) + "," + cy + "," + (cz - centreZ));
				}
			}
		}
		if (sb.Length == 0)
		{
			sb.Append("No block matching '" + search + "' found");
		}
		capi.ShowChatMessage(sb.ToString());
	}

	public float EyesInLavaDepth()
	{
		double eyePos = MainCamera.CameraEyePos.Y;
		BlockPos pos = new BlockPos((int)MainCamera.CameraEyePos.X, (int)eyePos, (int)MainCamera.CameraEyePos.Z);
		AssetLocation code = WorldMap.RelaxedBlockAccess.GetBlock(pos).Code;
		if (!(code != null) || !code.PathStartsWith("lava"))
		{
			return 0f;
		}
		float distFromSurface = 1f - ((float)eyePos - (float)(int)eyePos);
		while (true)
		{
			AssetLocation code2 = WorldMap.RelaxedBlockAccess.GetBlock(pos.Up()).Code;
			if ((object)code2 == null || !code2.PathStartsWith("lava"))
			{
				break;
			}
			distFromSurface += 1f;
		}
		return distFromSurface;
	}

	public float EyesInWaterDepth()
	{
		double eyePos = MainCamera.CameraEyePos.Y;
		BlockPos pos = new BlockPos((int)MainCamera.CameraEyePos.X, (int)eyePos, (int)MainCamera.CameraEyePos.Z);
		string liquidCode = WorldMap.RelaxedBlockAccess.GetBlock(pos).LiquidCode;
		if (!(liquidCode == "water") && !(liquidCode == "seawater") && !(liquidCode == "saltwater"))
		{
			return 0f;
		}
		float distFromSurface = 1f - ((float)eyePos - (float)(int)eyePos);
		liquidCode = WorldMap.RelaxedBlockAccess.GetBlock(pos.Up(), 2).LiquidCode;
		while (true)
		{
			switch (liquidCode)
			{
			case "water":
			case "seawater":
			case "saltwater":
				break;
			default:
				return distFromSurface;
			}
			distFromSurface += 1f;
			liquidCode = WorldMap.RelaxedBlockAccess.GetBlock(pos.Up(), 2).LiquidCode;
		}
	}

	public int GetEyesInWaterColorShift()
	{
		double eyePos = MainCamera.CameraEyePos.Y;
		string liquidCode = WorldMap.RelaxedBlockAccess.GetBlock((int)MainCamera.CameraEyePos.X, (int)eyePos, (int)MainCamera.CameraEyePos.Z, 2).LiquidCode;
		bool liquid = liquidCode == "water" || liquidCode == "seawater" || liquidCode == "saltwater";
		liquidCode = WorldMap.RelaxedBlockAccess.GetBlock((int)MainCamera.CameraEyePos.X, (int)(MainCamera.CameraEyePos.Y + 1.0), (int)MainCamera.CameraEyePos.Z, 2).LiquidCode;
		bool aboveLiquid = liquidCode == "water" || liquidCode == "seawater" || liquidCode == "saltwater";
		if (liquid && aboveLiquid)
		{
			return 100;
		}
		if (!liquid)
		{
			return 0;
		}
		float distFromSurface = (float)eyePos - (float)(int)eyePos;
		return (int)Math.Max(0f, Math.Min(100f, 600f * (1.04f - distFromSurface)));
	}

	public int GetEyesInLavaColorShift()
	{
		double eyePos = MainCamera.CameraEyePos.Y;
		AssetLocation code = WorldMap.RelaxedBlockAccess.GetBlock((int)MainCamera.CameraEyePos.X, (int)eyePos, (int)MainCamera.CameraEyePos.Z).Code;
		bool liquid = code != null && code.PathStartsWith("lava");
		code = WorldMap.RelaxedBlockAccess.GetBlock((int)MainCamera.CameraEyePos.X, (int)(MainCamera.CameraEyePos.Y + 1.0), (int)MainCamera.CameraEyePos.Z).Code;
		bool aboveLiquid = code != null && code.PathStartsWith("lava");
		if (liquid && aboveLiquid)
		{
			return 100;
		}
		if (!liquid)
		{
			return 0;
		}
		float distFromSurface = (float)eyePos - (float)(int)eyePos;
		return (int)Math.Max(0f, Math.Min(100f, 600f * (1.04f - distFromSurface)));
	}

	public void RedrawAllBlocks()
	{
		ShouldRedrawAllBlocks = true;
	}

	public void OnResize()
	{
		Platform.GlViewport(0, 0, Platform.WindowSize.Width, Platform.WindowSize.Height);
		Reset3DProjection();
	}

	public void DoReconnect()
	{
		doReconnect = true;
	}

	public void ExitAndSwitchServer(MultiplayerServerEntry redirect)
	{
		if (IsSingleplayer)
		{
			Platform.ExitSinglePlayerServer();
		}
		RedirectTo = redirect;
		exitToMainMenu = true;
	}

	public MultiplayerServerEntry GetRedirect()
	{
		return RedirectTo;
	}

	public void DestroyGameSession(bool gotDisconnected)
	{
		if (exitToMainMenu || exitToDisconnectScreen)
		{
			return;
		}
		Platform.ShaderUniforms = new DefaultShaderUniforms();
		Logger.Notification("Destroying game session, waiting up to 200ms for client threads to exit");
		api.eventapi.TriggerLeaveWorld();
		threadsShouldExit = true;
		int tries = 2;
		while (tries-- > 0)
		{
			bool allThreadsExited = true;
			foreach (Thread thread in clientThreads)
			{
				allThreadsExited &= !thread.IsAlive;
			}
			if (allThreadsExited)
			{
				break;
			}
			Thread.Sleep(100);
		}
		_clientThreadsCts.Cancel();
		if (IsSingleplayer && ScreenManager.Platform.IsServerRunning)
		{
			Logger.Notification("Stopping single player server");
			Platform.ExitSinglePlayerServer();
		}
		RedirectTo = null;
		exitToMainMenu = !gotDisconnected;
		exitToDisconnectScreen = gotDisconnected;
		MouseGrabbed = false;
		api.eventapi.TriggerLeftWorld();
		Dispose();
	}

	private void UpdateResize()
	{
		if (lastWidth != Platform.WindowSize.Width || lastHeight != Platform.WindowSize.Height)
		{
			lastWidth = Platform.WindowSize.Width;
			lastHeight = Platform.WindowSize.Height;
			OnResize();
		}
	}

	public void EnqueueMainThreadTask(Action action, string code)
	{
		lock (MainThreadTasksLock)
		{
			MainThreadTasks.Enqueue(new ClientTask
			{
				Action = action,
				Code = code
			});
		}
	}

	public void EnqueueGameLaunchTask(Action action, string code)
	{
		if (!disposed)
		{
			GameLaunchTasks.Enqueue(new ClientTask
			{
				Action = action,
				Code = code
			});
		}
	}

	public void Dispose()
	{
		disposed = true;
		api.disposed = true;
		BlockChunkDataLayer.Dispose();
		MeshData.Recycler?.Dispose();
		MusicEngineCts?.Cancel();
		ClientSystem[] array = clientSystems;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Dispose(this);
		}
		Thread.Sleep(100);
		foreach (LoadedTexture value in texturesByLocation.Values)
		{
			value?.Dispose();
		}
		if (Blocks != null)
		{
			foreach (Block block in Blocks)
			{
				block?.OnUnloaded(api);
			}
		}
		if (Items != null)
		{
			foreach (Item item in Items)
			{
				item?.OnUnloaded(api);
			}
		}
		PhysicsBehaviorBase.collisionTester = null;
		quadModel?.Dispose();
		api?.Dispose();
		ItemAtlasManager.Dispose();
		BlockAtlasManager.Dispose();
		EntityAtlasManager.Dispose();
		TesselatorManager.Dispose();
		WorldMap.Dispose();
		_clientThreadsCts.Dispose();
		MusicEngineCts?.Dispose();
		MusicEngineCts = null;
		Platform.ClearOnCrash();
		ClientSettings.Inst.ClearWatchers();
		ScreenRunningGame.ScreenManager.registerSettingsWatchers();
		ScreenManager.hotkeyManager.ClearInGameHotKeyHandlers();
		VtmlUtil.TagConverters.Clear();
		eventManager?.Dispose();
		eventManager = null;
		rand?.Dispose();
		ClassRegistry = null;
		MainNetClient?.Dispose();
		UdpNetClient?.Dispose();
		Platform.Logger.ClearWatchers();
		MeshData.Recycler = null;
		BlockChunkDataLayer.blocksByPaletteIndex = null;
		Platform.AssetManager.UnloadExternalAssets(Logger);
		Platform.AssetManager.CustomModOrigins.Clear();
	}

	public int ApplyColorMapOnRgba(string climateColorMap, string seasonColorMap, int color, int posX, int posY, int posZ, bool flipRb = true)
	{
		return WorldMap.ApplyColorMapOnRgba(climateColorMap, seasonColorMap, color, posX, posY, posZ, flipRb);
	}

	public int ApplyColorMapOnRgba(ColorMap climateMap, ColorMap seasonMap, int color, int posX, int posY, int posZ, bool flipRb = true)
	{
		return WorldMap.ApplyColorMapOnRgba(climateMap, seasonMap, color, posX, posY, posZ, flipRb);
	}

	public int ApplyColorMapOnRgba(string climateColorMap, string seasonColorMap, int color, int rain, int temp, bool flipRb = true)
	{
		return WorldMap.ApplyColorMapOnRgba(climateColorMap, seasonColorMap, color, rain, temp, flipRb);
	}

	public void TryAttackEntity(EntitySelection selection)
	{
		if (selection != null)
		{
			Entity entity = selection.Entity;
			Cuboidd cuboidd = entity.SelectionBox.ToDouble().Translate(entity.Pos.X, entity.Pos.Y, entity.Pos.Z);
			EntityPos pos = EntityPlayer.SidedPos;
			ItemStack heldStack = Player?.InventoryManager?.ActiveHotbarSlot?.Itemstack;
			float attackRange = heldStack?.Collectible.GetAttackRange(heldStack) ?? GlobalConstants.DefaultAttackRange;
			if (cuboidd.ShortestDistanceFrom(pos.X + EntityPlayer.LocalEyePos.X, pos.Y + EntityPlayer.LocalEyePos.Y, pos.Z + EntityPlayer.LocalEyePos.Z) <= (double)attackRange)
			{
				selection.Entity.OnInteract(EntityPlayer, player.inventoryMgr.ActiveHotbarSlot, selection.HitPosition, EnumInteractMode.Attack);
				SendPacketClient(ClientPackets.EntityInteraction(0, selection.Entity.EntityId, selection.Face, selection.HitPosition, selection.SelectionBoxIndex));
			}
		}
	}

	public void CloneBlockDamage(BlockPos sourcePos, BlockPos targetPos)
	{
		if (damagedBlocks.TryGetValue(sourcePos, out var blockdmg))
		{
			if (!damagedBlocks.TryGetValue(targetPos, out var targetDmg))
			{
				Dictionary<BlockPos, BlockDamage> dictionary = damagedBlocks;
				BlockDamage obj = new BlockDamage
				{
					Position = targetPos,
					Block = blockAccessor.GetBlock(targetPos),
					Facing = blockdmg.Facing,
					RemainingResistance = blockdmg.RemainingResistance,
					LastBreakEllapsedMs = blockdmg.LastBreakEllapsedMs,
					BeginBreakEllapsedMs = blockdmg.BeginBreakEllapsedMs,
					ByPlayer = blockdmg.ByPlayer,
					Tool = blockdmg.Tool,
					BreakingCounter = blockdmg.BreakingCounter
				};
				BlockDamage blockDamage = obj;
				dictionary[targetPos] = obj;
				targetDmg = blockDamage;
			}
			else
			{
				targetDmg.RemainingResistance = blockdmg.RemainingResistance;
				targetDmg.LastBreakEllapsedMs = blockdmg.LastBreakEllapsedMs;
				targetDmg.BeginBreakEllapsedMs = blockdmg.BeginBreakEllapsedMs;
				targetDmg.Tool = blockdmg.Tool;
				targetDmg.BreakingCounter = blockdmg.BreakingCounter;
			}
			eventManager?.TriggerBlockBreaking(targetDmg);
		}
	}

	public void IncurBlockDamage(BlockSelection blockSelection, EnumTool? withTool, float damage)
	{
		Block block = blockAccessor.GetBlock(blockSelection.Position);
		BlockDamage blockdmg = loadOrCreateBlockDamage(blockSelection, block, withTool, null);
		long elapsedMs = ElapsedMilliseconds;
		int diff = (int)(elapsedMs - blockdmg.LastBreakEllapsedMs);
		blockdmg.RemainingResistance = block.OnGettingBroken(player, blockSelection, player.inventoryMgr.ActiveHotbarSlot, blockdmg.RemainingResistance, (float)diff / 1000f, blockdmg.BreakingCounter);
		blockdmg.BreakingCounter++;
		blockdmg.Facing = blockSelection.Face;
		if (blockdmg.Position != blockSelection.Position || blockdmg.Block != block)
		{
			blockdmg.RemainingResistance = block.GetResistance(BlockAccessor, blockSelection.Position);
			blockdmg.Block = block;
			blockdmg.Position = blockSelection.Position;
		}
		blockdmg.LastBreakEllapsedMs = elapsedMs;
	}

	public BlockDamage loadOrCreateBlockDamage(BlockSelection blockSelection, Block block, EnumTool? tool, IPlayer byPlayer)
	{
		damagedBlocks.TryGetValue(blockSelection.Position, out var blockdmg);
		if (blockdmg == null)
		{
			blockdmg = new BlockDamage
			{
				Position = blockSelection.Position.Copy(),
				Block = block,
				Facing = blockSelection.Face,
				RemainingResistance = block.GetResistance(BlockAccessor, blockSelection.Position),
				LastBreakEllapsedMs = ElapsedMilliseconds,
				BeginBreakEllapsedMs = ElapsedMilliseconds,
				ByPlayer = byPlayer,
				Tool = tool
			};
			damagedBlocks[blockSelection.Position.Copy()] = blockdmg;
		}
		else if (blockdmg.Tool != tool)
		{
			blockdmg.RemainingResistance = block.GetResistance(BlockAccessor, blockSelection.Position);
			blockdmg.Tool = tool;
		}
		return blockdmg;
	}

	public void SetCameraShake(float strength)
	{
		MainCamera.CameraShakeStrength = strength * ClientSettings.CameraShakeStrength;
	}

	public void AddCameraShake(float strength)
	{
		MainCamera.CameraShakeStrength += strength * ClientSettings.CameraShakeStrength;
	}

	public void ReduceCameraShake(float amount)
	{
		MainCamera.CameraShakeStrength = Math.Max(0f, MainCamera.CameraShakeStrength - amount * ClientSettings.CameraShakeStrength);
	}

	private List<string> makeEntityCodesCache()
	{
		ICollection<AssetLocation> keys = EntityClassesByCode.Keys;
		List<string> list = new List<string>(keys.Count);
		foreach (AssetLocation key in keys)
		{
			list.Add(key.ToShortString());
		}
		return list;
	}

	public IMiniDimension GetOrCreateDimension(int dimId, Vec3d pos)
	{
		return ((ClientWorldMap)worldmap).GetOrCreateDimension(dimId, pos);
	}

	public bool TryGetMiniDimension(Vec3i origin, out IMiniDimension dimension)
	{
		return MiniDimensions.TryGetValue(BlockAccessorMovable.CalcSubDimensionId(origin), out dimension);
	}

	public void SetBlocksPreviewDimension(int dimId)
	{
		Dimensions.BlocksPreviewSubDimension_Client = dimId;
	}

	public void SetChunkColumnVisible(int cx, int cz, int dimension)
	{
		for (int cy = 0; cy < worldmap.chunkMapSizeY; cy++)
		{
			long index3d = worldmap.ChunkIndex3D(cx, cy + dimension * 1024, cz);
			ClientChunk chunk = null;
			if (WorldMap.chunks.TryGetValue(index3d, out chunk))
			{
				int bufIndex = ClientChunk.bufIndex;
				ClientChunk.bufIndex = 0;
				chunk.SetVisible(visible: true);
				ClientChunk.bufIndex = 1;
				chunk.SetVisible(visible: true);
				ClientChunk.bufIndex = bufIndex;
			}
		}
	}

	public Entity SpawnItemEntity(ItemStack itemstack, Vec3d position, Vec3d velocity = null)
	{
		return null;
	}

	public Entity SpawnItemEntity(ItemStack itemstack, BlockPos pos, Vec3d velocity = null)
	{
		return null;
	}

	public void SpawnEntity(Entity entity)
	{
	}

	public IPlayer[] GetPlayersAround(Vec3d position, float horRange, float vertRange, ActionConsumable<IPlayer> matches = null)
	{
		List<IPlayer> players = new List<IPlayer>();
		float horRangeSq = horRange * horRange;
		foreach (ClientPlayer player in PlayersByUid.Values)
		{
			if (player.Entity != null && player.Entity.Pos.InRangeOf(position, horRangeSq, vertRange) && (matches == null || matches(player)))
			{
				players.Add(player);
			}
		}
		return players.ToArray();
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

	public override Cuboidf[] GetBlockIntersectionBoxes(BlockPos pos)
	{
		return GetBlockIntersectionBoxes(pos, LiquidSelectable);
	}

	public override bool IsValidPos(BlockPos pos)
	{
		return WorldMap.IsValidPos(pos);
	}

	public void TrySetWorldConfig(byte[] configBytes)
	{
		if (WorldConfig == null && configBytes != null)
		{
			WorldConfig = new TreeAttribute();
			WorldConfig.FromBytes(configBytes);
		}
	}

	public override Block GetBlock(BlockPos pos)
	{
		return WorldMap.RelaxedBlockAccess.GetBlock(pos);
	}

	public long RegisterGameTickListener(Action<float> OnGameTick, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		if (eventManager != null)
		{
			return eventManager.AddGameTickListener(OnGameTick, millisecondInterval, initialDelayOffsetMs);
		}
		return 0L;
	}

	public long RegisterGameTickListener(Action<float> OnGameTick, Action<Exception> errorHandler, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		return eventManager.AddGameTickListener(OnGameTick, errorHandler, millisecondInterval, initialDelayOffsetMs);
	}

	public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay)
	{
		return RegisterCallback(OnTimePassed, millisecondDelay, permittedWhilePaused: false);
	}

	public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay, bool permittedWhilePaused)
	{
		if (IsPaused && !permittedWhilePaused)
		{
			Logger.Notification("Call to RegisterCallback while game is paused");
			if (ClientSettings.DeveloperMode && extendedDebugInfo)
			{
				throw new Exception("Call to RegisterCallback while game is paused. ExtendedDebug info and developermode is enabled, so will crash on this for reporting reasons.");
			}
		}
		if (eventManager != null)
		{
			return eventManager.AddDelayedCallback(OnTimePassed, millisecondDelay);
		}
		return 0L;
	}

	public long RegisterGameTickListener(Action<IWorldAccessor, BlockPos, float> OnGameTick, BlockPos pos, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		if (eventManager != null)
		{
			return eventManager.AddGameTickListener(OnGameTick, pos, millisecondInterval, initialDelayOffsetMs);
		}
		return 0L;
	}

	public long RegisterCallback(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay)
	{
		if (eventManager != null)
		{
			return eventManager.AddDelayedCallback(OnTimePassed, pos, millisecondDelay);
		}
		return 0L;
	}

	public long RegisterCallbackUnique(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay)
	{
		if (eventManager != null)
		{
			return eventManager.AddSingleDelayedCallback(OnTimePassed, pos, millisecondDelay);
		}
		return 0L;
	}

	public void UnregisterCallback(long listenerId)
	{
		eventManager?.RemoveDelayedCallback(listenerId);
	}

	public void UnregisterGameTickListener(long listenerId)
	{
		eventManager?.RemoveGameTickListener(listenerId);
	}

	public void TriggerNeighbourBlocksUpdate(BlockPos pos)
	{
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing facing in aLLFACES)
		{
			BlockPos neibPos = pos.AddCopy(facing.Normali.X, facing.Normali.Y, facing.Normali.Z);
			Block block = WorldMap.RelaxedBlockAccess.GetBlock(neibPos);
			block.OnNeighbourBlockChange(this, neibPos, pos);
			if (block.ForFluidsLayer)
			{
				continue;
			}
			Block liquidBlock = WorldMap.RelaxedBlockAccess.GetBlock(neibPos, 2);
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

	public void SpawnParticles(float quantity, int color, Vec3d minPos, Vec3d maxPos, Vec3f minVelocity, Vec3f maxVelocity, float lifeLength, float gravityEffect, float scale = 1f, EnumParticleModel model = EnumParticleModel.Quad, IPlayer dualCallByPlayer = null)
	{
		particleManager.SpawnParticles(quantity, color, minPos, maxPos, minVelocity, maxVelocity, lifeLength, gravityEffect, scale, model);
	}

	public void SpawnParticles(IParticlePropertiesProvider particlePropertiesProvider, IPlayer dualCallByPlayer = null)
	{
		particleManager.SpawnParticles(particlePropertiesProvider);
	}

	public void SpawnCubeParticles(Vec3d pos, ItemStack itemstack, float radius, int quantity, float scale = 1f, IPlayer dualCallByPlayer = null, Vec3f velocity = null)
	{
		particleManager.SpawnParticles(new StackCubeParticles(pos, itemstack, radius, quantity, scale, velocity));
	}

	public void SpawnCubeParticles(BlockPos blockpos, Vec3d pos, float radius, int quantity, float scale = 1f, IPlayer dualCallByPlayer = null, Vec3f velocity = null)
	{
		particleManager.SpawnParticles(new BlockCubeParticles(this, blockpos, pos, radius, quantity, scale, velocity));
	}

	public bool PlayerHasPrivilege(int clientid, string privilege)
	{
		return true;
	}

	public void ShowChatMessage(string message)
	{
		eventManager?.TriggerNewServerChatLine(GlobalConstants.CurrentChatGroup, message, EnumChatType.Notification, null);
	}

	public void SendMessageToClient(string message)
	{
		eventManager?.TriggerNewModChatLine(GlobalConstants.CurrentChatGroup, message, EnumChatType.OwnMessage, null);
	}

	public void SendArbitraryPacket(byte[] data)
	{
		SendPacket(data);
	}

	public void SendBlockEntityPacket(int x, int y, int z, int packetId, byte[] data = null)
	{
		Packet_BlockEntityPacket packet = new Packet_BlockEntityPacket
		{
			Packetid = packetId,
			X = x,
			Y = y,
			Z = z
		};
		packet.SetData(data);
		SendPacketClient(new Packet_Client
		{
			Id = 22,
			BlockEntityPacket = packet
		});
	}

	public void SendEntityPacket(long entityId, int packetId, byte[] data = null)
	{
		Packet_EntityPacket packet = new Packet_EntityPacket
		{
			Packetid = packetId,
			EntityId = entityId
		};
		packet.SetData(data);
		SendPacketClient(new Packet_Client
		{
			Id = 31,
			EntityPacket = packet
		});
	}

	public IPlayer NearestPlayer(double x, double y, double z)
	{
		IPlayer closestplayer = null;
		float closestSqDistance = -1f;
		foreach (ClientPlayer val in PlayersByUid.Values)
		{
			if (val.Entity != null)
			{
				float distanceSq = val.Entity.Pos.SquareDistanceTo(x, y, z);
				if (closestSqDistance == -1f || distanceSq < closestSqDistance)
				{
					closestSqDistance = distanceSq;
					closestplayer = val;
				}
			}
		}
		return closestplayer;
	}

	public IPlayer PlayerByUid(string playerUid)
	{
		if (playerUid == null)
		{
			return null;
		}
		PlayersByUid.TryGetValue(playerUid, out var plr);
		return plr;
	}

	public ColorMapData GetColorMapData(Block block, int posX, int posY, int posZ)
	{
		return WorldMap.getColorMapData(block, posX, posY, posZ);
	}

	public bool LoadEntity(Entity entity, long fromChunkIndex3d)
	{
		throw new InvalidOperationException("Cannot use LoadEntity on the Client side");
	}

	public AssetLocation ResolveSoundPath(AssetLocation location)
	{
		if (SoundConfig == null)
		{
			throw new Exception("soundconfig.json not loaded. Is it missing or are you trying to load sounds before the client has received the level finalize event?");
		}
		SoundConfig.Soundsets.TryGetValue(location, out var sounds);
		if (sounds != null && sounds.Length != 0)
		{
			if (!SoundIteration.TryGetValue(location, out var pos) || Rand.NextDouble() < 0.35)
			{
				pos = Rand.Next(sounds.Length);
				SoundIteration[location] = pos;
			}
			SoundIteration[location] = (SoundIteration[location] + 1) % sounds.Length;
			return sounds[pos];
		}
		if (location.EndsWithWildCard)
		{
			int catlen = location.Category.Code.Length + 1;
			string basePath = location.Path.Substring(catlen, location.Path.Length - catlen - 1);
			List<IAsset> assets = AssetManager.GetManyInCategory(location.Category.Code, basePath, location.Domain, loadAsset: false);
			if (assets.Count > 0)
			{
				return assets[rand.Value.Next(assets.Count)].Location;
			}
		}
		return location;
	}

	public void PlaySound(AssetLocation location, bool randomizePitch = false, float volume = 1f)
	{
		PlaySoundAt(location, 0.0, 0.0, 0.0, null, randomizePitch, 32f, volume);
	}

	public void PlaySoundAt(AssetLocation location, IPlayer atPlayer, IPlayer ignorePlayerUid = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		if (atPlayer == null)
		{
			atPlayer = Player;
		}
		PlaySoundAt(location, atPlayer.Entity.Pos.X, atPlayer.Entity.Pos.InternalY, atPlayer.Entity.Pos.Z, volume, randomizePitch, range);
	}

	public void PlaySoundAt(AssetLocation location, Entity atEntity, IPlayer dualCallByPlayer = null, float pitch = 1f, float range = 32f, float volume = 1f)
	{
		float yoff = 0f;
		if (atEntity.SelectionBox != null)
		{
			yoff = atEntity.SelectionBox.Y2 / 2f;
		}
		else if (atEntity.Properties?.CollisionBoxSize != null)
		{
			yoff = atEntity.Properties.CollisionBoxSize.Y / 2f;
		}
		PlaySoundAtInternal(location, atEntity.Pos.X, atEntity.Pos.InternalY + (double)yoff, atEntity.Pos.Z, volume, pitch, range);
	}

	public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer = null, float pitch = 1f, float range = 32f, float volume = 1f)
	{
		PlaySoundAtInternal(location, posx, posy, posz, volume, pitch, range);
	}

	public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer, EnumSoundType soundType, float pitch = 1f, float range = 32f, float volume = 1f)
	{
		PlaySoundAtInternal(location, posx, posy, posz, volume, pitch, range, soundType);
	}

	public void PlaySoundAt(AssetLocation location, Entity atEntity, IPlayer ignorePlayerUid = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		float yoff = 0f;
		if (atEntity.SelectionBox != null)
		{
			yoff = atEntity.SelectionBox.Y2 / 2f;
		}
		else if (atEntity.Properties?.CollisionBoxSize != null)
		{
			yoff = atEntity.Properties.CollisionBoxSize.Y / 2f;
		}
		PlaySoundAt(location, atEntity.Pos.X, atEntity.Pos.InternalY + (double)yoff, atEntity.Pos.Z, ignorePlayerUid, randomizePitch, range, volume);
	}

	public void PlaySoundAt(AssetLocation location, double x, double y, double z, IPlayer ignorePlayerUid = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		PlaySoundAt(location, x, y, z, volume, randomizePitch, range);
	}

	public void PlaySoundAt(AssetLocation location, BlockPos pos, double yOffsetFromCenter, IPlayer ignorePlayerUid = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		PlaySoundAt(location, (double)pos.X + 0.5, (double)pos.InternalY + 0.5 + yOffsetFromCenter, (double)pos.Z + 0.5, volume, randomizePitch, range);
	}

	public int PlaySoundAtAndGetDuration(AssetLocation location, double x, double y, double z, IPlayer ignorePlayerUid = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		return PlaySoundAt(location, x, y, z, volume, randomizePitch, range);
	}

	public void PlaySoundFor(AssetLocation location, IPlayer atPlayer, float pitch, float range = 32f, float volume = 1f)
	{
		if (atPlayer == null)
		{
			atPlayer = Player;
		}
		PlaySoundAtInternal(location, atPlayer.Entity.Pos.X, atPlayer.Entity.Pos.InternalY, atPlayer.Entity.Pos.Z, volume, pitch, range);
	}

	public void PlaySoundFor(AssetLocation location, IPlayer forPlayer, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		PlaySoundAt(location, forPlayer, null, randomizePitch, range, volume);
	}

	public int PlaySoundAt(AssetLocation location, double x, double y, double z, float volume, bool randomizePitch = true, float range = 32f)
	{
		float pitch = (randomizePitch ? RandomPitch() : 1f);
		return PlaySoundAtInternal(location, x, y, z, volume, pitch, range);
	}

	private int PlaySoundAtInternal(AssetLocation location, double x, double y, double z, float volume, float pitch = 1f, float range = 32f, EnumSoundType soundType = EnumSoundType.Sound)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Cannot call PlaySound outside the main thread, it is not thread safe");
		}
		if (ActiveSounds.Count >= 250)
		{
			if (ElapsedMilliseconds - lastSkipTotalMs > 1000)
			{
				Logger.Notification("Play sound {0} skipped because max concurrent sounds of 250 reached ({1} more skipped)", location, cntSkip);
				cntSkip = 0;
				lastSkipTotalMs = ElapsedMilliseconds;
			}
			cntSkip++;
			return 0;
		}
		if (location == null)
		{
			return 0;
		}
		if (ClientSettings.SoundLevel == 0)
		{
			return 0;
		}
		location = ResolveSoundPath(location).Clone().WithPathAppendixOnce(".ogg");
		if (location == null)
		{
			return 0;
		}
		SoundParams sparams;
		if (x == 0.0 && y == 0.0 && z == 0.0)
		{
			sparams = new SoundParams(location)
			{
				RelativePosition = true,
				Range = range,
				SoundType = soundType
			};
		}
		else
		{
			if (player.Entity.Pos.SquareDistanceTo(x, y, z) > range * range)
			{
				return 0;
			}
			sparams = new SoundParams(location)
			{
				Position = new Vec3f((float)x, (float)y, (float)z),
				Range = range,
				SoundType = soundType
			};
		}
		sparams.Pitch = pitch;
		sparams.Volume *= volume;
		ScreenManager.soundAudioData.TryGetValue(location, out var audiodata);
		if (audiodata != null)
		{
			int result = audiodata.Load_Async(new MainThreadAction(this, () => StartPlaying(audiodata, sparams, location), "playSound"));
			if (result >= 0)
			{
				return result;
			}
			return 500;
		}
		Platform.Logger.Warning("Audio File not found: {0}", location);
		return 0;
	}

	private int StartPlaying(AudioData audiodata, SoundParams sparams, AssetLocation location)
	{
		if (EyesInWaterDepth() > 0f)
		{
			sparams.LowPassFilter = 0.06f;
		}
		ILoadedSound loadedSound = Platform.CreateAudio(sparams, audiodata, this);
		if (audiodata.Loaded == 3)
		{
			return StartPlaying(loadedSound, location);
		}
		((AudioMetaData)audiodata).AddOnLoaded(new MainThreadAction(this, () => StartPlaying(loadedSound, location), "soundplaying"));
		return 100;
	}

	public int StartPlaying(ILoadedSound loadedSound, AssetLocation location)
	{
		loadedSound.Start();
		if (EyesInWaterDepth() > 0f && loadedSound.Params.SoundType != EnumSoundType.Music && loadedSound.Params.SoundType != EnumSoundType.MusicGlitchunaffected)
		{
			loadedSound.SetPitchOffset(-0.15f);
		}
		if (EyesInWaterDepth() == 0f && SystemSoundEngine.NowReverbness >= 0.25f && (loadedSound.Params.Position == null || loadedSound.Params.Position == SystemSoundEngine.Zero || SystemSoundEngine.RoomLocation.ContainsOrTouches(loadedSound.Params.Position)))
		{
			loadedSound.SetReverb(SystemSoundEngine.NowReverbness);
		}
		if (ClientSettings.DeveloperMode && loadedSound.Channels > 1 && !loadedSound.Params.RelativePosition)
		{
			Platform.Logger.Warning("Audio File {0} is a stereo sound but loaded as a locational sound, will not attenuate correctly.", location);
		}
		ActiveSounds.Enqueue(loadedSound);
		return (int)(loadedSound.SoundLengthSeconds * 1000f);
	}

	public ILoadedSound LoadSound(SoundParams sound)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Cannot call LoadSound outside the main thread, it is not thread safe");
		}
		if (sound.Location != null)
		{
			sound.Location = ResolveSoundPath(sound.Location.Clone());
			sound.Location.WithPathAppendixOnce(".ogg");
		}
		ScreenManager.soundAudioData.TryGetValue(sound.Location, out var data);
		if (data == null)
		{
			Platform.Logger.Warning("Audio File not found: {0}", sound.Location);
			return null;
		}
		ILoadedSound loadedSound = Platform.CreateAudio(sound, data, this);
		if (EyesInWaterDepth() > 0f)
		{
			loadedSound.SetPitchOffset(-0.2f);
		}
		if (EyesInWaterDepth() == 0f && SystemSoundEngine.NowReverbness >= 0.25f && (loadedSound.Params.Position == null || loadedSound.Params.Position == SystemSoundEngine.Zero || SystemSoundEngine.RoomLocation.ContainsOrTouches(loadedSound.Params.Position)))
		{
			loadedSound.SetReverb(SystemSoundEngine.NowReverbness);
		}
		ActiveSounds.Enqueue(loadedSound);
		return loadedSound;
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

	public void RegisterDialog(params GuiDialog[] dialogs)
	{
		foreach (GuiDialog dialog in dialogs)
		{
			if (!LoadedGuis.Contains(dialog))
			{
				int startIndex = LoadedGuis.FindIndex((GuiDialog d) => d.InputOrder >= dialog.InputOrder);
				if (startIndex < 0)
				{
					startIndex = LoadedGuis.Count;
				}
				int endIndex = LoadedGuis.FindIndex(startIndex, (GuiDialog d) => d.InputOrder < dialog.InputOrder);
				if (endIndex < 0)
				{
					endIndex = LoadedGuis.Count;
				}
				int index = LoadedGuis.FindIndex(startIndex, endIndex - startIndex, (GuiDialog d) => d.DrawOrder < dialog.DrawOrder);
				if (index < 0)
				{
					index = endIndex;
				}
				LoadedGuis.Insert(index, dialog);
			}
		}
	}

	public void UnregisterDialog(GuiDialog dialog)
	{
		LoadedGuis.Remove(dialog);
		OpenedGuis.Remove(dialog);
	}

	public void HighlightBlocks(IPlayer player, int slotId, List<BlockPos> blocks, List<int> colors, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary, float scale = 1f)
	{
		eventManager?.TriggerHighlightBlocks(player, slotId, blocks, colors, mode, shape, scale);
	}

	public void HighlightBlocks(IPlayer player, int slotId, List<BlockPos> blocks, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary)
	{
		eventManager?.TriggerHighlightBlocks(player, slotId, blocks, null, mode, shape);
	}

	public void RemoveEntityRenderer(Entity forEntity)
	{
		EntityRenderers.TryGetValue(forEntity, out var renderer);
		renderer?.Dispose();
		EntityRenderers.Remove(forEntity);
		forEntity.Properties.Client.Renderer = null;
	}
}
