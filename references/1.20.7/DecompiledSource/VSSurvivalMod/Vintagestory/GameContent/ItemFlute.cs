using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class ItemFlute : Item
{
	protected string GroupCode = "mountableanimal";

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		long ela = api.World.ElapsedMilliseconds;
		long prevela = slot.Itemstack.Attributes.GetLong("lastPlayerMs", -99999L);
		if (prevela > ela)
		{
			prevela = ela - 4001;
		}
		if (ela - prevela > 4000)
		{
			slot.Itemstack.Attributes.SetLong("lastPlayerMs", ela);
			api.World.PlaySoundAt(new AssetLocation("sounds/instrument/elkcall"), byEntity, (byEntity as EntityPlayer)?.Player, 0.75f, 32f, 0.5f);
			if (api.Side == EnumAppSide.Server)
			{
				callElk(byEntity);
			}
			handling = EnumHandHandling.PreventDefault;
		}
	}

	private void callElk(EntityAgent byEntity)
	{
		IPlayer plr = (byEntity as EntityPlayer).Player;
		if (!api.ModLoader.GetModSystem<ModSystemEntityOwnership>().OwnerShipsByPlayerUid.TryGetValue(plr.PlayerUID, out var ownerships) || ownerships == null || !ownerships.TryGetValue(GroupCode, out var ownership))
		{
			return;
		}
		Entity entity = api.World.GetEntityById(ownership.EntityId);
		if (entity == null)
		{
			return;
		}
		EntityBehaviorMortallyWoundable mw = entity.GetBehavior<EntityBehaviorMortallyWoundable>();
		if ((mw != null && mw.HealthState == EnumEntityHealthState.MortallyWounded) || (mw != null && mw.HealthState == EnumEntityHealthState.Dead))
		{
			return;
		}
		AiTaskManager tm = entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager;
		AiTaskComeToOwner aitcto = tm.AllTasks.FirstOrDefault((IAiTask t) => t is AiTaskComeToOwner) as AiTaskComeToOwner;
		if (entity.ServerPos.DistanceTo(byEntity.ServerPos) > (double)aitcto.TeleportMaxRange)
		{
			return;
		}
		IMountable mount = entity?.GetInterface<IMountable>();
		if (mount != null)
		{
			if (mount.IsMountedBy(plr.Entity))
			{
				return;
			}
			if (mount.AnyMounted())
			{
				entity.GetBehavior<EntityBehaviorRideable>()?.UnmnountPassengers();
			}
		}
		entity.AlwaysActive = true;
		entity.State = EnumEntityState.Active;
		aitcto.allowTeleportCount = 1;
		tm.StopTasks();
		tm.ExecuteTask(aitcto, 0);
	}
}
