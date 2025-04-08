using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class OreMapLayer : MarkerMapLayer
{
	public Dictionary<string, List<PropickReading>> PropickReadingsByPlayer = new Dictionary<string, List<PropickReading>>();

	private ICoreServerAPI sapi;

	public List<PropickReading> ownPropickReadings = new List<PropickReading>();

	private List<MapComponent> wayPointComponents = new List<MapComponent>();

	private List<MapComponent> tmpWayPointComponents = new List<MapComponent>();

	public MeshRef quadModel;

	private ICoreClientAPI capi;

	private CreateIconTextureDelegate oremapIconDele;

	public LoadedTexture oremapTexture;

	private string filterByOreCode;

	public override bool RequireChunkLoaded => false;

	public override string Title => "Player Ore map readings";

	public override EnumMapAppSide DataSide => EnumMapAppSide.Server;

	public override string LayerGroupCode => "prospecting";

	public OreMapLayer(ICoreAPI api, IWorldMapManager mapSink)
		: base(api, mapSink)
	{
		OreMapLayer oreMapLayer = this;
		if (api.Side == EnumAppSide.Client)
		{
			capi = api as ICoreClientAPI;
			IAsset iconAsset = api.Assets.Get("textures/icons/worldmap/0-circle.svg");
			oremapIconDele = delegate
			{
				int num = (int)Math.Ceiling(20f * RuntimeEnv.GUIScale);
				return oreMapLayer.capi.Gui.LoadSvg(iconAsset.Location, num, num, num, num, -1);
			};
			capi.Gui.Icons.CustomIcons["wpOreMapIcon"] = delegate(Context ctx, int x, int y, float w, float h, double[] rgba)
			{
				int value = ColorUtil.ColorFromRgba(rgba);
				oreMapLayer.capi.Gui.DrawSvg(iconAsset, ctx.GetTarget() as ImageSurface, ctx.Matrix, x, y, (int)w, (int)h, value);
			};
		}
		if (api.Side == EnumAppSide.Server)
		{
			ICoreServerAPI sapi = (this.sapi = api as ICoreServerAPI);
			sapi.Event.GameWorldSave += OnSaveGameGettingSaved;
			sapi.Event.PlayerDisconnect += OnPlayerDisconnect;
		}
		else
		{
			quadModel = (api as ICoreClientAPI).Render.UploadMesh(QuadMeshUtil.GetQuad());
		}
	}

	public override void ComposeDialogExtras(GuiDialogWorldMap guiDialogWorldMap, GuiComposer compo)
	{
		string key = "worldmap-layer-" + LayerGroupCode;
		HashSet<string> orecodes = new HashSet<string>();
		foreach (PropickReading ownPropickReading in ownPropickReadings)
		{
			foreach (KeyValuePair<string, OreReading> oreReading in ownPropickReading.OreReadings)
			{
				orecodes.Add(oreReading.Key);
			}
		}
		string[] values = new string[1].Append(orecodes.ToArray());
		string[] names = new string[1] { Lang.Get("worldmap-ores-everything") }.Append(orecodes.Select((string code) => Lang.Get("ore-" + code)).ToArray());
		ElementBounds dlgBounds = ElementStdBounds.AutosizedMainDialog.WithFixedPosition((compo.Bounds.renderX + compo.Bounds.OuterWidth) / (double)RuntimeEnv.GUIScale + 10.0, compo.Bounds.renderY / (double)RuntimeEnv.GUIScale).WithAlignment(EnumDialogArea.None);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		guiDialogWorldMap.Composers[key] = capi.Gui.CreateCompo(key, dlgBounds).AddShadedDialogBG(bgBounds, withTitleBar: false).AddDialogTitleBar(Lang.Get("maplayer-prospecting"), delegate
		{
			guiDialogWorldMap.Composers[key].Enabled = false;
		})
			.BeginChildElements(bgBounds)
			.AddDropDown(values, names, 0, onSelectionChanged, ElementBounds.Fixed(0.0, 30.0, 160.0, 35.0))
			.EndChildElements()
			.Compose();
		guiDialogWorldMap.Composers[key].Enabled = false;
	}

	private void onSelectionChanged(string code, bool selected)
	{
		filterByOreCode = code;
		RebuildMapComponents();
	}

	public int AddWaypoint(PropickReading waypoint, IServerPlayer player)
	{
		List<PropickReading> orLoadReadings = getOrLoadReadings(player);
		orLoadReadings.Add(waypoint);
		ResendWaypoints(player);
		return orLoadReadings.Count - 1;
	}

	public List<PropickReading> getOrLoadReadings(IPlayer player)
	{
		if (PropickReadingsByPlayer.TryGetValue(player.PlayerUID, out var readings))
		{
			return readings;
		}
		byte[] data = sapi.WorldManager.SaveGame.GetData("oreMapMarkers-" + player.PlayerUID);
		if (data != null)
		{
			return PropickReadingsByPlayer[player.PlayerUID] = SerializerUtil.Deserialize<List<PropickReading>>(data);
		}
		return PropickReadingsByPlayer[player.PlayerUID] = new List<PropickReading>();
	}

	private void OnSaveGameGettingSaved()
	{
		ISaveGame savegame = sapi.WorldManager.SaveGame;
		foreach (KeyValuePair<string, List<PropickReading>> val in PropickReadingsByPlayer)
		{
			savegame.StoreData("oreMapMarkers-" + val.Key, SerializerUtil.Serialize(val.Value));
		}
	}

	private void OnPlayerDisconnect(IServerPlayer player)
	{
		try
		{
			if (PropickReadingsByPlayer.TryGetValue(player.PlayerUID, out var readings))
			{
				sapi.WorldManager.SaveGame.StoreData("oreMapMarkers-" + player.PlayerUID, SerializerUtil.Serialize(readings));
			}
			PropickReadingsByPlayer.Remove(player.PlayerUID);
		}
		catch
		{
		}
	}

	public override void OnViewChangedServer(IServerPlayer fromPlayer, List<Vec2i> nowVisible, List<Vec2i> nowHidden)
	{
		ResendWaypoints(fromPlayer);
	}

	public override void OnMapOpenedClient()
	{
		ensureIconTexturesLoaded();
		RebuildMapComponents();
	}

	protected void ensureIconTexturesLoaded()
	{
		oremapTexture?.Dispose();
		oremapTexture = oremapIconDele();
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
		oremapTexture?.Dispose();
		oremapTexture = null;
		quadModel?.Dispose();
		base.Dispose();
	}

	public override void OnDataFromServer(byte[] data)
	{
		ownPropickReadings.Clear();
		ownPropickReadings.AddRange(SerializerUtil.Deserialize<List<PropickReading>>(data));
		RebuildMapComponents();
	}

	public void RebuildMapComponents()
	{
		if (!mapSink.IsOpened)
		{
			return;
		}
		foreach (MapComponent val in tmpWayPointComponents)
		{
			wayPointComponents.Remove(val);
		}
		foreach (OreMapComponent wayPointComponent in wayPointComponents)
		{
			wayPointComponent.Dispose();
		}
		wayPointComponents.Clear();
		for (int i = 0; i < ownPropickReadings.Count; i++)
		{
			PropickReading reading = ownPropickReadings[i];
			if (filterByOreCode == null || reading.GetTotalFactor(filterByOreCode) > PropickReading.MentionThreshold)
			{
				OreMapComponent comp = new OreMapComponent(i, reading, this, api as ICoreClientAPI, filterByOreCode);
				wayPointComponents.Add(comp);
			}
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
		List<PropickReading> plrReadings = getOrLoadReadings(toPlayer);
		mapSink.SendMapDataToClient(this, toPlayer, SerializerUtil.Serialize(plrReadings));
	}

	public void Delete(IPlayer player, int waypointIndex)
	{
		if (api.Side == EnumAppSide.Client)
		{
			ownPropickReadings.RemoveAt(waypointIndex);
			(api as ICoreClientAPI).Network.GetChannel("oremap").SendPacket(new DeleteReadingPacket
			{
				Index = waypointIndex
			});
			RebuildMapComponents();
		}
		else
		{
			List<PropickReading> plrReadings = getOrLoadReadings(player);
			if (plrReadings.Count > waypointIndex)
			{
				plrReadings.RemoveAt(waypointIndex);
			}
		}
	}
}
