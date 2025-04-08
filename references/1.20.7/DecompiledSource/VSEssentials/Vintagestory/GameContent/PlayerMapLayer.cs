using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class PlayerMapLayer : MarkerMapLayer
{
	private Dictionary<IPlayer, EntityMapComponent> MapComps = new Dictionary<IPlayer, EntityMapComponent>();

	private ICoreClientAPI capi;

	private LoadedTexture ownTexture;

	private LoadedTexture otherTexture;

	public override string Title => "Players";

	public override EnumMapAppSide DataSide => EnumMapAppSide.Client;

	public override string LayerGroupCode => "terrain";

	public PlayerMapLayer(ICoreAPI api, IWorldMapManager mapsink)
		: base(api, mapsink)
	{
		capi = api as ICoreClientAPI;
	}

	private void Event_PlayerDespawn(IClientPlayer byPlayer)
	{
		if (MapComps.TryGetValue(byPlayer, out var mp))
		{
			mp.Dispose();
			MapComps.Remove(byPlayer);
		}
	}

	private void Event_PlayerSpawn(IClientPlayer byPlayer)
	{
		if ((!capi.World.Config.GetBool("mapHideOtherPlayers") || !(byPlayer.PlayerUID != capi.World.Player.PlayerUID)) && mapSink.IsOpened && !MapComps.ContainsKey(byPlayer))
		{
			EntityMapComponent cmp = new EntityMapComponent(capi, otherTexture, byPlayer.Entity);
			MapComps[byPlayer] = cmp;
		}
	}

	public override void OnLoaded()
	{
		if (capi != null)
		{
			capi.Event.PlayerEntitySpawn += Event_PlayerSpawn;
			capi.Event.PlayerEntityDespawn += Event_PlayerDespawn;
		}
	}

	public override void OnMapOpenedClient()
	{
		int size = (int)GuiElement.scaled(32.0);
		if (ownTexture == null)
		{
			ImageSurface surface2 = new ImageSurface(Format.Argb32, size, size);
			Context ctx2 = new Context(surface2);
			ctx2.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
			ctx2.Paint();
			capi.Gui.Icons.DrawMapPlayer(ctx2, 0, 0, size, size, new double[4] { 0.0, 0.0, 0.0, 1.0 }, new double[4] { 1.0, 1.0, 1.0, 1.0 });
			ownTexture = new LoadedTexture(capi, capi.Gui.LoadCairoTexture(surface2, linearMag: false), size / 2, size / 2);
			ctx2.Dispose();
			surface2.Dispose();
		}
		if (otherTexture == null)
		{
			ImageSurface surface = new ImageSurface(Format.Argb32, size, size);
			Context ctx = new Context(surface);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
			ctx.Paint();
			capi.Gui.Icons.DrawMapPlayer(ctx, 0, 0, size, size, new double[4] { 0.3, 0.3, 0.3, 1.0 }, new double[4] { 0.7, 0.7, 0.7, 1.0 });
			otherTexture = new LoadedTexture(capi, capi.Gui.LoadCairoTexture(surface, linearMag: false), size / 2, size / 2);
			ctx.Dispose();
			surface.Dispose();
		}
		IPlayer[] allOnlinePlayers = capi.World.AllOnlinePlayers;
		foreach (IPlayer player in allOnlinePlayers)
		{
			if (MapComps.TryGetValue(player, out var cmp))
			{
				cmp?.Dispose();
				MapComps.Remove(player);
			}
			if (player.Entity == null)
			{
				capi.World.Logger.Warning("Can't add player {0} to world map, missing entity :<", player.PlayerUID);
			}
			else if (!capi.World.Config.GetBool("mapHideOtherPlayers") || !(player.PlayerUID != capi.World.Player.PlayerUID))
			{
				cmp = new EntityMapComponent(capi, (player == capi.World.Player) ? ownTexture : otherTexture, player.Entity);
				MapComps[player] = cmp;
			}
		}
	}

	public override void Render(GuiElementMap mapElem, float dt)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (KeyValuePair<IPlayer, EntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value.Render(mapElem, dt);
		}
	}

	public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (KeyValuePair<IPlayer, EntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value.OnMouseMove(args, mapElem, hoverText);
		}
	}

	public override void OnMouseUpClient(MouseEvent args, GuiElementMap mapElem)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (KeyValuePair<IPlayer, EntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value.OnMouseUpOnElement(args, mapElem);
		}
	}

	public override void OnMapClosedClient()
	{
	}

	public override void Dispose()
	{
		foreach (KeyValuePair<IPlayer, EntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value?.Dispose();
		}
		ownTexture?.Dispose();
		ownTexture = null;
		otherTexture?.Dispose();
		otherTexture = null;
	}
}
