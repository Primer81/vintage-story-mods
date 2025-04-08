using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityDeconstructTool : CollectibleBehavior
{
	public EntityDeconstructTool(CollectibleObject collObj)
		: base(collObj)
	{
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
	{
		if (entitySel != null)
		{
			JsonObject attributes = entitySel.Entity.Properties.Attributes;
			if (attributes != null && attributes.IsTrue("deconstructible"))
			{
				handHandling = EnumHandHandling.PreventDefault;
				handling = EnumHandling.PreventDefault;
				byEntity.StartAnimation("saw");
			}
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
	{
		if (entitySel == null)
		{
			byEntity.StopAnimation("saw");
			return false;
		}
		handling = EnumHandling.PreventDefault;
		if (byEntity.World.Side == EnumAppSide.Server)
		{
			return true;
		}
		return secondsUsed < 6f;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
	{
		if (secondsUsed < 6f)
		{
			return;
		}
		if (entitySel == null)
		{
			byEntity.StopAnimation("saw");
			return;
		}
		entitySel.Entity.Die();
		byEntity.StopAnimation("saw");
		IWorldAccessor world = byEntity.World;
		if (world.Side == EnumAppSide.Server)
		{
			JsonItemStack[] array = entitySel.Entity.Properties.Attributes["deconstructDrops"].AsObject<JsonItemStack[]>();
			foreach (JsonItemStack dropStack in array)
			{
				if (dropStack.Resolve(world, string.Concat(byEntity.Code, " entity deconstruction drop.")))
				{
					world.SpawnItemEntity(dropStack.ResolvedItemstack, entitySel.Entity.ServerPos.XYZ);
				}
			}
		}
		base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handled)
	{
		byEntity.StopAnimation("saw");
		return true;
	}
}
