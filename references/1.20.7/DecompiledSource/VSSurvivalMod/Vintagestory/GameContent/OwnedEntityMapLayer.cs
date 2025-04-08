using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class OwnedEntityMapLayer : MarkerMapLayer
{
	private Dictionary<long, OwnedEntityMapComponent> MapComps = new Dictionary<long, OwnedEntityMapComponent>();

	private ICoreClientAPI capi;

	private LoadedTexture otherTexture;

	public override string Title => "Owned Creatures";

	public override EnumMapAppSide DataSide => EnumMapAppSide.Client;

	public override string LayerGroupCode => "ownedcreatures";

	public OwnedEntityMapLayer(ICoreAPI api, IWorldMapManager mapsink)
		: base(api, mapsink)
	{
		capi = api as ICoreClientAPI;
	}

	public void Reload()
	{
		Dispose();
		OnMapOpenedClient();
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
		ModSystemEntityOwnership mseo = capi.ModLoader.GetModSystem<ModSystemEntityOwnership>();
		foreach (OwnedEntityMapComponent value in MapComps.Values)
		{
			value?.Dispose();
		}
		MapComps.Clear();
		foreach (KeyValuePair<string, EntityOwnership> eo in mseo.SelfOwnerShips)
		{
			MapComps[eo.Value.EntityId] = new OwnedEntityMapComponent(capi, otherTexture, eo.Value, eo.Value.Color);
		}
	}

	public override void Render(GuiElementMap mapElem, float dt)
	{
		if (!base.Active)
		{
			return;
		}
		foreach (KeyValuePair<long, OwnedEntityMapComponent> mapComp in MapComps)
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
		foreach (KeyValuePair<long, OwnedEntityMapComponent> mapComp in MapComps)
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
		foreach (KeyValuePair<long, OwnedEntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value.OnMouseUpOnElement(args, mapElem);
		}
	}

	public override void Dispose()
	{
		foreach (KeyValuePair<long, OwnedEntityMapComponent> mapComp in MapComps)
		{
			mapComp.Value?.Dispose();
		}
		otherTexture?.Dispose();
		otherTexture = null;
	}
}
