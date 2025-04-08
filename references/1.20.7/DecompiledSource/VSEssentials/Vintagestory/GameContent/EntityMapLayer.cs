using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class EntityMapLayer : MarkerMapLayer
{
	private Dictionary<long, EntityMapComponent> MapComps = new Dictionary<long, EntityMapComponent>();

	private ICoreClientAPI capi;

	private LoadedTexture otherTexture;

	public override string Title => "Creatures";

	public override EnumMapAppSide DataSide => EnumMapAppSide.Client;

	public override string LayerGroupCode => "creatures";

	public EntityMapLayer(ICoreAPI api, IWorldMapManager mapsink)
		: base(api, mapsink)
	{
		capi = api as ICoreClientAPI;
	}

	public override void OnLoaded()
	{
		if (capi != null)
		{
			capi.Event.OnEntitySpawn += Event_OnEntitySpawn;
			capi.Event.OnEntityLoaded += Event_OnEntitySpawn;
			capi.Event.OnEntityDespawn += Event_OnEntityDespawn;
		}
	}

	private void Event_OnEntityDespawn(Entity entity, EntityDespawnData reasonData)
	{
		if (MapComps.TryGetValue(entity.EntityId, out var mp))
		{
			mp.Dispose();
			MapComps.Remove(entity.EntityId);
		}
	}

	private void Event_OnEntitySpawn(Entity entity)
	{
		if (!(entity is EntityPlayer) && !entity.Code.Path.Contains("drifter") && mapSink.IsOpened && !MapComps.ContainsKey(entity.EntityId))
		{
			EntityMapComponent cmp = new EntityMapComponent(capi, otherTexture, entity, entity.Properties.Color);
			MapComps[entity.EntityId] = cmp;
		}
	}

	public override void OnMapOpenedClient()
	{
		int size = (int)GuiElement.scaled(32.0);
		if (otherTexture == null)
		{
			ImageSurface surface = new ImageSurface(Format.Argb32, size, size);
			Context ctx = new Context(surface);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
			ctx.Paint();
			capi.Gui.Icons.DrawMapPlayer(ctx, 0, 0, size, size, new double[4] { 0.3, 0.3, 0.3, 1.0 }, new double[4] { 0.95, 0.95, 0.95, 1.0 });
			otherTexture = new LoadedTexture(capi, capi.Gui.LoadCairoTexture(surface, linearMag: false), size / 2, size / 2);
			ctx.Dispose();
			surface.Dispose();
		}
		foreach (KeyValuePair<long, Entity> val in capi.World.LoadedEntities)
		{
			if (!(val.Value is EntityPlayer))
			{
				if (MapComps.TryGetValue(val.Value.EntityId, out var cmp))
				{
					cmp?.Dispose();
					MapComps.Remove(val.Value.EntityId);
				}
				cmp = new EntityMapComponent(capi, otherTexture, val.Value, val.Value.Properties.Color);
				MapComps[val.Value.EntityId] = cmp;
			}
		}
	}

	public override void Render(GuiElementMap mapElem, float dt)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (KeyValuePair<long, EntityMapComponent> mapComp in MapComps)
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
		foreach (KeyValuePair<long, EntityMapComponent> mapComp in MapComps)
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
		foreach (KeyValuePair<long, EntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value.OnMouseUpOnElement(args, mapElem);
		}
	}

	public override void Dispose()
	{
		foreach (KeyValuePair<long, EntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value?.Dispose();
		}
		otherTexture?.Dispose();
		otherTexture = null;
	}
}
