using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class WorldMapManager : ModSystem, IWorldMapManager
{
	public Dictionary<string, Type> MapLayerRegistry = new Dictionary<string, Type>();

	public Dictionary<string, double> LayerGroupPositions = new Dictionary<string, double>();

	private ICoreAPI api;

	private ICoreClientAPI capi;

	private IClientNetworkChannel clientChannel;

	public GuiDialogWorldMap worldMapDlg;

	public List<MapLayer> MapLayers = new List<MapLayer>();

	private Thread mapLayerGenThread;

	private IServerNetworkChannel serverChannel;

	public bool IsOpened => worldMapDlg?.IsOpened() ?? false;

	public bool IsShuttingDown { get; set; }

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		RegisterDefaultMapLayers();
		this.api = api;
	}

	public void RegisterDefaultMapLayers()
	{
		RegisterMapLayer<ChunkMapLayer>("chunks", 0.0);
		RegisterMapLayer<PlayerMapLayer>("players", 0.5);
		RegisterMapLayer<EntityMapLayer>("entities", 0.5);
		RegisterMapLayer<WaypointMapLayer>("waypoints", 1.0);
	}

	public void RegisterMapLayer<T>(string code, double position) where T : MapLayer
	{
		MapLayerRegistry[code] = typeof(T);
		LayerGroupPositions[code] = position;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		capi = api;
		capi.Event.LevelFinalize += OnLvlFinalize;
		capi.Event.RegisterGameTickListener(OnClientTick, 20);
		capi.Settings.AddWatcher<bool>("showMinimapHud", delegate
		{
			ToggleMap(EnumDialogType.HUD);
		});
		capi.Event.LeaveWorld += delegate
		{
			IsShuttingDown = true;
			int num = 0;
			while (mapLayerGenThread != null && mapLayerGenThread.IsAlive && num < 20)
			{
				Thread.Sleep(50);
				num++;
			}
			worldMapDlg?.Dispose();
			foreach (MapLayer mapLayer in MapLayers)
			{
				mapLayer?.OnShutDown();
				mapLayer?.Dispose();
			}
		};
		clientChannel = api.Network.RegisterChannel("worldmap").RegisterMessageType(typeof(MapLayerUpdate)).RegisterMessageType(typeof(OnViewChangedPacket))
			.RegisterMessageType(typeof(OnMapToggle))
			.SetMessageHandler<MapLayerUpdate>(OnMapLayerDataReceivedClient);
	}

	private void onWorldMapLinkClicked(LinkTextComponent linkcomp)
	{
		string[] xyzstr = linkcomp.Href.Substring("worldmap://".Length).Split('=');
		int x = xyzstr[1].ToInt();
		int y = xyzstr[2].ToInt();
		int z = xyzstr[3].ToInt();
		string text = ((xyzstr.Length >= 5) ? xyzstr[4] : "");
		if (worldMapDlg == null || !worldMapDlg.IsOpened() || (worldMapDlg.IsOpened() && worldMapDlg.DialogType == EnumDialogType.HUD))
		{
			ToggleMap(EnumDialogType.Dialog);
		}
		bool exists = false;
		GuiElementMap elem = worldMapDlg.SingleComposer.GetElement("mapElem") as GuiElementMap;
		WaypointMapLayer wml = elem?.mapLayers.FirstOrDefault((MapLayer ml) => ml is WaypointMapLayer) as WaypointMapLayer;
		Vec3d pos = new Vec3d(x, y, z);
		if (wml != null)
		{
			foreach (Waypoint ownWaypoint in wml.ownWaypoints)
			{
				if (ownWaypoint.Position.Equals(pos, 0.01))
				{
					exists = true;
					break;
				}
			}
		}
		if (!exists)
		{
			capi.SendChatMessage(string.Format("/waypoint addati {0} ={1} ={2} ={3} {4} {5} {6}", "circle", x, y, z, false, "steelblue", text));
		}
		elem?.CenterMapTo(new BlockPos(x, y, z));
	}

	private void OnClientTick(float dt)
	{
		foreach (MapLayer mapLayer in MapLayers)
		{
			mapLayer.OnTick(dt);
		}
	}

	private void OnLvlFinalize()
	{
		if (capi != null && mapAllowedClient())
		{
			capi.Input.RegisterHotKey("worldmaphud", Lang.Get("Show/Hide Minimap"), GlKeys.F6, HotkeyType.HelpAndOverlays);
			capi.Input.RegisterHotKey("minimapposition", Lang.Get("keycontrol-minimap-position"), GlKeys.F6, HotkeyType.HelpAndOverlays, altPressed: false, ctrlPressed: true);
			capi.Input.RegisterHotKey("worldmapdialog", Lang.Get("Show World Map"), GlKeys.M, HotkeyType.HelpAndOverlays);
			capi.Input.SetHotKeyHandler("worldmaphud", OnHotKeyWorldMapHud);
			capi.Input.SetHotKeyHandler("minimapposition", OnHotKeyMinimapPosition);
			capi.Input.SetHotKeyHandler("worldmapdialog", OnHotKeyWorldMapDlg);
			capi.RegisterLinkProtocol("worldmap", onWorldMapLinkClicked);
		}
		foreach (KeyValuePair<string, Type> val in MapLayerRegistry)
		{
			if (!(val.Key == "entities") || api.World.Config.GetAsBool("entityMapLayer"))
			{
				MapLayers.Add((MapLayer)Activator.CreateInstance(val.Value, api, this));
			}
		}
		foreach (MapLayer mapLayer in MapLayers)
		{
			mapLayer.OnLoaded();
		}
		mapLayerGenThread = new Thread((ThreadStart)delegate
		{
			while (!IsShuttingDown)
			{
				foreach (MapLayer mapLayer2 in MapLayers)
				{
					mapLayer2.OnOffThreadTick(0.02f);
				}
				Thread.Sleep(20);
			}
		});
		mapLayerGenThread.IsBackground = true;
		mapLayerGenThread.Start();
		if (capi != null && (capi.Settings.Bool["showMinimapHud"] || !capi.Settings.Bool.Exists("showMinimapHud")) && (worldMapDlg == null || !worldMapDlg.IsOpened()))
		{
			ToggleMap(EnumDialogType.HUD);
		}
	}

	private void OnMapLayerDataReceivedClient(MapLayerUpdate msg)
	{
		for (int i = 0; i < msg.Maplayers.Length; i++)
		{
			Type type = MapLayerRegistry[msg.Maplayers[i].ForMapLayer];
			MapLayers.FirstOrDefault((MapLayer x) => x.GetType() == type)?.OnDataFromServer(msg.Maplayers[i].Data);
		}
	}

	public bool mapAllowedClient()
	{
		if (!capi.World.Config.GetBool("allowMap", defaultValue: true))
		{
			return capi.World.Player.Privileges.IndexOf("allowMap") != -1;
		}
		return true;
	}

	private bool OnHotKeyWorldMapHud(KeyCombination comb)
	{
		ToggleMap(EnumDialogType.HUD);
		return true;
	}

	private bool OnHotKeyMinimapPosition(KeyCombination comb)
	{
		int prev = capi.Settings.Int["minimapHudPosition"];
		capi.Settings.Int["minimapHudPosition"] = (prev + 1) % 4;
		if (worldMapDlg == null || !worldMapDlg.IsOpened())
		{
			ToggleMap(EnumDialogType.HUD);
		}
		else if (worldMapDlg.DialogType == EnumDialogType.HUD)
		{
			worldMapDlg.Recompose();
		}
		return true;
	}

	private bool OnHotKeyWorldMapDlg(KeyCombination comb)
	{
		ToggleMap(EnumDialogType.Dialog);
		return true;
	}

	public void ToggleMap(EnumDialogType asType)
	{
		bool isDlgOpened = worldMapDlg != null && worldMapDlg.IsOpened();
		if (!mapAllowedClient())
		{
			if (isDlgOpened)
			{
				worldMapDlg.TryClose();
			}
			return;
		}
		if (worldMapDlg != null)
		{
			if (!isDlgOpened)
			{
				if (asType == EnumDialogType.HUD)
				{
					capi.Settings.Bool.Set("showMinimapHud", value: true, shouldTriggerWatchers: false);
				}
				worldMapDlg.Open(asType);
				foreach (MapLayer mapLayer in MapLayers)
				{
					mapLayer.OnMapOpenedClient();
				}
				clientChannel.SendPacket(new OnMapToggle
				{
					OpenOrClose = true
				});
			}
			else if (worldMapDlg.DialogType != asType)
			{
				worldMapDlg.Open(asType);
			}
			else
			{
				if (asType == EnumDialogType.HUD)
				{
					capi.Settings.Bool.Set("showMinimapHud", value: false, shouldTriggerWatchers: false);
				}
				else if (capi.Settings.Bool["showMinimapHud"])
				{
					worldMapDlg.Open(EnumDialogType.HUD);
					return;
				}
				worldMapDlg.TryClose();
			}
			return;
		}
		worldMapDlg = new GuiDialogWorldMap(onViewChangedClient, capi, getTabsOrdered());
		worldMapDlg.OnClosed += delegate
		{
			foreach (MapLayer mapLayer2 in MapLayers)
			{
				mapLayer2.OnMapClosedClient();
			}
			clientChannel.SendPacket(new OnMapToggle
			{
				OpenOrClose = false
			});
		};
		worldMapDlg.Open(asType);
		foreach (MapLayer mapLayer3 in MapLayers)
		{
			mapLayer3.OnMapOpenedClient();
		}
		clientChannel.SendPacket(new OnMapToggle
		{
			OpenOrClose = true
		});
		if (asType == EnumDialogType.HUD)
		{
			capi.Settings.Bool.Set("showMinimapHud", value: true, shouldTriggerWatchers: false);
		}
	}

	private List<string> getTabsOrdered()
	{
		Dictionary<string, double> tabs = new Dictionary<string, double>();
		foreach (MapLayer layer in MapLayers)
		{
			if (!tabs.ContainsKey(layer.LayerGroupCode))
			{
				if (!LayerGroupPositions.TryGetValue(layer.LayerGroupCode, out var pos))
				{
					pos = 1.0;
				}
				tabs[layer.LayerGroupCode] = pos;
			}
		}
		return (from val in tabs
			orderby val.Value
			select val.Key).ToList();
	}

	private void onViewChangedClient(List<Vec2i> nowVisible, List<Vec2i> nowHidden)
	{
		foreach (MapLayer mapLayer in MapLayers)
		{
			mapLayer.OnViewChangedClient(nowVisible, nowHidden);
		}
		clientChannel.SendPacket(new OnViewChangedPacket
		{
			NowVisible = nowVisible,
			NowHidden = nowHidden
		});
	}

	public void TranslateWorldPosToViewPos(Vec3d worldPos, ref Vec2f viewPos)
	{
		worldMapDlg.TranslateWorldPosToViewPos(worldPos, ref viewPos);
	}

	public void SendMapDataToServer(MapLayer forMapLayer, byte[] data)
	{
		if (api.Side != EnumAppSide.Server)
		{
			List<MapLayerData> maplayerdatas = new List<MapLayerData>();
			maplayerdatas.Add(new MapLayerData
			{
				Data = data,
				ForMapLayer = MapLayerRegistry.FirstOrDefault((KeyValuePair<string, Type> x) => x.Value == forMapLayer.GetType()).Key
			});
			clientChannel.SendPacket(new MapLayerUpdate
			{
				Maplayers = maplayerdatas.ToArray()
			});
		}
	}

	public override void StartServerSide(ICoreServerAPI sapi)
	{
		sapi.Event.ServerRunPhase(EnumServerRunPhase.RunGame, OnLvlFinalize);
		sapi.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, delegate
		{
			IsShuttingDown = true;
		});
		serverChannel = sapi.Network.RegisterChannel("worldmap").RegisterMessageType(typeof(MapLayerUpdate)).RegisterMessageType(typeof(OnViewChangedPacket))
			.RegisterMessageType(typeof(OnMapToggle))
			.SetMessageHandler<OnMapToggle>(OnMapToggledServer)
			.SetMessageHandler<OnViewChangedPacket>(OnViewChangedServer)
			.SetMessageHandler<MapLayerUpdate>(OnMapLayerDataReceivedServer);
	}

	private void OnMapLayerDataReceivedServer(IServerPlayer fromPlayer, MapLayerUpdate msg)
	{
		for (int i = 0; i < msg.Maplayers.Length; i++)
		{
			Type type = MapLayerRegistry[msg.Maplayers[i].ForMapLayer];
			MapLayers.FirstOrDefault((MapLayer x) => x.GetType() == type)?.OnDataFromClient(msg.Maplayers[i].Data);
		}
	}

	private void OnMapToggledServer(IServerPlayer fromPlayer, OnMapToggle msg)
	{
		foreach (MapLayer layer in MapLayers)
		{
			if (layer.DataSide != 0)
			{
				if (msg.OpenOrClose)
				{
					layer.OnMapOpenedServer(fromPlayer);
				}
				else
				{
					layer.OnMapClosedServer(fromPlayer);
				}
			}
		}
	}

	private void OnViewChangedServer(IServerPlayer fromPlayer, OnViewChangedPacket networkMessage)
	{
		List<Vec2i> empty = new List<Vec2i>(0);
		foreach (MapLayer layer in MapLayers)
		{
			if (layer.DataSide != 0)
			{
				layer.OnViewChangedServer(fromPlayer, networkMessage.NowVisible, empty);
			}
		}
	}

	public void SendMapDataToClient(MapLayer forMapLayer, IServerPlayer forPlayer, byte[] data)
	{
		if (api.Side != EnumAppSide.Client && forPlayer.ConnectionState == EnumClientState.Playing)
		{
			MapLayerData[] maplayerdatas = new MapLayerData[1]
			{
				new MapLayerData
				{
					Data = data,
					ForMapLayer = MapLayerRegistry.FirstOrDefault((KeyValuePair<string, Type> x) => x.Value == forMapLayer.GetType()).Key
				}
			};
			serverChannel.SendPacket(new MapLayerUpdate
			{
				Maplayers = maplayerdatas
			}, forPlayer);
		}
	}
}
