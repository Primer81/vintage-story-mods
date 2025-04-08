using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityBehaviorSeraphInventory : EntityBehaviorTexturedClothing
{
	private EntityAgent eagent;

	private InventoryGear inv;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "seraphinventory";

	public override string PropertyName()
	{
		return "seraphinventory";
	}

	public EntityBehaviorSeraphInventory(Entity entity)
		: base(entity)
	{
		eagent = entity as EntityAgent;
		inv = new InventoryGear(null, null);
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		Api = entity.World.Api;
		inv.LateInitialize("gearinv-" + entity.EntityId, Api);
		loadInv();
		eagent.WatchedAttributes.RegisterModifiedListener("wearablesInv", wearablesModified);
		base.Initialize(properties, attributes);
	}

	private void wearablesModified()
	{
		loadInv();
		eagent.MarkShapeModified();
	}
}
