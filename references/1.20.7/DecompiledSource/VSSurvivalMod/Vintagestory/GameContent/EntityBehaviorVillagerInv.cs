using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityBehaviorVillagerInv : EntityBehaviorContainer
{
	private InventoryGeneric inv;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "villagerinv";

	public EntityBehaviorVillagerInv(Entity entity)
		: base(entity)
	{
		inv = new InventoryGeneric(6, null, null);
	}

	public override string PropertyName()
	{
		return "villagerinv";
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		Api = entity.World.Api;
		inv.LateInitialize("villagerinv-" + entity.EntityId, Api);
		loadInv();
		base.Initialize(properties, attributes);
	}
}
