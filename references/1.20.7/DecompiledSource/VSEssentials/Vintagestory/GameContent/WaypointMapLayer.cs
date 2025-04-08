using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class WaypointMapLayer : MarkerMapLayer
{
	public List<Waypoint> Waypoints = new List<Waypoint>();

	private ICoreServerAPI sapi;

	public List<Waypoint> ownWaypoints = new List<Waypoint>();

	private List<MapComponent> wayPointComponents = new List<MapComponent>();

	public MeshRef quadModel;

	private List<MapComponent> tmpWayPointComponents = new List<MapComponent>();

	public Dictionary<string, LoadedTexture> texturesByIcon;

	private static string[] hexcolors = new string[36]
	{
		"#F9D0DC", "#F179AF", "#F15A4A", "#ED272A", "#A30A35", "#FFDE98", "#EFFD5F", "#F6EA5E", "#FDBB3A", "#C8772E",
		"#F47832", "C3D941", "#9FAB3A", "#94C948", "#47B749", "#366E4F", "#516D66", "93D7E3", "#7698CF", "#20909E",
		"#14A4DD", "#204EA2", "#28417A", "#C395C4", "#92479B", "#8E007E", "#5E3896", "D9D4CE", "#AFAAA8", "#706D64",
		"#4F4C2B", "#BF9C86", "#9885530", "#5D3D21", "#FFFFFF", "#080504"
	};

	public override bool RequireChunkLoaded => false;

	public OrderedDictionary<string, CreateIconTextureDelegate> WaypointIcons { get; set; } = new OrderedDictionary<string, CreateIconTextureDelegate>();


	public List<int> WaypointColors { get; set; } = new List<int>();


	public override string Title => "Player Set Markers";

	public override EnumMapAppSide DataSide => EnumMapAppSide.Server;

	public override string LayerGroupCode => "waypoints";

	public WaypointMapLayer(ICoreAPI api, IWorldMapManager mapSink)
		: base(api, mapSink)
	{
		WaypointColors = new List<int>();
		for (int i = 0; i < hexcolors.Length; i++)
		{
			WaypointColors.Add(ColorUtil.Hex2Int(hexcolors[i]));
		}
		List<IAsset> many = api.Assets.GetMany("textures/icons/worldmap/", null, loadAsset: false);
		ICoreClientAPI capi = api as ICoreClientAPI;
		foreach (IAsset icon in many)
		{
			string name = icon.Name.Substring(0, icon.Name.IndexOf('.'));
			name = Regex.Replace(name, "\\d+\\-", "");
			if (api.Side == EnumAppSide.Server)
			{
				WaypointIcons[name] = () => (LoadedTexture)null;
				continue;
			}
			WaypointIcons[name] = delegate
			{
				int num = (int)Math.Ceiling(20f * RuntimeEnv.GUIScale);
				return capi.Gui.LoadSvg(icon.Location, num, num, num, num, -1);
			};
			capi.Gui.Icons.CustomIcons["wp" + name.UcFirst()] = delegate(Context ctx, int x, int y, float w, float h, double[] rgba)
			{
				int value = ColorUtil.ColorFromRgba(rgba);
				capi.Gui.DrawSvg(icon, ctx.GetTarget() as ImageSurface, ctx.Matrix, x, y, (int)w, (int)h, value);
			};
		}
		if (api.Side == EnumAppSide.Server)
		{
			ICoreServerAPI sapi = (this.sapi = api as ICoreServerAPI);
			sapi.Event.GameWorldSave += OnSaveGameGettingSaved;
			sapi.Event.PlayerDeath += Event_PlayerDeath;
			CommandArgumentParsers parsers = sapi.ChatCommands.Parsers;
			sapi.ChatCommands.Create("waypoint").WithDescription("Put a waypoint at this location which will be visible for you on the map").RequiresPrivilege(Privilege.chat)
				.BeginSubCommand("deathwp")
				.WithDescription("Enable/Disable automatic adding of a death waypoint")
				.WithArgs(parsers.OptionalBool("enabled"))
				.RequiresPlayer()
				.HandleWith(OnCmdWayPointDeathWp)
				.EndSubCommand()
				.BeginSubCommand("add")
				.WithDescription("Add a waypoint to the map")
				.RequiresPlayer()
				.WithArgs(parsers.Color("color"), parsers.All("title"))
				.HandleWith(OnCmdWayPointAdd)
				.EndSubCommand()
				.BeginSubCommand("addp")
				.RequiresPlayer()
				.WithDescription("Add a waypoint to the map")
				.WithArgs(parsers.Color("color"), parsers.All("title"))
				.HandleWith(OnCmdWayPointAddp)
				.EndSubCommand()
				.BeginSubCommand("addat")
				.WithDescription("Add a waypoint to the map")
				.RequiresPlayer()
				.WithArgs(parsers.WorldPosition("position"), parsers.Bool("pinned"), parsers.Color("color"), parsers.All("title"))
				.HandleWith(OnCmdWayPointAddat)
				.EndSubCommand()
				.BeginSubCommand("addati")
				.WithDescription("Add a waypoint to the map")
				.RequiresPlayer()
				.WithArgs(parsers.Word("icon"), parsers.WorldPosition("position"), parsers.Bool("pinned"), parsers.Color("color"), parsers.All("title"))
				.HandleWith(OnCmdWayPointAddati)
				.EndSubCommand()
				.BeginSubCommand("modify")
				.WithDescription("")
				.RequiresPlayer()
				.WithArgs(parsers.Int("waypoint_id"), parsers.Color("color"), parsers.Word("icon"), parsers.Bool("pinned"), parsers.All("title"))
				.HandleWith(OnCmdWayPointModify)
				.EndSubCommand()
				.BeginSubCommand("remove")
				.WithDescription("Remove a waypoint by its id. Get a lost of ids using /waypoint list")
				.RequiresPlayer()
				.WithArgs(parsers.Int("waypoint_id"))
				.HandleWith(OnCmdWayPointRemove)
				.EndSubCommand()
				.BeginSubCommand("list")
				.WithDescription("List your own waypoints")
				.RequiresPlayer()
				.WithArgs(parsers.OptionalWordRange("details", "details", "d"))
				.HandleWith(OnCmdWayPointList)
				.EndSubCommand();
			sapi.ChatCommands.Create("tpwp").WithDescription("Teleport yourself to a waypoint starting with the supplied name").RequiresPrivilege(Privilege.tp)
				.WithArgs(parsers.All("name"))
				.HandleWith(OnCmdTpTo);
		}
		else
		{
			quadModel = (api as ICoreClientAPI).Render.UploadMesh(QuadMeshUtil.GetQuad());
		}
	}

	private TextCommandResult OnCmdWayPointList(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var textCommandResult))
		{
			return textCommandResult;
		}
		bool detailed = args[0] as string == "details" || args[0] as string == "d";
		StringBuilder wps = new StringBuilder();
		int i = 0;
		Waypoint[] array = Waypoints.Where((Waypoint p) => p.OwningPlayerUid == args.Caller.Player.PlayerUID).ToArray();
		foreach (Waypoint p2 in array)
		{
			Vec3d pos = p2.Position.Clone();
			pos.X -= api.World.DefaultSpawnPosition.X;
			pos.Z -= api.World.DefaultSpawnPosition.Z;
			if (detailed)
			{
				wps.AppendLine($"{i}: {p2.Title} at {pos.AsBlockPos} {ColorUtil.Int2Hex(p2.Color)} {p2.Icon}");
			}
			else
			{
				wps.AppendLine($"{i}: {p2.Title} at {pos.AsBlockPos}");
			}
			i++;
		}
		if (wps.Length == 0)
		{
			return TextCommandResult.Success(Lang.Get("You have no waypoints"));
		}
		return TextCommandResult.Success(Lang.Get("Your waypoints:") + "\n" + wps.ToString());
	}

	private bool IsMapDisallowed(out TextCommandResult response)
	{
		if (!api.World.Config.GetBool("allowMap", defaultValue: true))
		{
			response = TextCommandResult.Success(Lang.Get("Maps are disabled on this server"));
			return true;
		}
		response = null;
		return false;
	}

	private TextCommandResult OnCmdWayPointRemove(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var textCommandResult))
		{
			return textCommandResult;
		}
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		int id = (int)args.Parsers[0].GetValue();
		Waypoint[] ownwpaypoints = Waypoints.Where((Waypoint p) => p.OwningPlayerUid == player.PlayerUID).ToArray();
		if (ownwpaypoints.Length == 0)
		{
			return TextCommandResult.Success(Lang.Get("You have no waypoints to delete"));
		}
		if (args.Parsers[0].IsMissing || id < 0 || id >= ownwpaypoints.Length)
		{
			return TextCommandResult.Success(Lang.Get("Invalid waypoint number, valid ones are 0..{0}", ownwpaypoints.Length - 1));
		}
		Waypoints.Remove(ownwpaypoints[id]);
		RebuildMapComponents();
		ResendWaypoints(player);
		return TextCommandResult.Success(Lang.Get("Ok, deleted waypoint."));
	}

	private TextCommandResult OnCmdWayPointDeathWp(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var textCommandResult))
		{
			return textCommandResult;
		}
		if (!api.World.Config.GetBool("allowDeathwaypointing", defaultValue: true))
		{
			return TextCommandResult.Success(Lang.Get("Death waypointing is disabled on this server"));
		}
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		if (args.Parsers[0].IsMissing)
		{
			bool on = player.GetModData("deathWaypointing", defaultValue: false);
			return TextCommandResult.Success(Lang.Get("Death waypoint is {0}", on ? Lang.Get("on") : Lang.Get("off")));
		}
		bool on2 = (bool)args.Parsers[0].GetValue();
		player.SetModData("deathWaypointing", on2);
		return TextCommandResult.Success(Lang.Get("Death waypoint now {0}", on2 ? Lang.Get("on") : Lang.Get("off")));
	}

	private void Event_PlayerDeath(IServerPlayer byPlayer, DamageSource damageSource)
	{
		if (!api.World.Config.GetBool("allowMap", defaultValue: true) || !api.World.Config.GetBool("allowDeathwaypointing", defaultValue: true) || !byPlayer.GetModData("deathWaypointing", defaultValue: true))
		{
			return;
		}
		string title = Lang.Get("You died here");
		for (int i = 0; i < Waypoints.Count; i++)
		{
			Waypoint wp = Waypoints[i];
			if (wp.OwningPlayerUid == byPlayer.PlayerUID && wp.Title == title)
			{
				Waypoints.RemoveAt(i);
				i--;
			}
		}
		Waypoint waypoint = new Waypoint
		{
			Color = ColorUtil.ColorFromRgba(200, 200, 200, 255),
			OwningPlayerUid = byPlayer.PlayerUID,
			Position = byPlayer.Entity.Pos.XYZ,
			Title = title,
			Icon = "gravestone",
			Pinned = true
		};
		AddWaypoint(waypoint, byPlayer);
	}

	private TextCommandResult OnCmdTpTo(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		string name = (args.Parsers[0].GetValue() as string).ToLowerInvariant();
		Waypoint[] array = Waypoints.Where((Waypoint p) => p.OwningPlayerUid == player.PlayerUID).ToArray();
		foreach (Waypoint wp in array)
		{
			if (wp.Title != null && wp.Title.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
			{
				player.Entity.TeleportTo(wp.Position);
				return TextCommandResult.Success(Lang.Get("Ok teleported you to waypoint {0}.", wp.Title));
			}
		}
		return TextCommandResult.Success(Lang.Get("No such waypoint found"));
	}

	private TextCommandResult OnCmdWayPointAdd(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var textCommandResult))
		{
			return textCommandResult;
		}
		System.Drawing.Color parsedColor = (System.Drawing.Color)args.Parsers[0].GetValue();
		string title = args.Parsers[1].GetValue() as string;
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		return AddWp(player, player.Entity.Pos.XYZ, title, parsedColor, "circle", pinned: false);
	}

	private TextCommandResult OnCmdWayPointAddp(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var textCommandResult))
		{
			return textCommandResult;
		}
		System.Drawing.Color parsedColor = (System.Drawing.Color)args.Parsers[0].GetValue();
		string title = args.Parsers[1].GetValue() as string;
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		return AddWp(player, player.Entity.Pos.XYZ, title, parsedColor, "circle", pinned: true);
	}

	private TextCommandResult OnCmdWayPointAddat(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var textCommandResult))
		{
			return textCommandResult;
		}
		Vec3d pos = args.Parsers[0].GetValue() as Vec3d;
		bool pinned = (bool)args.Parsers[1].GetValue();
		System.Drawing.Color parsedColor = (System.Drawing.Color)args.Parsers[2].GetValue();
		string title = args.Parsers[3].GetValue() as string;
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		return AddWp(player, pos, title, parsedColor, "circle", pinned);
	}

	private TextCommandResult OnCmdWayPointAddati(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var textCommandResult))
		{
			return textCommandResult;
		}
		string icon = args.Parsers[0].GetValue() as string;
		Vec3d pos = args.Parsers[1].GetValue() as Vec3d;
		bool pinned = (bool)args.Parsers[2].GetValue();
		System.Drawing.Color parsedColor = (System.Drawing.Color)args.Parsers[3].GetValue();
		string title = args.Parsers[4].GetValue() as string;
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		return AddWp(player, pos, title, parsedColor, icon, pinned);
	}

	private TextCommandResult OnCmdWayPointModify(TextCommandCallingArgs args)
	{
		if (IsMapDisallowed(out var textCommandResult))
		{
			return textCommandResult;
		}
		int wpIndex = (int)args.Parsers[0].GetValue();
		System.Drawing.Color parsedColor = (System.Drawing.Color)args.Parsers[1].GetValue();
		string icon = args.Parsers[2].GetValue() as string;
		bool pinned = (bool)args.Parsers[3].GetValue();
		string title = args.Parsers[4].GetValue() as string;
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		Waypoint[] playerWaypoints = Waypoints.Where((Waypoint p) => p.OwningPlayerUid == player.PlayerUID).ToArray();
		if (args.Parsers[0].IsMissing || wpIndex < 0 || wpIndex >= playerWaypoints.Length)
		{
			return TextCommandResult.Success(Lang.Get("command-modwaypoint-invalidindex", playerWaypoints.Length - 1));
		}
		if (string.IsNullOrEmpty(title))
		{
			return TextCommandResult.Success(Lang.Get("command-waypoint-notext"));
		}
		playerWaypoints[wpIndex].Color = parsedColor.ToArgb() | -16777216;
		playerWaypoints[wpIndex].Title = title;
		playerWaypoints[wpIndex].Pinned = pinned;
		if (icon != null)
		{
			playerWaypoints[wpIndex].Icon = icon;
		}
		ResendWaypoints(player);
		return TextCommandResult.Success(Lang.Get("Ok, waypoint nr. {0} modified", wpIndex));
	}

	private TextCommandResult AddWp(IServerPlayer player, Vec3d pos, string title, System.Drawing.Color parsedColor, string icon, bool pinned)
	{
		if (string.IsNullOrEmpty(title))
		{
			return TextCommandResult.Success(Lang.Get("command-waypoint-notext"));
		}
		Waypoint waypoint = new Waypoint
		{
			Color = (parsedColor.ToArgb() | -16777216),
			OwningPlayerUid = player.PlayerUID,
			Position = pos,
			Title = title,
			Icon = icon,
			Pinned = pinned,
			Guid = Guid.NewGuid().ToString()
		};
		int nr = AddWaypoint(waypoint, player);
		return TextCommandResult.Success(Lang.Get("Ok, waypoint nr. {0} added", nr));
	}

	public int AddWaypoint(Waypoint waypoint, IServerPlayer player)
	{
		Waypoints.Add(waypoint);
		Waypoint[] array = Waypoints.Where((Waypoint p) => p.OwningPlayerUid == player.PlayerUID).ToArray();
		ResendWaypoints(player);
		return array.Length - 1;
	}

	private void OnSaveGameGettingSaved()
	{
		sapi.WorldManager.SaveGame.StoreData("playerMapMarkers_v2", SerializerUtil.Serialize(Waypoints));
	}

	public override void OnViewChangedServer(IServerPlayer fromPlayer, List<Vec2i> nowVisible, List<Vec2i> nowHidden)
	{
		ResendWaypoints(fromPlayer);
	}

	public override void OnMapOpenedClient()
	{
		reloadIconTextures();
		ensureIconTexturesLoaded();
		RebuildMapComponents();
	}

	public void reloadIconTextures()
	{
		if (texturesByIcon != null)
		{
			foreach (KeyValuePair<string, LoadedTexture> item in texturesByIcon)
			{
				item.Value.Dispose();
			}
		}
		texturesByIcon = null;
		ensureIconTexturesLoaded();
	}

	protected void ensureIconTexturesLoaded()
	{
		if (texturesByIcon != null)
		{
			return;
		}
		texturesByIcon = new Dictionary<string, LoadedTexture>();
		foreach (KeyValuePair<string, CreateIconTextureDelegate> val in WaypointIcons)
		{
			texturesByIcon[val.Key] = val.Value();
		}
	}

	public override void OnMapClosedClient()
	{
		foreach (MapComponent val in tmpWayPointComponents)
		{
			wayPointComponents.Remove(val);
		}
		tmpWayPointComponents.Clear();
	}

	public override void Dispose()
	{
		if (texturesByIcon != null)
		{
			foreach (KeyValuePair<string, LoadedTexture> item in texturesByIcon)
			{
				item.Value.Dispose();
			}
		}
		texturesByIcon = null;
		quadModel?.Dispose();
		base.Dispose();
	}

	public override void OnLoaded()
	{
		if (sapi == null)
		{
			return;
		}
		try
		{
			byte[] data = sapi.WorldManager.SaveGame.GetData("playerMapMarkers_v2");
			if (data != null)
			{
				Waypoints = SerializerUtil.Deserialize<List<Waypoint>>(data);
				sapi.World.Logger.Notification("Successfully loaded " + Waypoints.Count + " waypoints");
			}
			else
			{
				data = sapi.WorldManager.SaveGame.GetData("playerMapMarkers");
				if (data != null)
				{
					Waypoints = JsonUtil.FromBytes<List<Waypoint>>(data);
				}
			}
			for (int i = 0; i < Waypoints.Count; i++)
			{
				Waypoint wp2 = Waypoints[i];
				if (wp2 == null)
				{
					sapi.World.Logger.Error("Waypoint with no information loaded, will remove");
					Waypoints.RemoveAt(i);
					i--;
				}
				if (wp2.Title == null)
				{
					wp2.Title = wp2.Text;
				}
			}
		}
		catch (Exception e)
		{
			sapi.World.Logger.Error("Failed deserializing player map markers. Won't load them, sorry! Exception thrown:");
			sapi.World.Logger.Error(e);
		}
		foreach (Waypoint wp in Waypoints)
		{
			if (wp.Guid == null)
			{
				wp.Guid = Guid.NewGuid().ToString();
			}
		}
	}

	public override void OnDataFromServer(byte[] data)
	{
		ownWaypoints.Clear();
		ownWaypoints.AddRange(SerializerUtil.Deserialize<List<Waypoint>>(data));
		RebuildMapComponents();
	}

	public void AddTemporaryWaypoint(Waypoint waypoint)
	{
		WaypointMapComponent comp = new WaypointMapComponent(ownWaypoints.Count, waypoint, this, api as ICoreClientAPI);
		wayPointComponents.Add(comp);
		tmpWayPointComponents.Add(comp);
	}

	private void RebuildMapComponents()
	{
		if (!mapSink.IsOpened)
		{
			return;
		}
		foreach (MapComponent val in tmpWayPointComponents)
		{
			wayPointComponents.Remove(val);
		}
		foreach (WaypointMapComponent wayPointComponent in wayPointComponents)
		{
			wayPointComponent.Dispose();
		}
		wayPointComponents.Clear();
		for (int i = 0; i < ownWaypoints.Count; i++)
		{
			WaypointMapComponent comp = new WaypointMapComponent(i, ownWaypoints[i], this, api as ICoreClientAPI);
			wayPointComponents.Add(comp);
		}
		wayPointComponents.AddRange(tmpWayPointComponents);
	}

	public override void Render(GuiElementMap mapElem, float dt)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (MapComponent wayPointComponent in wayPointComponents)
		{
			wayPointComponent.Render(mapElem, dt);
		}
	}

	public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (MapComponent wayPointComponent in wayPointComponents)
		{
			wayPointComponent.OnMouseMove(args, mapElem, hoverText);
		}
	}

	public override void OnMouseUpClient(MouseEvent args, GuiElementMap mapElem)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (MapComponent wayPointComponent in wayPointComponents)
		{
			wayPointComponent.OnMouseUpOnElement(args, mapElem);
			if (args.Handled)
			{
				break;
			}
		}
	}

	private void ResendWaypoints(IServerPlayer toPlayer)
	{
		Dictionary<int, PlayerGroupMembership> memberOfGroups = toPlayer.ServerData.PlayerGroupMemberships;
		List<Waypoint> hisMarkers = new List<Waypoint>();
		foreach (Waypoint marker in Waypoints)
		{
			if (!(toPlayer.PlayerUID != marker.OwningPlayerUid) || memberOfGroups.ContainsKey(marker.OwningPlayerGroupId))
			{
				hisMarkers.Add(marker);
			}
		}
		mapSink.SendMapDataToClient(this, toPlayer, SerializerUtil.Serialize(hisMarkers));
	}
}
